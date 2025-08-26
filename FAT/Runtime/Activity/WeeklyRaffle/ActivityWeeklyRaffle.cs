// ================================================
// File: ActivityWeeklyRaffle.cs
// Author: yueran.li
// Date: 2025/06/09 17:47:50 星期一
// Desc: 签到抽奖 活动实例
// ================================================

using System;
using System.Collections.Generic;
using System.Linq;
using EL;
using EL.Resource;
using fat.gamekitdata;
using FAT.MSG;
using fat.rawdata;
using static FAT.RecordStateHelper;
using static fat.conf.Data;
using Random = UnityEngine.Random;

namespace FAT
{
    public class ActivityWeeklyRaffle : ActivityLike, IBoardEntry
    {
        public enum CheckRet
        {
            Success = 0,
            InvalidDate = 1, // 日期非法
            TokenNotEnough = 2, // 代币不足
            AlreadyOpen = 3, // 已经开启宝箱
            AlreadySign = 4, // 已经签到
            NoNeedRefill = 5, // 不需要补签
            GemNotEnough = 6, // 钻石不足
        }

        public override bool Valid => Lite.Valid;

        //当前代币数量
        public int TokenNum => GetSignCount() - GetBoxOpenCount();

        // conf
        public EventWeeklyRaffle Conf => _conf;
        public EventWeeklyRaffleDetail ConfDetail => _confDetail;

        #region 配置
        // 配置
        private EventWeeklyRaffle _conf;

        // 当前数值模版
        private EventWeeklyRaffleDetail _confDetail;

        //用户分层
        public int GroupId => groupId;
        #endregion

        public int LevelNoAppear => levelNoAppear;
        public int LevelAppear => levelAppear;

        #region 存档
        private int groupId; // 用户分层 区别棋盘配置 对应EventFarmBoardGroup.id
        private int levelNoAppear; // 前几次必不出大奖
        private int levelAppear; // 第几次必出大奖
        private int boxIdx; // 用户开箱记录 用位来记录宝箱是否开启过 (1-7)
        private int rewardIdx; // 用户抽奖记录 用位来记录奖励是否被抽取
        private int signIdx; // 用户签到记录 用位来记录是否签到过
        #endregion

        #region UI
        public override ActivityVisual Visual => MainPopUp.visual;

        public VisualRes VisualBuyToken { get; } = new(UIConfig.UIActivityWeeklyRaffleBuyToken); // 购买界面
        public VisualRes VisualRaffle { get; } = new(UIConfig.UIActivityWeeklyRaffleDraw); // 抽奖界面
        public VisualRes VisualHelp { get; } = new(UIConfig.UIActivityWeeklyRaffleHelp); // 帮助界面

        // 弹脸
        public VisualPopup ConvertPopup { get; } = new(UIConfig.UIActivityWeeklyRaffleConvert); // 补领
        public VisualPopup MainPopUp { get; } = new(UIConfig.UIActivityWeeklyRaffleMain); // 主UI
        #endregion

        #region 活动基础
        public ActivityWeeklyRaffle() { }

        public ActivityWeeklyRaffle(ActivityLite lite_)
        {
            Lite = lite_;
            MessageCenter.Get<GAME_DAY_CHANGE_TEN>().AddListener(OnDayChangeTen);
        }


        // 活动首次初始化 | 此时不走读档流程 不会调用LoadSetup
        public override void SetupFresh()
        {
            InitConf();
            InitTheme();

            //初始化随机概率 只在活动创建时走一次
            RandomAppearProbability();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            any.Add(ToRecord(i++, GroupId));
            any.Add(ToRecord(i++, levelNoAppear));
            any.Add(ToRecord(i++, levelAppear));
            any.Add(ToRecord(i++, boxIdx));
            any.Add(ToRecord(i++, rewardIdx));
            any.Add(ToRecord(i++, signIdx));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var i = 0;
            groupId = ReadInt(i++, any);
            levelNoAppear = ReadInt(i++, any);
            levelAppear = ReadInt(i++, any);
            boxIdx = ReadInt(i++, any);
            rewardIdx = ReadInt(i++, any);
            signIdx = ReadInt(i++, any);

            InitConf();
            InitTheme();
        }

