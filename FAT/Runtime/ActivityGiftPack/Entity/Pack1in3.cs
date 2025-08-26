using System;
using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT {
    using static RecordStateHelper;

    public class Pack1in3 : GiftPack {
        public ThreeForOnePack confD;
        public override bool Valid => confD != null;
        public override int PackId { get; set; }
        public int PackId1 { get; set; }
        public int PackId2 { get; set; }
        public int PackId3 { get; set; }
        public int PackId4 => PackId;
        public IAPPack Content1 { get; private set;}
        public IAPPack Content2 { get; private set;}
        public IAPPack Content3 { get; private set;}
        public IAPPack Content4 => Content;
        public BonusReward Goods1 { get; } = new();
        public BonusReward Goods2 { get; } = new();
        public BonusReward Goods3 { get; } = new();
        public string Price1 => Game.Manager.iap.PriceInfo(Content1.IapId);
        public string Price2 => Game.Manager.iap.PriceInfo(Content2.IapId);
        public string Price3 => Game.Manager.iap.PriceInfo(Content3.IapId);
        public override int ThemeId => confD.EventTheme;
        public override int StockTotal => 1;
        public bool Sale => confD.Sale;

        public Pack1in3() { }


        public Pack1in3(ActivityLite lite_) {
            Lite = lite_;
            confD = GetThreeForOnePack(lite_.Param);
            RefreshTheme();
        }

        public override void SaveSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            any.Add(ToRecord(1, BuyCount));
            any.Add(ToRecord(2, PackId1));
            any.Add(ToRecord(3, PackId2));
            any.Add(ToRecord(4, PackId3));
            any.Add(ToRecord(5, PackId));
        }

        public override void LoadSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            BuyCount = ReadInt(1, any);
            PackId1 = ReadInt(2, any);
            PackId2 = ReadInt(3, any);
            PackId3 = ReadInt(4, any);
            PackId = ReadInt(5, any);
            RefreshContent();
        }

        public override void SetupFresh() {
            var g = Game.Manager.userGradeMan;
            int I(int id_) => id_ > 0 ? g.GetTargetConfigDataId(id_) : 0;
            PackId1 = I(confD.PackOneGrpId);
            PackId2 = I(confD.PackTwoGrpId);
            PackId3 = I(confD.PackThreeGrpId);
            PackId = I(confD.ALLGrpId);
            RefreshContent();
        }

        public override void RefreshContent() {
            Content1 = GetIAPPack(PackId1);
            Content2 = GetIAPPack(PackId2);
            Content3 = GetIAPPack(PackId3);
            Content = GetIAPPack(PackId);
            RefreshPack();
        }

        public override void RefreshPack() {
            Goods1.Refresh(Content1);
            Goods2.Refresh(Content2);
            Goods3.Refresh(Content3);
            Goods.Refresh(Content);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().Dispatch(this);
        }

        public void ReportPurchase() {
            DataTracker.threeforone_reward.Track(this);
        }

        public override BonusReward MatchPack(int packId_) {
            if (packId_ == PackId1) return Goods1;
            if (packId_ == PackId2) return Goods2;
            if (packId_ == PackId3) return Goods3;
            if (packId_ == PackId4) return Goods;
            DebugEx.Error($"iap goods {packId_} not found in activity {Info3}");
            return null;
        }
    }
}