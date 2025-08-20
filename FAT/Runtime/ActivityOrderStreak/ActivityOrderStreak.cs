/**
 * @Author: zhangpengjian
 * @Date: 2025/6/30 14:10:29
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/6/30 14:10:29
 * Description: 连续订单活动
 */

using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using FAT.Merge;
using UnityEngine;
using static FAT.RecordStateHelper;

namespace FAT
{
    public class ActivityOrderStreak : ActivityLike, IActivityOrderHandler, IActivityOrderGenerator
    {
        public EventOrderStreak conf;
        public EventOrderStreakDetail confD;
        public override bool Valid => confD != null;
        public int boardId => conf.BoardId;
        public List<List<(int, int)>> OrderList => _orderList;
        public int OrderIdx => _orderIdx;
        private List<List<(int, int)>> _orderList = new();
        private List<RewardCommitData> _rewardList = new();
        private int _orderIdx = 0;
        public bool Complete => _orderIdx >= confD.OrderList.Count;
        private int _hasOrder = 0;
        private int _grpId = 0;
        private bool isOrderSuccess = false;

        public VisualPopup VisualRes { get; } = new(UIConfig.UIActivityOrderStreakMain);
        public VisualRes VisualResHelp { get; } = new(UIConfig.UIActivityOrderStreakHelp);
        public VisualPopup VisualConvert { get; } = new(UIConfig.UIActivityOrderStreakConvert);

        public override ActivityVisual Visual => VisualRes.visual;

        public ActivityOrderStreak(ActivityLite lite_)
        {
            Lite = lite_;
            conf = fat.conf.Data.GetEventOrderStreak(lite_.Param);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(OnRandomBoxFinish);
        }

        private void SetupTheme()
        {
            VisualConvert.Setup(conf.EndTheme, this, active_: false);
            VisualRes.Setup(conf.EventTheme, this, active_: false);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (!EntryVisible) return;
            popup_.TryQueue(VisualRes.popup, state_);
        }

        public override bool EntryVisible => OrderIdx < confD.OrderList.Count;

        public override void SetupFresh()
        {
            _grpId = Game.Manager.userGradeMan.GetTargetConfigDataId(conf.Detail);
            confD = fat.conf.Data.GetEventOrderStreakDetail(_grpId);
            SetupTheme();
            SetupOrder();
            Game.Manager.screenPopup.TryQueue(VisualRes.popup, PopupType.Login);
        }

        private void SetupOrder()
        {
            var provider = (OrderProviderRandom)Game.Manager.mainOrderMan.GetProvider(OrderProviderType.Random);
            if (provider == null)
            {
                return;
            }
            if (_orderList.Count == 0)
            {
                for (int i = 0; i < confD.OrderList.Count; i++)
                {
                    var c = fat.conf.Data.GetOrderRandomer(confD.OrderList[i]);
                    var order = provider.MakeOrder(c);
                    if (order != null)
                    {
                        var requireList = new List<(int, int)>();
                        for (int j = 0; j < 2; j++)
                        {
                            if (j < order.Requires.Count && order.Requires[j] != null)
                            {
                                requireList.Add((order.Requires[j].Id, order.Requires[j].TargetCount));
                            }
                            else
                            {
                                requireList.Add((0, 0));
                            }
                        }
                        _orderList.Add(requireList);
                    }
                }
                Game.Manager.archiveMan.SendImmediately(true);
            }
        }

        public static string GetOrderThemeRes(int eventId, int paramId)
        {
            if (paramId == 0)
            {
                var cfg = fat.conf.Data.GetOneEventTimeByFilter(x => x.Id == eventId && x.EventType == fat.rawdata.EventType.OrderStreak);
                paramId = cfg?.EventParam ?? 0;
            }
            if (paramId == 0)
            {
                DebugEx.Warning($"failed to find theme for {eventId} {paramId}");
                return string.Empty;
            }
            var cfgDetail = fat.conf.Data.GetEventOrderStreak(paramId);
            return cfgDetail?.OrderTheme;
        }

        public override void Open()
        {
            VisualRes.res.ActiveR.Open(this);
        }

        public override void WhenReset()
        {
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(OnRandomBoxFinish);
        }

