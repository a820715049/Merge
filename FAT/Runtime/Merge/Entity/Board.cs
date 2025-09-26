/**
 * @Author: handong.liu
 * @Date: 2021-02-19 14:29:59
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;

namespace FAT.Merge
{
    public enum ItemDeadType
    {
        None,
        Order,
        Common,
        ClickOut,
        Eat,
        Event,
        Bonus,
        TapBonus,
        BubbleUnlease,
        BubbleTimeout,
        OrderBoxOpen,
        JumpCDExpired,
        FarmAnimal, //农场棋盘中被动物直接吃掉
        WishBoard,
    }

    // 服务于ISpawnBonusHandler
    // 对物品的产出路径详细分类
    public enum ItemSpawnReason
    {
        None,       // 常规玩法机制以外的spawn行为。如: 盘面初始化 / 技能直接操纵棋子 / 其他未分类机制等
        Merge,
        ClickSource,
        AutoSource,
        ChestSource,
        BoxSource,
        EatSource,
        ToolSource,
        DieOutput,
        BubbleBorn,
        BubbleUnlease,
        BubbleTimeout,
        SpecialBox,
        ChoiceBox,
        MagicHour,  // 星想事成
        TrigAutoSource,
        OrderLike,  // 好评订单
        OrderRate,  //  进度礼盒
        ActiveSource,   // 外部触发式棋子产出
    }

    public interface IMergeGrid
    {
        int gridTid { get; }
        Area area { get; }
    }

    public class MergeGrid : IMergeGrid
    {
        public Item item { get; set; } = null;
        public int gridTid { get; set; } = 0;
        public Area area { get; set; } = null;
    }
    public class ItemSpawnContext
    {
        public enum SpawnType
        {
            None,
            TapSource,
            TapSourceDead,
            AutoSource,
            DyingItemDead,
            ToolSource,
            RewardList,
            Undo,
            Bubble,
            Degrade,
            Eat,
            Cheat,
            Upgrade,
            DieInto,
            SpecialBox,
            ChoiceBox,
            MixSource,
            MixSourceExtract,
            MagicHour,
            TrigAutoSource,
            OrderLike,  // 好评订单
            OrderRate,  //  进度礼盒
            ActiveSource,   // 外部触发式棋子产出
            Fishing,        // 钓鱼
            Farm,       // 农场
            Fight,
            WishBoard,
            MineCart,   //矿车棋盘
        }
        public Item spawner;
        public Item from1;           //如果是变过来的，它的来源之1
        public string spawnType;          //for data track use
        public SpawnType type = SpawnType.None;
        public Toast toastType = Toast.Empty;
        public ISpawnEffect spawnEffect;

        public static ItemSpawnContext Create()
        {
            return new ItemSpawnContext();
        }
        public static ItemSpawnContext CreateWithSource(Item src, SpawnType type)
        {
            return new ItemSpawnContext() { spawner = src, type = type };
        }
        public static ItemSpawnContext CreateWithType(SpawnType type)
        {
            return new ItemSpawnContext() { type = type };
        }
        public ItemSpawnContext WithSpawnType(string tp)
        {
            spawnType = tp;
            return this;
        }
        public ItemSpawnContext WithToast(Toast _toastType)
        {
            toastType = _toastType;
            return this;
        }
        public void Append()
        { }
    }

    //棋子状态变化时的context
    public class ItemStateChangeContext
    {
        //棋盘棋子因为什么原因导致状态改变
        public enum ChangeReason
        {
            Default,    //原有的默认行为
            TrigAutoSourceDead, //因为触发式棋子死亡的行为导致解锁
        }

        public Item from; //原因来源Item
        public MBItemView fromView;
        public ChangeReason reason = ChangeReason.Default;

        public static ItemStateChangeContext CreateWithFrom(Item from, ChangeReason reason)
        {
            return new ItemStateChangeContext() { from = from, reason = reason };
        }

        public void SetFromView(MBItemView view)
        {
            fromView = view;
        }
    }

    public class Board
    {
        public MergeWorld world => mParent;
        public int boardId { get; private set; } = 0;
        public int emptyGridCount => mEmptyGridCount;
        //business event
        public event System.Action<Item, Item, Item> onItemMerge;       //when item merge happened, onItemDead and onItemSpawn will not called in this case, param: src, dst, result
        public event System.Action<Item, Item> onItemEat;            //param1 eat param2, when onItemEat triggered, param2's onItemDead will not called
        public event System.Action<Item, Item> onItemConsume;        //param1 consumed param2, when triggered, param2's onItemDead will not called
        public event System.Action<Item> onItemMove;                 //when a item already on board move to a new position
        public event System.Action<Item, ItemStateChangeContext> onItemStateChange;         //when a item's locked or frozen state changes
        public event System.Action<Item, ItemDeadType> onItemDead;                //when a item is disposed, merge will not trigger this!
        // public event System.Action<Item, RewardCommitData> onCollectBonus;  //when a bonus item is collected
        public event System.Action<Item, RewardCommitData> onItemSell;      //when a item is selled
        public event System.Action<ItemSpawnContext, Item> onItemSpawn;     //when a item is spawn, merge will not trigger this
        public event System.Action<Item, List<RewardCommitData>> onItemSpawnFly;     //当棋子产出新棋子，但因为棋盘空间不够，导致新棋子要飞到奖励箱
        public event System.Action<Item> onItemToInventory;                 //when a existing item get out of board
        public event System.Action<Item> onItemFromInventory;                  //when a existing item back to board
        public event System.Action<Item> onItemComponentChange;                  //when item component changes
        public event System.Action<Item> onUseTimeSkipper;                  //when use time skipper
        public event System.Action<Item> onUseTimeScaleSource;                  //when use tesla
        public event System.Action<Item> onJumpCDBegin;                //when use jumpcd
        public event System.Action onJumpCDEnd;                        //jumpcd expired
        public event System.Action onLackOfEnergy;                  //when user lack of energy to do some operation
        public event System.Action<Item, FeatureEntry> onFeatureClicked; //when feature clicked
        public event System.Action<Item, List<int>, System.Action<int>> onChoiceBoxWaiting; //when choicebox clicked

        //general event
        public event System.Action<Item> onItemLeave;             //every time a item leave board
        public event System.Action<Item> onItemEnter;              //every time a item enter board
        public event System.Action<SpeedEffect> onEffectChange;     //every time a effect is changed, added, removed

        public Vector2Int size => new Vector2Int(mCols, mRows);
        private List<Cloud> mClouds = new List<Cloud>();
        private List<Area> mAreas = new List<Area>();
        private List<SpeedEffect> mAllEffects = new List<SpeedEffect>();
        private MergeAdjacentEffectTimeScale mTimeScaleEffect = new MergeAdjacentEffectTimeScale();
        private MergeGrid[] mGrids = new MergeGrid[0];
        private bool[] mGridsCloud = new bool[0];
        private int mEmptyGridCount = 0;
        private int mCols;
        private int mRows;
        private int mLongestSideLength = 0;
        private MergeWorld mParent;
        private HashSet<int> mLockedPosIdx = new HashSet<int>();            //提前被占据的格子，即便此时格子是空的，id在里面，它也不会在_FindEmptyPos里返回
        private ReasonString ProduceReason => EnergyBoostUtility.GetEnergyProduceReason(SettingManager.Instance.EnergyBoostState);

        public Board(MergeWorld parent)
        {
            mParent = parent;
        }

        public void Reset(int _boardId, int col, int row)
        {
            boardId = _boardId;
            _Reset(col, row);
        }

        public void Deserialize(IDictionary<int, Item> unusedItem)
        {
            for (int i = 0; i < mGrids.Length; i++)
            {
                mGrids[i].item = null;
            }
            foreach (var entry in unusedItem)
            {
                var item = entry.Value;
                var itemId = entry.Key;
                int idx = _CalculateIdxByCoord(item.coord.x, item.coord.y);
                if (idx < 0)
                {
                    DebugEx.FormatWarning("Merge::Board.Deserialize ----> item pos illigal:{0},{1}", item.coord.x, item.coord.y);
                    continue;
                }
                if (mGrids[idx].item != null)
                {
                    DebugEx.FormatWarning("Merge::Board.Deserialize ----> item pos already have item:{0},{1},{2}", item.coord.x, item.coord.y, mGrids[idx].item.id);
                    continue;
                }
                mGrids[idx].item = item;
                item.SetParent(this, mGrids[idx]);
                mTimeScaleEffect.Deserialize(item);
            }
            //把找到的item从container删除
            foreach (var g in mGrids)
            {
                if (g?.item != null)
                {
                    unusedItem.Remove(g.item.id);
                }
            }
            _RefreshEmptyGridCount();
        }

        public void WalkAllItem(System.Action<Item> func)
        {
            for (int i = 0; i < mGrids.Length; i++)
            {
                if (mGrids[i].item != null)
                {
                    func(mGrids[i].item);
                }
            }
        }

        public void WalkAllGrid(System.Action<IMergeGrid> func)
        {
            for (int i = 0; i < mGrids.Length; i++)
            {
                func(mGrids[i]);
            }
        }

        public void WalkAllArea(System.Action<Area> func)
        {
            for (int i = 0; i < mAreas.Count; i++)
            {
                func?.Invoke(mAreas[i]);
            }
        }

        public int GetGridTid(int c, int r)
        {
            int idx = _CalculateIdxByCoord(c, r);
            return mGrids.GetElementEx(idx, ArrayExt.OverflowBehaviour.Default)?.gridTid ?? 0;
        }

        public void AddArea(MergeGridArea config)
        {
            var area = new Merge.Area(config);
            mAreas.Add(area);
            area.WalkGrid((col, row, tid) =>
            {
                var idx = _CalculateIdxByCoord(col, row);
                if (idx >= 0)
                {
                    mGrids[idx].area = area;
                    mGrids[idx].gridTid = tid;
                    DebugEx.FormatTrace("Merge::Board::AddArea ----> set grid {0}, {1}, to area {2}, type {3}", col, row, area.config, tid);
                }
            });
        }

        //棋盘初始化时也init配置上所有的云，后续不会有移除操作。
        //若有移除操作势必意味着要有一定规则用于决定是否要创建/销毁云层，但这个规则目前看是和具体的活动强相关的，而棋盘实际上不关心这些
        //换句话说目前云层的创建/销毁规则，棋盘自身感知不到，以及从目前需求来看，棋盘也无需感知到这些规则。
        //云层属于纯表现层内容，表现层结合从活动拿到的数据完全可以实现需求，棋盘这块只需提供好云层的坐标就可以了。
        public void InitAllCloud()
        {
            mClouds.Clear();
            var cloudConf = Game.Manager.configMan.GetMergeCloudConfigs();
            foreach (var conf in cloudConf)
            {
                if (conf.BoardId == boardId)
                {
                    if (Cloud.CreateCloud(conf, CloudType.Movable, out var cloud))
                    {
                        DebugEx.FormatInfo("Merge::Board::AddCloud ----> {0}, {1}, {2}", boardId, cloud.ConfId, cloud.UnlockLevel);
                        mClouds.Add(cloud);
                    }
                }
            }
        }

        //结合当前棋盘行数和偏移程度 刷新棋盘中云层区域的坐标和解锁状态，对外部来讲，可以通过这个操作来感知到当前棋盘上正在显示哪些云层区域
        public void RefreshCloudInfo(int boardMoveOffset, int curLevel)
        {
            DebugEx.FormatInfo("Merge::Board::RefreshCloudInfo ----> {0}, {1}, {2}", boardId, boardMoveOffset, mClouds.Count);
            var boardRowCount = size.y;
            foreach (var cloud in mClouds)
            {
                cloud.RefreshCloudArea(boardRowCount, boardMoveOffset - boardRowCount);
                cloud.RefreshUnlockState(curLevel);
            }
            _RefreshCloudMark();
        }

        //外部需要时 传入container 填充目前可以显示的云层
        public void FillCurShowCloud(List<Cloud> container)
        {
            if (container == null)
                return;
            foreach (var cloud in mClouds)
            {
                //CloudArea的坐标有效且处于未解锁状态时 认为该云层正在显示
                if (cloud.CanShow)
                    container.Add(cloud);
            }
        }

        //检查目前正在棋盘上可以显示的云层中是否还有没解锁的
        public bool CheckHasLockCloud()
        {
            var hasLock = false;
            for (var i = 0; i < mClouds.Count; i++)
            {
                var cloud = mClouds[i];
                if (cloud.CanShow)
                {
                    hasLock = true;
                    break;
                }
            }
            return hasLock;
        }

        public bool HasCloud(int col, int row)
        {
            if (mClouds == null || mClouds.Count <= 0)           //early exit
            {
                return false;
            }
            var idx = _CalculateIdxByCoord(col, row);
            if (idx >= 0)
            {
                return mGridsCloud[idx];
            }
            else
            {
                return false;
            }
        }

        public bool DisposeItem(Item item, ItemDeadType type = ItemDeadType.Common)
        {
            if (item.parent != this)
            {
                DebugEx.FormatWarning("Merge::Board.RemoveItem ----> item not on board:{0}({1})", item.id, item.tid);
                return false;
            }
            _DisposeItem(item, true, type);
            _DisposeBonusProcess(item, item, type);
            return true;
        }

        public Item SpawnItem(int tid, int col, int row, bool locked, bool frozen)
        {
            var idx = _CalculateIdxByCoord(col, row);
            if (idx >= 0 && mGrids[idx].item == null)
            {
                var ret = _SpawnItem(tid, idx, col, row, locked, frozen);
                _SpawnBonusProcess(ret, false, 0, ItemSpawnReason.None);
                return ret;
            }
            else
            {
                DebugEx.FormatWarning("Merge::Board.SpawnItem ----> pos invalid:{0},{1},{2}", col, row, idx);
                return null;
            }
        }

        public Item SpawnItemByConf(int col, int row, string itemConf)
        {
            var idx = _CalculateIdxByCoord(col, row);
            if (idx >= 0 && mGrids[idx].item == null)
            {
                var ret = _SpawnItemByConf(idx, col, row, itemConf);
                if (ret != null)
                    _SpawnBonusProcess(ret, false, 0, ItemSpawnReason.None);
                return ret;
            }
            else
            {
                DebugEx.FormatWarning("Merge::Board.SpawnItemByConf ----> pos invalid:{0},{1},{2}", col, row, idx);
                return null;
            }
        }

        public Item SpawnItemMustWithReason(int tid, ItemSpawnContext context, int col, int row, bool locked, bool frozen)
        {
            var idx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = col, centerRow = row, itemTid = tid });
            if (_CalculateCoordByIdx(idx, out col, out row))
            {
                var ret = _SpawnItem(tid, idx, col, row, locked, frozen, false);
                _SpawnBonusProcess(ret, false, 0, ItemSpawnReason.None);
                _OnItemSpawn(context, ret);
                return ret;
            }
            else
            {
                DebugEx.FormatWarning("Merge::Board.SpawnItemWithReason ----> pos invalid:{0},{1},{2}", col, row, idx);
                return null;
            }
        }

        //检查指定行是否为空行 空行指的是无不可移动的棋子
        public bool CheckIsEmptyRow(int row, bool onlyCheckDrag = false)
        {
            if (row < 0 || row >= mRows)
            {
                return false;
            }
            for (int col = 0; col < mCols; col++)
            {
                var index = _CalculateIdxByCoord(col, row);
                if (mGrids.TryGetByIndex(index, out var grid) && grid.item != null)
                {
                    var item = grid.item;
                    if (onlyCheckDrag)
                    {
                        if (!item.isDraggable) return false;
                    }
                    else
                    {
                        if (!item.isMovable) return false;
                    }
                }
            }
            return true;
        }

        //将指定item从棋盘移动到奖励箱, 这里并没有清除棋子上原来的坐标信息，表现层可能会用得上
        //另外 目前认为item一旦被主动移到奖励箱中，则其原来的锁定状态(蜘蛛网、盒子覆盖)都失效
        public bool MoveItemToRewardBox(Item item, bool needItemEvent = false)
        {
            if (item.parent != this)
            {
                DebugEx.FormatWarning("Merge::Board.MoveItemToRewardBox ----> item not on board:{0}({1})", item.id, item.tid);
                return false;
            }
            var itemIdx = _CalculateIdxByCoord(item.coord.x, item.coord.y);
            if (!mGrids.TryGetByIndex(itemIdx, out var findGrid) || findGrid.item != item)
            {
                DebugEx.FormatWarning("Merge::Board.MoveItemToRewardBox ----> item not on board:{0}({1})", item.id, item.tid);
                return false;
            }
            //先将棋子从棋盘上移除
            findGrid.item = null;
            _ChangeEmptyGridCount(1);
            item.SetState(false, false);
            item.SetParent(null, null);
            onItemLeave?.Invoke(item);
            //再将棋子移入棋盘奖励箱
            mParent.AddReward(item, item.config?.IsTop ?? false);
            //外部自行决定是否需要触发棋子事件 触发后会当场执行棋子飞到奖励箱的表现
            if (needItemEvent)
            {
                world.TriggerItemEvent(item, ItemEventType.ItemEventMoveToRewardBox);
            }
            DebugEx.FormatInfo("Merge::Board.MoveItemToRewardBox ----> item {0}({1},{2}) is move to reward box", item.id, item.coord.x, item.coord.y);
            return true;
        }

        //尝试将指定坐标的棋子移动到新的坐标位置（纯数据层移动）
        //若目标格子上有棋子，则跳过本次移动
        public bool TryMoveItemToCoord(int oldCol, int oldRow, int newCol, int newRow)
        {
            var oldIdx = _CalculateIdxByCoord(oldCol, oldRow);
            //校验源格子是否存在棋子 没有棋子则返回false
            if (!mGrids.TryGetByIndex(oldIdx, out var oldGrid) || oldGrid.item == null)
            {
                return false;
            }
            var newIdx = _CalculateIdxByCoord(newCol, newRow);
            //校验目标格子是否存在棋子 存在棋子则跳过本次移动
            if (!mGrids.TryGetByIndex(newIdx, out var newGrid) || newGrid.item != null)
            {
                return false;
            }
            //移动棋子
            var moveItem = oldGrid.item;
            oldGrid.item = null;
            newGrid.item = moveItem;
            moveItem.SetPosition(newCol, newRow, newGrid);
            return true;
        }

        public Item TestSpawnItem(int id)
        {
            int c = mCols / 2, r = mRows / 2;
            int emptyIdx = _FindEmptyIdx(mCols / 2, mRows / 2);
            _CalculateCoordByIdx(emptyIdx, out c, out r);
            if (emptyIdx >= 0)
            {
                return _SpawnItem(id, emptyIdx, c, r, false, false);
            }
            else
            {
                return null;
            }
        }

        public Item TestSpawnBox(int id, int c, int r)
        {
            int emptyIdx = _FindEmptyIdx(c, r);
            _CalculateCoordByIdx(emptyIdx, out c, out r);
            if (emptyIdx >= 0)
            {
                var item = _SpawnItem(id, emptyIdx, c, r, true, true);
                return item;
            }
            else
            {
                return null;
            }
        }

        public Item FindItemById(int id)
        {
            foreach (var i in mGrids)
            {
                if (i.item != null && i.item.id == id)
                {
                    return i.item;
                }
            }
            return null;
        }

        //根据棋子的配置id找到棋盘上任意的对应id棋子
        public Item FindAnyItemByConfigId(int confId)
        {
            foreach (var i in mGrids)
            {
                if (i.item != null && i.item.tid == confId)
                {
                    return i.item;
                }
            }
            return null;
        }

        public bool EatSourceEatItem(Item eatSource, Item food)
        {
            var com = eatSource.GetItemComponent<ItemEatSourceComponent>();
            if (com != null && com.EatItem(food))
            {
                DebugEx.FormatInfo("Board::EatSourceEatItem ----> {0} eat {1}", eatSource, food);
                _DisposeItem(food, false);
                _DisposeBonusProcess(food, eatSource);
                onItemEat?.Invoke(eatSource, food);
                return true;
            }
            else
            {
                DebugEx.FormatWarning("Board::EatSourceEatItem ----> {0} eat {1} fail", eatSource, food);
                return false;
            }
        }

        public bool EatItem(Item eatSource, Item food)
        {
            var com = eatSource.GetItemComponent<ItemEatComponent>();
            if (com != null && com.EatItem(food))
            {
                DebugEx.FormatInfo("Board::EatItem ----> {0} eat {1}", eatSource, food);
                _DisposeItem(food, false);
                _DisposeBonusProcess(food, eatSource);
                onItemEat?.Invoke(eatSource, food);
                return true;
            }
            else
            {
                DebugEx.FormatWarning("Board::EatItem ----> {0} eat {1} fail", eatSource, food);
                return false;
            }
        }

        public Item UseEatSource(Item item, out ItemUseState state)
        {
            var com = item.GetItemComponent<ItemEatSourceComponent>();
            if (com != null && com.state == ItemEatSourceComponent.Status.Output)
            {
                var energyCost = com.energyCost;
                if (energyCost > 0 && !Env.Instance.CanUseEnergy(energyCost))
                {
                    DebugEx.FormatWarning("Merge::Board.UseEatSource ----> no energy for source {0}, cost {1}", item.tid, energyCost);
                    onLackOfEnergy?.Invoke();
                    state = ItemUseState.NotEnoughEnergy;
                    return null;
                }
                var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = item.coord.x, centerRow = item.coord.y, sourceComponent = com });
                if (emptyIdx >= 0)
                {
                    if (energyCost > 0)
                    {
                        Env.Instance.UseEnergy(energyCost, ProduceReason);
                    }
                    var nextItem = com.ConsumeNextItem();
                    _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                    var result = _SpawnItem(nextItem, emptyIdx, c, r, false, false, false);
                    _SpawnBonusProcess(result, false, energyCost, ItemSpawnReason.EatSource, from: com.item);
                    _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.TapSource), result);
                    mParent.TriggerItemEvent(item, ItemEventType.ItemEventOutputItem);
                    Env.Instance.NotifyItemUse(item, ItemComponentType.EatSource);
                    state = ItemUseState.Success;
                    return result;
                }
                else
                {
                    DebugEx.FormatInfo("Merge::Board.UseEatSource ----> not generate, no room for new source for {0}", item.tid);
                    state = ItemUseState.NotEnoughSpace;
                    return null;
                }
            }
            else
            {
                DebugEx.FormatWarning("Merge::Board.UseEatSource ----> not generate, not ready for {0}", item.tid);
                state = ItemUseState.UnknownError;
                if (com != null)
                {
                    if (com.state == ItemEatSourceComponent.Status.Eating)
                    {
                        state = ItemUseState.CoolingDown;
                    }
                }
                return null;
            }
        }

        public Item UseToolSource(Item item, out ItemUseState state)
        {
            var com = item.GetItemComponent<ItemToolSourceComponent>();
            if (com != null && com.IsNextItemReady())
            {
                bool willDead = com.willDead;
                var emptyIdx = -1;
                if (com.willDead)
                {
                    emptyIdx = _CalculateIdxByCoord(item.coord.x, item.coord.y);
                }
                else
                {
                    emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = item.coord.x, centerRow = item.coord.y, sourceComponent = com });
                }
                if (emptyIdx >= 0)
                {
                    var deadType = com.willDead ? ItemDeadType.ClickOut : ItemDeadType.Common;
                    var nextItem = com.ConsumeNextItem();
                    Item result = null;
                    if (nextItem > 0)
                    {
                        _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                        result = _SpawnItem(nextItem, emptyIdx, c, r, false, false, false);
                        _SpawnBonusProcess(result, false, 0, ItemSpawnReason.ToolSource, from: com.item);
                        _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.ToolSource), result);
                    }
                    if (willDead)
                    {
                        _DisposeItem(item, true, deadType);     //就算没有产出，最后也要死
                        _DisposeBonusProcess(item, item, deadType);
                    }
                    mParent.TriggerItemEvent(item, ItemEventType.ItemEventOutputItem);
                    Env.Instance.NotifyItemUse(item, ItemComponentType.ToolSouce);
                    state = ItemUseState.Success;
                    return result;
                }
                else
                {
                    DebugEx.FormatInfo("Merge::Board.UseToolSource ----> not generate, no room for new source for {0}", item.tid);
                    state = ItemUseState.NotEnoughSpace;
                    return null;
                }
            }
            else
            {
                DebugEx.FormatWarning("Merge::Board.UseToolSource ----> not generate, not ready for {0}", item.tid);
                state = ItemUseState.CoolingDown;
                return null;
            }
        }

        public bool UseTrigAutoSource(Item item, out ItemUseState state)
        {
            state = ItemUseState.UnknownError;
            var com = item.GetItemComponent<ItemTrigAutoSourceComponent>();
            if (com == null || !com.HasTriggerCount())
                return false;
            //检查是否即将死掉
            var willDead = com.CheckWillDead();
            //尝试消耗一次触发次数 若失败则返回
            if (!com.TryUseTriggerCount(out var triggerInfoId))
            {
                return false;
            }
            //记录棋子当前坐标
            var curCoord = item.coord;
            //若消耗成功 则发奖
            //如果即将死掉
            if (willDead)
            {
                var emptyIdx = _CalculateIdxByCoord(curCoord.x, curCoord.y);
                //直接原地死掉
                _DisposeItem(item, true, ItemDeadType.ClickOut);
                _DisposeBonusProcess(item, item, ItemDeadType.ClickOut);
                //检查是否有dieInto 有的话直接原地生成对应棋子
                var dieIntoId = com.GetDieIntoItemId();
                if (dieIntoId > 0)
                {
                    var result = _SpawnItem(dieIntoId, emptyIdx, curCoord.x, curCoord.y, false, false, false);
                    _SpawnBonusProcess(result, false, 0, ItemSpawnReason.TrigAutoSource, item);
                    _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.TrigAutoSource), result);
                }
            }
            //处理后续的发奖逻辑 如果棋盘满了发不上 则会直接发到奖励箱
            var container = PoolMapping.PoolMappingAccess.Take<List<int>>();
            if (com.GetRandomOutputList(triggerInfoId, container))
            {
                List<RewardCommitData> flyReward = null;
                //这里container中的奖励列表已经是随机后的了
                foreach (var outputId in container.obj)
                {
                    if (outputId > 0)
                    {
                        var idx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = curCoord.x, centerRow = curCoord.y, itemTid = outputId });
                        //如果找到的坐标不合法 说明棋盘满了
                        if (_CalculateCoordByIdx(idx, out var bonusCol, out var bonusRow))
                        {
                            var ret = _SpawnItem(outputId, idx, bonusCol, bonusRow, false, false, false);
                            _SpawnBonusProcess(ret, false, 0, ItemSpawnReason.TrigAutoSource, item);
                            _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.TrigAutoSource), ret);
                        }
                        else
                        {
                            var reward = Game.Manager.rewardMan.BeginReward(outputId, 1, ReasonString.trig_auto_source);
                            flyReward ??= new List<RewardCommitData>();
                            flyReward.Add(reward);
                        }
                    }
                }
                if (flyReward != null && flyReward.Count > 0)
                {
                    _OnItemSpawnFly(item, flyReward);
                }
            }
            container.Free();
            //触发式棋子死亡时 会带动周围的棋子解锁
            if (willDead)
            {
                _TriggerUnlockAround(curCoord.x, curCoord.y, ItemStateChangeContext.CreateWithFrom(item, ItemStateChangeContext.ChangeReason.TrigAutoSourceDead));
            }
            mParent.TriggerItemEvent(item, ItemEventType.ItemEventTrigAutoSource);
            state = ItemUseState.Success;
            return true;
        }

        private int FindEmptyIdx(ItemComponentBase com, bool willDie)
        {
            int emptyIdx;
            if (willDie)
            {
                emptyIdx = _CalculateIdxByCoord(com.item.coord.x, com.item.coord.y);
            }
            else
            {
                emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = com.item.coord.x, centerRow = com.item.coord.y, sourceComponent = com });
            }
            return emptyIdx;
        }

        public Item TryResolveMagicHourOutput(IOrderData fromOrder, out IOrderData targetOrder)
        {
            targetOrder = null;
            var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = 3, centerRow = 3 });
            if (emptyIdx < 0)
            {
                DebugEx.FormatInfo("Merge::Board.TryResolveMagicHourOutput ----> not generate, no room for new source");
                return null;
            }

            // 计算星想事成订单需求的平均难度
            var total_dffy = 0;
            // 当前帧内订单提交导致棋子发生改变
            Game.Manager.mergeItemDifficultyMan.ClearCache();
            foreach (var req in fromOrder.Requires)
            {
                Game.Manager.mergeItemDifficultyMan.TryGetItemDifficulty(req.Id, out _, out var real);
#if UNITY_EDITOR
                DebugEx.Info($"Merge::Board.TryResolveMagicHourOutput [boxdebug] itemId={req.Id} realDffy={real}");
#endif
                total_dffy += real * req.TargetCount;
            }
            // 计算难度区间
            var min = (int)(total_dffy * fromOrder.RewardDffyRange.min / 100f);
            var max = (int)(total_dffy * fromOrder.RewardDffyRange.max / 100f);

#if UNITY_EDITOR
            DebugEx.Info($"Merge::Board.TryResolveMagicHourOutput [boxdebug] total={total_dffy} min:{min}={total_dffy}*{fromOrder.RewardDffyRange.min}% max:{max}={total_dffy}*{fromOrder.RewardDffyRange.max}%");
#endif

            // 尝试产出 | 默认当前棋盘/主订单
            var suc = Game.Manager.mergeItemDifficultyMan.CalcMagicHourOutput(Game.Manager.mergeBoardMan.activeTracer,
                Game.Manager.mainOrderMan.curOrderHelper,
                min, max,
                out targetOrder, out var nextItem);

            // 产出失败时生成缺省棋子
            if (!suc)
            {
                nextItem = fromOrder.FallbackItemId;
            }
            DebugEx.Info($"Merge::Board.TryResolveMagicHourOutput [boxdebug] fromOrder={fromOrder.Id} toOrder={targetOrder?.Id} diff=({min}, {max}) state={suc} item={nextItem}");

            if (nextItem > 0)
            {
                _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                var result = _SpawnItem(nextItem, emptyIdx, c, r, false, false, false);
                _SpawnBonusProcess(result, false, 0, ItemSpawnReason.MagicHour);
                _OnItemSpawn(ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.MagicHour), result);
                DataTracker.wishing_reward.Track(fromOrder.GetValue(OrderParamType.EventId),
                    fromOrder.GetValue(OrderParamType.EventParam),
                    fromOrder.Id,
                    result.tid);
                return result;
            }
            return null;
        }

        /// <summary>
        /// 尝试直接用魔盒逻辑产出
        /// </summary>
        /// <param name="itemId">ID</param>
        /// <param name="reason">产出原因</param>
        /// <param name="spawnType">产出类型</param>
        /// <returns>产出结果</returns>
        public Item TrySpawnItem(int itemId, ItemSpawnReason reason, ItemSpawnContext context)
        {
            var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = 3, centerRow = 3 });
            if (itemId <= 0)
            {
                DebugEx.FormatInfo("Merge::Board.TrySpawnItem ----> not generate, no item id");
                return null;
            }
            if (emptyIdx < 0)
            {
                DebugEx.FormatInfo("Merge::Board.TrySpawnItem ----> not generate, no room for new item");
                return null;
            }
            _CalculateCoordByIdx(emptyIdx, out var c, out var r);
            var result = _SpawnItem(itemId, emptyIdx, c, r, false, false, false);
            _SpawnBonusProcess(result, false, 0, reason);
            _OnItemSpawn(context, result);
            return result;
        }

        public bool UseActiveSource(Item item, out ItemUseState state)
        {
            state = ItemUseState.UnknownError;
            var com = item.GetItemComponent<ItemActiveSourceComponent>();
            if (com == null || !com.CanOutput)
                return false;
            var willDead = com.WillDead;
            var emptyIdx = FindEmptyIdx(com, willDead);
            if (emptyIdx < 0)
            {
                DebugEx.FormatInfo("Merge::Board.UseActiveSource ----> not generate, no room for new source for {0}", item.tid);
                state = ItemUseState.NotEnoughSpace;
                return false;
            }
            var handlers = mParent.activityHandlers;
            IExternalOutput externalOutput = null;
            foreach (var handler in handlers)
            {
                if (handler is IExternalOutput ext)
                {
                    if (ext.CanUseItem(item))
                    {
                        externalOutput = ext;
                        break;
                    }
                }
            }
            if (externalOutput == null)
            {
                DebugEx.Warning($"Merge::Board.UseActiveSource ----> no output handler");
                return false;
            }
            if (!externalOutput.TrySpawnItem(item, out var outputItemId, out var spawnContext))
            {
                DebugEx.Warning($"Merge::Board.UseActiveSource ----> output failed");
                return false;
            }
            // 使用成功 消耗使用次数
            com.Consume();
            if (outputItemId > 0)
            {
                // 生成棋子
                _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                var result = _SpawnItem(outputItemId, emptyIdx, c, r, false, false, false);
                _SpawnBonusProcess(result, false, 0, ItemSpawnReason.ActiveSource, from: com.item);
                spawnContext ??= ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.ActiveSource);
                _OnItemSpawn(spawnContext, result);
            }
            if (willDead)
            {
                var deadType = willDead ? ItemDeadType.ClickOut : ItemDeadType.Common;
                _DisposeItem(item, true, deadType);
                _DisposeBonusProcess(item, item, deadType);
            }
            mParent.TriggerItemEvent(item, ItemEventType.ItemEventOutputItem);
            Env.Instance.NotifyItemUse(item, ItemComponentType.ActiveSource);
            state = ItemUseState.Success;
            return true;
        }

        public Item UseSpecialBox(Item item, out ItemUseState state)
        {
            var com = item.GetItemComponent<ItemSpecialBoxComponent>();
            if (com != null && com.canOutput)
            {
                var willDead = com.willDead;
                var emptyIdx = FindEmptyIdx(com, willDead);
                // var energyCost = com.energyCost;

                if (emptyIdx < 0)
                {
                    DebugEx.FormatInfo("Merge::Board.UseSpecialBox ----> not generate, no room for new source for {0}", item.tid);
                    state = ItemUseState.NotEnoughSpace;
                    return null;
                }
                // if (energyCost > 0)
                // {
                //     // cost类型为能量
                //     if (!Env.Instance.CanUseEnergy(energyCost))
                //     {
                //         DebugEx.FormatWarning("Merge::Board.UseSpecialBox ----> no energy for source {0}, cost {1}", item.tid, energyCost);
                //         onLackOfEnergy?.Invoke();
                //         state = ItemUseState.NotEnoughEnergy;
                //         return null;
                //     }
                // }

                var deadType = willDead ? ItemDeadType.ClickOut : ItemDeadType.Common;
                var nextItem = com.ConsumeNextItem();
                Item result = null;
                if (nextItem > 0)
                {
                    _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                    result = _SpawnItem(nextItem, emptyIdx, c, r, false, false, false);
                    _SpawnBonusProcess(result, false, 0, ItemSpawnReason.SpecialBox, from: com.item);
                    _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.SpecialBox), result);
                }
                if (willDead)
                {
                    _DisposeItem(item, true, deadType);
                    _DisposeBonusProcess(item, item, deadType);
                }
                mParent.TriggerItemEvent(item, ItemEventType.ItemEventOutputItem);
                Env.Instance.NotifyItemUse(item, ItemComponentType.SpecialBox);
                state = ItemUseState.Success;
                return result;
            }

            DebugEx.FormatWarning("Merge::Board.UseSpecialBox ----> not generate, not ready for {0}", item.tid);
            state = ItemUseState.CoolingDown;
            return null;
        }

        public bool UseChoiceBox(Item item, out ItemUseState state)
        {
            // 确认选择后进行实际生成
            void OnConfirmSelection(int id)
            {
                var com = item.GetItemComponent<ItemChoiceBoxComponent>();
                var emptyIdx = FindEmptyIdx(com, true);
                var deadType = ItemDeadType.ClickOut;
                var nextItem = id;
                if (nextItem > 0)
                {
                    _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                    var result = _SpawnItem(nextItem, emptyIdx, c, r, false, false, false);
                    _SpawnBonusProcess(result, false, 0, ItemSpawnReason.ChoiceBox, from: com.item);
                    _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.ChoiceBox), result);
                }
                _DisposeItem(item, true, deadType);
                _DisposeBonusProcess(item, item, deadType);
                mParent.TriggerItemEvent(item, ItemEventType.ItemEventOutputItem);
                Env.Instance.NotifyItemUse(item, ItemComponentType.ChoiceBox);
            }

            var com = item.GetItemComponent<ItemChoiceBoxComponent>();
            com.CalcChoiceBoxOutput();
            onChoiceBoxWaiting?.Invoke(item, ItemChoiceBoxComponent.outputResults, OnConfirmSelection);
            state = ItemUseState.Success;
            return true;
        }

        public Item UseClickItemSource(Item item, out ItemUseState state)
        {
            return UseClickItemSourceWithConsume(item, null, out state);
        }

        public Item UseClickItemSourceWithConsume(Item item, Item consumeTarget, out ItemUseState state)
        {
            var com = item.GetItemComponent<ItemClickSourceComponent>();
            if (com != null && com.IsNextItemReady())
            {
                MergeTapCost tapCost = null;
                Item itemToConsume = null;
                var energyCost = com.energyCost;

                if (com.costConfig.Count > 0)
                {
                    // 是否有指定的cost目标
                    if (consumeTarget != null)
                    {
                        if (!consumeTarget.isActive || consumeTarget.HasComponent(ItemComponentType.Bubble))
                        {
                            DebugEx.FormatWarning("Merge::Board.UseClickItemSource ----> provided cost item not valid {0}", item.tid);
                            state = ItemUseState.UnknownError;
                            return null;
                        }
                        tapCost = Env.Instance.FindCostByItem(com.config.CostId, consumeTarget);
                        itemToConsume = consumeTarget;
                    }
                    else
                    {
                        tapCost = Env.Instance.FindPossibleCost(com.costConfig);
                    }

                    if (tapCost == null)
                    {
                        // cost配置不为空 却没有找到可用的cost
                        DebugEx.FormatWarning("Merge::Board.UseClickItemSource ----> not enough cost for source {0}", item.tid);
                        state = ItemUseState.NotEnoughCost;
                        return null;
                    }

                    if (energyCost > 0)
                    {
                        // cost类型为能量
                        if (!Env.Instance.CanUseEnergy(energyCost))
                        {
                            DebugEx.FormatWarning("Merge::Board.UseClickItemSource ----> no energy for source {0}, cost {1}", item.tid, energyCost);
                            onLackOfEnergy?.Invoke();
                            state = ItemUseState.NotEnoughEnergy;
                            return null;
                        }
                    }
                }

                // 0. 检查棋盘空间
                // 为避免用户误操作 禁止在原地产出 计算格子时不考虑自身dead提供的位置
                int tapOutputSpaceNeed = 0;
                if (tapCost != null && tapCost.Outputs.Count > 0)
                {
                    // 常规产出
                    ++tapOutputSpaceNeed;
                    if (tapCost.Cost != 0 && tapCost.Cost != Constant.kMergeEnergyObjId)
                    {
                        // 消耗棋子
                        --tapOutputSpaceNeed;
                    }
                }
                if (tapOutputSpaceNeed > emptyGridCount)
                {
                    DebugEx.FormatInfo("Merge::Board.UseClickItemSource ----> not generate, no room for new source for {0}", item.tid);
                    state = ItemUseState.NotEnoughSpace;
                    return null;
                }

                // 1. 消耗
                if (tapCost != null)
                {
                    if (energyCost > 0)
                    {
                        // tap时要消耗能量
                        Env.Instance.UseEnergy(energyCost, ProduceReason);
                    }
                    else if (tapCost.Cost > 0)
                    {
                        // tap时要消耗物品
                        itemToConsume ??= FindConsumableItemById(tapCost.Cost);
                        DebugEx.Info($"Board::UseClickItemSource ----> {item} consume {itemToConsume}");
                        _DisposeItem(itemToConsume, false);
                        _DisposeBonusProcess(itemToConsume, item);
                        onItemConsume?.Invoke(item, itemToConsume);
                    }

                    // 接管当前产出列表 给spawn做准备
                    com.ResetOutputs(tapCost.Outputs);
                }

                // 2. 正常产出
                Item ret = null;
                var nextItemId = com.ConsumeNextItem(out int origItemId);
                if (nextItemId > 0)
                {
                    // 如果之前的空位不存在 死亡后再次尝试空位
                    var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = item.coord.x, centerRow = item.coord.y, sourceComponent = com });
                    if (emptyIdx >= 0)
                    {
                        _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                        ret = _SpawnItem(nextItemId, emptyIdx, c, r, false, com.config.Frozen, false);
                        _SpawnBonusProcess(ret, false, energyCost, ItemSpawnReason.ClickSource, from: com.item);
                        _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.TapSource).WithToast(com.GetToastTypeForItem(origItemId, nextItemId)), ret);
                    }
                    else
                    {
                        DebugEx.Warning($"Merge::Board.UseClickItemSource ----> not generate after cost, no room for new source for {item.tid}");
                    }
                }

                // 3. 死亡 已产出 itemCount 变为 0
                bool isDead = (com.itemCount <= 0 && com.itemInRechargeCount == 0 && com.config.ReviveTime <= 0) || com.isDead;
                if (isDead)
                {
                    _DisposeItem(item, true, ItemDeadType.ClickOut);
                    _DisposeBonusProcess(item, item, ItemDeadType.ClickOut);
                }

                // 4. 死亡产出
                if (isDead && com.config.DieInto.Count > 0)
                {
                    var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = item.coord.x, centerRow = item.coord.y, sourceComponent = com });
                    if (emptyIdx >= 0)
                    {
                        _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                        var idx = _CalculateIdxByCoord(c, r);
                        var dieOutputItem = com.config.DieInto.RandomChooseByWeight(e => e.Value).Key;
                        if (dieOutputItem > 0)
                        {
                            DebugEx.Info($"Board::UseClickItemSource ----> dieinto random select {dieOutputItem}");
                            var dieOutput = _SpawnItem(dieOutputItem, idx, c, r, false, false, false);
                            _SpawnBonusProcess(dieOutput, false, energyCost, ItemSpawnReason.DieOutput, from: com.item);
                            _OnItemSpawn(ItemSpawnContext.CreateWithSource(null, ItemSpawnContext.SpawnType.DieInto), dieOutput);

                            // 如果result为null 用dieOutput替代result 使调用结果表示操作成功
                            ret ??= dieOutput;
                        }
                    }
                }

                mParent.TriggerItemEvent(item, ItemEventType.ItemEventOutputItem);
                Env.Instance.NotifyItemUse(item, ItemComponentType.ClickSouce);
                state = ItemUseState.Success;
                return ret;
            }
            else
            {
                DebugEx.FormatWarning("Merge::Board.UseClickItemSource ----> not generate, not ready for {0}", item.tid);
                state = ItemUseState.CoolingDown;
                return null;
            }
        }

        public bool MixSourceConsume(Item item, Item itemToConsume, out ItemUseState state)
        {
            state = ItemUseState.UnknownError;
            // 被消耗方只能来自棋盘
            if (!item.isActive || itemToConsume == null || itemToConsume.parent != this)
            {
                DebugEx.Error($"Board::MixSourceConsume ----> {item} cant consume {itemToConsume}");
                state = ItemUseState.UnknownError;
                return false;
            }
            if (item.TryGetItemComponent(out ItemMixSourceComponent com))
            {
                if (!com.IsNextItemReady())
                {
                    DebugEx.FormatWarning("Board.MixSourceConsume ----> not generate, not ready for {0}", item.tid);
                    state = ItemUseState.CoolingDown;
                }
                else
                {
                    if (com.TryMixItem(itemToConsume))
                    {
                        DebugEx.Info($"Board::MixSourceConsume ----> {item} consume {itemToConsume}");
                        _DisposeItem(itemToConsume, false);
                        _DisposeBonusProcess(itemToConsume, item);
                        onItemConsume?.Invoke(item, itemToConsume);
                        state = ItemUseState.Success;
                    }
                    else
                    {
                        DebugEx.Error($"Board::MixSourceConsume ----> {item} consume {itemToConsume} failed");
                        state = ItemUseState.UnknownError;
                    }
                }
            }
            return state == ItemUseState.Success;
        }

        public Item MixSourceProduce(Item item, out ItemUseState state)
        {
            if (emptyGridCount < 1)
            {
                state = ItemUseState.NotEnoughSpace;
                return null;
            }
            if (item.TryGetItemComponent(out ItemMixSourceComponent com) && com.IsNextItemReady())
            {
                var (ready, mixId) = com.CheckMixState();
                if (!ready)
                {
                    // 没吃够
                    state = ItemUseState.NotEnoughCost;
                    return null;
                }
                Item ret = null;

                // 正常产出
                var nextItemId = com.ConsumeNextItem(mixId, out var origItemId);
                if (nextItemId > 0)
                {
                    var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = item.coord.x, centerRow = item.coord.y, sourceComponent = com });
                    if (emptyIdx >= 0)
                    {
                        _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                        ret = _SpawnItem(nextItemId, emptyIdx, c, r, false, false, false);
                        _SpawnBonusProcess(ret, false, 0, ItemSpawnReason.ClickSource, from: com.item);
                        _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.MixSource).WithToast(com.GetToastTypeForItem(origItemId, nextItemId)), ret);
                    }
                    else
                    {
                        DebugEx.Error($"Merge::Board.MixSourceProduce ----> not generate after cost, no room for new source for {item.tid} -> {nextItemId}");
                    }
                }

                // 死亡 已产出 itemCount 变为 0
                var isDead = (com.itemCount <= 0 && com.itemInRechargeCount == 0 && com.config.ReviveTime <= 0) || com.isDead;
                if (isDead)
                {
                    _DisposeItem(item, true, ItemDeadType.ClickOut);
                    _DisposeBonusProcess(item, item, ItemDeadType.ClickOut);
                }

                // 死亡产出
                if (isDead && com.config.DieInto.Count > 0)
                {
                    var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = item.coord.x, centerRow = item.coord.y, sourceComponent = com });
                    if (emptyIdx >= 0)
                    {
                        _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                        var idx = _CalculateIdxByCoord(c, r);
                        var dieOutputItem = com.config.DieInto.RandomChooseByWeight(e => e.Value).Key;
                        if (dieOutputItem > 0)
                        {
                            DebugEx.Info($"Board::UseClickItemSource ----> dieinto random select {dieOutputItem}");
                            var dieOutput = _SpawnItem(dieOutputItem, idx, c, r, false, false, false);
                            _SpawnBonusProcess(dieOutput, false, 0, ItemSpawnReason.DieOutput, from: com.item);
                            _OnItemSpawn(ItemSpawnContext.CreateWithSource(null, ItemSpawnContext.SpawnType.DieInto), dieOutput);

                            // 如果result为null 用dieOutput替代result 使调用结果表示操作成功
                            ret ??= dieOutput;
                        }
                    }
                }

                mParent.TriggerItemEvent(item, ItemEventType.ItemEventOutputItem);
                Env.Instance.NotifyItemUse(item, ItemComponentType.MixSource);
                state = ItemUseState.Success;
                return ret;
            }
            state = ItemUseState.CoolingDown;
            return null;
        }

        public Item MixSourceExtract(Item item, int targetId, out ItemUseState state)
        {
            state = ItemUseState.UnknownError;
            if (item.TryGetItemComponent(out ItemMixSourceComponent com) && com.TryExtract(targetId))
            {
                var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = item.coord.x, centerRow = item.coord.y, sourceComponent = com });
                if (emptyIdx >= 0)
                {
                    _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                    var ret = _SpawnItem(targetId, emptyIdx, c, r, false, false, false);
                    _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.MixSourceExtract), ret);
                    return ret;
                }
                else
                {
                    // 进礼物队列
                    return item.world.AddReward(targetId, true);
                }
            }
            return null;
        }

        public void ChangeItem(Item item, int targetId, ItemDeadType deadType, ItemSpawnContext.SpawnType spawnType)
        {
            DebugEx.FormatWarning("Merge::Board.ChangeItem ----> change {0} to {1}, reason:{2}, {3}", item, targetId, deadType, spawnType);
            var coord = item.coord;
            var idx = _CalculateIdxByCoord(coord.x, coord.y);
            _DisposeItem(item, false, deadType);
            _DisposeBonusProcess(item, item, deadType);
            var result = _SpawnItem(targetId, idx, coord.x, coord.y, false, false, false);
            _SpawnBonusProcess(result, false, 0, ItemSpawnReason.None);
            onItemDead?.Invoke(item, deadType);
            _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, spawnType), result);
        }

        public Item UseAutoItemSource(Item item, out ItemUseState state, int maxDist = 1)
        {
            var com = item.GetItemComponent<ItemAutoSourceComponent>();
            if (ItemUtility.IsItemInNormalState(item) && com != null && com.IsNextItemReady())
            {
                var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = item.coord.x, centerRow = item.coord.y, sourceComponent = com, maxDist = maxDist });
                if (emptyIdx >= 0)
                {
                    var nextItem = com.ConsumeNextItem();
                    _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                    var result = _SpawnItem(nextItem, emptyIdx, c, r, false, com.config.Frozen, false);
                    _SpawnBonusProcess(result, false, 0, ItemSpawnReason.AutoSource, from: com.item);
                    _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.AutoSource).WithToast(com.GetToastTypeForItem(nextItem)), result);
                    if (com.isDead)          //now we know source is dead
                    {
                        var newItem = 0;
                        if (com.config.DieInto.Count > 0)
                        {
                            newItem = com.config.DieInto.RandomChooseByWeight(e => e.Value).Key;
                            DebugEx.Info($"Board::UseAutoItemSource ----> dieinto random select {newItem}");
                        }
                        var pos = item.coord;
                        _DisposeItem(item);
                        _DisposeBonusProcess(item, item);
                        if (newItem > 0)
                        {
                            var idx = _CalculateIdxByCoord(pos.x, pos.y);
                            var dieOutput = _SpawnItem(newItem, idx, pos.x, pos.y, false, false);
                            _SpawnBonusProcess(dieOutput, false, 0, ItemSpawnReason.DieOutput, from: com.item);
                        }
                    }

                    mParent.TriggerItemEvent(item, ItemEventType.ItemEventOutputItem);

                    state = ItemUseState.Success;
                    return result;
                }
                else
                {
                    // DebugEx.FormatInfo("Merge::Board.UseAutoItemSource ----> not generate, no room for new source for {0}", item.tid);
                    state = ItemUseState.NotEnoughSpace;
                    return null;
                }
            }
            else
            {
                DebugEx.FormatWarning("Merge::Board.UseAutoItemSource ----> not generate, not ready for {0}", item.tid);
                state = ItemUseState.CoolingDown;
                return null;
            }
        }

        public Item UseChest(Item item, out ItemUseState state)
        {
            var com = item.GetItemComponent<ItemChestComponent>();
            if (com != null && com.canUse)
            {
                var energyCost = com.energyCost;
                if (energyCost > 0 && !Env.Instance.CanUseEnergy(energyCost))
                {
                    DebugEx.FormatWarning("Merge::Board.UseChest ----> no energy for source {0}, cost {1}", item.tid, energyCost);
                    onLackOfEnergy?.Invoke();
                    state = ItemUseState.NotEnoughEnergy;
                    return null;
                }
                var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = item.coord.x, centerRow = item.coord.y, sourceComponent = com });
                if (emptyIdx >= 0)
                {
                    if (energyCost > 0)
                    {
                        Env.Instance.UseEnergy(energyCost, ProduceReason);
                    }
                    var nextItem = com.ConsumeNextItem();
                    _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                    var result = _SpawnItem(nextItem, emptyIdx, c, r, false, com.config.Frozen, false);
                    _SpawnBonusProcess(result, false, energyCost, ItemSpawnReason.ChestSource, from: com.item);
                    _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.None), result);
                    if (com.countLeft <= 0)          //is dead
                    {
                        _DisposeItem(item);
                        _DisposeBonusProcess(item, item);
                    }
                    state = ItemUseState.Success;
                    return result;
                }
                else
                {
                    DebugEx.FormatInfo("Merge::Board.UseChest ----> not generate, no room for new source for {0}", item.tid);
                    state = ItemUseState.NotEnoughSpace;
                    return null;
                }
            }
            else
            {
                DebugEx.FormatInfo("Merge::Board.UseChest ----> not generate, not ready for {0}", item.tid);
                if (com?.isWaiting == true)
                    state = ItemUseState.CoolingDown;
                else
                    state = ItemUseState.UnknownError;
                return null;
            }
        }

        public void UseFeatureEntry(Item item)
        {
            var com = item.GetItemComponent<ItemFeatureComponent>();
            if (com != null)
            {
                onFeatureClicked?.Invoke(item, com.feature);
            }
            else
            {
                DebugEx.FormatWarning("Merge::Board::UseFeatureEntry ----> not feature {0}", item);
            }
        }

        public Item UseBox(Item item)
        {
            var com = item.GetItemComponent<ItemBoxComponent>();
            if (com != null)
            {
                var energyCost = com.energyCost;
                if (energyCost > 0 && !Env.Instance.CanUseEnergy(energyCost))
                {
                    DebugEx.FormatWarning("Merge::Board.UseBox ----> no energy for source {0}, cost {1}", item.tid, energyCost);
                    onLackOfEnergy?.Invoke();
                    return null;
                }
                Item ret = null;
                var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = item.coord.x, centerRow = item.coord.y, sourceComponent = com });
                if (emptyIdx >= 0)
                {
                    var nextItem = com.ConsumeNextItem();
                    if (nextItem > 0)
                    {
                        if (energyCost > 0)
                        {
                            Env.Instance.UseEnergy(energyCost, ProduceReason);
                        }
                        _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                        var result = _SpawnItem(nextItem, emptyIdx, c, r, false, false, false);
                        _SpawnBonusProcess(result, false, energyCost, ItemSpawnReason.BoxSource, from: com.item);
                        _OnItemSpawn(ItemSpawnContext.CreateWithSource(item, ItemSpawnContext.SpawnType.None), result);
                        ret = result;
                    }
                    else
                    {
                        DebugEx.FormatInfo("Merge::Board::UseBox ----> not generate, no item to output", item.tid);
                    }
                }
                else
                {
                    DebugEx.FormatInfo("Merge::Board::UseBox ----> not generate, no room for new source for {0}", item.tid);
                }
                if (com.countLeft <= 0)          //is dead
                {
                    _DisposeItem(item);
                    _DisposeBonusProcess(item, item);
                }
                return ret;
            }
            else
            {
                DebugEx.FormatInfo("Merge::Board::UseBox ----> not generate, not ready for {0}", item.tid);
                return null;
            }
        }

        public bool CanUseBonusItem(Item item)
        {
            var com = item.GetItemComponent<ItemBonusCompoent>();
            return com != null && item.isActive && !item.isDead;
        }

        public bool UseBonusItem(Item item)
        {
            if (CanUseBonusItem(item))
            {
                mParent.UseBonusItem(item);
                return true;
            }
            else
            {
                DebugEx.FormatInfo("Merge::Board.UseBonusItem ----> no bonus for item {0}", item.tid);
                return false;
            }
        }

        public bool CanUseTapBonusItem(Item item)
        {
            var com = item.GetItemComponent<ItemTapBonusComponent>();
            return com != null && item.isActive && !item.isDead;
        }

        public bool UseTapBonusItem(Item item)
        {
            if (CanUseTapBonusItem(item))
            {
                mParent.UseTapBonusItem(item);
                return true;
            }
            else
            {
                DebugEx.FormatInfo("Merge::Board.UseTapBonusItem ----> no bonus for item {0}", item.tid);
                return false;
            }
        }

        public bool CanUseJumpCDItem(Item item)
        {
            var com = item.GetItemComponent<ItemJumpCDComponent>();
            return com != null && item.isActive && !item.isDead;
        }

        public bool UseJumpCD(Item item)
        {
            if (CanUseJumpCDItem(item))
            {
                mParent.UseJumpCDItem(item);
                return true;
            }
            else
            {
                DebugEx.FormatInfo("Merge::Board.UseJumpCD ----> no jumpcd for item {0}", item.tid);
                return false;
            }
        }

        public bool CanUseOrderBoxItem(Item item)
        {
            var com = item.GetItemComponent<ItemOrderBoxComponent>();
            return com != null && item.isActive && !item.isDead;
        }

        public bool UseOrderBox(Item item)
        {
            if (CanUseOrderBoxItem(item))
            {
                mParent.UseOrderBoxItem(item);
                return true;
            }
            else
            {
                DebugEx.FormatInfo("Merge::Board.UseOrderBox ----> no orderbox for item {0}", item.tid);
                return false;
            }
        }

        public Item UnleashBubbleItem(Item item)
        {
            var com = item.GetItemComponent<ItemBubbleComponent>();
            if (com != null)
            {
                //unleash item in bubble
                var pos = item.coord;
                var targetId = item.tid;
                item.world.TriggerItemEvent(item, ItemEventType.ItemBubbleUnleash);
                _DisposeItem(item);
                _DisposeBonusProcess(item, item, ItemDeadType.BubbleUnlease);
                var newItem = _SpawnItem(targetId, _CalculateIdxByCoord(pos.x, pos.y), pos.x, pos.y, false, false, false);
                _SpawnBonusProcess(newItem, false, 0, ItemSpawnReason.BubbleUnlease);
                _OnItemSpawn(ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Bubble), newItem);
                MessageCenter.Get<MSG.ON_USE_SPEED_UP_ITEM_SUCCESS>().Dispatch();
                return newItem;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 转换生成器
        /// </summary>
        /// <param name="item"></param>
        /// <param name="tid"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public Item ConvertItem(Item item, int tid)
        {
            var pos = item.coord;
            _DisposeItem(item);
            var newItem = _SpawnItem(tid, _CalculateIdxByCoord(pos.x, pos.y), pos.x, pos.y, false, false, false);
            return newItem;
        }
        
        public Item KillBubbleItem(Item item, ItemBubbleType type, out int transItemId)
        {
            transItemId = 0;
            var com = item.GetItemComponent<ItemBubbleComponent>();
            if (com != null)
            {
                DebugEx.FormatInfo("Merge::Board.KillBubbleItem ----> bubble item killed {0}, type = {1}", item.tid, type);
                //kill bubble
                var pos = item.coord;
                var targetId = ItemUtility.GetBubbleDeadItemId(type);
                transItemId = targetId;
                var eventType = type == ItemBubbleType.Bubble ? ItemEventType.ItemBubbleBreak : ItemEventType.ItemBubbleFrozenBreak;
                item.world.TriggerItemEvent(item, eventType);
                _DisposeItem(item);
                _DisposeBonusProcess(item, item, ItemDeadType.BubbleTimeout);
                var newItem = _SpawnItem(targetId, _CalculateIdxByCoord(pos.x, pos.y), pos.x, pos.y, false, false);
                _SpawnBonusProcess(newItem, false, 0, ItemSpawnReason.BubbleTimeout);
                return newItem;
            }
            else
            {
                DebugEx.FormatWarning("Merge::Board.KillBubbleItem ----> not a bubble item {0}", item.id);
                return null;
            }
        }

        public Item GetItemByCoord(int col, int row)
        {
            int idx = _CalculateIdxByCoord(col, row);
            if (idx < 0)
            {
                return null;
            }
            else
            {
                return mGrids[idx].item;
            }
        }

        public Item FindConsumableItemById(int tid)
        {
            foreach (var g in mGrids)
            {
                if (g.item != null &&
                    g.item.tid == tid &&
                    g.item.isActive &&
                    !g.item.HasComponent(ItemComponentType.Bubble))
                {
                    return g.item;
                }
            }
            return null;
        }

        public Item FindAnyNormalItemByComponent<T>() where T : ItemComponentBase
        {
            foreach (var g in mGrids)
            {
                if (g.item != null &&
                    g.item.isActive &&
                    !g.item.isDead &&
                    g.item.GetItemComponent<T>() != null)
                {
                    return g.item;
                }
            }
            return null;
        }

        public bool PutItemInInventory(Item item)
        {
            if (item.parent != this)
            {
                DebugEx.FormatWarning("Merge::Board.PutItemInInventory ----> can't move {0} in inventory because it is not on board", item.id);
                return false;
            }
            if (!ItemUtility.CanItemInInventory(item))
            {
                DebugEx.FormatWarning("Merge::Board.PutItemInInventory ----> can't move {0} in inventory because the state is not fit", item.id);
                return false;
            }
            var world = mParent;
            if (world.inventory.PutItem(item, out int idx, out int invId))
            {
                int itemIdx = _CalculateIdxByCoord(item.coord.x, item.coord.y);
                if (itemIdx >= 0 && mGrids[itemIdx].item == item)
                {
                    mGrids[itemIdx].item = null;
                    _ChangeEmptyGridCount(1);
                }
                item.SetParent(null, null);
                onItemToInventory?.Invoke(item);
                onItemLeave?.Invoke(item);
                world.TriggerItemEvent(item, ItemEventType.ItemEventLeaveBoard);
                DataTracker.bag_change.Track(invId, item.tid, true);
                Game.Manager.bagMan.OnItemEnterBag();
                return true;
            }
            else
            {
                DebugEx.FormatWarning("Merge::Board.PutItemInInventory ----> can't move {0} in inventory because no room", item.id);
                return false;
            }
        }

        public bool GetItemFromInventory(int idx, int bagId)
        {
            var world = mParent;
            var item = world.inventory.PeekItem(idx, bagId);
            if (item == null)
            {
                DebugEx.FormatWarning("Merge::Board.GetItemFromInventory ----> can't get idx {0} from inventory because it is empty", idx);
                return false;
            }
            int r = mRows / 2, c = mCols / 2;
            var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = c, centerRow = r, itemTid = item.tid });
            _CalculateCoordByIdx(emptyIdx, out c, out r);
            if (emptyIdx < 0)
            {
                DebugEx.FormatWarning("Merge::Board.GetItemFromInventory ----> can't get idx {0} from inventory because no room", idx);
                return false;
            }
            item.SetParent(this, mGrids[emptyIdx]);
            _SetItemPos(item, -1, emptyIdx, c, r, false);
            world.inventory.RemoveItem(idx, bagId);
            onItemFromInventory?.Invoke(item);
            onItemEnter?.Invoke(item);
            world.TriggerItemEvent(item, ItemEventType.ItemEventEnterBoard);
            DataTracker.bag_change.Track(bagId, item.tid, false);
            Game.Manager.bagMan.OnItemLeaveBag();
            return true;
        }

        public bool CanMerge(Item src, Item dst)
        {
            return ItemUtility.CanMerge(src, dst);
        }

        private List<IMergeBonusHandler> mCachedMergeBonusHandlers = new List<IMergeBonusHandler>();
        public Item Merge(Item src, Item dst)
        {
            if (!CanMerge(src, dst))
            {
                DebugEx.FormatWarning("Merge::Board.Merge ----> not mergeable {0}, {1}", src.id, dst.id);
                return null;
            }
            var srcCom = src.GetItemComponent<ItemMergeComponent>();
            var dstCom = dst.GetItemComponent<ItemMergeComponent>();
            var target = dstCom.PeekMergeResult(srcCom);
            if (target > 0)
            {
                var pos = dst.coord;
                //remove item
                _DisposeItem(src, false);
                _DisposeItem(dst, false);
                var result = _SpawnItem(target, _CalculateIdxByCoord(pos.x, pos.y), pos.x, pos.y, false, false, false);
                _DisposeBonusProcess(src, result);
                _DisposeBonusProcess(dst, result);
                _SpawnBonusProcess(result, false, 0, ItemSpawnReason.Merge);
                result.ProcessPostMerge(src, dst);
                Env.Instance.NotifyItemMerge(result);

                // var level = ItemUtility.GetItemLevel(target);
                // if(level >= 5 && !ItemUtility.IsExpItem(target))
                // {
                //     var emptyIdx = _FindEmptyIdx(result.coord.x, result.coord.y);
                //     if(emptyIdx >= 0 && _CalculateCoordByIdx(emptyIdx, out var c, out var r))
                //     {
                //         SpawnItem(Constant.kMergeExpItemObjId, c, r, false, false);
                //     }
                // }
                mCachedMergeBonusHandlers.Clear();
                world.FillMergeBonusHandler(mCachedMergeBonusHandlers);
                Env.Instance.FillGlobalMergeBonusHandlers(mCachedMergeBonusHandlers);
                mCachedMergeBonusHandlers.Sort((a, b) => a.priority - b.priority);
                var context = new MergeBonusContext()
                {
                    world = world,
                    boardGrids = mGrids,
                    srcId = src.config.Id, result = result
                };
                foreach (var h in mCachedMergeBonusHandlers)
                {
                    DebugEx.FormatInfo("Merge::Board::Merge ----> process handler {0}", h.GetType().Name);
                    h.Process(context);
                }
                //_CheckSpawnPostCard(result);
                //unlock the cross nearby item
                _TriggerUnlockAround(pos.x, pos.y);
                onItemMerge?.Invoke(src, dst, result);
                // FAT_TODO
                // Game.Instance.activityMan.OnItemMerge(this);
                return result;
            }
            else
            {
                DebugEx.FormatInfo("Merge::Board.Merge ----> not mergeable, no target for item {0} and {1}", src.tid, dst.tid);
                return null;
            }
        }

        public MoveState GetMoveState(Item item, int newCol, int newRow)
        {
            var previous = item.coord;
            if (item.isDead || !item.isActive)
            {
                return MoveState.Unknown;
            }
            //棋子自身不可移动
            if (!item.isMovable)
            {
                return MoveState.ItemNotAllowed;
            }
            var gridType = GetGridTid(item.coord.x, item.coord.y);
            if (gridType == (int)GridState.CantMove)
            {
                // item所在位置禁止移动
                return MoveState.GridNotAllowed;
            }
            if (!ItemUtility.CanItemInGridByTid(gridType, item.tid))
            {
                return MoveState.GridNotAllowed;
            }
            if (previous.x != newCol || previous.y != newRow)
            {
                var idx = _CalculateIdxByCoord(newCol, newRow);
                var previousIdx = _CalculateIdxByCoord(previous.x, previous.y);
                if (idx >= 0)
                {
                    var previousItem = mGrids[idx].item;
                    if (previousItem != item)
                    {
                        //目标位置没有棋子或者有棋子且可移动
                        if (previousItem == null || previousItem.isMovable)
                        {
                            return MoveState.CanMove;
                        }
                        else
                        {
                            return MoveState.TargetNotMove;
                        }
                    }
                }
                else
                {
                    return MoveState.Unknown;
                }
            }
            return MoveState.Unknown;
        }

        public bool MoveItem(Item item, int newCol, int newRow, out MoveState state)
        {
            state = GetMoveState(item, newCol, newRow);
            if (state != MoveState.CanMove)
            {
                return false;
            }
            var previous = item.coord;
            if (!item.isDead && item.isActive && (previous.x != newCol || previous.y != newRow))
            {
                var idx = _CalculateIdxByCoord(newCol, newRow);
                var previousIdx = _CalculateIdxByCoord(previous.x, previous.y);
                if (idx >= 0)
                {
                    var previousItem = mGrids[idx].item;
                    if (previousItem != item)
                    {
                        //there is another item
                        if (previousItem == null)
                        {
                            _SetItemPos(item, previousIdx, idx, newCol, newRow);
                        }
                        else if (previousItem.isActive)
                        {
                            // 判断被挤到的新位置是否可以放置
                            if (GetMoveState(previousItem, item.coord.x, item.coord.y) != MoveState.CanMove)
                            {
                                // 不可交换
                                return false;
                            }

                            // // 挤到新位置
                            // int emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam(){centerCol = newCol, centerRow = newRow, item = previousItem});
                            // if (emptyIdx >= 0)
                            // {
                            //     //empty pos is found
                            //     _CalculateCoordByIdx(emptyIdx, out var c, out var r);
                            //     _SetItemPos(previousItem, idx, emptyIdx, c, r);
                            // }
                            // else
                            // {
                            //     //no empty pos, put it in moving item
                            //     _SetItemPos(previousItem, idx, previousIdx, previous.x, previous.y);
                            // }

                            // 交换
                            _SetItemPos(previousItem, idx, previousIdx, previous.x, previous.y);
                            _SetItemPos(item, previousIdx, idx, newCol, newRow);
                        }
                        else
                        {
                            DebugEx.FormatInfo("Merge::Board.MoveItem ----> not move because previous item is not movable!");
                        }
                    }
                }
                else
                {
                    DebugEx.FormatInfo("Merge::Board.MoveItem ----> not move because coord is invalid!");
                }
            }
            return state == MoveState.CanMove;
        }

        public Item SpawnNextRewardItem()
        {
            return SpawnRewardItemByIdx(0);
        }

        private bool _TryConsumeRewardItemImmediately(int idx, out Item rewardItem)
        {
            var world = mParent;
            var nextItem = world.PeekRewardByIdx(idx);
            if (nextItem > 0)
            {
                var cfg = Env.Instance.GetItemComConfig(nextItem);
                if (cfg?.orderBoxConfig != null)
                {
                    // 物品为订单礼盒 且 当前没有正在激活的礼盒
                    if (!world.orderBox.hasActiveOrderBox)
                    {
                        // 直接激活并消耗 无需占用格子
                        rewardItem = world.ConsumeRewardByIdx(idx);
                        if (rewardItem != null && world.orderBox.TryActivateOrderBox(rewardItem.tid))
                        {
                            MessageCenter.Get<MSG.GAME_ORDER_ORDERBOX_BEGIN>().Dispatch(rewardItem);
                        }
                        Env.Instance.NotifyItemUse(rewardItem, ItemComponentType.OrderBox);
                        mParent.TriggerItemEvent(rewardItem, ItemEventType.ItemEventRewardListOut);
                        DebugEx.FormatInfo("Merge::Board::_TryConsumeRewardItemImmediately ----> consume orderbox {0}:{1}", idx, rewardItem);
                        return true;
                    }
                }
                else if (ItemUtility.IsCardPack(nextItem))
                {
                    //只有不在开卡包流程中时才开卡包
                    if (!Game.Manager.cardMan.IsInOpenPackState)
                    {
                        UIFlyFactory.GetFlyTarget(FlyType.MergeItemFlyTarget, out var fromPos);
                        // 直接打开卡包并消耗 无需占用格子
                        rewardItem = world.ConsumeRewardByIdx(idx);
                        // 卡包不是真实棋子 没有component类别
                        mParent.TriggerItemEvent(rewardItem, ItemEventType.ItemEventRewardListOut);
                        DebugEx.FormatInfo("Merge::Board::_TryConsumeRewardItemImmediately ----> consume cardpack {0}:{1}", idx, rewardItem);
                        //开卡包
                        Game.Manager.cardMan.TryOpenCardPack(rewardItem.tid, fromPos);
                        return true;
                    }
                    //若因为连点导致尝试开下一张卡包 则返回一个空item 但结果返回true(否则卡包就被发到棋盘上了)
                    else
                    {
                        rewardItem = null;
                        return true;
                    }
                }
                else if ((world.activeBoard?.boardId ?? 0) == Constant.MainBoardId)
                {
                    //检查是否有不属于主棋盘的棋子被发到了主棋盘奖励箱里 如果有的话 在点击时直接消耗掉
                    var objConf = Env.Instance.GetItemMergeConfig(nextItem);
                    if (objConf != null && objConf.BoardId > Constant.MainBoardId)  //objConf.BoardId大于1说明本棋子不想发到主棋盘
                    {
                        // 直接消耗 无需占用格子
                        rewardItem = world.ConsumeRewardByIdx(idx);
                        mParent.TriggerItemEvent(rewardItem, ItemEventType.ItemEventRewardDisappear);
                        DebugEx.FormatInfo("Merge::Board::_TryConsumeRewardItemImmediately ----> consume activity board item {0}:{1}", idx, rewardItem);
                        return true;
                    }
                }
            }

            rewardItem = null;
            return false;
        }

        private bool _TryUseRewardItemImmediately(Item item)
        {
            if (item.HasComponent(ItemComponentType.JumpCD))
            {
                // 物品是<跳过冷却> 且当前没有正在激活的效果
                if (!world.jumpCD.hasActiveJumpCD)
                {
                    if (UseJumpCD(item))
                    {
                        Env.Instance.NotifyItemUse(item, ItemComponentType.JumpCD);
                        DebugEx.FormatInfo("Merge::Board::_TryUseRewardItemImmediately ----> use jumpcd {0}", item);
                    }
                    return true;
                }
            }
            return false;
        }

        public Item SpawnRewardItemByIdx(int idx)
        {
            // 尝试立即消耗 (不进入棋盘)
            if (_TryConsumeRewardItemImmediately(idx, out var rewardItem))
            {
                return rewardItem;
            }

            var world = mParent;
            var emptyId = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = 3, centerRow = 3, itemTid = world.PeekRewardByIdx(idx) });
            if (emptyId >= 0 && _CalculateCoordByIdx(emptyId, out var c, out var r))
            {
                var nextReward = world.ConsumeRewardByIdx(idx);
                if (nextReward != null)
                {
                    nextReward.SetParent(this, mGrids[emptyId]);
                    _SetItemPos(nextReward, -1, emptyId, c, r, false);
                    onItemEnter?.Invoke(nextReward);
                    mParent.TriggerItemEvent(nextReward, ItemEventType.ItemEventRewardListOut);
                    // _OnItemSpawn(ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.RewardList), item);
                    DebugEx.FormatInfo("Merge::Board::SpawnRewardItemByIdx ----> spawn item {0}:{1}", idx, nextReward);
                    // 尝试立即使用
                    _TryUseRewardItemImmediately(nextReward);
                    return nextReward;
                }
                else
                {
                    DebugEx.FormatWarning("Merge::Board::SpawnRewardItemByIdx ----> no reward item at idx {0}", idx);
                    return null;
                }
            }
            else
            {
                DebugEx.FormatWarning("Merge::Board::SpawnRewardItemByIdx ----> no empty room for reward item");
                return null;
            }
        }

        public void TriggerItemComponentChange(Item item)
        {
            onItemComponentChange?.Invoke(item);
        }

        public bool SellItem(Item item)
        {
            int itemIdx = _CalculateIdxByCoord(item.coord.x, item.coord.y);
            if (itemIdx < 0 || mGrids[itemIdx].item != item)
            {
                DebugEx.FormatWarning("Merge::Board.SellItem ----> item idx wrong:{0}, {1}", itemIdx, item.id);
                return false;
            }
            var world = mParent;

            var (id, num) = ItemUtility.GetSellReward(item.tid);
            if (num >= 0)
            {
                mGrids[itemIdx].item = null;
                _ChangeEmptyGridCount(1);
                // 卖出时结算可能获得的活动体力奖励
                _DisposeBonusProcess(item, item);
                item.SetParent(null, null);
                onItemLeave?.Invoke(item);
                world.TriggerItemEvent(item, ItemEventType.ItemEventLeaveBoard);

                var reward = Env.Instance.SellItem(id, num);
                world.SetSoldItem(item);
                onItemSell?.Invoke(item, reward);

                DebugEx.Info($"Merge::Board.SellItem ----> item {item.id}({item.tid}) sold for {id}x{num}");
                return true;
            }
            else
            {
                DebugEx.Warning($"Merge::Board.SellItem ----> item cant sell {item.id}({item.tid})");
                return false;
            }
        }

        public void TriggerUseTimeSkipper(Item item)
        {
            onUseTimeSkipper?.Invoke(item);
        }

        public void UnfrozenItemByGem(Item item)
        {
            if (item.parent != this)
            {
                DebugEx.FormatWarning("Merge::Board ----> Unfrozen: not on me");
                return;
            }
            int price = ItemUtility.GetUnfrozenPrice(item);
            if (price > 0 && Env.Instance.CanUseGem(price))
            {
                Env.Instance.UseGem(price, ReasonString.unfrozen, () =>
                {
                    UnfrozenItem(item);
                });
            }
        }

        public void UnfrozenItem(Item item)
        {
            item.SetState(false, false);
            onItemStateChange?.Invoke(item, null);
        }

        public void FreezeItem(Item item)
        {
            item.SetState(false, true);
            onItemStateChange?.Invoke(item, null);
        }

        public bool UndoSellItem()
        {
            var world = mParent;
            var item = world.undoItem;
            if (item == null)
            {
                DebugEx.FormatWarning("Merge::Board.UndoSellItem ----> no item");
                return false;
            }
            int r = item.coord.y, c = item.coord.x;
            var idx = _CalculateIdxByCoord(c, r);
            if (idx < 0)
            {
                DebugEx.FormatWarning("Merge::Board.UndoSellItem ----> coord is illigal {0},{1}", c, r);
                return false;
            }
            var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = c, centerRow = r, itemTid = item.tid });
            if (emptyIdx >= 0)
            {
                idx = emptyIdx;
                _CalculateCoordByIdx(idx, out c, out r);
            }
            var currentItem = mGrids[idx].item;
            if (currentItem != null)
            {
                if (currentItem.HasComponent(ItemComponentType.AutoSouce) ||
                    currentItem.HasComponent(ItemComponentType.ClickSouce) ||
                    !currentItem.isActive)
                {
                    DebugEx.FormatWarning("Merge::Board.UndoSellItem ----> a source item is in {0},{1}:{2}", c, r, currentItem.tid);
                    return false;
                }
            }
            if (world.GrabSelledItem() != item)
            {
                DebugEx.FormatWarning("Merge::Board.UndoSellItem --> grab selled item fail:{0}:{1}", world.undoItem?.id, world.undoItem?.tid);
                return false;
            }
            if (currentItem != null)
            {
                world.AddReward(currentItem.tid);
                _DisposeItem(currentItem);
                _DisposeBonusProcess(currentItem, currentItem);
            }
            DebugEx.FormatInfo("Merge::Board.UndoSellItem ---> undo item {0}:{1}", item.id, item.tid);
            item.SetParent(this, mGrids[idx]);
            _SetItemPos(item, -1, idx, c, r, false);
            _OnItemSpawn(ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Undo), item);
            onItemEnter?.Invoke(item);
            world.TriggerItemEvent(item, ItemEventType.ItemEventEnterBoard);
            return true;
        }

        public void TriggerLevelUnlock()
        {
            WalkAllItem(item =>
            {
                if (item.isLocked && item.unLockLevel > 0 && item.isReachBoardLevel)
                {
                    item.SetState(false, item.isFrozen);
                    _TriggerItemStateChange(item);
                }
            });
        }

        public void AddEffect(SpeedEffect effect)
        {
            mAllEffects.Add(effect);
            onEffectChange?.Invoke(effect);
        }

        public void KillEffect(SpeedEffect effect)
        {
            var idx = mAllEffects.IndexOf(effect);
            if (effect != null)
            {
                effect.SetDead();
            }
            mAllEffects.Remove(effect);
            TriggerEffectChange(effect);
        }

        public int CalculateIdxByCoord(int col, int row)
        {
            return _CalculateIdxByCoord(col, row);
        }

        public void WalkEffects(System.Action<SpeedEffect> cb)
        {
            foreach (var e in mAllEffects)
            {
                if (!e.isDead && !e.isDying)
                {
                    cb?.Invoke(e);
                }
            }
        }

        public void TriggerEffectChange(SpeedEffect effect)
        {
            DebugEx.FormatInfo("Merge::Board::TriggerEffectChange for creator {0},{1}", effect.creator?.id, effect.creator?.tid);
            _NotifyItemComponents<IComponentEventsEffectChange>((c) => c.OnEffectChanged(effect));
            onEffectChange?.Invoke(effect);
        }

        public void TriggerItemStatusChange(Item item)
        {
            _TriggerItemStateChange(item);
        }

        public int CalcTimeScale(Item item)
        {
            return mTimeScaleEffect.CalculateTimeScale(item);
        }

        public void TriggerUseTimeScaleSource(Item item)
        {
            mTimeScaleEffect.TriggerUseTimeScaleSource(item);
            onUseTimeScaleSource?.Invoke(item);
        }

        public void TriggerJumpCDBegin(Item item)
        {
            onJumpCDBegin?.Invoke(item);
        }

        public void TriggerJumpCDEnd()
        {
            onJumpCDEnd?.Invoke();
        }

        private void _TriggerUnlockAround(int col, int row, ItemStateChangeContext context = null)
        {
            _UnlockItem(col, row - 1, context);
            _UnlockItem(col, row + 1, context);
            _UnlockItem(col - 1, row, context);
            _UnlockItem(col + 1, row, context);
        }

        private void _NotifyItemComponents<T>(System.Action<T> notifier) where T : class
        {
            foreach (var grid in mGrids)
            {
                if (grid.item != null)
                {
                    grid.item.WalkAllComponents<T>(notifier);
                }
            }
        }

        //直接检查棋盘上是否有活跃的Bonus棋子
        public bool CheckHasBonusItem()
        {
            for (int i = 0; i < mGrids.Length; i++)
            {
                var item = mGrids[i].item;
                if (item != null && item.isActive)
                {
                    if (item.HasComponent(ItemComponentType.Bonus) || item.HasComponent(ItemComponentType.TapBonus))
                        return true;
                }
            }
            return false;
        }

        private struct FindEmptyIndexParam
        {
            public int centerCol;
            public int centerRow;
            public int itemTid;             //如果是0，就按普通棋子算，普通棋子不能放的地方，就不能返回
            public IEnumerable<int> itemTids;         //按顺序全得找到匹配, 如果是0，就按普通棋子算
            public ItemComponentBase sourceComponent;
            public Item item;
            public int maxDist;
        }

        //直接检查棋盘上是否有空格子
        public bool CheckHasEmptyIdx()
        {
            for (int i = 0; i < mGrids.Length; i++)
            {
                if (mGrids[i].item == null && !mLockedPosIdx.Contains(i))
                    return true;
            }
            return false;
        }

        private int _FindEmptyIdx(int centercol, int centerrow, int maxDist = -1)
        {
            return _FindEmptyIdx(new FindEmptyIndexParam()
            {
                centerCol = centercol,
                centerRow = centerrow,
                maxDist = maxDist
            });
        }

        List<int> mCachedEmptyIdx = new List<int>();
        private int _FindEmptyIdx(FindEmptyIndexParam param)
        {
            mCachedEmptyIdx.Clear();
            _FindEmptyIdxes(mCachedEmptyIdx, param);
            if (mCachedEmptyIdx.Count > 0)
            {
                return mCachedEmptyIdx[0];
            }
            else
            {
                return -1;
            }
        }

        private bool _FindEmptyIdxes(List<int> container, FindEmptyIndexParam param)
        {
            int count = 0;

            int targetId = param.itemTid;
            if (param.item != null)
            {
                targetId = param.item.tid;
                if (param.item.HasComponent(ItemComponentType.Bubble))
                {
                    targetId = 0;
                }
            }

            IEnumerator<int> targetIds = null;
            if (param.itemTids != null)          //如果提供了itemTids，则itemTid这个字段失效
            {
                targetId = -1;
                targetIds = param.itemTids.GetEnumerator();
                if (!targetIds.MoveNext())
                {
                    targetIds = null;
                }
                else
                {
                    targetId = targetIds.Current;
                }
            }
            if (targetId >= 0)
            {
                _WalkNearestFirst(true, param.centerCol, param.centerRow, (idx) =>
                {
                    if (mGrids[idx].item == null && !mLockedPosIdx.Contains(idx))
                    {
                        if (ItemUtility.CanItemInGridByTid(mGrids[idx].gridTid, targetId) &&
                            (param.sourceComponent == null || ItemUtility.CanSourceOutputInGrid(mGrids[idx].gridTid, param.sourceComponent)))
                        {
                            container.Add(idx);
                            count++;
                            targetId = -1;
                            if (targetIds != null)
                            {
                                if (targetIds.MoveNext())
                                {
                                    targetId = targetIds.Current;
                                }
                                else
                                {
                                    targetIds = null;
                                }
                            }
                        }
                    }
                    return targetId >= 0;
                }, param.maxDist);
            }
            return targetId < 0 && targetIds == null;
        }

        private bool mInGridWalk = false;
        public void Update(int milli)
        {
            do
            {
                var _dist = _GetNextCheckPointDist(milli);
                UpdateChest(_dist);
                _Update(_dist);
                milli -= _dist;
            }
            while (milli > 0);
        }

        // 查询需要分割结算时的节点
        // 目前只需要处理特斯拉
        private int _GetNextCheckPointDist(int milli)
        {
            var life = mTimeScaleEffect.GetNextTimeScaleItemLifeMilli();
            if (life < milli)
                return life;
            return milli;
        }

        public void UpdateChest(int milli)
        {
            if (mParent.currentWaitChest > 0)          //process chest
            {
                var item = FindItemById(mParent.currentWaitChest);
                if (item == null)
                {
                    DebugEx.FormatError("Board::_UpdateChest ----> chest not exists any more {0}", mParent.currentWaitChest);
                    mParent.SetWaitChest(null);
                    return;
                }

                // ========================
                // 受时间加速道具影响
                var scale = CalcTimeScale(item);
                milli *= scale;
                // ========================

                var component = item.GetItemComponent<ItemChestComponent>();
                var waitChestTime = mParent.ForwardChestWaitTime(EffectUtility.CalculateMilliBySpeedEffect(component, milli));
                if (component.config.WaitTime * 1000 <= waitChestTime)
                {
                    component.SetOpen();
                }
            }
        }

        private void _Update(int milli)
        {
            for (int i = 0; i < mGrids.Length; i++)
            {
                var item = mGrids[i].item;
                if (item != null && !item.isDead && item.isActive)
                {
                    item.Update(milli);
                }
            }
        }

        private static readonly int[] kSearchRange = new int[] {
            -1,-1, 1,-1,
            1,-1, 1, 1,
            1, 1, -1, 1,
            -1, 1, -1, -1
        };
        private void _WalkNearestFirst(bool includeSelf, int centerCol, int centerRow, System.Func<int, bool> onWalk, int maxDist = -1)
        {
            if (maxDist <= 0)
            {
                maxDist = mLongestSideLength - 1;
            }
            if (includeSelf)
            {
                int idx = _CalculateIdxByCoord(centerCol, centerRow);
                if (idx >= 0 && !onWalk.Invoke(idx))
                {
                    return;
                }
            }
            for (int delta = 1; delta <= maxDist; delta++)
            {
                for (int i = 0; i < kSearchRange.Length; i += 4)
                {
                    int startCol = centerCol + kSearchRange[i] * delta, startRow = centerRow + kSearchRange[i + 1] * delta;
                    int endCol = centerCol + kSearchRange[i + 2] * delta, endRow = centerRow + kSearchRange[i + 3] * delta;
                    int stride = 0;
                    if (startCol < endCol)
                    {
                        startCol++;
                        if (_ClampRange(ref startCol, ref endCol, mCols) && startRow >= 0 && startRow < mRows)
                        {
                            stride = 1;
                        }
                    }
                    else if (startCol > endCol)
                    {
                        startCol--;
                        if (_ClampRange(ref endCol, ref startCol, mCols) && startRow >= 0 && startRow < mRows)
                        {
                            stride = -1;
                        }
                    }
                    else if (startRow < endRow)
                    {
                        startRow++;
                        if (_ClampRange(ref startRow, ref endRow, mRows) && startCol >= 0 && startCol < mCols)
                        {
                            stride = mCols;
                        }
                    }
                    else if (startRow > endRow)
                    {
                        startRow--;
                        if (_ClampRange(ref endRow, ref startRow, mRows) && startCol >= 0 && startCol < mCols)
                        {
                            stride = -mCols;
                        }
                    }
                    if (stride == 0)     //the range is impossible
                    {
                        continue;
                    }
                    int startIdx = _CalculateIdxByCoord(startCol, startRow), endIdx = _CalculateIdxByCoord(endCol, endRow);
                    endIdx += stride;           //let the end condition be a invalid condition
                    for (int idx = startIdx; idx != endIdx; idx += stride)
                    {
                        if (!onWalk(idx))
                        {
                            return;
                        }
                    }
                }
            }
        }

        private bool _ClampRange(ref int min, ref int max, int length)
        {
            if (min < 0)
            {
                min = 0;
            }
            if (max >= length)
            {
                max = length - 1;
            }
            return min <= max;
        }

        private void _SetItemPos(Item item, int previousIdx, int idx, int col, int row, bool triggerEvent = true)
        {
            item.SetPosition(col, row, mGrids[idx]);
            bool hasPrevious = previousIdx >= 0 && mGrids[previousIdx].item == item;
            bool targetEmpty = idx >= 0 && mGrids[idx].item == null;
            if (hasPrevious)
            {
                mGrids[previousIdx].item = null;
            }
            mGrids[idx].item = item;
            if (targetEmpty && !hasPrevious)
            {
                _ChangeEmptyGridCount(-1);
            }
            else if (!targetEmpty && hasPrevious)
            {
                _ChangeEmptyGridCount(1);
            }
            item.WalkAllComponents<IComponentEventsItemMove>((c) => c.OnItemMove());
            if (triggerEvent)
            {
                onItemMove?.Invoke(item);
            }
        }

        private List<IDisposeBonusHandler> mCachedDisposeBonusHandlers = new List<IDisposeBonusHandler>();
        private DisposeBonusContext mDisposeBonusDefaultContext = new DisposeBonusContext();
        private int _DisposeBonusHandlerSort(IDisposeBonusHandler a, IDisposeBonusHandler b) { return a.priority - b.priority; }
        private void _DisposeBonusProcess(Item self, Item target, ItemDeadType deadType = ItemDeadType.Common)
        {
            mCachedDisposeBonusHandlers.Clear();
            Env.Instance.FillGlobalDisposeBonusHandlers(mCachedDisposeBonusHandlers);
            if (mCachedDisposeBonusHandlers.Count < 1)
                return;
            mDisposeBonusDefaultContext.world = world;
            mDisposeBonusDefaultContext.item = self;
            mDisposeBonusDefaultContext.dieToTarget = target;
            mDisposeBonusDefaultContext.deadType = deadType;
            mCachedDisposeBonusHandlers.Sort(_DisposeBonusHandlerSort);
            foreach (var h in mCachedDisposeBonusHandlers)
            {
                DebugEx.FormatInfo("Merge::Board::Dispose ----> process handler {0}", h.GetType().Name);
                h.Process(mDisposeBonusDefaultContext);
            }
        }

        private void _DisposeItem(Item item, bool triggerEvent = true, ItemDeadType deadType = ItemDeadType.Common)
        {
            var coord = item.coord;
            var idx = _CalculateIdxByCoord(coord.x, coord.y);
            if (mGrids[idx].item == item)
            {
                mGrids[idx].item = null;
                _ChangeEmptyGridCount(1);
            }
            var world = mParent;
            world.FinishDisposeItem(item);
            if (triggerEvent)
            {
                onItemDead?.Invoke(item, deadType);
            }
            onItemLeave?.Invoke(item);
            world.TriggerItemEvent(item, ItemEventType.ItemEventLeaveBoard);
            DebugEx.FormatInfo("Merge::Board._DisposeItem ----> item {0}({1},{2}) is disposed", item.id, item.coord.x, item.coord.y);
        }

        private List<ISpawnBonusHandler> mCachedSpawnBonusHandlers = new List<ISpawnBonusHandler>();
        private SpawnBonusContext mSpawnBonusDefaultContext = new SpawnBonusContext();
        private int _SpawnBonusHandlerSort(ISpawnBonusHandler a, ISpawnBonusHandler b) { return a.priority - b.priority; }
        private void _SpawnBonusProcess(Item spawnResult, bool isBubble, int energyCost, ItemSpawnReason reason, Item from = null)
        {
            // handler里不关心未定义spawn行为
            if (reason == ItemSpawnReason.None)
                return;

            mCachedSpawnBonusHandlers.Clear();
            Env.Instance.FillGlobalSpawnBonusHandlers(mCachedSpawnBonusHandlers);
            if (mCachedSpawnBonusHandlers.Count < 1)
                return;

            var context = mSpawnBonusDefaultContext;
            context.world = world;
            context.boardGrids = mGrids;
            context.from = from;
            context.result = spawnResult;
            context.isBubble = isBubble;
            context.energyCost = energyCost;
            context.reason = reason;

            mCachedSpawnBonusHandlers.Sort(_SpawnBonusHandlerSort);
            foreach (var h in mCachedSpawnBonusHandlers)
            {
                // DebugEx.FormatInfo("Merge::Board::Spawn ----> process handler {0}", h.GetType().Name);
                h.Process(context);
            }
        }

        private Item _SpawnItem(int id, int idx, int col, int row, bool locked, bool frozen, bool triggerEvent = true)
        {
            var item = new Item(mParent.ConsumeNextItemId(), mParent);
            item.SetParent(this, mGrids[idx]);
            item.InitWithNormalItem(id);
            _SetItemPos(item, -1, idx, col, row, false);
            item.SetState(locked, frozen);
            var world = mParent;
            onItemEnter?.Invoke(item);
            world.TriggerItemEvent(item, ItemEventType.ItemEventEnterBoard);
            if (triggerEvent)
            {
                _OnItemSpawn(ItemSpawnContext.Create(), item);
            }
            DebugEx.FormatInfo("Merge::Board._SpawnItem ----> item {0}({1}), coord({2},{3}) is spawned", item.tid, item.id, item.coord.x, item.coord.y);
            return item;
        }

        private Item _SpawnItemByConf(int idx, int col, int row, string itemConf, bool triggerEvent = true)
        {
            var itemGridConf = itemConf?.ConvertToMergeGridItem();
            //配置无效
            if (itemGridConf == null)
            {
                DebugEx.FormatError("Merge::Board._SpawnItemByConf ----> fail because itemConf {0} is invalid!", itemConf);
                return null;
            }
            //配置的空格子
            if (itemGridConf.Id <= 0) return null;
            //根据配置决定棋子当前状态
            bool frozen = true; // 蜘蛛网
            bool locked = true; // 纸箱
            switch (itemGridConf.State)
            {
                case 1: locked = false; break;
                case 2: locked = false; frozen = false; break;
                case 3: frozen = false; break;
            }
            //初始化棋子
            var item = new Item(mParent.ConsumeNextItemId(), mParent);
            item.SetParent(this, mGrids[idx]);
            item.InitWithNormalItem(itemGridConf.Id);
            _SetItemPos(item, -1, idx, col, row, false);
            item.SetState(locked, frozen);
            item.SetStateConfParam(itemGridConf.State, itemGridConf.Param);
            var world = mParent;
            onItemEnter?.Invoke(item);
            world.TriggerItemEvent(item, ItemEventType.ItemEventEnterBoard);
            if (triggerEvent)
            {
                _OnItemSpawn(ItemSpawnContext.Create(), item);
            }
            DebugEx.FormatInfo("Merge::Board._SpawnItemByConf ----> item: {0}({1},{2}) is spawned, itemConf: ({3})",
                item.id, item.coord.x, item.coord.y, itemConf);
            return item;
        }

        private void _OnItemSpawn(ItemSpawnContext cxt, Item item)
        {
            onItemSpawn?.Invoke(cxt, item);
            item.ProcessPostSpawn(cxt);
        }

        private void _OnItemSpawnFly(Item item, List<RewardCommitData> reward)
        {
            onItemSpawnFly?.Invoke(item, reward);
        }

        public Item TrySpawnBubbleItem(Item srcItem, int targetId, string spawnTypeStr)
        {
            var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = srcItem.coord.x, centerRow = srcItem.coord.y });       //泡泡不能放在特殊grid上，就视为普通item
            if (emptyIdx >= 0 && _CalculateCoordByIdx(emptyIdx, out var c, out var r))
            {
                var bubbleItem = _SpawnBubbleItem(targetId, emptyIdx, c, r, false);
                _OnItemSpawn(ItemSpawnContext.CreateWithSource(srcItem, ItemSpawnContext.SpawnType.None).WithSpawnType(spawnTypeStr), bubbleItem);
                return bubbleItem;
            }
            else
            {
                return null;
            }
        }

        private Item _SpawnBubbleItem(int id, int idx, int col, int row, bool triggerEvent = true)
        {
            var world = mParent;
            var item = new Item(world.ConsumeNextItemId(), mParent);
            item.SetParent(this, mGrids[idx]);
            item.InitWithBubbleItem(id, ItemBubbleType.Bubble);
            _SpawnBonusProcess(item, true, 0, ItemSpawnReason.BubbleBorn);
            _SetItemPos(item, -1, idx, col, row, false);
            if (triggerEvent)
            {
                _OnItemSpawn(ItemSpawnContext.Create(), item);
            }
            onItemEnter?.Invoke(item);
            world.TriggerItemEvent(item, ItemEventType.ItemEventEnterBoard);
            DebugEx.FormatInfo("Merge::Board._SpawnBubbleItem ----> item {0}({1},{2}) is spawned", item.id, item.coord.x, item.coord.y);
            return item;
        }
        
        public Item TrySpawnFrozenItem(Item srcItem, int targetId, long lifeTime)
        {
            var emptyIdx = _FindEmptyIdx(new FindEmptyIndexParam() { centerCol = srcItem.coord.x, centerRow = srcItem.coord.y });       //泡泡不能放在特殊grid上，就视为普通item
            if (emptyIdx >= 0 && _CalculateCoordByIdx(emptyIdx, out var c, out var r))
            {
                var bubbleItem = _SpawnFrozenItem(lifeTime, targetId, emptyIdx, c, r, false);
                _OnItemSpawn(ItemSpawnContext.CreateWithSource(srcItem, ItemSpawnContext.SpawnType.None), bubbleItem);
                return bubbleItem;
            }
            else
            {
                return null;
            }
        }

        private Item _SpawnFrozenItem(long lifeTime, int id, int idx, int col, int row, bool triggerEvent = true)
        {
            var world = mParent;
            var item = new Item(world.ConsumeNextItemId(), mParent);
            item.SetParent(this, mGrids[idx]);
            item.InitWithBubbleItem(id, ItemBubbleType.Frozen, lifeTime);
            _SpawnBonusProcess(item, true, 0, ItemSpawnReason.BubbleBorn);
            _SetItemPos(item, -1, idx, col, row, false);
            if (triggerEvent)
            {
                _OnItemSpawn(ItemSpawnContext.Create(), item);
            }
            onItemEnter?.Invoke(item);
            world.TriggerItemEvent(item, ItemEventType.ItemEventEnterBoard);
            DebugEx.FormatInfo("Merge::Board._SpawnFrozenItem ----> item {0}({1},{2}) is spawned", item.tid, item.coord.x, item.coord.y);
            return item;
        }

        private bool _UnlockItem(int col, int row, ItemStateChangeContext context = null)
        {
            var idx = _CalculateIdxByCoord(col, row);
            if (idx < 0)
            {
                return false;
            }
            var item = mGrids[idx].item;
            if (item != null && ItemUtility.CanUnlock(item))
            {
                item.SetState(false, item.isFrozen);
                _TriggerItemStateChange(item, context);
                //本棋子解锁后 是否可以进一步尝试解锁周围棋子
                if (item.CanUnlockAround())
                {
                    _TriggerUnlockAround(col, row, context);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private int _CalculateIdxByCoord(int col, int row)
        {
            if (col < 0 || col >= mCols || row < 0 || row >= mRows)
            {
                return -1;
            }

            return row * mCols + col;
        }

        private bool _CalculateCoordByIdx(int idx, out int col, out int row)
        {
            row = idx / mCols;
            col = idx % mCols;
            return idx >= 0 && idx < mGrids.Length;
        }

        private void _Reset(int col, int row)
        {
            //check empty
            for (int i = 0; i < mGrids.Length; i++)
            {
                if (mGrids[i].item != null)
                {
                    _CalculateCoordByIdx(i, out var c, out var r);
                    DebugEx.FormatWarning("Board::_Reset ----> fail, ({0},{1}) not empty", c, r);
                    return;
                }
            }
            _SetSize(col, row);
            mAllEffects.Clear();
            mTimeScaleEffect.Reset(col, row, this);
        }

        private void _SetSize(int col, int row)
        {
            mCols = col;
            mRows = row;
            mLongestSideLength = Mathf.Max(mCols, mRows);
            mGrids = new MergeGrid[col * row];
            for (int i = 0; i < mGrids.Length; i++)
            {
                mGrids[i] = new MergeGrid();
            }
            mGridsCloud = new bool[mGrids.Length];
            _RefreshEmptyGridCount();
        }

        private void _TriggerItemStateChange(Item item, ItemStateChangeContext context = null)
        {
            item.WalkAllComponents<IComponentEventsItemStatusChange>((c) => c.onItemStatusChange());
            onItemStateChange?.Invoke(item, context);
        }

        private void _RefreshEmptyGridCount()
        {
            mEmptyGridCount = 0;
            for (int i = 0; i < mGrids.Length; i++)
            {
                if (mGrids[i].item == null)
                {
                    mEmptyGridCount++;
                }
            }
        }

        private void _RefreshCloudMark()
        {
            for (int i = 0; i < mGridsCloud.Length; i++)
            {
                mGridsCloud[i] = false;
            }
            foreach (var cloud in mClouds)
            {
                if (cloud.IsUnlock)
                    continue;
                foreach (var coord in cloud.CloudArea)
                {
                    var idx = _CalculateIdxByCoord(coord.col, coord.row);
                    if (idx >= 0 && idx < mGridsCloud.Length)
                    {
                        mGridsCloud[idx] = true;
                    }
                }
            }
        }

        private void _ChangeEmptyGridCount(int delta)
        {
            mEmptyGridCount += delta;
            int oldCount = mEmptyGridCount;
            _RefreshEmptyGridCount();           //TODO: change it
            if (oldCount != mEmptyGridCount)
            {
                DebugEx.FormatError("Board::_ChangeEmptyGridCount ----> FUCK, empty count not equal! {0} vs {1}", oldCount, mEmptyGridCount);
            }
        }
    }
}
