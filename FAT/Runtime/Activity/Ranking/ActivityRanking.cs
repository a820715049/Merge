using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using Config;
using FAT.MSG;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading.Tasks;
using EL.Resource;
using System.Linq;
using UnityEngine;

namespace FAT
{
    using static MessageCenter;
    using static PoolMapping;
    using static RecordStateHelper;

    public interface IRankingSource
    {
        (int, int, int) Info3 { get; }
        int RankingScore { get; }
        Action<int> RankingScoreChange { set; }
        bool RankingValid(RankingType type_);
        void ResetRankingScore();
    }

    public sealed class RankingEmpty : IRankingSource
    {
        public static RankingEmpty I => _i ??= new RankingEmpty();
        private static RankingEmpty _i;
        public (int, int, int) Info3 => default;
        public int RankingScore { get; set; }
        public Action<int> RankingScoreChange { get; set; }

        public bool RankingValid(RankingType type_) => false;
        public void ResetRankingScore() => RankingScore = 0;
    }

    public sealed class RankingLimbo : IRankingSource
    {
        public static RankingLimbo I => _i ??= new RankingLimbo();
        private static RankingLimbo _i;
        public (int, int, int) Info3 => default;
        public int RankingScore { get; set; }
        public Action<int> RankingScoreChange { get; set; }

        public bool RankingValid(RankingType type_) => true;
        public void ResetRankingScore() => RankingScore = 0;
    }

    public partial class ActivityRanking : ActivityLike, IRankingSource, IBoardEntry
    {
        public string IdR { get; private set; }
        public virtual int SubId => 0;
        public readonly RankingTypeRecord record = new();
        public EventRank confD;
        public override bool Valid => confD != null;
        public override ActivityVisual Visual => VisualStart.visual;
        public VisualPopup VisualRanking { get; } = new(UIConfig.UIActivityRanking);
        public VisualPopup VisualStart { get; } = new(UIConfig.UIActivityRankingStart);
        public VisualPopup VisualEnd { get; } = new(UIConfig.UIActivityRankingEnd);
        public VisualRes VisualHelp { get; } = new(UIConfig.UIActivityRankingHelp);
        public VisualPopup VisualMilestonReward { get; } = new(UIConfig.UIActivityRankMilestoneReward);
        public VisualPopup VisualMilestonHelp { get; } = new(UIConfig.UIActivityRankMilestoneHelp);
        public PopupRankMelistonOrderLike RankMelistonPopup { get; internal set; }
        public int LastScore { get; set; }
        public int RankingScore { get; set; }
        public Action<int> RankingScoreChange { get; set; }
        public List<List<RewardConfig>> reward = new();
        public RankingCache Cache { get; } = new(RankingType.RankingGroup);
        public int Rank => (int)(Cache.Data?.Me?.RankingOrder ?? 0);
        public bool TargetValid => Target is not RankingEmpty;
        public IRankingSource Target { get; private set; } = RankingEmpty.I;
        public long TSData;
        private int lastRank;
        private bool popupNew;
        private bool claimed;
        private static ActivityLike activeLimbo;

        public void Test(int index)
        {
            async void R()
            {
                await UniTask.Delay(1000);
                Cache.Test(index, 25);
            }

            UniTask.RunOnThreadPool(R);
        }