        public override void WhenEnd()
        {
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(OnRandomBoxFinish);
            Game.Manager.screenPopup.TryQueue(VisualConvert.popup, PopupType.Login);
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            _orderIdx = ReadInt(0, any);
            _hasOrder = ReadInt(1, any);
            _grpId = ReadInt(2, any);
            confD = fat.conf.Data.GetEventOrderStreakDetail(_grpId);
            var i = 3;
            _orderList.Clear();
            for (int j = 0; j < confD.OrderList.Count; j++)
            {
                var requireList = new List<(int, int)>();
                for (int k = 0; k < 2; k++)
                {
                    var id = ReadInt(i++, any);
                    var count = ReadInt(i++, any);
                    requireList.Add((id, count));
                }
                _orderList.Add(requireList);
            }
            SetupTheme();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, _orderIdx));
            any.Add(ToRecord(1, _hasOrder));
            any.Add(ToRecord(2, _grpId));
            var i = 3;
            for (int j = 0; j < _orderList.Count; j++)
            {
                for (int k = 0; k < _orderList[j].Count; k++)
                {
                    any.Add(ToRecord(i++, _orderList[j][k].Item1));
                    any.Add(ToRecord(i++, _orderList[j][k].Item2));
                }
            }
        }

        bool IActivityOrderHandler.IsValidForBoard(int boardId)
        {
            return this.boardId == boardId;
        }

        bool IActivityOrderGenerator.TryGeneratePassiveOrder(OrderRandomer cfg, IOrderHelper helper, MergeWorldTracer tracer, Func<OrderRandomer, OrderData> builder, out OrderData order)
        {
            order = null;
            if (cfg.Id != confD.ActSlot)
                return false;
            if (Complete)
                return false;
            if (_hasOrder == 1)
                return false;

            var items = _orderList[_orderIdx];
            // 从items中提取物品ID，创建正确的整数列表
            var itemIds = new List<int>();
            foreach (var (id, count) in items)
            {
                for (int i = 0; i < count; i++)
                {
                    itemIds.Add(id);
                }
            }
            var cfgRandomer = fat.conf.Data.GetOrderRandomer(confD.OrderList[_orderIdx]);
            var difficulty = OrderUtility.CalcRealDifficultyForRequires(itemIds);
            var reward = Game.Manager.mergeItemMan.GetOrderRewardConfig(Game.Manager.userGradeMan.GetTargetConfigDataId(cfgRandomer.RewardGrpId)).Reward;
            order = OrderUtility.MakeOrderByConfig(helper, OrderProviderType.Random, cfgRandomer.Id, cfgRandomer.RoleId, 0, difficulty, itemIds, reward);
            order.ConfRandomer = cfgRandomer;
            order.OrderType = (int)OrderType.Streak;
            order.Record.OrderType = order.OrderType;
            var any = order.Record.Extra;
            any.Add(ToRecord((int)OrderParamType.EventId, Id));
            any.Add(ToRecord((int)OrderParamType.EventParam, Param));
            any.Add(ToRecord((int)OrderParamType.StartTimeSec, (int)Game.TimestampNow()));
            any.Add(ToRecord((int)OrderParamType.DurationSec, (int)Countdown));
            _hasOrder = 1;
            return true;
        }

        bool IActivityOrderHandler.OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if (!(order as IOrderData).IsFlash)
                return false;
            if (order.GetValue(OrderParamType.EventId) != Id)
                return false;
            if (order.State == OrderState.Rewarded)
            {
                var cfgRandomer = fat.conf.Data.GetOrderRandomer(confD.OrderList[_orderIdx]);
                DebugEx.Info($"OrderProviderRandom::_RefreshOrderListImp ----> remove completed order {order.Id}");
                _orderIdx++;
                if (_orderIdx >= confD.OrderList.Count)
                {
                    _rewardList.Clear();
                    foreach (var item in confD.Reward)
                    {
                        var r = item.ConvertToRewardConfig();
                        _rewardList.Add(Game.Manager.rewardMan.BeginReward(r.Id, r.Count, ReasonString.order_streak));
                    }
                }
                DataTracker.event_orderstreak_complete.Track(this, OrderList.Count, confD.Diff, cfgRandomer.Id, _orderIdx, _orderIdx == confD.OrderList.Count);
                _hasOrder = 0;
                isOrderSuccess = true;
                Game.Instance.StartCoroutineGlobal(CoWaitOrder());
            }
            else if (order.State == OrderState.Expired || (order as IOrderData).IsExpired)
            {
                Game.Manager.activity.EndImmediate(this, false);
            }
            // 不改变order 始终返回false
            return false;
        }

        private void OnRandomBoxFinish()
        {
            Game.Instance.StartCoroutineGlobal(CoWaitOrder());
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
                    if (_rewardList.Count > 0)
                    {
                        VisualRes.res.ActiveR.Open(this, true, _rewardList);
                    }
                    else
                    {
                        VisualRes.res.ActiveR.Open(this, true);
                    }
                    UIManager.Instance.Block(false);
                }
            }
        }
    }
}
