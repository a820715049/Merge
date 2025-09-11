/*
 * @Author: tang.yan
 * @Description: 棋子信息(来源与产出)管理器 用于整合各个系统数据 为界面提供方法
 * @Doc: 来源与产出 https://centurygames.yuque.com/ywqzgn/ne0fhm/reguc2wcte2uyp3n
 * @Doc: 吃与消耗 https://centurygames.yuque.com/ywqzgn/ne0fhm/oxmca3gyucycga0i
 * @Date: 2023-11-24 10:11:31
 */

using System.Collections.Generic;
using System.Linq;
using EL;
using fat.rawdata;
using Google.Protobuf.Collections;
using UnityEngine;

namespace FAT
{
    public class ItemInfoMan : IGameModule
    {
        //物品信息数据
        public class ItemInfoData
        {
            public int ItemId;      //id
            public ItemInfoType Type;    //物品信息类型
            public int ChainId;     //所属合成链id
            public int ItemLevel;   //在合成链中的等级 从1开始
            public bool IsLevelMax;  //是否是链中的满级物品
            public bool IsHideProduce;  //当棋子具备产出属性时，是否隐藏其产出内容
            public int OriginChainId;       //初始来源链条id
            public int OriginChainLevel;    //初始来源链内等级 链内等级 > 0，代表这条链中某个特定等级才是来源  = 0 代表链内任意等级都是来源
            public int DirectChainId;       //直接来源链条id
            public int DirectChainLevel;    //直接来源链内等级 链内等级 > 0，代表这条链中某个特定等级才是来源  = 0 代表链内任意等级都是来源
            public bool CanJumpShop;    //是否可以跳转到商店
            public int EmptyIndex;     //不显示棋子的cellIndex
            public bool CanShowBoost;  //此棋子是否要显示能量加倍相关UI
            public long RemainTime;    //剩余时间

            public void Clear()
            {
                ItemId = 0;
                Type = ItemInfoType.Normal;
                ChainId = 0;
                ItemLevel = 0;
                IsLevelMax = false;
                CanJumpShop = false;
                IsHideProduce = false;
                OriginChainId = 0;
                OriginChainLevel = 0;
                DirectChainId = 0;
                DirectChainLevel = 0;
                EmptyIndex = -1;
                TipsDataList?.Clear();
                TipsDataList = null;
                CanShowBoost = false;
                RemainTime = 0;
            }
            //存储当前物品的合成链数据 用于界面显示
            public List<ItemChainTipsData> TipsDataList;
            //外部调用时生成
            public void GenerateTipsDataList()
            {
                if (TipsDataList != null)
                    return;
                TipsDataList = new List<ItemChainTipsData>();
                Game.Manager.itemInfoMan.FillTipsItemDataList(ItemId, TipsDataList);
            }
        }

        //棋子信息界面展示类型 用于区别普通棋子和特殊棋子
        public enum ItemInfoType
        {
            Normal = 0,        //普通棋子(包含泡泡棋子)
            FrozenItem = 1,    //冰冻棋子
        }
        
        //当前正在展示的物品数据
        public ItemInfoData CurShowItemData = new ItemInfoData();

        //外部调用展示棋子信息界面
        public void TryOpenItemInfo(int itemId, ItemInfoType type = ItemInfoType.Normal, params object[] paramList)
        {
            if (_RefreshCurShowItemData(itemId, type, paramList))
            {
                UIManager.Instance.OpenWindow(UIConfig.UIItemInfo);
            }
        }

