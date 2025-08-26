/**
 * @Author: zhangpengjian
 * @Date: 2025/7/10 16:16:42
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/7/10 16:16:42
 * Description: 沙堡里程碑活动实例
 */

using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using Config;
using EL;
using FAT.Merge;
using EL.Resource;

namespace FAT
{
    using static PoolMapping;

    public class ActivityCastle : ActivityLike, IActivityOrderHandler, IBoardEntry
    {
        public CastleMilestone conf;
        public CastleMilestoneDetail confD;
        public CastleMilestoneGroup confG;
        public override bool Valid => conf != null;
        public override ActivityVisual Visual => VisualMain.visual;
        public VisualPopup VisualMain { get; } = new(UIConfig.UIActivityCastleMain);
        public VisualPopup VisualEnd { get; } = new(UIConfig.UIActivityCastleConvert);
        public VisualPopup VisualBegin { get; } = new(UIConfig.UIActivityCastleBegin);
        public bool Complete => false;
        public bool Claimed { get; set; }
        public List<RewardCommitData> rewardList = new();
        public int Score => score;
        public int ScorePhase => scorePhase;
        private int score;
        private int scorePhase;
        private int hasPopup;
        private readonly OutputSpawnBonusHandler bonusHandler = new();

        public ActivityCastle() { }


        public ActivityCastle(ActivityLite lite_)
        {
            Lite = lite_;
            conf = GetCastleMilestone(lite_.Param);
            if (conf == null) return;
            VisualMain.Setup(conf.EventMainTheme, this, active_: false);
            VisualEnd.Setup(conf.EventSettleTheme, this, active_: false);
            VisualBegin.Setup(conf.EventStartTheme, this, active_: false);
            Game.Manager.mergeBoardMan.RegisterGlobalSpawnBonusHandler(bonusHandler);
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            if (confD != null)
            {
                any.Add(ToRecord(1, confD.Id));
            }
            any.Add(ToRecord(2, score));
            any.Add(ToRecord(3, scorePhase));
            bonusHandler.Serialize(any, 1000);
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var gId = ReadInt(1, any);
            score = ReadInt(2, any);
            scorePhase = ReadInt(3, any);
            bonusHandler.Deserialize(any, 1000);
            SetupDetail(gId);
        }

        public override void SetupFresh()
        {
            var gId = Game.Manager.userGradeMan.GetTargetConfigDataId(conf.EventGroup);
            SetupDetail(gId);
            Game.Manager.screenPopup.TryQueue(VisualBegin.popup, PopupType.Login);
            hasPopup = 1;
        }

        public void SetupDetail(int gId)
        {
            confD = GetCastleMilestoneDetail(gId);
            if (confD == null)
            {
                DebugEx.Error($"failed to find castle milestone detail {gId}");
                return;
            }
            if (!IsComplete())
            {
                var idx = confD.MilestoneGroup[scorePhase];
                var c = GetCastleMilestoneGroup(idx);
                confG = c;
                bonusHandler.Init(c.Id, c.OutputsOne, c.OutputsTwo, c.OutputsFour, c.OutputsFixedOne, c.OutputsFixedTwo, c.OutputsFixedFour, c.WithoutputTime, null);
            }
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate()
        {
            if (!Valid) yield break;
            foreach (var v in Visual.ResEnumerate()) yield return v;
            foreach (var v in VisualEnd.ResEnumerate()) yield return v;
            foreach (var v in VisualBegin.ResEnumerate()) yield return v;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (IsComplete()) return;
            if (hasPopup == 1) return;
            popup_.TryQueue(VisualMain.popup, state_);
        }

        public override void Open()
        {
            Open(VisualMain.res);
        }

        public override void WhenEnd()
        {
            if (IsComplete()) return;
            if (!UIManager.Instance.IsShow(VisualMain.res.ActiveR))
            {
                VisualMain.Popup();
            }
            TryConvert();
        }

        public void TryConvert()
        {
            Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(bonusHandler);
            var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);
            ActivityExpire.ConvertToReward(conf.ExpireItem, list, ReasonString.castle_convert);
            if (list.Count > 0)
            {
                Game.Manager.screenPopup.TryQueue(VisualEnd.popup, PopupType.Login, listT);
            }
            else
            {
                listT.Free();
            }
        }

        public void AddMilestoneScore(int count)
        {
            if (IsComplete()) return;
            score += count;
            
            // 循环处理所有可能的里程碑
            while (score >= confG.MilestoneScore && !IsComplete())
            {
                score -= confG.MilestoneScore;
                rewardList.Clear();
                foreach (var r in confG.MilestoneReward)
                {
                    var reward = r.ConvertToRewardConfig();
                    var commit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.castle_milestone);
                    rewardList.Add(commit);
                }
                scorePhase++;
                DataTracker.event_castle_milestone.Track(this, scorePhase, confD.MilestoneGroup.Count, confD.Diff, 1, scorePhase == confD.MilestoneGroup.Count);
                MessageCenter.Get<MSG.CASTLE_MILESTONE_CHANGE>().Dispatch(score, rewardList, confG);
                if (!UIManager.Instance.IsBlocking())
                {
                    UIManager.Instance.Block(true);
                }
                if (scorePhase < confD.MilestoneGroup.Count)
                {
                    confG = GetCastleMilestoneGroup(confD.MilestoneGroup[scorePhase]);
                    bonusHandler.Init(confG.Id, confG.OutputsOne, confG.OutputsTwo, confG.OutputsFour, confG.OutputsFixedOne, confG.OutputsFixedTwo, confG.OutputsFixedFour, confG.WithoutputTime, null);
                }
                else
                {
                    Game.Manager.mergeBoardMan.UnregisterGlobalSpawnBonusHandler(bonusHandler);
                }
            }
        }

        public string BoardEntryAsset()
        {
            VisualMain.visual.AssetMap.TryGetValue("boardEntry", out var asset);
            if (string.IsNullOrEmpty(asset))
            {
                return "event_castle_default:UICastleEntry.prefab";
            }
            return asset;
        }

        public bool IsComplete() => scorePhase >= confD.MilestoneGroup.Count;

        public bool BoardEntryVisible => !IsComplete();
        public override bool EntryVisible => BoardEntryVisible;
    }
}