        public override void WhenActive(bool new_)
        {
            base.WhenActive(new_);

            // 上线自动签到
            TrySignIn(out var ret);
            if (ret == CheckRet.Success)
            {
                // 签到成功弹出界面
                MainPopUp.Popup(custom_: true);
                return;
            }

            // 签到失败 但是抽奖日有剩余Token
            // 判断是否弹出主界面
            if (CheckPopMain())
            {
                MainPopUp.Popup(custom_: false);
            }
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate()
        {
            if (!Valid) yield break;
            foreach (var v in MainPopUp.ResEnumerate()) yield return v;
            foreach (var v in ConvertPopup.ResEnumerate()) yield return v;
            foreach (var v in VisualRaffle.ResEnumerate()) yield return v;
            foreach (var v in VisualBuyToken.ResEnumerate()) yield return v;
            foreach (var v in VisualHelp.ResEnumerate()) yield return v;
        }

        public override void WhenReset()
        {
            RemoveListeners();
        }

        private void RemoveListeners()
        {
            MessageCenter.Get<GAME_DAY_CHANGE_TEN>().RemoveListener((OnDayChangeTen));
        }

        public override void WhenEnd()
        {
            if (_conf != null)
            {
                //回收剩余代币
                using var _ = PoolMapping.PoolMappingAccess.Borrow(out Dictionary<int, int> map);
                map[Conf.TokenId] = TokenNum;
                var tokenReward = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> tokenRewardList);
                ActivityExpire.ConvertToReward(Conf.ExpirePopup, tokenRewardList,
                    ReasonString.weekly_raffle_end_token_energy, map);
                if (tokenRewardList.Count > 0)
                {
                    Game.Manager.screenPopup.TryQueue(ConvertPopup.popup, PopupType.Login, tokenReward);
                }
            }

            RemoveListeners();
        }

        private void InitConf()
        {
            _conf = GetEventWeeklyRaffle(Lite.Param);

            // 判断是否分层
            if (GroupId <= 0)
            {
                groupId = Game.Manager.userGradeMan.GetTargetConfigDataId(Conf.Detail);
            }

            _confDetail = GetEventWeeklyRaffleDetail(GroupId);
        }


        private void InitTheme()
        {
            VisualBuyToken.Setup(Conf.ThemeBuy);
            VisualRaffle.Setup(Conf.ThemeRaffle);
            VisualHelp.Setup(Conf.ThemeInfo);

            MainPopUp.Setup(Conf.EventTheme, this);
            ConvertPopup.Setup(Conf.ThemeExchange, this, false, false);
        }
        #endregion

        #region 配置处理
        /// 获得配置的签到次数
        public int GetConfigSignDayCount()
        {
            // 根据开始和结束时间 计算配置天数
            var diff = endTS - startTS;
            var hour = diff / 3600;
            var day = hour / 24;
            return (int)day;
        }

        // 获取配置的刷新时间
        private int GetStartUtc()
        {
            return Game.Manager.configMan.globalConfig.RequireTypeUtcClock;
        }

        /// 获取当前奖池配置
        public List<EventWeeklyRaffleReward> GetRaffleRewards()
        {
            var rewardGroupID = ConfDetail.RewardGroup;
            var grpConfig = Game.Manager.configMan.GetEventWeeklyRaffleGroupConfig(groupId);
            var rewardList = new List<EventWeeklyRaffleReward>();
            foreach (var idx in grpConfig.RewardList)
            {
                rewardList.Add(GetEventWeeklyRaffleReward(idx)); // 获取单个奖励位配置
            }


            return rewardList;
        }

        public EventWeeklyRaffleGrp GetWeeklyRaffleGroup()
        {
            var grpConfig = Game.Manager.configMan.GetEventWeeklyRaffleGroupConfig(groupId);
            return grpConfig;
        }

        // 获得大奖配置
        public EventWeeklyRaffleReward GetConfigJackPot()
        {
            var rewards = GetRaffleRewards();

            foreach (var reward in rewards)
            {
                if (reward.IsJackpot)
                {
                    return reward;
                }
            }

            return null;
        }
        #endregion

