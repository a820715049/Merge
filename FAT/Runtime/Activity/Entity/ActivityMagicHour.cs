using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using EL;
using FAT.Merge;
using System.Text;
using System.Diagnostics;

namespace FAT {
    public class ActivityMagicHour : ActivityLike, IBoardEntry, IActivityOrderHandler, IActivityOrderGenerator {
        public EventWishing confD;
        public override bool Valid => confD != null;
        public UIResAlt Res { get; } = new(UIConfig.UINoticeMagicHour);
        public PopupActivity Popup { get; internal set; }

        // 等待激活的星想事成slot
        private Dictionary<int, (int reqNum, int realDffy)> waitingMagicHourSlots = new();

        public ActivityMagicHour() { }


        public ActivityMagicHour(ActivityLite lite_) {
            Lite = lite_;
            confD = GetEventWishing(lite_.Param);
            if (confD != null && Visual.Setup(confD.EventTheme, Res)) {
                Popup = new(this, Visual, Res, false);
                SetupTheme();
            }
        }

        public void SetupTheme() {
            var map = new VisualMap(Visual.Theme.AssetInfo);
            map.TryReplace("boardEntry", "event_magichour_default:EntryMagicHour.prefab");
            map = new VisualMap(Visual.Theme.TextInfo);
            map.TryReplace("mainTitle", "#SysComDesc809");
            map.TryReplace("desc1", "#SysComDesc810");
            map.TryCopy("mainTitle", "entryTitle");
        }

        public override void SaveSetup(ActivityInstance data_) {
            // var any = data_.AnyState;
        }

        public override void LoadSetup(ActivityInstance data_) {
            // var any = data_.AnyState;
        }

        public override (long, long) SetupTS(long sTS_, long eTS_) {
            if (sTS_ > 0) return (sTS_, eTS_);
            var sts = Game.TimestampNow();
            var ets = Math.Min(sts + confD.EventTime, Lite.EndTS);
            return (sts, ets);
        }

        public override void WhenActive(bool new_) {
            if (new_) {
                Game.Manager.screenPopup.Queue(Popup);
            }
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            popup_.TryQueue(Popup, state_);
        }

        public override void Open() => Open(Res);

        public string BoardEntryAsset() {
            Visual.Theme.AssetInfo.TryGetValue("boardEntry", out var s);
            return s;
        }

        #region order

        public static string GetOrderThemeRes(int eventId, int paramId)
        {
            if (paramId == 0)
            {
                var cfg = GetOneEventTimeByFilter(x => x.Id == eventId && x.EventType == EventType.Wishing);
                paramId = cfg?.EventParam ?? 0;
            }
            if (paramId == 0)
            {
                DebugEx.Warning($"failed to find theme for {eventId} {paramId}");
                return string.Empty;
            }
            var cfgDetail = GetEventWishing(paramId);
            return cfgDetail?.OrderTheme;
        }

        private bool OrderWillRemove(OrderData order)
        {
            return order.State == OrderState.Rewarded ||
                order.State == OrderState.Expired ||
                (order as IOrderData).IsExpired;
        }

        private bool MakeWish(int slotId, int reqNum, int realDffy)
        {
            if (!waitingMagicHourSlots.ContainsKey(slotId))
            {
                waitingMagicHourSlots.Add(slotId, (reqNum, realDffy));
                return true;
            }
            return false;
        }
        private void CancelWish(int slotId) { if (waitingMagicHourSlots.ContainsKey(slotId)) waitingMagicHourSlots.Remove(slotId); }
        private void ResolveWish(int slotId, out (int reqNum, int realDffy) info) { if (waitingMagicHourSlots.TryGetValue(slotId, out info)) waitingMagicHourSlots.Remove(slotId); }

        bool IActivityOrderHandler.OnPostUpdate(OrderData order, IOrderHelper helper, MergeWorldTracer tracer)
        {
            if (order.OrderType == (int)OrderType.MagicHour)
            {
                if (OrderWillRemove(order))
                {
                    CancelWish(order.Id);
                }
            }
            else if (confD.RandomerId.TryGetValue(order.Id, out var magicHourSlotId))
            {
                // 符合条件的slot完成时 允许关联的slot触发星想事成订单
                if (order.State == OrderState.Rewarded)
                {
                    var realDffy = (order as IOrderData).ActDifficulty;
                    if (MakeWish(magicHourSlotId, order.Requires.Count, realDffy))
                    {
                        helper.ImmediateSlotRequests.Add(magicHourSlotId);
                    }
                }
            }
            return false;
        }

