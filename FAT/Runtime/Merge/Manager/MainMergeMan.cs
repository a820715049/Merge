/**
 * @Author: handong.liu
 * @Date: 2022-02-24 11:58:27
 */

using System.Collections.Generic;
using Cysharp.Text;
using FAT.Merge;
using fat.gamekitdata;
using EL;

namespace FAT
{
    //主合成世界管理器 持有主棋盘所在的世界实体 
    public class MainMergeMan : IGameModule, IUserDataHolder
    {
        public MergeWorld world => mWorld;  //世界实体
        public MergeWorldTracer worldTracer => mWorldTracer;    //世界实体追踪器
        private MergeWorld mWorld;
        private MergeWorldTracer mWorldTracer;
        public float mainBoardScale;

        private void _AllocWorld()
        {
            mWorld = new MergeWorld();
            mWorldTracer = new MergeWorldTracer(() =>
            {
                Game.Manager.mainOrderMan.SetDirty();
                MessageCenter.Get<MSG.BINGO_ITEM_COMPLETE_DIRTY>().Dispatch();
            }, null);

            Game.Manager.mergeBoardMan.RegisterMergeWorldEntry(new MergeWorldEntry()
            {
                world = mWorld,
                icon = Constant.kMainBoardIcon,
                type = MergeWorldEntry.EntryType.MainGame,
                nameKey = mWorld.dataTrackName,
            });
            mWorldTracer.Bind(mWorld);
            mWorld.BindTracer(mWorldTracer);
            mWorld.BindOrderHelper(Game.Manager.mainOrderMan.curOrderHelper);
        }

        #region IUserDataHolder
        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            _AllocWorld();

            var data = archive.ClientData.PlayerGameData;
            if (data.Merge == null)
            {
                //init new world
                world.inventory.AddBag((int)BagMan.BagType.Item);
                world.inventory.AddBag((int)BagMan.BagType.Producer);
                Game.Manager.mergeBoardMan.InitializeBoard(mWorld, Constant.MainBoardId);
                mWorld.SetConfigVersion(Game.Manager.configMan.configVersion);
            }
            else
            {
                Game.Manager.mergeBoardMan.InitializeBoard(mWorld, Constant.MainBoardId, false);
                mWorld.Deserialize(data.Merge, null);
            }
            mWorldTracer.Invalidate();
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData;
            if (data.Merge == null)
            {
                data.Merge = new fat.gamekitdata.Merge();
            }
            mWorld.Serialize(data.Merge);
        }
        #endregion

        //传入棋子id，找到并展示他（只关注主棋盘）。 先在棋盘上找，若找不到就去背包里找。
        public bool TryFindAndShowItem(int itemId)
        {
            if (itemId <= 0 || worldTracer == null)
                return false;
            //查找棋子是否有关联配置
            var seriesConf = Game.Manager.configMan.GetMergeItemSeriesConfig(itemId);
            //没有时 只查找展示单一棋子id
            if (seriesConf == null)
            {
                return _ProcessSingleItem(itemId);
            }
            //有配置时 把关联棋子当做一个整体来处理
            else
            {
                using var _ = PoolMapping.PoolMappingAccess.Borrow<HashSet<int>>(out var itemHashSet);
                itemHashSet.Add(itemId);
                foreach (var id in seriesConf.SeriesId)
                {
                    itemHashSet.AddIfAbsent(id);
                }
                return _ProcessItemHashSet(itemHashSet);
            }
        }
        
        // 检查链条是否解锁
        private bool IsChainExistUnLock(int chainId)
        {
            var mergeItemMan = Game.Manager.mergeItemMan;
            var categoryConfig = mergeItemMan.GetCategoryConfig(chainId);
            var progress = categoryConfig.Progress;
            var handBookMan = Game.Manager.handbookMan;

            var unlock = false;
            foreach (var item in progress)
            {
                if (!handBookMan.IsItemLock(item))
                {
                    unlock = true;
                    break;
                }
            }

            return unlock;
        }