        //检查传入的itemId是否是当前棋子的产出物
        public bool CheckIsCurOutputItem(int checkItemId)
        {
            int itemId = CurShowItemData.ItemId;
            var mergeItemMan = Game.Manager.mergeItemMan;
            var itemCompConfig = mergeItemMan.GetItemComConfig(itemId);
            var clickSourceConfig = itemCompConfig?.clickSourceConfig;
            var autoSourceConfig = itemCompConfig?.autoSourceConfig;
            var chestConfig = itemCompConfig?.chestConfig;
            var toolSourceConfig = itemCompConfig?.toolSource;
            var mixSourceConfig = itemCompConfig?.mixSourceConfig;
            if (CurShowItemData.IsHideProduce)
            {
                return false;
            }
            else
            {
                if (clickSourceConfig != null)
                {
                    var check = mergeItemMan.GetMergeTapCostOutPuts(itemId)?.Contains(checkItemId) ?? false;
                    var checkFixed = clickSourceConfig.OutputsFixed.Contains(checkItemId);
                    return check || checkFixed;
                }
                else if (autoSourceConfig != null)
                {
                    var check = autoSourceConfig.Outputs?.Contains(checkItemId) ?? false;
                    var checkFixed = autoSourceConfig.OutputsFixed.Contains(checkItemId);
                    return check || checkFixed;
                }
                else if (chestConfig != null)
                {
                    return chestConfig.Outputs?.Contains(checkItemId) ?? false;
                }
                else if (toolSourceConfig != null)
                {
                    return toolSourceConfig.DropInfo?.Contains(checkItemId) ?? false;
                }
                else if (mixSourceConfig != null)
                {
                    foreach (var mixId in mixSourceConfig.MixId)
                    {
                        var cfg = Merge.Env.Instance.GetMergeMixCostConfig(mixId);
                        if (cfg.Outputs.ContainsKey(checkItemId))
                            return true;
                    }
                    return false;
                }
            }
            return false;
        }

        public RepeatedField<int> GetCurItemChainProgress()
        {
            var categoryConfig = Game.Manager.mergeItemMan.GetCategoryConfig(CurShowItemData.ChainId);
            return categoryConfig?.Progress;
        }

        public (bool, string) CheckCanShowProduceTitle()
        {
            int itemId = CurShowItemData.ItemId;
            var mergeItemMan = Game.Manager.mergeItemMan;
            var itemCompConfig = mergeItemMan.GetItemComConfig(itemId);
            var clickSourceConfig = itemCompConfig?.clickSourceConfig;
            var autoSourceConfig = itemCompConfig?.autoSourceConfig;
            var chestConfig = itemCompConfig?.chestConfig;
            var toolSourceConfig = itemCompConfig?.toolSource;
            var mixSourceConfig = itemCompConfig?.mixSourceConfig;
            if (CurShowItemData.IsHideProduce)
            {
                return (false, "");
            }
            else
            {
                string title = "";
                bool canShow = false;
                if (clickSourceConfig != null || autoSourceConfig != null || toolSourceConfig != null || mixSourceConfig != null)
                {
                    title = I18N.Text("#SysComDesc32");
                    canShow = true;
                }
                else if (chestConfig != null)
                {
                    title = I18N.Text("#SysComDesc33");
                    canShow = true;
                }
                return (canShow, title);
            }
        }

        public bool CheckItemIsNeedInOrder(int itemId)
        {
            using (ObjectPool<List<IOrderData>>.GlobalPool.AllocStub(out var allOrderDataList))
            {
                BoardViewWrapper.FillBoardOrder(allOrderDataList);
                foreach (var orderData in allOrderDataList)
                {
                    foreach (var itemInfo in orderData.Requires)
                    {
                        if (itemId == itemInfo.Id)
                            return true;
                    }
                }
            }
            return false;
        }

        public (bool, int) CheckIsSellInShop(int itemId)
        {
            var shopItemData = Game.Manager.shopMan.TryGetChessOrderDataById(itemId);
            if (shopItemData != null)
            {
                return (shopItemData.CheckCanBuy(), shopItemData.CurSellGoodsPrice);
            }
            else
            {
                return (false, 0);
            }
        }

        public void TryBuyShopChessGoods(int itemId, Vector3 flyFromPos)
        {
            var shopItemData = Game.Manager.shopMan.TryGetChessOrderDataById(itemId);
            if (shopItemData != null)
            {
                Game.Manager.shopMan.TryBuyShopChessOrderGoods(shopItemData, flyFromPos, 128f);
            }
        }

