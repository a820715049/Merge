// ==================================================
// // File: ActivitySeaRace.cs
// // Author: liyueran
// // Date: 2025-09-01 17:09:51
// // Desc: 海上竞速活动实例.  
// // ==================================================

using System;
using System.Collections.Generic;
using EL;
using fat.conf;
using fat.gamekitdata;
using FAT.MSG;
using fat.rawdata;
using UnityEngine;
using static FAT.RecordStateHelper;
using Random = UnityEngine.Random;
using System.Collections;
using Cysharp.Text;
using EL.Resource;


namespace FAT
{
    public class ActivitySeaRace : ActivityLike, IBoardEntry
    {
        #region 存档字段
        private int _detailID; // 用户分层

        private int _userUid = -1; // 玩家uid
        private int _score; // 当前积分（也就是本轮获得的金币数量）第一名为-1，第二名为-2，第三名为-3

        private int _curRoundIndex = -1; // 当前回合 (对应配置里的索引)
        private bool _needJoinRound; // 是否需要加入回合
        private bool _isRoundFinish; // 当前回合是否结束

        private int LastUpdateTime { get; set; } // 上次更新时间，用于计算离线积分 仅在存档时保存
        private int _roundStartTime; // 当前回合开始时间
        private int _levelRate; // 等级倍率

        private List<int> _roundFailTimes; // 每轮失败次数
        private List<int> _historyRewardIds; // 玩家历史获奖记录

        public List<SeaRacePlayerInfo> BotInfos = new(); // bot信息
        #endregion

        private readonly int _failTimeStart = 10; // 历史失败记录的存档开始位置
        private readonly int _historyStart = 30; // 历史获奖记录的存档开始位置
        private readonly int _robotStart = 50; // 机器人的存档开始位置
        public bool NeedJoinRound => _needJoinRound;
        public bool IsRoundFinish => _isRoundFinish;
        public bool IsRoundStart => _curRoundIndex > -1;

        public bool IsEnd => _curRoundIndex >= confDetail.IncludeRoundId.Count ||
                             (_curRoundIndex == confDetail.IncludeRoundId.Count - 1 && _isRoundFinish) ||
                             Countdown <= 0;

        public int CurRoundIndex => _curRoundIndex;
        public int Score => _score;
        public int UserUid => _userUid;

        public int LevelRate => _levelRate;
        public List<int> HistoryRewardIds => _historyRewardIds; // 获得过的宝箱id列表

        #region 运行时字段
        public EventSeaRace conf;
        public EventSeaRaceDetail confDetail;

        public SeaRaceCache Cache { get; private set; } // 开界面的初始状态数据

        private int _curRoundRewardAchieved; // 当前回合已被获得的奖励

        private int uidIndex = 0;
        private List<int> _uidList = new();

        // 里程碑奖励
        private readonly List<RewardCommitData> _rewardsMilestone = new();

        // 排行榜奖励
        private readonly List<RewardCommitData> _rewardsRank = new();
        #endregion

        #region UI
        public enum SeaRaceUIState
        {
            None,
            Rank, // 排行榜
            Milestone, // 里程碑
        }

        public VisualPopup PopUpMain { get; } = new(UIConfig.UIActivitySeaRaceMain); // 主UI
        public VisualPopup PopUpEnd { get; } = new(UIConfig.UIActivitySeaRaceEnd); // 结束UI
        public VisualRes PopUpFail { get; } = new(UIConfig.UIActivitySeaRaceFail); // 失败UI
        public VisualRes PopUpHelp { get; } = new(UIConfig.UIActivitySeaRaceHelp); // 帮助UI

        public override ActivityVisual Visual => PopUpMain.visual;

        public bool MainInOpenState()
        {
            var source = PopUpMain.res.ActiveR ?? UIConfig.UIActivitySeaRaceMain;
            var isOpen = UIManager.Instance.IsOpen(source);
            return isOpen;
        }

        public override void Open()
        {
            OpenByForce(true);
        }

        public void OpenByForce(bool force)
        {
            if (Active && _finishRoundCoroutine == null && !MainInOpenState())
            {
                OpenByParam(NeedJoinRound ? SeaRaceUIState.Milestone : SeaRaceUIState.Rank, null, force);
            }
        }

        public void OpenByParam(SeaRaceUIState state, SeaRaceCache cache = null, bool force = false)
        {
            // 运行中开启未执行LoadSetup时，Cache为空，需要刷新
            if (Cache == null)
            {
                RefreshCache();
            }

            var cacheNew = cache ?? new SeaRaceCache(this).Cache();
            var param = new List<object>();
            param.Add(this);
            param.Add(state);
            param.Add(cacheNew);
            if (force)
            {
                PopUpMain.res.ActiveR.Open(this, param);
            }
            else
            {
                Game.Manager.screenPopup.TryQueue(PopUpMain.popup, PopupType.Login, param);
            }
        }

