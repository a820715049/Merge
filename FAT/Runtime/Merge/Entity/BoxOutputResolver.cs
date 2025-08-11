/*
 * @Author: qun.chao
 * @doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/xugqxb402k708t2a#AER3u
 * @Date: 2024-07-03 16:09:12
 */
using System.Collections.Generic;
using System.Text;
using fat.rawdata;
using FAT.Merge;
using EL;

namespace FAT
{
    /*
    计算订单最需要的item
    链条里的item等级从0开始
    */
    public class BoxOutputResolver
    {
        struct ItemInfo
        {
            public int result;
            public int itemId;
            public int cid;
            public int realDffy;
        }

        private IOrderHelper helper;
        private MergeWorldTracer tracer;
        private (int min, int max) realDffyRange;

        // 被关注的common订单
        private HashSet<int> relateCommonSet = new();
        // 被关注的random订单
        private HashSet<int> relateRandomSet = new();
        // 随机订单链条池
        private Dictionary<int, OrderCategory> cfgOrderCategoryDict = new();
        // 订单范围替换
        private List<IOrderData> candidateOrderOverride = new();

        private readonly int levelMod = 1000;
        // 候选item集合
        // key: cid * 1000 + level, value: item_id
        private Dictionary<int, int> candidateItemDict = new();
        // 候选item 与 订单item 并集
        // key: cid * 1000 + level, value: item_id
        private Dictionary<int, int> catLvlItemCache = new();
        // 订单item的付出难度
        private Dictionary<int, int> requireItemPayDffyDict = new();
        // 某链条贡献的难度 | cache
        private Dictionary<int, int> categoryBoardDffyCache = new();

        // 订单item所在的链条集合 key: cid, value: levelmask
        private Dictionary<int, uint> requireCategoryLevelDict = new();
        // 候选item所在的链条集合 <关注的链条> key: cid, value: levelmask
        private Dictionary<int, uint> candidateCategoryLevelDict = new();
        // 最终候选item所在的链条集合 key: cid, value: levelmask
        private Dictionary<int, uint> releaseCategoryLevelDict = new();
        // 最佳结果列表
        private List<ItemInfo> bestItemList = new();

        /*
        魔法盒子 负数和0当成相同档次 可能有并列最优解 需要在其中随机一个
        三选一盒子 最优解不会并列
        */
        private bool isSpecialBox;
        private int curBoardId;

        private readonly string logTag = "[boxdebug]";

        public void Reset()
        {
            ResetConfig();
            ResetCache();
        }

        public void BindEnv(MergeWorldTracer _tracer, IOrderHelper _helper, int min, int max)
        {
            tracer = _tracer;
            helper = _helper;
            realDffyRange = (min, max);
        }

        /// <param name="possibleOutputs">记录平级可选项</param> // 或许需求是指某一阶段的候选
        public int CalcSpecialBox(List<int> possibleOutputs)
        {
            isSpecialBox = true;
            CommonProcess();
            if (possibleOutputs != null)
                FillPossibleOutputs(possibleOutputs, true);
            var itemId = ChooseResultForSpecialBox();
            var diff = Game.Manager.mergeItemDifficultyMan;
            DebugEx.FormatInfo("棋子难度 itemId {0}, difficulty {1}", itemId, diff.GetItemAvgDifficulty(itemId));
            ResetCache();
            return itemId;
        }

        public void CalcChoiceBox(List<int> container, int choiceCount)
        {
            isSpecialBox = false;
            CommonProcess();
            ChooseResultForChoiceBox(container, choiceCount);
            ResetCache();
        }

        // 星想事成活动 选中某个随机订单 尝试产出该订单需要的物品
        public bool CalcMagicHourOutput(out IOrderData targetOrder, out int targetId)
        {
            targetOrder = null;
            targetId = -1;

            // 所有随机订单配置
            var randomCfgs = fat.conf.Data.GetOrderRandomerMap();
            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<IOrderData>>(out var randomOrders);
            helper.proxy.FillActiveOrders(randomOrders, (int)OrderProviderTypeMask.Random);
            for (var i = randomOrders.Count - 1; i >= 0; i--)
            {
                var order = randomOrders[i];
                if (!randomCfgs.TryGetValue(order.Id, out var cfg) || !cfg.IsWishing)
                {
                    randomOrders.RemoveAt(i);
                    continue;
                }

                if (!order.Displayed ||
                    order.IsMagicHour ||
                    order.State == OrderState.Finished)
                {
                    randomOrders.RemoveAt(i);
                }
            }
            // 没有符合条件的随机订单
            if (randomOrders.Count < 1)
            {
                return false;
            }
            CalcMagicHourOutputPerOrder(randomOrders, out targetOrder, out targetId);
            return targetOrder != null && targetId > 0;
        }

