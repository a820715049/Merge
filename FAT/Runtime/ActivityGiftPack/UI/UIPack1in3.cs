using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using static fat.conf.Data;
using fat.rawdata;
using FAT.MSG;

namespace FAT {
    public class UIPack1in3 : PackUI {
        internal UIImageRes bg1;
        internal TextProOnACircle title;
        internal TextMeshProUGUI desc;
        internal MBRewardLayout layout1;
        internal MBRewardLayout layout2;
        internal MBRewardLayout layout3;
        internal UIStateGroup bg3;
        internal GameObject best3;
        internal GameObject group4;
        internal GameObject best4;
        internal MapButton confirm1;
        internal MapButton confirm2;
        internal MapButton confirm3;
        public float pos3 = -700;
        public float pos4 = -600;
        private Action WhenInit;
        private Pack1in3 pack;
        internal override ActivityLike Pack => pack;
        private MBRewardLayout active;

        protected override void OnCreate() {
            base.OnCreate();
            transform.Access("Content", out Transform root);
            root.Access("bg1", out bg1);
            root.Access("title", out title);
            root.Access("desc", out desc);
            root.Access("_group1", out layout1);
            root.Access("_group2", out layout2);
            root.Access("_group3", out layout3);
            root.Access("_group3", out bg3);
            best3 = root.TryFind("_group3/best");
            group4 = root.TryFind("group4");
            best4 = root.TryFind("group4/best");
            root.Access("_group1/confirm", out confirm1);
            root.Access("_group2/confirm", out confirm2);
            root.Access("_group3/confirm", out confirm3);
            root.Access("group4/confirm", out confirm);
            confirm1.WithClickScale().FixPivot().WhenClick = () => ConfirmClick(layout1, pack.Content1);
            confirm2.WithClickScale().FixPivot().WhenClick = () => ConfirmClick(layout2, pack.Content2);
            confirm3.WithClickScale().FixPivot().WhenClick = () => ConfirmClick(layout3, pack.Content3);
            confirm.WithClickScale().FixPivot().WhenClick = ConfirmClick;
        }

        protected override void OnParse(params object[] items) {
            pack = (Pack1in3)items[0];
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
            MessageCenter.Get<IAP_INIT>().AddListener(WhenInit);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<ACTIVITY_REFRESH>().AddListener(WhenRefresh);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnPreClose() {
            MessageCenter.Get<IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<ACTIVITY_REFRESH>().RemoveListener(WhenRefresh);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        public override void RefreshTheme() {
            var visual = pack.Visual;
            visual.Refresh(bg1, "titleBg");
            visual.Refresh(title, "mainTitle");
            visual.Refresh(desc, "subTitle");
        }

        public void RefreshPack() {
            var active4 = pack.PackId4 > 0;
            var sale3 = pack.Sale && !active4;
            var sale4 = pack.Sale && active4;
            var y = active4 ? pos4 : pos3;
            static void SetY(Transform t_, float y_){
                var rt = (RectTransform)t_;
                rt.anchoredPosition = new(rt.anchoredPosition.x, y_);
            }
            static void RefreshL(MBRewardLayout layout_, BonusReward r_, bool alt_ = false) {
                var rr = new MBRewardLayout.RewardList() { list = r_.reward };
                if (alt_) layout_.RefreshActiveN(rr.Count);
                else layout_.RefreshActive(rr.Count);
                layout_.RefreshList(rr);
                foreach(var e in layout_.list) {
                    var s = (UITextState)e.objRef[0];
                    s.Enabled(!alt_);
                }
            }
            SetY(layout1.transform, y);
            SetY(layout2.transform, y);
            SetY(layout3.transform, y);
            RefreshL(layout1, pack.Goods1);
            RefreshL(layout2, pack.Goods2);
            RefreshL(layout3, pack.Goods3, sale3);
            bg3.Enabled(!sale3);
            best3.SetActive(sale3);
            group4.SetActive(active4);
            best4.SetActive(sale4);
        }

        public void RefreshPrice() {
            var valid = Game.Manager.iap.Initialized;
            confirm1.State(valid, pack.Price1);
            confirm2.State(valid, pack.Price2);
            confirm3.State(valid, pack.Price3);
            if (pack.PackId4 > 0) confirm.State(valid, pack.Price);
        }

        internal override void PurchaseComplete(IList<RewardCommitData> list_) {
            Pack.ResetPopup();
            pack.ReportPurchase();
            static void R(MBRewardLayout layout_, IList<RewardCommitData> list_, int o_) {
                for (int n = 0, m = o_; n < layout_.count; ++n, ++m) {
                    var pos = layout_.list[n].icon.transform.position;
                    if (m >= list_.Count) return;
                    UIFlyUtility.FlyReward(list_[m], pos);
                }
            }
            if (active == null) {
                if (list_.Count != layout1.count + layout2.count + layout3.count) {
                    DebugEx.Warning($"{nameof(Pack1in3)} reward count mismatch {list_.Count}");
                }
                var e = 0;
                R(layout1, list_, e);
                R(layout2, list_, e += layout1.count);
                R(layout3, list_, e += layout2.count);
            }
            else {
                R(active, list_, 0);
            }
            IEnumerator DelayClose() {
                yield return new WaitForSeconds(closeDelay);
                Close();
            }
            StartCoroutine(DelayClose());
        }

        internal void ConfirmClick(MBRewardLayout layout_, IAPPack content_) {
            active = layout_;
            Game.Manager.activity.giftpack.Purchase(pack, content_, WhenPurchase);
        }

        internal override void ConfirmClick() {
            Game.Manager.activity.giftpack.Purchase(pack, pack.Content, WhenPurchase);
        }
    }
}