        public void FillItemCellDataGroupList(List<List<int>> cellDataGroupList, out int panelSize)
        {
            panelSize = -1;
            int itemId = CurShowItemData.ItemId;
            if (itemId <= 0 || cellDataGroupList == null)
                return;
            var mergeItemMan = Game.Manager.mergeItemMan;
            var categoryConfig = mergeItemMan.GetCategoryConfig(CurShowItemData.ChainId);
            var itemCompConfig = mergeItemMan.GetItemComConfig(itemId);
            if (categoryConfig == null || itemCompConfig == null)
                return;
            var chainProgress = categoryConfig.Progress;
            var clickSourceConfig = itemCompConfig.clickSourceConfig;
            var autoSourceConfig = itemCompConfig.autoSourceConfig;
            var chestConfig = itemCompConfig.chestConfig;
            var toolSourceConfig = itemCompConfig.toolSource;
            var mixSourceConfig = itemCompConfig.mixSourceConfig;
            //棋子具有任意产出属性时 选用大面板
            if (!CurShowItemData.IsHideProduce &&
                (clickSourceConfig != null || autoSourceConfig != null || chestConfig != null || toolSourceConfig != null || mixSourceConfig != null))
            {
                panelSize = 3;
            }
            else
            {
                if (chainProgress.Count <= 1)
                {
                    // panelSize = CurShowItemData.IsSpecial ? 0 : 1;  //todo 后续要做的针对特殊棋子的提示 需要使用小面板
                    panelSize = 1;
                }
                else
                {
                    panelSize = 2;
                }
            }
            List<int> tempList = new List<int>();
            //填充物品所属合成链的相关信息
            FillChainProgress(tempList, cellDataGroupList, chainProgress);
            CurShowItemData.EmptyIndex = -1;
            //根据面板尺寸填充后续信息
            if (panelSize == 3)
            {
                tempList.Clear();
                //往列表中加一个空List 用于界面上在scroll中显示其他信息
                cellDataGroupList.Add(tempList);
                CurShowItemData.EmptyIndex = cellDataGroupList.Count - 1;
                //根据棋子具备的属性填充其掉落信息 与其合成链中的下一级的掉落信息 取并集
                int nextItemId = CurShowItemData.IsLevelMax ? 0 : chainProgress[CurShowItemData.ItemLevel];
                var nextItemConfig = mergeItemMan.GetItemComConfig(nextItemId);
                if (clickSourceConfig != null)
                {
                    var curChainProgress = mergeItemMan.GetMergeTapCostOutPuts(itemId);
                    var nextChainProgress = nextItemConfig != null ? mergeItemMan.GetMergeTapCostOutPuts(nextItemId) : null;
                    var fixedProgress = new List<int>();
                    fixedProgress.AddRange(clickSourceConfig.OutputsFixed);
                    var nextFixedProgress = nextItemConfig?.clickSourceConfig?.OutputsFixed;
                    if (nextFixedProgress != null)
                    {
                        fixedProgress = Enumerable.ToList(fixedProgress.Union(nextFixedProgress));
                    }
                    FillOutputProgress(itemId, tempList, cellDataGroupList, curChainProgress, nextChainProgress, fixedProgress);
                }
                else if (autoSourceConfig != null)
                {
                    var curChainProgress = autoSourceConfig.Outputs;
                    var nextChainProgress = nextItemConfig?.autoSourceConfig?.Outputs;
                    var fixedProgress = new List<int>();
                    fixedProgress.AddRange(autoSourceConfig.OutputsFixed);
                    var nextFixedProgress = nextItemConfig?.autoSourceConfig?.OutputsFixed;
                    if (nextFixedProgress != null)
                    {
                        fixedProgress = Enumerable.ToList(fixedProgress.Union(nextFixedProgress));
                    }
                    FillOutputProgress(itemId, tempList, cellDataGroupList, curChainProgress, nextChainProgress, fixedProgress);
                }
                else if (chestConfig != null)
                {
                    FillOutputProgress(itemId, tempList, cellDataGroupList, chestConfig.Outputs, nextItemConfig?.chestConfig?.Outputs);
                }
                else if (toolSourceConfig != null)
                {
                    FillOutputProgress(itemId, tempList, cellDataGroupList, toolSourceConfig.DropInfo);
                }
                else if (mixSourceConfig != null)
                {
                    using var _ = PoolMapping.PoolMappingAccess.Borrow<Dictionary<int, int>>(out var outputAll);
                    using var __ = PoolMapping.PoolMappingAccess.Borrow<List<int>>(out var outputList);
                    foreach (var mixId in mixSourceConfig.MixId)
                    {
                        var cost = Merge.Env.Instance.GetMergeMixCostConfig(mixId);
                        foreach (var kv in cost.Outputs)
                        {
                            if (!outputAll.ContainsKey(kv.Key))
                            {
                                outputAll.Add(kv.Key, 1);
                            }
                        }
                    }
                    foreach (var kv in outputAll) outputList.Add(kv.Key);
                    outputList.Sort();
                    FillOutputProgress(itemId, tempList, cellDataGroupList, outputList);
                }
            }
            else if (panelSize == 2 || panelSize == 1)
            {
                var shopMan = Game.Manager.shopMan;
                //判断当前物品对应合成链中的所有物品是否有在商店中出售  如果界面是从商店打开的 则不判断
                if (!UIManager.Instance.IsShow(UIConfig.UIShop))
                {
                    foreach (var chainItemId in chainProgress)
                    {
                        if (shopMan.TryGetChessOrderDataById(chainItemId) != null)
                        {
                            tempList.Clear();
                            //往列表中加一个空List 用于界面上在scroll中显示其他信息
                            cellDataGroupList.Add(tempList);
                            CurShowItemData.EmptyIndex = cellDataGroupList.Count - 1;
                            break;
                        }
                    }
                }
            }
        }