        /* public void CheckOpenRank()
        {
            if (!_isRoundFinish)
            {
                return;
            }
            if (!MainInOpenState())
            {
                OpenByParam(SeaRaceUIState.Rank);
            }
        } */

        public void OpenHelp()
        {
            PopUpHelp.res.ActiveR.Open(this);
        }

        public void OpenFail(Action closeCallback)
        {
            PopUpFail.res.ActiveR.Open(this, closeCallback);
        }

        public void OpenEnd(bool queue)
        {
            if (MainInOpenState() || _finishRoundCoroutine != null)
            {
                return;
            }

            if (queue)
            {
                Game.Manager.screenPopup.TryQueue(PopUpEnd.popup, PopupType.Login, this);
            }
            else
            {
                PopUpEnd.res.ActiveR.Open(this);
            }
        }
        #endregion

        #region ActivityLike
        public ActivitySeaRace(ActivityLite lite_)
        {
            Lite = lite_;
            conf = Data.GetEventSeaRace(Lite.Param);
            _AddListener();
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;

            _detailID = ReadInt(i++, any);

            // 初始化数据
            _InitData();

            _userUid = ReadInt(i++, any);
            _score = ReadInt(i++, any);
            _curRoundIndex = ReadInt(i++, any);
            _needJoinRound = ReadBool(i++, any);
            _isRoundFinish = ReadBool(i++, any);
            _roundStartTime = ReadInt(i++, any);
            LastUpdateTime = ReadInt(i++, any);
            _levelRate = ReadInt(i++, any);

            var len = i; // 不定长存档的存档个数

            // 不定长存档 10 - 30
            _roundFailTimes = new();
            i = _failTimeStart;
            for (var index = 0; index < confDetail.IncludeRoundId.Count; index++)
            {
                var failTime = ReadInt(i++, any);
                _roundFailTimes.Add(failTime);
                len += 1;
            }

            // 不定长存档 30 - 50
            i = _historyStart;
            _historyRewardIds = new();
            for (var index = _historyStart; index < _robotStart; index++)
            {
                var history = ReadInt(i++, any);
                if (history != 0)
                {
                    len += 1;
                    _historyRewardIds.Add(history);
                }
            }

            // 机器人信息
            ReadBotInfo(len, data_);

            _curRoundRewardAchieved = 0;

            // 初始缓存
            RefreshCache();

            // 先记录缓存 再机器人加分
            AddOfflineScore();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            any.Add(ToRecord(i++, _detailID));
            any.Add(ToRecord(i++, _userUid));
            any.Add(ToRecord(i++, _score));
            any.Add(ToRecord(i++, _curRoundIndex));
            any.Add(ToRecord(i++, _needJoinRound));
            any.Add(ToRecord(i++, _isRoundFinish));
            any.Add(ToRecord(i++, _roundStartTime));

            var cur = (int)Game.Instance.GetTimestampSeconds();
            any.Add(ToRecord(i++, cur)); // 上次更新时间 LastUpdateTime
            any.Add(ToRecord(i++, _levelRate));

            // 每轮失败次数存档
            i = _failTimeStart;
            for (var j = 0; j < confDetail.IncludeRoundId.Count; j++)
            {
                var times = 0;
                if (j >= 0 && j < _roundFailTimes.Count)
                {
                    times = _roundFailTimes[j];
                }

                any.Add(ToRecord(i++, times));
            }

            // 历史获奖信息存档
            i = _historyStart;
            foreach (var id in _historyRewardIds)
            {
                any.Add(ToRecord(i++, id));
            }

            //机器人信息存档  按照：ID、UID、分数的顺序存储
            i = _robotStart;
            foreach (var botInfo in BotInfos)
            {
                any.Add(ToRecord(i++, botInfo.Id));
                any.Add(ToRecord(i++, botInfo.Uid));
                any.Add(ToRecord(i++, botInfo.Score));
            }
        }

        public override void SetupFresh()
        {
            Cache?.Release();
            Cache = null;
            RefreshCache();

            _score = 0;
            _curRoundRewardAchieved = 0;
            _curRoundIndex = -1;
            _needJoinRound = true; // 活动第一次创建 需要玩家手动加入回合
            _isRoundFinish = false;
            _historyRewardIds = new();
            _roundFailTimes = new();
            _levelRate = Game.Manager.mergeLevelMan.GetCurrentLevelRate();

            _InitData();

            //  初始化失败次数
            for (var i = 0; i < confDetail.IncludeRoundId.Count; i++)
            {
                _roundFailTimes.Add(0);
            }

            OpenByForce(false);
        }

