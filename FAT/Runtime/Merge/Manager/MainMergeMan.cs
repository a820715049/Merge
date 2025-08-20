/**
 * @Author: handong.liu
 * @Date: 2022-02-24 11:58:27
 */

using System.Collections.Generic;
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
                DataTracker.missing_item.Track(missChains, ReasonString.missing_item);
            }
        }

        void IGameModule.Reset() { }
        void IGameModule.LoadConfig() { }
        void IGameModule.Startup() { }
    }
}