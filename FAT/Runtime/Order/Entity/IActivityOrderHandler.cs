/*
 * @Author: qun.chao
 * @Date: 2024-03-13 18:35:29
 */
using System;
using System.Collections.Generic;
using FAT.Merge;
using fat.rawdata;

namespace FAT
{
    public interface IActivityOrderHandler
    {
        // 是否对特定棋盘生效
        bool IsValidForBoard(int boardId) => true;

        // 刷新订单前调用 | 订单如发生改变 返回true
        bool OnPreUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer) => false;
        // 刷新订单后调用 | 订单如发生改变 返回true
        bool OnPostUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer) => false;

        void HandlerCollected() { }

        // 收集detector类型订单需要排除的物品
        void CollectDetectorExcludeItemMap(List<IDictionary<int, int>> container) { }
    }

    public interface IActivityOrderGenerator
    {
        // 被动订单slot 由活动决策是否生成订单 | 成功生成返回true
        bool TryGeneratePassiveOrder(OrderRandomer cfg, IOrderHelper helper, MergeWorldTracer tracer, Func<OrderRandomer, OrderData> builder, out OrderData order);
    }
}