        public override void WhenReset()
        {
            _RemoveListener();
            // restart游戏后 终端排行后续表现， 因为数据已经更新，缓存数据被重置。
            if (_finishRoundCoroutine != null)
            {
                Game.Instance.StopCoroutineGlobal(_finishRoundCoroutine);
                _finishRoundCoroutine = null;
            }
        }

        public override void WhenEnd()
        {
            _RemoveListener();
            OpenEnd(true);
            conf = null;

            // 因为表现还需要在活动结束时用 所以不在活动时清理
            // 在活动开始时清理 避免两期连开的问题
            // confDetail = null;
            // Cache?.Release();
            // Cache = null;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (NeedJoinRound && !MainInOpenState())
            {
                OpenByForce(false);
            }
        }
        #endregion

        #region 接口
        // 开始新的回合
        public bool StartRound()
        {
            if (!_needJoinRound)
            {
                // 当前回合还没结束
                return false;
            }

            if (IsEnd)
            {
                // 活动完成
                return false;
            }

            // 在StartRound清理数据 finish不清理 ui表现需要
            _curRoundIndex += 1; // 更新回合索引
            _curRoundRewardAchieved = 0; // 更新宝箱获得数量
            _needJoinRound = false; // 更新标志位
            _isRoundFinish = false;
            _score = 0; // 更新玩家得分/排名
            _roundStartTime = (int)Game.Instance.GetTimestampSeconds(); // 更新回合开始时间

            uidIndex = 0;
            _uidList.Clear();
            _userUid = GenerateUid(); // 更新uid

            BotInfos.Clear(); // 更新机器人信息
            CreateRobots();

            MessageCenter.Get<UI_SEA_RACE_ENTRY_UPDATE>().Dispatch();
            //立即存档
            Game.Manager.archiveMan.SendImmediately(true);

            // 回合开始打点
            DataTracker.event_searace_roundstart.Track(this, confDetail.Id, confDetail.IncludeRoundId.Count,
                _curRoundIndex + 1, _roundFailTimes[_curRoundIndex] + 1, 1,
                _curRoundIndex == confDetail.IncludeRoundId.Count - 1, GetRobotIds().ToString());

            return true;
        }

        private Utf16ValueStringBuilder GetRobotIds()
        {
            var sb = ZString.CreateStringBuilder();
            var roundConf = GetConfRound(_curRoundIndex);
            if (roundConf == null)
            {
                return sb;
            }

            foreach (var robot in roundConf.RobotId)
            {
                sb.Append($"{robot},");
            }

            return sb;
        }

        // 刷新临时数据
        // cache用于记录上次的数据 做表现
        public void RefreshCache()
        {
            if (Cache == null)
            {
                Cache = new SeaRaceCache(this);
            }

            Cache.Cache();
        }

        // 获得当前回合的目标分数
        public int GetCurRoundTarget()
        {
            return GetCurRoundTarget(GetConfRound());
        }

        public int GetCurRoundTarget(EventSeaRaceRound confRound)
        {
            if (confRound == null)
            {
                return -1;
            }

            var split = confRound.Target.ConvertToInt3();
            var count = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(split.Item1, split.Item2, _levelRate);

            return count;
        }

        // 填充里程碑奖励
        public int FillMilestoneRewards(List<RewardCommitData> container)
        {
            var count = _rewardsMilestone.Count;
            container.AddRange(_rewardsMilestone);
            _rewardsMilestone.Clear();
            return count;
        }

        // 填充排行榜奖励
        public int FillRankRewards(List<RewardCommitData> container)
        {
            var count = _rewardsRank.Count;
            container.AddRange(_rewardsRank);
            _rewardsRank.Clear();
            return count;
        }

        // 获得玩家的排名
        public bool TryGetUserRank(out int rank)
        {
            rank = -1;
            if (_needJoinRound)
            {
                return false;
            }

            rank = 1;
            foreach (var info in BotInfos)
            {
                // 得分比玩家高的机器人
                if (info.Score > _score)
                {
                    rank += 1;
                }

                // 已经获胜的机器人
                if (info.Score < 0)
                {
                    rank += 1;
                }
            }

            return true;
        }
        #endregion

        #region 业务逻辑
        private void _AddListener()
        {
            MessageCenter.Get<GAME_COIN_ADD>().AddListener(_WhenCoinChange);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(_OnSecond);
        }

        private void _RemoveListener()
        {
            MessageCenter.Get<GAME_COIN_ADD>().RemoveListener(_WhenCoinChange);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnSecond);
        }

