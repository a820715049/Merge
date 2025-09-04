/*
 * @Author: chaoran.zhang
 * @Date: 2025-07-21 17:46:21
 * @LastEditors: chaoran.zhang
 * @LastEditTime: 2025-08-26 12:21:30
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Text;
using EL;
using fat.conf;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;
using FAT.MSG;
using UnityEngine;
using static DataTracker;

namespace FAT
{
    public class ActivityMultiplierRanking : ActivityLike, IBoardEntry, IActivityOrderHandler
    {
        #region 存档字段
        private int _detailID;
        private int _botGroupID;
        private int _totalScore;
        private int _milestoneScore;
        private int _milestoneTarget;
        private int _milestoneIndex; //当前进度条所处阶段 从0开始 根据阶段值读配置获取当前的最大进度以及达成后可获得的奖励
        private int _multiplierIndex;
        private int _multiplierResetTime = -1;
        private int _lastRanking;
        private int _lastMilestoneIndex;
        private int _lastMilestoneScore;
        private int _totalEnergy;
        private int _lastOnline;
        private int _bestRanking;
        private int _nameRandom;
        private readonly List<int> _waitClaimMilestoneTarget = new();
        #endregion

        #region 业务逻辑字段
        public MultiRank conf { get; private set; }
        public MultiRankDetail detail { get; private set; }
        public MultiRankRobotGroup botGroup;
        public readonly MultiplierRankingPlayer myself = new();
        public readonly List<MultiplierRankingPlayer> totalList = new();
        public int LastRank => _lastRanking; //上一次的排名
        public int CurRank => myself.data.ranking; //当前的排名
        public List<int> WaitClaimMilestoneTarget => _waitClaimMilestoneTarget;
        public List<RewardCommitData> _finalReward = new();
        public List<RewardCommitData> _finalMilestoneReward = new();
        private readonly ScoreEntity _scoreEntity = new();
        private bool _isForbidPopupMain;
        #endregion

        #region EventTheme
        public VisualPopup VisualUIRankingMain { get; } = new(UIConfig.UIMultiplyRankingMain); //主界面
        public VisualPopup VisualUIRankingStart { get; } = new(UIConfig.UIMultiplyRankingStart); //活动开始
        public VisualPopup VisualUIRankingEnd { get; } = new(UIConfig.UIMultiplyRankingEnd); //活动结束
        public VisualRes VisualUIRankingHelp { get; } = new(UIConfig.UIMultiplyRankingHelp); //帮助
        public VisualPopup VisualUIRankingEndReward { get; } = new(UIConfig.UIMultiplyRankingEndReward); //活动结束奖励
        public VisualRes VisualUIRankingMilestone { get; } = new(UIConfig.UIMultiplyRankingMilestone); //里程碑奖励
        public VisualRes VisualUIRankingEntryTips { get; } = new(UIConfig.UIRankingEntryTips); //转盘Tips
        public VisualRes VisualUIRankingTurntableTips { get; } = new(UIConfig.UIRankingTurntableTips); //主界面转盘Tips
        #endregion
        public override bool Valid => Lite.Valid && conf != null;
        public override ActivityVisual Visual => VisualUIRankingMain.visual;

        #region Activity
        public ActivityMultiplierRanking(ActivityLite lite)
        {
            Lite = lite;
            _InitConf();
            _InitTheme();
            _SetUpScore();
            _AddListener();
        }

        public override (long, long) SetupTS(long sTS_, long eTS_)
        {
            if (sTS_ > 0) return (sTS_, eTS_);
            var sts = Game.TimestampNow();
            var ets = sts + conf.EventDuration;
            return (sts, ets);
        }

        public override void SetupFresh()
        {
            _InitDetail();
            _InitBotGroup();
            _InitBotPlayer();
            totalList.Add(myself);
            _lastRanking = totalList.Count;
            _bestRanking = totalList.Count;
            _RefreshRanking();
            Game.Manager.screenPopup.TryQueue(VisualUIRankingStart.popup, PopupType.Login);
            _milestoneTarget = fat.conf.MultiRankMilestoneVisitor.Get(detail.MilestoneRewardGroup[_milestoneIndex]).MilestoneScore;
            _isForbidPopupMain = true;
            DataTracker.event_multiranking_start.Track(this, detail.Diff, _botGroupID);
        }

        public override void Open()
        {
            VisualUIRankingMain.res.ActiveR.Open(this, RankingOpenType.Main);
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            _LoadNormalData(data_);
            _InitDetail();
            _InitBotGroup();
            _LoadBotData(data_);
            _LoadMilestoneData(data_);
            _CheckMultiTimeOut();
            totalList.Add(myself);
            _RefreshRanking();
            _AddOffline(data_.EndTS);
        }

        private void _LoadNormalData(ActivityInstance data_)
        {
            _detailID = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _botGroupID = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _totalScore = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _milestoneScore = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _milestoneTarget = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _milestoneIndex = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _multiplierIndex = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _multiplierResetTime = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _lastRanking = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _lastMilestoneIndex = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _lastMilestoneScore = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _totalEnergy = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _lastOnline = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _bestRanking = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            _nameRandom = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
            myself.data.score = _totalScore;
        }

        private void _LoadBotData(ActivityInstance data_)
        {
            dataIndex = 100;
            for (var i = 0; i < botGroup.RobotId.Count; i++)
            {
                var id = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
                var score = RecordStateHelper.ReadInt(dataIndex++, data_.AnyState);
                var name = RecordStateHelper.ReadInt(1000 + dataIndex, data_.AnyState);
                var count = RecordStateHelper.ReadInt(1100 + dataIndex, data_.AnyState);
                totalList.Add(new MultiplierRankingPlayer(id, score, name, count));
            }
        }

        private void _LoadMilestoneData(ActivityInstance data_)
        {
            dataIndex = 500;
            while (RecordStateHelper.ReadInt(dataIndex, data_.AnyState) != 0) { _waitClaimMilestoneTarget.Add(RecordStateHelper.ReadInt(dataIndex++, data_.AnyState)); }
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            _SaveNormalData(data_);
            _SaveBotData(data_);
            _SaveMilestoneData(data_);
        }

        private void _SaveNormalData(ActivityInstance data_)
        {
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _detailID));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _botGroupID));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _totalScore));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _milestoneScore));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _milestoneTarget));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _milestoneIndex));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _multiplierIndex));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _multiplierResetTime));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _lastRanking));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _lastMilestoneIndex));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _lastMilestoneScore));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _totalEnergy));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, (int)Game.TimestampNow()));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _bestRanking));
            data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, _nameRandom));
        }

        private void _SaveBotData(ActivityInstance data_)
        {
            dataIndex = 100;
            foreach (var player in totalList)
            {
                if (!player.data.isBot) { continue; }
                data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, player.configID));
                data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, player.data.score));
                data_.AnyState.Add(RecordStateHelper.ToRecord(1000 + dataIndex, player.data.id));
                data_.AnyState.Add(RecordStateHelper.ToRecord(1100 + dataIndex, player.updateCount));
            }
        }

        private void _SaveMilestoneData(ActivityInstance data_)
        {
            dataIndex = 500;
            foreach (var target in _waitClaimMilestoneTarget) { data_.AnyState.Add(RecordStateHelper.ToRecord(dataIndex++, target)); }
        }

        private void _SetUpScore()
        {
            VisualUIRankingMain.visual.AssetMap.TryGetValue("Score", out var prefab);
            _scoreEntity.Setup(_totalScore, this, conf.Token, conf.ExtraScore, ReasonString.multi_ranking_token, prefab);
        }

        public override void WhenEnd()
        {
            Game.Manager.screenPopup.TryQueue(VisualUIRankingMain.popup, PopupType.Login, RankingOpenType.End);
            _BeginFinalReward();
            _BeginFinalMilestoneReward();
            _scoreEntity.Clear();
            _RemoveListener();
            _isForbidPopupMain = false;
            var list = new List<MultiplierRankingPlayer>();
            list.AddRange(totalList);
            list.Remove(myself);
            var final = list.Select(x => x.data.score);
            event_multiranking_complete.Track(this, detail.Diff, myself.data.ranking, _totalScore, _totalEnergy, ZString.Join(',', final));
        }

        public override void WhenReset()
        {
            _scoreEntity.Clear();
            _RemoveListener();
            _isForbidPopupMain = false;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            base.TryPopup(popup_, state_);
            if (state_ != PopupType.Login) return;
            if (_isForbidPopupMain) return;
            var popup = Game.Manager.screenPopup;
            VisualUIRankingMain.Popup(popup, custom_: RankingOpenType.Main);
        }

        #endregion

        #region IBoardEntry
        public string BoardEntryAsset()
        {
            VisualUIRankingMain.visual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }
        #endregion

        #region  IActivityOrderHandler
        public bool OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            var changed = false;
            if ((order as IOrderData).IsMagicHour) { return changed; }
            var state = order.GetState((int)OrderParamType.ScoreEventId);
            if (state == null || state.Value != Id)
            {
                changed = true;
                _scoreEntity.CalcOrderScore(order, tracer);
            }
            return changed;
        }
        #endregion

        #region 业务逻辑

        private void _AddListener()
        {
            MessageCenter.Get<SCORE_ENTITY_ADD_COMPLETE>().AddListener(_UpdateScore);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(_OneSecondDriver);
            MessageCenter.Get<GAME_MERGE_ENERGY_CHANGE>().AddListener(_OnEnergyChange);
        }

        private void _RemoveListener()
        {
            MessageCenter.Get<SCORE_ENTITY_ADD_COMPLETE>().RemoveListener(_UpdateScore);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(_OneSecondDriver);
            MessageCenter.Get<GAME_MERGE_ENERGY_CHANGE>().RemoveListener(_OnEnergyChange);
        }

        /// <summary>
        /// 初始化活动基本配置信息 
        /// </summary>
        private void _InitConf() => conf = fat.conf.MultiRankVisitor.Get(Lite.Param);

        /// <summary>
        /// 初始化detail信息
        /// </summary>
        private void _InitDetail()
        {
            if (_detailID == 0) { _detailID = Game.Manager.userGradeMan.GetTargetConfigDataId(conf.EventGroup); }
            detail = MultiRankDetailVisitor.Get(_detailID);
        }

        private void _InitBotGroup()
        {
            if (_botGroupID == 0)
            {
                var list = new List<(int, int, int)>();
                list.AddRange(detail.RobotGroup.Select(e => e.ConvertToInt3()));
                _botGroupID = list.RandomChooseByWeight(e => e.Item2).Item1;
            }
            botGroup = fat.conf.MultiRankRobotGroupVisitor.Get(_botGroupID);
        }

        private void _InitBotPlayer()
        {
            _nameRandom = UnityEngine.Random.Range(200000, 1000000);
            foreach (var bot in botGroup.RobotId) { totalList.Add(new MultiplierRankingPlayer(bot, 0)); }
        }

        /// <summary>
        /// 初始化EventTheme信息
        /// </summary>
        private void _InitTheme()
        {
            VisualUIRankingMain.Setup(conf.EventMainTheme, this, active_: false);
            VisualUIRankingStart.Setup(conf.EventStartTheme, this, active_: false);
            VisualUIRankingHelp.Setup(conf.EventHelpTheme);
            VisualUIRankingMilestone.Setup(conf.EventMilestoneTheme);
        }

        /// <summary>
        /// 加分逻辑
        /// </summary>
        /// <param name="data"></param>
        private void _UpdateScore((int pre, int score, int id) data)
        {
            if (data.id != conf.Token) { return; }
            var update = (data.score - data.pre) * GetMultiplier(GetMultiplierIndex());
            MessageCenter.Get<MSG.MULTIPLY_RANKING_BLOCK_ENTRY_UPDATE>().Dispatch();
            _UpdateMyself(update);
            _UpdateMilestoneScore(update);
            _AfterUpdateScore();
        }

        /// <summary>
        /// 更新玩家自身的积分
        /// </summary>
        /// <param name="update">积分数量</param>
        private void _UpdateMyself(int update)
        {
            _totalScore += update;
            myself.data.score = _totalScore;
            _RefreshRanking();
        }

        /// <summary>
        /// 刷新排行榜
        /// </summary>
        private void _RefreshRanking()
        {
            var beforeRanking = CurRank;
            totalList.Sort((a, b) => b.data.score - a.data.score);
            for (var index = 0; index < totalList.Count; index++)
            {
                var player = totalList[index];
                if (index == 0) player.data.ranking = 1;
                else if (player.data.score == totalList[index - 1].data.score && player.data.score != 0) player.data.ranking = totalList[index - 1].data.ranking;
                else { player.data.ranking = index + 1; }
                player.data.rewardID = detail.RankGroup[player.data.ranking - 1];
            }
            if (_bestRanking > myself.data.ranking) { _bestRanking = myself.data.ranking; }
            if (beforeRanking != CurRank) { MessageCenter.Get<MSG.MULTIPLY_RANKING_RANKING_CHANGE>().Dispatch(); }
        }

        /// <summary>
        /// 刷新里程碑进度
        /// </summary>
        /// <param name="update"></param>
        private void _UpdateMilestoneScore(int update)
        {
            if (_milestoneIndex < detail.MilestoneRewardGroup.Count) { _milestoneScore += update; }
            _RefreshMultiplier();
        }

        /// <summary>
        /// 加分后更新逻辑
        /// </summary>
        private void _AfterUpdateScore()
        {
            if (_milestoneIndex >= detail.MilestoneRewardGroup.Count) { return; }
            _EnterNextMilestone();
        }


        /// <summary>
        /// 里程碑进入下一阶段
        /// </summary>
        private void _EnterNextMilestone()
        {
            while (_milestoneScore >= _milestoneTarget && _milestoneIndex < detail.MilestoneRewardGroup.Count)
            {
                _waitClaimMilestoneTarget.Add(_milestoneTarget);
                _milestoneIndex++;
                if (_milestoneIndex < detail.MilestoneRewardGroup.Count) { _milestoneScore -= _milestoneTarget; }
                if (_milestoneIndex >= detail.MilestoneRewardGroup.Count) { _milestoneScore = _milestoneTarget; }
                else { _milestoneTarget = MultiRankMilestoneVisitor.Get(detail.MilestoneRewardGroup[_milestoneIndex]).MilestoneScore * GetMultiplier(_multiplierIndex); }
            }
        }

        /// <summary>
        /// 刷新倍率
        /// </summary>
        private void _RefreshMultiplier()
        {
            _multiplierIndex++;
            _multiplierResetTime = (int)Game.Instance.GetTimestampSeconds() + conf.MultiplierDur[_multiplierIndex >= conf.MultiplierDur.Count ? conf.MultiplierDur.Count - 1 : _multiplierIndex];
            DataTracker.event_multiranking_multiplier.Track(this, detail.Diff, GetMultiplier(_multiplierIndex), GetMultiplier(_multiplierIndex - 1), false);
        }

        /// <summary>
        /// 刷新上一次打开界面时的里程碑数据
        /// </summary>
        private void _RefreshLast()
        {
            _lastMilestoneIndex = _milestoneIndex;
            _lastMilestoneScore = _milestoneScore;
            _waitClaimMilestoneTarget.Clear();
        }

        /// <summary>
        /// 每秒更新逻辑
        /// </summary>
        private void _OneSecondDriver()
        {
            _CheckMultiTimeOut();
            _UpdateRobot();
        }

        /// <summary>
        /// 检测倍率是否超时
        /// </summary>
        private void _CheckMultiTimeOut()
        {
            if (_multiplierResetTime < 0) { return; }
            if (_multiplierResetTime > Game.Instance.GetTimestampSeconds()) { return; }
            DataTracker.event_multiranking_multiplier.Track(this, detail.Diff, GetMultiplier(0), GetMultiplier(_multiplierIndex), true);
            _multiplierIndex = 0;
            _multiplierResetTime = conf.MultiplierDur[_multiplierIndex];
        }

        /// <summary>
        /// 机器人更新积分逻辑
        /// </summary>
        private void _UpdateRobot()
        {
            var needRefresh = false;
            foreach (var player in totalList) { if (player.TryUpdateScore(this)) { needRefresh = true; } }
            if (needRefresh) { _RefreshRanking(); }
        }

        /// <summary>
        /// 记录能量消耗
        /// </summary>
        /// <param name="num"></param>
        private void _OnEnergyChange(int num)
        {
            if (num > 0) { return; }
            _totalEnergy -= num;
        }

        /// <summary>
        /// 计算机器人离线积分
        /// </summary>
        private void _AddOffline(long end)
        {
            var now = Game.TimestampNow() > end ? end : Game.TimestampNow();
            foreach (var player in totalList)
            {
                player.TryAddScoreOffline((int)now - _lastOnline, this);
            }
            _RefreshRanking();
        }

        /// <summary>
        /// 发放排名奖励
        /// </summary>
        private void _BeginFinalReward()
        {
            var config = MultiRankRewardVisitor.Get(detail.RankGroup[myself.data.ranking - 1]);
            foreach (var reward in config.RankReward)
            {
                var rewardConfig = reward.ConvertToRewardConfig();
                _finalReward.Add(Game.Manager.rewardMan.BeginReward(rewardConfig.Id, rewardConfig.Count, ReasonString.multi_ranking_token));
            }
        }

        /// <summary>
        /// 活动结束时发放未领取的里程碑奖励
        /// </summary>
        private void _BeginFinalMilestoneReward()
        {
            for (var i = _lastMilestoneIndex; i < _milestoneIndex; i++)
            {
                var config = fat.conf.MultiRankMilestoneVisitor.Get(detail.MilestoneRewardGroup[i]).MilestoneReward[0].ConvertToRewardConfig();
                _finalMilestoneReward.Add(Game.Manager.rewardMan.BeginReward(config.Id, config.Count, ReasonString.multi_ranking_token));
                DataTracker.event_multiranking_milestone.Track(this, detail.Diff, i + 1, detail.MilestoneRewardGroup.Count, i == detail.MilestoneRewardGroup.Count - 1);
            }
        }
        #endregion

        #region 接口
        /// <summary>
        /// 获取当前里程碑进度
        /// </summary>
        public int GetMilestoneScore() => _milestoneScore;

        /// <summary>
        /// 获取当前里程碑目标进度
        /// </summary>
        public int GetMilestoneTarget() => _milestoneTarget;

        /// <summary>
        /// 获取当前里程碑处在第几个阶段
        /// </summary>
        /// <returns>从0开始</returns>
        public int GetCurProgressPhase() => _milestoneIndex;

        /// <summary>
        /// 是否完成所有里程碑
        /// </summary>
        /// <returns></returns>
        public bool IsMilestoneAllComplete() => _milestoneIndex >= detail.MilestoneRewardGroup.Count;

        /// <summary>
        /// 获取指定档位的倍率
        /// </summary>
        public int GetMultiplier(int index) => conf.MultiplierSeq.TryGetByIndex(index >= conf.MultiplierSeq.Count ? conf.MultiplierSeq.Count - 1 : index, out var ret) ? ret : 1;

        /// <summary>
        /// 获取当前倍率档位
        /// </summary>
        /// <returns></returns>
        public int GetMultiplierIndex() => _multiplierIndex;

        /// <summary>
        /// 获取倍率重置倒计时
        /// </summary>
        /// <returns></returns>
        public long GetLeftMultiplierResetTime() => _multiplierResetTime - Game.Instance.GetTimestampSeconds();

        /// <summary>
        /// 待领取的奖励
        /// </summary>
        /// <returns></returns>
        public int GetTokenNum() => _waitClaimMilestoneTarget.Count;

        /// <summary>
        /// 获取上一次里程碑奖励进度
        /// </summary>
        /// <returns></returns>
        public int GetLastMilestoneIndex() => _lastMilestoneIndex;

        /// <summary>
        /// 获取上一次里程碑积分
        /// </summary>
        /// <returns></returns>
        public int GetLastMilestoneScore() => _lastMilestoneScore;

        public bool WhetherMilestoneAllComplete() => _milestoneTarget < 0;

        /// <summary>
        /// 获取上次排名并更新
        /// </summary>
        /// <returns>上次排名</returns>
        public int GetAndRefreshLastRanking()
        {
            var result = _lastRanking;
            _lastRanking = myself.data.ranking;
            return result;
        }

        /// <summary>
        /// 填充排行榜数据
        /// </summary>
        /// <param name="list"></param>
        public void FillPlayerList(List<MultiplierRankingPlayerData> list)
        {
            list.Clear();
            foreach (var player in totalList)
            {
                var data = new MultiplierRankingPlayerData(player.data);
                list.Add(data);
            }
        }

        /// <summary>
        /// 是否有里程碑奖励待领取
        /// </summary>
        /// <returns></returns>
        public bool CheckWeatherMilestoneComplete() => _waitClaimMilestoneTarget.Count > 0;

        /// <summary>
        /// 领取里程碑奖励,同时更新数据
        /// </summary>
        /// <param name="rewardList">奖励容器</param>
        /// <param name="scoreList"里程碑目标积分容器</param>
        public void ClaimMilestoneReward(List<RewardCommitData> rewardList, List<int> scoreList)
        {
            scoreList.Clear();
            rewardList.Clear();
            scoreList.AddRange(_waitClaimMilestoneTarget);
            if (Active)
            {
                for (var i = _lastMilestoneIndex; i < _milestoneIndex; i++)
                {
                    var config = fat.conf.MultiRankMilestoneVisitor.Get(detail.MilestoneRewardGroup[i]).MilestoneReward[0].ConvertToRewardConfig();
                    rewardList.Add(Game.Manager.rewardMan.BeginReward(config.Id, config.Count, ReasonString.multi_ranking_token));
                    DataTracker.event_multiranking_milestone.Track(this, detail.Diff, i + 1, detail.MilestoneRewardGroup.Count, i == detail.MilestoneRewardGroup.Count - 1);
                }
            }
            else { rewardList.AddRange(_finalMilestoneReward); }
            _RefreshLast();
        }

        /// <summary>
        /// 是否有排名奖励
        /// </summary>
        /// <returns></returns>
        public bool IsHasRankingReward()
        {
            var rewardList = MultiRankRewardVisitor.Get(CurRank);
            return rewardList.RankReward.Count > 0;
        }

        /// <summary>
        /// 获取当前倍率
        /// </summary>
        /// <returns></returns>
        public int GetCurMultiplierNum()
        {
            var multiplierIndex = GetMultiplierIndex() - 1;
            int slotNum = GetMultiplier(multiplierIndex);
            return slotNum;
        }

        /// <summary>
        /// 获取排名奖励
        /// </summary>
        /// <returns></returns>
        public List<RewardCommitData> GetRankingReward() => _finalReward;

        /// <summary>
        /// 玩家排名是否上升
        /// </summary>
        /// <param name="rank"></param>
        /// <returns></returns>
        public bool IsUpRank()
        {
            return GetAndRefreshLastRanking() > CurRank;
        }

        /// <summary>
        /// 主动调用刷新存档中记录的上次打开UI时的进度
        /// </summary>
        public void UpdateLastScore() => _lastMilestoneScore = _milestoneScore;

        public void AddScoreDebug(int score)
        {
            _UpdateMyself(score);
            _milestoneScore += score;
            _AfterUpdateScore();
        }

        public void SetEnergy(int score)
        {
            _totalEnergy = score;
        }

        /// <summary>
        /// 检测机器人是够追赶
        /// </summary>
        /// <returns></returns>
        public bool CheckNeedChasing(MultiplierRankingPlayer bot)
        {
            if (bot.data.ranking < myself.data.ranking || myself.data.score <= botGroup.RobotChasingPt) { return false; }
            if (_totalEnergy > MultiRankRewardVisitor.Get(detail.RankGroup[myself.data.ranking - 1]).RankRequiredConsume.ConvertToInt()) { return false; }
            return _totalEnergy < MultiRankRewardVisitor.Get(detail.RankGroup[bot.data.ranking - 2]).RankRequiredConsume.ConvertToInt();
        }

        /// <summary>
        /// 检测机器人是够等待
        /// </summary>
        /// <returns></returns>
        public bool CheckNeedWaiting(MultiplierRankingPlayer bot)
        {
            if (bot.data.ranking > myself.data.ranking || myself.data.score <= botGroup.RobotChasingPt) { return false; }
            if (_totalEnergy < MultiRankRewardVisitor.Get(detail.RankGroup[myself.data.ranking - 1]).RankRequiredConsume.ConvertToInt()) { return false; }
            return _totalEnergy > MultiRankRewardVisitor.Get(detail.RankGroup[bot.data.ranking - 1]).RankRequiredConsume.ConvertToInt();
        }

        #endregion
    }

    public class MultiplierRankingEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => e;
        private readonly ListActivity.Entry e;
        private readonly ActivityMultiplierRanking p;
        public MultiplierRankingEntry(ListActivity.Entry e_, ActivityMultiplierRanking p_)
        {
            (e, p) = (e_, p_);
            RefreshRedDot();
            RefreshRankNumState();
            MessageCenter.Get<FLY_ICON_FEED_BACK>().AddListener(RefreshRedDot);
            MessageCenter.Get<MULTIPLY_RANKING_RANKING_CHANGE>().AddListener(RefreshRankNumState);
        }

        private void RefreshRedDot(FlyableItemSlice slice)
        {
            if (slice.FlyType != FlyType.MergeItemFlyTarget) return;
            RefreshRedDot();
        }

        public void RefreshRedDot()
        {
            e.dot.SetActive(p.GetTokenNum() > 0);
            e.dotCount.gameObject.SetActive(p.GetTokenNum() > 0);
            e.dotCount.SetText(p.GetTokenNum().ToString());
        }

        public override void Clear(ListActivity.Entry e_)
        {
            e.frame.gameObject.SetActive(false);
            MessageCenter.Get<FLY_ICON_FEED_BACK>().RemoveListener(RefreshRedDot);
            MessageCenter.Get<MULTIPLY_RANKING_RANKING_CHANGE>().RemoveListener(RefreshRankNumState);
        }

        public override string TextCD(long diff_)
        {
            return UIUtility.CountDownFormat(diff_);
        }

        private void RefreshRankNumState()
        {
            if (p == null) return;
            var rankNum = p.CurRank;
            var stateIndex = rankNum <= 3 ? rankNum - 1 : 3;
            e.iconState.Select(stateIndex);
            e.token.text = p.CurRank.ToString();
            e.frame.gameObject.SetActive(true);
        }
    }

}