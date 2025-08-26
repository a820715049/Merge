/*
 *@Author:chaoran.zhang
 *@Desc:热气球活动类
 *@Created Time:2024.06.27 星期四 16:36:40
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using EL;
using fat.gamekitdata;
using FAT.Merge;
using FAT.MSG;
using fat.rawdata;
using UnityEngine;
using static FAT.RecordStateHelper;
using static fat.conf.Data;
using Random = UnityEngine.Random;

namespace FAT
{
    public class ActivityRace : ActivityLike, IActivityOrderHandler, IBoardEntry, IActivityComplete
    {
        public enum RaceType
        {
            Normal = 0,
            Once = 1,
            Revive = 2
        }
        public RaceType ActivityRaceType { get; private set; }
        public EventRace ConfD;
        public override bool Valid => ConfD != null;

        #region 需要存档的成员变量
        public int Score { get; private set; } //积分
        public int Round { get; private set; } //当前轮次,-1表示进入循环轮次
        public bool HasStartRound { get; private set; } //当前是否已经开启轮次
        public int CurReward { get; private set; } //当前可用的最高奖励
        public bool HasFinish { get; private set; } //当前轮次已经完成(用于判断弹窗
        public int CurRoundID { get; private set; } //轮次ID
        public int LastUpdateTime { get; private set; } //上次更新时间，用于计算离线积分
        public int StartTime { get; private set; } //轮次开启时间，用于埋点
        #endregion

        #region 需要初始化的成员变量
        public EventRaceGroup CurRaceGroup;
        public EventRaceRound CurRaceRound;
        public List<RacePlayerInfo> BotInfos = new(); //bot信息
        #endregion

        //其他成员变量
        public bool HasPop { get; private set; } //是否已经弹出过轮次开启弹窗
        public readonly int BotStart = 7; //开始存机器人信息的下标
        public bool RefreshPanel = false; //需要重置排行界面信息
        public long LastUpdate;
        public bool HasReward;
        public bool HasShowFinish;

        #region 弹脸
        public UIResAlt StartUIResAlt => new(UIConfig.UIRaceStart);
        public UIResAlt EndUIResAlt => new(UIConfig.UIRaceEnd);
        public UIResAlt RaceResAlt => new(UIConfig.UIRacePanel);

        public ActivityVisual StartVisual = new(); //轮次开启弹窗
        public PopupActivity StartPopup = new();

        public ActivityVisual EndVisual = new(); //活动结束弹窗
        public PopupActivity EndPopup = new();

        public ActivityVisual RoundOverVisual = new(); //轮次结束弹窗
        public PopupActivity RoundOverPopup = new();

        public ActivityVisual RacePanelVisual = new();
        public PopupActivity RacePanelPopup = new();

        public override ActivityVisual Visual => RacePanelVisual;
        #endregion

        private readonly ScoreEntity _scoreEntity = new();

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Clear();
            any.Add(ToRecord(0, Score));
            any.Add(ToRecord(1, Round));
            any.Add(ToRecord(2, HasStartRound));
            any.Add(ToRecord(3, HasFinish));
            any.Add(ToRecord(4, CurRoundID));
            any.Add(ToRecord(5, (int)Game.Instance.GetTimestampSeconds()));
            any.Add(ToRecord(6, StartTime));
            var index = 0;
            //按照：ID、头像、名称、分数的顺序存储
            foreach (var botInfo in BotInfos)
            {
                any.Add(ToRecord(BotStart + index, botInfo.Id));
                index++;
                any.Add(ToRecord(BotStart + index, botInfo.Avatar));
                index++;
                any.Add(ToRecord(BotStart + index, botInfo.Name));
                index++;
                any.Add(ToRecord(BotStart + index, botInfo.Score));
                index++;
            }
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            RaceManager.GetInstance().SetActivity(this);
            CurReward = 0;
            var any = data_.AnyState;
            Score = ReadInt(0, any);
            RaceManager.GetInstance().PlayerInfo.Score = Score;
            RaceManager.GetInstance().PlayerInfo.Enable = true;
            Round = ReadInt(1, any);
            HasStartRound = ReadBool(2, any);
            HasFinish = ReadBool(3, any);
            CurRoundID = ReadInt(4, any);
            LastUpdateTime = ReadInt(5, any);
            StartTime = ReadInt(6, any);
            RefreshPop();
            if (HasStartRound)
            {
                CreateConf(false);
                ReadBotInfo(data_);
                RaceManager.GetInstance().AddBot();
                AddOfflineScore();
            }

            RacePanelVisual.Theme.AssetInfo.TryGetValue("Score", out var prefab);
            if (prefab != string.Empty)
                _scoreEntity.Setup(Score, this, ConfD.RequireScoreId, ConfD.ExtraScore, ReasonString.race_reward,
                    prefab, ConfD.BoardId);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(CheckBotScoreUpdate);
            MessageCenter.Get<SCORE_ENTITY_ADD_COMPLETE>().AddListener(UpdateScore);
            MessageCenter.Get<FLY_ICON_FEED_BACK>().AddListener(WhenFlyFeedBack);
            MessageCenter.Get<RACE_REWARD_END>().AddListener(WhenRaceRewardEnd);
        }

        public override void WhenReset()
        {
            _scoreEntity.Clear();
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(CheckBotScoreUpdate);
            MessageCenter.Get<SCORE_ENTITY_ADD_COMPLETE>().RemoveListener(UpdateScore);
            MessageCenter.Get<FLY_ICON_FEED_BACK>().RemoveListener(WhenFlyFeedBack);
            MessageCenter.Get<RACE_REWARD_END>().RemoveListener(WhenRaceRewardEnd);
            RaceManager.GetInstance().SetActivity(null);
        }

        public override void SetupFresh()
        {
            Score = 0;
            Round = 0;
            CurReward = 0;
            HasStartRound = false;
            HasFinish = false;
            RefreshPop();
            Game.Manager.screenPopup.TryQueue(StartPopup, PopupType.Login);
            HasPop = true;
            RaceManager.GetInstance().SetActivity(this);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(CheckBotScoreUpdate);
            MessageCenter.Get<SCORE_ENTITY_ADD_COMPLETE>().AddListener(UpdateScore);
            MessageCenter.Get<FLY_ICON_FEED_BACK>().AddListener(WhenFlyFeedBack);
            MessageCenter.Get<RACE_REWARD_END>().AddListener(WhenRaceRewardEnd);
            RacePanelVisual.Theme.AssetInfo.TryGetValue("Score", out var prefab);
            if (prefab != string.Empty)
                _scoreEntity.Setup(Score, this, ConfD.RequireScoreId, ConfD.ExtraScore, ReasonString.race_reward,
                    prefab, ConfD.BoardId);
        }

        public void WhenRaceRewardEnd()
        {
            if (needEndImmediate)
                Game.Manager.activity.EndImmediate(this, false);
        }

        public void UpdateScore((int pre, int score, int id) data)
        {
            if (data.id != ConfD.RequireScoreId)
                return;
            Score += data.score - data.pre;
            CheckAllFinish();
        }

        public override void Open()
        {
            if (!Active) { return; }
            if (HasStartRound) { UIManager.Instance.OpenWindow(UIConfig.UIRacePanel); }
            else { UIManager.Instance.OpenWindow(UIConfig.UIRaceStart); }
        }

        private void RefreshPop()
        {
            if (StartVisual.Setup(ConfD.EventTheme, StartUIResAlt))
                StartPopup.Setup(this, StartVisual, StartUIResAlt);
            if (EndVisual.Setup(ConfD.EndTheme, EndUIResAlt))
                EndPopup.Setup(this, EndVisual, EndUIResAlt, false, false);
            if (RoundOverVisual.Setup(ConfD.RoundOverTheme, RaceResAlt))
                RoundOverPopup.Setup(this, RoundOverVisual, RaceResAlt);
            if (RacePanelVisual.Setup(ConfD.RaceTheme, RaceResAlt))
                RacePanelPopup.Setup(this, RacePanelVisual, RaceResAlt);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (HasFinish && CurRaceRound != null)
            {
                popup_.TryQueue(RoundOverPopup, state_);
                HasPop = true;
            }

            if (!HasPop && !HasStartRound)
            {
                if (Round >= ConfD.NormalRoundId.Count && ActivityRaceType == RaceType.Once)
                {
                    endTS = Game.TimestampNow();
                    return;
                }
                popup_.TryQueue(StartPopup, state_);
            }
        }

        public override void WhenEnd()
        {
            _scoreEntity.Clear();
            Game.Manager.screenPopup.TryQueue(EndPopup, PopupType.Login);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(CheckBotScoreUpdate);
            MessageCenter.Get<SCORE_ENTITY_ADD_COMPLETE>().RemoveListener(UpdateScore);
            MessageCenter.Get<FLY_ICON_FEED_BACK>().RemoveListener(WhenFlyFeedBack);
            MessageCenter.Get<RACE_REWARD_END>().RemoveListener(WhenRaceRewardEnd);
            RaceManager.GetInstance().SetActivity(null);
        }

        public ActivityRace() { }


        public ActivityRace(ActivityLite lite)
        {
            Lite = lite;
            ConfD = GetEventRace(lite.Param);
            if (ConfD.CycleRoundId != 0) { ActivityRaceType = RaceType.Normal; }
            else { ActivityRaceType = ConfD.IsRevive ? RaceType.Revive : RaceType.Once; }
        }

        //读取存档创建bot信息
        private void ReadBotInfo(ActivityInstance data)
        {
            var any = data.AnyState;
            var index = BotStart;
            while (index < any.Count)
            {
                var id = ReadInt(index, any);
                index++;
                var avatar = ReadInt(index, any);
                index++;
                var name = ReadInt(index, any);
                index++;
                var score = ReadInt(index, any);
                index++;
                var temp = new RacePlayerInfo();
                temp.Id = id;
                temp.Avatar = avatar;
                temp.Name = name;
                temp.Score = score;
                BotInfos.Add(temp);
            }

            LastUpdate = Game.Instance.GetTimestampSeconds();
        }

        //开始计算离线积分
        private void AddOfflineScore()
        {
            var cur = Game.Instance.GetTimestampSeconds();
            if (CurRaceRound == null) { return; }
            var offline = cur - LastUpdateTime;
            if (offline > 0)
                CheckBotScoreOffline(offline);
            CheckAllFinish();
        }

        //开启新一轮活动
        public void StartRound()
        {
            if (!Active)
                return;
            Score = 0;
            HasReward = false;
            HasStartRound = true;
            RefreshPanel = true;
            HasFinish = false;
            HasShowFinish = false;
            WaitShowEnd = false;
            StartTime = (int)Game.Instance.GetTimestampSeconds();
            CreateConf();
            CreateNewBot();
            RaceManager.GetInstance().AddNewBot();
            RaceManager.GetInstance().RefreshScore();
            Game.Manager.screenPopup.TryQueue(RacePanelPopup, PopupType.Login);
            MessageCenter.Get<RACE_ROUND_START>().Dispatch(true);
        }

        //创建活动配置信息
        private void CreateConf(bool next = true)
        {
            if (Round >= ConfD.NormalRoundId.Count || Round < 0)
            {
                if (Round > 0)
                    Round = -1;
                CurRaceGroup = GetEventRaceGroup(ConfD.CycleRoundId);
            }
            else
            {
                CurRaceGroup = GetEventRaceGroup(ConfD.NormalRoundId[Round]);
            }

            if (next)
            {
                var mapping = Game.Manager.userGradeMan.GetTargetConfigDataId(CurRaceGroup.IncludeRoundGrpId);
                CurRaceRound = GetEventRaceRound(mapping);
                CurRoundID = mapping;
            }
            else
            {
                CurRaceRound = GetEventRaceRound(CurRoundID);
                if (CurRaceRound == null)
                {
                    var mapping = Game.Manager.userGradeMan.GetTargetConfigDataId(CurRaceGroup.IncludeRoundGrpId);
                    CurRoundID = mapping;
                    CurRaceRound = GetEventRaceRound(mapping);
                }
            }
        }

        public int NextRoundRewardNum()
        {
            if (Round >= ConfD.NormalRoundId.Count || Round < 0)
            {
                var group = GetEventRaceGroup(ConfD.CycleRoundId);
                var mapping = Game.Manager.userGradeMan.GetTargetConfigDataId(group.IncludeRoundGrpId);
                var round = CurRaceRound = GetEventRaceRound(mapping);
                return round.RaceGetNum.Count;
            }
            else
            {
                var group = GetEventRaceGroup(ConfD.NormalRoundId[Round]);
                var mapping = Game.Manager.userGradeMan.GetTargetConfigDataId(group.IncludeRoundGrpId);
                var round = CurRaceRound = GetEventRaceRound(mapping);
                return round.RaceGetNum.Count;
            }
        }

        //创建机器人数据
        private void CreateNewBot()
        {
            BotInfos.Clear();
            if (CurRaceRound.RobotId.Count <= 0)
                return;
            foreach (var bot in CurRaceRound.RobotId)
            {
                var temp = new RacePlayerInfo(bot, Random.Range(1f, 5f));
                BotInfos.Insert(Random.Range(0, BotInfos.Count + 1), temp);
            }

            CreateBotName();
            CreateBotAvatar();
            LastUpdate = Game.Instance.GetTimestampSeconds();
        }

        //确定机器人编号
        private void CreateBotName()
        {
            var num = 1;
            var list = new List<RacePlayerInfo>();
            list.AddRange(BotInfos);
            list.Sort((a, b) => (int)(a.AddTime - b.AddTime));
            foreach (var botInfo in list)
            {
                botInfo.Name = num;
                num++;
            }
        }

        //确定机器人头像
        private void CreateBotAvatar()
        {
            //var rd = new System.Random();
            var list = GetEventRaceRobotIconMap();
            var avatarList = new List<EventRaceRobotIcon>();
            avatarList.AddRange(list.Values.ToList());
            foreach (var botInfo in BotInfos)
                if (avatarList.Count == 1)
                {
                    botInfo.Avatar = avatarList[0].Id;
                }
                else
                {
                    var avatar = avatarList[Random.Range(0, avatarList.Count)];
                    botInfo.Avatar = avatar.Id;
                    avatarList.Remove(avatar);
                }
        }

        //每一秒检测机器人分数增长
        private void CheckBotScoreUpdate()
        {
            if (WaitShowEnd) waitTime++;
            if (!HasStartRound) return;
            if (Countdown <= 0) return;
            foreach (var bot in BotInfos) bot.UpdateScore(Game.Instance.GetTimestampSeconds());

            CheckAllFinish();
        }

        private void CheckBotScoreOffline(long offline)
        {
            var list = new List<RacePlayerInfo>();
            list.AddRange(BotInfos);
            list.Sort((a, b) => a.Id - b.Id);
            foreach (var bot in list) bot.UpdateOffline(offline);
        }

        //检测是否拿到所有奖励
        public void CheckAllFinish()
        {
            if (HasFinish) return;
            if (Score >= CurRaceRound.Score)
            {
                EndCurRound();
                return;
            }

            if (CheckBotFinish())
                EndCurRound();
        }
        bool needEndImmediate;
        //结束当前轮次并弹板
        private void EndCurRound()
        {
            if (HasFinish) return;
            UIManager.Instance.CloseWindow(UIConfig.UIRacePanel);
            HasFinish = true;
            HasStartRound = false;
            HasReward = false;
            var isfinal = ConfD.NormalRoundId.Contains(CurRaceGroup.Id) && ConfD.NormalRoundId.IndexOf(CurRaceGroup.Id) == ConfD.NormalRoundId.Count - 1;
            MessageCenter.Get<RACE_ROUND_START>().Dispatch(false);

            if (CurReward + 1 > CurRaceRound.RaceGetNum.Count)
            {
                var num = 1;
                foreach (var bot in BotInfos)
                {
                    if (bot.Score > Score || bot.Score < 0) num++;
                }
                var t = Game.Instance.GetTimestampSeconds();
                var diff = (long)Mathf.Max(0, endTS - t);
                if (diff > 0)
                {
                    Game.Manager.screenPopup.TryQueue(RoundOverPopup, PopupType.Login);
                    Game.Manager.screenPopup.TryQueue(StartPopup, PopupType.Login);
                }
                if (Active)
                    DataTracker.event_race.Track(Id, Param, From,
                        Round >= 0 ? Round + 1 : ConfD.NormalRoundId.Count - Round,
                        false, -1, num, Score,
                        (int)(Game.Instance.GetTimestampSeconds() - StartTime), false,
                        Round >= 0 ? Round + 1 : ConfD.NormalRoundId.Count - Round, ConfD.NormalRoundId.Count,
                        1, ConfD.SubType == 1 ? phase + 1 : 1, isfinal, Round < 0);
            }
            else
            {
                HasReward = true;
                RaceManager.GetInstance().RewardData = new RaceRewardData(CurReward);
                var rewardList = GetEventRaceReward(CurRaceRound.RaceGetGift[CurReward]);
                foreach (var kv in rewardList.Reward)
                {
                    var reward = ConvertToRewardConfig(kv);
                    var commitData =
                        Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.race_reward);
                    RaceManager.GetInstance().RewardData.Reward.Add(commitData);
                }
                if (Round >= 0) { Round++; }
                else { Round--; }

                if (Active)
                    DataTracker.event_race.Track(Id, Param, From,
                        Round > 0 ? Round : ConfD.NormalRoundId.Count - Round - 1,
                        false, -1, CurReward + 1, Score,
                        (int)(Game.Instance.GetTimestampSeconds() - StartTime), true,
                        Round > 0 ? Round : ConfD.NormalRoundId.Count - Round - 1, ConfD.NormalRoundId.Count,
                        1, ConfD.SubType == 1 ? phase + 1 : 1, isfinal, Round < 0);

                if (Round >= ConfD.NormalRoundId.Count)
                {
                    if (ActivityRaceType == RaceType.Normal)
                    {
                        Round = -1;
                    }
                    else if (ActivityRaceType == RaceType.Revive)
                    {
                        Round = 0;
                        phase++;
                    }
                    else if (ActivityRaceType == RaceType.Once)
                    {
                        needEndImmediate = true;
                    }
                }
                Game.Instance.StartCoroutineGlobal(ShowEnd());
            }
        }

        public bool WaitShowEnd;
        public bool Block;
        public float waitTime;
        private void WhenFlyFeedBack(FlyableItemSlice itemSlice)
        {
            if (!HasFinish || !WaitShowEnd) { return; }
            if (itemSlice.FlyType != FlyType.RaceToken) { return; }
            if (itemSlice.CurIdx < itemSlice.SplitNum) { return; }
            WaitShowEnd = false;
        }
        private IEnumerator ShowEnd()
        {
            WaitShowEnd = true;
            Block = true;
            yield return new WaitUntil(() => !WaitShowEnd || waitTime >= 3);
            if (Active)
            {
                WaitShowEnd = false;
                waitTime = 0;
                Game.Manager.screenPopup.TryQueue(RoundOverPopup, PopupType.Login);
                if (!needEndImmediate)
                {
                    Game.Manager.screenPopup.TryQueue(StartPopup, PopupType.Login);
                }
            }
        }

        //转换规则参照配表备注
        private RewardConfig ConvertToRewardConfig(string str)
        {
            var config = new RewardConfig();
            var split = str.ConvertToInt3();
            config.Id = split.Item1;
            config.Count = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(split.Item2, split.Item3);
            return config;
        }

        //检测机器人是否获得名次
        private bool CheckBotFinish()
        {
            CurReward = 0;
            var isfinal = ConfD.NormalRoundId.Contains(CurRaceGroup.Id) && ConfD.NormalRoundId.IndexOf(CurRaceGroup.Id) == ConfD.NormalRoundId.Count - 1;
            foreach (var bot in BotInfos)
                if (bot.Score < 0)
                    CurReward++;

            foreach (var bot in BotInfos)
                if (bot.Score >= CurRaceRound.Score)
                {
                    bot.Score = -CurReward - 1; //第一名为-1，第二名为-2，第三名为-3
                    CurReward++;
                    DataTracker.event_race.Track(Id, Param, From, Round >= 0 ? Round : ConfD.NormalRoundId.Count - Round,
                        true, bot.Id, CurReward, bot.Score,
                        (int)(Game.Instance.GetTimestampSeconds() - StartTime), true,
                        Round >= 0 ? Round + 1 : ConfD.NormalRoundId.Count - Round, ConfD.NormalRoundId.Count,
                        1, 1, isfinal, Round < 0);
                    if (CurReward >= CurRaceRound.RaceGetNum.Count)
                        return true;
                }

            return false;
        }

        public bool IsValidForBoard(int boardId)
        {
            return ConfD.BoardId == boardId;
        }

        public bool OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if ((order as IOrderData).IsMagicHour)
                return false;
            if (!HasStartRound)
                return false;
            var changed = false;
            var state = order.GetState((int)OrderParamType.ScoreEventId);
            if (state == null || state.Value != Id)
            {
                // 没有积分 or 不是同一期活动
                changed = true;
                _scoreEntity.CalcOrderScore(order, tracer);
            }

            return changed;
        }

        #region Debug

        public void AddScoreBot()
        {
            foreach (var bot in BotInfos)
                if (bot.Score >= 0)
                    bot.Score += Random.Range(0, 10);

            CheckAllFinish();
        }

        public void AddScore()
        {
            Score += 100;
            CheckAllFinish();
        }

        public void JumpToNext(string count)
        {
            var round = 0;
            int.TryParse(count, out round);
            if (round >= 1)
            {
                Round = round - 1;
                UIManager.Instance.OpenWindow(UIConfig.UIRaceStart);
            }
        }

        public string BoardEntryAsset()
        {
            RacePanelVisual.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        public bool HasComplete()
        {
            return false;
        }

        public bool IsActive => HasStartRound;

        public bool BoardEntryVisible => RaceManager.GetInstance().Race != null;

        #endregion
    }

    public class RaceManager
    {
        private static RaceManager Instance;
        public ActivityRace Race { get; private set; }
        public RaceRewardData RewardData = null;
        public List<RacePlayerInfo> BotInfos = new();
        public RacePlayerInfo PlayerInfo = new();
        public bool RaceStart => Race != null && Race.HasStartRound;

        private RaceManager()
        {
        }

        public static RaceManager GetInstance()
        {
            return Instance ??= new RaceManager();
        }

        public void SetActivity(ActivityRace race)
        {
            if (race == null && RewardData != null)
                foreach (var reward in RewardData.Reward)
                    Game.Manager.rewardMan.CommitReward(reward);
            RewardData = null;
            Race = race;
        }

        //用于新建的机器人信息
        public void AddNewBot()
        {
            if (Race == null || !Race.Active)
                return;
            BotInfos.Clear();
            foreach (var botInfo in Race.BotInfos)
            {
                var temp = new RacePlayerInfo();
                temp.CopyInfo(botInfo, false);
                BotInfos.Add(temp);
            }

            var list = new List<RacePlayerInfo>();
            list.AddRange(BotInfos);
            var num = Random.Range(1, Race.CurRaceRound.RobotId.Count + 1);
            for (var i = 0; i < num; i++)
            {
                var index = Random.Range(0, list.Count);
                list[index].Enable = true;
                list.RemoveAt(index);
            }

            StartAdd();
        }

        //开始加载所有bot
        private void StartAdd()
        {
            foreach (var botInfo in BotInfos)
            {
                if (botInfo.Enable)
                    continue;

                Game.Instance.StartCoroutineGlobal(EnableBot(botInfo));
            }

            return;

            IEnumerator EnableBot(RacePlayerInfo info)
            {
                yield return new WaitForSeconds(info.AddTime);
                info.Enable = true;
            }
        }

        //用于从存档中创建机器人信息
        public void AddBot()
        {
            BotInfos.Clear();
            foreach (var botInfo in Race.BotInfos)
            {
                var temp = new RacePlayerInfo();
                temp.CopyInfo(botInfo, true);
                BotInfos.Add(temp);
            }
        }

        //更新bot积分
        public void RefreshScore()
        {
            for (var i = 0; i < BotInfos.Count; i++) BotInfos[i].RefreshScore(Race.BotInfos[i]);

            PlayerInfo.Score = Race.Score;
            PlayerInfo.Enable = true;
        }

        //开启新一轮
        public void StartNewRound()
        {
            Race?.StartRound();
        }

        //获取当前名次
        public int GetNum()
        {
            var num = 1;
            if (Race == null)
                return num;
            foreach (var bot in Race.BotInfos)
                if (bot.Score < 0)
                    num++;
                else if (bot.Score >= Race.Score)
                    num++;

            return num;
        }
    }

    public class RacePlayerInfo
    {
        public int Id; //配置id
        public int Score; //分数,-1为第一名，-2为第二名，-3为第三名
        public int Avatar; //头像数据
        public int Name; //bot编号
        public float AddTime; //加入时间
        public bool Enable; //机器人是否可用
        public long LastUpdate;
        public long UpdateTime; //刷新时间
        public double LeftScore = 0f; //记录增长分数取整后残留的小数点后的分数

        public RacePlayerInfo()
        {
        }

        public RacePlayerInfo(int id, float addTime)
        {
            Id = id;
            AddTime = addTime;
            Enable = true;
        }

        public void CopyInfo(RacePlayerInfo info, bool enable)
        {
            Id = info.Id;
            Score = info.Score;
            Avatar = info.Avatar;
            Name = info.Name;
            AddTime = info.AddTime;
            Enable = enable;
        }

        public void RefreshScore(RacePlayerInfo info)
        {
            Score = info.Score;
        }

        public void UpdateScore(long interval)
        {
            //已完成的不更新
            if (Score < 0)
                return;
            if (LastUpdate == 0)
                LastUpdate = Game.Instance.GetTimestampSeconds();
            UpdateTime = interval;
            var conf = GetEventRaceRobot(Id);
            var random = Random.Range(conf.Online[0], conf.Online[1]) / 100f;
            if (interval - LastUpdate < random)
                return;
            var count = (interval - LastUpdate) / random;
            LastUpdate = UpdateTime;
            var up = Random.Range(conf.AddScore[0], conf.AddScore[1]) * count / 100f + LeftScore;
            Debug.LogFormat("RaceLog:Bot {0},need add score: {1},LeftScore{2}, count:{3}", Id, up, LeftScore,
                count);
            Score += (int)Math.Floor(up);
            Debug.LogFormat("RaceLog:Bot {0},Online interval = {1},Add score {2}", Id, random, (int)Math.Floor(up));
            LeftScore = up - (int)Math.Floor(up);
            Debug.LogFormat("RaceLog:Bot {0},true Add score: {1}, LeftScore: {2}", Id, (int)Math.Floor(up), LeftScore);
        }

        public void UpdateOffline(long offline)
        {
            //已完成的不更新
            if (RaceManager.GetInstance().Race.HasFinish)
                return;
            var conf = GetEventRaceRobot(Id);
            var interval = Random.Range(conf.Offline[0], conf.Offline[1]);
            var up = (int)offline / interval * Random.Range(conf.OfflineAddScore[0], conf.Offline[1]) / 100;
            Debug.LogFormat("RaceLog:Bot {0},Offline interval = {1},Add score {2},offline time{3}", Id, interval, up,
                offline);
            if (Score >= 0)
                Score += up;
            RaceManager.GetInstance().Race.CheckAllFinish();
        }
    }

    public class RaceRewardData
    {
        public int RewardID; //名次
        public List<RewardCommitData> Reward = new();

        public RaceRewardData(int ID)
        {
            RewardID = ID;
        }
    }
}
