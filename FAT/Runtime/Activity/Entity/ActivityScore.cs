/**
 * @Author: zhangpengjian
 * @Date: 2024-2-28 15:17:26
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/9/24 18:15:10
 * Description: 积分活动 https://centurygames.yuque.com/ywqzgn/ne0fhm/vw9cnufckkt2wnez#EPlBv 
    https://centurygames.yuque.com/ywqzgn/ne0fhm/ogvldlvkdcg6hbd5
 */

using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;
using System.Collections.Generic;
using Config;
using EL;
using FAT.Merge;
using UnityEngine;

namespace FAT
{
    public class ActivityScore : ActivityLike, IActivityOrderHandler, IBoardEntry, IActivityComplete
    {
        public struct Node
        {
            public RewardConfig reward;
            public int value;
            public int showNum;
            public bool isPrime; //是否阶段性大奖
            public bool isCur;
            public bool isDone;
            public bool isGoal;
            public bool isComplete;
        }

        public EventScore ConfD;
        public EventScoreDetail ConfDetail;
        public override bool Valid => ConfD != null;
        public PopupActivity Popup { get; internal set; }
        public UIResAlt Res { get; } = new(UIConfig.UIScoreHelp);
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureScore);
        public int CurShowScore;
        public int CurMileStoneScore;
        public int PrevFinalMileStoneRewardId;
        public int PrevFinalMileStoneRewardCount;

        #region theme key
        public string themeFontStyleId_Score => "score";
        #endregion

        #region 存储
        public int TotalScore;
        public int RecordFinalMileStoneRewardId;
        public int RecordFinalMileStoneRewardCount;
        private int FinalMileStoneCount;
        private int grpMappingId;
        #endregion

