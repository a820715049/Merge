using System;
using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using EL;

namespace FAT {
    public abstract class GiftPack : ActivityLike, IGiftPackLike {
        public IAPPack Content { get; internal set; }
        public override bool Valid => Content != null;
        public abstract int PackId { get; set; }
        public abstract int ThemeId { get; }
        public virtual int LabelId { get; }
        public virtual UIResAlt Res { get; } = new(UIConfig.UIGiftPack);
        public PopupActivity Popup { get; internal set; }
        public int BuyCount { get; set;}
        public abstract int StockTotal { get; }
        public int Stock => StockTotal - BuyCount;
        public virtual bool WillEnd => Stock == 0;
        public BonusReward Goods { get; } = new();
        public string Price => Game.Manager.iap.PriceInfo(Content.IapId);

        public override void SaveSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            any.Add(ToRecord(1, BuyCount));
            any.Add(ToRecord(2, PackId));
        }

        public override void LoadSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            BuyCount = ReadInt(1, any);
            var packId = ReadInt(2, any);
            if (packId > 0) {
                PackId = packId;
                RefreshContent();
            }
        }

        public virtual void RefreshContent() {
            Content = GetIAPPack(PackId);
            if (Content == null) {
                DebugEx.Warning($"pack content invalid activity:{Info3} packId:{PackId}");
            }
            RefreshPack();
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            popup_.TryQueue(Popup, state_);
        }

        public override void ResetPopup() {
            Game.Manager.screenPopup.ResetState(Popup.PopupState);
        }

        public override void Open() => Open(Res);

        public void RefreshTheme(bool popupCheck_ = true) {
            if (Visual.Setup(ThemeId, Res)) {
                Popup = new(this, Visual, Res, check_:popupCheck_);
            }
        }

        public virtual void RefreshPack() {
            Goods.Refresh(Content);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().Dispatch(this);
        }

        public virtual BonusReward MatchPack(int packId_) => Goods;

        public virtual void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_) {
            if (WillEnd) Game.Manager.activity.EndImmediate(this, expire_:false);
        }
    }
}