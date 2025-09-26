/**
 * @Author: zhangpengjian
 * @Date: 2024-2-28 15:17:26
 * @LastEditors: shentuange
 * @LastEditTime: 2025/06/27 16:37:11
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

        #region 需要向 领奖时主动弹窗 的UI传递的 纯表现用参数
        public int TotalShowScore_UI;//UI动画结束时，总分该显示多少
        public int LastShowScore_UI;//UI弹出时，总分该显示多少
        public PopupScoreReward PopupReward;//领奖弹窗,轨道活动中和Popup是同一个UI,原因是Popup会受到弹窗次数限制
        private bool m_hasRegisteredIdleAction;
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
            UIManager.Instance.OpenWindow(UIConfig.UIOrderDiffchoice, this, false);
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
                //如果后续需要和Popup不同的领奖弹窗,可以在这里改
                PopupReward = new(this, Visual, Res);
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
            //初始化的时候，这两个值是一样的
            TotalShowScore_UI = TotalScore;
            LastShowScore_UI = TotalScore;
        }

        private void OnUpdateScore((int prev, int total, int coinId) data)
        {
            if (data.coinId != ConfD.RequireCoinId)
                return;
            if (!HasCycleMilestone())
                return;
            InitCurScoreActData(data.total, false);
            TotalShowScore_UI = TotalScore;
            m_hasRegisteredIdleAction = false;
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
            var configList = rewardConfigList;
            var index = curMileStoneIndex;
            //累计积分已经达到普通里程的最大值
            if (TotalScore >= mileStoneMax)
            {
                //如果没有循环奖励且完成了所有里程碑
                //发放从当前里程碑到最后一个里程碑之间的所有奖励
                if (goalScore == 0)
                {
                    if (ConfDetail.FinalMilestoneReward.Count > 0)
                    {
                        //发最终大奖，这里不考虑多个配置的情况，如果后续有不同的需求，可以在这里修改
                        var finalReward = ConfDetail.FinalMilestoneReward[0].ConvertToRewardConfig();
                        var commitFinalRewardData = Game.Manager.rewardMan.BeginReward(finalReward.Id, finalReward.Count, ReasonString.score);
                        commitRewardList.Add(commitFinalRewardData);
                    }
                    for (var i = index; i < configList.Count; i++)
                    {
                        var commitData = Game.Manager.rewardMan.BeginReward(configList[i].Id,
                            configList[i].Count, ReasonString.score);
                        commitRewardList.Add(commitData);
                        DataTracker.event_score_milestone.Track(Id, Param, i + 1, From, mileStone.Count, ConfDetail.Diff, i + 1 == mileStone.Count, false);
                    }
                    return;
                }
                curMileStoneIndex = mileStone.Count - 1;
                if (TotalScore - mileStoneMax >=
                    (FinalMileStoneCount > 0 ? goalScore * FinalMileStoneCount : goalScore))
                {
                    var count = (TotalScore - mileStoneMax) / goalScore;
                    if (count != FinalMileStoneCount && FinalMileStoneCount < count)
                    {
                        // 计算需要发放的循环奖励次数
                        var rewardCount = count - FinalMileStoneCount;

                        // 如果不是初始化数据，需要发放循环奖励
                        if (!isInitScore)
                        {
                            // 发放所有应得的循环奖励
                            for (int i = 0; i < rewardCount; i++)
                            {
                                // 如果还没有随机奖励，先随机一个
                                if (RecordFinalMileStoneRewardId <= 0 || RecordFinalMileStoneRewardCount <= 0)
                                    RandomFinalReward();

                                // 发奖
                                var commitData = Game.Manager.rewardMan.BeginReward(RecordFinalMileStoneRewardId,
                                    RecordFinalMileStoneRewardCount, ReasonString.score);
                                commitRewardList.Add(commitData);
                                // 发放完奖励后，重新随机下一循环里程碑奖励
                                RandomFinalReward();

                                // 更新循环里程碑计数
                                FinalMileStoneCount += 1;

                                DataTracker.event_score_milestone.Track(Id, Param,
                                    mileStone.Count + FinalMileStoneCount, From, mileStone.Count, ConfDetail.Diff, false, true);
                            }
                        }
                        else
                        {
                            // 如果是初始化数据，直接更新计数
                            FinalMileStoneCount = count;
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
                        // 发放从当前里程碑到最后一个里程碑之间的所有奖励
                        for (var i = index; i < configList.Count; i++)
                        {
                            Game.Manager.rewardMan.CommitReward(Game.Manager.rewardMan.BeginReward(
                                configList[i].Id,
                                configList[i].Count, ReasonString.score));
                            DataTracker.event_score_milestone.Track(Id, Param, i + 1, From, mileStone.Count, ConfDetail.Diff, i + 1 == mileStone.Count, false);
                        }
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
            if (order == null || order.ConfRandomer == null || !order.ConfRandomer.IsExtraScore)
            {
                return false;
            }
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
        /// <summary>
        ///这个方法名和实际作用有些不同，实际作用更像IsComplete。
        /// </summary>
        /// <returns></returns>
        public bool HasCycleMilestone()
        {
            if (ConfDetail != null)
            {
                //是否有循环里程碑
                var hasCycle = ConfDetail.FinalMilestoneScore > 0 && ConfDetail.FinalMilestoneReward.Count > 0;
                //如果有循环里程碑，这么写是考虑可读性
                if (hasCycle)
                {
                    return true;
                }

                var mileStone = ConfDetail.MilestoneScore;
                var mileStoneMax = mileStone[mileStone.Count - 1];
                //是否还没有完成所有里程碑。
                return TotalScore < mileStoneMax;
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

        /// <summary>
        /// 检查是否需要弹窗
        /// </summary>
        /// <returns>当有新的里程碑奖励且RewardPopup为true时返回true</returns>
        public bool ShouldPopup()
        {
            return commitRewardList.Count > 0 && ConfD.RewardPopup && TotalShowScore_UI != LastShowScore_UI;
        }
        public void OnGetRewardUIPostClose()
        {
            // 保底处理：如果UI被其他弹窗打断，自动提交未Commit的奖励
            if (commitRewardList.Count > 0)
            {
                DebugEx.FormatInfo("[ActivityScore.OnGetRewardUIPostClose] 保底处理：UI被其他弹窗打断，自动提交未Commit的奖励");
                TryCommitReward();
            }

            TotalShowScore_UI = TotalScore;
            LastShowScore_UI = TotalScore;
        }
        public bool BoardEntryVisible => HasCycleMilestone();

        /// <summary>
        /// 计算给定分数对应的里程碑索引和在进度条中应该显示的分数
        /// 此方法基于InitCurScoreActData中的逻辑，用于UI表现计算
        /// 注意：此版本不考虑循环里程碑，只处理普通里程碑
        /// </summary>
        /// <param name="score">要计算的分数</param>
        /// <returns>(里程碑索引, 在进度条中显示的分数, 当前里程碑目标分数)</returns>
        public (int milestoneIndex, int showScore, int milestoneScore) CalculateScoreDisplayData(int score)
        {
            // 获取配置数据
            var eventScoreConfig = ConfDetail;
            var mileStone = eventScoreConfig.MilestoneScore;             // 普通里程碑分数列表

            // 边界检查：确保配置有效
            if (mileStone == null || mileStone.Count == 0)
            {
                DebugEx.FormatError("[CalculateScoreDisplayData] mileStone配置为空");
                return (0, score, score);
            }

            // 情况1：分数小于第一个里程碑要求分数
            if (score < mileStone[0])
            {
                // milestoneIndex: 0（第一个里程碑）
                // showScore: 直接显示当前分数
                // milestoneScore: 第一个里程碑的目标分数
                return (0, score, mileStone[0]);
            }

            // 情况2：分数在某个里程碑区间内
            // 遍历里程碑列表，找到当前分数所在的区间
            for (var i = 0; i < mileStone.Count - 1; i++)
            {
                var currentMilestone = mileStone[i];     // 当前里程碑分数
                var nextMilestone = mileStone[i + 1];    // 下一个里程碑分数

                // 检查分数是否在当前里程碑区间内：[currentMilestone, nextMilestone)
                if (score >= currentMilestone && score < nextMilestone)
                {
                    var milestoneScore = nextMilestone - currentMilestone;  // 当前里程碑的目标分数
                    var showScore = score - currentMilestone;               // 在当前里程碑内的进度

                    // milestoneIndex: i+1（里程碑索引从1开始）
                    // showScore: 在当前里程碑内的相对进度
                    // milestoneScore: 当前里程碑的目标分数
                    return (i + 1, showScore, milestoneScore);
                }
            }

            // 情况3：分数达到或超过最后一个里程碑
            var lastMilestone = mileStone[mileStone.Count - 1] - mileStone[mileStone.Count - 2];
            return (mileStone.Count - 1, lastMilestone, lastMilestone);
        }

        /// <summary>
        /// 根据里程碑索引获取该里程碑区间内的目标分数
        /// 用于进度条显示，返回的是该里程碑区间内需要达到的分数（当前里程碑分数减去前一个里程碑分数）
        /// </summary>
        /// <param name="milestoneIndex">里程碑索引（从1开始）</param>
        /// <returns>该里程碑区间内的目标分数</returns>
        public int GetMilestoneEndValue(int milestoneIndex)
        {
            var mileStone = ConfDetail.MilestoneScore;

            // 边界检查：确保配置有效
            if (mileStone == null || mileStone.Count == 0)
            {
                DebugEx.FormatError("[GetMilestoneEndValue] mileStone配置为空");
                return 0;
            }

            // 边界检查：确保索引有效
            if (milestoneIndex <= 0 || milestoneIndex > mileStone.Count)
            {
                return mileStone[^1] - mileStone[^2];
            }

            // 计算里程碑区间内的目标分数
            if (milestoneIndex == 0)
            {
                // 第一个里程碑：直接返回第一个里程碑的分数
                return mileStone[0];
            }
            else
            {
                // 其他里程碑：当前里程碑分数减去前一个里程碑分数
                return mileStone[milestoneIndex] - mileStone[milestoneIndex - 1];
            }
        }
        public void TryPopRewardUI()
        {
            if (!m_hasRegisteredIdleAction)
            {
                m_hasRegisteredIdleAction = true;
                if (!UIManager.Instance.IsOpen(PopupReward.PopupRes))
                {
                    Game.Manager.screenPopup.Queue(PopupReward);
                }
            }
        }
    }
}
