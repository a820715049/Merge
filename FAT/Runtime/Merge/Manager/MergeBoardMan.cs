/**
 * @Author: handong.liu
 * @Date: 2021-05-27 20:16:04
 */
using System.Collections.Generic;
using EL;
using FAT.Merge;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    [System.Flags]
    public enum ClearMergeBoardHint
    {
        ReserveCommonItem = 1
    }
    public class ClearBoardResult
    {
        public enum ItemResultType
        {
            Sell,
            Destroy,
            BubbleBreak,
            RewardList
        }
        public struct ItemDestroyType
        {
            public Item item;
            public ItemResultType type;
            public int coin;

            public override string ToString()
            {
                return string.Format("{0}-{1}-{2}", item, type, coin);
            }
        }
        public struct RewardListDestroyType
        {
            public int idx;
            public int tid;
            public ItemResultType type;
            public int coin;

            public override string ToString()
            {
                return string.Format("{0}-{1}-{2}-{3}", idx, tid, type, coin);
            }
        }
        public List<ItemDestroyType> allItemResult = new List<ItemDestroyType>();
        public List<RewardListDestroyType> allRewardListResult = new List<RewardListDestroyType>();
        public List<RewardCommitData> rewardToCommit = null;
        public bool isNoReward => CalculateTotalGem() <= 0 && CalculateTotalEnergy() <= 0 && CalculateTotalMergeCoin() <= 0;

        public override string ToString()
        {
            return string.Format("coin:{0}, allItemResult: {1}, allRewardListResult: {2}",
                CalculateTotalMergeCoin(), allItemResult?.ToStringEx(), allRewardListResult?.ToStringEx());
        }

        public int CalculateTotalMergeCoin()
        {
            int ret = 0;
            for (int i = 0; i < allItemResult.Count; i++)
            {
                ret += allItemResult[i].coin;
            }
            for (int i = 0; i < allRewardListResult.Count; i++)
            {
                ret += allRewardListResult[i].coin;
            }
            return ret;
        }

        public int CalculateTotalGem()
        {
            int ret = 0;
            return ret;
        }

        public int CalculateTotalEnergy()
        {
            int ret = 0;
            return ret;
        }
    }
}

namespace FAT
{
    public class MergeWorldEntry
    {
        //合成世界实体类型
        public enum EntryType
        {
            MainGame,   //主棋盘
            MiniBoard,  //迷你棋盘
            MiniBoardMulti,     //多轮迷你棋盘
            MineBoard,          //挖矿棋盘
            FishingBoard,       //钓鱼棋盘
            FarmBoard,          //农场棋盘
            FightBoard,
            WishBoard,  //许愿棋盘
            MineCartBoard,      //矿车棋盘
            TrainMission,
        }
        public EntryType type;
        public MergeWorld world;
        public string nameKey;          //这是一个i18n key
        public Config.AssetConfig icon;
    }

    //合成棋盘管理器 控制所有的棋盘相关逻辑
    public class MergeBoardMan : IGameModule, IUserDataHolder
    {
        public MergeWorld activeWorld => mCurrentActiveWorld;
        public MergeWorldTracer activeTracer => mCurrentActiveTracer;
        public Item activeItem => mCurrentInteractingItem;
        public Item recentActiveItem => mRecentInteractingItem;
        public MergeGlobal globalData => mGlobalData;
        private MergeWorld mCurrentActiveWorld = null;
        private MergeWorldTracer mCurrentActiveTracer = null;
        private Item mCurrentInteractingItem = null; // 正在交互的item
        private Item mRecentInteractingItem = null; // 最近交互过的item | 仅记录非空item
        private List<MergeWorldEntry> mAllMergeWorld = new List<MergeWorldEntry>();
        private IDictionary<int, MergeBoardGrp> mAllMergeBoardGrp;
        private IDictionary<int, MergeBoard> mAllBoardConfigs;
        private Dictionary<int, int> mBoardIdToBoardGrpId = new Dictionary<int, int>();
        private Dictionary<int, fat.rawdata.MergeGrid> mAllMergeGridConfigs = new Dictionary<int, fat.rawdata.MergeGrid>();
        private MergeGlobal mGlobalData = new MergeGlobal();

