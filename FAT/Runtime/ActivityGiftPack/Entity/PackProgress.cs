/*
 * @Author: pengjian.zhang
 * @Description: 进阶礼包
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/svi0dnfcyt60i5mm
 * @Date: 2024-07-24 10:55:21
 */

using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT
{
    using static RecordStateHelper;

    public class PackProgress : GiftPack
    {
        public ProgressPack confD;

        public override int PackId
        {
            get => GetPackId();
            set { }
        }

        public int PackId1 { get; set; }
        public int PackId2 { get; set; }
        public int PackId3 { get; set; }
        public IAPPack Content1 { get; private set; }
        public IAPPack Content2 { get; private set; }
        public IAPPack Content3 { get; private set; }
        public BonusReward Goods1 { get; } = new();
        public BonusReward Goods2 { get; } = new();
        public BonusReward Goods3 { get; } = new();
        public string Price1 => Game.Manager.iap.PriceInfo(Content1.IapId);
        public string Price2 => Game.Manager.iap.PriceInfo(Content2.IapId);
        public string Price3 => Game.Manager.iap.PriceInfo(Content3.IapId);
        public override int ThemeId => confD.EventTheme;
        public override int StockTotal => 3;
        public override UIResAlt Res { get; } = new(UIConfig.UIPackProgress);

        public PackProgress() { }


        public PackProgress(ActivityLite lite_)
        {
            Lite = lite_;
            confD = GetProgressPack(lite_.Param);
            RefreshTheme();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(1, BuyCount));
            any.Add(ToRecord(2, PackId1));
            any.Add(ToRecord(3, PackId2));
            any.Add(ToRecord(4, PackId3));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            BuyCount = ReadInt(1, any);
            PackId1 = ReadInt(2, any);
            PackId2 = ReadInt(3, any);
            PackId3 = ReadInt(4, any);
            RefreshContent();
        }

        public override void SetupFresh()
        {
            PackId1 = GetPackId(confD.PackOneGrpId);
            PackId2 = GetPackId(confD.PackTwoGrpId);
            PackId3 = GetPackId(confD.PackThreeGrpId);
            RefreshContent();
        }

        private int GetPackId(int confDPackOneGrpId)
        {
            return confDPackOneGrpId > 0 ? Game.Manager.userGradeMan.GetTargetConfigDataId(confDPackOneGrpId) : 0;
        }

        public override void RefreshContent()
        {
            Content1 = GetIAPPack(PackId1);
            Content2 = GetIAPPack(PackId2);
            Content3 = GetIAPPack(PackId3);
            Content = GetIAPPack(PackId);
            RefreshPack();
        }

        public override void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_)
        {
            //重复购买且补单时 buyCount不增加
            if (late_ && (packId_ == PackId1 && BuyCount == 2 || packId_ == PackId2 && BuyCount == 3))
            {
                BuyCount--;
            }
            else
            {
                base.PurchaseSuccess(packId_, rewards_, late_);
            }
        }

        public override void RefreshPack()
        {
            Goods1.Refresh(Content1);
            Goods2.Refresh(Content2);
            Goods3.Refresh(Content3);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().Dispatch(this);
        }

        public void ReportPurchase()
        {
            DataTracker.progresspack_reward.Track(this);
        }

        private int GetPackId()
        {
            return BuyCount switch
            {
                0 => PackId1,
                1 => PackId2,
                2 => PackId3,
                _ => 0
            };
        }
        
        public override BonusReward MatchPack(int packId_) {
            if (packId_ == PackId1) return Goods1;
            if (packId_ == PackId2) return Goods2;
            if (packId_ == PackId3) return Goods3;
            DebugEx.Error($"iap goods {packId_} not found in activity {Info3}");
            return null;
        }
    }
}