// ===================================================
// Author: mengqc
// Date: 2025/09/04
// ===================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Text;
using DG.Tweening;
using EL;
using fat.conf;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;
using UnityEngine;

namespace FAT
{
    public enum LevelState
    {
        None,
        During,
        Fail,
        Win,
    }

    public class ActivityVineLeap : ActivityLike, IActivityOrderHandler, IBoardEntry, IGuideActEntryMaskShow, IActivityComplete
    {
        #region Theme

        public VisualRes VisualLoading { get; } = new(UIConfig.UIVineLeapLoading);
        public VisualPopup VisualStart { get; } = new(UIConfig.UIVineLeapStart);
        public VisualPopup VisualMain { get; } = new(UIConfig.UIVineLeapMain);
        public VisualRes VisualHelp { get; } = new(UIConfig.UIVineLeapHelp);
        public VisualRes VisualPass { get; } = new(UIConfig.UIVineLeapPass);
        public VisualPopup VisualFailed { get; } = new(UIConfig.UIVineLeapFailed);
        public VisualRes VisualLevelReward { get; } = new(UIConfig.UIVineLeapLevelReward);
        public VisualPopup VisualEnd { get; } = new(UIConfig.UIVineLeapEnd);

        public override ActivityVisual Visual => VisualStart.visual;

        public readonly string themeFontStyleId_Score = "score";

        #endregion

        #region Config

        public EventVineLeap Conf { get; private set; }
        public EventVineLeapDiff DiffConf { get; private set; }
        public int TokenId => Conf.TokenId;
        public EventVineLeapGroup CurGroup;
        public EventVineLeapLevel CurLevelConf;

        #endregion

        #region 存档字段

        // 当前数值模版id
        public int CurTemplateId { get; private set; } = -1;

        // 当前难度id
        public int CurDifficultyId { get; private set; } = -1;

        // 当前关卡索引
        public int CurLevel { get; private set; } = 0;

        public int CurTokenNum { get; private set; }
        public int CurLeftPlayerNum { get; private set; }
        public long LastOnlineUpdate { get; private set; }
        public int PlayerLevel { get; private set; }
        public int ChallengeCount { get; private set; }
        public int TotalTime { get; private set; }

        #endregion

        #region 运行字段

        public bool LevelResult { get; private set; } //比赛结果
        public long NextOnlineTime { get; private set; }
        public float OutCountLeft;
        public bool IsVisitedStart { get; private set; } = true;
        public bool IsVisitedResult { get; private set; } = true;
        public List<RewardCommitData> LevelRewardWaitCommit = new();
        public List<RewardCommitData> FinalRewardWaitCommit = new();
        public readonly ScoreEntity ScoreEntity = new();
        public int FinalRanking { get; private set; }
        public LevelState CurLevelState = LevelState.None;
        public bool IsOpenWithLoading = false;
        private int _flyingCount = 0;

        #endregion

        public ActivityVineLeap(ActivityLite lite_)
        {
            Lite = lite_;
            AddListener();
        }

        public override void SetupFresh()
        {
            InitConf();
            InitTheme();
            PlayerLevel = Game.Manager.mergeLevelMan.level;
            VisualStart.Popup();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(RecordStateHelper.ToRecord(dataIndex++, CurTemplateId));
            any.Add(RecordStateHelper.ToRecord(dataIndex++, CurDifficultyId));
            any.Add(RecordStateHelper.ToRecord(dataIndex++, CurLevel));
            any.Add(RecordStateHelper.ToRecord(dataIndex++, CurTokenNum));
            any.Add(RecordStateHelper.ToRecord(dataIndex++, CurLeftPlayerNum));
            any.Add(RecordStateHelper.ToRecord(dataIndex++, (int)Game.Instance.GetTimestampSeconds()));
            any.Add(RecordStateHelper.ToRecord(dataIndex++, PlayerLevel));
            any.Add(RecordStateHelper.ToRecord(dataIndex++, ChallengeCount));
            any.Add(RecordStateHelper.ToRecord(dataIndex++, TotalTime));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            CurTemplateId = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            CurDifficultyId = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            CurLevel = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            CurTokenNum = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            CurLeftPlayerNum = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            LastOnlineUpdate = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            PlayerLevel = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            ChallengeCount = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            TotalTime = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            InitConf();
            InitTheme();
            if (CurGroup == null) return;
            VisualStart.visual.Theme.AssetInfo.TryGetValue("Score", out var prefab);
            ScoreEntity.Setup(CurTokenNum, this, Conf.TokenId, CurGroup.ExtraScore, ReasonString.vineleap_reward, prefab, Constant.MainBoardId);
        }

