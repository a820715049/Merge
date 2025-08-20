/*
 * @Author: qun.chao
 * @Date: 2023-11-12 21:31:06
 */
using System;
using System.Text;
using System.Collections.Generic;
using EL;
using FAT.Merge;
using fat.rawdata;
using fat.gamekitdata;
using FAT.RemoteAnalysis;

namespace FAT
{
    // doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/ta0f3dp8t5sgeezz#VPLDa
    // 2024.04.01 doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/qcavgsi4hxkpbuyr
    // 2024.08.26 doc:  https://centurygames.yuque.com/ywqzgn/ne0fhm/czf57svmn2qg71ce
    public class OrderProviderRandom : IOrderProvider
    {
        enum ItemNumMask
        {
            One = 1,
            Two = 1 << 1,
            Three = 1 << 2,
            All = One | Two | Three,
        }

        // 受控难度类型
        internal enum CtrlDffyType
        {
            Normal,
            Easy,
            Hard,
            Safe,
        }

        internal struct CandidateItemInfo
        {
            public int Id;
            public int OriginGraphId;   // 源头链条
            public int CategoryId;
            public int CategoryWeight;
            public int AccDffy; // 从1级开始累积的难度
            public int RealDffy; // 实际难度
            public int PayDffy; // 付出难度
            public int CareDffy; // 牵连难度 careGraphId字段提供的难度

            public override string ToString()
            {
                return $"{Id}@{CategoryId}-R{RealDffy}-P{PayDffy}-C{CareDffy}";
            }
        }

        internal class CandidateGroupInfo
        {
            public int Weight;
            public int ItemCount;
            public CandidateItemInfo[] Items = new CandidateItemInfo[3];
            public long CatBundle;     // 链条搭配组成的唯一标识
        }

        class CandidateCategory
        {
            public OrderCategory Cat;
            public int Weight;
            public int WeightRaw;
            public bool NeedByOrder;        // 订单正在需求 且 有缺口
            public bool CareByOtherCat;     // 被其他订单'前置'
            public bool OpposeByOtherCat;   // 被其他订单'对立'
            public bool RecentUsed;         // 最近提交过
            public bool OriginNoNeed;       // 源头没有需求
        }

        class DataContainer
        {
            public Dictionary<int, OrderRandomer> orderConfigDict = new();
            public List<OrderRandomer> orderConfigList = new();
            public List<OrderData> activeOrderList = new();
            public List<OrderCategory> categoryPoolList = new();
            public IDictionary<int, OrderIgnore> ignoreItemDict;
            public MergeBoardOrder mergeBoardOrder;

            // 付出难度 区间0,1,2
            public (int from, int to) pay_0;
            public (int from, int to) pay_1;
            public (int from, int to) pay_2;

            // 实际难度 区间0,1,2
            public (int from, int to) real_0;
            public (int from, int to) real_1;
            public (int from, int to) real_2;

            public int[] group_type_weight = new int[3];
        }

        private Action<List<IOrderData>, List<IOrderData>> mOnOrderListDirty;

        #region cache
        private List<IOrderData> mCacheChangedList = new();
        private List<IOrderData> mCacheNewlyAddedList = new();
        private HashSet<int> mActiveOrderIdSet = new();
        // 记录订单中需要的链条 (忽略需求已满足的订单)
        private HashSet<int> mCategoryInOrderSet = new();
        // 最近完成的链条记录
        private HashSet<int> mCategoryRecentFinishedSet = new();

        // 1. 记录合法的候选链条
        private Dictionary<int, CandidateCategory> mCandCategoryDict = new();
        // 2. 候选链条中遍历得到所有候选item
        private List<CandidateItemInfo> mCandItemList = new();
        // 3. 候选item组合成候选组
        private List<CandidateGroupInfo> mCandGroupList = new();

        private List<int> mCacheRequireId = new();
        private List<IActivityOrderHandler> mActivityOrderHanlderList = new();
        private Dictionary<int, int> mCacheCategoryBoardDffy = new();
        #endregion

        #region api order
        // api上报需要的候选列表
        private List<CandidateGroupInfo> mCandGroupListForApi = new();
        // 配置为需要api生成的订单
        private HashSet<int> mApiOrderSet = new();
        private RemoteOrderWrapper mRemoteOrderWrapper = new();
        #endregion

        private DataContainer mDataHolder = new();
        private MergeWorldTracer mTracer;
        private IOrderHelper mHelper;
        private bool mDirty;

        private readonly long catBundleMod = 10000L;
        private int mSpawnLimitCount = 1;
        private float mSpawnIntervalSec => Game.Manager.configMan.globalConfig.OrderEnterDelay / 1000f;
        private float mLastOrderSpawnTime = 0f;

        void IOrderProvider.Reset()
        {
            mRemoteOrderWrapper.Reset();
            mCandGroupListForApi.Clear();
            mApiOrderSet.Clear();

            mCacheChangedList.Clear();
            mCacheNewlyAddedList.Clear();
            mActiveOrderIdSet.Clear();
            mCategoryInOrderSet.Clear();
            mCategoryRecentFinishedSet.Clear();
            mCandItemList.Clear();
            mCandCategoryDict.Clear();
            mCacheRequireId.Clear();
            _FreeCandidateGroup(mCandGroupList);

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
            mDataHolder.activeOrderList.Clear();
            mDataHolder.categoryPoolList.Clear();
            mDataHolder.orderConfigDict.Clear();
            mDataHolder.orderConfigList.AddRange(Game.Manager.configMan.GetOrderRandomerConfigByFilter(x => x.BoardId == bid));
            foreach (var cfg in mDataHolder.orderConfigList) { mDataHolder.orderConfigDict.Add(cfg.Id, cfg); }
            mDataHolder.categoryPoolList.AddRange(Game.Manager.configMan.GetOrderCategoryConfigByFilter(x => x.BoardId == bid));
            mDataHolder.ignoreItemDict = Game.Manager.configMan.GetOrderIgnoreConfigMap();
            mDataHolder.mergeBoardOrder = Game.Manager.configMan.GetOneMergeBoardOrderByFilter(x => x.Id == bid);

            foreach (var cfg in mDataHolder.orderConfigList)
            {
                if (cfg.IsApiOrder)
                    mApiOrderSet.AddIfAbsent(cfg.Id);
            }

            using var _ = PoolMapping.PoolMappingAccess.Borrow<Dictionary<int, OrderRandomer>>(out var dict);
            foreach (var cfg in mDataHolder.orderConfigList)
            {
                dict.Add(cfg.Id, cfg);
            }

            foreach (var rec in records)
            {
                var order = OrderUtility.MakeOrderByRecord(rec);
                if (dict.TryGetValue(order.Id, out var cfg))
                {
                    order.ConfRandomer = cfg;
                }
                mDataHolder.activeOrderList.Add(order);
            }
        }