        public RewardConfig NormalMileStoneReward;
        public readonly List<Node> ListM = new();
        private List<RewardConfig> rewardConfigList = new List<RewardConfig>();
        private List<RewardConfig> finalRewardConfigList = new List<RewardConfig>();
        private int curMileStoneIndex;
        private List<RewardCommitData> commitRewardList = new();
        private ScoreEntity scoreEntity = new();
        private IActivityOrderHandler activityOrderHandlerImplementation;
        private int cycleScoreShowCount;

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (HasCycleMilestone())
                popup_.TryQueue(Popup, state_, true);
        }

        public override void Open()
        {
            var mileStone = ConfDetail.MilestoneScore;
            var mileStoneMax = mileStone[mileStone.Count - 1];
            if (!HasCycleMilestone() && TotalScore >= mileStoneMax)
            {
                return;
            }
            UIManager.Instance.OpenWindow(Res.ActiveR, this, false);
        }

        public override void WhenEnd()
        {
            scoreEntity.Clear();
            MessageCenter.Get<MSG.SCORE_ENTITY_ADD_COMPLETE>().RemoveListener(OnUpdateScore);
            UIManager.Instance.CloseWindow(UIConfig.UIScoreProgress);
            if (ConfD != null)
            {
                var c = ActivityExpire.ConvertExpire(ConfD.ExpireItem, Game.Manager.mainMergeMan.world);
                Debug.Log($"{nameof(ActivityScore)} convert {c} expire items");
            }
        }

        public override bool EntryVisible
        {
            get
            {
                return HasCycleMilestone();
            }
        }

        public ActivityScore(ActivityLite lite_)
        {
            Lite = lite_;
            ConfD = Game.Manager.configMan.GetEventScoreConfig(lite_.Param);
            if (ConfD == null)
            {
                DebugEx.FormatError(
                    "[ActivityScore.InitCurScoreActData]: eventScoreConfig = null, activityId = {0} eventParam = {1}",
                    Id, Lite.Param);
                return;
            }
            //初始化弹脸
            if (ConfD != null && Visual.Setup(ConfD.EventThemeId, Res))
            {
                Popup = new(this, Visual, Res, false);
            }
            MessageCenter.Get<MSG.SCORE_ENTITY_ADD_COMPLETE>().AddListener(OnUpdateScore);
        }

        public override void SetupFresh()
        {
            grpMappingId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.GradeId);
            ConfDetail = Game.Manager.configMan.GetEventScoreDetail(grpMappingId);

            foreach (var reward in ConfDetail.MilestoneReward)
            {
                rewardConfigList.Add(reward.ConvertToRewardConfig());
            }
            //初始化积分活动数据
            InitCurScoreActData(TotalScore);
            //填充里程碑配置
            SetupMilestone();

            Visual.Theme.AssetInfo.TryGetValue("bgPrefab", out var prefab);
            scoreEntity.Setup(0, this, ConfD.RequireCoinId, ConfDetail.ExtraScore, ReasonString.score, prefab, ConfD.BoardId);
        }

        public override void SetupClear()
        {
            ConfD = null;
            commitRewardList.Clear();
            rewardConfigList.Clear();
        }

        public override void WhenReset()
        {
            scoreEntity.Clear();
            MessageCenter.Get<MSG.SCORE_ENTITY_ADD_COMPLETE>().RemoveListener(OnUpdateScore);
            UIManager.Instance.CloseWindow(UIConfig.UIScoreProgress);
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, TotalScore));
            any.Add(ToRecord(1, RecordFinalMileStoneRewardId));
            any.Add(ToRecord(2, RecordFinalMileStoneRewardCount));
            any.Add(ToRecord(3, FinalMileStoneCount));
            any.Add(ToRecord(4, grpMappingId));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            TotalScore = ReadInt(0, any);
            RecordFinalMileStoneRewardId = ReadInt(1, any);
            RecordFinalMileStoneRewardCount = ReadInt(2, any);
            FinalMileStoneCount = ReadInt(3, any);
            grpMappingId = ReadInt(4, any);
            if (grpMappingId != 0)
            {
                ConfDetail = Game.Manager.configMan.GetEventScoreDetail(grpMappingId);
            }
            else
            {
                grpMappingId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.GradeId);
                ConfDetail = Game.Manager.configMan.GetEventScoreDetail(grpMappingId);
            }
            cycleScoreShowCount = FinalMileStoneCount;

            foreach (var reward in ConfDetail.MilestoneReward)
            {
                rewardConfigList.Add(reward.ConvertToRewardConfig());
            }

            //填充里程碑配置
            SetupMilestone();

            Visual.Theme.AssetInfo.TryGetValue("bgPrefab", out var prefab);
            if (HasCycleMilestone())
            {
                //初始化积分活动数据
                InitCurScoreActData(TotalScore);
                scoreEntity.Setup(TotalScore, this, ConfD.RequireCoinId, ConfDetail.ExtraScore, ReasonString.score, prefab, ConfD.BoardId);
            }
        }

        private void OnUpdateScore((int prev, int total, int coinId) data)
        {
            if (data.coinId != ConfD.RequireCoinId)
                return;
            if (!HasCycleMilestone())
                return;
            InitCurScoreActData(data.total, false);
            MessageCenter.Get<MSG.SCORE_DATA_UPDATE>().Dispatch(data.prev, data.total);
        }

        /// <summary>
        /// 既做初始化也做checkScore
        /// </summary>
        /// <param name="curScore"></param>
        /// <param name="isInitScore"></param>
        private void InitCurScoreActData(int curScore, bool isInitScore = true)
        {
            TotalScore = curScore;
            //通过总积分 算出显示数据 当前里程碑目标积分 当前展示积分
            var eventScoreConfig = ConfDetail;
            var goalScore = eventScoreConfig.FinalMilestoneScore;
            var mileStone = eventScoreConfig.MilestoneScore;
            var mileStoneMax = mileStone[mileStone.Count - 1];
            //累计积分已经达到普通里程的最大值
            var configList = rewardConfigList;
            var index = curMileStoneIndex;
            if (TotalScore >= mileStoneMax)
            {
                //如果没有循环奖励，发最后一个普通里程碑的奖励
                if (goalScore == 0)
                {
                    var id = configList[configList.Count - 1].Id;
                    var count = configList[configList.Count - 1].Count;
                    var commitData = Game.Manager.rewardMan.BeginReward(id, count, ReasonString.score);
                    DataTracker.event_score_milestone.Track(Id, Param, mileStone.Count, From, mileStone.Count, ConfDetail.Diff, true, false);
                    commitRewardList.Add(commitData);
                    return;
                }
                curMileStoneIndex = mileStone.Count - 1;
                if (TotalScore - mileStoneMax >=
                    (FinalMileStoneCount > 0 ? goalScore * FinalMileStoneCount : goalScore))
                {
                    var count = (TotalScore - mileStoneMax) / goalScore;
                    if (count != FinalMileStoneCount && FinalMileStoneCount < count)
                    {
                        FinalMileStoneCount += 1;
                        //如果不是初始化数据 发放随机奖励
                        if (!isInitScore && (curScore - mileStoneMax) >= goalScore * FinalMileStoneCount)
                        {
                            //如果一次性获得大额积分 从普通里程碑跨越到循环里程碑时 需要随机奖励
                            if (RecordFinalMileStoneRewardId <= 0 || RecordFinalMileStoneRewardCount <= 0)
                                RandomFinalReward();
                            //发奖
                            var commitData = Game.Manager.rewardMan.BeginReward(RecordFinalMileStoneRewardId,
                                RecordFinalMileStoneRewardCount, ReasonString.score);
                            commitRewardList.Add(commitData);
                            //发放完奖励后 重新随机下一循环里程碑奖励
                            RandomFinalReward();
                            DataTracker.event_score_milestone.Track(Id, Param,
                                mileStone.Count + FinalMileStoneCount, From, mileStone.Count, ConfDetail.Diff, false, true);
                        }
                    }

                    //超过普通里程碑最大值且完成了一次循环里程碑
                    CurShowScore = (TotalScore - mileStoneMax) % goalScore;
                }
                else
                {

                    //刚超过里程碑最大值 但还没有达到循环目标分值
                    CurShowScore = TotalScore - mileStoneMax;
                    if (RecordFinalMileStoneRewardId <= 0 || RecordFinalMileStoneRewardCount <= 0)
                    {
                        if (index < configList.Count - 1)
                        {
                            for (var i = 0; i < configList.Count - 1 - index; i++)
                            {
                                Game.Manager.rewardMan.CommitReward(Game.Manager.rewardMan.BeginReward(
                                    configList[index + i].Id,
                                    configList[index + i].Count, ReasonString.score));
                            }
                        }

                        var commitData = Game.Manager.rewardMan.BeginReward(configList[configList.Count - 1].Id,
                            configList[configList.Count - 1].Count, ReasonString.score);
                        DataTracker.event_score_milestone.Track(Id, Param, mileStone.Count, From, mileStone.Count, ConfDetail.Diff, true, false);
                        commitRewardList.Add(commitData);
                        //完成所有里程碑（到达循环）
                        MessageCenter.Get<MSG.ACTIVITY_SUCCESS>().Dispatch(this);
                        if (ConfDetail.FinalMilestoneReward.Count > 0)
                        {
                            RandomFinalReward();
                        }
                    }
                }

                CurMileStoneScore = goalScore;
            }
            else
            {
                //两种边界情况
                //1.当前分数小于第一里程碑要求分数
                if (curScore < mileStone[0])
                {
                    CurShowScore = curScore;
                    CurMileStoneScore = mileStone[0];
                    curMileStoneIndex = 0;
                    NormalMileStoneReward = configList[0];
                }
                else
                {
                    var mileStoneIndex = 0;
                    for (var i = 0; i < mileStone.Count; i++)
                    {
                        if (curScore >= mileStone[i] && curScore < mileStone[i + 1])
                        {
                            CurMileStoneScore = mileStone[i + 1] - mileStone[i];
                            CurShowScore = curScore - mileStone[i];
                            mileStoneIndex = i + 1;
                            break;
                        }
                    }

                    if (!isInitScore && mileStoneIndex != index)
                    {
                        //如果一次性获得大额积分 需要发n次奖
                        for (var i = 0; i < mileStoneIndex - index; i++)
                        {
                            var commitData = Game.Manager.rewardMan.BeginReward(configList[index + i].Id,
                                configList[index + i].Count, ReasonString.score);
                            commitRewardList.Add(commitData);
                            MessageCenter.Get<MSG.BOARD_ORDER_SCROLL_RESET>().Dispatch();
                            DataTracker.event_score_milestone.Track(Id, Param, index + i + 1, From, mileStone.Count, ConfDetail.Diff, false, false);
                        }
                    }

                    curMileStoneIndex = mileStoneIndex;
                    NormalMileStoneReward = configList[mileStoneIndex];
                }
            }
        }

        public void DebugAddScore(ScoreEntity.ScoreType t, int score)
        {
            scoreEntity.AddScore(t, score);
            MessageCenter.Get<MSG.SCORE_ADD_DEBUG>().Dispatch();
        }

        private void RandomFinalReward()
        {
            //随机一个循环里程碑奖励 做显示
            if (finalRewardConfigList.Count <= 0)
            {
                foreach (var reward in ConfDetail.FinalMilestoneReward)
                {
                    finalRewardConfigList.Add(reward.ConvertToRewardConfig());
                }
            }

            var finalReward = finalRewardConfigList.RandomChooseByWeight();
            PrevFinalMileStoneRewardId = RecordFinalMileStoneRewardId;
            PrevFinalMileStoneRewardCount = RecordFinalMileStoneRewardCount;
            RecordFinalMileStoneRewardId = finalReward.Id;
            RecordFinalMileStoneRewardCount = finalReward.Count;
        }

        public void SetupMilestone()
        {
            var confR = ConfDetail.MilestoneReward;
            var confS = ConfDetail.MilestoneScore;
            ListM.Clear();
            for (var n = 0; n < confR.Count; ++n)
            {
                ListM.Add(new()
                {
                    reward = confR[n].ConvertToRewardConfig(),
                    value = confS[n],
                    showNum = n + 1,
                });
            }
        }

        public int MilestoneNext(int v_)
        {
            var mileStone = ConfDetail.MilestoneScore;
            var mileStoneMax = mileStone[mileStone.Count - 1];
            if (v_ >= mileStoneMax)
            {
                return -1;
            }

            var ret = ListM.Count;
            for (var n = 0; n < ListM.Count; ++n)
            {
                var node = ListM[n];
                var ready = v_ >= node.value;
                if (!ready)
                {
                    ret = n;
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// 获取当前里程碑奖励
        /// </summary>
        /// <returns></returns>
        public RewardConfig GetCurMileStoneReward()
        {
            var resultRewardId = RecordFinalMileStoneRewardId > 0
                ? RecordFinalMileStoneRewardId
                : NormalMileStoneReward.Id;
            var resultRewardCount = RecordFinalMileStoneRewardCount > 0
                ? RecordFinalMileStoneRewardCount
                : NormalMileStoneReward.Count;
            var r = new RewardConfig
            {
                Id = resultRewardId,
                Count = resultRewardCount
            };
            return r;
        }

        public int GetMilestoneIndex()
        {
            return curMileStoneIndex;
        }

        public RewardCommitData TryGetCommitReward(RewardConfig reward)
        {
            RewardCommitData rewardCommitData = null;
            foreach (var commitData in commitRewardList)
            {
                if (commitData.rewardId == reward.Id && commitData.rewardCount == reward.Count)
                {
                    rewardCommitData = commitData;
                    break;
                }
            }

            return rewardCommitData;
        }

        public void TryCommitReward()
        {
            if (commitRewardList.Count > 0)
            {
                foreach (var commitData in commitRewardList)
                {
                    Game.Manager.rewardMan.CommitReward(commitData);
                }
                commitRewardList.Clear();
            }
        }

        #region order

        public bool IsValidForBoard(int boardId)
        {
            return ConfD.BoardId == boardId;
        }

        public bool OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if ((order as IOrderData).IsMagicHour)
                return false;
            if (!HasCycleMilestone())
                return false;
            var changed = false;
            var state = order.GetState((int)OrderParamType.ScoreEventId);
            if (state == null || state.Value != Id)
            {
                // 没有积分 or 不是同一期活动
                changed = true;
                scoreEntity.CalcOrderScore(order, tracer);
            }

            return changed;
        }

        #endregion
        public void SetCycleScoreCount()
        {
            cycleScoreShowCount += 1;
        }

        public int GetScoreCycleCount()
        {
            return cycleScoreShowCount;
        }

        public int GetCyclePrevScore(float currentValue)
        {
            var s = currentValue;
            var mileStone = ConfDetail.MilestoneScore;
            var mileStoneMax = mileStone[mileStone.Count - 1];
            var finalScore = ConfDetail.FinalMilestoneScore;
            var t = finalScore * FinalMileStoneCount + mileStoneMax;
            if (t < s)
            {
                return t;
            }
            else
            {
                t = finalScore * (FinalMileStoneCount - 1) + mileStoneMax;
                return t;
            }
        }

        public bool HasCycleMilestone()
        {
            if (ConfDetail != null)
            {
                var mileStone = ConfDetail.MilestoneScore;
                var mileStoneMax = mileStone[mileStone.Count - 1];
                return !(TotalScore >= mileStoneMax && ConfDetail.FinalMilestoneReward.Count <= 0);
            }
            return true;
        }

        public void FillMilestoneData()
        {
            for (int i = 0; i < ListM.Count; i++)
            {
                var m = ListM[i];
                var idx = m.showNum - 1;
                m.isCur = curMileStoneIndex == idx;
                m.isGoal = idx > curMileStoneIndex;
                m.isDone = idx < curMileStoneIndex;
                m.isPrime = IsPrimeReward(idx);
                m.isComplete = m.showNum > ListM.Count - 5;
                ListM[i] = m;
            }
        }

        private bool IsPrimeReward(int idx)
        {
            if (ConfD == null)
                return false;
            foreach (var item in ConfDetail.RewardStepNum)
            {
                if (idx + 1 == item)
                    return true;
            }
            return false;
        }

        public string BoardEntryAsset()
        {
            Visual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            if (string.IsNullOrEmpty(key))
            {
                return "event_score_cook#UIScoreEntry.prefab";
            }
            return key;
        }

        public bool HasComplete()
        {
            return !HasCycleMilestone();
        }

        public bool BoardEntryVisible => HasCycleMilestone();
    }
}