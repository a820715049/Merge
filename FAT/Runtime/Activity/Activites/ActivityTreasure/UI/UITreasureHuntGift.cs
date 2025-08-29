using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using static fat.conf.Data;

namespace FAT {
    public class UITreasureHuntGift : PackUI {
        public UIImageRes bg;
        public UIImageRes bg1;
        public TextProOnACircle title;
        public TextMeshProUGUI desc;
        public MBRewardIcon icon;
        private Action WhenInit;
        private ActivityTreasure acti;
        private GiftPackLike pack;
        internal override ActivityLike Pack => acti;

        internal override void OnValidate() {
            if (Application.isPlaying) return;
            base.OnValidate();
            var root = transform.Find("Content");
            bg = root.FindEx<UIImageRes>("bg");
            bg1 = root.FindEx<UIImageRes>("bg1");
            title = root.FindEx<TextProOnACircle>("title");
            desc = root.FindEx<TextMeshProUGUI>("desc");
            icon = root.FindEx<MBRewardIcon>("entry");
        }

        protected override void OnParse(params object[] items) {
            acti = (ActivityTreasure)items[0];
            pack = acti.pack;
        }

        protected override void OnPreOpen() {
            RefreshTheme();
            RefreshPack();
            RefreshPrice();
            RefreshCD();
            WhenInit ??= RefreshPrice;
            WhenEnd ??= RefreshEnd;
            WhenTick ??= RefreshCD;
            WhenRefresh ??= p_ => { if (p_ == acti) RefreshPack(); };
            WhenPurchase ??= r_ => PurchaseComplete(r_);
            MessageCenter.Get<MSG.IAP_INIT>().AddListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().AddListener(WhenRefresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnPreClose() {
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().RemoveListener(WhenRefresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        public override void RefreshTheme() {
            var visual = acti.VisualGift;
            visual.Refresh(bg, "bgPrefab");
            visual.Refresh(bg1, "titleBg");
            visual.Refresh(title, "mainTitle");
            visual.Refresh(desc, "subTitle");
        }

        public void RefreshPack() {
            var r = pack.Goods.reward[0];
            icon.Refresh(r);
            stock.text = I18N.FormatText("#SysComDesc115", $"{pack.Stock}/{pack.StockTotal}");
        }

        public void RefreshPrice() {
            var valid = Game.Manager.iap.Initialized;
            confirm.State(valid, pack.Price);
        }

        internal override void PurchaseComplete(IList<RewardCommitData> list_) {
            Pack.ResetPopup();
            for (var n = 0; n < list_.Count; ++n) {
                var pos = icon.transform.position;
                UIFlyUtility.FlyReward(list_[n], pos);
            }
            IEnumerator DelayClose() {
                yield return new WaitForSeconds(closeDelay);
                Close();
            }
            MessageCenter.Get<MSG.TREASURE_KEY_UPDATE>().Dispatch(1);
            StartCoroutine(DelayClose());
        }

        internal override void ConfirmClick() {
            Game.Manager.activity.giftpack.Purchase(pack, WhenPurchase);
        }
    }
}