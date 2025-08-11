/*
 * @Author: qun.chao
 * @Date: 2024-03-11 12:47:48
 */
using System.Collections.Generic;
using System.Linq;
using fat.gamekitdata;
using FAT.Merge;
using EL;
using fat.rawdata;

namespace FAT
{
    public class OrderGroupProxy
    {
        public HashSet<int> completedOrderSet = new HashSet<int>();
        public Bitmap64 completedOrderBitmap = new Bitmap64(1);
        public long totalFinished => mTotalFinished;
        public OrderData recentApiOrder { get; private set; }

        private Dictionary<OrderProviderType, IOrderProvider> orderProviders = new()
        {
            { OrderProviderType.Common, new OrderProviderCommon() },
            { OrderProviderType.Random, new OrderProviderRandom() },
            { OrderProviderType.Detector, new OrderProviderDetector() },
        };
        private Dictionary<int, IOrderData> mActiveCommonOrderCache = new ();

        private long mTotalFinished = 0;
        private IOrderHelper orderHelper;
        private MergeWorldTracer worldTracer;

        #region 受控订单
        private int orderCtrlRequireNum => Game.Manager.configMan.globalConfig.OrderCtrlNum;
        private List<int> recentActDffyList = new();
        private List<int> recentPayDffyList = new();
        private int recentStrategy;
        #endregion

        #region api订单
        private int orderApiPastNum => Game.Manager.configMan.globalConfig.OrderApiPastNum;
        private List<int> recentApiActDffyList = new();
        private List<int> recentApiPayDffyList = new();
        #endregion

        public void Reset(IOrderHelper helper)
        {
            helper.proxy = this;
            helper.RequireItemTargetCountDict.Clear();
            orderHelper = helper;

            completedOrderSet.Clear();
            completedOrderBitmap.Clear();
            foreach (var p in orderProviders)
            {
                p.Value.Reset();
            }

            mActiveCommonOrderCache.Clear();
            recentActDffyList.Clear();
            recentPayDffyList.Clear();
            recentApiActDffyList.Clear();
            recentApiPayDffyList.Clear();
        }

        public void Update(float dt)
        {
            foreach (var p in orderProviders)
            {
                p.Value.Update();
            }
        }

        // 已经在场上的订单在一些时机需要立即刷新
        // 1. item数量改变 订单进度/是否可提交 需要立即更新
        // 2. 用户等级改变 原先锁定的订单需要立即解锁
        // 3. 活动过期 相关订单需要立即改变状态或结束 (相关的UI单元检测到过期后触发dirty)
        public void SetDirty()
        {
            foreach (var p in orderProviders)
            {
                p.Value.SetDirty();
            }
        }

        public void SetData(OrderGroup data, MergeWorldTracer tracer)
        {
            worldTracer = tracer;

            mTotalFinished = data.TotalFinished;
            completedOrderBitmap.Reset(data.Finished);
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var list))
            {
                completedOrderBitmap.ExtractIds(list);
                foreach (var item in list)
                {
                    completedOrderSet.Add(item);
                }
            }

            _ReplaceOrderItem(data);

            orderProviders[OrderProviderType.Common].Deserialize(data.OrderCommon, tracer, orderHelper, _OnOrderListDirty);
            orderProviders[OrderProviderType.Detector].Deserialize(data.OrderDetector, tracer, orderHelper, _OnOrderListDirty);
            orderProviders[OrderProviderType.Random].Deserialize(data.OrderRandom, tracer, orderHelper, _OnOrderListDirty);

            recentActDffyList.AddRange(data.RecentActDffy);
            recentPayDffyList.AddRange(data.RecentPayDffy);
            recentStrategy = data.RecentStrategy;
            if (data.RecentApiOrder != null)
                recentApiOrder = OrderUtility.MakeOrderByRecord(data.RecentApiOrder);

            recentApiActDffyList.AddRange(data.RecentApiActDffy);
            recentApiPayDffyList.AddRange(data.RecentApiPayDffy);