        private void CalcMagicHourOutputPerOrder(IList<IOrderData> orders, out IOrderData targetOrder, out int targetId)
        {
            targetOrder = null;
            targetId = -1;
            foreach (var order in orders)
            {
                DebugEx.Info($"{logTag} magichour try order {order.Id}");

                // 选一个作为星想事成的目标订单
                candidateOrderOverride.Clear();
                candidateOrderOverride.Add(order);

                // 借用魔盒逻辑计算
                isSpecialBox = true;
                CommonProcess();
                if (requireCategoryLevelDict.Count > 0 && releaseCategoryLevelDict.Count > 0)
                {
                    // 需求池不为空且最终棋子备选池不为空 允许产出
                    targetOrder = order;
                    targetId = ChooseResultForSpecialBox();
                    ResetCache();
                    break;
                }
                ResetCache();
            }
        }

        private void CommonProcess()
        {
            // 确保配置存在
            EnsureBoardOrderConfig();
            // 候选池
            PrepareCandItemPool();
            // 订单需求池
            PrepareOrderRequireItemPool();
            // 根据订单需求过滤候选池等级
            ClampCandItemLevelToMaxRequireLevel();
            // 构建最佳候选列表
            GenerateOrderedResult();
        }

        private void ResetConfig()
        {
            curBoardId = -1;
            relateCommonSet.Clear();
            relateRandomSet.Clear();
            cfgOrderCategoryDict.Clear();
        }

        private void ResetCache()
        {
            candidateOrderOverride.Clear();
            candidateItemDict.Clear();
            catLvlItemCache.Clear();
            requireItemPayDffyDict.Clear();
            categoryBoardDffyCache.Clear();
            requireCategoryLevelDict.Clear();
            candidateCategoryLevelDict.Clear();
            releaseCategoryLevelDict.Clear();
            bestItemList.Clear();
        }

        private void EnsureBoardOrderConfig()
        {
            var boardId = tracer.world.activeBoard.boardId;
            if (boardId == curBoardId)
                return;
            ResetConfig();
            curBoardId = boardId;

            var orderCats = Game.Manager.configMan.GetOrderCategoryConfigByFilter(x => x.BoardId == boardId);
            foreach (var oc in orderCats)
            {
                cfgOrderCategoryDict.Add(oc.Id, oc);
            }
            var commons = Game.Manager.configMan.GetOrderCommonConfigByFilter(x => x.BoardId == boardId).ToList();
            commons.ForEach(x => { if (x.IsRelateBox) relateCommonSet.Add(x.Id); });
            var randoms = Game.Manager.configMan.GetOrderRandomerConfigByFilter(x => x.BoardId == boardId).ToList();
            randoms.ForEach(x => { if (x.IsRelateBox) relateRandomSet.Add(x.Id); });
        }

        private void IndexingItemByCatAndLevel(int itemId, int cid, int level)
        {
            catLvlItemCache[cid * levelMod + level] = itemId;
        }