        private void FillChainProgress(List<int> tempList, List<List<int>> cellDataGroupList, RepeatedField<int> chainProgress)
        {
            if (chainProgress.Count < 4)
            {
                cellDataGroupList.Add(new List<int>(chainProgress));
            }
            else
            {
                for (int i = 0; i < chainProgress.Count; i++)
                {
                    int chainItemId = chainProgress[i];
                    //将数据四个为一组划分
                    if (i % 4 < 3)
                    {
                        tempList.Add(chainItemId);
                    }
                    else
                    {
                        tempList.Add(chainItemId);
                        cellDataGroupList.Add(new List<int>(tempList));
                        tempList.Clear();
                    }
                }
                //添加最后的末尾部分
                if (tempList.Count > 0)
                {
                    cellDataGroupList.Add(new List<int>(tempList));
                    tempList.Clear();
                }
            }
        }

        private void FillOutputProgress(int itemId, List<int> tempList, List<List<int>> cellDataGroupList, IList<int> chainProgress,
            IList<int> nextChainProgress = null, IList<int> subChainProgress = null)
        {
            List<int> mergeList = new List<int>();
            //逻辑上认为chainProgress必不会为空 若出现这种情况会报错(一般为策划配错 检查IsHideProduce字段是否配对)
            if (chainProgress == null)
            {
                DebugEx.FormatError("[ItemInfoMan.FillOutputProgress]: Item Config Error! Id = {0}", itemId);
                return;
            }
            mergeList.AddRange(chainProgress);
            if (nextChainProgress != null)
                mergeList = Enumerable.ToList(mergeList.Union(nextChainProgress));
            if (subChainProgress != null)
                mergeList = Enumerable.ToList(mergeList.Union(subChainProgress));
            if (mergeList.Count < 4)
            {
                cellDataGroupList.Add(new List<int>(mergeList));
            }
            else
            {
                for (int i = 0; i < mergeList.Count; i++)
                {
                    int chainItemId = mergeList[i];
                    //将数据四个为一组划分
                    if (i % 4 < 3)
                    {
                        tempList.Add(chainItemId);
                    }
                    else
                    {
                        tempList.Add(chainItemId);
                        cellDataGroupList.Add(new List<int>(tempList));
                        tempList.Clear();
                    }
                }
                //添加最后的末尾部分
                if (tempList.Count > 0)
                {
                    cellDataGroupList.Add(new List<int>(tempList));
                    tempList.Clear();
                }
            }
        }

