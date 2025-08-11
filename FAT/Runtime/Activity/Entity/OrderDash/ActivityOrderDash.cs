
using fat.gamekitdata;
using fat.rawdata;
using fat.conf;
using static FAT.RecordStateHelper;
using System;
using FAT.Merge;
using static fat.conf.Data;
using System.Collections.Generic;
using System.Linq;
using FAT;
using DG.Tweening;

namespace FAT
{
    using static UILayer;
    using static EL.MessageCenter;
    using static EL.PoolMapping;

    public partial class UIConfig {
        public static UIResource UIActivityOrderDash = new("UIActivityOrderDash.prefab", AboveStatus, "event_orderdash_default");
    }

    public partial class ReasonString {
        public static ReasonString orderdash_reward = new(nameof(orderdash_reward));
    }

    public class ActivityOrderDash : ActivityLike, IActivityOrderHandler, IActivityOrderGenerator
    {
        public override bool Valid => confD != null;
        public int boardId => confD.BoardId;
        public string orderItemRes => confD.OrderTheme;

        internal EventOrderDash confD;
        public readonly List<Config.RewardConfig> reward = new();
        public override ActivityVisual Visual => VisualMain.visual;
        public VisualPopup VisualMain { get; } = new(UIConfig.UIActivityOrderDash);

        public int visualIndex;
        public int orderIndex;
        public int orderTotal;
        private int orderStartTime;
        public bool start;
        internal Ref<List<RewardCommitData>> cache;

        public static string GetOrderThemeRes(int eventId, int paramId)
        {
            if (paramId == 0) {
                var cfg = GetOneEventTimeByFilter(x => x.Id == eventId && x.EventType == EventType.OrderDash);
                paramId = cfg?.EventParam ?? 0;
            }
            if (paramId == 0) {
                EL.DebugEx.Warning($"failed to find theme for {eventId} {paramId}");
                return string.Empty;
            }
            var cfgDetail = GetEventOrderDash(paramId);
            return "event_orderdash_default:OrderItem_OrderDash.prefab";
            // return cfgDetail?.OrderTheme;
        }

        public (int eventId, int paramId, int startTime, int duration) StartNextOrder(int realDifficulty)
        {
            var (life, method, _) = confD.OrderTime.ConvertToInt3();
            var lifeTime = Game.Manager.rewardMan.CalcDynamicOrderLifeTime(method, life, realDifficulty);
            lifeTime = Math.Min(lifeTime, (int)Countdown);
            orderStartTime = (int)Game.Instance.GetTimestampSeconds();
            return (Id, Param, orderStartTime, lifeTime);
        }

        public void OnOrderPassed(OrderData order_, bool isWin)
        {
            DataTracker.OrderDashComplete(this, order_, isWin);
            if (!isWin) {
                Game.Manager.activity.EndImmediate(this, false);
                return;
            }
            ++orderIndex;
            orderStartTime = 0;
            DOVirtual.DelayedCall(1.2f, () => {
                Open();
                Get<MSG.ACTIVITY_REFRESH>().Dispatch(this);
            });
            if (orderIndex >= confD.OrderList.Count) {
                DataTracker.OrderDashReward(this);
                var rMan = Game.Manager.rewardMan;
                cache = PoolMappingAccess.Take(out List<RewardCommitData> list);
                foreach(var r in reward) {
                    var data = rMan.BeginReward(r.Id, r.Count, ReasonString.orderdash_reward);
                    list.Add(data);
                }
                Get<MSG.ACTIVITY_SUCCESS>().Dispatch(this);
                Game.Manager.activity.EndImmediate(this, false);
                Open();
            }
        }

        public bool CheckSlotReady(int slotId)
        {
            if (orderStartTime > 0)
            {
                return false;
            }
            var list = confD.OrderList;
            if (orderIndex < 0 || orderIndex >= list.Count) {
                Game.Manager.activity.EndImmediate(this, false);
                return false;
            }
            return slotId == list[orderIndex];
        }

        public ActivityOrderDash(ActivityLite lite_) {
            Lite = lite_;
            confD = GetEventOrderDash(lite_.Param);
            SetupDetail();
            SetupTheme();
        }

        public void SetupDetail() {
            orderTotal = confD.OrderList.Count;
            reward.AddRange(confD.Reward.Select(p => new Config.RewardConfig() { Id = p.Key, Count = p.Value }));
        }

        public void SetupTheme() {
            VisualMain.Setup(confD.EventTheme, this);
            var map = VisualMain.visual.TextMap;
            map.TryReplace("prizeP", "#SysComDesc936");
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            orderIndex = ReadInt(0, any);
            orderStartTime = ReadInt(1, any);
            start = ReadBool(2, any);
            visualIndex = ReadInt(3, any);
            
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, orderIndex));
            any.Add(ToRecord(1, orderStartTime));
            any.Add(ToRecord(2, start));
            any.Add(ToRecord(3, visualIndex));
        }

        public override void SetupClear()
        {
            base.SetupClear();
            confD = null;
        }

        public override void Open() {
            Open(VisualMain.res);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            VisualMain.Popup(popup_, state_, limit_:1);
        }

        public override void WhenActive(bool new_) {
            if (!new_) return;
            VisualMain.Popup();
        }

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

            order.OrderType = (int)OrderType.OrderDash;
            order.Record.OrderType = order.OrderType;
            // 开始下一个限时订单
            var ret = StartNextOrder((order as IOrderData).CalcRealDifficulty());
            var any = order.Record.Extra;
            any.Add(ToRecord((int)OrderParamType.EventId, ret.eventId));
            any.Add(ToRecord((int)OrderParamType.EventParam, ret.paramId));
            any.Add(ToRecord((int)OrderParamType.StartTimeSec, ret.startTime));
            any.Add(ToRecord((int)OrderParamType.DurationSec, ret.duration));
            EL.DebugEx.Info($"ActivityFlashOrder::TryGeneratePassiveOrder FlashOrder {ret.eventId} {order.Id} {ret.duration}");
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
                EL.DebugEx.Info($"OrderProviderRandom::_RefreshOrderListImp ----> remove completed order {order.Id}");
                OnOrderPassed(order, true);
            }
            else if (order.State == OrderState.Expired || (order as IOrderData).IsExpired)
            {
                OnOrderPassed(order, false);
            }
            // 不改变order 始终返回false
            return false;
        }
    }
}

public partial class DataTracker {
    public static void OrderDashComplete(ActivityOrderDash acti_, OrderData order_, bool result_) {
        var data = BorrowTrackObject();
        FillActivity(data, acti_);
        var index = acti_.orderIndex;
        var order = (IOrderData)order_;
        data["order_id"] = order.Id;
        data["pay_difficulty"] = order.PayDifficulty;
        data["difficulty"]= order.ActDifficulty;
        data["order_require"]= order_start.Require(order);
        data["order_sequence"]= index + 1;
        data["is_final"]= index == acti_.orderTotal - 1;
        var name = result_ ? "event_orderdash_complete" : "event_orderdash_fail";
        TrackObject(data, name);
    }

    public static void OrderDashReward(ActivityOrderDash acti_) {
        var data = BorrowTrackObject();
        FillActivity(data, acti_);
        TrackObject(data, "event_orderdash_reward");
    }
}