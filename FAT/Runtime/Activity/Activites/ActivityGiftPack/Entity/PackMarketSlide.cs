/*
 * @Author: tang.yan
 * @Description: 商店轮播礼包数据类
 * @Date: 2024-08-26 15:08:52
 */

using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using EL;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using EL.Resource;

namespace FAT {
    public class PackMarketSlide : GiftPack {
        //礼包数据结构
        public class MarketSlidePkgData
        {
            public int DetailId;        //礼包详情id MarketSlidePackDetail.id
            public MarketSlidePackDetail DetailConf;    //配置
            public int PackId;          //IAPPack.id    走用户分层 只在活动初始化时设置 后续走存档读取
            public IAPPack Content;     //礼包配置内容
            public int BuyCount;     //当前购买次数
            public int TotalStock;      //限购次数
            public BonusReward Goods;   //礼包奖励内容 在集卡期间礼包内容会有变化
            public string Price => Game.Manager.iap.PriceInfo(Content?.IapId ?? 0);        //礼包价格信息
            public ActivityVisual PackTheme;    //礼包theme

            public override string ToString()
            {
                return $"DetailId = {DetailId}, PackId = {PackId}, BuyCount = {BuyCount}, Price = {Price}\n" +
                       $"DetailConf = {DetailConf}\n Content = {Content}";
            }
        }
        //配置数据
        public MarketSlidePack confD;
        public override bool Valid => confD != null;
        //Theme相关
        public ActivityVisual InCdTheme = new();
        
        //当前活动所有礼包数据
        public List<MarketSlidePkgData> PkgDataList = new List<MarketSlidePkgData>();
        
        //本礼包活动不使用底层字段
        public override int PackId { get; set; }
        public override int ThemeId { get; }
        public override int StockTotal => -1;   //设成-1 避免此字段的影响
        
        public PackMarketSlide(ActivityLite lite_) {
            Lite = lite_;
            confD = GetMarketSlidePack(lite_.Param);
        }

        public override void SetupFresh()
        {
            _InitPkgData();
            _InitPackId();
            RefreshContent();
        }
        
        public override void LoadSetup(ActivityInstance data_) {
            base.LoadSetup(data_);
            var any = data_.AnyState;
            int startIndex = 3;
            _InitPkgData();
            if (any.Count > startIndex)
            {
                var totalCount = PkgDataList.Count * 2;
                var index = 0;
                for (var i = startIndex; i < startIndex + totalCount; i += 2)
                {
                    var pkgData = PkgDataList[index];
                    pkgData.PackId = ReadInt(i, any);
                    pkgData.BuyCount = ReadInt(i + 1, any);
                    index++;
                }
            }
            RefreshContent();
        }
        
        public override void SaveSetup(ActivityInstance data_) {
            base.SaveSetup(data_);
            var any = data_.AnyState;
            int startIndex = 3;
            foreach (var pkgData in PkgDataList)
            {
                any.Add(ToRecord(startIndex, pkgData.PackId));
                startIndex++;
                any.Add(ToRecord(startIndex, pkgData.BuyCount));
                startIndex++;
            }
        }
        
        public override IEnumerable<(string, AssetTag)> ResEnumerate() {
            if (!Valid) yield break;
            foreach(var v in InCdTheme.ResEnumerate()) yield return v;
            foreach (var pkgData in PkgDataList)
            {
                if (pkgData.PackTheme == null) continue;
                foreach (var v in pkgData.PackTheme.ResEnumerate()) yield return v;
            }
            foreach(var v in Visual.ResEnumerate()) yield return v;
        }

        public override void RefreshContent()
        {
            _RefreshThemeInfo();
            RefreshPack();
        }

        public override void RefreshPack()
        {
            _RefreshPackInfo();
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().Dispatch(this);
        }

        public override BonusReward MatchPack(int packId_) {
            foreach (var pkgData in PkgDataList)
            {
                if (packId_ == pkgData.PackId)
                    return pkgData.Goods;
            }
            DebugEx.Error($"iap goods {packId_} not found in activity {Info3}");
            return null;
        }

        //检查是否所有礼包都售罄了
        public bool CheckIsAllSoldOut()
        {
            var isSoldOut = true;
            foreach (var pkgData in PkgDataList)
            {
                if (pkgData.BuyCount < pkgData.TotalStock)
                {
                    isSoldOut = false;
                    break;
                }
            }
            return isSoldOut;
        }

        public void FillCanBuyPack(List<MarketSlidePkgData> container)
        {
            foreach (var pkgData in PkgDataList)
            {
                if (pkgData.BuyCount < pkgData.TotalStock)
                {
                    container.Add(pkgData);
                }
            }
        }

        private void _InitPkgData()
        {
            if (!Valid) return;
            PkgDataList.Clear();
            foreach (var detailId in confD.Detailid)
            {
                var detailConf = GetMarketSlidePackDetail(detailId);
                var pkgData = new MarketSlidePkgData()
                {
                    DetailId = detailId,
                    DetailConf = detailConf,
                    BuyCount = 0,
                    TotalStock = detailConf?.Paytimes ?? 0,
                    Goods = new BonusReward(),
                    PackTheme = new ActivityVisual(),
                };
                pkgData.PackTheme.Setup(detailConf?.EventTheme ?? 0);
                PkgDataList.Add(pkgData);
            }
        }

        //活动第一次创建时抉择好PackId
        private void _InitPackId()
        {
            var userGradeMan = Game.Manager.userGradeMan;
            foreach (var pkgData in PkgDataList)
            {
                pkgData.PackId = userGradeMan.GetTargetConfigDataId(pkgData.DetailConf?.PackGrpId ?? 0);
            }
        }

        private void _RefreshPackInfo()
        {
            foreach (var pkgData in PkgDataList)
            {
                var content = GetIAPPack(pkgData.PackId);
                pkgData.Content = content;
                pkgData.Goods.Refresh(content);
            }
        }
        
        private void _RefreshThemeInfo()
        {
            if (!Valid)
                return;
            InCdTheme.Setup(confD.EventRefreshTheme);
        }

        public void TryPurchase(MarketSlidePkgData pkgData)
        {
            if (pkgData == null)
                return;
            if (pkgData.BuyCount >= pkgData.TotalStock)
                return;
            MessageCenter.Get<MSG.GAME_MARKET_SLIDE_TRY_BUY>().Dispatch();
            Game.Manager.activity.giftpack.Purchase(this, pkgData.Content, (rewards) =>
            {
                //buyCount会在底层调用下面的PurchaseSuccess 自己+1
                _PurchaseFinish(rewards, pkgData);
            });
        }
        
        public override void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_)
        {
            base.PurchaseSuccess(packId_, rewards_, late_);
            foreach (var pkgData in PkgDataList)
            {
                if (packId_ == pkgData.PackId)
                {
                    pkgData.BuyCount++;
                }
            }
        }
        
        private void _PurchaseFinish(IList<RewardCommitData> rewards, MarketSlidePkgData pkgData)
        {
            var detailId = pkgData?.DetailId ?? -1;
            //发奖励 刷界面
            MessageCenter.Get<MSG.GAME_MARKET_SLIDE_PGK_REC_SUCC>().Dispatch(detailId, rewards);
            //打点
            DataTracker.marketslidepack_reward.Track(this, detailId);
        }
    }
}