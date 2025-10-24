/**
 * @Author: handong.liu
 * @Date: 2021-02-25 17:49:53
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using FAT.Merge;
using Config;
using fat.rawdata;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class MergeItemMan : IGameModule
    {
        public class ItemBubbleSpawnData
        {
            public BubbleSpawn config;
            public int mergeCount;
        }

        private class MergeItemCategoryRuntimeConfig
        {
            public int id => config.Id;
            public string name => config.Name;
            public MergeItemCategory config;
        }

        public IDictionary<int, int> fixedCategoryOutputDB => Game.Manager.mergeBoardMan.globalData.FixedOutputId;
        public IDictionary<int, int> fixedItemOutputDB => Game.Manager.mergeBoardMan.globalData.FixedOutputByItemId;
        // public IDictionary<int, int> showCountDB => Game.Manager.mergeBoardMan.globalData.CategoryShowCount;
        private IDictionary<int, ObjTool> mToolBasicConfigMap;
        private IDictionary<int, ObjMergeTool> mToolMergeConfigMap;
        private IDictionary<int, MergeMixCost> mMergeMixCostMap;
        private IDictionary<int, MergeTapCost> mMergeTapCostMap;
        private IDictionary<int, DropLimitItem> mAllDropLimitItemConfigs;
        private IDictionary<int, ComTrigAutoDetail> mTrigAutoDetailConfigs;
        private IDictionary<int, OrderBoxDetail> mOrderBoxDetailMap;
        private IDictionary<int, OrderDiff> mOrderDiffMap;
        private IDictionary<int, OrderReward> mOrderRewardMap;
        private IDictionary<int, OrderCategory> mOrderCategoryMap;
        private IDictionary<string, OrderApiWhitelist> mOrderApiWhiteListMap;

        private Dictionary<int, ItemComConfig> mItemConfigs = new Dictionary<int, ItemComConfig>();
        private Dictionary<int, MergeRule> mRuleConfigs = new Dictionary<int, MergeRule>();
        // 全局固定产出 以链条为依据
        private Dictionary<int, MergeFixedOutput> mFixedOutputConfigs = new Dictionary<int, MergeFixedOutput>();
        // 全局固定产出 以具体item为依据
        private Dictionary<int, MergeFixedItem> mFixedOutputByItemConfigs = new Dictionary<int, MergeFixedItem>();
        private Dictionary<int, ItemBubbleSpawnData> mBubbleSpawnDatas = new Dictionary<int, ItemBubbleSpawnData>();
        //具有TapCost属性的物品id与其所有OutPuts产出的对应  key : 物品id value: MergeTapCost表中outputs字段各项的key
        private Dictionary<int, List<int>> mTapCostOutPutsDict = new Dictionary<int, List<int>>();

        private Dictionary<int, int> mItemCategoryMap = new Dictionary<int, int>();
        private Dictionary<int, (int cid, int level)> mItemCategoryLevelMap = new();
        private Dictionary<int, MergeItemCategoryRuntimeConfig> mCategoryConfigs = new Dictionary<int, MergeItemCategoryRuntimeConfig>();
        private Dictionary<int, int> mCategoryBoardId = new Dictionary<int, int>();     //key is category id, value is boardId
        private Dictionary<int, List<int>> mGridMatchItemsDict = new Dictionary<int, List<int>>();
        private IDictionary<int, GalleryCategory> mGalleryCategoryConfigs;
        private Dictionary<int, List<int>> _mMergeChainGroupByCategoryDict = new Dictionary<int, List<int>>();
        private Dictionary<int, List<int>> mMergeChainGroupByCategoryDict
        {
            get
            {
                if (_mMergeChainGroupByCategoryDict.Count < 1)
                {
                    foreach (var item in mCategoryConfigs)
                    {
                        var cat = item.Value.config;
                        if (_mMergeChainGroupByCategoryDict.TryGetValue(cat.GalleryCategory, out var value))
                        {
                            value.Add(cat.Id);
                        }
                        else
                        {
                            _mMergeChainGroupByCategoryDict.Add(cat.GalleryCategory, new List<int> { cat.Id });
                        }
                    }
                }
                return _mMergeChainGroupByCategoryDict;
            }
        }

        //获取到所有的页签id
        public int FillCollectionCategoryOrdered(List<int> container)
        {
            int ret = 0;
            using (ObjectPool<List<GalleryCategory>>.GlobalPool.AllocStub(out var rawContainer))
            {
                rawContainer.AddRange(mGalleryCategoryConfigs.Values);
                rawContainer.Sort((a, b) => a.Sort - b.Sort);
                foreach (var cat in rawContainer)
                {
                    container?.Add(cat.Id);
                    ret++;
                }
            }
            return ret;
        }

        //获取到指定页签id下所有的链条id
        public int FillSeriesInCategoryOrdered(int categoryId, List<int> container, bool includeHidden)
        {
            int ret = 0;
            using (ObjectPool<List<MergeItemCategoryRuntimeConfig>>.GlobalPool.AllocStub(out var rawContainer))
            {
                var group = mMergeChainGroupByCategoryDict.GetDefault(categoryId);
                if (group != null)
                {
                    foreach (var catId in group)
                    {
                        var cat = mCategoryConfigs.GetDefault(catId);
                        if (cat != null)
                        {
                            if (includeHidden || !cat.config.Hidden)
                            {
                                rawContainer.Add(cat);
                            }
                        }
                    }
                    rawContainer.Sort(_CategorySort);
                }
                foreach (var cat in rawContainer)
                {
                    container?.Add(cat.id);
                }
                ret = rawContainer.Count;
            }
            return ret;
        }

        private int _CategorySort(MergeItemCategoryRuntimeConfig a, MergeItemCategoryRuntimeConfig b)
        {
            if (a.config.Sort != b.config.Sort)
            {
                return a.config.Sort - b.config.Sort;
            }
            return a.config.Id - b.config.Id;
        }

        private void _OnConfigLoaded()
        {
            mGalleryCategoryConfigs = Game.Manager.configMan.GetGalleryCategoryConfigs();
            var autoSource = Game.Manager.configMan.GetComMergeAutoSourceConfigs();
            var bonusConfig = Game.Manager.configMan.GetComMergeBonusConfigs();
            var tapBonusConfig = Game.Manager.configMan.GetComTapBonusConfigs();
            var dyingConfig = Game.Manager.configMan.GetComMergeDyingConfigs();
            var timeSkipConfig = Game.Manager.configMan.GetComMergeTimeSkipConfigs();
            var boxConfg = Game.Manager.configMan.GetComMergeBoxConfigs();
            var skillConfig = Game.Manager.configMan.GetComMergeSkillConfigs();
            var featureConfig = Game.Manager.configMan.GetComMergeFeatureConfigs();
            var eatSourceConfig = Game.Manager.configMan.GetComMergeEatSourceConfigs();
            var eatConfig = Game.Manager.configMan.GetComMergeEatConfigs();
            var toolConfig = Game.Manager.configMan.GetComMergeToolSourceConfigs();
            var orderBoxConfig = Game.Manager.configMan.GetComMergeOrderBoxConfigs();
            var jumpCDConfig = Game.Manager.configMan.GetComMergeJumpCDConfigs();
            var specialBoxConfig = Game.Manager.configMan.GetComMergeSpecialBoxConfigs();
            var choiceBoxConfig = Game.Manager.configMan.GetComMergeChoiceBoxConfigs();
            var mixSourceConfig = Game.Manager.configMan.GetComMergeMixSourceConfigs();
            var trigAutoSourceConfig = Game.Manager.configMan.GetComTrigAutoSourceConfigs();
            var activeSourceConfig = Game.Manager.configMan.GetComMergeActiveSourceConfigs();
            var tokenMultiConfig = Game.Manager.configMan.GetComMergeTokenMultiplierConfigs();
            mItemConfigs.Clear();

            foreach (var c in autoSource)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.autoSourceConfig = c;
            }
            foreach (var c in bonusConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.bonusConfig = c;
            }
            foreach (var c in tapBonusConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.tapBonusConfig = c;
            }
            foreach (var c in dyingConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.dyingConfig = c;
            }
            foreach (var c in eatSourceConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.eatSourceConfig = c;
            }
            foreach (var c in timeSkipConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.timeSkipConfig = c;
            }
            foreach (var c in boxConfg)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.boxConfig = c;
            }
            foreach (var c in skillConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.skillConfig = c;
            }
            foreach (var c in featureConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.featureConfig = c;
            }
            foreach (var c in eatConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.eatConfig = c;
            }
            foreach (var c in toolConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.toolSource = c;
            }
            foreach (var c in orderBoxConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.orderBoxConfig = c;
            }
            foreach (var c in jumpCDConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.jumpCDConfig = c;
            }
            foreach (var c in specialBoxConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.specialBoxConfig = c;
            }
            foreach (var c in choiceBoxConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.choiceBoxConfig = c;
            }
            foreach (var c in mixSourceConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.mixSourceConfig = c;
            }
            foreach (var c in trigAutoSourceConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.trigAutoSourceConfig = c;
            }
            foreach (var c in activeSourceConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.activeSourceConfig = c;
            }
            foreach (var c in tokenMultiConfig)
            {
                var config = _GetOrCreateItemConfig(c.Id, true);
                config.tokenMultiConfig = c;
            }

            var fixedConfg = Game.Manager.configMan.GetMergeFixedOutputConfigs();
            foreach (var c in fixedConfg)
            {
                mFixedOutputConfigs[c.CategoryId] = c;
            }
            var fixedOutputByItem = Game.Manager.configMan.GetMergeFixedOutputByItemConfigs();
            foreach (var c in fixedOutputByItem)
            {
                mFixedOutputByItemConfigs[c.ItemId] = c;
            }
            var ruleConfig = Game.Manager.configMan.GetMergeRuleConfigs();
            foreach (var c in ruleConfig)
            {
                mRuleConfigs[c.Id] = c;
            }

            mToolBasicConfigMap = Game.Manager.configMan.GetObjToolMap();
            mToolMergeConfigMap = Game.Manager.configMan.GetObjMergeToolMap();
            mMergeTapCostMap = Game.Manager.configMan.GetMergeTapCostMap();
            mMergeMixCostMap = Game.Manager.configMan.GetMergeMixCostMap();
            mAllDropLimitItemConfigs = Game.Manager.configMan.GetDropLimitItemMap();
            mTrigAutoDetailConfigs = Game.Manager.configMan.GetComTrigAutoDetailConfigs();
            mOrderBoxDetailMap = Game.Manager.configMan.GetOrderBoxDetailMap();
            mOrderDiffMap = Game.Manager.configMan.GetOrderDiffConfigMap();
            mOrderRewardMap = Game.Manager.configMan.GetOrderRewardConfigMap();
            mOrderCategoryMap = Game.Manager.configMan.GetOrderCategoryMap();
            mOrderApiWhiteListMap = Game.Manager.configMan.GetOrderApiWhiteListMap();

            OnMergeBoardVersionUpdate(0);
        }

        #region 生成器丢失检查
        // 活动需要 持有状况检查 的链条ID
        public List<int> GetAliveCheckChain()
        {
            var aliveCheckChain = new List<int>();
            foreach (var (key, value) in mCategoryConfigs)
            {
                if (value.config.IsAliveCheck)
                {
                    aliveCheckChain.Add(key);
                }
            }

            return aliveCheckChain;
        }
        #endregion
        

        private int mPreviousVersion = -1;
        private int mPreviousTapSourceVersion = -1;
        private int mPreviousCategoryVersion = -1;
        public void OnMergeBoardVersionUpdate(int version)
        {
            var _dict = mGridMatchItemsDict;
            _dict.Clear();
            var gridConfigs = Game.Manager.configMan.GetMergeGridConfigs();
            // prepare
            foreach (var grid in gridConfigs)
            {
                if (!_dict.ContainsKey(grid.Id))
                {
                    _dict.Add(grid.Id, new List<int>());
                }
            }
            // fill
            var itemConfigs = Game.Manager.configMan.GetObjMergeItemConfigs(version, out var _);
            foreach (var item in itemConfigs)
            {
                foreach (var gid in item.MergeGrid)
                {
                    _dict[gid].Add(item.Id);
                }
            }
            // sort
            foreach (var items in _dict.Values)
            {
                items.Sort();
            }

            var chestConfig = Game.Manager.configMan.GetComMergeChestConfigs(version, out var realVersion);
            if (realVersion != mPreviousVersion)
            {
                mPreviousVersion = realVersion;
                //unset all chest config first
                foreach (var itemComConfig in mItemConfigs.Values)
                {
                    itemComConfig.chestConfig = null;
                }
                foreach (var c in chestConfig)
                {
                    var config = _GetOrCreateItemConfig(c.Id, true);
                    config.chestConfig = c;
                }
            }
            var clickSourceConfig = Game.Manager.configMan.GetComMergeClickSourceConfigs(version, out realVersion);
            if (realVersion != mPreviousTapSourceVersion)
            {
                mPreviousTapSourceVersion = realVersion;
                mTapCostOutPutsDict.Clear();
                //unset all chest config first
                foreach (var itemComConfig in mItemConfigs.Values)
                {
                    itemComConfig.clickSourceConfig = null;
                }

                var tapCostMap = Game.Manager.configMan.GetMergeTapCostMap();
                foreach (var c in clickSourceConfig)
                {
                    var config = _GetOrCreateItemConfig(c.Id, true);
                    config.clickSourceConfig = c;
                    if (!_ValidateItemSourceFixedOutput(c))
                    {
                        break;
                    }
                    //构造clickSource的产出map
                    foreach (var costId in c.CostId)
                    {
                        var tapCost = tapCostMap.GetDefault(costId);
                        if (tapCost != null && tapCost.Outputs.Count > 0)
                        {
                            if (mTapCostOutPutsDict.TryGetValue(c.Id, out var tempList))
                            {
                                tempList.AddRange(tapCost.Outputs.Keys);
                            }
                            else
                            {
                                tempList = new List<int>();
                                tempList.AddRange(tapCost.Outputs.Keys);
                                mTapCostOutPutsDict.Add(c.Id, tempList);
                            }
                        }
                    }
                }
            }
            var categoryConfig = Game.Manager.configMan.GetMergeCategoryConfigs(version, out realVersion);
            if (mPreviousCategoryVersion != realVersion)
            {
                mPreviousCategoryVersion = realVersion;
                mItemCategoryMap.Clear();
                mItemCategoryLevelMap.Clear();
                mCategoryConfigs.Clear();
                foreach (var cat in categoryConfig)
                {
                    for (int i = 0; i < cat.Progress.Count; ++i)
                    {
                        mItemCategoryMap[cat.Progress[i]] = cat.Id;
                        mItemCategoryLevelMap[cat.Progress[i]] = (cat.Id, i);
                    }
                    mCategoryConfigs[cat.Id] = new MergeItemCategoryRuntimeConfig() { config = cat };
                    var itemConfig = Game.Manager.objectMan.GetMergeItemConfig(cat.Progress[0]);
                    if (itemConfig != null)
                    {
                        mCategoryBoardId[cat.Id] = itemConfig.BoardId;
                    }
                }
            }
        }

        public int FillMatchItemByGridTemplate(int tid, List<int> container = null)
        {
            if (mGridMatchItemsDict.TryGetValue(tid, out var list))
            {
                container?.AddRange(list);
                return list.Count;
            }
            return 0;
        }

        public int GetExpItemByCount(int count)
        {
            return GetBonusItemByCount(Constant.kMergeExpObjId, count);
        }

        public int GetBonusItemByCount(int id, int count)
        {
            var bonusConfig = Game.Manager.configMan.GetComMergeBonusConfigs();
            int winner = 0;
            int winnerCount = 0;
            foreach (var c in bonusConfig)
            {
                if (c.BonusId == id && c.BonusCount <= count && winnerCount < c.BonusCount)
                {
                    winner = c.Id;
                    winnerCount = c.BonusCount;
                }
            }
            return winner;
        }

        public ItemComConfig GetItemComConfig(int id)
        {
            return _GetOrCreateItemConfig(id, false);
        }

        public MergeItemCategory GetCategoryConfig(int id)
        {
            return mCategoryConfigs.GetDefault(id, null)?.config;
        }

        public MergeFixedOutput GetFixedOutputConfig(int categoryId)
        {
            return mFixedOutputConfigs.GetDefault(categoryId, null);
        }

        public MergeFixedItem GetFixedOutputByItemConfig(int itemId)
        {
            return mFixedOutputByItemConfigs.GetDefault(itemId, null);
        }

        public MergeRule GetMergeRuleByItem(int itemId)
        {
            return mRuleConfigs.GetDefault(itemId, null);
        }

        // public void RefreshItemUnlockState()
        // {
        //     _RefreshItemUnlockState(Game.Instance.archiveMan.isArchiveLoaded);
        // }

        //传入物品id 返回其所在合成链的下n等级(n默认为1)的物品id 找不到时返回curItemId
        public int GetNextLevelItemId(int curItemId, int nextLevel = 1)
        {
            GetItemCategoryIdAndLevel(curItemId, out var cid, out var level);
            int targetLevel = level + nextLevel + 1;    //传入的等级默认从1开始
            GetClampedChainItemIdByLevel(cid, targetLevel, out var nextItemId);
            return nextItemId;
        }

        public MergeItemCategory GetCategoryConfigByItemId(int itemId)
        {
            return GetCategoryConfig(GetItemCategoryId(itemId));
        }

        public void OnItemShow(int itemId)
        {
            //检查是否是活动类棋盘专属棋子 是的话就立即刷新图鉴相关数据
            var isActivityBoardItem = Game.Manager.miniBoardMan.CheckIsMiniBoardItem(itemId)
                || Game.Manager.miniBoardMultiMan.CheckIsMiniBoardItem(itemId)
                || Game.Manager.mineBoardMan.CheckIsMineBoardItem(itemId);
            //若已经是活动类棋盘 则无需检查
            if (!isActivityBoardItem)
            {
                var allActivity = Game.Manager.activity.map;
                foreach (var (_, activity) in allActivity)
                {
                    if (activity is IBoardActivityHandbook boardActivity)
                    {
                        isActivityBoardItem = boardActivity.CheckIsBoardItem(itemId);
                        break;
                    }
                }
            }
            Game.Manager.handbookMan.UnlockHandbookItem(itemId, isActivityBoardItem);
        }

        //判断传入的物品id是否在对应合成链中是最后一个 找不到合成链时也返回true
        public bool IsLastItemInChain(int itemId)
        {
            var cat = GetCategoryConfigByItemId(itemId);
            if (cat != null)
            {
                var idx = cat.Progress.IndexOf(itemId);
                return idx == cat.Progress.Count - 1;
            }
            else
            {
                return true;
            }
        }

        //获取合成链中目前在图鉴中已解锁的最高等级的物品id
        public int GetMaxUnlockLevelItemIdInChain(int chainId, int limitLevel = 0, bool useDefault = true)
        {
            int maxLevelId = 0;
            var handbookMan = Game.Manager.handbookMan;
            var categoryConfig = GetCategoryConfig(chainId);
            if (categoryConfig != null)
            {
                var progress = categoryConfig.Progress;
                int startIndex = limitLevel <= 0 ? progress.Count : limitLevel;
                for (int i = startIndex - 1; i >= 0; i--)
                {
                    int itemId = progress[i];
                    if (handbookMan.IsItemUnlocked(itemId))
                    {
                        maxLevelId = itemId;
                        break;
                    }
                }
            }
            //没找到时(该条链都没解锁 一般不会出现) 返回该条链中第一个物品id
            if (maxLevelId <= 0 && useDefault)
                categoryConfig?.Progress.TryGetByIndex(0, out maxLevelId);
            return maxLevelId;
        }

        //获取指定链条中指定等级的棋子id  传入的等级默认从1开始
        public int GetChainItemIdByLevel(int chainId, int targetLevel, out bool isLevelMax)
        {
            isLevelMax = false;
            var categoryConfig = GetCategoryConfig(chainId);
            if (categoryConfig != null)
            {
                if (categoryConfig.Progress.TryGetByIndex(targetLevel - 1, out var itemId))
                {
                    isLevelMax = categoryConfig.Progress.Count <= targetLevel;
                    return itemId;
                }
            }
            return 0;
        }

        /// <summary>
        /// 获取指定链条中指定等级的棋子id
        /// </summary>
        /// <param name="chainId">链条id</param>
        /// <param name="targetLevel">目标等级 从1开始</param>
        /// <param name="resultId">结果id</param>
        /// <returns>是否未被裁剪</returns>
        public bool GetClampedChainItemIdByLevel(int chainId, int targetLevel, out int resultId)
        {
            var cat = GetCategoryConfig(chainId);
            if (cat.Progress.Count >= targetLevel)
            {
                resultId = cat.Progress[targetLevel - 1];
                return true;
            }
            else
            {
                resultId = cat.Progress[^1];
                return false;
            }
        }

        public bool TryIncMergeTestSpawnBubbleCount(int tid)
        {
            if (mBubbleSpawnDatas.TryGetValue(tid, out var data))
            {
                data.mergeCount++;
                DebugEx.FormatInfo("MergeItemMan::TestMergeSpawnBubble ----> tid {0} merge count add to {1}", tid, data.mergeCount);
                foreach (var count in data.config.SpawnCount)
                {
                    if (data.mergeCount == count)
                    {
                        DebugEx.FormatInfo("MergeItemMan::TestMergeSpawnBubble ----> tid {0} will output bubble:{1}", tid, data.config);
                        return true;
                    }
                }
            }
            return false;
        }

        public int GetItemCategoryId(int itemId)
        {
            return mItemCategoryMap.GetDefault(itemId, 0);
        }

        public Dictionary<int, int> GetItemCategoryMap()
        {
            return mItemCategoryMap;
        }

        /// <summary>
        /// 注意这里获取到的等级从0开始
        /// </summary>
        public void GetItemCategoryIdAndLevel(int itemId, out int cid, out int itemLevel)
        {
            if (mItemCategoryLevelMap.TryGetValue(itemId, out var info)) { }
            {
                cid = info.cid;
                itemLevel = info.level;
            }
        }

        public GalleryCategory GetGalleryCategoryConfigById(int id)
        {
            return mGalleryCategoryConfigs.GetDefault(id);
        }

        public IDictionary<int, ObjTool> GetToolConfigMap()
        {
            return mToolBasicConfigMap;
        }

        public ObjTool GetToolBasicConfig(int id)
        {
            return mToolBasicConfigMap.GetDefault(id);
        }

        public ObjMergeTool GetToolMergeConfig(int id)
        {
            return mToolMergeConfigMap.GetDefault(id);
        }

        public MergeMixCost GetMergeMixCostConfig(int id)
        {
            return mMergeMixCostMap.GetDefault(id);
        }

        public MergeTapCost GetMergeTapCostConfig(int id)
        {
            return mMergeTapCostMap.GetDefault(id);
        }

        public bool TryGetDropLimitItemConfig(int tid, out DropLimitItem cfg)
        {
            return mAllDropLimitItemConfigs.TryGetValue(tid, out cfg);
        }

        public bool TryGetTrigAutoDetailConfig(int detailId, out ComTrigAutoDetail cfg)
        {
            return mTrigAutoDetailConfigs.TryGetValue(detailId, out cfg);
        }

        //传入物品id 返回这个物品tapSource对应的所有产出 没有则返回null
        public IList<int> GetMergeTapCostOutPuts(int itemId)
        {
            return mTapCostOutPutsDict.GetDefault(itemId);
        }

        public OrderBoxDetail GetOrderBoxDetailConfig(int id)
        {
            return mOrderBoxDetailMap.GetDefault(id);
        }

        public OrderDiff GetOrderDiffConfig(int id)
        {
            return mOrderDiffMap.GetDefault(id);
        }

        public OrderReward GetOrderRewardConfig(int id)
        {
            return mOrderRewardMap.GetDefault(id);
        }

        public OrderCategory GetOrderCategoryConfig(int id)
        {
            return mOrderCategoryMap.GetDefault(id);
        }

        public bool CheckFpIdInOrderApiWhiteList(string fpid)
        {
            return mOrderApiWhiteListMap.ContainsKey(fpid);
        }

        private static readonly ItemComConfig kSharedEmptyComConfig = new ItemComConfig();
        private ItemComConfig _GetOrCreateItemConfig(int id, bool create)
        {
            if (!mItemConfigs.TryGetValue(id, out var config) && create)
            {
                config = new ItemComConfig();
                mItemConfigs[id] = config;
            }
            if (config != null)
            {
                return config;
            }
            else
            {
                return kSharedEmptyComConfig;
            }
        }

        // private List<int> mCachedNewlyUnlockedItem = new List<int>();
        // private void _RefreshItemUnlockState(bool notice = true)
        // {
        //     mItemUnlockMask.Clear();
        //     mCachedNewlyUnlockedItem.Clear();
        //     mItemUnlockMask.AddRange(mItemRewardMask);              //all item that is rewarded is unlocked!
        //     var world = Game.Instance.mergeWorldMan.world;
        //     int currentBoardOrder = Game.Instance.mergeWorldMan.GetBoardOrder(Game.Instance.mergeWorldMan.currentBoardId);
        //     foreach(var cat in mCategoryConfigs.Values)
        //     {
        //         int count = world.GetItemShowCountInCategory(cat.Id);
        //         int boardIdOrder = Game.Instance.mergeWorldMan.GetBoardOrder(mCategoryBoardId.GetDefault(cat.Id, 0));
        //         if(boardIdOrder >= 0 && boardIdOrder < currentBoardOrder)
        //         {
        //             count = cat.Progress.Count;
        //         }
        //         _SetCategoryUnlocked(cat.Id, count);
        //     }
        //     var orders = ObjectPool<List<MergeOrder>>.GlobalPool.Alloc();
        //     orders.Clear();
        //     Game.Instance.schoolMan.FillActiveOrders(orders);
        //     foreach(var order in orders)
        //     {
        //         if(order.itemIds != null)
        //         {
        //             foreach(var id in order.itemIds)
        //             {
        //                 _SetItemUnlocked(id);
        //             }
        //         }
        //     }
        //     orders.Clear();
        //     ObjectPool<List<MergeOrder>>.GlobalPool.Free(orders);
        //     if(notice)
        //     {
        //         _CheckDispatchItemUnlockEvent();
        //     }
        //     else
        //     {
        //         mCachedNewlyUnlockedItem.Clear();
        //     }
        // }

        private void _ItemIdToMask(int tid, out int idx, out ulong mask)
        {
            var order = tid - Constant.kMergeItemIdBase - 1;
            if (order < 0 || order >= Constant.kObjIdCapacity)
            {
                idx = 0;
                mask = 0;
                DebugEx.FormatWarning("MergeItemMan::_ItemIdToMask ----> illigal id {0}", tid);
                return;
            }
            const int kBitSize = sizeof(ulong) * 8;
            idx = order / kBitSize;
            mask = (ulong)1 << (order % kBitSize);
        }

        private int _MaskToItemId(List<int> container, int idx, ulong mask)
        {
            const int kBitSize = sizeof(ulong) * 8;
            int id = Constant.kMergeItemIdBase + idx * kBitSize + 1;
            int ret = 0;
            while (mask > 0)
            {
                if ((mask & 1) == 1)
                {
                    if (container != null)
                    {
                        container.Add(id);
                    }
                    ret++;
                }
                mask >>= 1;
                id++;
            }
            return ret;
        }

        // private void _SetCategoryUnlocked(int categoryId, int count)
        // {
        //     var cat = GetCategoryConfig(categoryId);
        //     for(int i = 0; i < count && i < cat.Progress.Count; i++)
        //     {
        //         var itemId = cat.Progress[i];
        //         if(!_SetItemUnlockedInner(itemId)) 
        //         {
        //             mCachedNewlyUnlockedItem.AddIfAbsent(itemId);
        //         }
        //     }
        // }

        // private void _SetItemUnlocked(int itemId)
        // {
        //     if(GetItemCategoryId(itemId) > 0)
        //     {
        //         if(!_SetItemUnlockedInner(itemId)) 
        //         {
        //             mCachedNewlyUnlockedItem.AddIfAbsent(itemId);
        //         }
        //     }
        // }

        // private void _CheckDispatchItemUnlockEvent()
        // {
        //     if(mCachedNewlyUnlockedItem.Count > 0)
        //     {
        //         DebugEx.FormatInfo("MergeItemMan::_CheckDispatchItemUnlockEvent ----> {0}", mCachedNewlyUnlockedItem);
        //         foreach(var item in mCachedNewlyUnlockedItem)
        //         {
        //             if(_IsItemHasUnlockReward(item))
        //             {
        //                 mGalleryHasUnread = true;
        //                 break;
        //             }
        //         }
        //         DebugEx.FormatTrace("MergeItemMan::_CheckDispatchItemUnlockEvent ----> {0}", mCachedNewlyUnlockedItem);
        //         MessageCenter.Get<MSG.GAME_HANDBOOK_UNLOCK_ITEM>().Dispatch(mCachedNewlyUnlockedItem);
        //         mCachedNewlyUnlockedItem.Clear();
        //     }
        // }

        // private bool _SetItemUnlockedInner(int tid)          //return previous state
        // {
        //     _ItemIdToMask(tid, out var idx, out var mask);
        //     while(mItemUnlockMask.Count <= idx)
        //     {
        //         mItemUnlockMask.Add(0);
        //     }
        //     bool previous = (mItemUnlockMask[idx] & mask) == mask;
        //     mItemUnlockMask[idx] = mItemUnlockMask[idx] | mask;
        //     return previous;
        // }

        // private void _SetItemRewarded(int tid)
        // {
        //     _ItemIdToMask(tid, out var idx, out var mask);
        //     while(mItemRewardMask.Count <= idx)
        //     {
        //         mItemRewardMask.Add(0);
        //     }
        //     mItemRewardMask[idx] = mItemRewardMask[idx] | mask;
        // }

        private bool _ValidateItemSourceFixedOutput(ComMergeTapSource config)
        {
            if (config.OutputsFixedTime.Count < config.OutputsFixed.Count * 2)
            {
                // DreamMerge.UIUtility.ShowMessageBoxTop(string.Format("生成器{0}的随机产出序列配置错误", config.Id), () =>
                // {
                //     // save any game data here
                //     GameProcedure.QuitGame();
                // });
                throw new System.Exception(string.Format("生成器{0}的随机产出序列配置错误", config.Id));
            }
            else
            {
                return true;
            }
        }

        void IGameModule.Reset()
        {
            mItemConfigs.Clear();
            mRuleConfigs.Clear();
            mFixedOutputConfigs.Clear();
            mFixedOutputByItemConfigs.Clear();
            mBubbleSpawnDatas.Clear();
            mTapCostOutPutsDict.Clear();
            mItemCategoryMap.Clear();
            mItemCategoryLevelMap.Clear();
            mCategoryConfigs.Clear();
            mCategoryBoardId.Clear();
            mGridMatchItemsDict.Clear();
            mGalleryCategoryConfigs?.Clear();
            _mMergeChainGroupByCategoryDict.Clear();

            mPreviousVersion = -1;
            mPreviousTapSourceVersion = -1;
            mPreviousCategoryVersion = -1;
        }

        void IGameModule.LoadConfig() { _OnConfigLoaded(); }

        void IGameModule.Startup() { }
    }
}