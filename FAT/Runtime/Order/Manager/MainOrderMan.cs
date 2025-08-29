/*
 * @Author: qun.chao
 * @Date: 2023-10-24 16:16:32
 */
using System.Collections.Generic;
using fat.gamekitdata;

namespace FAT
{
    public class MainOrderMan : IGameModule, IUpdate, IUserDataHolder
    {
        public long totalFinished => orderGroupProxy.totalFinished;
        public IOrderHelper curOrderHelper => orderHelper;

        private IOrderHelper orderHelper = new MainOrderHelper();
        private OrderGroupProxy orderGroupProxy = new();

        #region implement

        void IGameModule.LoadConfig()
        { }

        void IGameModule.Reset()
        {
            orderGroupProxy.Reset(orderHelper);
        }

        void IGameModule.Startup()
        { }

        void IUpdate.Update(float dt)
        {
            // 主线 / 仅在主棋盘激活期间更新订单
            if (Game.Manager.mainMergeMan.world != Game.Manager.mergeBoardMan.activeWorld)
                return;
            orderGroupProxy.Update(dt);
        }

        void IUserDataHolder.SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData.MainOrder;
            data ??= new OrderGroup();
            orderGroupProxy.SetData(data, Game.Manager.mainMergeMan.worldTracer);
        }

        void IUserDataHolder.FillData(LocalSaveData archive)
        {
            var data = new OrderGroup();
            archive.ClientData.PlayerGameData.MainOrder = data;
            orderGroupProxy.FillData(data);
        }

        #endregion

        public IOrderProvider GetProvider(OrderProviderType type)
        {
            return orderGroupProxy.GetProvider(type);
        }

        public int GetActiveOrderNum()
        {
            return orderGroupProxy.GetActiveOrderNum();
        }

        public bool TryFinishOrder(IOrderData order, ICollection<RewardCommitData> rewards)
        {
            if (orderGroupProxy.TryFinishOrder(order, rewards))
            {
                if ((OrderProviderType)order.ProviderType == OrderProviderType.Common)
                {
                    AdjustTracker.TrackCommonOrderEvent(order.Id);

                    Game.Manager.featureUnlockMan.OnMainOrderFinished();
                    GuideUtility.OnMainOrderFinished();
                }
                
                // 检查生成器丢失 打点
                Game.Manager.mainMergeMan.CheckMissingItem();
                
                return true;
            }
            return false;
        }

        public void OnMergeLevelChange()
        {
            SetDirty();
        }

        public void SetDirty()
        {
            orderGroupProxy.SetDirty();
        }

        public bool IsOrderCompleted(int id)
        {
            return orderGroupProxy.IsOrderCompleted(id);
        }

        public int FillActiveOrders(List<IOrderData> container, int mask)
        {
            return orderGroupProxy.FillActiveOrders(container, mask);
        }

        public void ValidateOrderDisplayCache()
        {
            orderGroupProxy.ValidateOrderDisplayCache();
        }

        public Dictionary<int, int> GetOrderRequireItemStateCache()
        {
            return orderGroupProxy.GetOrderRequireItemStateCache();
        }

        public IOrderData GetActiveCommonOrderById(int orderId)
        {
            return orderGroupProxy.GetActiveCommonOrderById(orderId);
        }
    }
}