        private bool _RefreshCurShowItemData(int itemId, ItemInfoType type, params object[] paramList)
        {
            if (itemId <= 0)
                return false;
            var itemConfig = Game.Manager.objectMan.GetMergeItemConfig(itemId);
            if (itemConfig == null)
                return false;
            //如果棋子配了替代id 则后序逻辑完全使用该替代棋子id来进行 界面显示的也是替代的棋子
            if (itemConfig.ReplaceId > 0)
            {
                itemId = itemConfig.ReplaceId;
                itemConfig = Game.Manager.objectMan.GetMergeItemConfig(itemId);
                if (itemConfig == null)
                    return false;
            }
            //开始构造当前要展示的界面数据
            CurShowItemData.Clear();
            CurShowItemData.ItemId = itemId;
            CurShowItemData.Type = type;
            if (type == ItemInfoType.FrozenItem)
                CurShowItemData.RemainTime = (long)paramList[0];
            CurShowItemData.IsHideProduce = itemConfig.IsHideProd;
            var mergeItemMan = Game.Manager.mergeItemMan;
            //获取到棋子所属链条和在链条中的等级
            mergeItemMan.GetItemCategoryIdAndLevel(itemId, out CurShowItemData.ChainId, out var level);
            CurShowItemData.ItemLevel = level + 1;
            //根据链条id找到对应链条配置
            var categoryConfig = mergeItemMan.GetCategoryConfig(CurShowItemData.ChainId);
            if (categoryConfig != null)
            {
                //判断棋子是否是满级
                CurShowItemData.IsLevelMax = CurShowItemData.ItemLevel == categoryConfig.Progress.Count;
                //设置棋子的来源信息
                var originFrom = categoryConfig.OriginFrom;
                if (originFrom.Count > 1)
                {
                    CurShowItemData.OriginChainId = originFrom[0];
                    CurShowItemData.OriginChainLevel = originFrom[1];
                }
                var directFrom = categoryConfig.DirectFrom;
                if (directFrom.Count > 1)
                {
                    CurShowItemData.DirectChainId = directFrom[0];
                    CurShowItemData.DirectChainLevel = directFrom[1];
                }
            }
            //判断棋子是否可以用能量加倍
            var clickSourceConfig = mergeItemMan.GetItemComConfig(itemId)?.clickSourceConfig;
            CurShowItemData.CanShowBoost = (clickSourceConfig?.IsBoostable ?? false) && Merge.Env.Instance.IsInEnergyBoost();
            return true;
        }

        public class ItemChainTipsData
        {
            public int ShowItemId;  //要显示icon的棋子id 为0则显示问号
            public int TipsLevel;   //用于判断是否显示右上角提示按钮 默认为0表示不显示 大于0表示具体提示的等级
            public bool IsTipsLevelMax; //提示按钮toast中使用到的等级是否为满级
            //吃与消耗的逻辑  吃和消耗是互斥的
            public bool IsShowEat = false;  //是否显示吃或消耗的图标 默认不显示
            public int EatShowItemId; //目前要吃的棋子id 也要根据是否在图鉴中已获得 来显示问号或者右上角的tips按钮
            public int EatTipsLevel;   //用于判断是否显示右上角提示按钮 默认为0表示不显示 大于0表示具体提示的等级
            public bool IsEatTipsLevelMax; //提示按钮toast中使用到的等级是否为满级