        void IOrderProvider.Serialize(IList<OrderRecord> records)
        {
            foreach (var order in mDataHolder.activeOrderList)
            {
                if (order.OrderType == (int)OrderType.MagicHour)
                {
                    // 星想事成订单不保存
                    continue;
                }
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
                DebugEx.Warning($"OrderProviderRandom::TryFinishOrder ----> no such order {id}");
                return false;
            }
            if (OrderUtility.TryFinishOrder(order, mTracer, mHelper, ReasonString.order, rewards))
            {
                // 记录最近完成的api槽订单
                if (mApiOrderSet.Contains(id))
                {
                    mHelper.proxy.SetRecentApiOrder(order);
                }
                _RefreshOrderSpawnTime();
                DebugEx.Warning($"OrderProviderRandom::TryFinishOrder ----> finish order {id} succeed");
                (this as IOrderProvider).SetDirty();

                // 记录最近完成的订单链条 用于后续生成新订单时的权重计算
                mCategoryRecentFinishedSet.Clear();
                for (int i = 0; i < order.Requires.Count; i++)
                {
                    var cat = Merge.Env.Instance.GetCategoryByItem(order.Requires[i].Id);
                    mCategoryRecentFinishedSet.Add(cat.Id);
                }

                return true;
            }
            else
            {
                DebugEx.Warning($"OrderProviderRandom::TryFinishOrder ----> finish order {id} failed");
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

        // 此订单是否被配置为<受控订单>
        public bool IsCtrledOrder(IOrderData order)
        {
            if (mDataHolder.orderConfigDict.TryGetValue(order.Id, out var cfg))
            {
                return cfg.IsCtrled;
            }
            return false;
        }

        // 此订单是否被配置为<受控订单>
        public bool IsApiOrder(IOrderData order)
        {
            if (mDataHolder.orderConfigDict.TryGetValue(order.Id, out var cfg))
            {
                return cfg.IsApiOrder;
            }
            return false;
        }

        public OrderData MakeOrder(OrderRandomer cfg)
        {
            return _MakeOrder_Passive(cfg);
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
                if (mCacheNewlyAddedList.Count > 0)
                {
                    // 已生成新订单
                    // 最近完成的订单已参与过计算
                    // 可以清空了
                    mCategoryRecentFinishedSet.Clear();
                }
                mOnOrderListDirty?.Invoke(mCacheChangedList, mCacheNewlyAddedList);
            }
        }

        private bool _RefreshOrderListImp(List<OrderData> activeContainer, List<IOrderData> changedContainer, List<IOrderData> newlyAddedContainer)
        {
            var list_active = activeContainer;
            var list_changed = changedContainer;
            var list_new = newlyAddedContainer;
            var list_request = mHelper.ImmediateSlotRequests;
            var activity_handlers = mActivityOrderHanlderList;

            list_changed.Clear();
            list_new.Clear();
            list_request.Clear();
            activity_handlers.Clear();
            _CollectActivityHandler(mTracer.world.activeBoard.boardId, activity_handlers);

            // 更新订单状态
            for (var i = list_active.Count - 1; i >= 0; --i)
            {
                var order = list_active[i];
                var changed = _ProcessOrder(activity_handlers, order);
                if (order.State == OrderState.Rewarded || order.State == OrderState.Expired)
                {
                    changed = true;
                    list_active.RemoveAt(i);
                }
                if (changed)
                {
                    list_changed.Add(order);
                }
            }

            // 缓存当前订单Id
            var cacheForActive = mActiveOrderIdSet;
            cacheForActive.Clear();
            foreach (var order in list_active)
            {
                cacheForActive.Add(order.Id);
            }

            // 没有立即生成请求时 可以不生成订单
            if (list_request.Count < 1)
            {
                // 随机订单最多不超过配置个 | 即 每个运行中的随机订单都有唯一的配置对应
                if (list_active.Count >= mDataHolder.orderConfigList.Count ||
                    _IsWaitingForNextOrder())
                {
                    return list_changed.Count > 0;
                }
            }

            // 刷新订单生成时间
            _RefreshOrderSpawnTime();

            mCandCategoryDict.Clear();
            mCacheCategoryBoardDffy.Clear();

            // 处理立即生成请求
            _ProcessImmediateSlotRequest(list_active, list_new, list_request, activity_handlers);

            // 尝试生成新订单
            foreach (var cfg in mDataHolder.orderConfigList)
            {
                if (list_new.Count >= mSpawnLimitCount)
                    break;
                _TryGenerateOrder(cfg, list_active, list_new, activity_handlers);
            }

            // 再次处理立即生成请求
            _ProcessImmediateSlotRequest(list_active, list_new, list_request, activity_handlers);

            // clear
            _FreeCandidateGroup(mCandGroupList);

            return list_changed.Count > 0 || list_new.Count > 0;
        }

        private void _ProcessImmediateSlotRequest(List<OrderData> list_active, List<IOrderData> list_new, List<int> list_request, List<IActivityOrderHandler> activity_handlers)
        {
            var count = 0;
            foreach (var slotId in list_request)
            {
                if (mDataHolder.orderConfigDict.TryGetValue(slotId, out var cfg))
                {
                    _TryGenerateOrder(cfg, list_active, list_new, activity_handlers);
                }
                ++count;
            }
            if (count > 0)
            {
                list_request.RemoveRange(0, count);
            }
        }


        private void _TryGenerateOrder(OrderRandomer cfg, List<OrderData> list_active, List<IOrderData> list_new, List<IActivityOrderHandler> activity_handlers)
        {
            if (_CheckShouldShutDownOrSkip(cfg))
                return;
            if (!mHelper.CheckStateByConditionGroup(cfg.ActiveLevel,
                                                    cfg.ShutdownLevel,
                                                    cfg.ActiveOrderId,
                                                    cfg.ShutdownOrderId,
                                                    cfg.ActiveItemId,
                                                    cfg.ShutdownItemId))
            {
                return;
            }

            // 订单可用
            OrderData newOrder = null;
            if (cfg.IsPassive)
            {
                _ActivityHandler_TryGeneratePassiveOrder(activity_handlers, cfg, out newOrder);
            }
            else
            {
                if (cfg.IsCtrled)
                {
                    newOrder = _MakeOrder_Ctrled(cfg);
                }
                if (newOrder == null)
                {
                    // 未成功受控 or 本来就是常规订单 再用normal难度尝试一次
                    ItemNumMask laskMask;
                    (newOrder, laskMask) = _MakeOrder(cfg, CtrlDffyType.Normal);
                    if (newOrder == null)
                    {
                        // 对normal难度的订单再用safe难度尝试一次 且保持上次尝试的订单需求棋子个数
                        (newOrder, _) = _MakeOrder(cfg, CtrlDffyType.Safe, laskMask);
                    }
                }
            }

            if (newOrder == null)
                return;

            _ProcessOrder(activity_handlers, newOrder);
            mActiveOrderIdSet.Add(newOrder.Id);
            list_active.Add(newOrder);
            list_new.Add(newOrder);
            DataTracker.order_start.Track(newOrder);
        }

        private bool _CheckShouldShutDownOrSkip(OrderRandomer cfg)
        {
            if (mActiveOrderIdSet.Contains(cfg.Id))
                return true;
            foreach (var shutdownId in cfg.ShutdownRandId)
            {
                if (mActiveOrderIdSet.Contains(shutdownId))
                {
                    return true;
                }
            }
            return false;
        }

        private void _FreeCandidateGroup(List<CandidateGroupInfo> list)
        {
            foreach (var g in list)
            {
                ObjectPool<CandidateGroupInfo>.GlobalPool.Free(g);
            }
            list.Clear();
        }

        private void _FreeCandidateGroupByRange(List<CandidateGroupInfo> list, int startIdx, int count)
        {
            for (int i = 0; i < count; i++)
            {
                ObjectPool<CandidateGroupInfo>.GlobalPool.Free(list[i + startIdx]);
            }
            list.RemoveRange(startIdx, count);
        }

        /// <summary>
        /// 根据 单/双/三组合的个数 决定是否参与三选一权重
        /// 进而决定使用哪种组合 进而对列表进行过滤
        /// 仅保留目标类别 清除其他类别的候选
        /// </summary>
        /// <param name="a">单组合数量</param>
        /// <param name="b">双组合数量</param>
        /// <param name="c">三组合数量</param>
        /// <returns>订单需求item的数量(1/2/3)</returns>
        private ItemNumMask _FilterByGroupTypeCount(int a, int b, int c)
        {
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var list))
            {
                list.Add(a);
                list.Add(b);
                list.Add(c);
                var typeIdx = _ChooseGroupTypeIdx(list);
                var candGroup = mCandGroupList;
                // 仅保留 a/b/c
                if (typeIdx == 0)
                {
                    _FreeCandidateGroupByRange(candGroup, a, b + c);
                }
                else if (typeIdx == 1)
                {
                    _FreeCandidateGroupByRange(candGroup, a + b, c);
                    _FreeCandidateGroupByRange(candGroup, 0, a);
                }
                else if (typeIdx == 2)
                {
                    _FreeCandidateGroupByRange(candGroup, 0, a + b);
                }
                var itemNumType = 1 << typeIdx;
                return (ItemNumMask)itemNumType;
            }
        }