        public override void AfterLoad(ActivityInstance data_)
        {
            if (CurLeftPlayerNum > 0)
            {
                if (CurTokenNum >= CurLevelConf.Score)
                {
                    CurLevelState = LevelState.Win;
                    DOVirtual.DelayedCall(1.5f, WinCurStep);
                }
                else
                {
                    var outconfig = EventVineLeapOutVisitor.Get(CurLevelConf.OutId);
                    if (outconfig == null) return;
                    var offlineTime = Game.Instance.GetTimestampSeconds() - LastOnlineUpdate;
                    var interval = UnityEngine.Random.Range(outconfig.Offline[0], outconfig.Offline[1] + 1);
                    var outNum = 0;
                    while (offlineTime >= interval)
                    {
                        offlineTime -= interval;
                        var _out = UnityEngine.Random.Range(outconfig.OfflineOutCount[0], outconfig.OfflineOutCount[1] + 1);
                        outNum += _out;
                        DebugEx.FormatInfo("Vine Leap Offline Add Score: 离线间隔 : {0}, 淘汰人数 : {1}", interval, _out * 0.01f);
                        interval = UnityEngine.Random.Range(outconfig.Offline[0], outconfig.Offline[1] + 1);
                    }
                    outNum = outNum / 100;
                    DebugEx.FormatInfo("Vine Leap Offline Add Score: 离线总时间 : {0}, 淘汰总人数 : {1}", Game.Instance.GetTimestampSeconds() - LastOnlineUpdate, outNum);
                    CurLeftPlayerNum = Math.Max(0, CurLeftPlayerNum - outNum);
                    if (CurLeftPlayerNum == 0)
                    {
                        FailCurStep();
                        return;
                    }
                }
            }
        }

        public override void WhenEnd()
        {
            ScoreEntity.Clear();
            VisualEnd.Popup();
            RemoveListener();
            var levelCount = CurGroup?.LevelId.Count ?? 0;
            var isFinal = CurLevel == levelCount;
            DataTracker.event_vineleap_end.Track(this, CurGroup?.Diff ?? 0, isFinal ? levelCount : CurLevel + 1, levelCount, isFinal,
                DiffConf.SelectDiffId.IndexOf(CurGroup?.Id ?? 0) + 1, CurGroup?.Id ?? 0, ChallengeCount, Countdown > 0 ? 1 : 0);
        }

        public override void WhenReset()
        {
            ScoreEntity.Clear();
            RemoveListener();
        }

        private void InitConf()
        {
            Conf = EventVineLeapVisitor.Get(Lite.Param);
            if (CurTemplateId <= 0)
            {
                CurTemplateId = Game.Manager.userGradeMan.GetTargetConfigDataId(Conf.GradeId);
            }

            DiffConf = EventVineLeapDiffVisitor.Get(CurTemplateId);
            CurGroup = EventVineLeapGroupVisitor.Get(CurDifficultyId);
            if (CurGroup == null)
            {
                return;
            }

            CurLevelConf = EventVineLeapLevelVisitor.Get(CurGroup.LevelId[CurLevel]);
            if (CurLeftPlayerNum > 0)
            {
                CurLevelState = LevelState.During;
            }
        }

        #region Logic

        public bool HasComplete()
        {
            return false;
        }