            public override string ToString()
            {
                return $"ShowItemId = {ShowItemId}, TipsLevel = {TipsLevel}, IsTipsLevelMax = {IsTipsLevelMax}, IsShowEat = {IsShowEat}, " +
                       $"EatShowItemId = {EatShowItemId}, EatTipsLevel = {EatTipsLevel}, IsEatTipsLevelMax = {IsEatTipsLevelMax}";
            }
        }

        public void FillTipsItemDataList(int showItemId, List<ItemChainTipsData> itemDataList)
        {
            if (showItemId <= 0 || itemDataList == null)
                return;
            var categoryConfig = Game.Manager.mergeItemMan.GetCategoryConfigByItemId(showItemId);
            var directFrom = categoryConfig?.DirectFrom;
            if (directFrom == null || directFrom.Count < 1)
                return;
            //先把初始要查找的加进去
            var itemData = new ItemChainTipsData()
            {
                ShowItemId = showItemId,
            };
            itemDataList.Add(itemData);
            //开始递归
            var directChainId = directFrom[0];
            var directChainLevel = directFrom[1];
            int maxRecursiveCount = 1;    //记录最大递归层数 不超过5层
            _FillTipsItemDataRecursively(directChainId, directChainLevel, itemDataList, ref maxRecursiveCount);
        }

        private void _FillTipsItemDataRecursively(int directChainId, int directChainLevel, List<ItemChainTipsData> itemDataList, ref int maxRecursiveCount)
        {
            //递归层数不超过5层
            if (maxRecursiveCount >= 5)
                return;
            var mergeItemMan = Game.Manager.mergeItemMan;
            //-----找目标item id 用于显示图标------
            //根据直接来源的链条和等级  查找配置上想要的目标棋子
            var wantItemId = mergeItemMan.GetChainItemIdByLevel(directChainId, directChainLevel, out bool isLevelMax);
            if (wantItemId <= 0 && directChainLevel > 0)
            {
                DebugEx.FormatError("ItemInfoMan._FillTipsItemDataRecursively : error direct chain, id = {0}, level = {1}", directChainId, directChainLevel);
                return;
            }
            //默认当前itemDataList中最后一个数据就是目前正在查看查找的棋子
            int curLookItemId = itemDataList.Last().ShowItemId;
            ItemChainTipsData tempData = new ItemChainTipsData();
            //根据直接来源的链条和等级 找到直接来源链条里可以显示icon的实际itemId 这里找不到的话 会一次查找链条中比指定等级低的其他棋子
            var directRealItemId = mergeItemMan.GetMaxUnlockLevelItemIdInChain(directChainId, directChainLevel <= 0 ? 0 : directChainLevel, false);
            //如果没找到任何棋子 该棋子位置处直接显示问号
            if (directRealItemId <= 0)
            {
                //如果指定等级>0则需要使用配置的目标棋子继续查找吃的棋子 <=0的不需要找吃直接显示问号
                if (directChainLevel > 0)
                {
                    //显示问号
                    tempData.ShowItemId = 0;
                    itemDataList.Add(tempData);
                    //---吃与消耗的逻辑---
                    _TryFindEatItem(wantItemId, curLookItemId, ref tempData);
                }
                else
                {
                    tempData.ShowItemId = 0;
                    itemDataList.Add(tempData);
                }
            }
            //如果找到了
            else
            {
                //如果指定等级>0
                if (directChainLevel > 0)
                {
                    //判断找到的实际棋子和配置上想要的目标棋子是否一样
                    //如果一样 则显示该棋子图标
                    if (wantItemId == directRealItemId)
                    {
                        tempData.ShowItemId = wantItemId;
                        itemDataList.Add(tempData);
                    }
                    //不一样的话 需要在界面上弹提示
                    else
                    {
                        tempData.ShowItemId = directRealItemId;
                        tempData.TipsLevel = directChainLevel;
                        tempData.IsTipsLevelMax = isLevelMax;
                        itemDataList.Add(tempData);
                    }
                    //如果指定等级>0则需要使用配置的目标棋子继续查找吃的棋子 <=0的不需要找吃
                    //---吃与消耗的逻辑---
                    _TryFindEatItem(wantItemId, curLookItemId, ref tempData);
                }
                //如果指定等级<=0 则直接显示找到的实际棋子icon
                else
                {
                    tempData.ShowItemId = directRealItemId;
                    itemDataList.Add(tempData);
                }
            }

            //-----找当前链条id对应的直接来源链条id 用于推进下一轮递归流程------
            var nextChainConfig = mergeItemMan.GetCategoryConfig(directChainId);
            var directFrom = nextChainConfig?.DirectFrom;
            if (directFrom == null || directFrom.Count < 1)
                return;
            var nextDirectChainId = directFrom[0];
            var nextDirectChainLevel = directFrom[1];
            maxRecursiveCount++;
            _FillTipsItemDataRecursively(nextDirectChainId, nextDirectChainLevel, itemDataList, ref maxRecursiveCount);
        }