        private int _ChooseGroupTypeIdx(List<int> countList)
        {
            int totalWeight = 0;
            var gtw = mDataHolder.group_type_weight;
            for (int i = 0; i < gtw.Length; i++)
            {
                if (gtw[i] > 0 && countList[i] > 0)
                {
                    totalWeight += gtw[i];
                }
            }
            // 0,1,2 表示 单/双/三
            int chosenTypeIdx = 0;
            var weight_sum = 0;
            var roll = _Random_Roll(totalWeight);
            for (int i = 0; i < gtw.Length; i++)
            {
                if (gtw[i] > 0 && countList[i] > 0)
                {
                    weight_sum += gtw[i];
                    if (weight_sum >= roll)
                    {
                        chosenTypeIdx = i;
                        break;
                    }
                }
            }

            DebugEx.Info($"OrderProviderRandom::_ChooseGroupTypeIdx roll {roll}/{totalWeight} type-{chosenTypeIdx}");
            return chosenTypeIdx;
        }

        /*
        按权重分类
        对类别以权重值随机
        仅保留选中的类别
        */
        private void _FilterCandGroupByWeight()
        {
            var targetWeight = 0;
            using (ObjectPool<Dictionary<int, int>>.GlobalPool.AllocStub(out var hash))
            {
                foreach (var item in mCandGroupList)
                {
                    if (!hash.ContainsKey(item.Weight))
                    {
                        hash.Add(item.Weight, 0);
                    }
                }
                using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var list))
                {
                    var totalWeight = 0;
                    foreach (var kv in hash)
                    {
                        totalWeight += kv.Key;
                        list.Add(kv.Key);
                    }

                    _DebugPrint_CandGroup_GroupByWeight(mCandGroupList, list);

                    var roll = _Random_Roll(totalWeight);
                    var weight_sum = 0;
                    for (var i = 0; i < list.Count; i++)
                    {
                        weight_sum += list[i];
                        if (weight_sum >= roll)
                        {
                            targetWeight = list[i];
                            break;
                        }
                    }
                    DebugEx.Info($"OrderProviderRandom::_FilterCandGroupByWeight roll {roll}/{totalWeight} w={targetWeight}");
                }
            }
            var candList = mCandGroupList;
            for (var i = candList.Count - 1; i >= 0; --i)
            {
                if (candList[i].Weight != targetWeight)
                {
                    ObjectPool<CandidateGroupInfo>.GlobalPool.Free(candList[i]);
                    candList.RemoveAt(i);
                }
            }
        }

        // 按所属链条筛选候选组合
        private void _FilterCandGroupByCatId()
        {
            var candList = mCandGroupList;
            var targetKey = 0L;
            using (ObjectPool<Dictionary<long, int>>.GlobalPool.AllocStub(out var hash))
            {
                foreach (var cand in candList)
                {
                    // 链条组合计算得到key
                    var catBundle = 0L;
                    for (int i = 0; i < cand.ItemCount; i++)
                    {
                        catBundle = catBundle * catBundleMod + cand.Items[i].CategoryId;
                    }
                    cand.CatBundle = catBundle;

                    if (!hash.ContainsKey(catBundle))
                    {
                        hash.Add(catBundle, 0);
                    }
                }

                using (ObjectPool<List<long>>.GlobalPool.AllocStub(out var catBundleList))
                {
                    foreach (var kv in hash)
                    {
                        catBundleList.Add(kv.Key);
                    }

                    _DebugPrint_CandGroup_GroupByCat(candList, catBundleList);

                    targetKey = catBundleList[_Random_Pick(catBundleList.Count)];
                    DebugEx.Info($"OrderProviderRandom::_FilterCandGroupByCatId catgroup => {targetKey}");
                }
            }
            // 仅保留目标链条组合
            for (var i = candList.Count - 1; i >= 0; --i)
            {
                if (candList[i].CatBundle != targetKey)
                {
                    ObjectPool<CandidateGroupInfo>.GlobalPool.Free(candList[i]);
                    candList.RemoveAt(i);
                }
            }
        }

        private int _Remap(int id)
        {
            return Game.Manager.userGradeMan.GetTargetConfigDataId(id);
        }

        // 棋盘统计信息
        private IDictionary<int, int> _GetCurrentActiveItemCount()
        {
            // 包括仓库 / 礼物通道
            return mTracer.GetCurrentActiveItemCount();
        }

        // 难度受控订单
        private OrderData _MakeOrder_Ctrled(OrderRandomer cfg)
        {
            if (mHelper.proxy.GetRecentCtrlDffyInfo(out var act, out var pay, out var recentCtrlType))
            {
                var (low, high) = _ExtractDffyThreshold((CtrlDffyType)recentCtrlType);
                if (pay * 1.0f / act >= high / 100.0f)
                {
                    DebugEx.Info($"[ORDERDEBUG] [ORDERCTRL] {pay}/{act} >= {high}");
                    return _MakeOrder(cfg, CtrlDffyType.Easy).order;
                }
                else if (pay * 1.0f / act <= low / 100.0f)
                {
                    DebugEx.Info($"[ORDERDEBUG] [ORDERCTRL] {pay}/{act} <= {low}");
                    return _MakeOrder(cfg, CtrlDffyType.Hard).order;
                }
            }
            return null;
        }

        private OrderData _MakeOrder_Default(OrderRandomer cfg)
        {
            return _MakeOrder(cfg).order;
        }

        private OrderData _MakeOrder_Passive(OrderRandomer cfg)
        {
            var (order, mask) = _MakeOrder(cfg);
            if (order == null)
            {
                (order, _) = _MakeOrder(cfg, CtrlDffyType.Safe, mask);
            }
            return order;
        }

        private (OrderData order, ItemNumMask numMask) _MakeOrder(OrderRandomer cfg, CtrlDffyType dffyType = CtrlDffyType.Normal, ItemNumMask numMask = ItemNumMask.All)
        {
            _PrepareRangeParams(cfg, dffyType);
            _EnsureCategoryRefInOrder();
            _EnsureCandidateCategory();
            _PrepareCandidateItemPool(cfg, dffyType);

            _DebugPrint_CandCategoryPool();
            _DebugPrint_CandItemPool();

            var candGroup = mCandGroupList;
            if ((numMask & ItemNumMask.One) > 0)
                _FilterSingle();
            var single_count = candGroup.Count;
            if ((numMask & ItemNumMask.Two) > 0 && mDataHolder.group_type_weight[1] > 0)
                _FilterDouble();
            var double_count = candGroup.Count - single_count;
            if ((numMask & ItemNumMask.Three) > 0 && mDataHolder.group_type_weight[2] > 0)
                _FilterTriple();
            var triple_count = candGroup.Count - single_count - double_count;

            // 根据require(单/双/三)搭配数按权重确定本轮需要的形态 删除其他两类候选组合
            var candNumMask = _FilterByGroupTypeCount(single_count, double_count, triple_count);

            // 候选组合按权重类别分组 仅保留一种权重值
            _FilterCandGroupByWeight();
            _DebugPrint_CandGroup(candGroup, "<weight>");

            if (candGroup.Count < 1)
            {
                DebugEx.Info($"OrderProviderRandom::_MakeOrder no valid cand");
                return (null, candNumMask);
            }

            // 如果允许api参与 则备份到此为止的候选组合 准备上传数据
            var shouldCallApi = _TryPrepareApiRequestData(cfg);

            // 候选组合为遍历收集得到 不存在仅require的顺序不同的候选组合
            _FilterCandGroupByCatId();
            _DebugPrint_CandGroup(candGroup, "<cat>");

            // 从当前候选中随机选取一个
            var cand = candGroup[_Random_Pick(candGroup.Count)];
            if (cand == null)
            {
                DebugEx.Info($"OrderProviderRandom::_MakeOrder cand group not found");
                return (null, candNumMask);
            }
            DebugEx.Info(@$"OrderProviderRandom::_MakeOrder payDiff {cfg.PayDiffGrpId} => {_Remap(cfg.PayDiffGrpId)},
                actDiff {cfg.ActDiffGrpId} => {_Remap(cfg.ActDiffGrpId)},
                r {cfg.RewardGrpId} => {_Remap(cfg.RewardGrpId)}");
            mCacheRequireId.Clear();
            for (int i = 0; i < cand.ItemCount; i++)
            {
                mCacheRequireId.Add(cand.Items[i].Id);
            }
            var realDffy = OrderUtility.CalcRealDifficultyForRequires(mCacheRequireId);
            var payDffy = 0;
            for (int i = 0; i < cand.ItemCount; i++)
            {
                payDffy += cand.Items[i].PayDffy;
            }
            var realDffyRound = OrderUtility.CalcRealDffyRound(payDffy, realDffy, cfg.MinDiffRate);
            DebugEx.Info($"OrderProviderRandom::_MakeOrder D {realDffyRound} | P {payDffy} | R {realDffy} | min_rate {cfg.MinDiffRate}");

            var reward = Game.Manager.mergeItemMan.GetOrderRewardConfig(_Remap(cfg.RewardGrpId)).Reward;
            var newOrder = OrderUtility.MakeOrderByConfig(mHelper, OrderProviderType.Random, cfg.Id, cfg.RoleId, cfg.DisplayLevel,
                                                            realDffyRound,
                                                            mCacheRequireId, reward);
            // 记录conf
            newOrder.ConfRandomer = cfg;

            // 随机订单需要记录pay难度(付出难度)
            newOrder.Record.Extra.Add(RecordStateHelper.ToRecord((int)OrderParamType.PayDifficulty, payDffy));
            // 随机订单需要记录act难度(实际难度)
            newOrder.Record.Extra.Add(RecordStateHelper.ToRecord((int)OrderParamType.ActDifficulty, realDffy));

            // 尝试让api决策最终订单
            if (shouldCallApi)
            {
                // api决策的订单需要记录模型版本
                newOrder.Record.ModelVersion = cfg.ModelVersion;
                using (ObjectPool<List<IOrderData>>.GlobalPool.AllocStub(out var list))
                {
                    // 收集当前其他的api类(配置)订单
                    foreach (var item in mDataHolder.activeOrderList)
                    {
                        if (mApiOrderSet.Contains(item.Id))
                        {
                            list.Add(item);
                        }
                    }
                    mRemoteOrderWrapper.SendRequest(cfg.ModelVersion, newOrder, mHelper.proxy.recentApiOrder, mCandGroupListForApi, list);
                    newOrder.RemoteOrderResolver = _TryApplyApiOrder;
                    // 记录订单等待api
                    OrderUtility.SetOrderApiStatus(newOrder, OrderApiStatus.Requesting);
                }
            }

            newOrder.DffyStrategy = (int)dffyType;
            if (cfg.IsCtrled)
            {
                mHelper.proxy.SetRecentOrderCtrlStrategy((int)dffyType);
            }

            return (newOrder, candNumMask);
        }

        private bool _check_range_pay_0(int val) { return mDataHolder.pay_0.from <= val && mDataHolder.pay_0.to >= val; }
        private bool _check_range_pay_1(int val) { return mDataHolder.pay_1.from <= val && mDataHolder.pay_1.to >= val; }
        private bool _check_range_pay_2(int val) { return mDataHolder.pay_2.from <= val && mDataHolder.pay_2.to >= val; }
        private bool _check_range_real_0(int val) { return mDataHolder.real_0.from <= val && mDataHolder.real_0.to >= val; }
        private bool _check_range_real_1(int val) { return mDataHolder.real_1.from <= val && mDataHolder.real_1.to >= val; }
        private bool _check_range_real_2(int val) { return mDataHolder.real_2.from <= val && mDataHolder.real_2.to >= val; }

        private int _SortByCatId(CandidateItemInfo a, CandidateItemInfo b)
        {
            return a.CategoryId - b.CategoryId;
        }

        #region order ctrl / 订单动态难度控制
        private (int actDffyGrpId, int payDffyGrpId) _ExtractDffyGrp(OrderRandomer cfg, CtrlDffyType dffyType)
        {
            return dffyType switch
            {
                CtrlDffyType.Easy => (cfg.EasyActDiff, cfg.EasyPayDiff),
                CtrlDffyType.Hard => (cfg.HardActDiff, cfg.HardPayDiff),
                CtrlDffyType.Safe => (cfg.SafeActDiff, cfg.SafePayDiff),
                _ => (cfg.ActDiffGrpId, cfg.PayDiffGrpId),
            };
        }

        private (int low, int high) _ExtractDffyThreshold(CtrlDffyType recentStrategy)
        {
            var cfg = Game.Manager.configMan.globalConfig;
            return recentStrategy switch
            {
                CtrlDffyType.Easy => (cfg.OrderCtrlTooLow, cfg.OrderCtrlTooHighTarget),
                CtrlDffyType.Hard => (cfg.OrderCtrlTooLowTarget, cfg.OrderCtrlTooHigh),
                _ => (cfg.OrderCtrlTooLow, cfg.OrderCtrlTooHigh),
            };
        }
        #endregion

        private void _PrepareRangeParams(OrderRandomer cfg, CtrlDffyType dffyType)
        {
            var holder = mDataHolder;
            var (actGrpId, payGrpId) = _ExtractDffyGrp(cfg, dffyType);
            var actualDiff = Game.Manager.mergeItemMan.GetOrderDiffConfig(_Remap(actGrpId)).Diff;
            var payDiff = Game.Manager.mergeItemMan.GetOrderDiffConfig(_Remap(payGrpId)).Diff;

            _ParseRange(actualDiff[0], ref holder.real_0);
            _ParseRange(payDiff[0], ref holder.pay_0);
            if (payDiff.Count > 1)
            {
                _ParseRange(actualDiff[1], ref holder.real_1);
                _ParseRange(payDiff[1], ref holder.pay_1);
            }
            if (payDiff.Count > 2)
            {
                _ParseRange(actualDiff[2], ref holder.real_2);
                _ParseRange(payDiff[2], ref holder.pay_2);
            }

            var gtw = holder.group_type_weight;
            for (int i = 0; i < gtw.Length; i++)
            {
                gtw[i] = 0;
            }
            gtw[0] = cfg.NumWt[0];
            if (cfg.NumWt.Count > 1)
            {
                gtw[1] = cfg.NumWt[1];
            }
            if (cfg.NumWt.Count > 2)
            {
                gtw[2] = cfg.NumWt[2];
            }

            DebugEx.Info($"OrderProviderRandom::_PrepareRangeParams p0 {holder.pay_0.from} -- {holder.pay_0.to} | p1 {holder.pay_1.from} -- {holder.pay_1.to} | p2 {holder.pay_2.from} -- {holder.pay_2.to}");
            DebugEx.Info($"OrderProviderRandom::_PrepareRangeParams r0 {holder.real_0.from} -- {holder.real_0.to} | r1 {holder.real_1.from} -- {holder.real_1.to} | r2 {holder.real_2.from} -- {holder.real_2.to}");
            DebugEx.Info($"OrderProviderRandom::_PrepareRangeParams group-w {holder.group_type_weight[0]} / {holder.group_type_weight[1]} / {holder.group_type_weight[2]}");
        }

        private void _ParseRange(string str, ref (int from, int to) range)
        {
            if (string.IsNullOrEmpty(str))
            {
                range.from = 0;
                range.to = 0;
            }
            else
            {
                var strs = str.Split(':');
                int.TryParse(strs[0], out range.from);
                int.TryParse(strs[1], out range.to);
            }
        }

        private (int realDffy, int accDffy) _CalcDffy(int itemId)
        {
            var mgr = Game.Manager.mergeItemDifficultyMan;
            var itemCount = _GetCurrentActiveItemCount();

            mgr.TryGetItemDifficulty(itemId, out _, out var realDffy);
            int accDffy = 0;
            var cat = Env.Instance.GetCategoryByItem(itemId);
            foreach (var item in cat.Progress)
            {
                if (itemCount.TryGetValue(item, out var _num))
                {
                    mgr.TryGetItemDifficulty(item, out _, out var _realDffy);
                    accDffy += _num * _realDffy;
                }
            }
            return (realDffy, accDffy);
        }

        private int _CalcTotalCareGraphDffy(IList<int> cats)
        {
            var dffy = 0;
            foreach (var catId in cats)
            {
                dffy += _CalcCategoryBoardRealDffy(catId);
            }
            return dffy;
        }

        // 计算整个链条在棋盘上贡献的实际难度
        private int _CalcCategoryBoardRealDffy(int catId)
        {
            var cache = mCacheCategoryBoardDffy;
            if (cache.ContainsKey(catId))
                return cache[catId];
            var mgr = Game.Manager.mergeItemDifficultyMan;
            var itemCount = _GetCurrentActiveItemCount();
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

        private void _PrepareCandidateItemPool(OrderRandomer cfg, CtrlDffyType dffyType)
        {
            var candItemList = mCandItemList;
            candItemList.Clear();

            // 统计所有随机订单的需求
            using var _stub = ObjectPool<HashSet<int>>.GlobalPool.AllocStub(out var requireTable);
            foreach (var order in mDataHolder.activeOrderList)
            {
                foreach (var req in order.Requires)
                {
                    if (!requireTable.Contains(req.Id))
                        requireTable.Add(req.Id);
                }
            }

            var ignoreTable = mDataHolder.ignoreItemDict;
            var mgr = Game.Manager.mergeItemDifficultyMan;
            var itemCount = _GetCurrentActiveItemCount();
            var _realDffy = 0;

            var boardCfg = mDataHolder.mergeBoardOrder;
            var wt_used = cfg.IsSkipCategoryWt ? 0 : boardCfg.UsedWt;
            var wt_care = cfg.IsSkipCategoryWt ? 0 : boardCfg.CareWt;
            var wt_oppose = cfg.IsSkipCategoryWt ? 0 : boardCfg.OpposeWt;
            var wt_recent = cfg.IsSkipCategoryWt ? 0 : boardCfg.RecentWt;
            var wt_origin = cfg.IsSkipCategoryWt ? 0 : boardCfg.OriginWt;

            var wt_min = boardCfg.MinWt;
            var wt_max = boardCfg.MaxWt;
            var mergeLevel = Game.Manager.mergeLevelMan.level;

            foreach (var kv in mCandCategoryDict)
            {
                var orderCat = kv.Value;
                if (dffyType == CtrlDffyType.Easy && orderCat.Cat.IsIgnoreByEasy)
                    continue;
                orderCat.WeightRaw = orderCat.Cat.BasicWt +
                    (orderCat.NeedByOrder ? wt_used : 0) +
                    (orderCat.CareByOtherCat ? wt_care : 0) +
                    (orderCat.OpposeByOtherCat ? wt_oppose : 0) +
                    (orderCat.RecentUsed ? wt_recent : 0) +
                    (orderCat.OriginNoNeed ? wt_origin : 0);
                orderCat.Weight = Math.Clamp(orderCat.WeightRaw, wt_min, wt_max);

                int _accDffy = 0;
                var cat = Game.Manager.mergeItemMan.GetCategoryConfig(orderCat.Cat.Id);

                // 遍历链条内的全部物品 并排除ignore物品
                // 假设链条内排列靠后的棋子都被ignore了 后续循环将全部是无效计算
                // 为了算法容易理解 且计算量不大 此处暂不优化
                foreach (var item in cat.Progress)
                {
                    mgr.TryGetItemDifficulty(item, out _, out _realDffy);
                    if (itemCount.TryGetValue(item, out var _num))
                    {
                        _accDffy += _num * _realDffy;
                    }
                    // 排除已经在需求的item
                    if (!requireTable.Contains(item))
                    {
                        // 在忽略表内 且 等级达到激活等级
                        if (ignoreTable.TryGetValue(item, out var ignore) && mergeLevel >= ignore.ActiveLevel)
                        {
                            continue;
                        }
                        // 不在忽略表内
                        var careDffy = _CalcTotalCareGraphDffy(orderCat.Cat.CareGraphId);
                        var pay = _realDffy - _accDffy - careDffy;
                        var info = new CandidateItemInfo()
                        {
                            Id = item,
                            OriginGraphId = orderCat.Cat.OriginGraphId,
                            CategoryId = cat.Id,
                            CategoryWeight = orderCat.Weight,
                            AccDffy = _accDffy,
                            RealDffy = _realDffy,
                            PayDffy = pay < 0 ? 0 : pay,
                            CareDffy = careDffy,
                        };
                        candItemList.Add(info);
                    }
                }
            }

            // 按链条排序
            candItemList.Sort(_SortByCatId);
        }

        private void _FilterSingle()
        {
            foreach (var info in mCandItemList)
            {
                if (_check_range_pay_0(info.PayDffy) && _check_range_real_0(info.RealDffy))
                {
                    var group = ObjectPool<CandidateGroupInfo>.GlobalPool.Alloc();
                    group.ItemCount = 1;
                    group.Items[0] = info;
                    group.Weight = info.CategoryWeight;
                    mCandGroupList.Add(group);
                }
            }
        }

        private int _FindIndexForNextCatId(List<CandidateItemInfo> list, int startIdx, int catId)
        {
            for (int i = startIdx; i < list.Count; i++)
            {
                if (list[i].CategoryId != catId)
                {
                    return i;
                }
            }
            return list.Count;
        }

        private void _FilterDouble()
        {
            var sameOriginWt = mDataHolder.mergeBoardOrder.SameOriginWt;
            var list = mCandItemList;
            for (int aa = 0; aa < list.Count; aa++)
            {
                if (!_check_range_pay_1(list[aa].PayDffy) || !_check_range_real_1(list[aa].RealDffy))
                    continue;
                for (int bb = _FindIndexForNextCatId(list, aa + 1, list[aa].CategoryId); bb < list.Count; bb++)
                {
                    if (!_check_range_pay_1(list[bb].PayDffy) || !_check_range_real_1(list[bb].RealDffy))
                        continue;
                    if (_check_range_real_0(list[aa].RealDffy + list[bb].RealDffy))
                    {
                        var group = ObjectPool<CandidateGroupInfo>.GlobalPool.Alloc();
                        group.ItemCount = 2;
                        group.Items[0] = list[aa];
                        group.Items[1] = list[bb];
                        // 源相同
                        if (list[aa].OriginGraphId == list[bb].OriginGraphId)
                            group.Weight = sameOriginWt;
                        else
                            group.Weight = Math.Min(list[aa].CategoryWeight, list[bb].CategoryWeight);
                        mCandGroupList.Add(group);
                    }
                }
            }
        }

        private void _FilterTriple()
        {
            var sameOriginWt = mDataHolder.mergeBoardOrder.SameOriginWt;
            var list = mCandItemList;
            for (int aa = 0; aa < list.Count; aa++)
            {
                if (!_check_range_pay_2(list[aa].PayDffy) || !_check_range_real_2(list[aa].RealDffy))
                    continue;
                for (int bb = _FindIndexForNextCatId(list, aa + 1, list[aa].CategoryId); bb < list.Count; bb++)
                {
                    if (!_check_range_pay_2(list[bb].PayDffy) || !_check_range_real_2(list[bb].RealDffy))
                        continue;
                    for (int cc = _FindIndexForNextCatId(list, bb + 1, list[bb].CategoryId); cc < list.Count; cc++)
                    {
                        if (!_check_range_pay_2(list[cc].PayDffy) || !_check_range_real_2(list[cc].RealDffy))
                            continue;
                        if (_check_range_real_0(list[aa].RealDffy + list[bb].RealDffy + list[cc].RealDffy))
                        {
                            var group = ObjectPool<CandidateGroupInfo>.GlobalPool.Alloc();
                            group.ItemCount = 3;
                            group.Items[0] = list[aa];
                            group.Items[1] = list[bb];
                            group.Items[2] = list[cc];
                            // 源相同
                            if (list[aa].OriginGraphId == list[bb].OriginGraphId && list[aa].OriginGraphId == list[cc].OriginGraphId)
                                group.Weight = sameOriginWt;
                            else
                                group.Weight = UnityEngine.Mathf.Min(list[aa].CategoryWeight, list[bb].CategoryWeight, list[cc].CategoryWeight);
                            mCandGroupList.Add(group);
                        }
                    }
                }
            }
        }

        private CandidateGroupInfo _ChooseCandidateByWeight()
        {
            CandidateGroupInfo info = null;
            var list = mCandGroupList;
            var totalWeight = 0;
            for (int i = 0; i < list.Count; i++)
            {
                totalWeight += list[i].Weight;
            }
            var weight_sum = 0;
            var roll = _Random_Roll(totalWeight);
            for (int i = 0; i < list.Count; i++)
            {
                weight_sum += list[i].Weight;
                if (weight_sum >= roll)
                {
                    info = list[i];
                    break;
                }
            }

            DebugEx.Info($"OrderProviderRandom::_ChooseCandidateByWeight roll {roll}/{totalWeight}");
            return info;
        }

        private void _EnsureCategoryRefInOrder()
        {
            var catCache = mCategoryInOrderSet;
            catCache.Clear();

            using (ObjectPool<Dictionary<int, int>>.GlobalPool.AllocStub(out var allRequires))
            {
                // 统计所有随机订单的需求
                foreach (var order in mDataHolder.activeOrderList)
                {
                    foreach (var req in order.Requires)
                    {
                        if (allRequires.ContainsKey(req.Id))
                        {
                            allRequires[req.Id] += req.TargetCount;
                        }
                        else
                        {
                            allRequires.Add(req.Id, req.TargetCount);
                        }
                    }
                }

                // 找出全盘考虑后未能凑齐的需求 以链条的方式记录
                var itemCount = _GetCurrentActiveItemCount();
                foreach (var kv in allRequires)
                {
                    itemCount.TryGetValue(kv.Key, out var count);
                    if (count < kv.Value)
                    {
                        var cat = Merge.Env.Instance.GetCategoryByItem(kv.Key);
                        if (cat == null)
                        {
                            DebugEx.Error($"cat not found {kv.Key}");
                            continue;
                        }
                        catCache.Add(cat.Id);
                    }
                }
            }
        }

        private void _EnsureCandidateCategory()
        {
            var candCatDict = mCandCategoryDict;
            if (candCatDict.Count < 1)
            {
                // 收集合法的链条
                foreach (var catFilter in mDataHolder.categoryPoolList)
                {
                    if (mHelper.CheckStateByConditionGroup(catFilter.ActiveLevel,
                                                            catFilter.ShutdownLevel,
                                                            catFilter.ActiveOrderId,
                                                            catFilter.ShutdownOrderId,
                                                            catFilter.ActiveItemId,
                                                            catFilter.ShutdownItemId))
                    {
                        mCandCategoryDict.Add(catFilter.Id, new CandidateCategory()
                        {
                            Cat = catFilter,
                        });
                    }
                }
            }

            // 所有链条清除权重标记
            foreach (var kv in candCatDict)
            {
                var cand = kv.Value;
                cand.NeedByOrder = false;
                cand.CareByOtherCat = false;
                cand.OpposeByOtherCat = false;
                cand.RecentUsed = false;
                cand.OriginNoNeed = false;
            }

            // 标记
            var _requireRef = mCategoryInOrderSet;
            foreach (var kv in candCatDict)
            {
                var cand = kv.Value;
                if (_requireRef.Contains(cand.Cat.Id))
                {
                    // 1.链条有需求 标记
                    cand.NeedByOrder = true;
                    // 2.对每个前置链条 标记
                    foreach (var item in cand.Cat.CareGraphId)
                    {
                        if (candCatDict.ContainsKey(item))
                        {
                            candCatDict[item].CareByOtherCat = true;
                        }
                    }
                }
                else
                {
                    // 3.有任意oppo在ref表中的 标记
                    foreach (var item in cand.Cat.OpposeGraphId)
                    {
                        if (_requireRef.Contains(item))
                        {
                            cand.OpposeByOtherCat = true;
                            break;
                        }
                    }
                }
            }

            // 4.最近提交过的链条 标记
            foreach (var item in mCategoryRecentFinishedSet)
            {
                if (candCatDict.TryGetValue(item, out var cand))
                {
                    cand.RecentUsed = true;
                }
            }

            // 5.同源头 且 没有兄弟是自动链条 且 都没有require => 标记
            using (ObjectPool<Dictionary<int, bool>>.GlobalPool.AllocStub(out var cache))
            {
                // key 源链id / value 是否suc
                foreach (var kv in candCatDict)
                {
                    var cat = kv.Value.Cat;
                    if (cache.TryGetValue(cat.OriginGraphId, out var suc))
                    {
                        // 已经fail
                        if (!suc)
                            continue;
                        if (cat.IsAutoGraph || _requireRef.Contains(cat.Id))
                        {
                            cache[cat.OriginGraphId] = false;
                        }
                    }
                    else
                    {
                        if (cat.IsAutoGraph || _requireRef.Contains(cat.Id))
                        {
                            cache.Add(cat.OriginGraphId, false);
                        }
                        else
                        {
                            cache.Add(cat.OriginGraphId, true);
                        }
                    }
                }

                foreach (var kv in candCatDict)
                {
                    if (cache.TryGetValue(kv.Value.Cat.OriginGraphId, out var suc))
                    {
                        if (suc)
                        {
                            kv.Value.OriginNoNeed = true;
                        }
                    }
                }
            }
        }

        // 为api订单准备数据
        private bool _TryPrepareApiRequestData(OrderRandomer cfg)
        {
            if (!cfg.IsApiOrder)
                return false;
            // 是否使用api需要走白名单机制
            if (Game.Manager.configMan.globalConfig.IsOrderApiOnlyWhitelist && !Game.Manager.mergeItemMan.CheckFpIdInOrderApiWhiteList(Game.Manager.networkMan.fpId))
                return false;
            mCandGroupListForApi.Clear();
            mCandGroupListForApi.AddRange(mCandGroupList);
            return true;
        }

        private bool _TryApplyApiOrder(IOrderData order)
        {
            if (order.ShouldNotChange)
                return false;
            order.ShouldNotChange = true;

            if (!mDataHolder.orderConfigDict.TryGetValue(order.Id, out var cfg))
                return false;

            // 是否不使用api (请求流程完整走 仅不使用)
            if (!cfg.IsApiUse)
                return false;

            var reward = Game.Manager.mergeItemMan.GetOrderRewardConfig(_Remap(cfg.RewardGrpId)).Reward;
            if (!mRemoteOrderWrapper.TryApplyOrder(order, mHelper, reward, cfg.MinDiffRate))
            {
                // 订单api没准备好
                OrderUtility.SetOrderApiStatus(order, OrderApiStatus.Timeout);
                return false;
            }
            // 记录订单来自api
            OrderUtility.SetOrderApiStatus(order, OrderApiStatus.UseApi);
            // 刷新并且可能触发排序变动
            var changed = OrderUtility.UpdateOrderStatus(order as OrderData, mTracer, mHelper);
            if (changed)
            {
                mCacheChangedList.Clear();
                mCacheNewlyAddedList.Clear();
                mCacheChangedList.Add(order);
                mOnOrderListDirty?.Invoke(mCacheChangedList, mCacheNewlyAddedList);
            }
            DebugEx.Info($"[ORDERDEBUG] api order {order.Id} applied");
            return true;
        }

        private bool _ProcessOrder(List<IActivityOrderHandler> actHandlers, OrderData data)
        {
            var changed = OrderAttachmentUtility.TryRemoveInvalidEventData(data);
            changed = _ActivityHandler_OnPreUpdate(actHandlers, data) || changed;
            changed = OrderUtility.UpdateOrderStatus(data, mTracer, mHelper) || changed;
            changed = _ActivityHandler_OnPostUpdate(actHandlers, data) || changed;
            return changed;
        }

        #region random
        private Random random = new Random();
        /* [min, max] */
        private int _Random_Range(int min, int max)
        {
            return random.Next(min, max + 1);
        }
        /* [1, max] */
        private int _Random_Roll(int max)
        {
            return random.Next(max) + 1;
        }
        /* [0, max] */
        private int _Random_Split(int max)
        {
            return random.Next(max + 1);
        }
        /* [0, max-1] */
        private int _Random_Pick(int max)
        {
            return random.Next(max);
        }
        #endregion

        #region activity

        private void _CollectActivityHandler(int boardId, List<IActivityOrderHandler> container)
        {
            var all = Game.Manager.activity.map;
            foreach (var kv in all)
            {
                if ((kv.Value is IActivityOrderHandler handler) && kv.Value.Active && handler.IsValidForBoard(boardId))
                {
                    handler.HandlerCollected();
                    container.Add(handler);
                }
            }
        }

        private void _ActivityHandler_TryGeneratePassiveOrder(List<IActivityOrderHandler> container, OrderRandomer cfg, out OrderData order)
        {
            order = null;
            foreach (var handler in container)
            {
                if (handler is IActivityOrderGenerator generator)
                {
                    if (generator.TryGeneratePassiveOrder(cfg, mHelper, mTracer, _MakeOrder_Passive, out order))
                    {
                        return;
                    }
                }
            }
        }

        private bool _ActivityHandler_OnPreUpdate(List<IActivityOrderHandler> actHandlers, OrderData order)
        {
            var changed = false;
            foreach (var handler in actHandlers)
            {
                if (handler.OnPreUpdate(order, mHelper, mTracer))
                    changed = true;
            }
            return changed;
        }

        private bool _ActivityHandler_OnPostUpdate(List<IActivityOrderHandler> actHandlers, OrderData order)
        {
            var changed = false;
            foreach (var handler in actHandlers)
            {
                if (handler.OnPostUpdate(order, mHelper, mTracer))
                    changed = true;
            }
            return changed;
        }

        #endregion

        #region debug

        private void _DebugPrint_CandGroup_Item(StringBuilder sb, CandidateGroupInfo info)
        {
            sb.Append($"(N{info.ItemCount}|W{info.Weight}|");
            for (var i = 0; i < info.ItemCount; i++)
            {
                sb.Append($"{info.Items[i]},");
            }
            sb.Append(")");
        }

        // 链条组解析成链条
        private void _DebugPrint_CatBundle(StringBuilder sb, long catBundle)
        {
            int idx = 0;
            while (catBundle > 0)
            {
                if (idx != 0)
                {
                    sb.Append("^");
                }
                ++idx;
                var cat = catBundle % catBundleMod;
                catBundle /= catBundleMod;
                sb.Append($"{cat}");
            }
        }

        private void _DebugPrint_CandCategoryPool()
        {
#if UNITY_EDITOR
            var dict = mCandCategoryDict;
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                sb.Append($"[ORDERDEBUG] cand category total {dict.Count}");
                int idx = 0;
                foreach (var kv in dict)
                {
                    sb.AppendLine();
                    var cand = kv.Value;
                    sb.Append($"{cand.Cat.Id} W={cand.Weight} Self-{cand.NeedByOrder} Care-{cand.CareByOtherCat} Oppo-{cand.OpposeByOtherCat} Recent-{cand.RecentUsed} Origin-{cand.OriginNoNeed}");
                }
                DebugEx.Info($"OrderProviderRandom::_DebugPrint_CandCategoryPool ---> {sb}");
            }
#endif
        }

        private void _DebugPrint_CandItemPool()
        {
#if UNITY_EDITOR
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                var list = mCandItemList;
                sb.Append($"[ORDERDEBUG] cand item total {list.Count}");
                for (int idx = 0; idx < list.Count; idx++)
                {
                    var item = list[idx];
                    // if (idx % 4 == 0)
                    sb.AppendLine();
                    sb.Append($"{item.Id}|{item.CategoryId}|W{item.CategoryWeight}|A{item.AccDffy}|R{item.RealDffy}|P{item.PayDffy}|C{item.CareDffy}");
                }
                sb.AppendLine();
                DebugEx.Info($"OrderProviderRandom::_DebugPrint_CandItem ---> {sb}");
            }
#endif
        }

        private void _DebugPrint_CandGroup_GroupByWeight(List<CandidateGroupInfo> candList, List<int> weightList)
        {
#if UNITY_EDITOR
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                sb.Append($"[ORDERDEBUG] cand group total {candList.Count} / weight type {weightList.Count}");
                for (var wtIdx = 0; wtIdx < weightList.Count; wtIdx++)
                {
                    var w = weightList[wtIdx];
                    var count = 0;
                    sb.AppendLine();
                    sb.Append($"weight {w} begin");
                    foreach (var item in candList)
                    {
                        if (item.Weight == w)
                        {
                            if (count % 4 == 0)
                                sb.AppendLine();
                            ++count;
                            _DebugPrint_CandGroup_Item(sb, item);
                        }
                    }
                    sb.AppendLine();
                    sb.Append($"weight {w} end with {count}");
                }
                DebugEx.Info($"OrderProviderRandom::_DebugPrint_CandGroup_GroupByWeight ---> {sb}");
            }
