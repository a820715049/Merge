using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using fat.conf;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;
using FAT.MSG;

namespace FAT
{
    public class ActivityRaceExtend : ActivityLike, IActivityOrderHandler, IBoardEntry
    {
        public enum RaceExtendType
        {
            Normal,
            Revive,
            Cycle
        }
        #region Constant

        private const float ROUND_WIN_POPUP_DELAY = 1.5f;
        private const string SCORE_ENTITY_NAME = "Score";
        private const int PHASE_START = 0;
        private const int PLAYER_LIST_START_INDEX = 100;
        private const int RANKING_LIST_START_INDEX = 200;

        #endregion

        #region Config

        public EventRaceExtend raceExtendConfig { get; private set; }
        public RaceExtendGroup raceExtendGroupConfig { get; private set; }
        public RaceExtendRound raceExtendRoundConfig { get; private set; }
        public RaceExtendType raceExtendType { get; private set; }

        #endregion 

        #region Member variables

        public readonly RaceExtendRoundManager raceExtendManager = new();
        private readonly List<RewardCommitData> _roundRewardList = new();
        private readonly List<RewardCommitData> _milestoneRewardList = new();
        private readonly ScoreEntity _scoreEntity = new();

        #endregion

        #region Theme

        public readonly VisualPopup startPopup = new();
        public readonly VisualPopup mainPopup = new();
        public readonly VisualPopup roundOverPopup = new();
        public readonly VisualPopup endPopup = new();

        #endregion

        #region Archive

        public bool hasStart { get; private set; }
        public int roundID { get; private set; }
        public int lastOnline { get; private set; }
        public List<int> rankingList { get; private set; } = new();

        #endregion

        #region ActivityLike        

        public ActivityRaceExtend(ActivityLite lite)
        {
            Lite = lite;
            raceExtendConfig = EventRaceExtendVisitor.Get(Lite.Param);
            if (raceExtendConfig.IsRevive) { raceExtendType = RaceExtendType.Revive; }
            else { raceExtendType = RaceExtendGroupVisitor.Get(raceExtendConfig.CycleRoundId) != null ? RaceExtendType.Normal : RaceExtendType.Cycle; }
            _InitTheme();
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenSecondUpdate);
            MessageCenter.Get<SCORE_ENTITY_ADD_COMPLETE>().AddListener(_UpdateScore);
        }

        private void _InitTheme()
        {
            startPopup.Setup(raceExtendConfig.EventTheme, this, false, false);
            mainPopup.Setup(raceExtendConfig.RaceTheme, this, false, false);
            roundOverPopup.Setup(raceExtendConfig.RoundOverTheme, this, false, false);
            endPopup.Setup(raceExtendConfig.EndTheme, this, false, false);
        }

        public override void LoadSetup(ActivityInstance instance)
        {
            var any = instance.AnyState;
            hasStart = RecordStateHelper.ReadBool(dataIndex++, any);
            roundID = RecordStateHelper.ReadInt(dataIndex++, any);
            lastOnline = RecordStateHelper.ReadInt(dataIndex++, any);
            raceExtendGroupConfig = phase > raceExtendConfig.NormalRoundId.Count - 1 ? RaceExtendGroupVisitor.Get(raceExtendConfig.CycleRoundId) : RaceExtendGroupVisitor.Get(raceExtendConfig.NormalRoundId[phase]);
            raceExtendRoundConfig = RaceExtendRoundVisitor.Get(roundID);
        }

        public override void SaveSetup(ActivityInstance instance)
        {
            var any = instance.AnyState;
            any.Add(RecordStateHelper.ToRecord(dataIndex++, hasStart));
            any.Add(RecordStateHelper.ToRecord(dataIndex++, roundID));
            any.Add(RecordStateHelper.ToRecord(dataIndex++, (int)Game.Instance.GetTimestampSeconds()));
            dataIndex = RANKING_LIST_START_INDEX;
            foreach (var ranking in rankingList) { any.Add(RecordStateHelper.ToRecord(dataIndex++, ranking)); }
            dataIndex = PLAYER_LIST_START_INDEX;
            any.Add(RecordStateHelper.ToRecord(dataIndex++, raceExtendManager.myself.curScore));
            foreach (var robot in raceExtendManager.robots)
            {
                any.Add(RecordStateHelper.ToRecord(dataIndex++, robot.RobotID));
                any.Add(RecordStateHelper.ToRecord(dataIndex++, robot.curScore));
                any.Add(RecordStateHelper.ToRecord(dataIndex++, robot.avatarID));
            }
        }

        public override void Open()
        {
            if (hasStart) { mainPopup.Open(this); }
            else { startPopup.Open(this); }
        }