        public UIResAlt MainRes = new(UIConfig.UIActivityRankMilestoneReward);
        public ActivityRanking(ActivityLite lite_)
        {
            Lite = lite_;
            confD = GetEventRank(lite_.Param);
            if (confD == null) return;
            IdR = $"{Id}_{From}_{(int)Type}";
            VisualRanking.Setup(confD.RankTheme, this, active_:false);
            VisualStart.Setup(confD.EventTheme, this);
            VisualEnd.Setup(confD.EndTheme, this, active_:false);
            VisualHelp.Setup(confD.HelpTheme);
            //VisualMilestonReward.Setup(confD.MilestoneTheme, this, active_:false);
            VisualMilestonHelp.Setup(confD.HelpTheme, this, active_:false);
            RankMelistonPopup = new PopupRankMelistonOrderLike(this, VisualMilestonReward.visual, VisualMilestonReward.res);
            
            SetupTheme();
            Cache.WhenRefresh = Refresh;
            Cache.RefreshInterval = confD.RefreshTime;
            var rewardE = confD.IncludeId.Select(n => GetEventRankReward(n)).Where(c => c != null)
                .Select(c => Enumerable.ToList(c.RankGetReward.Select(r => r.ConvertToRewardConfig())));
            reward = Enumerable.ToList(rewardE);
        }

        public void SetupTheme()
        {
            var map = VisualRanking.visual.AssetMap;
            map.TryReplace("rank1", "event_ranking_default:i_s_dailytheme1_ranking.png");
            map.TryReplace("rank2", "event_ranking_default:i_s_dailytheme2_ranking.png");
            map.TryReplace("rank3", "event_ranking_default:i_s_dailytheme3_ranking.png");
            map.TryReplace("rank4", "event_ranking_default:i_s_dailytheme4_ranking.png");
            map = VisualRanking.visual.StyleMap;
            map.TryReplace("rank1", "18");
            map.TryReplace("rank2", "52");
            map.TryReplace("rank3", "18");
            map.TryReplace("rank4", "22");
        }

        public void Fill(RankingCfg v_)
        {
            v_.EventID = Id;
            v_.EventFrom = From;
            v_.ParamID = Param;
            v_.ActivityExpiredTs = endTS + confD.DeleteTime;
            v_.ActivityEndTs = endTS;
        }

        public void Fill(IDictionary<int, RankingTypeRecord> map_)
        {
            var v = RankingScore;
            if (v != LastScore)
            {
                DebugEx.Info($"ranking data: {IdR} {Cache.Type}:{v}");
                LastScore = v;
            }
            record.Value = v;
            record.Id = SubId;
            map_[(int)Cache.Type] = record;
            Cache.CheckRefresh(this, 300);
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, lastRank));
            any.Add(ToRecord(1, RankingScore));
            any.Add(ToRecord(10, claimed));
            any.Add(ToRecord(11, RecordCurMilesScore));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            lastRank = ReadInt(0, any);
            RankingScore = ReadInt(1, any);
            claimed = ReadBool(10, any);
            RecordCurMilesScore = ReadInt(11, any);
            DebugEx.Info("RankActivityLoadSetup: Claimed=" + claimed + "  RecordCurMilesScore" + RecordCurMilesScore);