        /// <summary>
        /// 初始化活动数据
        /// </summary>
        private void _InitData()
        {
            _InitDetail();
            _InitTheme();
        }

        /// <summary>
        /// 初始化detail信息(_detailID、detail)
        /// </summary>
        private void _InitDetail()
        {
            if (_detailID == 0)
            {
                _detailID = Game.Manager.userGradeMan.GetTargetConfigDataId(conf.DetailId);
            }

            confDetail = Data.GetEventSeaRaceDetail(_detailID);
        }

        /// <summary>
        /// 初始化EventTheme信息（弹窗等）
        /// </summary>
        private void _InitTheme()
        {
            // 活动结束的时候，也需要pop主面板
            PopUpMain.Setup(conf.EventTheme, this, active_: false);
            PopUpEnd.Setup(conf.EventEnd, this, false, false);
            PopUpFail.Setup(conf.EventFail);
            PopUpHelp.Setup(conf.EventHelp);
        }

        // 获得回合配置
        public EventSeaRaceRound GetConfRound()
        {
            if (_needJoinRound)
            {
                // 当前回合还未开始
                return null;
            }

            return GetConfRound(_curRoundIndex);
        }

        // 获得回合配置
        public EventSeaRaceRound GetConfRound(int index)
        {
            if (confDetail == null)
            {
                return null;
            }

            if (index < 0 || index >= confDetail.IncludeRoundId.Count)
            {
                // 索引不合法
                return null;
            }

            return Data.GetEventSeaRaceRound(confDetail.IncludeRoundId[index]);
        }

        #region 机器人
        private int GenerateUid()
        {
            var roundConf = GetConfRound();
            if (roundConf == null)
            {
                return -1;
            }

            if (_uidList.Count == 0)
            {
                // 随机生成机器人uid
                _uidList = new List<int>(roundConf.RobotId.Count);
                for (var i = 0; i < roundConf.RobotId.Count + 1; i++) // 机器人数量 + 1个玩家
                {
                    _uidList.Add(i);
                }

                _uidList.Shuffle();
            }

            var uid = _uidList[uidIndex];
            uidIndex += 1;

            return uid;
        }

        //读取存档创建bot信息
        private void ReadBotInfo(int len, ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = _robotStart;
            for (var j = 0; j < any.Count - len;)
            {
                var id = ReadInt(i++, any);
                var uid = ReadInt(i++, any);
                var score = ReadInt(i++, any);
                j += 3;

                var temp = new SeaRacePlayerInfo(this)
                {
                    Id = id,
                    Uid = uid,
                    Score = score,
                };
                BotInfos.Add(temp);
            }
        }

        private void CreateRobots()
        {
            var roundConf = GetConfRound();
            if (roundConf == null)
            {
                return;
            }

            BotInfos.Clear();
            var len = roundConf.RobotId.Count;
            for (int i = 0; i < len; i++)
            {
                var robotId = roundConf.RobotId[i];
                var robot = new SeaRacePlayerInfo(this)
                {
                    Id = robotId,
                    Uid = GenerateUid(),
                    Enable = true,
                };

                BotInfos.Add(robot);
            }

            RefreshCache();
        }

        // 机器人离线加分
        private void AddOfflineScore()
        {
            var cur = Game.Instance.GetTimestampSeconds();

            var roundConf = GetConfRound();
            if (roundConf == null)
            {
                return;
            }

            // 计算离线时长
            var offline = cur - LastUpdateTime;
            if (offline > 0)
            {
                foreach (var bot in BotInfos)
                {
                    bot.UpdateScoreOffline(offline);
                }
            }
        }
        #endregion

