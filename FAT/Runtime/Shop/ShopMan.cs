/*
 * @Author: tang.yan
 * @Description: 商城管理器
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/rgceg0giq9to7nk5
 * @Date: 2023-11-08 11:11:38
 * @LastEditors: ange.shentu
 * @LastEditTime: 2025-07-08 16:18:00
 */

using UnityEngine;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;
using static DataTracker;
using EventType = fat.rawdata.EventType;
using System;

namespace FAT
{
    public class ShopMan : IGameModule, IUserDataHolder, ISecondUpdate
    {
        //当前所有的商店数据 key为绑定的棋盘id
        public Dictionary<int, ShopBaseData> ShopDataDict = new Dictionary<int, ShopBaseData>();
        //飞奖励起始位置
        private static Vector3 _rewardFromPos = Vector3.zero;
        //飞奖励起始大小
        private static float _rewardSize = 0;
        //钻石不足弹窗
        private PopupOOG popupOOG = new();

        public void Reset()
        {
            ShopDataDict.Clear();
            MessageCenter.Get<MSG.IAP_LATE_DELIVERY>().AddListenerUnique(_ShopGemGoodsLateDelivery);
            MessageCenter.Get<MSG.ACTIVITY_STATE>().AddListenerUnique(CheckActivity);
            MessageCenter.Get<MSG.IAP_REWARD_CHECK>().AddListenerUnique(RefreshGemData);
            MessageCenter.Get<MSG.SCREEN_POPUP_QUERY>().AddListenerUnique(TryPopup);
        }

        public void LoadConfig() { }

        public void Startup()
        {

        }

        public void SetData(LocalSaveData archive)
        {
            //有存档时以存档数据为准创建相关商店
            var shopInfo = archive.ClientData.PlayerGameData.ShopInfo;
            if (shopInfo != null)
            {
                ShopDataDict.Clear();
                foreach (var saveShopData in shopInfo.ShopDataList)
                {
                    int boardId = saveShopData.BoardId;
                    BindShopWithBoard(boardId);
                    ShopDataDict[boardId].SetData(saveShopData);
                }
            }
            else
            {
                //没有存档时默认先创建主棋盘商店  后续其他商店的创建根据具体业务逻辑处理
                BindShopWithBoard(Constant.MainBoardId);    //创建主棋盘对应的商店 默认主棋盘id为1
            }
        }

        public void FillData(LocalSaveData archive)
        {
            var data = new ShopInfo();
            foreach (var shopData in ShopDataDict.Values)
            {
                var saveShopData = new ShopData();
                shopData.FillData(saveShopData);
                data.ShopDataList.Add(saveShopData);
            }
            archive.ClientData.PlayerGameData.ShopInfo = data;
        }

        public void SecondUpdate(float dt)
        {
            foreach (var shopData in ShopDataDict.Values)
            {
                foreach (var tabData in shopData.ShopTabDataMap.Values)
                {
                    //触发了数据刷新
                    if (tabData.OnSecondUpdate())
                    {
                        MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().Dispatch();
                    }
                }
            }
        }

        //传入棋盘id 创建并绑定对应的商店数据
        public void BindShopWithBoard(int boardId)
        {
            if (!ShopDataDict.ContainsKey(boardId))
            {
                var shopData = new ShopBaseData(boardId);
                ShopDataDict.Add(boardId, shopData);
            }
        }

        public bool CheckShopIsUnlock(int boardId = Constant.MainBoardId)
        {
            if (ShopDataDict.TryGetValue(boardId, out var shopData))
            {
                return shopData.IsUnlock;
            }
            return false;
        }

        public bool CheckShopTabIsUnlock(ShopTabType type, int boardId = Constant.MainBoardId)
        {
            if (!ShopDataDict.TryGetValue(boardId, out var shopData))
            {
                return false;
            }
            if (!shopData.ShopTabDataMap.TryGetValue(type, out var tabData))
            {
                return false;
            }
            return tabData.IsUnlock;
        }

        public void TryOpenUIShop(ShopTabType type = ShopTabType.Gem)
        {
            if (CheckShopTabIsUnlock(type))
            {
                //打开商店界面时如果发现物品信息界面正在开启 则关闭
                if (UIManager.Instance.IsOpen(UIConfig.UIItemInfo))
                {
                    UIManager.Instance.CloseWindow(UIConfig.UIItemInfo);
                }
                UIManager.Instance.OpenWindow(UIConfig.UIShop, type);
            }
        }

        public void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (state_ != PopupType.Diamond) return;
            popup_.Queue(popupOOG);
        }

        public void AdjustTrackerShopOpen()
        {
            int openCount;
            if (!Game.Manager.accountMan.TryGetClientStorage("ShopOpenCount", out string count))
            {
                openCount = 1;
            }
            else
            {
                openCount = count.ConvertToInt() + 1;
            }
            Game.Manager.accountMan.SetClientStorage("ShopOpenCount", openCount.ToString());
            if (openCount == 3)
            {
                AdjustTracker.TrackOnceEvent(AdjustEventType.ShowShop3, AdjustEventType.ShowShop3.ToString());
            }
            else if (openCount == 8)
            {
                AdjustTracker.TrackOnceEvent(AdjustEventType.ShowShop8, AdjustEventType.ShowShop8.ToString());
            }
        }