        private void PrepareCandItemPool()
        {
            var dffyMan = Game.Manager.mergeItemDifficultyMan;
            var mergeItemMan = Game.Manager.mergeItemMan;

            foreach (var kv in cfgOrderCategoryDict)
            {
                var cat = kv.Value;
                // 类似订单逻辑，忽略未激活的链条
                if (!helper.CheckStateByConditionGroup(cat.ActiveLevel,
                                                        cat.ShutdownLevel,
                                                        cat.ActiveOrderId,
                                                        cat.ShutdownOrderId,
                                                        cat.ActiveItemId,
                                                        cat.ShutdownItemId))
                {
                    continue;
                }

                // 遍历链条，取实际难度在预设区间内的item作为候选
                var config = mergeItemMan.GetCategoryConfig(cat.Id);
                var level = 0;
                foreach (var item in config.Progress)
                {
                    if (dffyMan.TryGetItemDifficulty(item, out _, out var dff))
                    {
                        if (dff >= realDffyRange.min && dff <= realDffyRange.max)
                        {
                            // 缓存到cat-level表
                            IndexingItemByCatAndLevel(item, cat.Id, level);
                            // 记录item
                            candidateItemDict.Add(cat.Id * levelMod + level, item);
                            // 记录链条
                            UpdateMaskDict(candidateCategoryLevelDict, cat.Id, level);
                        }
                    }
                    ++level;
                }
            }

#if UNITY_EDITOR
            DebugEx.Info($"{logTag} diff range [{realDffyRange.min}, {realDffyRange.max}]");
            DebugPrint_PoolInfo(candidateCategoryLevelDict, "初始奖励备选池");
#endif
        }

        private void PrepareOrderRequireItemPool()
        {
            if (candidateOrderOverride.Count > 0)
            {
                foreach (var order in candidateOrderOverride)
                {
                    ExtractOrderItem(order);
                }
            }
            else
            {
                var orders = ObjectPool<List<IOrderData>>.GlobalPool.Alloc();
                // 仅关注common和random
                helper.proxy.FillActiveOrders(orders, (int)(OrderProviderTypeMask.Common | OrderProviderTypeMask.Random));

                foreach (var order in orders)
                {
                    if (order.ProviderType == (int)OrderProviderType.Common)
                    {
                        if (relateCommonSet.Contains(order.Id))
                        {
                            ExtractOrderItem(order);
                        }
                    }
                    else if (order.ProviderType == (int)OrderProviderType.Random)
                    {
                        if (relateRandomSet.Contains(order.Id))
                        {
                            ExtractOrderItem(order);
                        }
                    }
                }
                ObjectPool<List<IOrderData>>.GlobalPool.Free(orders);
            }
#if UNITY_EDITOR
            DebugPrint_PoolInfo(requireCategoryLevelDict, "订单需求池");
#endif
        }

        private void ExtractOrderItem(IOrderData order)
        {
            foreach (var req in order.Requires)
            {
                var itemId = req.Id;
                // 已记录相同id的需求
                if (requireItemPayDffyDict.ContainsKey(itemId))
                    continue;
                Game.Manager.mergeItemMan.GetItemCategoryIdAndLevel(itemId, out var cid, out var level);
                // 此item的链条不在<关注的链条>里
                if (!candidateCategoryLevelDict.ContainsKey(cid))
                    continue;
                // 无配置?
                if (!cfgOrderCategoryDict.TryGetValue(cid, out var orderCat))
                    continue;
                var pay = CalcItemPayDffy(itemId, orderCat);
                if (pay > 0)
                {
                    // 缓存到cat-level表
                    IndexingItemByCatAndLevel(itemId, cid, level);
                    // 要求排除付出难度小于等于0的项目
                    requireItemPayDffyDict.Add(itemId, pay);
                    // 以链条分组记录需求
                    UpdateMaskDict(requireCategoryLevelDict, cid, level);
                }
            }
        }

        private int CalcItemPayDffy(int itemId, OrderCategory orderCat)
        {
            var mgr = Game.Manager.mergeItemDifficultyMan;
            var itemCount = GetCurrentActiveItemCount();
            var cat = Game.Manager.mergeItemMan.GetCategoryConfigByItemId(itemId);
            var accDffy = 0;
            foreach (var item in cat.Progress)
            {
                mgr.TryGetItemDifficulty(item, out _, out var realDffy);
                if (itemCount.TryGetValue(item, out var num))
                {
                    accDffy += num * realDffy;
                }
                if (item == itemId)
                {
                    var careDffy = CalcTotalCareGraphDffy(orderCat.CareGraphId);
                    // 付出难度 = 实际难度 - 累积难度(最新文档里用了<持有难度>的说法) - 牵连难度
                    var pay = realDffy - accDffy - careDffy;
                    return pay;
                }
            }
            throw new System.ArgumentOutOfRangeException("item not found in cat!");
        }