            if (IsValidRankMileStoneActivity())
            {
                //设置里程碑数据
                SetRankingMilestoneData();
                CheckRefreshMilestoneData(RecordCurMilesScore);
                FillMilestoneData();
            }
        }

        public override void SetupFresh()
        {
            DataTracker.RankingStart(this);
        }

        public override void WhenActive(bool new_)
        {
            TryMatchTarget();
            if (!TargetValid) {
                Game.Manager.activity.Observe(this);
            }
            popupNew = new_;
            var popup = Game.Manager.screenPopup;
            if (new_ && popup.queryTS.ContainsKey(PopupType.Login)) {
                _ = PopupActive(popup.PopupNone);
            }
        }

        public override bool WhenObserve(string e_)
        {
            if (e_ is not nameof(ACTIVITY_ACTIVE)) return true;
            TryMatchTarget();
            return !TargetValid;
        }

        public override void WhenEnd()
        {
            Game.Manager.activity.AddLimbo(this);
            EndPopup();
        }

        public override void WakeLimbo()
        {
            DebugEx.Info($"ranking active limbo {Info3}");
            activeLimbo = this;
            Target = RankingLimbo.I;
            EndPopup();
        }

        public void EndPopup()
        {
            if (claimed) return;  //领取状态检查

            void R(RankingCache c_)
            {
                Cache.WhenRefresh = null;
                _ = R1(c_);
            }

            async UniTask R1(RankingCache c_)
            {
                if(claimed) return;
                var token = PoolMappingAccess.Take(out List<RewardCommitData> list);
                var r = (int)(Cache.Data?.Me?.RankingOrder ?? 0) - 1;
                if (r < 0) {
                    DebugEx.Info($"ranking {Info3} ended with invalid rank {r}");
                    goto end;
                }
                if (claimed) {
                    DebugEx.Warning($"ranking {Info3} read already claimed");
                    goto end;
                }
                var popup = Game.Manager.screenPopup;
                //await popup.Wait(500);
                var valid = r >= 0 && r < reward.Count;
                if (valid) {
                    DebugEx.Info($"ranking {Info3} claim reward rank:{r}");
                    var rewardMan = Game.Manager.rewardMan;
                    var rList = reward[r];
                    foreach (var c in rList)
                    {
                        var d = rewardMan.BeginReward(c.Id, c.Count, ReasonString.ranking);
                        list.Add(d);
                    }
                    claimed = true;
                    DebugEx.Info("ranking: Popup ListAdd");
                    VisualRanking.Popup(popup);
                    VisualEnd.Popup(popup, custom_: token);
                }
                else {
                    DebugEx.Info($"ranking {Info3} ended with no reward rank:{r}");
                    VisualRanking.Popup(popup);
                }
            end:
                DataTracker.RankingEnd(this, list.Count > 0);
                DebugEx.Info("ranking: DataTracker.RankingEnd");
                if (list.Count == 0) token.Free();
                if (activeLimbo == this) {
                    activeLimbo = null;
                    DebugEx.Info($"ranking wait active limbo {Info3} end");
                }
                Game.Manager.activity.RemoveLimbo(this);
            }
            var wait = Sync(interval_:1);
            DebugEx.Info($"ranking {Info3} end popup wait:{wait}");
            if (!wait) R(Cache);
            else Cache.WhenRefresh = R;
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate() {
            if (!Valid) yield break;
            foreach(var v in Visual.ResEnumerate()) yield return v;
            foreach(var v in VisualRanking.ResEnumerate()) yield return v;
            foreach(var v in VisualEnd.ResEnumerate()) yield return v;
            foreach(var v in VisualHelp.ResEnumerate()) yield return v;
        }

        private async UniTask PopupActive(PopupType state_) {
            var popup = Game.Manager.screenPopup;
            await Task.Delay(200);
            if (activeLimbo != null) {
                DebugEx.Info($"ranking wait active limbo {activeLimbo.Info3}");
            }
            while (activeLimbo != null) await Task.Delay(1000);
            await popup.Wait(200);
            if (popupNew) VisualStart.Popup(popup, state_);
            else VisualRanking.Popup(popup, state_);
            popupNew = false;
            Game.Manager.screenPopup.changed = false;//suppress sorting
        }

        public override void Open()
        {
            Open(VisualRanking);
            RefreshEntry();
        }

        public void OpenHelp()
        {
            if (IsValidRankMileStoneActivity())
            {
                VisualMilestonHelp.Open(this);
            }
            else
            {
                VisualHelp.Open(this);
            }
        }

        public bool RankingValid()
        {
            return RankingValid(Cache.Type);
        }

        public bool RankingValid(RankingType type_)
        {
            if (!Target.RankingValid(type_)) return false;
            return RankingScore >= confD.ScoreNum;
        }

        public void ResetRankingScore() => RankingScore = 0;

        public void TryMatchTarget(ActivityLike e_) {
            if (!Valid || TargetValid || e_ is not IRankingSource r || e_.Type != confD.EventType) return;
            TargetMatch(e_, r);
        }

        public IRankingSource TryMatchTarget()
        {
            if (!Valid) return RankingEmpty.I;
            var e = Game.Manager.activity.LookupAny(confD.EventType);
            if (e == null)
            {
                DebugEx.Warning($"ranking activity target type not found. type:{confD.EventType} activity:{Info3}");
                return RankingEmpty.I;
            }

            if (e is not IRankingSource r)
            {
                DebugEx.Warning(
                    $"ranking activity target is not IRankingSource. type:{confD.EventType} target:{e.Info3} activity:{Info3}");
                return RankingEmpty.I;
            }

            TargetMatch(e, r);
            return r;
        }

        private void TargetMatch(ActivityLike e_, IRankingSource r_) {
            Target = r_;
            r_.RankingScoreChange = SyncChange;
            DebugEx.Info($"ranking activity match. target:{e_.Info3} activity:{Info3}");
        }

        public string BoardEntryAsset()
        {
            return VisualRanking.visual.Theme.EntranceImage;
        }

        public bool Sync(int interval_ = -1)
        {
            return Cache.SyncRanking(this, interval_:interval_);
        }

        public void SyncChange(int v_)
        {
            Cache.IsScoreChange = false;
            Game.StartCoroutine(SyncScoreChange(v_));
        }

        private IEnumerator SyncScoreChange(int v_)
        {
            IEnumerator R()
            {
                var m = Game.Manager.archiveMan;
                m.SendImmediately(true);
                while (!m.uploadCompleted) yield return null;
                DebugEx.Info($"ranking sync change");
                Sync(interval_: 1);
            }
            RankingScore += v_;
            RankingScoreChange?.Invoke(v_);
            Game.StartCoroutine(R());
            if (IsValidRankMileStoneActivity() && Active)
            {
                yield return new WaitUntil(() => Cache.IsScoreChange);
                if (MilestoneNodeList.Count <= 0)
                {
                    SetRankingMilestoneData();
                }
                CheckRefreshMilestoneData(RankingScore);
                DebugEx.Info("ranking: scoreChange" + RankingScore + " ranking: CurMilestoneIndex" + CurMilestoneIndex + "rankNum: " + Rank);
                RankMilestoneRewarPopup(RankingScore);
            }
        }
        public void Refresh(RankingCache c_)
        {
            var rank = (int)(c_.Data?.Me?.RankingOrder ?? 0);
            if (lastRank <= 0 && rank > 0) {
                async UniTask P() {
                    var popup = Game.Manager.screenPopup;
                    await popup.Wait(200);
                    VisualRanking.Popup(popup);
                }
                _ = P();
            }
            lastRank = rank;
            entry.Refresh(null, this, c_);
            DataTracker.RankingRecord(this);
            Get<ACTIVITY_RANKING_DATA>().Dispatch(this, c_.Type);
        }

        public bool CheckRankUp()
        {
            return Cache.RankUpAfter(ref TSData);
        }

        #region 排行榜里程碑

        //里程碑节点数据
        public struct NodeItem
        {
            public RewardConfig Reward; //奖励配置
            public int Scorevalue; //对应的积分值
            public int showNum; //显示的里程碑Num
            public bool IsCurPro; //是否当前里程碑
            public bool IsDonePro; //是否已完成
            public bool IsGoalPro; //是否目标进度
        }

        #region 存储
        public int RecordCurMilesScore; //记录当前里程碑积分值

        #endregion
        public int CurShowScore;  //当前显示的积分值
        public int CurMilestoneIndex = -1; //当前里程碑Index
        public List<NodeItem> MilestoneNodeList = new();    //里程碑节点List
        public List<RewardCommitData> rewardListFinale = new();
        private List<RewardConfig> rewardConfigList = new List<RewardConfig>(); //奖励列表List
        private List<NodeItem> curShowRewardNodeList = new List<NodeItem>(); //奖励列表List
        List<RankMelistoneTrackData> trackRankMelestoneDataList = new List<RankMelistoneTrackData>(); //当前里程碑打点数据list


        /// <summary>
        /// 设置里程碑奖励数据
        /// </summary>
        private void SetRankingMilestoneData()
        {
            var milestoneScoreConf = confD.RequireScoreNum; //积分值
            var milestoneRewarConf = confD.Reward; //里程碑奖励
            if (milestoneScoreConf == null || milestoneRewarConf ==null) return;
            MilestoneNodeList.Clear();
            for (int i = 0; i < milestoneRewarConf.Count; i++)
            {
                MilestoneNodeList.Add(new NodeItem()
                {
                    Reward = milestoneRewarConf[i].ConvertToRewardConfig(),
                    Scorevalue = milestoneScoreConf[i],
                    showNum = i+1,
                });
            }
            MilestoneNodeList.Sort((a, b) => a.showNum.CompareTo(b.showNum));
        }



        /// <summary>
        /// 检查里程碑数据
        /// </summary>
        /// <param name="score"></param>
        /// <param name="rewardId"></param>
        /// <param name="rewardCount"></param>
        private void CheckRefreshMilestoneData(int curScore)
        {
            RecordCurMilesScore = curScore;
            var milestoneScoreConf = confD.RequireScoreNum; //积分值
            var mileStoneScoreMax = milestoneScoreConf[milestoneScoreConf.Count - 1];

            if (curScore >= mileStoneScoreMax)
            {
                CurMilestoneIndex = milestoneScoreConf.Count + 1;
            }
            else
            {
                //边界情况判断
                if (curScore < milestoneScoreConf[0])
                {
                    CurMilestoneIndex = 0;
                    CurShowScore = milestoneScoreConf[0];
                }
                else
                {
                    // 根据当前的积分值计算出里程碑索引
                    for (int i = 0; i < milestoneScoreConf.Count; i++)
                    {
                        if (curScore >= milestoneScoreConf[i] && curScore < milestoneScoreConf[i + 1])
                        {
                            CurMilestoneIndex = i + 1;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 填充里程碑节点剩余数据
        /// </summary>
        public void FillMilestoneData()
        {
            for (int i = 0; i < MilestoneNodeList.Count; i++)
            {
                var nodeData = MilestoneNodeList[i];
                var idx = nodeData.showNum - 1;
                nodeData.IsCurPro = CurMilestoneIndex == idx;
                nodeData.IsGoalPro = idx > CurMilestoneIndex;
                nodeData.IsDonePro = idx < CurMilestoneIndex;
                MilestoneNodeList[i] = nodeData;
            }
        }

        /// <summary>
        /// 获取当前里程碑奖励
        /// </summary>
        /// <returns></returns>
        public RewardConfig GetCurMileStoneReward()
        {
            var milestoneScoreConf = confD.RequireScoreNum; //积分值
            if (milestoneScoreConf.Count > 0 && MilestoneNodeList.Count > 0)
            {
                if (!IsCompleteMaxMilestoneScore(RecordCurMilesScore))
                {
                    if (CurMilestoneIndex == 0)
                    {
                        return MilestoneNodeList[0].Reward;
                    }
                    else
                    {
                        return MilestoneNodeList[CurMilestoneIndex].Reward;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取当前里程碑积分值
        /// </summary>
        /// <returns></returns>
        public int GetCurMilestonScore()
        {
            if (MilestoneNodeList.Count <= 0) return -1;
            if (!IsCompleteMaxMilestoneScore(RecordCurMilesScore))
            {
                return MilestoneNodeList[CurMilestoneIndex].Scorevalue;
            }
            else
            {
                //返回最大奖励
                return MilestoneNodeList[MilestoneNodeList.Count - 1].Scorevalue;
            }
        }

        /// <summary>
        /// 是否达到最大里程碑
        /// </summary>
        /// <param name="score_"></param>
        /// <returns></returns>
        public bool IsCompleteMaxMilestoneScore(int score)
        {
            var milestoneScoreConf = confD.RequireScoreNum; //积分值
            var mileStoneScoreMax = milestoneScoreConf[milestoneScoreConf.Count - 1];
            return score >= mileStoneScoreMax;
        }

        /// <summary>
        /// 里程碑奖励弹窗
        /// </summary>
        /// <param name="score"></param>
        private void RankMilestoneRewarPopup(int score)
        {
            if (IsNeedPopUpMilestoneReward(score))
            {
                var rewardMan = Game.Manager.rewardMan;
                var rewardDataList = GetCurShowRewardListConfigByScore(RankingScore);
                if (rewardDataList.Count <= 0)
                {
                    DebugEx.Info("ranking: milestone reward Count is 0");
                    return;
                }
                rewardListFinale.Clear();
                foreach (var rewardItem in rewardDataList)
                {
                    var d = rewardMan.BeginReward(rewardItem.Id, rewardItem.Count, ReasonString.ranking);
                    rewardListFinale.Add(d);
                }

                RankMelistonPopup.Custom = rewardListFinale;
                RankMelistonPopup.OpenPopup();
                RankingMelistonTrackData();
                FillMilestoneData();
            }
            else
            {
                DebugEx.Info("ranking: milestone reward popup not needed");
            }
        }


        /// <summary>
        /// 是否需要弹出里程碑奖励
        /// </summary>
        /// <param name="curScore"></param>
        /// <returns></returns>
        private bool IsNeedPopUpMilestoneReward(int curScore)
        {
            var milestoneScoreConf = confD.RequireScoreNum; //积分值
            var mileStoneScoreMax = milestoneScoreConf[milestoneScoreConf.Count - 1];
            if (curScore >= mileStoneScoreMax && !IsFinishMaxMilestone())
            {
                return true;
            }
            else
            {
                for (int i = 0; i < milestoneScoreConf.Count; i++)
                {
                    if (i + 1 < milestoneScoreConf.Count)
                    {
                        if (curScore >= milestoneScoreConf[i] && curScore < milestoneScoreConf[i + 1])
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 根据状态判断最大里程碑是否已经完成
        /// </summary>
        /// <returns></returns>
        private bool IsFinishMaxMilestone()
        {
            var mileStoneScoreMax = MilestoneNodeList[MilestoneNodeList.Count - 1];
            if (mileStoneScoreMax.IsDonePro)
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// 根据当前的积分值获取当前需要展示的里程碑奖励
        /// </summary>
        /// <returns></returns>
        public List<RewardConfig> GetCurShowRewardListConfigByScore(int score)
        {
            rewardConfigList.Clear();
            foreach (var item in MilestoneNodeList)
            {
                if (item.IsDonePro && !item.IsGoalPro)
                {
                    continue;
                }
                if (item.Scorevalue <= score)
                {
                    rewardConfigList.Add(item.Reward);
                }
            }
            return rewardConfigList;
        }


        /// <summary>
        /// 排行榜里程碑活动是否有效
        /// </summary>
        /// <returns></returns>
        public bool IsValidRankMileStoneActivity()
        {
            return confD.RequireScoreNum != null && confD.RequireScoreNum.Count > 0;
        }


        /// <summary>
        /// 里程碑获得奖励打点
        /// </summary>
        private void RankingMelistonTrackData()
        {
            var curMilestonNum = GetCurMilestonNum();
            trackRankMelestoneDataList.Clear();
            var showRewardNodeItemList = GetaAllShowRewardNodeListByScore(RecordCurMilesScore);
            foreach (var item in showRewardNodeItemList)
            {
                trackRankMelestoneDataList.Add(new RankMelistoneTrackData(item.showNum, MilestoneNodeList.Count, Rank, RecordCurMilesScore));
            }

            foreach (var item in trackRankMelestoneDataList)
            {
                //打点
                DataTracker.RankingGetMelistoneReward(this, item);
            }
        }


        /// <summary>
        /// 当前里程碑Num
        /// </summary>
        /// <returns></returns>
        public int GetCurMilestonNum()
        {
            if (MilestoneNodeList.Count <= 0) return -1;
            if (!IsCompleteMaxMilestoneScore(RecordCurMilesScore))
            {
                return MilestoneNodeList[CurMilestoneIndex].showNum;
            }
            else
            {
                //返回最大奖励
                return MilestoneNodeList[MilestoneNodeList.Count - 1].showNum;
            }
        }
        
        /// <summary>
        /// 里程碑打点数据类
        /// </summary>
        public class RankMelistoneTrackData
        {
            public int MelistoneNum;
            public int MelistoneAllCount;
            public int RankOrderNum;
            public int CollectScoreNum;

            public RankMelistoneTrackData(int melistoneNum, int melistoneAllCount, int rankOrderNum, int collectScoreNum)
            {
                MelistoneNum = melistoneNum;
                MelistoneAllCount = melistoneAllCount;
                RankOrderNum = rankOrderNum;
                CollectScoreNum = collectScoreNum;
            }
        }


        /// <summary>
        /// 根据当前的积分值获取当前需要展示的所有里程碑奖励
        /// </summary>
        /// <returns></returns>
        public List<NodeItem> GetaAllShowRewardNodeListByScore(int score)
        {
            curShowRewardNodeList.Clear();
            foreach (var item in MilestoneNodeList)
            {
                if (item.IsDonePro && !item.IsGoalPro)
                {
                    continue;
                }
                if (item.Scorevalue <= score)
                {
                    curShowRewardNodeList.Add(item);
                }
            }
            return curShowRewardNodeList;
        }

        #endregion

    }

    public partial class ActivityRanking
    {
        public class Entry : ListActivity.IEntrySetup
        {
            public long TSData;
            public ListActivity.Entry e;

            public void Setup(ListActivity.Entry e_)
            {
                var v = e_.visual;
                v.Clear();
                v.Add(e_.tokenState, "rank1", 0);
                v.Add(e_.tokenState, "rank2", 1);
                v.Add(e_.tokenState, "rank3", 2);
                v.Add(e_.tokenState, "rank4", 3);
                v.Add(e_.frame, "rank1", 0);
                v.Add(e_.frame, "rank2", 1);
                v.Add(e_.frame, "rank3", 2);
                v.Add(e_.frame, "rank4", 3);
            }

            public void Refresh(ListActivity.Entry e_, ActivityRanking a_, RankingCache c_)
            {
                e = e_ ?? e;
                if (e == null || !e.obj.activeInHierarchy) return;
                var me = c_.Data?.Me;
                var valid = me != null && a_.RankingValid(c_.Type);
                var rUp = CheckRankUp(c_);
                e.token.gameObject.SetActive(valid);
                e.frame.gameObject.SetActive(valid);
                e.up.gameObject.SetActive(rUp);
                if (valid)
                {
                    var rank = (int)me.RankingOrder;
                    e.token.text = $"{rank}";
                    e.iconState.SelectNear(rank - 1);
                }
                if (rUp) {
                    Game.Manager.audioMan.TriggerSound("HotAirGetPointUp");
                }
            }

            public override void Clear(ListActivity.Entry _)
            {
                e?.visual.Clear();
                e = null;
            }

            public override string TextCD(long diff_)
            {
                return UIUtility.CountDownFormat(diff_);
            }

            public bool CheckRankUp(RankingCache c_)
            {
                return c_.RankUpAfter(ref TSData);
            }
        }

        public readonly Entry entry = new();

        public Entry SetupEntry(ListActivity.Entry e_)
        {
            Sync();
            entry.Setup(e_);
            entry.Refresh(e_, this, Cache);
            VisualRanking.Refresh(e_.visual);
            return entry;
        }

        public void RefreshEntry()
        {
            entry.Refresh(null, this, Cache);
        }
    }
}