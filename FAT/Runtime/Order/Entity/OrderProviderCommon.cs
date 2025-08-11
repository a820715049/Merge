/*
 * @Author: qun.chao
 * @Date: 2023-10-26 18:20:44
 */
using System;
using System.Collections.Generic;
using Config;
using EL;
using FAT.Merge;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT
{
    public class OrderProviderCommon : IOrderProvider
    {
        class DataContainer
        {
            public List<OrderCommon> orderConfigList = new List<OrderCommon>();
            public List<OrderData> activeOrderList = new List<OrderData>();
        }

        private Action<List<IOrderData>, List<IOrderData>> mOnOrderListDirty;

        private List<IOrderData> mCacheChangedList = new List<IOrderData>();
        private List<IOrderData> mCacheNewlyAddedList = new List<IOrderData>();
        private HashSet<int> mCacheHashSet = new HashSet<int>();
        private DataContainer mDataHolder = new DataContainer();
        private MergeWorldTracer mTracer;
        private IOrderHelper mHelper;
        private bool mDirty;

        private int mSpawnLimitCount = 1;
        private float mSpawnIntervalSec => Game.Manager.configMan.globalConfig.OrderEnterDelay / 1000f;
        private float mLastOrderSpawnTime = 0f;

        void IOrderProvider.Reset()
        {
            mCacheChangedList.Clear();
            mCacheNewlyAddedList.Clear();
            mCacheHashSet.Clear();
            mDataHolder.orderConfigList.Clear();
            mDataHolder.activeOrderList.Clear();

            mDirty = false;
            mOnOrderListDirty = null;
        }

        void IOrderProvider.Deserialize(IList<OrderRecord> records, MergeWorldTracer _tracer, IOrderHelper _helper, Action<List<IOrderData>, List<IOrderData>> onDirty)
        {
            mTracer = _tracer;
            mHelper = _helper;
            mOnOrderListDirty = onDirty;
            var bid = _tracer.world.activeBoard.boardId;
            mDataHolder.orderConfigList.Clear();
            mDataHolder.orderConfigList.AddRange(Game.Manager.configMan.GetOrderCommonConfigByFilter(x => x.BoardId == bid));
            mDataHolder.activeOrderList.Clear();
            foreach (var rec in records)
            {
                mDataHolder.activeOrderList.Add(OrderUtility.MakeOrderByRecord(rec));
            }
        }

        void IOrderProvider.Serialize(IList<OrderRecord> records)
        {
            foreach (var order in mDataHolder.activeOrderList)
            {
                records.Add(order.Record);
            }
        }

        int IOrderProvider.FillActiveOrders(List<IOrderData> container)
        {
            container?.AddRange(mDataHolder.activeOrderList);
            return mDataHolder.activeOrderList.Count;
        }

        bool IOrderProvider.TryFinishOrder(int id, ICollection<RewardCommitData> rewards)
        {
            var order = _FindOrderById(id);
            if (order == null)
            {
                DebugEx.Warning($"OrderProviderCommon::TryFinishOrder ----> no such order {id}");
                return false;
            }
            if (OrderUtility.TryFinishOrder(order, mTracer, mHelper, ReasonString.order, rewards))
            {
                _RefreshOrderSpawnTime();
                DebugEx.Warning($"OrderProviderCommon::TryFinishOrder ----> finish order {id} succeed");
                (this as IOrderProvider).SetDirty();
                return true;
            }
            else
            {
                DebugEx.Warning($"OrderProviderCommon::TryFinishOrder ----> finish order {id} failed");
            }
            return false;
        }

        void IOrderProvider.SetDirty()
        {
            mDirty = true;
        }

        void IOrderProvider.Update()
        {
            if (mDirty || !_IsWaitingForNextOrder())
            {
                mDirty = false;
                _RefreshOrderList();
            }
        }

        private bool _IsWaitingForNextOrder()
        {
            return mLastOrderSpawnTime + mSpawnIntervalSec > UnityEngine.Time.timeSinceLevelLoad;
        }

        private void _RefreshOrderSpawnTime()
        {
            mLastOrderSpawnTime = UnityEngine.Time.timeSinceLevelLoad;
        }

        private OrderData _FindOrderById(int id)
        {
            foreach (var order in mDataHolder.activeOrderList)
            {
                if (order.Id == id)
                {
                    return order;
                }
            }
            return null;
        }

        private void _RefreshOrderList()
        {
            if (_RefreshOrderListImp(mDataHolder.activeOrderList, mCacheChangedList, mCacheNewlyAddedList))
            {
                mOnOrderListDirty?.Invoke(mCacheChangedList, mCacheNewlyAddedList);
            }
        }

        private bool _RefreshOrderListImp(List<OrderData> activeContainer, List<IOrderData> changedContainer, List<IOrderData> newlyAddedContainer)
        {
            var max_order_num = 50;
            var list_active = activeContainer;
            var list_changed = changedContainer;
            var list_new = newlyAddedContainer;

            list_changed.Clear();
            list_new.Clear();

            for (int i = list_active.Count - 1; i >= 0; --i)
            {
                var order = list_active[i];
                if (order.State == OrderState.Rewarded)
                {
                    DebugEx.Info($"CommonOrderProvider::_RefreshOrderListImp ----> remove completed order {order.Id}");
                    list_active.RemoveAt(i);
                    list_changed.Add(order);
                }
            }

            _UpdateAllOrders(list_active, list_changed);

            if (list_active.Count >= max_order_num ||
                _IsWaitingForNextOrder())
            {
                return list_changed.Count > 0;
            }
            _RefreshOrderSpawnTime();

            var cacheForActive = mCacheHashSet;
            cacheForActive.Clear();
            foreach (var order in list_active)
            {
                cacheForActive.Add(order.Id);
            }

            // 尝试生成新订单
            foreach (var cfg in mDataHolder.orderConfigList)
            {
                if (list_active.Count >= max_order_num)
                {
                    break;
                }
                if (cacheForActive.Contains(cfg.Id))
                {
                    continue;
                }
                if (mHelper.IsOrderCompleted(cfg.Id))
                {
                    continue;
                }
                if (!mHelper.CheckStateByConditionGroup(cfg.ActiveLevel,
                                                        cfg.ShutdownLevel,
                                                        cfg.ActiveOrderId,
                                                        cfg.ShutdownOrderId,
                                                        cfg.ActiveItemId,
                                                        cfg.ShutdownItemId))
                {
                    continue;
                }
                var newOrder = OrderUtility.MakeOrderByConfig(mHelper, OrderProviderType.Common, cfg.Id, cfg.RoleId, cfg.DisplayLevel,
                                                                OrderUtility.CalcRealDifficultyForRequires(cfg.RequireItemId),
                                                                cfg.RequireItemId, cfg.Reward);
                if (newOrder.Requires.Count < 1 && cfg.RequireOrderCount > 0)
                {
                    // 计数类订单
                    newOrder.OrderType = (int)OrderType.Counting;
                    newOrder.Record.OrderType = newOrder.OrderType;
                    var any = newOrder.Record.Extra;
                    var from = mHelper.GetTotalFinished();
                    any.Add(RecordStateHelper.ToRecord((int)OrderParamType.OrderCountFrom, (int)from));
                    any.Add(RecordStateHelper.ToRecord((int)OrderParamType.OrderCountRequire, cfg.RequireOrderCount));
                    any.Add(RecordStateHelper.ToRecord((int)OrderParamType.OrderCountTotal, (int)from));
                }

                _UpdateOrder(newOrder);
                cacheForActive.Add(newOrder.Id);
                list_new.Add(newOrder);
                list_active.Add(newOrder);
                DataTracker.order_start.Track(newOrder);

                if (list_new.Count >= mSpawnLimitCount) break;
            }
            return list_changed.Count > 0 || list_new.Count > 0;
        }

        private void _UpdateAllOrders(List<OrderData> activeOrders, List<IOrderData> changedOrders)
        {
            foreach (var order in activeOrders)
            {
                if (_UpdateOrder(order))
                {
                    changedOrders.Add(order);
                }
            }
        }

        private bool _UpdateOrder(OrderData data)
        {
            return OrderUtility.UpdateOrderStatus(data, mTracer, mHelper);
        }
    }
}