        public override void WhenReset()
        {
            MessageCenter.Get<SCORE_ENTITY_ADD_COMPLETE>().RemoveListener(_UpdateScore);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenSecondUpdate);
            _scoreEntity.Clear();
        }

        public override void WhenEnd()
        {
            MessageCenter.Get<SCORE_ENTITY_ADD_COMPLETE>().RemoveListener(_UpdateScore);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenSecondUpdate);
            _scoreEntity.Clear();
            endPopup.Popup();
        }

        public override void AfterLoad(ActivityInstance data_)
        {
            _LoadRankingList(data_);
            _LoadRobotList(data_);
            _CheckOfflineScore();
        }

        public override void WhenActive(bool new_)
        {
            mainPopup.visual.Theme.AssetInfo.TryGetValue(SCORE_ENTITY_NAME, out var prefab);
            if (prefab != string.Empty) { _scoreEntity.Setup(GetCurRoundCount(), this, raceExtendConfig.RequireScoreId, raceExtendConfig.ExtraScore, ReasonString.race_reward, prefab, raceExtendConfig.BoardId); }
        }

        #endregion

        #region Interface

        /// <summary>
        /// 尝试开启新一轮比赛
        /// </summary>
        public bool TryStartRound()
        {
            if (raceExtendConfig == null) { return false; }
            if (!_TryGetGroupAndRoundConfig()) { return false; }
            if (!_TryInitRobotPlayers()) { return false; }
            _BeforeStartRound();
            hasStart = true;
            MessageCenter.Get<ACTIVITY_SCORE_ROUND_CHANGE>().Dispatch();
            return true;
        }

        /// <summary>
        /// 每秒更新，机器人自动涨分数
        /// </summary>
        public void WhenSecondUpdate()
        {
            if (!hasStart) { return; }
            raceExtendManager.UpdateRobotScoreSecond();
            if (raceExtendManager.robots.Count(data => data.hasComplete) >= raceExtendRoundConfig.RaceGetNum.Count) { _RoundFail(); }
        }

        /// <summary>
        /// 获取当前回合数
        /// </summary>
        /// <returns>当前回合数</returns>
        public int GetCurRoundCount()
        {
            return phase + 1;
        }

        /// <summary>
        /// 检查是否有回合奖励
        /// </summary>
        /// <returns>是否有回合奖励</returns>
        public bool CheckHasRoundReward() => _roundRewardList.Count > 0;

        /// <summary>
        /// 检查是否有里程碑奖励
        /// </summary>
        /// <returns>是否有里程碑奖励</returns>
        public bool CheckHasMilestoneReward() => _milestoneRewardList.Count > 0;

        /// <summary>
        /// 获取回合奖励
        /// </summary>
        /// <returns>回合奖励</returns>
        public void GetRoundRewardList(out List<RewardCommitData> rewardList)
        {
            rewardList = new List<RewardCommitData>();
            rewardList.AddRange(_roundRewardList);
            _roundRewardList.Clear();
        }

        /// <summary>
        /// 获取里程碑奖励
        /// </summary>
        /// <returns>里程碑奖励, 没有里程碑奖励时返回长度为0的列表</returns>
        public void GetMilestoneRewardList(out List<RewardCommitData> rewardList)
        {
            rewardList = new List<RewardCommitData>();
            rewardList.AddRange(_milestoneRewardList);
            _milestoneRewardList.Clear();
        }

        /// <summary>
        /// 获取当前分数
        /// </summary>
        /// <returns>当前分数</returns>
        public int GetCurScore() => raceExtendManager.myself.curScore;

        #endregion

        #region IActivityOrderHandler
        public bool OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if (order == null || order.ConfRandomer == null || !order.ConfRandomer.IsExtraScore) { return false; }
            if (!hasStart) { return false; }
            var changed = false;
            var state = order.GetState((int)OrderParamType.ScoreEventId);
            if (state == null || state.Value != Id)
            {
                changed = true;
                _scoreEntity.CalcOrderScore(order, tracer);
            }

            return changed;
        }
        #endregion

        #region IBoardEntry

        public string BoardEntryAsset()
        {
            mainPopup.visual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        #endregion

        #region Logic

        /// <summary>
        /// 尝试获取新一轮的配置 
        /// </summary>
        private bool _TryGetGroupAndRoundConfig()
        {
            if (!raceExtendConfig.NormalRoundId.TryGetByIndex(phase, out var groupID)) { groupID = raceExtendConfig.CycleRoundId; }
            raceExtendGroupConfig = RaceExtendGroupVisitor.Get(groupID);
            if (raceExtendGroupConfig == null) { return false; }
            roundID = Game.Manager.userGradeMan.GetTargetConfigDataId(raceExtendGroupConfig.IncludeRoundGrpId);
            raceExtendRoundConfig = RaceExtendRoundVisitor.Get(roundID);
            if (raceExtendRoundConfig == null) { return false; }
            return true;
        }

        /// <summary>
        /// 尝试初始化比赛成员
        /// </summary>
        private bool _TryInitRobotPlayers()
        {
            if (!raceExtendManager.TryStartRaceRound(raceExtendRoundConfig)) { return false; }
            return true;
        }

        /// <summary>
        /// 回合失败
        /// </summary>
        private void _RoundFail()
        {
            hasStart = false;
            roundOverPopup.Popup();
            MessageCenter.Get<ACTIVITY_SCORE_ROUND_CHANGE>().Dispatch();
        }

        /// <summary>
        /// 回合胜利
        /// </summary>
        private void _RoundWin()
        {
            hasStart = false;
            _BeginReward();
            rankingList.Add(raceExtendManager.myself.ranking);
            MessageCenter.Get<ACTIVITY_SCORE_ROUND_CHANGE>().Dispatch();
            DOVirtual.DelayedCall(ROUND_WIN_POPUP_DELAY, () => roundOverPopup.Popup());
            _AfterWin();
        }

        private void _BeginReward()
        {
            _BeginRoundReward();
            _BeginMilestoneReward();
        }

        /// <summary>
        /// 发放回合奖励
        /// </summary>
        private void _BeginRoundReward()
        {
            var rewards = RaceExtendRewardVisitor.Get(raceExtendRoundConfig.RaceGetGift[raceExtendManager.myself.ranking]);
            _roundRewardList.Clear();
            foreach (var str in rewards.Reward)
            {
                var (cfgID, cfgCount, param) = str.ConvertToInt3();
                cfgCount = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(cfgCount, param);
                _roundRewardList.Add(Game.Manager.rewardMan.BeginReward(cfgID, cfgCount, ReasonString.race_reward));
            }
        }

        /// <summary>
        /// 发放里程碑奖励
        /// </summary>
        private void _BeginMilestoneReward()
        {
            var rewards = RaceExtendRewardVisitor.Get(raceExtendRoundConfig.MilestoneRwd);
            if (rewards == null) { return; }
            _milestoneRewardList.Clear();
            foreach (var str in rewards.Reward)
            {
                var (cfgID, cfgCount, param) = str.ConvertToInt3();
                cfgCount = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(cfgCount, param);
                _milestoneRewardList.Add(Game.Manager.rewardMan.BeginReward(cfgID, cfgCount, ReasonString.race_reward));
            }
        }

        private void _AfterWin()
        {
            switch (raceExtendType)
            {
                case RaceExtendType.Revive:
                    _CheckRevive();
                    break;
                case RaceExtendType.Cycle:
                    _CheckCycle();
                    break;
                case RaceExtendType.Normal:
                    _CheckNextRound();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Revive模式 检查如何进入下一关
        /// </summary>
        private void _CheckRevive()
        {
            if (phase == raceExtendConfig.NormalRoundId.Count - 1) { phase = PHASE_START; }
            else { phase++; }
        }

        /// <summary>
        /// Cycle模式 检查如何进入下一关
        /// </summary>
        private void _CheckCycle()
        {
            phase++;
        }

        /// <summary>
        /// Normal模式 检查如何进入下一关
        /// </summary>
        private void _CheckNextRound()
        {
            if (phase == raceExtendConfig.NormalRoundId.Count - 1) { _EndNormal(); }
            else { phase++; }
        }

        /// <summary>
        /// Normal模式 结束活动
        /// </summary>
        private void _EndNormal()
        {

        }

        private void _UpdateScore((int pre, int score, int id) data)
        {
            if (data.id != raceExtendConfig.RequireScoreId) { return; }
            raceExtendManager.UpdateMyself(data.score - data.pre);
            if (raceExtendManager.myself.hasComplete) { _RoundWin(); }
        }

        /// <summary>
        /// 加载历史名次列表
        /// </summary>
        /// <param name="data_"></param>
        private void _LoadRankingList(ActivityInstance data_)
        {
            var any = data_.AnyState;
            dataIndex = RANKING_LIST_START_INDEX;
            for (var i = 0; i < phase; i++) { rankingList.Add(RecordStateHelper.ReadInt(dataIndex++, any)); }
        }

        /// <summary>
        /// 加载机器人列表
        /// </summary>
        /// <param name="data_"></param>
        private void _LoadRobotList(ActivityInstance data_)
        {
            if (!hasStart) { return; }
            var any = data_.AnyState;
            dataIndex = PLAYER_LIST_START_INDEX;
            raceExtendManager.LoadMyself(RecordStateHelper.ReadInt(dataIndex++, any));
            for (var i = 0; i < raceExtendManager.robots.Count; i++) { raceExtendManager.LoadRobot(RecordStateHelper.ReadInt(dataIndex++, any), RecordStateHelper.ReadInt(dataIndex++, any), RecordStateHelper.ReadInt(dataIndex++, any)); }
        }

        /// <summary>
        /// 添加离线分数
        /// </summary>
        private void _CheckOfflineScore()
        {
            if (!hasStart) { return; }
            var offlineTime = Game.Instance.GetTimestampSeconds() - lastOnline;
            raceExtendManager.AddOfflineScore(offlineTime);
            if (raceExtendManager.robots.Count(data => data.hasComplete) >= raceExtendRoundConfig.RaceGetNum.Count) { _RoundFail(); }
        }

        /// <summary>
        /// 回合开始前的特殊处理
        /// </summary>
        private void _BeforeStartRound()
        {
            switch (raceExtendType)
            {
                case RaceExtendType.Revive:
                    if (phase == 0) { rankingList.Clear(); }
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