        // 检查是否有丢失的棋子链条 item
        public void CheckMissingItem()
        {
            var mergeItemMan = Game.Manager.mergeItemMan;
            if (mergeItemMan == null)
            {
                return;
            }
            
            var handBookMan = Game.Manager.handbookMan;
            if (handBookMan == null)
            {
                return;
            }

            // 需要检查的链条
            var checkChainList = mergeItemMan.GetAliveCheckChain();
            if (checkChainList.Count == 0)
            {
                return;
            }

            if (worldTracer == null)
            {
                return;
            }
            
            // 所有活跃物品
            var archiveItemDic = worldTracer.GetCurrentActiveItemCount();
            List<int> missChains = new();

            foreach (var chainId in checkChainList)
            {
                // 链条未解锁
                if (!IsChainExistUnLock(chainId))
                {
                    continue;
                }
                
                var categoryConfig = mergeItemMan.GetCategoryConfig(chainId);
                if (categoryConfig == null)
                {
                    continue;
                }

                // 需要检查的棋子
                var progress = categoryConfig.Progress;
                if (progress.Count == 0)
                {
                    continue;
                }

                var checkPass = false;
                foreach (var item in progress)
                {
                    if (archiveItemDic.ContainsKey(item) && archiveItemDic[item] > 0)
                    {
                        // 只要存在链条的其中一个 检查通过
                        checkPass = true;
                        break;
                    }
                }
                
                if (!checkPass)
                {
                    missChains.Add(chainId);
                }
            }

            // 打点
            if (missChains.Count > 0)
            {
                var sb = ZString.CreateStringBuilder();
                foreach (var chain in missChains)
                {
                    sb.Append($"{chain},");
                }

                DataTracker.missing_item.Track(sb.ToString(), ReasonString.missing_item);
            }
        }
        
        private bool _ProcessSingleItem(int itemId)
        {
            var boardItemCountDict = worldTracer.GetCurrentActiveBoardItemCount();
            var allItemCountDict = worldTracer.GetCurrentActiveBoardAndInventoryItemCount();
            if (boardItemCountDict == null || allItemCountDict == null)
                return false;
            if (boardItemCountDict.TryGetValue(itemId, out _))
            {
                var mainBoard = worldTracer.world.activeBoard;  //只关注主棋盘
                var curBoardId = Game.Manager.mergeBoardMan.activeWorld?.activeBoard?.boardId ?? 0;
                if (mainBoard == null || mainBoard.boardId != curBoardId)
                    return false;
                var holder = BoardViewManager.Instance.boardView.boardHolder;
                //暂停合成提示
                BoardViewManager.Instance.OnUserActive();
                //对所有找到的item播放点击动画
                mainBoard.WalkAllItem(item => 
                { 
                    if (item.tid == itemId && ItemUtility.CanUseInOrder(item))  //排除掉死亡的棋子以及泡泡棋子
                        holder.TapItem(item.id, true); 
                });
                //关闭可能存在的背包界面
                UIManager.Instance.CloseWindow(UIConfig.UIBag);
                return true;
            }
            else if (allItemCountDict.TryGetValue(itemId, out _)) //此时找到的只能是背包中的棋子
            {
                Game.Manager.bagMan.FindItemAndJumpToUIBag(itemId);
                return true;
            }
            return false;
        }

        private bool _ProcessItemHashSet(HashSet<int> itemHashSet)
        {
            var boardItemCountDict = worldTracer.GetCurrentActiveBoardItemCount();
            var allItemCountDict = worldTracer.GetCurrentActiveBoardAndInventoryItemCount();
            if (boardItemCountDict == null || allItemCountDict == null)
                return false;
            var findInBoard = false;
            var findInBag = false;
            foreach (var itemId in itemHashSet)
            {
                if (boardItemCountDict.TryGetValue(itemId, out _))
                {
                    findInBoard = true; //一旦在棋盘上找到了就break
                    break;
                }
                else if (allItemCountDict.TryGetValue(itemId, out _)) //此时找到的只能是背包中的棋子
                {
                    findInBag = true;   //在背包中找到时还可能会在棋盘上也找到 所以不break
                }
            }
            if (findInBoard)
            {
                var mainBoard = worldTracer.world.activeBoard;  //只关注主棋盘
                var curBoardId = Game.Manager.mergeBoardMan.activeWorld?.activeBoard?.boardId ?? 0;
                if (mainBoard == null || mainBoard.boardId != curBoardId)
                    return false;
                var holder = BoardViewManager.Instance.boardView.boardHolder;
                //暂停合成提示
                BoardViewManager.Instance.OnUserActive();
                //对所有找到的item播放点击动画
                mainBoard.WalkAllItem(item => 
                { 
                    if (itemHashSet.Contains(item.tid) && ItemUtility.CanUseInOrder(item))  //排除掉死亡的棋子以及泡泡棋子
                        holder.TapItem(item.id, true); 
                });
                //关闭可能存在的背包界面
                UIManager.Instance.CloseWindow(UIConfig.UIBag);
            }
            else if (findInBag)
            {
                foreach (var itemId in itemHashSet)
                {
                    //找到了就break
                    if (Game.Manager.bagMan.FindItemAndJumpToUIBag(itemId))
                        break;
                }
            }
            return findInBoard || findInBag;
        }

        void IGameModule.Reset() { }
        void IGameModule.LoadConfig() { }
        void IGameModule.Startup() { }
    }
}