        public ShopBaseData GetShopData(int boardId = Constant.MainBoardId)
        {
            return ShopDataDict.TryGetValue(boardId, out var shopData) ? shopData : null;
        }

        public ShopTabBaseData GetShopTabData(ShopTabType type, int boardId = Constant.MainBoardId)
        {
            var shopData = GetShopData(boardId);
            if (shopData != null)
                return shopData.ShopTabDataMap.TryGetValue(type, out var shopTabData) ? shopTabData : null;
            else
                return null;
        }

        //传入物品id 尝试在商店的订单随机页签部分找到是否有卖对应物品
        //这里还会检查一下该页签是否解锁 如果未解锁 则不返回任何数据
        public ShopChessOrderData TryGetChessOrderDataById(int itemId, int boardId = Constant.MainBoardId)
        {
            var chessTabData = (ShopTabChessData)Game.Manager.shopMan.GetShopTabData(ShopTabType.Chess, boardId);
            return chessTabData.IsUnlock ? chessTabData.GetChessOrderData(itemId) : null;
        }

        #region 钻石商店相关

        //尝试购买内购钻石商品
        public void TryBuyShopGemGoods(ShopGemData data, Vector3 flyFromPos)
        {
            _rewardFromPos = flyFromPos;
            Game.Manager.iap.Purchase(data.PackId, IAPFrom.ShopMan, _PurchaseComplete);
        }

        //购买成功回调
        private void _PurchaseComplete(bool isSuccess, IAPPack iapPack)
        {
            _ProcessShopGemIAP(isSuccess, iapPack);
        }

        //补单逻辑
        private void _ShopGemGoodsLateDelivery(IAPLateDelivery delivery)
        {
            if (delivery.from != IAPFrom.ShopMan) return;
            if (_ProcessShopGemIAP(true, delivery.pack))
            {
                delivery.ClaimReason = nameof(_ShopGemGoodsLateDelivery);
            }
        }

        //处理购买结果逻辑
        private bool _ProcessShopGemIAP(bool isSuccess, IAPPack iapPack)
        {
            DebugEx.FormatInfo("ShopMan::_ProcessShopGemIAP, receive purchase result = {0}", isSuccess);
            if (!isSuccess || iapPack == null) return false;
            var gemTabData = (ShopTabGemData)Game.Manager.shopMan.GetShopTabData(ShopTabType.Gem);
            if (gemTabData == null)
                return false;
            var shopGemData = gemTabData.GemDataList.FindEx(x => x.PackId == iapPack.Id);
            if (shopGemData == null)
                return false;
            using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
            {
                foreach (var r in shopGemData.Reward.reward)
                {
                    //构造基础奖励
                    rewards.Add(Game.Manager.rewardMan.BeginReward(r.Id, r.Count, ReasonString.purchase, RewardFlags.IsUseIAP));
                    market_buy.Track(1, gemTabData.BindBoardId, shopGemData.GirdId, shopGemData.PackId, r.Id);
                }
                // 发货
                UIFlyUtility.FlyRewardList(rewards, _rewardFromPos);
                //Dispatch
                MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().Dispatch();
                DebugEx.FormatInfo("ShopMan::_ProcessShopGemIAP fly Gem reward finish! iapPackId = {0}", iapPack.Id);
                return true;
            }
        }

        public void CheckActivity(ActivityLike acti_)
        {
            if (acti_.Type == EventType.MarketIapgift)
            {
                RefreshGemData();
            }
        }

        public void RefreshGemData()
        {
            var gemTabData = (ShopTabGemData)Game.Manager.shopMan.GetShopTabData(ShopTabType.Gem);
            if (gemTabData == null) return;
            foreach (var d in gemTabData.GemDataList)
            {
                d.Reward.RefreshShop(d.GirdId, d.ConfPackId);
            }
            MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().Dispatch();
        }

        #endregion

        #region 能量商店相关