        #region 日期
        /// 判断是不是抽奖日
        public bool CheckIsRaffleDay()
        {
            var currentDay = GetOffsetDay();
            // 判断是不是最后一天
            return currentDay >= GetConfigSignDayCount() - 1;
        }

        /// 判断是不是抽奖日
        public bool CheckIsRaffleDay(int day)
        {
            // 判断是不是最后一天
            return day == GetConfigSignDayCount() - 1;
        }

        /// 获取指定天数的状态 0:未来 1:今天 2:过去
        public int GetDayState(int day)
        {
            var offsetDay = GetOffsetDay();
            var today = offsetDay == day; // 今天
            if (today)
            {
                return 1;
            }

            var future = day > offsetDay; // 未来

            if (future)
            {
                return 0;
            }

            // 过去
            return 2;
        }

        /// <summary>
        /// 获取当前UTC十点为新的一天的依据，判断当前日期
        /// </summary>
        public DateTime GetUtcOffsetTime()
        {
            // 获取校准后的当前 UTC 时间
            var utcTime = DateTime.UtcNow.AddSeconds(Game.Manager.networkMan.networkBias);

            var startUtc = GetStartUtc();
            // 创建当天的刷新时间点
            var utcTimeOffset =
                new DateTime(utcTime.Year, utcTime.Month, utcTime.Day, startUtc, 0, 0, DateTimeKind.Utc);

            // 判断是否需要回退一天
            if (utcTime < utcTimeOffset)
            {
                // 如果当前时间小于设定的刷新时间点，说明今天还没到刷新时间
                // 此时将时间点往前推一天，表示现在还在"前一天"
                utcTimeOffset = utcTimeOffset.AddDays(-1);
            }

            return utcTimeOffset;
        }

        /// 获得现在是签到第几天 从0开始
        public int GetOffsetDay()
        {
            // 如果当前时间还没到今天的刷新点（比如现在是 9:30，刷新点是 10:00），就返回昨天的刷新点
            // 如果当前时间已经过了今天的刷新点，就返回今天的刷新点
            var t = Game.Timestamp(GetUtcOffsetTime());

            // 计算总时间差（秒）
            var diff = t - startTS;
            var hour = diff / 3600;
            var day = hour / 24;
            return (int)day;
        }
        #endregion

        #region 签到
        // 检查是否弹出主界面
        private bool CheckPopMain()
        {
            var day = GetOffsetDay();

            // 日期非法
            if (day < 0 || day >= GetConfigSignDayCount())
            {
                return false;
            }

            // 没签到 弹出
            if (!HasSign(day))
            {
                return true;
            }

            // 抽奖日有剩余Token
            if (CheckIsRaffleDay(day))
            {
                return TokenNum > 0;
            }

            return false;
        }

        // 判断某天是否签到过
        public bool HasSign(int day)
        {
            return (signIdx & (1 << day)) != 0;
        }

        // 判断某天是否漏签
        public bool CheckMissSign(int day)
        {
            if (HasSign(day))
            {
                return false;
            }

            if (day >= GetOffsetDay())
            {
                // 时间还没到 不会漏签
                return false;
            }

            return true;
        }

        private void SaveSignIdx(int day)
        {
            signIdx |= 1 << day;
            MessageCenter.Get<WEEKLYRAFFLE_TOKEN_CHANGE>().Dispatch(false);
        }

        /// 检查是否可以签到/补签
        private bool CheckSignInEnable(int day, out CheckRet ret)
        {
            if (day < 0 || day >= GetConfigSignDayCount())
            {
                // 日期非法
                ret = CheckRet.InvalidDate;
                return false;
            }

            if (day > GetOffsetDay())
            {
                // 时间还没到 不能签到
                ret = CheckRet.InvalidDate;
                return false;
            }

            if (HasSign(day))
            {
                // 已经签到完成
                ret = CheckRet.AlreadySign;
                return false;
            }

            ret = CheckRet.Success;
            return true;
        }