        private IDictionary<int, int> GetCurrentActiveItemCount()
        {
            return tracer.GetCurrentActiveItemCount();
        }

        private int CalcTotalCareGraphDffy(IList<int> cats)
        {
            var dffy = 0;
            foreach (var catId in cats)
            {
                dffy += CalcCategoryBoardRealDffy(catId);
            }
            return dffy;
        }

        // 计算整个链条在棋盘上贡献的实际难度
        private int CalcCategoryBoardRealDffy(int catId)
        {
            var cache = categoryBoardDffyCache;
            if (cache.ContainsKey(catId))
                return cache[catId];
            var mgr = Game.Manager.mergeItemDifficultyMan;
            var itemCount = GetCurrentActiveItemCount();
            var catCfg = Game.Manager.mergeItemMan.GetCategoryConfig(catId);
            var catBoardDffy = 0;
            foreach (var item in catCfg.Progress)
            {
                if (itemCount.TryGetValue(item, out var _num))
                {
                    mgr.TryGetItemDifficulty(item, out _, out var realDffy);
                    catBoardDffy += realDffy * _num;
                }
            }
            cache.Add(catId, catBoardDffy);
            return catBoardDffy;
        }

        // <候选池item>的等级不能超过同链条<订单池item>的最大等级
        // 于是需要裁剪候选池的链条物品等级
        private void ClampCandItemLevelToMaxRequireLevel()
        {
            foreach (var kv in candidateCategoryLevelDict)
            {
                var cid = kv.Key;
                // 订单里没在要的链条直接忽略
                if (!requireCategoryLevelDict.TryGetValue(cid, out var levelMask))
                    continue;
                // 取得订单链条里需求的最高等级
                var highLevelBit = HighestOneBit(levelMask);
                // 候选item不允许超过最高等级
                var clampedMask = ((highLevelBit << 1) - 1) & kv.Value;
                if (clampedMask > 0)
                {
                    releaseCategoryLevelDict.Add(cid, clampedMask);
                }
            }

#if UNITY_EDITOR
            DebugPrint_PoolInfo(releaseCategoryLevelDict, "最终奖励备选池");
#endif
        }

