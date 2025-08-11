/**
 * @Author: zhangpengjian
 * @Date: 2024/10/28 18:36:56
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/28 18:36:56
 * Description: 连续限时订单活动实例
 */

using EL;
using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;
using Config;
using Random = UnityEngine.Random;
using Google.Protobuf.Collections;
using FAT.Merge;
using System;
using System.Linq;
using static fat.conf.Data;
using static FAT.ListActivity;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using EL.Resource;

namespace FAT
{
    using static PoolMapping;

    public class ActivityOrderChallenge : ActivityLike, IActivityOrderGenerator, IActivityOrderHandler, IBoardEntry
    {
        #region data
        private int curQuestGroupId;
        private int curLevelIndex;
        private int curGroupStartTs;
        private int curLevelCostDiamond;
        private int challengeCount;
        private int prevIsWon;
        private int allOrderLifetime;
        private int hasGenerateOrder;
        private int outPlayerNum;
        private int curOrderStartTs;
        private int curOrderLiftTime;
        private int isChallenge;
        private int isOrderFinishInMeta;
        #endregion
        #region Theme
        public UIResAlt HelpRes => new(UIConfig.UIOrderChallengeHelp);
        public UIResAlt MatchRes => new(UIConfig.UIOrderChallengeMatch);
        public UIResAlt StartRes => new(UIConfig.UIOrderChallengeStart);
        public UIResAlt VictoryRes => new(UIConfig.UIOrderChallengeVictory);
        public UIResAlt Res => new(UIConfig.UIOrderChallengeMain);
        public PopupActivity PopupStart = new();
        public PopupActivity PopupMatch = new();
        public PopupActivity PopupMain = new();
        public PopupActivity PopupVictory = new();
        public PopupActivity PopupHelp = new();
        public ActivityVisual VisualStart = new(); //活动开启弹窗
        public ActivityVisual VisualMatch { get; } = new(); //匹配
        public ActivityVisual VisualHelp { get; } = new();  //帮助
        public ActivityVisual VisualVictory { get; } = new();  //胜利
        public ActivityVisual VisualMain { get; } = new();  //主界面
        public override ActivityVisual Visual => VisualStart;

        #endregion
        public int boardId => conf.BoardId;
        private EventZeroQuest conf;
        private EventZeroQuestGroup groupConf;
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureZeroQuest);
        private int curLevelOutNum;
        private Entry entry;
        public RewardCommitData rewardCommitData;
        public int finalGetRewardNum;
        public RewardConfig curLevelTotalReward;
        public bool IsWait => conf.IsWait;
        private bool isOrderSuccess;
        private bool isOrderFail;

        public ActivityOrderChallenge(ActivityLite lite_)
        {
            Lite = lite_;
            conf = Game.Manager.configMan.GetEventZeroQuestConfig(lite_.Param);
            MessageCenter.Get<MSG.GAME_COIN_USE>().AddListener(OnCoinChange);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListenerUnique(OnTick);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(OnRandomBoxFinish);

        }

        private void OnTick()
        {
            if (entry != null)
                SetupEntry(entry);
        }

        private void OnCoinChange(CoinChange change)
        {
            if (IsOver())
                return;
            if (change.type != CoinType.Gem)
                return;
            if (isChallenge == 0)
                return;
            curLevelCostDiamond += change.amount;
        }