        // 处理十点日期变更事件
        private void OnDayChangeTen()
        {
            var day = GetOffsetDay();
            if (day < GetSignCount())
            {
                return;
            }

            // 日期变更
            TrySignIn(out var ret);
            if (ret == CheckRet.Success)
            {
                var ui = UIManager.Instance.TryGetUI(UIConfig.UIActivityWeeklyRaffleMain);
                if (ui != null && ui is UIActivityWeeklyRaffleMain main && ui.IsOpen())
                {
                    // 关闭其他界面
                    UIManager.Instance.CloseWindow(UIConfig.UIActivityWeeklyRaffleHelp);
                    UIManager.Instance.CloseWindow(UIConfig.UIActivityWeeklyRaffleBuyToken);
                    UIManager.Instance.CloseWindow(UIConfig.UIActivityWeeklyRaffleRewardTips);

                    // 播放动画
                    main.PlaySignAnim(day, false);
                }
                else
                {
                    MainPopUp.Popup(custom_: true);
                }
            }
        }

        private void TrySignIn(out CheckRet ret)
        {
            var day = GetOffsetDay();

            if (!CheckSignInEnable(day, out ret))
            {
                return;
            }

            // 签到
            SaveSignIdx(day);

            // 埋点
            DataTracker.event_weeklyraffle_tokenget.Track(this, day + 1, 1, 0, ConfDetail.Diff);
        }

        // 获得签到次数
        public int GetSignCount()
        {
            int count = 0;
            int temp = signIdx;
            while (temp != 0)
            {
                count += temp & 1; // 检查最低位是否为1
                temp >>= 1; // 右移一位
            }

            return count;
        }
        #endregion

        #region 抽奖
        /// 检查是否可以抽奖
        public bool CheckCanRaffle(int boxId, out CheckRet ret)
        {
            if (!CheckIsRaffleDay())
            {
                ret = CheckRet.InvalidDate;
                return false;
            }

            if (HasOpen(boxId))
            {
                ret = CheckRet.AlreadyOpen;
                return false;
            }

            if (TokenNum <= 0)
            {
                ret = CheckRet.TokenNotEnough;
                return false;
            }

            ret = CheckRet.Success;
            return true;
        }

        /// <summary>
        /// 随机第几次出与不出次数 只在初始化调用一次
        /// </summary>
        private void RandomAppearProbability()
        {
            var raffleGroup = GetWeeklyRaffleGroup();
            if (levelNoAppear == 0 && raffleGroup.JackpoNoAppear.Count > 0)
                levelNoAppear = Random.Range(raffleGroup.JackpoNoAppear[0], raffleGroup.JackpoNoAppear[1] + 1);
            if (levelAppear == 0 && raffleGroup.JackpotAppear.Count > 0)
                levelAppear = Random.Range(raffleGroup.JackpotAppear[0], raffleGroup.JackpotAppear[1] + 1);
        }

        /// 该宝箱是否开启过
        public bool HasOpen(int boxId)
        {
            return (boxIdx & (1 << boxId)) != 0;
        }

        /// 保存开启过的宝箱
        private void SaveBoxIndexOpened(int boxId)
        {
            boxIdx |= 1 << boxId;
            MessageCenter.Get<WEEKLYRAFFLE_TOKEN_CHANGE>().Dispatch(true);
        }

        // 获得当前已经抽奖次数
        public int GetBoxOpenCount()
        {
            int count = 0;
            int temp = boxIdx;
            while (temp != 0)
            {
                count += temp & 1; // 检查最低位是否为1
                temp >>= 1; // 右移一位
            }

            return count;
        }

        /// 该奖励否领取过
        public bool HasRaffle(int rewardId)
        {
            return (rewardIdx & (1 << rewardId)) != 0;
        }

        /// 保存开启过的宝箱
        private void SaveRaffleRewardIndex(int rewardId)
        {
            rewardIdx |= 1 << rewardId;
        }