        #region handler => on bonus
        private List<Merge.IMergeBonusHandler> mGlobalBonusHandler = new List<IMergeBonusHandler>();
        public void RegisterGlobalMergeBonusHandler(Merge.IMergeBonusHandler handler) { if (mGlobalBonusHandler.AddIfAbsent(handler)) { handler?.OnRegister(); } }
        public void UnregisterGlobalMergeBonusHandler(Merge.IMergeBonusHandler handler) { if (mGlobalBonusHandler.Remove(handler)) { handler?.OnUnRegister(); } }
        public void FillGlobalMergeBonusHandler(List<Merge.IMergeBonusHandler> container) { container.AddRange(mGlobalBonusHandler); }
        #endregion

        #region handler => on spawn
        private List<Merge.ISpawnBonusHandler> mGlobalSpawnBonusHandler = new List<ISpawnBonusHandler>();
        public void RegisterGlobalSpawnBonusHandler(Merge.ISpawnBonusHandler handler) { if (mGlobalSpawnBonusHandler.AddIfAbsent(handler)) handler?.OnRegister(); }
        public void UnregisterGlobalSpawnBonusHandler(Merge.ISpawnBonusHandler handler) { if (mGlobalSpawnBonusHandler.Remove(handler)) handler?.OnUnRegister(); }
        public void FillGlobalSpawnBonusHandler(List<Merge.ISpawnBonusHandler> container) { container.AddRange(mGlobalSpawnBonusHandler); }
        #endregion

        #region handler => on dispose
        private List<Merge.IDisposeBonusHandler> mGlobalDisposeBonusHandler = new List<IDisposeBonusHandler>();
        public void RegisterGlobalDisposeBonusHandler(Merge.IDisposeBonusHandler handler) { if (mGlobalDisposeBonusHandler.AddIfAbsent(handler)) handler?.OnRegister(); }
        public void UnregisterGlobalDisposeBonusHandler(Merge.IDisposeBonusHandler handler) { if (mGlobalDisposeBonusHandler.Remove(handler)) handler?.OnUnRegister(); }
        public void FillGlobalDisposeBonusHandler(List<Merge.IDisposeBonusHandler> container) { container.AddRange(mGlobalDisposeBonusHandler); }
        #endregion

        #region  IUserDataHolder
        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            mGlobalData = archive.ClientData.PlayerGameData.MergeGlobal;
            mGlobalData ??= new MergeGlobal();
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            archive.ClientData.PlayerGameData.MergeGlobal = mGlobalData;
        }
        #endregion

        #region 注册目前可用的棋盘世界，以及设置全局唯一的当前正在活跃的棋盘

        public void RegisterMergeWorldEntry(MergeWorldEntry entry)
        {
            UnregisterMergeWorldEntry(entry.world);
            mAllMergeWorld.Add(entry);
            DebugEx.FormatInfo("MergeBoardMan::RegisterMergeWorldEntry ----> register {0}", entry.nameKey);
        }

        public void UnregisterMergeWorldEntry(MergeWorld world)
        {
            for (int i = 0; i < mAllMergeWorld.Count; i++)
            {
                if (mAllMergeWorld[i].world == world)
                {
                    DebugEx.FormatInfo("MergeBoardMan::RegisterMergeWorldEntry ----> unregister {0}", mAllMergeWorld[i].nameKey);
                    mAllMergeWorld.RemoveAt(i);
                    break;
                }
            }
        }

        public void SetCurrentActiveWorld(MergeWorld world)
        {
            mCurrentActiveWorld = world;
            if (world == null)
            {
                // 及时清除
                SetCurrentInteractingItem(null);
            }
        }

        public void SetCurrentActiveTracer(MergeWorldTracer tracer)
        {
            mCurrentActiveTracer = tracer;
        }

        public void SetCurrentInteractingItem(Item item)
        {
            mCurrentInteractingItem = item;
            if (item != null)
            {
                mRecentInteractingItem = item;
            }
        }

        #endregion

        #region config相关

        private void _OnConfigLoaded()
        {
            mAllBoardConfigs = Game.Manager.configMan.GetMergeBoardConfigs();
            mAllMergeBoardGrp = Game.Manager.configMan.GetMergeBoardGrpConfigs();
            mBoardIdToBoardGrpId.Clear();
            foreach (var c in mAllMergeBoardGrp.Values)
            {
                foreach (var boardId in c.BoardIds)
                {
                    mBoardIdToBoardGrpId[boardId] = c.Id;
                }
            }
            mAllMergeGridConfigs = Game.Manager.configMan.GetMergeGridConfigs().ToDictionaryEx((e) => e.Id);
        }