        public override void SetupFresh()
        {
            RandomBaseGroupId();
            SetupPopup();
            if (Active)
            {
                UIManager.Instance.RegisterIdleAction("ui_order_challenge", 401, () => 
                {
                    if (Active)
                    {
                        StartRes.ActiveR.Open(this);
                    }
                });
            }
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate()
        {
            if (!Valid) yield break;
            foreach(var v in VisualMain.ResEnumerate()) yield return v;
            foreach(var v in VisualMatch.ResEnumerate()) yield return v;
            foreach(var v in VisualHelp.ResEnumerate()) yield return v;
            foreach(var v in VisualStart.ResEnumerate()) yield return v;
            foreach(var v in VisualVictory.ResEnumerate()) yield return v;
        }

        private void RandomBaseGroupId()
        {
            var idx = Random.Range(0, conf.Base.Count);
            groupConf = Game.Manager.configMan.GetEventZeroQuestGroupConfig(conf.Base[idx]);
            if (groupConf == null) return;
            curQuestGroupId = groupConf.Id;
            curLevelTotalReward = groupConf.RewardTotal.ConvertToRewardConfig();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(1, challengeCount));
            any.Add(ToRecord(2, curQuestGroupId));
            any.Add(ToRecord(3, curLevelIndex));
            any.Add(ToRecord(4, prevIsWon));
            any.Add(ToRecord(5, allOrderLifetime));
            any.Add(ToRecord(6, curGroupStartTs));
            any.Add(ToRecord(7, hasGenerateOrder));
            any.Add(ToRecord(8, outPlayerNum));
            any.Add(ToRecord(9, curOrderLiftTime));
            any.Add(ToRecord(10, curOrderStartTs));
            any.Add(ToRecord(11, isChallenge));
            any.Add(ToRecord(12, isOrderFinishInMeta));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            challengeCount = ReadInt(1, any);
            curQuestGroupId = ReadInt(2, any);
            curLevelIndex = ReadInt(3, any);
            prevIsWon = ReadInt(4, any);
            allOrderLifetime = ReadInt(5, any);
            curGroupStartTs = ReadInt(6, any);
            hasGenerateOrder = ReadInt(7, any);
            outPlayerNum = ReadInt(8, any);
            curOrderLiftTime = ReadInt(9, any);
            curOrderStartTs = ReadInt(10, any);
            isChallenge = ReadInt(11, any);
            isOrderFinishInMeta = ReadInt(12, any);
            if (curQuestGroupId != 0)
            {
                groupConf = Game.Manager.configMan.GetEventZeroQuestGroupConfig(curQuestGroupId);
                curLevelTotalReward = groupConf.RewardTotal.ConvertToRewardConfig();
            }
            else
            {
                RandomBaseGroupId();
            }
            SetupPopup();
            if (!IsOver() && !IsChallenge && Active)
            {
                UIManager.Instance.RegisterIdleAction("ui_order_challenge", 401, () => 
                {
                    if (Active)
                    {
                        StartRes.ActiveR.Open(this);
                    }
                });
            }
        }

        private void SetupPopup()
        {
            if (conf == null) return;
            VisualMain.Setup(conf.EventTheme, Res);
            VisualStart.Setup(conf.StartTheme, StartRes);
            VisualMatch.Setup(conf.NpcTheme, MatchRes);
            VisualVictory.Setup(conf.CompleteTheme, VictoryRes);
            VisualHelp.Setup(conf.HelpTheme, HelpRes);
        }

        public override void Open()
        {
            var cd = curOrderStartTs + curOrderLiftTime - Game.TimestampNow();
            if (cd > 0)
            {
                if (IsChallenge)
                    Res.ActiveR.Open(this);
            }
            else
            {
                StartRes.ActiveR.Open(this);
            }
        }

        public void OpenPanel()
        {
            if (curOrderStartTs == 0)
            {
                return;
            }
            if (IsChallenge)
            {
                Res.ActiveR.Open(this);
            }
        }

        public override bool EntryVisible
        {
            get
            {
                return !IsOver();
            }
        }

        public IEntrySetup SetupEntry(Entry e_)
        {
            if (e_.activity != this)
                return null;
            if (entry == null)
                entry = e_;
            if (IsOver() && isOrderFinishInMeta == 1)
            {
                e_.obj.SetActive(false);
                return null;
            }
            e_.cd.gameObject.SetActive(false);
            if (isChallenge == 0)
            {
                e_.img.gameObject.SetActive(true);
                e_.orderCd.gameObject.SetActive(false);
                VisualStart.Theme.AssetInfo.TryGetValue("entryBg", out var entryBg);
                e_.img.SetImage(entryBg);
                UIUtility.CountDownFormat(e_.actCd, Countdown);
            }
            else
            {
                e_.orderCd.gameObject.SetActive(true);
                e_.img.gameObject.SetActive(false);
                var cd = curOrderStartTs + curOrderLiftTime - Game.TimestampNow();
                if (cd > 0)
                    UIUtility.CountDownFormat(e_.orderCd, cd);
                else
                {
                    var active = Game.Manager.mapSceneMan.scene.Active;
                    if (isOrderFinishInMeta == 0 && hasGenerateOrder == 1 && active)
                    {
                        isOrderFail = true;
                        OnOrderFail();
                    }
                    e_.img.gameObject.SetActive(true);
                    e_.orderCd.gameObject.SetActive(false);
                    VisualStart.Theme.AssetInfo.TryGetValue("entryBg", out var entryBg);
                    e_.img.SetImage(entryBg);
                    UIUtility.CountDownFormat(e_.actCd, Countdown);
                }

            }
            return null;
        }