        #region 回合
        // 检查当前回合是否结束
        public void CheckRoundFinish()
        {
            if (_needJoinRound || _isRoundFinish)
            {
                return;
            }

            var target = GetCurRoundTarget();

            // 检查玩家积分
            if (_score >= target)
            {
                // 计算玩家排名
                _score = -1; //第一名为-1，第二名为-2，第三名为-3
                foreach (var bot in BotInfos)
                {
                    if (bot.Score < 0)
                    {
                        // 已经获奖的机器人
                        _score -= 1;
                    }
                }

                // 发放排行榜奖励
                AddRankReward();

                // 发放里程碑奖励
                AddMilestoneReward();

                // 回合胜利打点
                var cur = Game.Instance.GetTimestampSeconds();
                DataTracker.event_searace_roundwin.Track(this, confDetail.Id, confDetail.IncludeRoundId.Count,
                    _curRoundIndex + 1, _roundFailTimes[_curRoundIndex] + 1, 1,
                    cur - _roundStartTime, -_score, _curRoundIndex == confDetail.IncludeRoundId.Count - 1,
                    GetRobotIds().ToString());

                // 玩家达成任务 回合结束
                _finishRoundCoroutine = Game.Instance.StartCoroutineGlobal(FinishRound());

                // 判断活动是否结束
                if (_curRoundIndex >= confDetail.IncludeRoundId.Count - 1)
                {
                    Game.Manager.activity.EndImmediate(this, false);
                    //立即存档
                    Game.Manager.archiveMan.SendImmediately(true);
                }

                return;
            }

            // 记录获得奖励
            _curRoundRewardAchieved = 0;

            foreach (var bot in BotInfos)
            {
                if (bot.Score < 0)
                {
                    // 已经获奖的机器人
                    _curRoundRewardAchieved += 1;
                }
            }

            // 检查机器人积分
            foreach (var bot in BotInfos)
            {
                if (bot.Score >= target)
                {
                    // 达到目标的机器人
                    bot.Score = -_curRoundRewardAchieved - 1; //第一名为-1，第二名为-2，第三名为-3
                    _curRoundRewardAchieved += 1;

                    if (_curRoundRewardAchieved >= GetConfRound().RoundReward.Count)
                    {
                        // 更新失败次数
                        _roundFailTimes[_curRoundIndex] += 1;

                        // 回合失败打点
                        DataTracker.event_searace_roundlose.Track(this, confDetail.Id, confDetail.IncludeRoundId.Count,
                            _curRoundIndex + 1, _roundFailTimes[_curRoundIndex], 1,
                            _score, _curRoundIndex == confDetail.IncludeRoundId.Count - 1, GetRobotIds().ToString());

                        _score = 0;

                        // 更新index
                        _curRoundIndex -= 1;

                        // 机器人获胜 玩家失败 回合结束
                        _finishRoundCoroutine = Game.Instance.StartCoroutineGlobal(FinishRound());
                        return;
                    }
                }
            }
        }

        public Coroutine _finishRoundCoroutine;


        private IEnumerator FinishRound()
        {
            _needJoinRound = true;
            _isRoundFinish = true;

            if (!MainInOpenState())
            {
                if (_score < 0)
                {
                    yield return new UnityEngine.WaitForSeconds(2f);
                }

                // _finishRoundCoroutine = null;
                OpenByParam(SeaRaceUIState.Rank, null, false);
            }
            else
            {
                _finishRoundCoroutine = null;
                MessageCenter.Get<UI_SEA_RACE_SCORE_CHANGE>().Dispatch();
            }
        }
        #endregion

        #region 奖励
        // 里程碑奖励
        private void AddMilestoneReward()
        {
            var rewardMan = Game.Manager.rewardMan;

            if (!confDetail.MilestoneScore.Contains(_curRoundIndex + 1))
            {
                // 当前里程碑没有奖励
                return;
            }

            var milestoneRewardIndex = confDetail.MilestoneScore.IndexOf(_curRoundIndex + 1);
            var milestone = Data.GetEventSeaMilestoneReward(confDetail.MilestoneReward[milestoneRewardIndex]);
            if (milestone == null)
            {
                return;
            }

            foreach (var item in milestone.Reward)
            {
                var reward = item.ConvertToRewardConfig();
                _rewardsMilestone.Add(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.fish_milestone));
            }

            // 回合开始打点
            DataTracker.event_searace_milestone.Track(this, confDetail.Id, confDetail.MilestoneScore.Count,
                milestoneRewardIndex + 1, 1, _curRoundIndex == confDetail.IncludeRoundId.Count - 1);
        }

        // 排行榜奖励
        private void AddRankReward()
        {
            var confRound = GetConfRound();
            if (confRound == null)
            {
                return;
            }

            if (_score > 0)
            {
                // 玩家没有获胜
                return;
            }

            var rewardIndex = -_score - 1;
            var roundReward = confRound.RoundReward;
            var rankReward = Data.GetEventSeaRaceReward(roundReward[rewardIndex]);

            if (rankReward == null)
            {
                return;
            }

            // 记录历史记录
            _historyRewardIds.Add(rankReward.Id);

            var rewardMan = Game.Manager.rewardMan;
            foreach (var item in rankReward.Reward)
            {
                var reward = item.ConvertToRewardConfig();
                _rewardsRank.Add(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.sea_race_rank_chest));
            }
        }
        #endregion
        #endregion

        #region 事件
        private void _WhenCoinChange(CoinChange change_)
        {
            // 统计除卖棋子外的其他金币获取途径
            if (change_.type == CoinType.MergeCoin && change_.reason != ReasonString.sell_item)
            {
                // 玩家已经获胜 或 新一轮没开始
                if (_score < 0 || _needJoinRound || _isRoundFinish || change_.amount <= 0)
                {
                    return;
                }

                _score += change_.amount;
                DebugEx.Info($"SeaRace_{Id} user add Score:{change_.amount} cur:{_score}");

                CheckRoundFinish();
                MessageCenter.Get<UI_SEA_RACE_SCORE_CHANGE>().Dispatch();
            }
        }

        private void _OnSecond()
        {
            if (_needJoinRound)
            {
                return;
            }

            if (Countdown <= 0)
            {
                return;
            }

            var change = false;

            // 机器人加分
            foreach (var bot in BotInfos)
            {
                var result = bot.UpdateScoreOnline(Game.Instance.GetTimestampSeconds());
                if (result)
                {
                    change = true;
                }
            }

            if (change)
            {
                CheckRoundFinish();
                MessageCenter.Get<SEA_RACE_ROBOT_ADD_ONLINE_SCORE>().Dispatch();
            }
        }
        #endregion