        bool IActivityOrderGenerator.TryGeneratePassiveOrder(OrderRandomer cfg, IOrderHelper helper, MergeWorldTracer tracer, Func<OrderRandomer, OrderData> builder, out OrderData order)
        {
            order = null;
            ResolveWish(cfg.Id, out var info);
            if (info.reqNum <= 0 || info.reqNum > 2)
                return false;
            // 棋盘没有空闲棋子不能触发星想事成订单
            if (tracer.world.activeBoard.emptyGridCount < 1)
                return false;

            order = CreateMagicHourOrder(cfg, tracer, info.reqNum, info.realDffy);
            if (order == null)
                return false;

            order.OrderType = (int)OrderType.MagicHour;
            order.Record.OrderType = order.OrderType;
            var any = order.Record.Extra;
            any.Add(ToRecord((int)OrderParamType.EventId, Id));
            any.Add(ToRecord((int)OrderParamType.EventParam, Param));

            // 星想事成订单不进存档
            order.FallbackItemId = confD.DefaultOutput;
            order.RewardDffyRange = (confD.DiffRange[0], confD.DiffRange[1]);
            order.MagicHourTimeDurationMilli = confD.DeadLine * 1000;

            DebugEx.Info($"ActivityMagicHour::TryGeneratePassiveOrder MagicHourOrder {Id} {order.Id} {confD.DeadLine}");
            return true;
        }

        private OrderData CreateMagicHourOrder(OrderRandomer slotCfg, MergeWorldTracer tracer, int reqNum, int orderDffy)
        {
            // 最终候选棋子 难度距离 / id
            using var _1 = PoolMapping.PoolMappingAccess.Borrow(out List<(int dist, int id)> candidateItemList);
            // 本活动正在使用的棋子
            using var _2 = PoolMapping.PoolMappingAccess.Borrow(out HashSet<int> usingItemCache);
            // 其他随机订单棋子涉及的 源头/前置 链条
            using var _3 = PoolMapping.PoolMappingAccess.Borrow(out HashSet<int> relatedCatCache);
            // 其他随机订单需要的棋子的链条 key: cid, value: levelmask
            using var _4 = PoolMapping.PoolMappingAccess.Borrow(out Dictionary<int, uint> catLvlMaskCache);
            // 当前订单列表
            using var _5 = PoolMapping.PoolMappingAccess.Borrow<List<IOrderData>>(out var orders);

            var recycleCfg = GetEventWishingRecycle(slotCfg.Id);
            var (diffMin, diffMax) = (recycleCfg?.DifferenceLeft ?? 0, recycleCfg?.DifferenceRight ?? 0);
            diffMin = (int)(diffMin * orderDffy / 100f);
            diffMax = (int)(diffMax * orderDffy / 100f);
            var diffMid = (diffMin + diffMax) / 2;
            DebugEx.Info($"ActivityMagicHour::TryGeneratePassiveOrder MagicHourOrder recycle orderDffy={orderDffy} diffRange=({diffMin}, {diffMax}) diffMid={diffMid}");

            // 整理当前订单需求
            BoardViewWrapper.FillBoardOrder(orders, (int)OrderProviderTypeMask.Random);
            foreach (var order in orders)
            {
                if (order.OrderType == (int)OrderType.MagicHour)
                    RecordMagicHourOrderItem(order, usingItemCache);
                else
                    RecordRandomOrderItem(order, catLvlMaskCache, relatedCatCache);
            }

            var boardItemCache = tracer.GetCurrentActiveBoardItemCount();
            foreach (var kv in boardItemCache)
            {
                var itemId = kv.Key;
                // 忽略星想事成订单正在要的棋子
                if (usingItemCache.Contains(itemId))
                    continue;
                // 忽略非回收棋子
                var cfg = Env.Instance.GetItemMergeConfig(itemId);
                if (cfg == null || !cfg.IsRecycle)
                    continue;
                Game.Manager.mergeItemMan.GetItemCategoryIdAndLevel(itemId, out var cid, out var level);
                // 忽略订单的关联链条
                if (relatedCatCache.Contains(cid))
                    continue;
                // 忽略等级不高于订单需求的棋子
                if (catLvlMaskCache.TryGetValue(cid, out var levelmask))
                {
                    if (levelmask >= (1u << level))
                        continue;
                }
                Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(itemId, out var _, out var real);
                // 难度超过max的棋子不可能选中
                if (real > diffMax)
                    continue;
                candidateItemList.Add((real, itemId));
            }

            if (candidateItemList.Count < 2 && reqNum == 2 || candidateItemList.Count == 0 && reqNum == 1)
                return null;
            DebugPrint(candidateItemList, reqNum, "all");
            // 订单需求
            using var _reqIdList = PoolMapping.PoolMappingAccess.Borrow<List<int>>(out var requireItemId);
            if (reqNum == 2)
            {
                CalcOrderRequiresTwo(candidateItemList, diffMid, requireItemId);
            }
            else
            {
                CalcOrderRequires(candidateItemList, diffMid, requireItemId);
            }
            var data = OrderUtility.MakeOrder_Init(null, OrderProviderType.Random, slotCfg.Id, 0, 0);
            OrderUtility.MakeOrder_Require(data, requireItemId);
            OrderUtility.MakeOrder_Record(data);

            var payDffy = 0;
            var realDffy = OrderUtility.CalcRealDifficultyForRequires(requireItemId);
            // 随机订单需要记录pay难度(付出难度)
            data.Record.Extra.Add(RecordStateHelper.ToRecord((int)OrderParamType.PayDifficulty, payDffy));
            // 随机订单需要记录act难度(实际难度)
            data.Record.Extra.Add(RecordStateHelper.ToRecord((int)OrderParamType.ActDifficulty, realDffy));

            return data;
        }