#endif
        }

        private void _DebugPrint_CandGroup_GroupByCat(List<CandidateGroupInfo> candList, List<long> catBundleList)
        {
#if UNITY_EDITOR
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                sb.Append($"[ORDERDEBUG] candidate group total {candList.Count} / category combine type total {catBundleList.Count}");
                for (var idx = 0; idx < catBundleList.Count; idx++)
                {
                    var bundle = catBundleList[idx];
                    var count = 0;
                    sb.AppendLine();
                    sb.Append($"cat combine begin ");
                    _DebugPrint_CatBundle(sb, bundle);
                    foreach (var item in candList)
                    {
                        if (item.CatBundle == bundle)
                        {
                            if (count % 4 == 0)
                                sb.AppendLine();
                            ++count;
                            _DebugPrint_CandGroup_Item(sb, item);
                        }
                    }
                    sb.AppendLine();
                    sb.Append($"cat combine end with {count}");
                }
                DebugEx.Info($"OrderProviderRandom::_DebugPrint_CandGroup_GroupByCat ---> {sb}");
            }
#endif
        }

        private void _DebugPrint_CandGroup(List<CandidateGroupInfo> list, string tag)
        {
#if UNITY_EDITOR
            using (ObjectPool<StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                sb.Append($"[ORDERDEBUG] candidate group total {list.Count}");
                for (int idx = 0; idx < list.Count; idx++)
                {
                    if (idx % 4 == 0)
                        sb.AppendLine();
                    _DebugPrint_CandGroup_Item(sb, list[idx]);
                }
                DebugEx.Info($"OrderProviderRandom::_DebugPrint_CandGroup {tag} ---> {sb}");
            }
#endif
        }

        #endregion
    }
}