#if UNITY_EDITOR

        #region Debug
        public void LogState()
        {
            Debug.LogError(
                $"_curRoundIndex:{_curRoundIndex} _needJoinRound:{_needJoinRound} _isRoundFinish:{_isRoundFinish} target:{GetCurRoundTarget()}");

            Debug.LogError($"User:   score:{_score}  uid:{_userUid}");

            foreach (var robot in BotInfos)
            {
                Debug.LogError($"robot id:{robot.Id}  score:{robot.Score}  uid:{robot.Uid}");
            }
        }

        public void LogCache()
        {
            if (Cache == null)
            {
                return;
            }

            Debug.LogError(
                $"Cache: _curRoundIndex:{Cache.RoundIndex}  _isRoundFinish:{Cache.IsRoundFinish}");

            Debug.LogError($"Cache: User: uid:{Cache.PlayerInfo.Uid} score:{Cache.PlayerInfo.Score}");

            foreach (var robot in Cache.Infos)
            {
                Debug.LogError($"Cache: player id:{robot.Id}  score:{robot.Score}  uid:{robot.Uid}");
            }
        }

        public void DebugStart()
        {
            StartRound();
            RefreshCache();
        }

        public void AddRound()
        {
            _needJoinRound = true;
            _isRoundFinish = true;
            _curRoundIndex += 1;
        }

        public void DebugRefreshCache()
        {
            RefreshCache();
            LogState();
            LogCache();
        }
        #endregion

