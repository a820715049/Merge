using System;
using UnityEngine;
using EL;

namespace FAT {
    public class UIGiftPack : PackUI {
        public Transform label;
        private Action WhenInit;
        private GiftPack pack;
        internal override ActivityLike Pack => pack;
        private readonly UIIAPLabel iapLabel = new();

        internal override void OnValidate() {
            if (Application.isPlaying) return;
            base.OnValidate();
            transform.Access("Content", out Transform root);
            label = root.Access<Transform>("label");
        }

        protected override void OnParse(params object[] items) {
            pack = (GiftPack)items[0];
        }

        protected override void OnPreOpen() {
            RefreshTheme();
            RefreshPack();
            RefreshPrice();
            RefreshCD();
            WhenInit ??= RefreshPrice;
            WhenEnd ??= RefreshEnd;
            WhenTick ??= RefreshCD;
            WhenRefresh ??= p_ => { if (p_ == pack) RefreshPack(); };
            WhenPurchase ??= r_ => PurchaseComplete(r_);
            MessageCenter.Get<MSG.IAP_INIT>().AddListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().AddListener(WhenRefresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnPreClose()
        {
            iapLabel.Clear();
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().RemoveListener(WhenRefresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        public void RefreshPack() {
            iapLabel.Setup(label, pack.LabelId, pack.PackId);
            var r = pack.Goods.reward;
            layout.Refresh(r);
            stock.text = I18N.FormatText("#SysComDesc115", $"{pack.Stock}/{pack.StockTotal}");
        }

        public void RefreshPrice() {
            var valid = Game.Manager.iap.Initialized;
            confirm.State(valid, pack.Price);
        }

        internal override void ConfirmClick() {
            Game.Manager.activity.giftpack.Purchase(pack, WhenPurchase);
        }
    }
}