        public bool IsOver()
        {
            var cd = curOrderStartTs + curOrderLiftTime - Game.TimestampNow();
            return challengeCount >= conf.ChallengeNum && cd <= 0;
        }

        public override void WhenEnd()
        {
            var group = 0;
            if (challengeCount == 1)
            {
                group = 0;
            }
            else if (prevIsWon == 1)
            {
                group = 1;
            }
            else
            {
                group = 2;
            }
            DataTracker.event_zero_end.Track(this, challengeCount, group, 0, curLevelIndex + 1, curQuestGroupId);
            MessageCenter.Get<MSG.GAME_COIN_USE>().RemoveListener(OnCoinChange);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(OnTick);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(OnRandomBoxFinish);

        }

        public override void WhenReset()
        {
            MessageCenter.Get<MSG.GAME_COIN_USE>().RemoveListener(OnCoinChange);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(OnTick);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(OnRandomBoxFinish);
        }

        private void OnRandomBoxFinish()
        {
            Game.Instance.StartCoroutineGlobal(CoWaitOrder());
            OnOrderFail();
        }

        public void Challenge()
        {
            if (IsOver())
            {
                //结束活动
                return;
            }
            curLevelIndex = 0;
            outPlayerNum = 0;
            isOrderFinishInMeta = 0;
            var group = 0;
            if (challengeCount != 0)
            {
                if (prevIsWon > 0)
                    group = 1;
                else
                    group = 2;
            }
            challengeCount += 1;
            isChallenge = 1;
            curGroupStartTs = (int)Game.Instance.GetTimestampSeconds();
            curLevelTotalReward = groupConf.RewardTotal.ConvertToRewardConfig();
            DataTracker.event_zero_enter.Track(this, challengeCount, group, curQuestGroupId);
            MessageCenter.Get<MSG.ORDER_CHALLENGE_BEGIN>().Dispatch();
        }

        public void TryOpenVictory()
        {
            if (prevIsWon == 1 && curLevelIndex == groupConf.OrderId.Count)
            {
                VictoryRes.ActiveR.Open();
                Res.ActiveR.Close();
                MessageCenter.Get<MSG.ORDER_CHALLENGE_VICTORY>().Dispatch();
            }
        }

        public void TryOpenStart()
        {
            if (IsOver())
                return;
            StartRes.ActiveR.Open(this);
        }

        #region 活动主界面展示需要的接口
        public int CurLevelIndex => curLevelIndex;

        public bool IsChallenge => isChallenge == 1;

        public string ChallengeShowInfo => $"{CurLevelIndex}/{groupConf.OrderId.Count}";

        public string PlayerShowInfo => $"{FinalLeftNum}/{conf.TotalNum}";

        public int FinalLeftNum => conf.TotalNum - outPlayerNum;

        public int GetFinalLeftNumWhenJump()
        {
            if (curLevelIndex >= groupConf.OrderId.Count)
                return FinalLeftPlayer;
            else
                return conf.TotalNum - outPlayerNum;
        }

        public int FinalLeftPlayer => (int)Math.Round(curLevelTotalReward.Count * 1.0f / finalGetRewardNum);

        public RewardConfig TotalReward => groupConf.RewardTotal.ConvertToRewardConfig();

        public int TotalNum => conf.TotalNum;

        public int LevelCount => groupConf.OrderId.Count;

        public long GetCurrentOrderCD()
        {
            if (curOrderStartTs == 0) return 0;
            if (curOrderLiftTime == 0) return 0;
            return (curOrderStartTs + curOrderLiftTime) - Game.TimestampNow();
        }

        //根据此次淘汰人数 得到展示的淘汰人数
        public int OutPlayerShowNum => CalcOutShowNum(curLevelOutNum);

        public int GetLeftShowNum()
        {
            if (TotalNum - outPlayerNum <= 6)
                return TotalNum - outPlayerNum;
            var idx = curLevelIndex - 1 < 0 ? 0 : curLevelIndex - 1;
            if (groupConf == null) return 0;
            var showNum = groupConf.SaveIcon[idx];
            var left = showNum - OutPlayerShowNum;
            if (left <= 0)
                return 0;
            return left;
        }