#endif

        public string BoardEntryAsset()
        {
            PopUpMain.visual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        public string MetaEntryAsset()
        {
            PopUpMain.visual.Theme.AssetInfo.TryGetValue("metaEntry", out var key);
            return key;
        }

        public void TriggerSound(string name)
        {
            PopUpMain.visual.Theme.AssetInfo.TryGetValue("sound", out var theme);
            if (string.IsNullOrEmpty(theme))
            {
                return;
            }

            var soudId = theme + name;
            Game.Manager.audioMan.TriggerSound(soudId);
        }
    }

    public class SeaRaceEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => entry;
        private readonly ListActivity.Entry entry;
        private readonly ActivitySeaRace activity;

        public SeaRaceEntry(ListActivity.Entry ent, ActivitySeaRace act)
        {
            (entry, activity) = (ent, act);
            StopMetaEntryCoroutine();
            ReleaseEntry();
            metaEntryCoroutine = Game.Instance.StartCoroutineGlobal(CreateMetaEntryElement());
        }

        public override void Clear(ListActivity.Entry e_)
        {
            StopMetaEntryCoroutine();
            ReleaseEntry();
        }

        public override string TextCD(long diff_)
        {
            return UIUtility.CountDownFormat(diff_);
        }


        #region MetaEntry 入口扩展
        private GameObject metaEntryElement; //meta入口自定义元素
        private string metaEntryPoolKey; //meta入口自定义元素对象池key
        private Coroutine metaEntryCoroutine;

        private void StopMetaEntryCoroutine()
        {
            if (metaEntryCoroutine != null)
            {
                Game.Instance.StopCoroutineGlobal(metaEntryCoroutine);
                metaEntryCoroutine = null;
            }
        }

        private System.Collections.IEnumerator CreateMetaEntryElement()
        {
            var prefabName = activity.MetaEntryAsset();
            if (string.IsNullOrEmpty(prefabName))
            {
                yield break;
            }

            var asset = prefabName.ConvertToAssetConfig();
            var entryName = asset.Asset.Split(".");
            var poolKey = $"{activity.Type.ToString()}_{entryName[0]}_{activity.Id}";
            if (poolKey != metaEntryPoolKey)
            {
                ReleaseEntry();
                metaEntryPoolKey = poolKey;
            }

            if (!GameObjectPoolManager.Instance.HasPool(metaEntryPoolKey))
            {
                var loader = EL.Resource.ResManager.LoadAsset<GameObject>(asset.Group, asset.Asset);
                yield return loader;
                if (!loader.isSuccess)
                {
                    DebugEx.Error($"SeaRaceEntry::CreateMetaEntry ----> loading res error {loader.error}");
                    yield break;
                }

                GameObjectPoolManager.Instance.PreparePool(metaEntryPoolKey, loader.asset as GameObject);
            }

            if (metaEntryElement != null)
            {
                ReleaseEntry();
            }

            metaEntryElement = GameObjectPoolManager.Instance.CreateObject(metaEntryPoolKey);
            metaEntryElement.GetComponent<UISeaRaceEntryMeta>().SetData(activity);
            metaEntryElement.transform.SetParent(entry.icon.transform);
            metaEntryElement.transform.localPosition = Vector3.zero;
            metaEntryElement.transform.localScale = Vector3.one;
            metaEntryElement.name = prefabName;
            metaEntryElement.SetActive(true);
            metaEntryElement.transform.SetAsFirstSibling();
            metaEntryCoroutine = null;
        }

        private void ReleaseEntry()
        {
            if (!string.IsNullOrEmpty(metaEntryPoolKey) && metaEntryElement != null)
            {
                metaEntryElement.GetComponent<UISeaRaceEntryMeta>().SetData(null);
                metaEntryElement.SetActive(false);
                GameObjectPoolManager.Instance.ReleaseObject(metaEntryPoolKey, metaEntryElement);
                metaEntryElement = null;
            }
        }
        #endregion
    }

    public class SeaRacePlayerInfo
    {
        // 存档数据
        public int Id; //配置id
        public int Uid; // 每一轮内唯一ID
        public int Score; //分数,     /*-1为第一名，-2为第二名，-3为第三名*/

        // 运行时
        public bool Enable; //机器人是否可用
        public long LastUpdate;
        public long UpdateTime; //刷新时间
        public double LeftScore; //记录增长分数取整后残留的小数点后的分数

        private ActivitySeaRace _activity;
        private int _rank;

        public ActivitySeaRace Activity => _activity;

        public SeaRacePlayerInfo(ActivitySeaRace act)
        {
            this._activity = act;
        }

        public void CopyInfo(SeaRacePlayerInfo info, bool enable)
        {
            Id = info.Id;
            Score = info.Score;
            Uid = info.Uid;
            Enable = enable;
        }

        public void RefreshScore(SeaRacePlayerInfo info)
        {
            Score = info.Score;
        }

        // 在线加分
        public bool UpdateScoreOnline(long curTime)
        {
            //已完成的不更新
            if (Score < 0)
            {
                return false;
            }

            if (LastUpdate == 0)
            {
                LastUpdate = Game.Instance.GetTimestampSeconds();
            }

            UpdateTime = curTime;

            var conf = Data.GetEventSeaRaceRobot(Id);
            if (conf == null)
            {
                return false;
            }

            // 随机 在线 加分时间间隔
            var randomTime = Random.Range(conf.Online[0], conf.Online[1]) / 100f;
            if (curTime - LastUpdate < randomTime)
            {
                // 时间未到 不加分
                return false;
            }

            var count = (curTime - LastUpdate) / randomTime;

            // 更新刷新时间
            LastUpdate = UpdateTime;
            var up = Random.Range(conf.AddScore[0], conf.AddScore[1]) * count / 100f + LeftScore;

            LeftScore = up - (int)Math.Floor(up);

            var split = _activity.GetConfRound().Target.ConvertToInt3();
            up = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount((int)Math.Floor(up), split.Item2,
                _activity.LevelRate);

            DebugEx.Info(
                $"SeaRace_{Activity.Id} Robot: ID_{Id} Uid_{Uid} add Online Score:{(int)Math.Floor(up)} randomTime:{randomTime}");

            // 增加得分
            Score += (int)Math.Floor(up);

            return true;
        }

        // 离线加分
        public void UpdateScoreOffline(long offlineTime)
        {
            //已完成的不更新
            if (_activity.NeedJoinRound || _activity.IsRoundFinish)
            {
                return;
            }

            //已完成的不更新
            if (Score < 0)
            {
                return;
            }

            var conf = Data.GetEventSeaRaceRobot(Id);

            // 随机时间
            var randomTime = Random.Range(conf.Offline[0], conf.Offline[1]);
            var up = (int)offlineTime / randomTime * Random.Range(conf.OfflineAddScore[0], conf.Offline[1]) / 100;

            var split = _activity.GetConfRound().Target.ConvertToInt3();
            up = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(up, split.Item2, _activity.LevelRate);

            DebugEx.Info(
                $"SeaRace_{Activity.Id} Robot: ID_{Id} Uid_{Uid} add Offline Score:{up} randomTime:{randomTime}");

            if (Score >= 0)
            {
                Score += up;
            }

            // 检查加分后 活动会否完成
            _activity.CheckRoundFinish();
        }

        public int GetRank()
        {
            return _rank;
        }

        public void SetRank(int rank)
        {
            _rank = rank;
        }
    }

    public class SeaRaceCache
    {
        public List<SeaRacePlayerInfo> Infos { get; private set; } = new(); // 玩家数据+机器人数据
        public SeaRacePlayerInfo PlayerInfo { get; private set; } // 玩家数据引用
        public EventSeaRaceRound RoundConfig { get; private set; } // 当前回合配置
        public int RoundIndex { get; private set; } = -1; // 当前回合索引
        public bool IsRoundFinish { get; private set; } = false; // 当前回合是否结束

        public ActivitySeaRace Activity { get; private set; }

        public SeaRaceCache(ActivitySeaRace activity)
        {
            this.Activity = activity;
        }

        public void Paste(SeaRaceCache cache)
        {
            this.RoundIndex = cache.RoundIndex;
            this.IsRoundFinish = cache.IsRoundFinish;
            this.RoundConfig = cache.RoundConfig;

            var uIdIndexs = new Dictionary<int, int>();
            foreach (var info in Infos)
            {
                var cacheInfo = cache.GetPlayerInfo(info.Uid);
                uIdIndexs.Add(info.Uid, cache.Infos.IndexOf(cacheInfo));
                info.CopyInfo(cacheInfo, true);
            }

            Infos.Sort((a, b) =>
            {
                var a_index = uIdIndexs[a.Uid];
                var b_index = uIdIndexs[b.Uid];
                return a_index.CompareTo(b_index);
            });
            uIdIndexs.Clear();
            uIdIndexs = null;
        }

        public SeaRaceCache Cache()
        {
            Release();
            IsRoundFinish = Activity.IsRoundFinish;
            RoundIndex = Activity.CurRoundIndex;
            RoundConfig = Activity.GetConfRound(RoundIndex);

            var botInfo = Activity.BotInfos;

            foreach (var bot in botInfo)
            {
                var info = new SeaRacePlayerInfo(Activity);
                info.CopyInfo(bot, false);
                Infos.Add(info);
            }

            PlayerInfo = new SeaRacePlayerInfo(Activity)
            {
                Id = 0,
                Uid = Activity.UserUid,
                Score = Activity.Score,
            };
            Infos.Add(PlayerInfo);

            Infos.Sort((a, b) =>
            {
                var a_Score = a.Score;
                var b_Score = b.Score;
                // 排序方案：如果同为负数或者同为正数则越大越在前。如果一个正数一个负数则负数在前
                bool aNeg = a_Score < 0;
                bool bNeg = b_Score < 0;
                if (aNeg && !bNeg)
                {
                    return -1; // a负b正，a在前
                }

                if (!aNeg && bNeg)
                {
                    return 1; // a正b负，b在前
                }

                if (a_Score == b_Score)
                {
                    var aIsSelf = a.Uid == Activity.UserUid;
                    var bIsSelf = b.Uid == Activity.UserUid;
                    return aIsSelf ? -1 : bIsSelf ? 1 : 0;
                }

                // 同为负数或同为正数，越大越在前
                return b_Score.CompareTo(a_Score);
            });

            for (int i = 0; i < Infos.Count; i++)
            {
                Infos[i].SetRank(i + 1);
            }

            //把自己放到第一个 方便显示到最上层
            if (PlayerInfo != null)
            {
                Infos.Remove(PlayerInfo);
                Infos.Insert(0, PlayerInfo);
            }

            return this;
        }

        public void Release()
        {
            Infos.Clear();
            PlayerInfo = null;
            RoundConfig = null;
            RoundIndex = -1;
        }

        public SeaRacePlayerInfo GetPlayerInfo(int uid)
        {
            foreach (var info in Infos)
            {
                if (info.Uid == uid)
                {
                    return info;
                }
            }

            return null;
        }

        public int GetCompletedRoundIndex()
        {
            var roundIndex = RoundIndex;
            if (!IsRoundFinish && roundIndex > -1)
            {
                roundIndex -= 1;
            }

            return roundIndex;
        }
    }
}