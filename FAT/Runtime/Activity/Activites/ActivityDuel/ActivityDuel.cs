/**
 * @Author: zhangpengjian
 * @Date: 2025/3/6 15:31:37
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/3/6 15:31:37
 * Description: 1v1 对战活动
 */

using EL;
using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using FAT.Merge;
using System.Linq;
using UnityEngine;
using Config;
using DG.Tweening;
using FAT.MSG;
using System;

namespace FAT
{
    using static MessageCenter;

    public class ActivityDuel : ActivityLike, IBoardEntry, IActivityComplete, IActivityOrderHandler
    {
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureScoreDuel);
        public override ActivityVisual Visual => VisualMain.visual;
        public VisualPopup VisualStart { get; } = new(UIConfig.UIActivityDuelStart);
        public VisualPopup VisualMain { get; } = new(UIConfig.UIActivityDuelMain);
        public VisualRes VisualResult { get; } = new(UIConfig.UIActivityDuelResult);
        public VisualRes VisualHelp { get; } = new(UIConfig.UIDuelHelp);
        private ScoreEntity scoreEntity = new();
        public RewardCommitData ScoreCommitReward;
        public RewardCommitData MilestoneCommitReward;
        public int MilestoneMax => ConfD.MilestoneScore.Sum();
        public int MilestoneCur => curMileStoneIndex;
        public int Round => curRoundIndex;
        public bool RoundActive => isInRound == 1;
        public bool RoundValid => curRoundIndex < ConfD.RoundTarget.Count;
        public bool VisualActive { get; private set; }
        public RewardConfig RoundReward => curRoundIndex < ConfD.RoundReward.Count ? ConfD.RoundReward[curRoundIndex].ConvertToRewardConfig() : null;
        public RewardConfig LastReward => curRoundIndex > 0 ? ConfD.RoundReward[curRoundIndex - 1].ConvertToRewardConfig() : null;
        public int MilestoneTokenId => Conf.MilestoneToken;
        public int visualRound;
        public int visualScore;
        public int visualRobotScore;
        public int visualTargetScore;
        public int robotIcon;
        public bool startPopup;

        private int scorePrev;
        #region data
        private int score;
        private int robotScore;
        private int winCount;
        private int failCount;
        private int isInRound;
        private int grpIndexMappingId;
        private int curRoundIndex;
        private int curMileStoneIndex;
        private int offsetStrategyUsedTimes;
        private int roundStartTs;
        private int nextScoreTs;
        private int stgId;
        private int addScoreTimes;
        private long lastTs;
        #endregion
        private int boardId => Conf.BoardId;
        private List<(int min, int max, int weight)> ranges = new();
        internal EventScoreDuel Conf;
        internal EventScoreDuelDetail ConfD;
        private EventScoreDuelSTG currentStrategy;
        public PopupDuelStart StartPopup;


        public ActivityDuel(ActivityLite lite_)
        {
            Lite = lite_;
            Conf = Game.Manager.configMan.GetEventScoreDuelConfig(lite_.Param);
            Get<SCORE_ENTITY_ADD_COMPLETE>().AddListener(OnUpdateScore);
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(UpdateRobotScore);
            SetupTheme();
        }

        public override void SetupFresh()
        {
            grpIndexMappingId = Game.Manager.userGradeMan.GetTargetConfigDataId(Conf.DetailId);
            ConfD = Game.Manager.configMan.GetEventScoreDuelDetailConfig(grpIndexMappingId);
            //Visual.Theme.AssetInfo.TryGetValue("bgPrefab", out var prefab);
            var prefab = "event_duel_default#UIFlyMergeScore.prefab";
            scoreEntity.Setup(score, this, Conf.TokenId, ConfD.ExtraScore, ReasonString.duel, prefab, boardId);
            StartPopup = new PopupDuelStart(this, VisualStart.visual, VisualStart.res);
        }

        public void SetupTheme() {
            VisualMain.Setup(Conf.EventTheme, this, active_ : false);
            VisualStart.Setup(Conf.StartTheme, this);
            VisualResult.Setup(Conf.ResultTheme);
        }

        public string BoardEntryAsset()
        {
            //Visual.Theme.AssetInfo.TryGetValue("boardEntry", out var s);
            return "event_duel_default:ActivityDuelEntry.prefab";
        }

        public bool IsComplete()
        {
            return curRoundIndex >= ConfD.RoundTarget.Count;
        }

        public bool HasComplete()
        {
            return IsComplete();
        }

        public bool IsActive => RoundActive;

        public bool BoardEntryVisible => !IsComplete();

