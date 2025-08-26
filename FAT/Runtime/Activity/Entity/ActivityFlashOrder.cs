/*
 * @Author: qun.chao
 * @Description: 限时订单
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/lc1trxl4l7r6o7ig
 * @Date: 2024-01-23 11:40:05
 */
using EL;
using fat.gamekitdata;
using fat.rawdata;
using fat.conf;
using static FAT.RecordStateHelper;
using System;
using FAT.Merge;

namespace FAT
{
    public class ActivityFlashOrder : ActivityLike, IActivityOrderHandler, IActivityOrderGenerator
    {
        enum ParamKey
        {
            SlotConsumedCount,
            OrderStartTime,
            LastOrderWon,
        }

        public override bool Valid => confD != null;
        public int boardId => confD.BoardId;
        public string orderItemRes => confD.OrderTheme;

        private EventFlashOrder confD;

        private int slotConsumedCount;
        private int orderStartTime;
        private bool lastOrderWon;

        public static string GetOrderThemeRes(int eventId, int paramId)
        {
            if (paramId == 0) {
                var cfg = Data.GetOneEventTimeByFilter(x => x.Id == eventId && x.EventType == EventType.OrderExtra);
                paramId = cfg?.EventParam ?? 0;
            }
            if (paramId == 0) {
                DebugEx.Warning($"failed to find theme for {eventId} {paramId}");
                return string.Empty;
            }
            var cfgDetail = Game.Manager.configMan.GetEventFlashOrderConfig(paramId);
            return cfgDetail?.OrderTheme;
        }

        public (int eventId, int paramId, int startTime, int duration) StartNextOrder(int realDifficulty)
        {
            var (life, method, _) = confD.LifeTime.ConvertToInt3();
            var lifeTime = Game.Manager.rewardMan.CalcDynamicOrderLifeTime(method, life, realDifficulty);
            orderStartTime = (int)Game.Instance.GetTimestampSeconds();
            return (Id, Param, orderStartTime, lifeTime);
        }

        public void OnOrderPassed(bool isWin)
        {
            if (isWin && slotConsumedCount == 0)
            {
                // 初始订单完成则视为success
                MessageCenter.Get<MSG.ACTIVITY_SUCCESS>().Dispatch(this);
            }
            ++slotConsumedCount;
            lastOrderWon = isWin;
            orderStartTime = 0;
        }

        public bool CheckSlotReady(int slotId)
        {
            if (orderStartTime > 0)
            {
                // 订单进行中
                return false;
            }

            if (Game.Instance.GetTimestampSeconds() > endTS - confD.Deadline)
            {
                // 结束前一段时间内不能再刷新订单
                return false;
            }

            if (slotConsumedCount == 0)
            {
                // 是否是初始订单槽位
                return slotId == confD.InitRandomer;
            }

            return _FindNextSlot(lastOrderWon) == slotId;
        }

        private int _FindNextSlot(bool isWin)
        {
            var idx = slotConsumedCount - 1;
            if (isWin)
            {
                return confD.WinRandomer.GetElementEx(idx, ArrayExt.OverflowBehaviour.Default);
            }
            else
            {
                return confD.LoseRandomer.GetElementEx(idx, ArrayExt.OverflowBehaviour.Default);
            }
        }

        public ActivityFlashOrder() { }


        public ActivityFlashOrder(ActivityLite lite_) {
            Lite = lite_;
            confD = Game.Manager.configMan.GetEventFlashOrderConfig(lite_.Param);
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            slotConsumedCount = ReadInt((int)ParamKey.SlotConsumedCount, any);
            orderStartTime = ReadInt((int)ParamKey.OrderStartTime, any);
            lastOrderWon = ReadBool((int)ParamKey.LastOrderWon, any);
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord((int)ParamKey.SlotConsumedCount, slotConsumedCount));
            any.Add(ToRecord((int)ParamKey.OrderStartTime, orderStartTime));
            any.Add(ToRecord((int)ParamKey.LastOrderWon, lastOrderWon));
        }

        public override void SetupClear()
        {
            base.SetupClear();
            confD = null;
        }

        public override void Open() { }

        bool IActivityOrderHandler.IsValidForBoard(int boardId)
        {
            return this.boardId == boardId;
        }

        bool IActivityOrderGenerator.TryGeneratePassiveOrder(OrderRandomer cfg, IOrderHelper helper, MergeWorldTracer tracer, Func<OrderRandomer, OrderData> builder, out OrderData order)
        {
            order = null;
            if (!CheckSlotReady(cfg.Id))
                return false;
            order = builder?.Invoke(cfg);
            if (order == null)
                return false;

            order.OrderType = (int)OrderType.Flash;
            order.Record.OrderType = order.OrderType;
            // 开始下一个限时订单
            var ret = StartNextOrder((order as IOrderData).CalcRealDifficulty());
            var any = order.Record.Extra;
            any.Add(ToRecord((int)OrderParamType.EventId, ret.eventId));
            any.Add(ToRecord((int)OrderParamType.EventParam, ret.paramId));
            any.Add(ToRecord((int)OrderParamType.StartTimeSec, ret.startTime));
            any.Add(ToRecord((int)OrderParamType.DurationSec, ret.duration));
            DebugEx.Info($"ActivityFlashOrder::TryGeneratePassiveOrder FlashOrder {ret.eventId} {order.Id} {ret.duration}");
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
                DebugEx.Info($"OrderProviderRandom::_RefreshOrderListImp ----> remove completed order {order.Id}");
                OnOrderPassed(true);
            }
            else if (order.State == OrderState.Expired || (order as IOrderData).IsExpired)
            {
                OnOrderPassed(false);
            }
            // 不改变order 始终返回false
            return false;
        }
    }
}