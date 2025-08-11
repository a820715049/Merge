/**
 * @Author: handong.liu
 * @Date: 2022-02-24 11:58:27
 */
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

        void IGameModule.Reset() { }
        void IGameModule.LoadConfig() { }
        void IGameModule.Startup() { }
    }
}