        private void CalcOrderRequiresTwo(List<(int dffy, int id)> candList, int diffMid, List<int> requireItemId)
        {
            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<(int dist, int id_a, int id_b)>> (out var groupList);
            for (var i = 0; i < candList.Count; i++)
            {
                var a = candList[i];
                for (var j = i + 1; j < candList.Count; j++)
                {
                    var b = candList[j];
                    groupList.Add((Math.Abs(a.dffy + b.dffy - diffMid), a.id, b.id));
                }
            }
            // 按距离排序
            groupList.Sort((a, b) =>
            {
                var diff = a.dist - b.dist;
                if (diff == 0)
                {
                    if (a.id_a == b.id_a)
                        return a.id_b - b.id_b;
                    return a.id_a - b.id_a;
                }
                return diff;
            });

#if UNITY_EDITOR
            using var __ = PoolMapping.PoolMappingAccess.Borrow<StringBuilder>(out var sb);
            sb.Clear();
            sb.Append($"[magichour] [wish] all require={2}");
            sb.AppendLine();
            foreach (var (_dist, _id_a, _id_b) in groupList)
            {
                sb.Append($"({_dist}, {_id_a}, {_id_b}) ");
            }
            DebugEx.Info($"{sb}");
#endif

            // 找出最佳N项作为候选范围
            var lastDist = groupList[0].dist;
            var candCount = 0;
            for (var i = 0; i < groupList.Count; i++)
            {
                var (d, _, _) = groupList[i];
                if (lastDist == d)
                {
                    // 后续棋子权重相同 则继续扩大范围
                    ++candCount;
                    continue;
                }
                else
                {
                    // 后续棋子不是最佳选择 则停止
                    break;
                }
            }
            var idx = UnityEngine.Random.Range(0, candCount);
            var (dist, id_a, id_b) = groupList[idx];
            DebugEx.Info($"[magichour] [wish] roll {idx}, dist={dist} id_a={id_a} id_b={id_b}");
            requireItemId.Add(id_a);
            requireItemId.Add(id_b);
        }

        private void CalcOrderRequires(List<(int dffy, int id)> candList, int diffMid, List<int> requireItemId)
        {
            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<(int dist, int id)>> (out var groupList);
            for (var i = 0; i < candList.Count; i++)
            {
                var (_dffy, _id) = candList[i];
                groupList.Add((Math.Abs(_dffy - diffMid), _id));
            }
            groupList.Sort((a, b) => a.dist - b.dist);

            // 找出最佳N项作为候选范围
            var lastDist = groupList[0].dist;
            var candCount = 0;
            for (var i = 0; i < groupList.Count; i++)
            {
                var (d, _) = groupList[i];
                if (lastDist == d)
                {
                    // 后续棋子权重相同 则继续扩大范围
                    ++candCount;
                    continue;
                }
                else
                {
                    // 后续棋子不是最佳选择 则停止
                    break;
                }
            }
            var idx = UnityEngine.Random.Range(0, candCount);
            var (dist, id) = groupList[idx];
            DebugEx.Info($"[magichour] [wish] roll {idx}, dist={dist} id={id}");
            requireItemId.Add(id);
        }

        private void DebugPrint(List<(int dffy, int id)> candList, int reqNum, string tag)
        {
#if UNITY_EDITOR
            using var _ = PoolMapping.PoolMappingAccess.Borrow<StringBuilder>(out var sb);
            sb.Clear();
            sb.Append($"[magichour] [wish] <{tag}> require={reqNum}");
            sb.AppendLine();
            foreach (var (dist, id) in candList)
            {
                sb.Append($"({dist}, {id})");
            }
            DebugEx.Info($"{sb}");
#endif
        }

        // 记录星想事成订单正在要的棋子
        private void RecordMagicHourOrderItem(IOrderData order, HashSet<int> cache)
        {
            foreach (var req in order.Requires)
            {
                cache.Add(req.Id);
            }
        }

        // 记录其他随机订单的 链条/棋子等级/关联链条
        private void RecordRandomOrderItem(IOrderData order, Dictionary<int, uint> catLvlMaskCache_, HashSet<int> relatedCatCache_)
        {
            foreach (var req in order.Requires)
            {
                var itemId = req.Id;
                Game.Manager.mergeItemMan.GetItemCategoryIdAndLevel(itemId, out var cid, out var level);

                // 记录等级
                if (catLvlMaskCache_.TryGetValue(cid, out var mask))
                    catLvlMaskCache_[cid] = mask | (1u << level);
                else
                    catLvlMaskCache_.Add(cid, 1u << level);

                // 记录关联链条
                var order_cat = GetOrderCategory(cid);
                if (order_cat != null)
                {
                    relatedCatCache_.Add(order_cat.OriginGraphId);
                    foreach (var c in order_cat.CareGraphId)
                    {
                        relatedCatCache_.Add(c);
                    }
                }
            }
        }

        #endregion
    }
}