            // 主动触发订单状态刷新
            SetDirty();
        }

        public void FillData(OrderGroup data)
        {
            data.TotalFinished = mTotalFinished;
            data.Finished.AddRange(completedOrderBitmap.data);
            data.RecentActDffy.AddRange(recentActDffyList);
            data.RecentPayDffy.AddRange(recentPayDffyList);
            data.RecentStrategy = recentStrategy;
            data.RecentApiOrder = recentApiOrder?.Record;
            data.RecentApiActDffy.AddRange(recentApiActDffyList);
            data.RecentApiPayDffy.AddRange(recentApiPayDffyList);

            orderProviders[OrderProviderType.Common].Serialize(data.OrderCommon);
            orderProviders[OrderProviderType.Detector].Serialize(data.OrderDetector);
            orderProviders[OrderProviderType.Random].Serialize(data.OrderRandom);
        }

        // 记录最近完成的API类订单
        public void SetRecentApiOrder(OrderData order)
        {
            recentApiOrder = order;
        }

        // 记录上一次订单受控生成使用的难度策略
        public void SetRecentOrderCtrlStrategy(int st)
        {
            recentStrategy = st;
            _DebugPrintRecentCtrlDffy("PostOrderSpawn");
        }

        public bool GetRecentCtrlDffyInfo(out int totalActDffy, out int totalPayDffy, out int recentCtrlType)
        {
            _DebugPrintRecentCtrlDffy("PreOrderSpawn");
            totalActDffy = 0;
            totalPayDffy = 0;
            recentCtrlType = recentStrategy;
            var recordedCount = recentActDffyList.Count;
            var requiredCount = orderCtrlRequireNum;
            if (recordedCount < requiredCount)
            {
                return false;
            }
            for (var i = 0; i < requiredCount; i++)
            {
                totalActDffy += recentActDffyList[recordedCount - 1 - i];
                totalPayDffy += recentPayDffyList[recordedCount - 1 - i];
            }
            return true;
        }

        public int GetActiveOrderNum()
        {
            return orderHelper.ActiveOrderCount;
        }

        public bool TryFinishOrder(IOrderData order, ICollection<RewardCommitData> rewards)
        {
            var providerType = (OrderProviderType)order.ProviderType;
            var p = orderProviders[providerType];
            var result = p.TryFinishOrder(order.Id, rewards);
            if (result)
            {
                ++mTotalFinished;
                if (providerType == OrderProviderType.Common)
                {
                    completedOrderSet.Add(order.Id);
                    completedOrderBitmap.AddId(order.Id);
                }
                // 尝试刷新Common里特有的计数类订单
                orderProviders[OrderProviderType.Common].SetDirty();
                if (p is OrderProviderRandom opr)
                {
                    if (opr.IsCtrledOrder(order))
                    {
                        // 记录最近完成的受控订单的难度相关参数
                        _AppendRecentDffy(order, recentActDffyList, recentPayDffyList, orderCtrlRequireNum);
                        _DebugPrintRecentCtrlDffy("OrderFinish");
                    }
                    else if (opr.IsApiOrder(order))
                    {
                        // 记录最近完成的配置属性为api的订单的难度相关参数
                        _AppendRecentDffy(order, recentApiActDffyList, recentApiPayDffyList, orderApiPastNum);
                        _DebugPrintRecentApiDffy("OrderFinish");
                    }
                }
            }
            _OnOrderFinished(order);
            return result;
        }

        public int FillActiveOrders(List<IOrderData> container, int mask)
        {
            var count = 0;
            foreach (var p in orderProviders)
            {
                if ((mask & p.Key.ToIntMask()) != 0)
                {
                    count += p.Value.FillActiveOrders(container);
                }
            }
            return count;
        }

        public int FillActiveOrders(List<IOrderData> container, params OrderProviderType[] exceptTypes)
        {
            var count = 0;
            foreach (var p in orderProviders)
            {
                if (!exceptTypes.Contains(p.Key))
                {
                    count += p.Value.FillActiveOrders(container);
                }
            }
            return count;
        }

        public void FillRecentApiOrderDiff(List<int> actList, List<int> payList)
        {
            actList.AddRange(recentApiActDffyList);
            payList.AddRange(recentApiPayDffyList);
        }

        public bool IsOrderCompleted(int id)
        {
            return _IsOrderCompletedInner(id);
        }

        public Dictionary<int, int> GetOrderRequireItemStateCache()
        {
            return orderHelper.RequireItemStateCache;
        }

        public IOrderData GetActiveCommonOrderById(int orderId)
        {
            mActiveCommonOrderCache.TryGetValue(orderId, out var order);
            return order;
        }

        public void ValidateOrderDisplayCache()
        {
            using (ObjectPool<List<IOrderData>>.GlobalPool.AllocStub(out var all))
            {
                FillActiveOrders(all, (int)OrderProviderTypeMask.All);
                orderHelper.OnOrderDisplayChange(all);
            }
        }

        private void _ReplaceOrderItem(OrderGroup data)
        {
            var replaceMap = Game.Manager.configMan.GetItemReplaceMap();
            foreach (var order in data.OrderCommon)
            {
                _ReplaceOrderItemImp(order, replaceMap);
            }
            foreach (var order in data.OrderDetector)
            {
                _ReplaceOrderItemImp(order, replaceMap);
            }
            foreach (var order in data.OrderRandom)
            {
                _ReplaceOrderItemImp(order, replaceMap);
            }
        }

        private void _ReplaceOrderItemImp(OrderRecord order, IDictionary<int, ItemReplace> replaceMap)
        {
            for (var i = 0; i < order.RequireIds.Count; i++)
            {
                if (replaceMap.TryGetValue(order.RequireIds[i], out var replace))
                {
                    order.RequireIds[i] = replace.ReplaceInto;
                    DataTracker.TrackItemReplace(replace.Id, replace.ReplaceInto);
                }
            }
            for (var i = 0; i < order.RewardIds.Count; i++)
            {
                if (replaceMap.TryGetValue(order.RewardIds[i], out var replace))
                {
                    order.RewardIds[i] = replace.ReplaceInto;
                    DataTracker.TrackItemReplace(replace.Id, replace.ReplaceInto);
                }
            }
        }

        private void _OnOrderFinished(IOrderData order)
        {
            Game.Manager.audioMan.TriggerSound("OrderFinish");  //订单完成时播音效
            DataTracker.order_end.Track(order);
            MessageCenter.Get<MSG.GAME_ORDER_COMPLETED>().Dispatch(order);
        }

        private void _OnOrderListDirty(List<IOrderData> changedOrders, List<IOrderData> newlyAddedOrders)
        {
            using (ObjectPool<List<IOrderData>>.GlobalPool.AllocStub(out var all))
            {
                FillActiveOrders(all, (int)OrderProviderTypeMask.All);
                orderHelper.OnOrderListUpdated(all);
                _CacheActiveCommonOrder(all);
            }
            MessageCenter.Get<MSG.GAME_ORDER_CHANGE>().Dispatch(changedOrders, newlyAddedOrders);
        }

        private void _CacheActiveCommonOrder(List<IOrderData> allOrders)
        {
            mActiveCommonOrderCache.Clear();
            foreach (var order in allOrders)
            {
                if (order.ProviderType == (int)OrderProviderType.Common)
                {
                    mActiveCommonOrderCache.Add(order.Id, order);
                }
            }
        }

        private bool _IsOrderCompletedInner(int id)
        {
            return completedOrderSet.Contains(id);
        }

        private void _AppendRecentDffy(IOrderData order, List<int> actList, List<int> payList, int maxCount)
        {
            actList.Add(order.ActDifficulty);
            payList.Add(order.PayDifficulty);
            if (actList.Count > maxCount)
            {
                actList.RemoveRange(0, actList.Count - maxCount);
            }
            if (payList.Count > maxCount)
            {
                payList.RemoveRange(0, payList.Count - maxCount);
            }
        }

        private void _DebugPrintRecentApiDffy(string tag)
        {
#if UNITY_EDITOR
            _DebugPrintRecentDffy($"api {tag}", recentApiActDffyList, recentApiPayDffyList);
#endif
        }

        private void _DebugPrintRecentCtrlDffy(string tag)
        {
#if UNITY_EDITOR
            _DebugPrintRecentDffy($"[ORDERCTRL] {tag}", recentActDffyList, recentPayDffyList);
#endif
        }

        private void _DebugPrintRecentDffy(string tag, List<int> actList, List<int> payList)
        {
#if UNITY_EDITOR
            using (ObjectPool<System.Text.StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                var totalAct = 0;
                var totalPay = 0;
                for (var i = 0; i < actList.Count; i++)
                {
                    totalAct += actList[i];
                    totalPay += payList[i];
                }
                sb.Append($"[ORDERDEBUG] {tag} strategy {(OrderProviderRandom.CtrlDffyType)recentStrategy} count {actList.Count} -> {totalPay}/{totalAct}={totalPay*1.0f/totalAct:F4}");
                sb.AppendLine();
                for (var i = 0; i < actList.Count; i++)
                {
                    sb.Append($"act {actList[i]} pay {payList[i]}");
                    sb.AppendLine();
                }
                DebugEx.Info(sb.ToString());
            }
#endif
        }
    }
}