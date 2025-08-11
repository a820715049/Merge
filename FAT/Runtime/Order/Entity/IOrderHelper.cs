/*
 * @Author: qun.chao
 * @Date: 2023-10-24 16:18:43
 */
using System.Collections.Generic;

namespace FAT
{
    public interface IOrderHelper
    {
        OrderGroupProxy proxy { get; set; }

        int GetBoardLevel();
        bool IsOrderCompleted(int id);
        long GetTotalFinished();

        bool CheckCond_Level(int level)
        {
            return GetBoardLevel() >= level;
        }
        bool CheckCond_OrderCompleted(IEnumerable<int> orders)
        {
            foreach (var order in orders)
            {
                if (!IsOrderCompleted(order))
                {
                    return false;
                }
            }
            return true;
        }
        bool CheckCond_ItemUnlocked(IEnumerable<int> items)
        {
            foreach (var item in items)
            {
                if (!Game.Manager.handbookMan.IsItemUnlocked(item))
                {
                    return false;
                }
            }
            return true;
        }

        bool CheckStateByConditionGroup(int level_on,
                                        int level_off,
                                        IList<int> order_on,
                                        IList<int> order_off,
                                        IList<int> item_on,
                                        IList<int> item_off)
        {
            if (!CheckCond_Level(level_on))
                return false;
            if (level_off > 0 && CheckCond_Level(level_off))
                return false;
            if (!CheckCond_OrderCompleted(order_on))
                return false;
            if (order_off.Count > 0 && CheckCond_OrderCompleted(order_off))
                return false;
            if (!CheckCond_ItemUnlocked(item_on))
                return false;
            if (item_off.Count > 0 && CheckCond_ItemUnlocked(item_off))
                return false;
            return true;
        }

        #region 即时订单生成请求 | 活动可能触发某些订单立即生成
        List<int> ImmediateSlotRequests { get; }
        #endregion

        #region 所有订单涉及到的棋子 及 是否可用于完成订单

        // 用于棋盘内订单背景状态显示
        Dictionary<int, int> RequireItemStateCache { get; }
        void OnOrderDisplayChange(List<IOrderData> orders)
        {
            var cache = RequireItemStateCache;
            cache.Clear();
            foreach (var order in orders)
            {
                // 显示以后才统计
                if (!order.Displayed || order.State == OrderState.NotStart || order.State == OrderState.PreShow)
                    continue;
                if (order.State == OrderState.Finished)
                {
                    // 可完成 state=1
                    foreach (var item in order.Requires)
                    {
                        cache[item.Id] = 1;
                    }
                }
                else
                {
                    // 至少达到 state=0
                    foreach (var item in order.Requires)
                    {
                        if (!cache.ContainsKey(item.Id))
                        {
                            cache.Add(item.Id, 0);
                        }
                    }
                }
            }
        }

        // 活跃状态的订单数量
        int ActiveOrderCount { get; set; }
        void OnOrderListUpdated(List<IOrderData> orders)
        {
            var count = 0;
            foreach (var order in orders)
            {
                if (order.State == OrderState.NotStart || order.State == OrderState.PreShow)
                    continue;
                ++count;
            }
            ActiveOrderCount = count;
        }
        #endregion

        #region item cache

        // 所有已解锁且未结束的订单汇总的需求数量
        Dictionary<int, int> RequireItemTargetCountDict { get; }
        // 统计订单npc使用情况
        Dictionary<int, int> NpcInUseCountDict { get; }

        public void ClearCache()
        {
            RequireItemStateCache.Clear();
            RequireItemTargetCountDict.Clear();
            NpcInUseCountDict.Clear();
        }

        // 更新订单里需求的物品和npc的引用计数
        // 1 添加 / -1 扣除 / 0 不变
        void UpdateOrderStatisticInfo(OrderData order, int flag_item, int flag_npc)
        {
            if (flag_npc != 0)
            {
                _UpdateDictSum(NpcInUseCountDict, order.RoleId, flag_npc);
            }
            if (flag_item != 0)
            {
                foreach (var req in order.Requires)
                {
                    _UpdateDictSum(RequireItemTargetCountDict, req.Id, req.TargetCount * flag_item);
                }
                // // debug
                // using (EL.ObjectPool<System.Text.StringBuilder>.GlobalPool.AllocStub(out var sb))
                // {
                //     sb.Clear();
                //     foreach (var info in RequireItemCountDict)
                //     {
                //         sb.Append($" {info.Key}-{info.Value}");
                //     }
                //     EL.DebugEx.Error($"require item update : {sb}");
                // }
            }
        }

        private void _UpdateDictSum(Dictionary<int, int> dict, int key, int change)
        {
            if (dict.TryGetValue(key, out var cur))
            {
                cur += change;
                if (cur <= 0)
                {
                    dict.Remove(key);
                }
                else
                {
                    dict[key] = cur;
                }
            }
            else if (change > 0)
            {
                dict[key] = change;
            }
        }
        #endregion
    }
}