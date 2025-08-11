using System;
using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using EL;

namespace FAT {
    public interface IGiftPackLike {
        int Id { get; }
        int From { get; }
        IAPPack Content { get; }
        int BuyCount { get; set;}
        int StockTotal { get; }
        int Stock { get; }
        BonusReward Goods { get; }
        string Price { get;}
        
        //当有多个pack时 可以依照packId_来索引和刷新
        BonusReward MatchPack(int packId_);
        void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_);
    }

    public class GiftPackLike : IGiftPackLike {
        public int Id { get; set; }
        public int From { get; set; }
        public string SubType { get; set; }
        public IAPPack Content { get; private set; }
        public int PackId { get; set; }
        public int BuyCount { get; set;}
        public int StockTotal { get; internal set; }
        public int Stock => StockTotal - BuyCount;
        public BonusReward Goods { get; } = new();
        public string Price => Game.Manager.iap.PriceInfo(Content.IapId);

        public void Setup(int buy_, int total_) {
            BuyCount = buy_;
            StockTotal = total_;
        }

        public void Refresh(int id_, int from_, int packId_) {
            Id = id_;
            From = from_;
            PackId = packId_;
            RefreshContent();
        }

        public void RefreshContent() {
            Content = GetIAPPack(PackId);
            RefreshPack();
        }

        public void RefreshPack() {
            Goods.Refresh(Content);
        }

        public virtual BonusReward MatchPack(int packId_) => Goods;

        public virtual void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_) {}
    }
}