        public int GetCurLevelShowNum()
        {
            if (TotalNum - outPlayerNum <= 6)
                return TotalNum - outPlayerNum;
            var idx = curLevelIndex >= groupConf.SaveIcon.Count() ? groupConf.SaveIcon.Count - 1 : curLevelIndex;
            if (idx <= 0)
                idx = 0;
            if (groupConf == null) return 0;
            var showNum = groupConf.SaveIcon[idx];
            return showNum;
        }

        public int GetLastLevelShowNum()
        {
            if (TotalNum - outPlayerNum <= 6)
                return TotalNum - outPlayerNum;
            var idx = curLevelIndex >= groupConf.SaveIcon.Count() ? groupConf.SaveIcon.Count - 1 : curLevelIndex - 1;
            if (idx <= 0)
                idx = 0;
            if (groupConf == null) return 0;
            var showNum = groupConf.SaveIcon[idx];
            return showNum;
        }

        public string BoardEntryAsset()
        {
            VisualStart.Theme.AssetInfo.TryGetValue("boardEntry", out var key);
            return key;
        }

        public bool BoardEntryVisible => !IsOver();
        #endregion

        #region Order
        public static readonly string orderPrefabKey = "bgOrder";
        public static string GetOrderThemeRes(int eventId, int paramId)
        {
            if (paramId == 0)
            {
                var cfg = GetOneEventTimeByFilter(x => x.Id == eventId && x.EventType == fat.rawdata.EventType.ZeroQuest);
                paramId = cfg?.EventParam ?? 0;
            }
            if (paramId == 0)
            {
                DebugEx.Warning($"failed to find theme for {eventId} {paramId}");
                return string.Empty;
            }
            var cfgDetail = GetEventZeroQuest(paramId);
            var theme = GetOneEventThemeByFilter(x => x.Id == cfgDetail.EventTheme);
            if (theme.AssetInfo.TryGetValue(orderPrefabKey, out var assetInfo))
            {
                return assetInfo;
            }
            return string.Empty;
        }

        public bool TryGeneratePassiveOrder(OrderRandomer cfg, IOrderHelper helper, MergeWorldTracer tracer, Func<OrderRandomer, OrderData> builder, out OrderData order)
        {
            order = null;
            if (IsOver() && isChallenge != 1)
            {
                return false;
            }
            if (hasGenerateOrder == 1)
                return false;
            if (isChallenge == 0)
                return false;
            if (groupConf == null)
                return false;
            if (challengeCount <= 0)
                return false;
            if (curLevelIndex >= groupConf.OrderId.Count)
                return false;
            var c = Game.Manager.configMan.GetEventZeroQuestRandomConfig(groupConf.OrderId[curLevelIndex]);
            if (c == null)
                return false;
            var randomCfg = GetOrderRandomer(c.RandomId);
            if (randomCfg == null)
                return false;
// #if UNITY_EDITOR
//             Debug.LogError($"验证订单槽位 随机订单id/零度random表id{c.RandomId}/ {c.Id}");
// #endif
            order = builder?.Invoke(randomCfg);
            if (order == null)
                return false;
            order.OrderType = (int)OrderType.Challenge;
            order.Record.OrderType = order.OrderType;
            // 开始下一个限时订单
            var realDifficulty = (order as IOrderData).CalcRealDifficulty();
            var (life, method, _) = c.LifeTime.ConvertToInt3();
            //策划新增规则：干预订单存在时长
            //订单倒计时 > 活动倒计时 ？ 订单倒计时 = 活动倒计时 
            var lifeTime = Game.Manager.rewardMan.CalcDynamicOrderLifeTime(method, life, realDifficulty);
            allOrderLifetime += lifeTime;
            var diff = Countdown - lifeTime;
            if (diff <= 0)
            {
                lifeTime = (int)(endTS - Game.TimestampNow());
            }
            var any = order.Record.Extra;
            any.Add(ToRecord((int)OrderParamType.EventId, Id));
            any.Add(ToRecord((int)OrderParamType.EventParam, Param));
            any.Add(ToRecord((int)OrderParamType.StartTimeSec, (int)Game.TimestampNow()));
            any.Add(ToRecord((int)OrderParamType.DurationSec, lifeTime));
            hasGenerateOrder = 1;
            curOrderLiftTime = lifeTime;
            curOrderStartTs = (int)Game.TimestampNow();
            return true;
        }

        public bool IsValidForBoard(int boardId)
        {
            return this.boardId == boardId;
        }