        public int TryRaffle(int boxId, out CheckRet ret)
        {
            if (!CheckCanRaffle(boxId, out ret))
            {
                return -1;
            }

            EventWeeklyRaffleReward raffleReward = null;

            HandleRewardPool(out var unDrawnRewards, out var jackPot);

            var boxOpenCount = GetBoxOpenCount();

            if (boxOpenCount < levelNoAppear)
            {
                //前几次必不出大奖
                var unDrawnNoJack = Enumerable.ToList(unDrawnRewards.Where(x => !x.IsJackpot));
                raffleReward = RandomBoxReward(unDrawnNoJack);
            }
            else if (boxOpenCount == levelAppear - 1)
            {
                //必出宝藏
                raffleReward = !HasRaffle(jackPot.Id) ? jackPot : RandomBoxReward(unDrawnRewards);
            }
            else
            {
                // 随机抽取所有未抽到的奖励(可能会抽到大奖)
                raffleReward = RandomBoxReward(unDrawnRewards);
            }

            if (raffleReward == null)
            {
                return -1;
            }

            // 保存宝箱开启
            SaveBoxIndexOpened(boxId);
            // 保存领取奖励
            SaveRaffleRewardIndex(raffleReward.Id);

            // 发奖
            List<(int id, int count)> container = new(raffleReward.Reward.Count);
            BeginRaffleRewards(raffleReward, boxId);

            // 埋点
            var queue = GetBoxOpenCount();
            int final = queue == GetConfigSignDayCount() ? 1 : 0;
            int treasure = raffleReward.Equals(jackPot) ? 1 : 0;
            DataTracker.event_weeklyraffle_raffle.Track(this, ConfDetail.Diff, queue, treasure, final);

            return raffleReward.Id;
        }

        /// 抽奖全部抽完 活动结束
        public bool TryEndActivity()
        {
            if (GetBoxOpenCount() != GetConfigSignDayCount())
            {
                return false;
            }

            Game.Manager.activity.EndImmediate(this, false);
            return true;
        }

        private void BeginRaffleRewards(EventWeeklyRaffleReward rConfig, int boxId)
        {
            var levelRate = 0;
            if (BoardViewWrapper.IsMainBoard())
            {
                levelRate = Game.Manager.mergeLevelMan.GetCurrentLevelRate();
            }

            var rewardMan = Game.Manager.rewardMan;
            var rewardDataList = PoolMapping.PoolMappingAccess.Take(out List<RewardCommitData> rewardList);

            foreach (var r in rConfig.Reward)
            {
                var (cfgID, cfgCount, param) = r.ConvertToInt3();
                var (id, count) = rewardMan.CalcDynamicReward(cfgID, cfgCount, levelRate, 0, param);
                var reward = Game.Manager.rewardMan.BeginReward(id, count, ReasonString.weekly_raffle_draw);
                rewardList.Add(reward);
            }

            if (rConfig.Reward.Count > 0)
            {
                // 发事件 通知抽奖结束 告诉表现层奖励
                MessageCenter.Get<WEEKLYRAFFLE_RAFFLE_END>().Dispatch(rewardDataList, boxId, rConfig.Id);
            }
            else
            {
                rewardDataList.Free();
            }
        }

        /// <summary>
        /// 奖池处理
        /// </summary>
        /// <param name="unDrawnRewards">没有抽取的奖励 包含大奖</param>
        /// <param name="jackPot">大奖</param>
        public void HandleRewardPool(out List<EventWeeklyRaffleReward> unDrawnRewards,
            out EventWeeklyRaffleReward jackPot)
        {
            var rewards = GetRaffleRewards();
            unDrawnRewards = new List<EventWeeklyRaffleReward>();
            jackPot = null;
            foreach (var reward in rewards)
            {
                if (reward.IsJackpot)
                {
                    jackPot = reward;
                }

                if (!HasRaffle(reward.Id))
                {
                    unDrawnRewards.Add(reward);
                }
            }
        }

        /// 根据权重随机一个宝箱
        private EventWeeklyRaffleReward RandomBoxReward(List<EventWeeklyRaffleReward> listR)
        {
            if (listR.Count == 0)
            {
                return null;
            }

            var totalWeight = 0;

            foreach (var reward in listR)
            {
                var w = reward.Weight;
                totalWeight += w;
            }

            var roll = Random.Range(1, totalWeight + 1);

            var weightSum = 0;
            foreach (var box in listR)
            {
                weightSum += box.Weight;
                if (weightSum >= roll)
                {
                    return box;
                }
            }

            return null;
        }
        #endregion