        //尝试购买能量商品
        public void TryBuyShopEnergyGoods(ShopEnergyData data, Vector3 flyFromPos, Action whenSuccess = null, float size = 0)
        {
            _rewardFromPos = flyFromPos;
            _rewardSize = size;
            if (data?.CurSellGoodsConfig == null)
                return;
            var price = data.CurSellGoodsConfig.Price.ConvertToRewardConfig();
            var reward = data.CurSellGoodsConfig.Reward.ConvertToRewardConfig();
            if (price == null || reward == null)
                return;
            CoinType type = Game.Manager.coinMan.GetCoinTypeById(price.Id);
            if (Game.Manager.coinMan.CanUseCoin(type, price.Count))
            {
                Game.Manager.coinMan.UseCoin(type, price.Count, ReasonString.market_energy)
                .OnSuccess(() =>
                 {
                     using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
                     {
                         //构造基础奖励
                         rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.market_energy));
                         market_buy.Track(2, data.BelongBoardId, data.GirdId, data.CurSellGoodsConfig.Id, reward.Id);
                         // 发货
                         UIFlyUtility.FlyRewardList(rewards, _rewardFromPos, null, _rewardSize);
                     }
                     //在数据刷新前尝试Adjust打点
                     _AdjustTrackerShopEnergy(data.CurSellGoodsConfig.Id);
                     //数据刷新
                     data.OnBuyGoodsSuccess();
                     //Dispatch
                     MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().Dispatch();
                     whenSuccess?.Invoke();
                 })
                 .Execute();
            }
        }
        private void _AdjustTrackerShopEnergy(int goodsId)
        {
            if (goodsId == 1)   //10钻石能量
            {
                AdjustTracker.TrackOnceEvent(AdjustEventType.EnergyPack10, AdjustEventType.EnergyPack10.ToString());
            }
            else if (goodsId == 4)  //24钻石能量箱
            {
                AdjustTracker.TrackOnceEvent(AdjustEventType.EnergyChest10, AdjustEventType.EnergyChest10.ToString());
            }
        }

        #endregion

        #region 棋子商店相关

        //尝试购买随机权重棋子商品
        public void TryBuyShopChessRandomGoods(ShopChessRandomData data, Vector3 flyFromPos, float size = 0)
        {
            if (data == null || !data.CheckCanBuy())
                return;
            _rewardFromPos = flyFromPos;
            _rewardSize = size;
            ProcessShopRandomChess(data);
        }

        private void ProcessShopRandomChess(ShopChessRandomData data)
        {
            if (data?.CurSellGoodsConfig == null)
                return;
            var price = data.CurSellGoodsConfig.Price.ConvertToRewardConfig();
            var reward = data.CurSellGoodsConfig.Reward.ConvertToRewardConfig();
            if (price == null || reward == null)
                return;
            CoinType type = Game.Manager.coinMan.GetCoinTypeById(price.Id);
            if (Game.Manager.coinMan.CanUseCoin(type, price.Count))
            {
                Game.Manager.coinMan.UseCoin(type, price.Count, ReasonString.market_item)
                .OnSuccess(() =>
                {
                    using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
                    {
                        //构造基础奖励
                        rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.market_item));
                        market_buy.Track(3, data.BelongBoardId, data.GirdId, data.CurSellGoodsConfig.Id, reward.Id);
                        // 发货
                        UIFlyUtility.FlyRewardList(rewards, _rewardFromPos, null, _rewardSize);
                    }
                    //数据刷新
                    data.OnBuyGoodsSuccess();
                    //Dispatch
                    MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().Dispatch();
                    //尝试积分活动加分
                    MessageCenter.Get<MSG.ON_BUY_SHOP_ITEM>().Dispatch(price.Count, data.BelongBoardId);
                })
                .CloseOnShopRefresh()
                .Execute();
            }
        }

        public void TryRefreshShopChessGoods(ShopTabChessData data)
        {
            if (Game.Manager.coinMan.CanUseCoin(CoinType.Gem, Game.Manager.configMan.globalConfig.MarketRefreshNum))
            {
                //默认刷新时使用钻石
                Game.Manager.coinMan.UseCoin(CoinType.Gem, Game.Manager.configMan.globalConfig.MarketRefreshNum, ReasonString.market_boost)
                .OnSuccess(() =>
                {
                    //数据刷新
                    data.RefreshCurSellGoods();
                    //Dispatch
                    MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().Dispatch();
                })
                .CloseOnShopRefresh()
                .Execute();
            }
        }

        //尝试购买订单棋子商品
        public void TryBuyShopChessOrderGoods(ShopChessOrderData data, Vector3 flyFromPos, float size = 0)
        {
            if (data == null || !data.CheckCanBuy())
                return;
            _rewardFromPos = flyFromPos;
            _rewardSize = size;
            ProcessShopOrderChess(data);
        }

        private void ProcessShopOrderChess(ShopChessOrderData data)
        {
            if (Game.Manager.coinMan.CanUseCoin(CoinType.Gem, data.CurSellGoodsPrice))
            {
                Game.Manager.coinMan.UseCoin(CoinType.Gem, data.CurSellGoodsPrice, ReasonString.market_boost)
                .OnSuccess(() =>
                {
                    using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
                    {
                        //构造基础奖励
                        rewards.Add(Game.Manager.rewardMan.BeginReward(data.CurSellGoodsId, 1, ReasonString.market_boost));
                        market_buy.Track(4, data.BelongBoardId, data.GirdId, 0, data.CurSellGoodsId);
                        // 发货
                        UIFlyUtility.FlyRewardList(rewards, _rewardFromPos, null, _rewardSize);
                    }
                    //数据刷新
                    data.OnBuyGoodsSuccess();
                    //Dispatch
                    MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().Dispatch();
                    //尝试积分活动加分
                    MessageCenter.Get<MSG.ON_BUY_SHOP_ITEM>().Dispatch(data.CurSellGoodsPrice, data.BelongBoardId);
                })
                .CloseOnShopRefresh()
                .Execute();
            }
        }
        #endregion
    }
}