        private int RandomPickFromOrigCandidate()
        {
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var list))
            {
                foreach (var kv in candidateItemDict)
                {
                    list.Add(kv.Value);
                }
                if (list.Count < 1)
                    return -1;
                var rand = UnityEngine.Random.Range(0, list.Count);
                DebugEx.Info($"{logTag} fallback roll {rand}/{list.Count} => {list[rand]}");
                return list[rand];
            }
        }

        // 逐链条尝试组合
        private void GenerateOrderedResult()
        {
            foreach (var kv in requireCategoryLevelDict)
            {
                // 候选链条可能因等级不匹配被裁掉了
                if (!releaseCategoryLevelDict.TryGetValue(kv.Key, out var candLevels))
                    continue;
                ResolveReqWithEveryCand(kv.Key, kv.Value, candLevels);
            }
            bestItemList.Sort(SortItemInfo_Default);

#if UNITY_EDITOR
            DebugPrint_ItemInfoList(bestItemList, "default");
#endif
        }

        private static int SortItemInfo_Default(ItemInfo a, ItemInfo b)
        {
            if (a.result != b.result)
                return a.result - b.result;
            if (a.realDffy != b.realDffy)
                return -(a.realDffy - b.realDffy);
            return a.itemId - b.itemId;
        }

        // 根据订单需求遍历各种组合 并记录相关参数
        private void ResolveReqWithEveryCand(int cid, uint reqMask, uint candMask)
        {
            while (reqMask > 0)
            {
                var (reqId, reqLv) = ExtractHighLevelItem(cid, ref reqMask);
                // 只能使用等级小于等于当前订单需求的item 所以把candMask卡到当前等级
                var _mask = ((1u << (reqLv + 1)) - 1u) & candMask;
                while (_mask > 0)
                {
                    var (candId, _) = ExtractHighLevelItem(cid, ref _mask);
                    var result = ReqPayDffy_Minus_CandRealDffy(reqId, candId, out var realDffy);
                    if (isSpecialBox && result < 0)
                    {
                        result = 0;
                    }
                    bestItemList.Add(new ItemInfo()
                    {
                        itemId = candId,
                        cid = cid,
                        result = result,
                        realDffy = realDffy,
                    });
                }
            }
        }

        // 需求item的付出难度 减去 尝试item的实际难度
        private int ReqPayDffy_Minus_CandRealDffy(int reqId, int candId, out int candIdRealDffy)
        {
            var payDffy = requireItemPayDffyDict[reqId];
            Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(candId, out _, out candIdRealDffy);
            return payDffy - candIdRealDffy;
        }

        private void FillPossibleOutputs(List<int> outputs, bool foreceFallback = false)
        {
            // 如果 force or <订单棋子需求池>为空 or <最终棋子备选池>为空 则以<奖励棋子备选池>为准
            if (foreceFallback ||
                requireCategoryLevelDict.Count < 1 ||
                releaseCategoryLevelDict.Count < 1)
            {
                foreach (var kv in candidateItemDict)
                {
                    outputs.Add(kv.Value);
                }
            }
            else
            {
                // 需要展示的可选项为<最终棋子备选池>
                foreach (var kv in releaseCategoryLevelDict)
                {
                    var levelMask = kv.Value;
                    while (levelMask > 0)
                    {
                        var (itemId, _) = ExtractHighLevelItem(kv.Key, ref levelMask);
                        outputs.Add(itemId);
                    }
                }
            }
            outputs.Sort();
        }

        // 魔盒 / 提供一个结果
        private int ChooseResultForSpecialBox()
        {
            var list = bestItemList;
            if (list.Count < 1)
            {
                // 没有结果
                return RandomPickFromOrigCandidate();
            }

            // 根据分数线范围取一批最优项
            var offset = Game.Manager.configMan.globalConfig.BoxPossibleOffset;
            var valid_result_count = 1;
            var first_result = list[0].result;
            for (var i = 1; i < list.Count; ++i)
            {
                if (list[i].result > first_result + offset)
                {
                    break;
                }
                ++valid_result_count;
            }
            var rand = UnityEngine.Random.Range(0, valid_result_count);
            DebugEx.Info($"{logTag} roll {rand}/{valid_result_count} => {list[rand].itemId}");
            return list[rand].itemId;
        }

        // 三选一盒子
        // 需要提供三个候选项目 三个选项的链条尽量不重复
        private void ChooseResultForChoiceBox(List<int> container, int needNum)
        {
            var usedCats = ObjectPool<HashSet<int>>.GlobalPool.Alloc();
            var fallbackItems = ObjectPool<List<ItemInfo>>.GlobalPool.Alloc();

            PrepareFallbackCandPool(fallbackItems);

            // 首先尝试在算法结果里找出不同链条的N个最佳item
            if (TryFillWithCandPool(container, bestItemList, usedCats, needNum))
            {
                // 直接找到
                DebugPrint_ChoiceResult(container, false, true, true);
            }
            else if (TryFillWithCandPool(container, fallbackItems, usedCats, needNum))
            {
                // 直接结果无法凑够N个需求 启用fallback
                DebugPrint_ChoiceResult(container, true, true, true);
            }
            else if (TryFillWithCandPool(container, fallbackItems, usedCats, needNum, false))
            {
                // fallback后也无法凑够 需要允许链条重复
                DebugPrint_ChoiceResult(container, true, false, true);
            }
            else
            {
                // fallback且链条重复也凑不够 只能再允许item重复
                while (container.Count < needNum)
                {
                    TryFillWithCandPool(container, fallbackItems, usedCats, needNum, false, false);
                }
                DebugPrint_ChoiceResult(container, true, false, false);
            }

            ObjectPool<HashSet<int>>.GlobalPool.Free(usedCats);
            ObjectPool<List<ItemInfo>>.GlobalPool.Free(fallbackItems);
        }

        // 对原始候选item排序 当常规逻辑无法提供足够的候选时 用原始数据替补
        private void PrepareFallbackCandPool(List<ItemInfo> container)
        {
            var mgr = Game.Manager.mergeItemDifficultyMan;
            foreach (var kv in candidateItemDict)
            {
                mgr.TryGetItemDifficulty(kv.Value, out _, out var realDffy);
                container.Add(new ItemInfo()
                {
                    itemId = kv.Value,
                    cid = kv.Key / levelMod,
                    realDffy = realDffy,
                });
            }
            // 优先实际难度大的 / 其次id靠前的
            container.Sort((a, b) =>
            {
                if (a.realDffy != b.realDffy)
                    return -(a.realDffy - b.realDffy);
                return a.itemId - b.itemId;
            });

#if UNITY_EDITOR
            DebugPrint_ItemInfoList(container, "fallback");
#endif
        }

        private bool TryFillWithCandPool(List<int> container,
                                        List<ItemInfo> sortedPool,
                                        HashSet<int> usedCats,
                                        int needNum,
                                        bool distinctCat = true,
                                        bool distinctItem = true)
        {
            foreach (var info in sortedPool)
            {
                if (container.Count >= needNum)
                    break;
                if (distinctCat && usedCats.Contains(info.cid))
                    continue;
                if (distinctItem && container.Contains(info.itemId))
                    continue;
                usedCats.AddIfAbsent(info.cid);
                container.Add(info.itemId);
            }
            return container.Count >= needNum;
        }

        #region help method

        private void UpdateMaskDict(Dictionary<int, uint> dict, int key, int bitOffset)
        {
            if (dict.TryGetValue(key, out var mask))
            {
                dict[key] = mask | (1u << bitOffset);
            }
            else
            {
                dict.Add(key, 1u << bitOffset);
            }
        }

        // 提取mask里的最高等级item
        private (int id, int level) ExtractHighLevelItem(int cid, ref uint levelMask)
        {
            if (levelMask > 0)
            {
                var level = 0;
                var hb = HighestOneBit(levelMask);
                levelMask -= hb;
                while (hb > 1)
                {
                    hb >>= 1;
                    ++level;
                }
                return (FindItemIdByCatAndLevel(cid, level), level);
            }
            return (-1, -1);
        }

        private int FindItemIdByCatAndLevel(int cid, int level)
        {
            _ = catLvlItemCache.TryGetValue(cid * levelMod + level, out var id);
            return id;
        }

        private uint HighestOneBit(uint val)
        {
            val |= val >> 1;
            val |= val >> 2;
            val |= val >> 4;
            val |= val >> 8;
            val |= val >> 16;
            return val - (val >> 1);
        }

        #endregion

        #region debug

        private void DebugPrint_ItemInfo(StringBuilder sb, ItemInfo info)
        {
            sb.Append($"({info.itemId}@{info.cid}\tΔ{info.result}\tR{info.realDffy})");
        }

        private void DebugPrint_ItemInfoList(List<ItemInfo> list, string tag)
        {
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                sb.Append($"{logTag} result pool <{tag}>");
                foreach (var info in list)
                {
                    sb.AppendLine();
                    DebugPrint_ItemInfo(sb, info);
                }
                DebugEx.Info($"{sb}");
            }
        }

        private void DebugPrint_ChoiceResult(List<int> results, bool fallback, bool distinctCat, bool distinctItem)
        {
#if UNITY_EDITOR
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                sb.Append($"{logTag} choice done");
                sb.AppendLine();
                sb.Append($"fallback: {fallback} | distinctCat: {distinctCat} | distinctItem: {distinctItem}");
                sb.AppendLine();
                sb.Append($"(");
                foreach (var item in results)
                {
                    sb.Append($"{item},");
                }
                sb.Append($")");
                DebugEx.Info($"{sb}");
            }
#endif
        }

        private void DebugPrint_PoolInfo(Dictionary<int, uint> pool, string tag)
        {
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                sb.Append($"{logTag} {tag} pool detail");
                var list = pool.ToList();
                list.Sort((a, b) => a.Key - b.Key);
                foreach (var item in list)
                {
                    sb.AppendLine();
                    // .NET8+才支持{:B}
                    // sb.Append($"{item.Key} => {item.Value:b20}");
                    sb.Append($"{item.Key} => {System.Convert.ToString(item.Value, 2).PadLeft(20, '0')}");
                }
                DebugEx.Info($"{sb}");
            }
        }

        #endregion
    }
}