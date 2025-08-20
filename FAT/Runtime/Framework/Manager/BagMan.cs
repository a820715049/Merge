/*
 * @Author: tang.yan
 * @Description: 背包管理器(在原有数据基础上，对棋子、资源货币等数据进行进一步包装，对外统称为背包，并以不同类型区分)
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/zpw0a6hf75t3xzat
 * @Date: 2023-10-30 15:10:23
 */

using UnityEngine;
using FAT.Merge;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT
{
    public class BagMan : IGameModule, IUserDataHolder
    {
        //背包类型
        public enum BagType
        {
            None = 0,       //空
            Item = 1,       //棋子背包
            Producer = 2,   //生成器背包
            Tool = 3        //工具背包
        }

        //背包格子数据类 BagMan中自己维护对应list 在数据变化时进行刷新
        public class BagGirdData
        {
            public BagType BelongBagType;   //所属背包类型
            public int BelongBagId;         //所处背包id
            public int GirdIndex;           //格子在背包中对应的index
            public int ItemTId;             //格子上面目前的物品id 没有则为0
            public bool IsUnlock;           //格子是否解锁 根据背包类型会有不同含义
            public int BuyCostNum;          //解锁这个格子需要花费多少钻石 此字段只针对棋子背包有效
            public int BelongItemId;        //这个格子专属于哪个棋子id 此字段只针对生成器背包有效
            public int RelateItemId;        //这个格子关联到哪个棋子id 此字段只针对工具背包有效

            public override string ToString()
            {
                return $"BelongBagType = {BelongBagType}, BelongBagId = {BelongBagId}, GirdIndex = {GirdIndex}, ItemTId = {ItemTId}, IsUnlock = {IsUnlock}, " +
                       $"\n BuyCostNum = {BuyCostNum}, BelongItemId = {BelongItemId}, RelateItemId = {RelateItemId}";
            }
        }

        public int CurItemBagUnlockId => _curItemBagUnlockId;
        public bool CanPutItemInBag => ItemBagEmptyGirdNum > 0; //是否可以往棋子背包中放棋子
        public int ItemBagEmptyGirdNum { get; private set; }   //背包中目前空着的可用的格子数

        //棋子背包 生成棋背包的总容器
        private Inventory _mergeInventory;
        //当前棋子背包解锁到的格子id(id也代表具体解锁的格子总数量)
        private int _curItemBagUnlockId = 0;
        //所有背包格子数据
        private Dictionary<BagType, List<BagGirdData>> _allBagGirdData = new Dictionary<BagType, List<BagGirdData>>();

        //从棋盘打开仓库，永远默认显示棋盘仓库。从主场景打开仓库，永远默认显示工具仓库，并且不允许切换至其他仓库。
        public void TryOpenUIBag()
        {
            if (Game.Manager.mapSceneMan.scene.Active)
            {
                if (CheckBagUnlock(BagType.Tool))
                    UIManager.Instance.OpenWindow(UIConfig.UIBag, BagType.Tool);
            }
            else
            {
                if (CheckBagUnlock(BagType.Item))
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIBag, BagType.Item);
                }
                else if (CheckBagUnlock(BagType.Tool))
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIBag, BagType.Tool);
                }
                else if (CheckBagUnlock(BagType.Producer))
                {
                    UIManager.Instance.OpenWindow(UIConfig.UIBag, BagType.Producer);
                }
            }
        }

        public bool CheckBagUnlock(params BagType[] typeList)
        {
            if (typeList.Length <= 0)
                return false;
            bool isUnlock = false;
            foreach (var type in typeList)
            {
                if (type == BagType.Item)
                {
                    isUnlock = isUnlock || _CheckBagIsUnlock(BagType.Item);
                }
                else if (type == BagType.Producer)
                {
                    isUnlock = isUnlock || _CheckBagIsUnlock(BagType.Producer);
                }
                else if (type == BagType.Tool)
                {
                    isUnlock = isUnlock || _CheckBagIsUnlock(BagType.Tool);
                }
            }
            return isUnlock;
        }

        private bool _CheckBagIsUnlock(BagType type)
        {
            if (type == BagType.Item)
            {
                return Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureBagItem);
            }
            else if (type == BagType.Producer)
            {
                return Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureBagProducer);
            }
            else if (type == BagType.Tool)
            {
                return Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureBagTool);
            }
            return false;
        }

        public List<BagGirdData> GetBagGirdDataList(int bagId)
        {
            if (_allBagGirdData.TryGetValue((BagType)bagId, out var girdDataList))
            {
                return girdDataList;
            }
            else
            {
                return null;
            }
        }

        public void OnMergeLevelChange()
        {
            _CheckProducerBagCapacity();
            _CheckProducerBagRedPoint();
            //升级后刷新
            _UpdateBagGirdData(BagType.Producer);
            //升级可能伴随棋子背包的棋子自动放入生成器背包 所以也要刷新一下
            _UpdateBagGirdData(BagType.Item);
        }

        public void OnItemEnterBag()
        {
            _UpdateBagGirdData(BagType.Item, BagType.Producer);
        }

        public void OnItemLeaveBag()
        {
            _UpdateBagGirdData(BagType.Item, BagType.Producer);
        }

        public void OnGalleryUnlock()
        {
            _UpdateBagGirdData(BagType.Tool);
        }

        //检查是否可以购买新的棋子背包格子
        public bool CanBuyNewItemBagGird()
        {
            var itemBag = _mergeInventory.GetBagByType(BagType.Item);
            return itemBag != null && itemBag.MaxShowGirdNum > itemBag.capacity;
        }

        //购买棋子背包格子
        public void PurchaseItemBagGird(int price)
        {
            if (!CanBuyNewItemBagGird())
            {
                return;
            }
            if (price <= 0)
            {
                DebugEx.FormatWarning("BagMan::PurchaseSlot ----> gird price is error, id = {0}", _curItemBagUnlockId + 1);
                return;
            }
            if (Game.Manager.coinMan.CanUseCoin(CoinType.Gem, price))
            {
                Game.Manager.coinMan.UseCoin(CoinType.Gem, price, ReasonString.inventory)
                .OnSuccess(() =>
                {
                    _curItemBagUnlockId++;
                    _mergeInventory.SetCapacity(_curItemBagUnlockId, (int)BagType.Item);
                    DataTracker.bag_item_unlock.Track(_curItemBagUnlockId);
                    //购买了新格子后刷新
                    _UpdateBagGirdData(BagType.Item);
                    DebugEx.FormatInfo("BagMan::PurchaseSlot ----> purchased new girdId = {0}", _curItemBagUnlockId);
                })
                .Execute();
            }
        }

        //判断棋子是否可以放入生成器背包
        public BagGirdData CheckCanPutProducerBag(Item item)
        {
            if (item == null || item.tid <= 0)
                return null;
            if (!_allBagGirdData.TryGetValue(BagType.Producer, out var girdDataList))
                return null;
            foreach (var girdData in girdDataList)
            {
                //格子已解锁(到达指定等级) 格子专属的棋子id等于传入的棋子id 并且 格子当前没有放其他同id生成器(这个情况不会出现 同一个顶级生成器策划保证只会出现一个 低级生成器不会进生成器背包)
                if (girdData.IsUnlock && girdData.BelongItemId == item.tid && girdData.ItemTId <= 0)
                    return girdData;
            }
            return null;
        }

        //获取升级时可以显示在升级界面的对应等级生成器id
        public int GetProduceIdByLevel(int level)
        {
            int produceId = 0;
            if (level <= 0)
                return produceId;
            var bagProducerConfig = Game.Manager.configMan.GetInventoryProducerConfig();
            foreach (var config in bagProducerConfig.Values)
            {
                if (config.IsShow && config.UnlockLevel == level)
                {
                    produceId = config.ObjBasicId;
                    break;
                }
            }
            return produceId;
        }

        public void Reset()
        {
            _mergeInventory = null;
            _curItemBagUnlockId = 0;
            _allBagGirdData.Clear();
        }

        public void LoadConfig() { }

        public void Startup() { }

        //此方法执行时 背包已经在 MainMergeMan._InitializeWorld中初始化出来了 在这里可以获取到inventory 以及设置对应背包容量
        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            //获取到棋盘上的背包
            _mergeInventory = Game.Manager.mainMergeMan.world.inventory;
            //根据配置初始化最初的格子数量
            _CheckItemBagCapacity();
            _CheckProducerBagCapacity();
            //初始化背包格子相关数据
            _InitBagGirdGirdData();
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            //BagMan所有数据都是源自外部其他Manager 故这里无需存数据
        }

        private void _CheckItemBagCapacity()
        {
            //计算出配置给到的初始容量
            var bagItemConfig = Game.Manager.configMan.GetInventoryItemConfig();
            int maxId = 0;
            foreach (var config in bagItemConfig.Values)
            {
                if (config.CostCount == 0)
                {
                    if (maxId < config.Id)
                        maxId = config.Id;
                }
                else
                {
                    break;
                }
            }
            //比较初始配置容量和目前背包存档数据中记录的容量 取较大值作为最终容量
            var itemBag = _mergeInventory.GetBagByType(BagType.Item);
            int saveCapacity = itemBag.capacity;
            if (maxId >= saveCapacity)
            {
                _curItemBagUnlockId = maxId;
                itemBag.SetCapacity(_curItemBagUnlockId);
            }
            else
            {
                _curItemBagUnlockId = saveCapacity;
            }
            itemBag.MaxShowGirdNum = bagItemConfig.Count;   //界面中最大可显示的格子数 棋子背包直接取配置最大值
        }

        private void _CheckProducerBagCapacity()
        {
            //根据等级判断当前生成器背包的格子容量  格子总数完全由配置决定  格子的开启由登记决定
            int curLevel = Game.Manager.mergeLevelMan.level;
            var bagProducerConfig = Game.Manager.configMan.GetInventoryProducerConfig();
            int maxId = 0;
            foreach (var config in bagProducerConfig.Values)
            {
                //如果配置缺失或者等级不够就break
                if (config.ObjBasicId > 0 && curLevel >= config.UnlockLevel)
                {
                    if (maxId < config.Id)
                        maxId = config.Id;
                }
                else
                {
                    break;
                }
            }
            var producerBag = _mergeInventory.GetBagByType(BagType.Producer);
            producerBag.SetCapacity(maxId);
            //生成器背包固定多显示(InventoryProducerExtraGrid)个格子 同时保证不会超出配置的最大格子数
            int maxShowGirdNum = maxId + Game.Manager.configMan.globalConfig.InventoryProducerExtraGrid;
            producerBag.MaxShowGirdNum = maxShowGirdNum <= bagProducerConfig.Count ? maxShowGirdNum : bagProducerConfig.Count;
        }

        private void _CheckProducerBagRedPoint()
        {
            int curLevel = Game.Manager.mergeLevelMan.level;
            var bagProducerConfig = Game.Manager.configMan.GetInventoryProducerConfig();
            foreach (var config in bagProducerConfig.Values)
            {
                //如果配置缺失或者等级不够就break
                if (config.ObjBasicId > 0 && curLevel >= config.UnlockLevel)
                {
                    if (curLevel == config.UnlockLevel)
                        _mergeInventory.GetBagByType(BagType.Producer).TryAddRedPointItem(config.ObjBasicId);
                }
                else
                {
                    break;
                }
            }
        }

        //在配置和存档数据全都设置完后 初始化相关数据
        private void _InitBagGirdGirdData()
        {
            _InitItemGirdData();
            _InitProducerGirdData();
            _InitToolGirdData();
        }

        private void _InitItemGirdData()
        {
            //初始化棋子背包
            var itemBag = _mergeInventory.GetBagByType(BagType.Item);
            List<BagGirdData> itemBagGirdDataList = new List<BagGirdData>();
            int hasItemNum = 0;
            for (int i = 0; i < itemBag.MaxShowGirdNum; i++)
            {
                var item = itemBag.PeekItem(i);
                var config = Game.Manager.configMan.GetInventoryItemConfigById(i + 1);
                BagGirdData girdData = new BagGirdData()
                {
                    BelongBagType = BagType.Item,
                    BelongBagId = itemBag.id,
                    GirdIndex = i,
                    IsUnlock = i < _curItemBagUnlockId,
                    ItemTId = item?.tid ?? 0,
                    BuyCostNum = config.CostCount,
                };
                if (item != null && item.tid > 0)
                {
                    hasItemNum++;
                }
                itemBagGirdDataList.Add(girdData);
            }
            _allBagGirdData.Add(BagType.Item, itemBagGirdDataList);
            ItemBagEmptyGirdNum = _curItemBagUnlockId - hasItemNum;
        }

        private void _InitProducerGirdData()
        {
            //初始化生成器背包
            var producerBag = _mergeInventory.GetBagByType(BagType.Producer);
            int curLevel = Game.Manager.mergeLevelMan.level;
            List<BagGirdData> producerBagGirdDataList = new List<BagGirdData>();
            for (int i = 0; i < producerBag.MaxShowGirdNum; i++)
            {
                var item = producerBag.PeekItem(i);
                var config = Game.Manager.configMan.GetInventoryProducerConfigById(i + 1);
                BagGirdData girdData = new BagGirdData()
                {
                    BelongBagType = BagType.Producer,
                    BelongBagId = producerBag.id,
                    GirdIndex = i,
                    ItemTId = item?.tid ?? 0,
                    IsUnlock = curLevel >= (config?.UnlockLevel ?? 0),
                    BelongItemId = config?.ObjBasicId ?? 0,
                };
                producerBagGirdDataList.Add(girdData);
            }
            _allBagGirdData.Add(BagType.Producer, producerBagGirdDataList);
        }

        private void _InitToolGirdData()
        {
            //初始化工具背包
            List<BagGirdData> toolBagGirdDataList = new List<BagGirdData>();
            var toolBagConfig = Game.Manager.configMan.GetInventoryToolConfig();
            foreach (var config in toolBagConfig.Values)
            {
                int relateItemId = config?.RelatedItemId ?? 0;
                BagGirdData girdData = new BagGirdData()
                {
                    BelongBagType = BagType.Tool,
                    BelongBagId = (int)BagType.Tool,
                    GirdIndex = (config?.Id ?? 0) - 1,
                    ItemTId = config?.ObjCoinId ?? 0,
                    IsUnlock = _CheckIsUnlockInGallery(relateItemId),
                    RelateItemId = relateItemId,
                };
                toolBagGirdDataList.Add(girdData);
            }
            _allBagGirdData.Add(BagType.Tool, toolBagGirdDataList);
        }

        //检查指定棋子id是否在图鉴系统中解锁
        private bool _CheckIsUnlockInGallery(int itemId)
        {
            return Game.Manager.handbookMan.IsItemUnlocked(itemId);
        }

        //根据外部数据情况 在Man中创建对应方法 方法中再调用此更新方法
        private void _UpdateBagGirdData(params BagType[] typeList)
        {
            if (typeList.Length <= 0)
                return;
            foreach (var type in typeList)
            {
                if (type == BagType.Item)
                {
                    _UpdateItemGirdData();
                }
                else if (type == BagType.Producer)
                {
                    _UpdateProducerGirdData();
                }
                else if (type == BagType.Tool)
                {
                    _UpdateToolGirdData();
                }
            }
            //Dispatch事件
            MessageCenter.Get<MSG.GAME_BAG_ITEM_INFO_CHANGE>().Dispatch();
        }

        private void _UpdateItemGirdData()
        {
            if (_allBagGirdData.TryGetValue(BagType.Item, out var girdDataList))
            {
                var itemBag = _mergeInventory.GetBagByType(BagType.Item);
                int hasItemNum = 0;
                for (int i = 0; i < itemBag.MaxShowGirdNum; i++)
                {
                    var item = itemBag.PeekItem(i);
                    var data = girdDataList[i];
                    data.IsUnlock = i < _curItemBagUnlockId;
                    data.ItemTId = item?.tid ?? 0;
                    if (item != null && item.tid > 0)
                    {
                        hasItemNum++;
                    }
                }
                ItemBagEmptyGirdNum = _curItemBagUnlockId - hasItemNum;
            }
        }

        private void _UpdateProducerGirdData()
        {
            if (_allBagGirdData.TryGetValue(BagType.Producer, out var girdDataList))
            {
                var producerBag = _mergeInventory.GetBagByType(BagType.Producer);
                int curLevel = Game.Manager.mergeLevelMan.level;
                int curLength = girdDataList.Count;
                //生成器背包的最大格子数会随等级发生变化
                for (int i = 0; i < producerBag.MaxShowGirdNum; i++)
                {
                    var config = Game.Manager.configMan.GetInventoryProducerConfigById(i + 1);
                    var item = producerBag.PeekItem(i);
                    if (i < curLength)
                    {
                        var data = girdDataList[i];
                        data.ItemTId = item?.tid ?? 0;
                        data.IsUnlock = curLevel >= (config?.UnlockLevel ?? 0);
                    }
                    else
                    {
                        BagGirdData girdData = new BagGirdData()
                        {
                            BelongBagType = BagType.Producer,
                            BelongBagId = producerBag.id,
                            GirdIndex = i,
                            ItemTId = item?.tid ?? 0,
                            IsUnlock = curLevel >= (config?.UnlockLevel ?? 0),
                            BelongItemId = config?.ObjBasicId ?? 0,
                        };
                        girdDataList.Add(girdData);
                    }
                }
                //如果检测到生成器背包格子数量发生了变化 则尝试将棋子背包中对应的生成器(如果有)，自动转入生成器背包的格子中
                if (curLength < producerBag.MaxShowGirdNum)
                {
                    var itemBag = _mergeInventory.GetBagByType(BagType.Item);
                    //遍历移出物品时 从大往小遍历
                    for (int i = itemBag.capacity - 1; i >= 0; i--)
                    {
                        var girdData = CheckCanPutProducerBag(itemBag.PeekItem(i));
                        //找到了对应棋子
                        if (girdData != null)
                        {
                            //将棋子转移到生成器背包对应格子
                            var removeItem = itemBag.RemoveItem(i);
                            int putIndex = producerBag.PutItemWithIndex(removeItem, girdData.GirdIndex);
                            if (putIndex >= 0)
                            {
                                //刷新对应格子数据
                                var data = girdDataList[putIndex];
                                data.ItemTId = removeItem.tid;
                                _mergeInventory.GetBagByType(BagType.Producer).TryRemoveRedPointItem(removeItem.tid);
                            }
                        }
                    }
                }
            }
        }

        private void _UpdateToolGirdData()
        {
            if (_allBagGirdData.TryGetValue(BagType.Tool, out var girdDataList))
            {
                foreach (var data in girdDataList)
                {
                    data.IsUnlock = _CheckIsUnlockInGallery(data.RelateItemId);
                }
            }
        }
    }
}