        public bool OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if ((order as IOrderData).OrderType != (int)OrderType.Challenge)
                return false;
            if (order.GetValue(OrderParamType.EventId) != Id)
                return false;
            if (IsOver() && isChallenge != 1)
                return false;
            if (order.State == OrderState.Rewarded)
            {
                DebugEx.Info($"OrderProviderRandom::_RefreshOrderListImp ----> remove completed order {order.Id}");
                curLevelIndex += 1;
                var r = groupConf.RewardTotal.ConvertToRewardConfig();
                //最多保留人数=总奖励数/瓜分奖励最少数量
                var lastV = groupConf.TimeUse.Last().Value;
                var s = lastV.IndexOf('=');
                var n = int.Parse(lastV[(s + 1)..]);
                var minPlayer = r.Count / (r.Count * (n * 0.001f));
                //可淘汰人数=总参与人数-最多保留人数
                var canOutPlayer = conf.TotalNum - minPlayer;
                //为计算淘汰率准备参数：难度/时间/耗钻
                var realDifficulty = (order as IOrderData).CalcRealDifficulty();
                var orderStartTs = ReadInt((int)OrderParamType.StartTimeSec, order.Record.Extra);
                var tsDiff = Game.TimestampNow() - orderStartTs;
                var min = (int)Math.Ceiling((double)tsDiff / 60);
                //淘汰%
                var rate1 = CalcRate(conf.Diff, realDifficulty);
                var rate2 = CalcRate(conf.Diamond, curLevelCostDiamond);
                var rate3 = CalcRate(conf.Time, min);
                var rate = rate1 + rate2 + rate3;
                var rateReal = rate * 1.0 / 100;
                //每订单提交淘汰人数=（可淘汰人数-已累计淘汰人数）* 淘汰%
                var outNumEach = (int)Math.Ceiling((canOutPlayer - outPlayerNum) * rateReal);
                curLevelOutNum = outNumEach;
// #if UNITY_EDITOR
//                 DebugEx.Error($"配置总人数{conf.TotalNum}/必须保留人数{minPlayer}/可淘汰人数总计{canOutPlayer}");
//                 DebugEx.Error($"三个维度分别：难度/分钟/耗钻：{realDifficulty}/{min}/{curLevelCostDiamond}");
//                 DebugEx.Error($"总淘汰率{rate} 分别：{rate1}/{rate3}/{rate2}");
//                 DebugEx.Error($"完成此次订单淘汰了：{outNumEach}人");
// #endif
                outPlayerNum += outNumEach;
// #if UNITY_EDITOR
//                 DebugEx.Error($"完成此订单后累计淘汰{outPlayerNum}");
// #endif
                curOrderStartTs = 0;
                var group = 0;
                if (challengeCount == 1)
                {
                    group = 0;
                }
                else if (prevIsWon == 1)
                {
                    group = 1;
                }
                else
                {
                    group = 2;
                }
                isOrderSuccess = true;
                Game.Instance.StartCoroutineGlobal(CoWaitOrder());
                DataTracker.event_zero_order_end.Track(this, challengeCount, group, curLevelIndex, realDifficulty, (int)tsDiff, curLevelCostDiamond, outNumEach, curQuestGroupId);
                curLevelCostDiamond = 0;
                if (curLevelIndex >= groupConf.OrderId.Count)
                {
                    //瓜分大奖
                    var now = (int)Game.Instance.GetTimestampSeconds();
                    var cost = now - curGroupStartTs;
                    var v = (int)Math.Ceiling((cost * 1.0 / allOrderLifetime) * 100);
// #if UNITY_EDITOR
//                     DebugEx.Error("=================");
//                     DebugEx.Error($"花费时间是{cost}");
//                     DebugEx.Error($"现在时间是{now}");
//                     DebugEx.Error($"所有订单时间是{allOrderLifetime}");
//                     DebugEx.Error($"比值是{v}");
// #endif
                    //策划修改了字段含义 拿到的是一个百分比 需要进一步计算
                    var rateTimeUse = CalcRate(groupConf.TimeUse, v);
                    var num = (int)Math.Ceiling(r.Count * (rateTimeUse * 0.001f));
// #if UNITY_EDITOR
//                     DebugEx.Error($"瓜分到奖励：{num}");
//                     DebugEx.Error("=================");
// #endif
                    var id = r.Id;
                    rewardCommitData = Game.Manager.rewardMan.BeginReward(id, num, ReasonString.order_challenge);
                    finalGetRewardNum = num;
                    prevIsWon = 1;
                    curGroupStartTs = 0;
                    isChallenge = 0;
                    DataTracker.event_zero_success.Track(this, challengeCount, group, num, curLevelIndex, curQuestGroupId, FinalLeftPlayer, groupConf.OrderId.Count, groupConf.OrderId.Count, 1, true, challengeCount);
                    if (challengeCount >= conf.ChallengeNum)
                    {
                        DataTracker.event_zero_end.Track(this, challengeCount, group, 1, groupConf.OrderId.Count, curQuestGroupId);
                    }
                    else
                    {
                        var idx = Random.Range(0, conf.Win.Count);
                        groupConf = Game.Manager.configMan.GetEventZeroQuestGroupConfig(conf.Win[idx]);
                        if (groupConf != null)
                            curQuestGroupId = groupConf.Id;
                    }
                }
                else
                {
                    DataTracker.event_zero_success.Track(this, challengeCount, group, 0, curLevelIndex, curQuestGroupId, 0, curLevelIndex, groupConf.OrderId.Count, 1, false, challengeCount);
                }
                hasGenerateOrder = 0;
            }
            else if (order.State == OrderState.Expired || (order as IOrderData).IsExpired)
            {
                var realDifficulty = (order as IOrderData).CalcRealDifficulty();
                var group = 0;
                if (challengeCount == 1)
                {
                    group = 0;
                }
                else if (prevIsWon == 1)
                {
                    group = 1;
                }
                else
                {
                    group = 2;
                }
                curOrderStartTs = 0;
                prevIsWon = 0;
                isChallenge = 0;
                curGroupStartTs = 0;
                hasGenerateOrder = 0;
                DataTracker.event_zero_order_fail.Track(this, challengeCount, group, curLevelIndex + 1, realDifficulty, curQuestGroupId);
                if (challengeCount >= conf.ChallengeNum)
                {
                    DataTracker.event_zero_end.Track(this, challengeCount, group, 1, curLevelIndex + 1, curQuestGroupId);
                }
                var idx = Random.Range(0, conf.Lose.Count);
                groupConf = Game.Manager.configMan.GetEventZeroQuestGroupConfig(conf.Lose[idx]);
                if (groupConf != null)
                    curQuestGroupId = groupConf.Id;
                MessageCenter.Get<MSG.ORDER_CHALLENGE_EXPIRE>().Dispatch();
                if (isOrderFinishInMeta != 1)
                {
                    isOrderFail = true;
                    OnOrderFail();
                }
            }
            // 不改变order 始终返回false
            return false;
        }

        private void OnOrderFail()
        {
            if (!Game.Manager.specialRewardMan.CheckCanClaimSpecialReward())
            {
                if (isOrderFail)
                {
                    isOrderFail = false;
                    var active = Game.Manager.mapSceneMan.scene.Active;
                    if (isOrderFinishInMeta == 0 && hasGenerateOrder == 1 && active)
                        GameProcedure.SceneToMerge();
                    isOrderFinishInMeta = 1;
                    Res.ActiveR.Open(this, true, true,true);
                }
            }
        }

        private IEnumerator CoWaitOrder()
        {
            if (!Game.Manager.specialRewardMan.IsBusy())
            {
                if (isOrderSuccess)
                {
                    isOrderSuccess = false;
                    UIManager.Instance.Block(true);
                    yield return new WaitForSeconds(1.5f);
                    Res.ActiveR.Open(this, true);
                    UIManager.Instance.Block(false);
                }
            }
        }

        public int CalcOutShowNum(int num)
        {
            var n = 0;
            //左开右闭
            foreach (var (left, t) in conf.Out)
            {
                var s = t.IndexOf('=');
                var right = int.Parse(t[..s]);
                var showNum = int.Parse(t[(s + 1)..]);
                if (num > left && num <= right)
                {
                    n = showNum;
                }

            }
            return n;
        }

        private int CalcRate(MapField<int, string> map, int num)
        {
            var r = 0;
            //left=0 全闭区间；反之 左开右闭
            foreach (var (left, t) in map)
            {
                var s = t.IndexOf('=');
                var right = int.Parse(t[..s]);
                var rate = int.Parse(t[(s + 1)..]);
                if (left == 0)
                {
                    if (num >= left && num <= right)
                    {
                        r = rate;
                    }
                }
                else
                {
                    if (num > left && num <= right)
                    {
                        r = rate;
                    }
                }
            }
            return r;
        }

        #endregion
    }
}