        public fat.rawdata.MergeGrid GetMergeGridConfig(int tid)
        {
            return mAllMergeGridConfigs.GetDefault(tid, null);
        }

        public MergeBoard GetBoardConfig(int id)
        {
            return mAllBoardConfigs.GetDefault(id, null);
        }

        #endregion

        #region 外部传入棋盘id或棋盘组id 获取对应的MergeWorld

        public MergeWorld GetMergeWorldForRewardByBoardId(int boardId, bool strict = false)      //strict 表示如果找不到boardId对应的World，返回null
        {
            var world = _GetMergeWorldByBoardId(boardId, true); //先把boardId当做正常的棋盘id来查找
            if (world == null)
            {
                var group = GetBoardGroupByBoardId(boardId);  //如果找不到对应world 则把boardId看做棋盘组id再次查找
                if (group != null)
                {
                    foreach (var otherBoardId in group.BoardIds)
                    {
                        if (otherBoardId != boardId)
                        {
                            world = _GetMergeWorldByBoardId(otherBoardId, true);
                            if (world != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            if (world == null && !strict)
            {
                if (mCurrentActiveWorld != null && mCurrentActiveWorld.isGiftboxUsable)
                {
                    world = mCurrentActiveWorld;
                }
                else
                {
                    world = Game.Manager.mainMergeMan.world;
                }
            }
            return world;
        }

        public MergeBoardGrp GetBoardGroupByBoardId(int boardId)
        {
            if (mBoardIdToBoardGrpId.TryGetValue(boardId, out var groupId))
            {
                return mAllMergeBoardGrp.GetDefault(groupId, null);
            }
            else
            {
                return null;
            }
        }

        private bool _FilterWorld(MergeWorld world, bool needGiftbox)
        {
            return !needGiftbox || world.isGiftboxUsable;
        }

        private MergeWorld _GetMergeWorldByBoardId(int boardId, bool needGiftbox)
        {
            if (boardId > 0)
            {
                if (mCurrentActiveWorld != null && boardId == mCurrentActiveWorld.activeBoard.boardId && _FilterWorld(mCurrentActiveWorld, needGiftbox))
                {
                    return mCurrentActiveWorld;
                }
                else if (boardId == Game.Manager.mainMergeMan.world.activeBoard.boardId && _FilterWorld(Game.Manager.mainMergeMan.world, needGiftbox))
                {
                    return Game.Manager.mainMergeMan.world;
                }
                else
                {
                    foreach (var entry in mAllMergeWorld)
                    {
                        if (entry.world.activeBoard.boardId == boardId && _FilterWorld(entry.world, needGiftbox))
                        {
                            return entry.world;
                        }
                    }
                    return null;
                }
            }
            if (mCurrentActiveWorld != null && _FilterWorld(mCurrentActiveWorld, needGiftbox))
            {
                return mCurrentActiveWorld;
            }
            else if (_FilterWorld(Game.Manager.mainMergeMan.world, needGiftbox))
            {
                return Game.Manager.mainMergeMan.world;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region 棋盘初始化相关

        public bool InitializeBoard(MergeWorld world, int boardId, bool fillInitialItems = true)
        {
            if (!mAllBoardConfigs.TryGetValue(boardId, out var boardConfig))
            {
                DebugEx.FormatWarning("MergeBoardMan::InitializeBoard ----> fail because board {0} not exists!", boardId);
                return false;
            }
            var board = world.activeBoard;
            board.Reset(boardId, boardConfig.ColCount, boardConfig.RowCount);
            using (ObjectPool<List<MergeGridArea>>.GlobalPool.AllocStub(out var areas))
            {
                FillMergeAreaForBoard(boardId, areas);
                foreach (var a in areas)
                {
                    board.AddArea(a);
                }
            }
            //初始化当前棋盘可能存在的云层区域 这里只初始化 实际坐标的刷新在各自业务逻辑处 根据需要自行判断是否执行
            board.InitAllCloud();
            //是否是第一次初始化棋盘棋子
            if (!fillInitialItems)
            {
                return true;
            }
            //默认走按列优先的方式处理棋盘初始的棋子配置
            _InitBoardItemColumnFirst(board, boardConfig.Col, boardConfig.ColCount, boardConfig.RowCount);
            return true;
        }
        
        //活动类棋盘在初始化棋盘数据时 如果需要用到活动实例 可以调用此接口 （如可以上下滚动的棋盘）
        public bool InitializeBoard(ActivityLike activityLike, MergeWorld world, int boardId, bool fillInitialItems = true)
        {
            if (!mAllBoardConfigs.TryGetValue(boardId, out var boardConfig))
            {
                DebugEx.FormatWarning("MergeBoardMan::InitializeBoard ----> fail because board {0} not exists!", boardId);
                return false;
            }
            var board = world.activeBoard;
            board.Reset(boardId, boardConfig.ColCount, boardConfig.RowCount);
            using (ObjectPool<List<MergeGridArea>>.GlobalPool.AllocStub(out var areas))
            {
                FillMergeAreaForBoard(boardId, areas);
                foreach (var a in areas)
                {
                    board.AddArea(a);
                }
            }
            //初始化当前棋盘可能存在的云层区域 这里只初始化 实际坐标的刷新在各自业务逻辑处 根据需要自行判断是否执行
            board.InitAllCloud();
            //是否是第一次初始化棋盘棋子
            if (!fillInitialItems)
            {
                return true;
            }
            //根据feature来决定棋盘初始化方式
            var feature = boardConfig.Feature;
            if (feature == FeatureEntry.FeatureMine)
            {
                using (ObjectPool<List<string>>.GlobalPool.AllocStub(out var rowItems))
                {
                    if (Game.Manager.mineBoardMan.FillBoardRowConfStr(boardConfig.DetailParam, rowItems, 0, boardConfig.RowCount))
                    {
                        _InitBoardItemRowFirst(board, rowItems, boardConfig.RowCount, boardConfig.ColCount);
                    }
                }
            }
            else if (feature == FeatureEntry.FeatureFarmBoard)
            {
                if (activityLike is IBoardActivityRowConf IRowConf && activityLike.Type == EventType.FarmBoard)
                {
                    using (ObjectPool<List<string>>.GlobalPool.AllocStub(out var rowItems))
                    {
                        var rowCount = boardConfig.RowCount;
                        if (BoardActivityUtility.FillBoardRowConfStr(IRowConf, boardConfig.DetailParam, rowItems, 0, rowCount))
                        {
                            _InitBoardItemRowFirstBottom(board, rowItems, rowCount, boardConfig.ColCount, rowCount);
                        }
                    }
                }
            }
            else if (feature == FeatureEntry.FeatureWishBoard)
            {
                if (activityLike is IBoardActivityRowConf IRowConf && activityLike.Type == EventType.WishBoard)
                {
                    using (ObjectPool<List<string>>.GlobalPool.AllocStub(out var rowItems))
                    {
                        var rowCount = boardConfig.RowCount;
                        if (BoardActivityUtility.FillBoardRowConfStr(IRowConf, boardConfig.DetailParam, rowItems, 0, rowCount))
                        {
                            _InitBoardItemRowFirstBottom(board, rowItems, rowCount, boardConfig.ColCount, rowCount);
                        }
                    }
                }
            }
            else if (feature == FeatureEntry.FeatureMineCart)
            {
                if (activityLike is IBoardActivityRowConf IRowConf && activityLike.Type == EventType.MineCart)
                {
                    using (ObjectPool<List<string>>.GlobalPool.AllocStub(out var rowItems))
                    {
                        var rowCount = boardConfig.RowCount;
                        if (BoardActivityUtility.FillBoardRowConfStr(IRowConf, boardConfig.DetailParam, rowItems, 0, rowCount))
                        {
                            _InitBoardItemRowFirst(board, rowItems, rowCount, boardConfig.ColCount);
                        }
                    }
                }
            }
            else
            {
                _InitBoardItemColumnFirst(board, boardConfig.Col, boardConfig.ColCount, boardConfig.RowCount);
            }
            return true;
        }

        //收集当前棋盘前N行棋子的所有棋子到奖励箱 itemList表示被收集的棋子
        //默认从上往下数 即isTopToBottom为true， 传false时为从下往上数
        public void CollectBoardItemByRow(Board board, int rowCount, List<Item> itemList, bool isTopToBottom = true, bool needItemEvent = false)
        {
            if (board == null || rowCount <= 0 || itemList == null)
            {
                return;
            }
            var boardSize = board.size;
            var cols = boardSize.x;
            var rows = boardSize.y;
            //校验行数列数合理性
            if (cols <= 0 || rows <= 0 || rowCount > rows)
            {
                return;
            }
            for (int i = 0; i < rowCount; i++)
            {
                // 根据方向，计算要处理的行
                int row = isTopToBottom ? i : (rows - 1 - i);
                for (int col = 0; col < cols; col++)
                {
                    var item = board.GetItemByCoord(col, row);
                    if (item != null && board.MoveItemToRewardBox(item, needItemEvent))
                    {
                        itemList.Add(item);
                    }
                }
            }
        }

        //将一定行数范围内的所有棋子，按顺序向上平移到棋盘顶部 
        //minRow范围[1, 棋盘最大行数]，maxRow范围[1, 棋盘最大行数] -1代表使用棋盘最大行数
        //默认移动的目标格子上没有棋子，且只涉及棋子移动，不涉及棋盘区域的移动
        //若目标格子上有棋子，则跳过本次移动
        //未做处理的极限情况：棋子被移到奖励箱后，因为有空位导致棋子自动产出，此时即将有棋子平移过来
        public void MoveUpBoardItem(Board board, int minRow, int maxRow = -1)
        {
            if (board == null) return;
            var boardSize = board.size;
            var cols = boardSize.x;
            var rows = boardSize.y;
            maxRow = maxRow == -1 ? rows : maxRow;
            //校验行数列数合理性
            if (cols <= 0 || rows <= 0 || minRow < 0 || maxRow < 0 || maxRow > rows || minRow > maxRow)
                return;
            var start = minRow - 1;
            var end = maxRow - 1;
            for (int row = start; row <= end; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    board.TryMoveItemToCoord(col, row, col, row - start);
                }
            }
        }

        //将从第0行开始到倒数第count行为止范围内的所有棋子，整体向下平移到棋盘底部
        //count范围[1, 棋盘最大行数]
        public void MoveDownBoardItem(Board board, int count)
        {
            if (board == null) return;
            var boardSize = board.size;
            var cols = boardSize.x;
            var rows = boardSize.y;
            //校验行数列数合理性
            if (cols <= 0 || rows <= 0 || count <= 0 || count > rows)
                return;
            var start = rows - count;
            var end = 0;
            for (int row = start; row >= end; row--)
            {
                for (int col = 0; col < cols; col++)
                {
                    board.TryMoveItemToCoord(col, row, col, row + count - 1);
                }
            }
        }

        //根据传入配置，在从第startRow行开始的所有格子上，向下创建rowItems.Count行新棋子
        //startRow范围[1, 棋盘最大行数]
        public void CreateNewBoardItemByRow(Board board, IList<string> rowItems, int startRow)
        {
            if (board == null) return;
            var boardSize = board.size;
            var cols = boardSize.x;
            var rows = boardSize.y;
            var totalRow = rowItems.Count;
            var maxRow = startRow + totalRow;
            //校验行数列数合理性
            if (cols <= 0 || rows <= 0 || startRow < 0 || maxRow < 0 || maxRow > rows || startRow > maxRow)
                return;
            _InitBoardItemRowFirst(board, rowItems, maxRow, cols, startRow);
        }

        //根据传入配置，在从第startRow行开始的所有格子上，向上创建rowItems.Count行新棋子
        public void CreateNewBoardItemFromRowToTop(Board board, IList<string> rowItems, int startRow)
        {
            if (board == null) return;
            var boardSize = board.size;
            var cols = boardSize.x;
            var rows = boardSize.y;
            var totalRow = rowItems.Count;
            //校验行数列数合理性
            if (cols <= 0 || rows <= 0 || startRow < 0 || totalRow < 0 || totalRow > rows || startRow > totalRow)
                return;
            _InitBoardItemRowFirstBottom(board, rowItems, totalRow, cols, startRow);
        }

        //return: the count
        public int FillMergeAreaForBoard(int boardid, List<MergeGridArea> container = null)
        {
            int ret = 0;
            foreach (var c in Game.Manager.configMan.GetMergeGridAreaConfigs())
            {
                if (c.BoardId == boardid)
                {
                    ret++;
                    container?.Add(c);
                }
            }
            return ret;
        }

        //按列优先的方式处理棋盘初始的棋子配置
        private void _InitBoardItemColumnFirst(Board board, IList<string> colItems, int colCount, int rowCount, int startCol = 0)
        {
            for (int col = startCol; col < colCount; col++)
            {
                var colItem = colItems.GetElementEx(col, ArrayExt.OverflowBehaviour.Default);
                var rowItemConfStrList = colItem?.Split(',');
                if (rowItemConfStrList == null)
                {
                    continue;
                }
                for (int row = 0; row < rowItemConfStrList.Length && row < rowCount; row++)
                {
                    _SpawnItemByConf(board, col, row, rowItemConfStrList[row]);
                }
            }
        }

        //按行优先的方式处理棋盘初始的棋子配置 默认左上角为坐标原点
        private void _InitBoardItemRowFirst(Board board, IList<string> rowItems, int rowCount, int colCount, int startRow = 0)
        {
            for (int row = startRow; row < rowCount; row++)
            {
                var rowItem = rowItems.GetElementEx(row - startRow, ArrayExt.OverflowBehaviour.Default);
                var colItemConfStrList = rowItem?.Split(',');
                if (colItemConfStrList == null)
                {
                    continue;
                }
                for (int col = 0; col < colItemConfStrList.Length && col < colCount; col++)
                {
                    _SpawnItemByConf(board, col, row, colItemConfStrList[col]);
                }
            }
        }

        //按行优先的方式处理棋盘初始的棋子配置 默认左下角为坐标原点
        //因为从下往上创建是和之前的棋盘坐标逻辑相反 这里创建的时候默认把rowItems的第一个当做棋盘上的最下面一行 以此类推
        private void _InitBoardItemRowFirstBottom(Board board, IList<string> rowItems, int rowCount, int colCount, int startRow)
        {
            for (int row = startRow - 1; row >= startRow - rowCount; row--)
            {
                var rowItem = rowItems.GetElementEx(startRow - row - 1, ArrayExt.OverflowBehaviour.Default);
                var colItemConfStrList = rowItem?.Split(',');
                if (colItemConfStrList == null)
                {
                    continue;
                }

                for (int col = 0; col < colItemConfStrList.Length && col < colCount; col++)
                {
                    _SpawnItemByConf(board, col, row, colItemConfStrList[col]);
                }
            }
        }

        private void _SpawnItemByConf(Board board, int col, int row, string itemConf)
        {
            board.SpawnItemByConf(col, row, itemConf);
        }

        #endregion

        #region debug相关
        public void ClaimAllBonus(HashSet<int> targetItemTids)
        {
            using (ObjectPool<List<Item>>.GlobalPool.AllocStub(out var itemsToClaim))
            {
                foreach (var worldEntry in mAllMergeWorld)
                {
                    worldEntry.world.WalkAllItem((item) =>
                    {
                        if (targetItemTids.Contains(item.tid) && item.HasComponent(ItemComponentType.Bonus))
                        {
                            itemsToClaim.Add(item);
                        }
                    });
                    if (itemsToClaim.Count > 0)
                    {
                        worldEntry.world.onCollectBonus += _EnsureClaimRewardCommit;
                        foreach (var item in itemsToClaim)
                        {
                            worldEntry.world.UseBonusItem(item);
                        }
                        worldEntry.world.onCollectBonus -= _EnsureClaimRewardCommit;
                        itemsToClaim.Clear();
                    }
                }
            }
        }

        private void _EnsureClaimRewardCommit(Merge.MergeWorld.BonusClaimRewardData rewardData)
        {
            var reward = rewardData.GrabReward();
            if (reward != null)
            {
                Game.Manager.rewardMan.CommitReward(reward);
            }
        }
        #endregion

        #region IGameModule

        void IGameModule.Reset()
        {
            mAllMergeWorld.Clear();
            mGlobalBonusHandler.Clear();
            mGlobalSpawnBonusHandler.Clear();
            mGlobalDisposeBonusHandler.Clear();
        }

        void IGameModule.LoadConfig() { _OnConfigLoaded(); }

        void IGameModule.Startup() { }

        #endregion
    }
}