        private void _TryFindEatItem(int wantItemId, int curLookItemId, ref ItemChainTipsData tempData)
        {
            var mergeItemMan = Game.Manager.mergeItemMan;
            //检查配置上想要的目标棋子是否具有吃的属性
            var eatConfig = mergeItemMan.GetItemComConfig(wantItemId)?.eatConfig;
            if (eatConfig != null)
            {
                int findIndex = eatConfig.Changeid.IndexOf(curLookItemId);
                if (findIndex < 0)
                {
                    //有吃的属性 但是没找到和curLookItemId对应的配置项 那这里就不显示吃
                }
                else
                {
                    //找到了curLookItemId对应的index，再根据index查找要吃哪个棋子才能变成curLookItemId
                    if (eatConfig.Eat.TryGetByIndex(findIndex, out var eatItemStr))
                    {
                        string[] info = eatItemStr.Split(':');
                        int eatItemId = info.GetElementEx(0, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                        //找到了要吃的目标棋子后，还要检查一下是否在图鉴中获得过 再进一步判断是否显示tips
                        if (eatItemId > 0)
                        {
                            _CheckEatShowItemState(eatItemId, ref tempData);
                        }
                        else
                        {
                            //配置错误 不显示吃
                        }
                    }
                    else
                    {
                        //没找到对应要吃的棋子 那这里就不显示吃
                    }
                }
            }
            //如果没有吃的属性 则检查是否有tapCost属性或者dieInto属性 且消耗的是棋子
            else
            {
                var categoryConfig = mergeItemMan.GetCategoryConfigByItemId(curLookItemId);
                var tapSourceConfig = mergeItemMan.GetItemComConfig(wantItemId)?.clickSourceConfig;
                if (categoryConfig != null && tapSourceConfig != null)
                {
                    var tapCostOutPutsList = mergeItemMan.GetMergeTapCostOutPuts(wantItemId);
                    //直接来源的棋子有tapCost产出 且 产出当前正在查找的棋子
                    if (tapCostOutPutsList != null)
                    {
                        //在所有outputs中优先确认是否包含curLookItemId
                        int findItemId = tapCostOutPutsList.FindEx(id => id == curLookItemId);
                        //如果不包含的话 再从curLookItemId所在的链中等级最低的棋子开始，在所有outputs中寻找是否有对应的 找到了就break
                        if (findItemId <= 0)
                        {
                            for (int i = 0; i < categoryConfig.Progress.Count; i++)
                            {
                                var itemId = categoryConfig.Progress[i];
                                if (tapCostOutPutsList.FindEx(id => id == itemId) >= 0)
                                {
                                    findItemId = itemId;
                                    break;
                                }
                            }
                        }
                        //根据找到的产出棋子id 再反过来去查tapCost,看他的cost是否是棋子，如果是的话就显示消耗 否则的话 无事发生 流程结束
                        if (findItemId > 0)
                        {
                            int consumeItemId = 0;  //查找的要被消耗的棋子id
                            foreach (var costId in tapSourceConfig.CostId)
                            {
                                var tapCost = mergeItemMan.GetMergeTapCostConfig(costId);
                                if (tapCost != null && tapCost.Cost > 0 && tapCost.Cost != Constant.kMergeEnergyObjId
                                    && tapCost.Outputs.FindEx(kv => kv.Key == findItemId).Key > 0)
                                {
                                    consumeItemId = tapCost.Cost;
                                    break;
                                }
                            }
                            if (consumeItemId > 0)
                            {
                                _CheckEatShowItemState(consumeItemId, ref tempData);
                            }
                            else
                            {
                                //没有找到可以被消耗的棋子 不显示消耗 流程结束
                            }
                        }
                        else
                        {
                            //整个图鉴中都没找到对应产出 配错了 不显示消耗 流程结束
                        }
                    }
                    else
                    {
                        //没有tapCost属性 再尝试查找dieInto属性
                        //从wantItemId的tapSourceConfig中的DieInto遍历查找是否含有curLookItemId的项，
                        //如果有的话 说明wantItemId死后会变成curLookItemId
                        //则接下来会反过来去查wantItemId对应的tapCost,看他的cost是否是棋子，如果是的话就显示消耗 否则的话 无事发生 流程结束
                        if (tapSourceConfig.DieInto.Keys.FindEx(id => id == curLookItemId) > 0)
                        {
                            int consumeItemId = 0;  //查找的要被消耗的棋子id
                            foreach (var costId in tapSourceConfig.CostId)
                            {
                                var tapCost = mergeItemMan.GetMergeTapCostConfig(costId);
                                if (tapCost != null && tapCost.Cost > 0 && tapCost.Cost != Constant.kMergeEnergyObjId)
                                {
                                    consumeItemId = tapCost.Cost;
                                    break;
                                }
                            }
                            if (consumeItemId > 0)
                            {
                                _CheckEatShowItemState(consumeItemId, ref tempData);
                            }
                            else
                            {
                                //没有找到可以被消耗的棋子 不显示消耗 流程结束
                            }
                        }
                    }
                }
                else
                {
                    //没找到对应图鉴配置或者tapSource配置 不显示消耗 结束流程
                }
            }
        }

        //检查要显示的吃/消耗的物品状态
        private void _CheckEatShowItemState(int eatItemId, ref ItemChainTipsData tempData)
        {
            var mergeItemMan = Game.Manager.mergeItemMan;
            //标记一下要显示消耗
            tempData.IsShowEat = true;
            //获取到要消耗的棋子所属链条和在链条中的等级
            mergeItemMan.GetItemCategoryIdAndLevel(eatItemId, out var categoryId, out var level);
            level = level + 1;  //获取到的等级从0开始 所以要+1
            var showEatItemId = mergeItemMan.GetMaxUnlockLevelItemIdInChain(categoryId, level, false);
            if (showEatItemId <= 0)
            {
                //没找到要显示的吃的棋子 则直接显示问号
                tempData.EatShowItemId = 0;
            }
            else
            {
                //判断找到的实际吃的棋子和配置上想要吃的目标棋子是否一样
                //如果一样 则显示该吃的棋子图标 流程完成
                if (eatItemId == showEatItemId)
                {
                    tempData.EatShowItemId = eatItemId;
                }
                //不一样的话 需要在界面上弹提示
                else
                {
                    tempData.EatShowItemId = showEatItemId;
                    tempData.EatTipsLevel = level;
                    var categoryConfig = mergeItemMan.GetCategoryConfig(categoryId);
                    tempData.IsEatTipsLevelMax = level >= categoryConfig.Progress.Count;  //判断该棋子是否是链中的满级棋子
                }
            }
        }

        public void Reset() { }

        public void LoadConfig() { }

        public void Startup() { }
    }
}