        #region 购买/补签
        // 抽奖日是否可以补签所有
        public bool CheckCanBuyRaffleDay(out CheckRet ret)
        {
            if (!CheckIsRaffleDay())
            {
                ret = CheckRet.InvalidDate;
                return false;
            }

            if (GetSignCount() >= GetConfigSignDayCount())
            {
                // 已经签到次数 >= 配置的签到次数 不需要补签
                ret = CheckRet.NoNeedRefill;
                return false;
            }

            ret = CheckRet.Success;
            return true;
        }

        // 补签某1天
        public void TryBuy(int day, out CheckRet ret)
        {
            if (!CheckSignInEnable(day, out ret))
            {
                return;
            }

            // 计算需要多少钻石
            var gemCount = CalBuyGem(1);
            if (!Game.Manager.coinMan.CanUseCoin(CoinType.Gem, gemCount))
            {
                ret = CheckRet.GemNotEnough;
                return;
            }
            Game.Manager.coinMan.UseCoin(CoinType.Gem, gemCount, ReasonString.weekly_raffle_reward)
            .OnSuccess(() =>
            {
                SaveSignIdx(day);
                MessageCenter.Get<WEEKLYRAFFLE_REFILL>().Dispatch(day, false);
                // 埋点
                DataTracker.event_weeklyraffle_tokenget.Track(this, day + 1, 1, 1, ConfDetail.Diff);
            })
            .FromActivity(Lite)
            .Execute();
        }

        // 补签所有
        public void TryBuyAll(out CheckRet ret)
        {
            if (!CheckCanBuyRaffleDay(out ret))
            {
                return;
            }

            var dayCount = GetConfigSignDayCount() - GetSignCount();
            var gemCount = CalBuyGem(dayCount);
            if (!Game.Manager.coinMan.CanUseCoin(CoinType.Gem, gemCount))
            {
                ret = CheckRet.GemNotEnough;
                return;
            }
            Game.Manager.coinMan.UseCoin(CoinType.Gem, gemCount, ReasonString.weekly_raffle_reward)
            .OnSuccess(() =>
            {
                for (var i = 0; i < GetConfigSignDayCount(); i++)
                {
                    if (CheckSignInEnable(i, out var _))
                    {
                        SaveSignIdx(i);
                        // 埋点
                        DataTracker.event_weeklyraffle_tokenget.Track(this, i + 1, 1, 1, ConfDetail.Diff);
                    }
                }
                MessageCenter.Get<WEEKLYRAFFLE_REFILL>().Dispatch(-1, true);
            })
            .FromActivity(Lite)
            .Execute();
        }

        public int CalBuyGem(int count)
        {
            var reFillCost = ConfDetail.RefillCost;

            return reFillCost * count;
        }
        #endregion

        public bool CheckIsShowRedPoint()
        {
            return CheckIsRaffleDay() && TokenNum > 0;
        }

        public override void Open()
        {
            MainPopUp.Popup(custom_: false);
        }

        private bool CheckIsShowEntry()
        {
            return CheckIsRaffleDay();
        }

        // 主棋盘入口显示
        public string BoardEntryAsset()
        {
            MainPopUp.visual.AssetMap.TryGetValue("boardEntry", out var key);
            return key;
        }
    }

    // meta 入口显示
    public class WeeklyRaffleEntry : ListActivity.IEntrySetup
    {
        public ListActivity.Entry Entry => entry;
        private readonly ListActivity.Entry entry;
        private readonly ActivityWeeklyRaffle activity;

        public WeeklyRaffleEntry(ListActivity.Entry ent, ActivityWeeklyRaffle act)
        {
            (entry, activity) = (ent, act);
            var showRedPoint = activity.CheckIsShowRedPoint();
            ent.dot.SetActive(showRedPoint);
            ent.dotCount.gameObject.SetActive(showRedPoint);
            ent.dotCount.SetRedPoint(activity.TokenNum);
        }

        public override void Clear(ListActivity.Entry e_)
        {
        }

        public override string TextCD(long diff_)
        {
            // 借助每秒刷新 刷红点显示
            var showRedPoint = activity.CheckIsShowRedPoint();
            entry.dot.SetActive(showRedPoint);
            if (showRedPoint)
            {
                entry.dotCount.SetRedPoint(activity.TokenNum);
            }

            return UIUtility.CountDownFormat(diff_);
        }
    }
}