        public override void Open()
        {
            Open(VisualMain.res);
        }

        public override void WhenActive(bool new_)
        {
            if (!new_) return;
            if (startPopup) return;
            Game.Manager.screenPopup.TryQueue(StartPopup, PopupType.Login, true);
        }

        public override void WhenEnd()
        {
            Get<SCORE_ENTITY_ADD_COMPLETE>().RemoveListener(OnUpdateScore);
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(UpdateRobotScore);

            scoreEntity.Clear();
        }

        public override void WhenReset()
        {
            Get<SCORE_ENTITY_ADD_COMPLETE>().RemoveListener(OnUpdateScore);
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(UpdateRobotScore);

            scoreEntity.Clear();
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (startPopup) return;
            popup_.TryQueue(StartPopup, state_, true);
        }

        public void TryPopupStart(ScreenPopup popup_, PopupType state_) {
            if (startPopup) 
            {
                return;
            }
            VisualStart.Popup(popup_, state_);
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            score = ReadInt(0, any);
            robotScore = ReadInt(1, any);
            winCount = ReadInt(2, any);
            isInRound = ReadInt(3, any);
            grpIndexMappingId = ReadInt(4, any);
            curRoundIndex = ReadInt(5, any);
            curMileStoneIndex = ReadInt(6, any);
            failCount = ReadInt(7, any);
            offsetStrategyUsedTimes = ReadInt(8, any);
            roundStartTs = ReadInt(9, any);
            nextScoreTs = ReadInt(10, any);
            var lastSaveTime = ReadInt(11, any);
            stgId = ReadInt(12, any);
            visualScore = ReadInt(13, any);
            visualRobotScore = ReadInt(14, any);
            visualTargetScore = ReadInt(15, any);
            robotIcon = ReadInt(16, any);
            addScoreTimes = ReadInt(17, any);
            visualRound = ReadInt(18, any);
            startPopup = ReadBool(19, any);
            VisualActive = RoundActive;
            if (grpIndexMappingId != 0)
            {
                ConfD = Game.Manager.configMan.GetEventScoreDuelDetailConfig(grpIndexMappingId);
            }
            else
            {
                ConfD = Game.Manager.configMan.GetEventScoreDuelDetailConfig(1);
            }
            if (isInRound == 1)
            {
                currentStrategy = Game.Manager.configMan.GetEventScoreDuelSTGConfig(stgId);
                // 处理离线期间的分数
                TryHandleOfflineScore(lastSaveTime);
            }
            if (!IsComplete())
            {
                //Visual.Theme.AssetInfo.TryGetValue("bgPrefab", out var prefab);
                var prefab = "event_duel_default#UIFlyMergeScore.prefab";
                scoreEntity.Setup(score, this, Conf.TokenId, ConfD.ExtraScore, ReasonString.duel, prefab, boardId);
            }
            if (visualRound != curRoundIndex) {
                SyncScore();
            }
            lastTs = Game.Instance.GetTimestampSeconds();
            StartPopup = new PopupDuelStart(this, VisualStart.visual, VisualStart.res);
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, score));
            any.Add(ToRecord(1, robotScore));
            any.Add(ToRecord(2, winCount));
            any.Add(ToRecord(3, isInRound));
            any.Add(ToRecord(4, grpIndexMappingId));
            any.Add(ToRecord(5, curRoundIndex));
            any.Add(ToRecord(6, curMileStoneIndex));
            any.Add(ToRecord(7, failCount));
            any.Add(ToRecord(8, offsetStrategyUsedTimes));
            any.Add(ToRecord(9, roundStartTs));
            any.Add(ToRecord(10, nextScoreTs));
            any.Add(ToRecord(11, (int)Game.Instance.GetTimestampSeconds()));
            any.Add(ToRecord(12, stgId));
            any.Add(ToRecord(13, visualScore));
            any.Add(ToRecord(14, visualRobotScore));
            any.Add(ToRecord(15, visualTargetScore));
            any.Add(ToRecord(16, robotIcon));
            any.Add(ToRecord(17, addScoreTimes));
            any.Add(ToRecord(18, visualRound));
            any.Add(ToRecord(19, startPopup));
        }

        public static void DebugAddScore() {
            var acti = (ActivityDuel)Game.Manager.activity.LookupAny(fat.rawdata.EventType.ScoreDuel);
            var v = 10;
            var prev = acti.score;
            acti.score += v;
            Get<ACTIVITY_DUEL_SCORE>().Dispatch(prev, acti.score);
            Debug.LogWarning($"DebugAddScore: {acti.score}");
            acti.CheckScore();
        }

        public static void DebugAddRobotScore() {
            var acti = (ActivityDuel)Game.Manager.activity.LookupAny(fat.rawdata.EventType.ScoreDuel);
            acti.nextScoreTs = 0;
            acti.UpdateRobotScore();
        }

        #region Logic
        public void OnUpdateScore((int prev, int total, int coinId) data)
        {
            if (data.coinId != Conf.TokenId)
            {
                return;
            }
            if (isInRound == 0)
            {
                return;
            }
            score = data.total;
            scorePrev = data.prev;
            Get<ACTIVITY_DUEL_SCORE>().Dispatch(scorePrev, score);
            Debug.LogWarning($"OnUpdateScore: {score}");
            CheckScore();
        }

        private void CheckScore()
        {
            if (curRoundIndex >= ConfD.RoundTarget.Count)
            {
                Debug.LogWarning($"CheckScore: curRoundIndex >= ConfD.RoundTarget.Count");
                return;
            }
            if (score >= ConfD.RoundTarget[curRoundIndex])
            {
                Debug.LogWarning($"CheckScore: curRoundIndex: {curRoundIndex}");
                var reward = RoundReward;
                ScoreCommitReward = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.duel);
                winCount++;
                DataTracker.event_duel_roundwin.Track(this, ConfD.Diff, ConfD.RoundTarget.Count, curRoundIndex + 1, failCount + 1, (int)Game.Instance.GetTimestampSeconds() - roundStartTs, stgId);
                SetRoundEnd(true);
                Debug.LogWarning($"CheckScore: winCount: {winCount}");
                curRoundIndex++;
                failCount = 0;
                if (curMileStoneIndex < ConfD.MilestoneScore.Count && winCount >= ConfD.MilestoneScore[curMileStoneIndex])
                {
                    winCount = 0;
                    Debug.LogWarning($"CheckScore: curMileStoneIndex: {curMileStoneIndex}");
                    var milestoneReward = ConfD.MilestoneReward[curMileStoneIndex].ConvertToRewardConfig();
                    MilestoneCommitReward = Game.Manager.rewardMan.BeginReward(milestoneReward.Id, milestoneReward.Count, ReasonString.duel);
                    DataTracker.event_duel_milestone.Track(this, ConfD.Diff, curMileStoneIndex + 1, curMileStoneIndex + 1 == ConfD.MilestoneScore.Count);
                    curMileStoneIndex++;
                }
            }
        }

        public void SyncScore() {
            if (!RoundActive) {
                score = 0;
                robotScore = 0;
                robotIcon = 0;
            }
            visualRound = curRoundIndex;
            visualScore = score;
            visualRobotScore = robotScore;
            visualTargetScore = GetCurTargetScore();
            VisualActive = RoundActive;
        }

        private void InitRobot()
        {
            if (isInRound == 0)
            {
                return;
            }
            if (curRoundIndex >= ConfD.StrategyGrp.Count)
            {
                return;
            }
            robotIcon = Random.Range(1, 5);
            var strategyStr = ConfD.StrategyGrp[curRoundIndex];
            var strategies = strategyStr.Split(':');
            if (failCount >= strategies.Length)
            {
                stgId = int.Parse(strategies[strategies.Length - 1]);
            }
            else
            {
                stgId = int.Parse(strategies[failCount]);
            }
            currentStrategy = Game.Manager.configMan.GetEventScoreDuelSTGConfig(stgId);
            if (currentStrategy != null)
            {
                roundStartTs = (int)Game.Instance.GetTimestampSeconds();
                nextScoreTs = roundStartTs + currentStrategy.StartTime * 60;
                offsetStrategyUsedTimes = 0;
                Debug.LogWarning($"InitRobot: currentStrategy id = {stgId}");
                addScoreTimes = 0;
            }
            else
            {
                Debug.LogWarning($"InitRobot: currentStrategy is null");
            }
        }

        private int CalculateBaseScore()
        {
            // 解析区间和权重
            ranges.Clear();
            int totalWeight = 0;

            foreach (var range in currentStrategy.BaseScore)
            {
                var parts = range.Split(':');
                if (parts.Length == 3)
                {
                    int min = int.Parse(parts[0]);
                    int max = int.Parse(parts[1]);
                    int weight = int.Parse(parts[2]);
                    ranges.Add((min, max, weight));
                    totalWeight += weight;
                }
            }

            // 按权重随机选择区间
            int randomWeight = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var range in ranges)
            {
                currentWeight += range.weight;
                if (randomWeight < currentWeight)
                {
                    // 在选中的区间内随机取值
                    return Random.Range(range.min, range.max);
                }
            }

            return 0;
        }

        private int CalculateOffsetScore()
        {
            int scoreDiff = score - robotScore;

            if (scoreDiff <= currentStrategy.TriggerGap[0])
            {
                var offsetRange = currentStrategy.OffsetScore[0].Split(':');
                if (offsetRange.Length == 2)
                {
                    int offsetMin = int.Parse(offsetRange[0]);
                    int offsetMax = int.Parse(offsetRange[1]);
                    offsetStrategyUsedTimes++;
                    return Random.Range(offsetMin, offsetMax);
                }
            }
            // 检查触发条件
            for (int i = 0; i < currentStrategy.TriggerGap.Count - 1; i++)
            {
                int min = currentStrategy.TriggerGap[i];
                int max = currentStrategy.TriggerGap[i + 1];

                if (scoreDiff > min && scoreDiff <= max)
                {
                    // 找到对应的偏移策略
                    if (i < currentStrategy.OffsetScore.Count)
                    {
                        var offsetRange = currentStrategy.OffsetScore[i + 1].Split(':');
                        if (offsetRange.Length == 2)
                        {
                            int offsetMin = int.Parse(offsetRange[0]);
                            int offsetMax = int.Parse(offsetRange[1]);
                            offsetStrategyUsedTimes++;
                            return Random.Range(offsetMin, offsetMax);
                        }
                    }
                }
            }

            return 0;
        }

        private void UpdateRobotScore()
        {
            if (isInRound == 0 || currentStrategy == null || IsComplete())
            {
                return;
            }


            long currentTimestamp = (int)Game.Instance.GetTimestampSeconds();
            if (currentTimestamp - lastTs > 1)
            {
                TryHandleOfflineScore(lastTs);
            }

            lastTs = currentTimestamp;

            // 检查是否到达加分时间点
            if (currentTimestamp >= nextScoreTs)
            {
                // 计算基础分数
                int baseScore = CalculateBaseScore();
                DebugEx.Warning($"baseScore: {baseScore}");
                // 检查是否可以使用偏移策略
                int offsetScore = 0;
                if (offsetStrategyUsedTimes < currentStrategy.OffsetTimes)
                {
                    offsetScore = CalculateOffsetScore();
                    DebugEx.Warning($"offsetScore: {offsetScore}");
                }

                // 计算最终分数并更新
                int finalScore = baseScore + offsetScore;
                if (finalScore > 0)
                {
                    var prev = robotScore;
                    robotScore += finalScore;
                    Get<ACTIVITY_DUEL_ROBOT_SCORE>().Dispatch(prev, robotScore);
                    DebugEx.Warning($"robotScore: {robotScore}");
                    if (robotScore >= ConfD.RoundTarget[curRoundIndex])
                    {
                        DebugEx.Warning($"robot Win");
                        DataTracker.event_duel_roundlose.Track(this, ConfD.Diff, ConfD.RoundTarget.Count, curRoundIndex + 1, failCount + 1, stgId);
                        SetRoundEnd(false);
                        failCount++;
                        return;
                    }
                }

                // 计算下次加分时间点（在addStep区间内随机）
                float stepSeconds = Random.Range(0.001f, currentStrategy.AddStep * 60f + 0.001f);  // 改为最小1秒
                nextScoreTs = (roundStartTs + currentStrategy.StartTime * 60) + (currentStrategy.AddStep * 60 * addScoreTimes) + (int)stepSeconds;
                addScoreTimes += 1;
                DebugEx.Warning($"randomStepMinutes: {stepSeconds}");
                DebugEx.Warning($"nextScoreTs: {nextScoreTs}");
            }
        }
        
        private void TryHandleOfflineScore(long lastSaveTime)
        {
            if (lastSaveTime > 0)
            {
                HandleOfflineScore(lastSaveTime);
            }
            if (isInRound == 0)
            {
                return;
            }
            long currentTs = Game.Instance.GetTimestampSeconds();
            if (currentTs > nextScoreTs)
            {
                long offlineDuration = currentTs - lastSaveTime;
                var t = offlineDuration / (currentStrategy.AddStep * 60f);
                addScoreTimes += (int)Math.Floor(t);
                float stepSeconds = Random.Range(0.001f, currentStrategy.AddStep * 60f + 0.001f);  // 改为最小1秒
                nextScoreTs = (roundStartTs + currentStrategy.StartTime * 60) + (currentStrategy.AddStep * 60 * addScoreTimes) + (int)stepSeconds;
                addScoreTimes += 1;
            }
        }

        private void HandleOfflineScore(long lastOnlineTs)
        {
            if (currentStrategy == null)
            {
                return;
            }

            long currentTs = Game.Instance.GetTimestampSeconds();
            long offlineDuration = currentTs - lastOnlineTs;

            // 解析离线配置
            var offlineConfig = currentStrategy.OfflineScore.Split(':');
            if (offlineConfig.Length != 2)
            {
                return;
            }

            int intervalSeconds = int.Parse(offlineConfig[0]);
            int scorePerInterval = int.Parse(offlineConfig[1]);

            // 计算离线期间应该加的分数
            int intervals = (int)(offlineDuration / intervalSeconds);
            int offlineScore = intervals * scorePerInterval;

            if (offlineScore > 0)
            {
                var prev = robotScore;
                robotScore += offlineScore;
                Get<ACTIVITY_DUEL_ROBOT_SCORE>().Dispatch(prev, robotScore);
                DebugEx.Warning($"offlineScore: {offlineScore}");
                DebugEx.Warning($"robotScore: {robotScore}");
                // 检查是否达到回合目标分数
                if (robotScore >= ConfD.RoundTarget[curRoundIndex])
                {
                    SetRoundEnd(false);
                    failCount++;
                }
            }
        }
        public void SetRoundStart()
        {
            startPopup = true;
            if (isInRound == 1)
            {
                DebugEx.Warning($"Already in round");
                return;
            }
            if (IsComplete())
            {
                DebugEx.Warning($"Already complete");
                return;
            }
            DebugEx.Warning($"SetRoundStart");
            isInRound = 1;
            VisualActive = true;
            robotScore = 0;
            score = 0;
            SyncScore();
            scoreEntity.UpdateScore(score);
            InitRobot();
            DataTracker.event_duel_roundstart.Track(this, ConfD.Diff, ConfD.RoundTarget.Count, curRoundIndex + 1, failCount, stgId);
            Get<ACTIVITY_REFRESH>().Dispatch(this);
            Get<ACTIVITY_ENTRY_LAYOUT_REFRESH>().Dispatch();
        }

        public void SetRoundEnd(bool win_)
        {
            isInRound = 0;
            nextScoreTs = 0;
            offsetStrategyUsedTimes = 0;
            var delay = win_ ? 2.5f : 0f;
            DOVirtual.DelayedCall(delay, () => {
                if (!UIManager.Instance.IsShow(VisualMain.res.ActiveR))
                {
                    Game.Manager.screenPopup.TryQueue(VisualMain.popup, PopupType.Login);
                }
                if (IsComplete()) {
                    Game.Manager.activity.EndImmediate(this, false);
                }
            });
            Get<ACTIVITY_ENTRY_LAYOUT_REFRESH>().Dispatch();
        }

        public RewardConfig GetFinialReward()
        {
            var c = ConfD.MilestoneReward[^1];
            var reward = c.ConvertToRewardConfig();
            return reward;
        }

        public int GetPlayerScore()
        {
            return score;
        }

        public int GetRobotScore()
        {
            return robotScore;
        }

        public int GetCurTargetScore()
        {
            var list = ConfD.RoundTarget;
            return curRoundIndex < list.Count ? list[curRoundIndex] : 0;
        }

        #endregion

        #region Order
        public bool IsValidForBoard(int boardId)
        {
            return Conf.BoardId == boardId;
        }

        public bool OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if ((order as IOrderData).IsMagicHour)
                return false;
            if (IsComplete())
                return false;
            if (isInRound == 0)
            {
                return false;
            }
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
    }
}

namespace FAT {
    using static UILayer;
    
    public partial class UIConfig {
        public static UIResource UIDuelHelp = new("UIDuelHelp.prefab", AboveStatus, "event_duel_default");
        public static UIResource UIActivityDuelStart = new("UIActivityDuelStart.prefab", AboveStatus, "event_duel_default");
        public static UIResource UIActivityDuelMain = new("UIActivityDuelMain.prefab", AboveStatus, "event_duel_default");
        public static UIResource UIActivityDuelResult = new("UIActivityDuelResult.prefab", SubStatus, "event_duel_default");
    }

    public partial class ReasonString {
        public static readonly ReasonString duel = new(nameof(duel));
    }
}

namespace FAT.MSG {
    public class ACTIVITY_DUEL_SCORE : MessageBase<int, int> {}
    public class ACTIVITY_DUEL_ROBOT_SCORE : MessageBase<int, int> {}
}