        public bool IsActive => CurLevelState == LevelState.During;

        public void AddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(SecondUpdate);
            MessageCenter.Get<MSG.SCORE_ENTITY_ADD_COMPLETE>().AddListener(UpdateScore);
            MessageCenter.Get<MSG.FLY_ICON_START>().AddListener(OnFlyTokenStart);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(OnFlyTokenEnd);
        }

        public void RemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(SecondUpdate);
            MessageCenter.Get<MSG.SCORE_ENTITY_ADD_COMPLETE>().RemoveListener(UpdateScore);
            MessageCenter.Get<MSG.FLY_ICON_START>().RemoveListener(OnFlyTokenStart);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(OnFlyTokenEnd);
        }

        private void OnFlyTokenStart(FlyableItemSlice item)
        {
            if (item.FlyType != FlyType.VineLeapToken) return;
            _flyingCount++;
        }

        private void OnFlyTokenEnd(FlyableItemSlice item)
        {
            if (item.FlyType != FlyType.VineLeapToken) return;
            if (item.CurIdx >= item.SplitNum)
            {
                _flyingCount--;
            }
        }

        public void UpdateScore((int pre, int score, int id) data)
        {
            if (CurLevelState != LevelState.During)
            {
                return;
            }

            if (data.id != Conf.TokenId)
            {
                return;
            }

            CurTokenNum += data.score - data.pre;
            if (CurTokenNum >= CurLevelConf.Score)
            {
                CurLevelState = LevelState.Win;
                Game.StartCoroutine(CheckWin());
            }
        }

        public bool IsTokenFull()
        {
            return CurTokenNum >= CurLevelConf.Score;
        }

        private IEnumerator CheckWin()
        {
            yield return new WaitForSeconds(1.5f);
            yield return new WaitUntil(() => _flyingCount == 0);
            yield return new WaitForSeconds(0.6f);

            WinCurStep();
        }

        public void SecondUpdate()
        {
            if (CurLevelState != LevelState.During)
            {
                return;
            }

            var outconfig = EventVineLeapOutVisitor.Get(CurLevelConf.OutId);
            if (NextOnlineTime == 0)
            {
                NextOnlineTime = Game.Instance.GetTimestamp() + UnityEngine.Random.Range(outconfig.Online[0], outconfig.Online[1] + 1) * 10;
                return;
            }

            var before = CurLeftPlayerNum;
            while (NextOnlineTime <= Game.Instance.GetTimestamp())
            {
                var outCount = OutCountLeft + (float)UnityEngine.Random.Range(outconfig.OutCount[0], outconfig.OutCount[1]) / 100;
                var newOutCount = outCount - OutCountLeft;
                OutCountLeft = outCount - (int)outCount;
                CurLeftPlayerNum = Math.Max(0, CurLeftPlayerNum - (int)outCount);
                if (before > CurLeftPlayerNum)
                {
                    Game.Manager.audioMan.TriggerSound("VineLeapCountDecrease");
                }
                var lastTime = NextOnlineTime;
                NextOnlineTime += UnityEngine.Random.Range(outconfig.Online[0], outconfig.Online[1] + 1) * 10;
                DebugEx.FormatInfo("Vine Leap Online: 实际淘汰人数:{0}, 新增淘汰人数:{1}, 取整时舍弃的人数:{2}, 下次更新时间:{3}秒后", (int)outCount, newOutCount, OutCountLeft, (NextOnlineTime - lastTime) / 1000);
                if (CurLeftPlayerNum == 0)
                {
                    FailCurStep();
                    return;
                }
            }
        }

        // 选择难度
        public void SetDifficultyIndex(int index)
        {
            CurDifficultyId = DiffConf.SelectDiffId[index];
            CurGroup = EventVineLeapGroupVisitor.Get(CurDifficultyId);
            VisualStart.visual.Theme.AssetInfo.TryGetValue("Score", out var prefab);
            ScoreEntity.Setup(CurTokenNum, this, Conf.TokenId, CurGroup.ExtraScore, ReasonString.vineleap_reward, prefab, Constant.MainBoardId);
            StartCurStep();
        }

        // 开启当前Step
        public void StartCurStep()
        {
            if (CurLevel >= CurGroup.LevelId.Count)
            {
                return;
            }
            CurLevelState = LevelState.During;
            CurLevelConf = EventVineLeapLevelVisitor.Get(CurGroup.LevelId[CurLevel]);
            CurLeftPlayerNum = CurLevelConf.TotalNum;
            IsVisitedStart = false;
            MessageCenter.Get<MSG.VINELEAP_STEP_START>().Dispatch();
            ChallengeCount++;
            TotalTime = (int)Game.Instance.GetTimestampSeconds();
            DataTracker.event_vineleap_enter.Track(this, CurGroup.Diff, CurLevel + 1, CurGroup.LevelId.Count, CurLevel == CurGroup.LevelId.Count - 1, DiffConf.SelectDiffId.IndexOf(CurGroup.Id) + 1, CurGroup.Id, ChallengeCount);
            MessageCenter.Get<MSG.ACTIVITY_SCORE_ROUND_CHANGE>().Dispatch();
        }

        public void SetVisitedStart()
        {
            IsVisitedStart = true;
        }

        public void SetVisitedResult()
        {
            IsVisitedResult = true;
        }

        public void FailCurStep()
        {
            IsVisitedResult = false;
            LevelResult = false;
            CurLevelState = LevelState.Fail;
            MessageCenter.Get<MSG.VINELEAP_STEP_END>().Dispatch(false);
            MessageCenter.Get<MSG.ACTIVITY_SCORE_ROUND_CHANGE>().Dispatch();
            OpenStepFailed();
            NextOnlineTime = 0;
            OutCountLeft = 0;
            CurTokenNum = 0;
            CurLeftPlayerNum = 0;
            DataTracker.event_vineleap_fail.Track(this, CurGroup.Diff, CurLevel + 1, CurGroup.LevelId.Count, CurLevel == CurGroup.LevelId.Count - 1,
                DiffConf.SelectDiffId.IndexOf(CurGroup.Id) + 1, CurGroup.Id, ChallengeCount, (int)Game.Instance.GetTimestampSeconds() - TotalTime);
            TotalTime = 0;
        }

        public void WinCurStep()
        {
            LevelRewardWaitCommit.Clear();
            IsVisitedResult = false;
            LevelResult = true;
            FinalRanking = CurLevelConf.TotalNum - CurLeftPlayerNum + 1;
            CurLevel++;
            var isFinalWin = false;
            if (CurLevel < CurGroup.LevelId.Count)
            {
                CurLevelConf = EventVineLeapLevelVisitor.Get(CurGroup.LevelId[CurLevel]);
                var rewardconf = EventVineLeapRewardVisitor.Get(CurLevelConf.RewardId);
                if (rewardconf != null)
                {
                    foreach (var str in rewardconf.Reward)
                    {
                        var rwd = str.ConvertToRewardConfig();
                        LevelRewardWaitCommit.Add(Game.Manager.rewardMan.BeginReward(rwd.Id, rwd.Count, ReasonString.vineleap_reward));
                    }
                }
            }
            else
            {
                isFinalWin = true;
                var final = EventVineLeapRewardVisitor.Get(CurGroup.MilestoneReward);
                foreach (var str in final.Reward)
                {
                    var rwd = str.ConvertToRewardConfig();
                    FinalRewardWaitCommit.Add(Game.Manager.rewardMan.BeginReward(rwd.Id, rwd.Count, ReasonString.vineleap_reward));
                }
            }
            if (!isFinalWin)
            {
                DataTracker.event_vineleap_success.Track(this, CurGroup.Diff, CurLevel, CurGroup.LevelId.Count, CurLevel == CurGroup.LevelId.Count,
                    DiffConf.SelectDiffId.IndexOf(CurGroup.Id) + 1, CurGroup.Id, ChallengeCount, FinalRanking, LevelRewardWaitCommit.Count > 0, FinalRewardWaitCommit.Count > 0
                    , CurLevelConf.RewardId, ZString.Join(',', LevelRewardWaitCommit.Select(x => string.Format("{0}:{1}", x.rewardId, x.rewardCount))), (int)Game.Instance.GetTimestampSeconds() - TotalTime);
            }
            else
            {
                DataTracker.event_vineleap_success.Track(this, CurGroup.Diff, CurLevel, CurGroup.LevelId.Count, CurLevel == CurGroup.LevelId.Count,
                    DiffConf.SelectDiffId.IndexOf(CurGroup.Id) + 1, CurGroup.Id, ChallengeCount, FinalRanking, LevelRewardWaitCommit.Count > 0, FinalRewardWaitCommit.Count > 0
                    , CurGroup.MilestoneReward, ZString.Join(',', FinalRewardWaitCommit.Select(x => string.Format("{0}:{1}", x.rewardId, x.rewardCount))), (int)Game.Instance.GetTimestampSeconds() - TotalTime);
            }

            NextOnlineTime = 0;
            OutCountLeft = 0;
            CurTokenNum = 0;
            CurLeftPlayerNum = 0;
            TotalTime = 0;
            ChallengeCount = 0;

            MessageCenter.Get<MSG.VINELEAP_STEP_END>().Dispatch(true);
            MessageCenter.Get<MSG.ACTIVITY_SCORE_ROUND_CHANGE>().Dispatch();
            OpenMain();
            if (isFinalWin)
            {
                Game.Manager.activity.EndImmediate(this, false);
            }
        }

        // 是否正在比赛中
        public bool IsCurStepRunning() => CurLevelState == LevelState.During;

        // 获取比赛剩余席位
        public int GetSeatsLeft()
        {
            return CurLeftPlayerNum;
        }

        public bool IsFinalWin()
        {
            if (CurGroup == null) return false;
            return CurLevel >= CurGroup.LevelId.Count;
        }

        // 获取本关配置
        public EventVineLeapLevel GetCurLevelConf() => CurLevelConf;

        public int GetCurLevelTargetScore()
        {
            return CurLevelConf.Score;
        }

        // 当前关卡获取Token的数量
        public int GetTokenNum() => CurTokenNum;

        // 获取过关名次
        public int GetResultRank() => FinalRanking;

        public EventVineLeapGroup GetCurGroupConf() => CurGroup;

        public EventVineLeapLevel GetLevelConf(int lvIndex) => EventVineLeapLevelVisitor.Get(CurGroup.LevelId[lvIndex]);

        public RewardConfig[] GetMilestoneRewards()
        {
            return GetRewardsById(CurGroup.MilestoneReward);
        }

        public RewardConfig[] GetRewardsById(int id)
        {
            var reward = EventVineLeapRewardVisitor.Get(id);
            if (reward == null) return Array.Empty<RewardConfig>();
            var rewards = new List<RewardConfig>();
            foreach (var rwd in reward.Reward)
            {
                rewards.Add(ConvertRewardString(rwd));
            }

            return rewards.ToArray();
        }

        public RewardConfig ConvertRewardString(string rwd)
        {
            if (PlayerLevel == 0) PlayerLevel = Game.Manager.mergeLevelMan.level;
            var info = rwd.ConvertToInt3();
            var count = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(info.Item2, info.Item3, PlayerLevel);
            return new RewardConfig() { Id = info.Item1, Count = count };
        }

        public bool OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if ((order as IOrderData).IsMagicHour)
                return false;
            if (CurLevelState != LevelState.During)
                return false;
            var changed = false;
            var state = order.GetState((int)OrderParamType.ScoreEventId);
            if (state == null || state.Value != Id)
            {
                // 没有积分 or 不是同一期活动
                changed = true;
                ScoreEntity.CalcOrderScore(order, tracer);
            }

            return changed;
        }

        #endregion

        #region UI

        public override void Open()
        {
            if (CurDifficultyId <= 0)
            {
                // 没有选择难度
                OpenChoice();
            }
            else
            {
                if (CurLevelState == LevelState.Win) { return; }
                IsOpenWithLoading = true;
                ActivityTransit.Enter(this, VisualLoading, VisualMain.res);
            }
        }

        public void OpenMain()
        {
            IsOpenWithLoading = false;
            VisualMain.Popup();
        }

        public void OpenChoice()
        {
            VisualStart.res.ActiveR.Open(this, true);
        }

        public void OpenStepFailed()
        {
            if (!UIManager.Instance.IsShow(UIConfig.UIVineLeapMain))
            {
                VisualFailed.Popup();
            }
        }

        public string BoardEntryAsset()
        {
            VisualStart.visual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        private void InitTheme()
        {
            if (Conf == null) return;
            VisualStart.Setup(Conf.StartTheme, this, active_: false);
            VisualLoading.Setup(Conf.LoadingTheme);
            VisualMain.Setup(Conf.EventTheme, this, active_: false);
            VisualMain.popup.option = new() { ignoreDelay = true };
            VisualEnd.Setup(Conf.EndTheme, this, active_: false);
            VisualFailed.Setup(Conf.FailTheme, this);
        }

        public string MetaEntryAsset()
        {
            VisualStart.visual.Theme.AssetInfo.TryGetValue("metaEntry", out var key);
            return key;
        }

        public string GetChestIcon()
        {
            var index = DiffConf.SelectDiffId.IndexOf(CurDifficultyId);
            return GetChestIconByIndex(index);
        }

        public string GetChestIconByIndex(int index)
        {
            VisualStart.visual.Theme.AssetInfo.TryGetValue($"chest{index + 1}", out var value);
            return value;
        }

        #endregion

        #region Debug

        public static void DebugSetLevel(string lvStr)
        {
            if (!Game.Manager.activity.LookupAny(fat.rawdata.EventType.VineLeap, out var act)) return;
            var activity = act as ActivityVineLeap;
            var lv = int.Parse(lvStr);
            activity!.CurLevel = lv;
            activity.LastOnlineUpdate = 0;
            activity.CurLeftPlayerNum = 0;
            activity.CurLevelState = LevelState.None;
        }

        public static void DebugAddToken(string numStr)
        {
            if (!Game.Manager.activity.LookupAny(fat.rawdata.EventType.VineLeap, out var act)) return;
            var activity = act as ActivityVineLeap;
            var num = int.Parse(numStr);
            activity!.UpdateScore((activity.GetTokenNum(), activity.GetTokenNum() + num, activity.TokenId));
        }

        public static void DebugReduceLeftNum(string numStr)
        {
            if (!Game.Manager.activity.LookupAny(fat.rawdata.EventType.VineLeap, out var act)) return;
            var activity = act as ActivityVineLeap;
            var num = int.Parse(numStr);
            activity!.CurLeftPlayerNum = Mathf.Max(1, activity.CurLeftPlayerNum - num);
            activity.NextOnlineTime = 1;
        }

        #endregion

        public bool IsActEntryMaskCanShow()
        {
            return true;
        }
    }

    public class VineLeapEntry : DynamicEntry<ActivityVineLeap, UIVineLeapEntryMeta>
    {
        public VineLeapEntry(ListActivity.Entry entry, ActivityVineLeap act) : base(entry, act)
        {
        }

        public override string TextCD(long diff)
        {
            _metaEntry?.Refresh();
            return base.TextCD(diff);
        }

        protected override string GetMetaEntryAsset()
        {
            return _activity.MetaEntryAsset();
        }

        protected override void UpdateMetaEntry(UIVineLeapEntryMeta metaEntry)
        {
            _metaEntry.SetData(_entry, _activity);
        }
    }
}
