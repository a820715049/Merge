using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;
using fat.rawdata;
using static fat.conf.Data;
using System.Collections;
using Config;

namespace FAT {
    public class UIOutOfEnergy : UIBase {
        public Animator uiAnim;
        public GameObject groupStatus;
        public TextMeshProUGUI diamondCount;
        public UIImageRes icon;
        public TextMeshProUGUI desc;
        public TextMeshProUGUI count;
        public UITextState discount;
        public MapButton close;
        public MapButton confirm;
        public GameObject groupAd;
        public TextMeshProUGUI descAd;
        public MapButton confirmAd;

        private Merge.Board board;
        private bool free;
        private int claim;
        private MergeBoardEnergy eConf;
        private ShopEnergyData item;
        // 是否消费/看广告获得体力
        private bool isConfirmUsed;

#if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;
            uiAnim = GetComponent<Animator>();
            transform.Access("Content", out Transform root);
            groupStatus = root.Find("frame")?.gameObject;
            diamondCount = root.FindEx<TextMeshProUGUI>("frame/text");
            close = root.FindEx<MapButton>("close");
            root = root.Find("root");
            icon = root.FindEx<UIImageRes>("center/icon");
            desc = root.FindEx<TextMeshProUGUI>("desc");
            count = root.FindEx<TextMeshProUGUI>("count");
            discount = root.FindEx<UITextState>("confirm/discount");
            confirm = root.FindEx<MapButton>("confirm");
            root = root.Find("ad");
            groupAd = root.gameObject;
            descAd = root.FindEx<TextMeshProUGUI>("desc");
            confirmAd = root.FindEx<MapButton>("confirm");
        }
#endif

        protected override void OnCreate() {
            close.WithClickScale().FixPivot().WhenClick = UserClose;
            confirm.WithClickScale().FixPivot().WhenClick = ConfirmClick;
            confirmAd.WithClickScale().FixPivot().WhenClick = AdClick;
        }

        protected override void OnPreOpen() {
            isConfirmUsed = false;
            UIUtility.FadeIn(this, uiAnim);
            RefreshInfo();
            RefreshStatus();
            RefreshPrice();
            RefreshAd();
            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().AddListener(OnCoinChange);
            MessageCenter.Get<MSG.AD_READY_ANY>().AddListener(RefreshAd);
        }

        protected override void OnPreClose() {
            MessageCenter.Get<MSG.UI_TOP_BAR_POP_STATE>().Dispatch();
            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().RemoveListener(OnCoinChange);
            MessageCenter.Get<MSG.AD_READY_ANY>().RemoveListener(RefreshAd);
        }

        protected override void OnPostClose() {
            if (!isConfirmUsed) {
                Game.Manager.notification.TryRemindEnergy();
            }
            isConfirmUsed = false;
        }

        private void UserClose() {
            UIUtility.FadeOut(this, uiAnim);
        }

        private void OnCoinChange(CoinType t_) {
            if (t_ == CoinType.Gem) RefreshStatus();
        }

        private void RefreshStatus() {
            groupStatus.SetActive(!free);
            if (free) return;
            var coinMan = Game.Manager.coinMan;
            diamondCount.text = $"{coinMan.GetCoin(CoinType.Gem)}";
        }


        private void RefreshInfo() {
            board = Game.Manager.mainMergeMan.world.activeBoard;
            var id = board.boardId;
            eConf = GetMergeBoardEnergy(id);
            claim = Game.Manager.mergeEnergyMan.ClaimCount(id);
            free = claim < eConf.FreeReward.Count;
            close.gameObject.SetActive(!free);
            icon.SetImage(eConf.Image);
        }

        private void RefreshPrice() {
            confirm.enabled = true;
            discount.gameObject.SetActive(false);
            if (free) {
                confirm.text.Text = I18N.Text("#SysComBtn8");
                return;
            }
            var itemId = eConf.MarketIncrease;
            var boardTarget = itemId[0];
            var itemTarget = itemId[1];
            var sData = (ShopTabEnergyData)Game.Manager.shopMan.GetShopTabData(ShopTabType.Energy, boardId: boardTarget);
            foreach(var e in sData.EnergyDataList) {
                if (e.GirdId == itemTarget) {
                    item = e;
                    break;
                }
            }
            var conf = item.CurSellGoodsConfig;
            var price = conf.Price.ConvertToRewardConfig();
            confirm.text.Text = $"{price.Count}{TextSprite.Diamond}";
            var price1s = conf.OriginalPrice;
            if (!string.IsNullOrEmpty(price1s)) {
                discount.gameObject.SetActive(true);
                var price1 = price1s.ConvertToRewardConfig();
                discount.Text = $"{price1.Count}{TextSprite.Diamond}";
            }
        }

        private void RefreshAd() {
            var ads = Game.Manager.adsMan;
            var reward = eConf.AdReward.ConvertToRewardConfig();
            var ready = ads.CheckCanWatchAds(eConf.AdId);
            var valid = !free && ads.IsAdsOpen && ready;
            groupAd.SetActive(valid);
            if (!valid) return;
            descAd.text = I18N.FormatText(eConf.AdDesc, $"{reward.Count}{TextSprite.Energy}");
            confirmAd.State(ready);
            DataTracker.TrackAdIconShow(eConf.AdId);
        }

        private void ConfirmUsed() {
            isConfirmUsed = true;
            confirm.enabled = false;
            Game.Manager.screenPopup.ResetState(PopupType.Energy);
            UserClose();
        }

        private void ConfirmClick() {
            bool success;
            var pos = icon.transform.position;
            if (free) {
                var r = eConf.FreeReward[claim].ConvertToRewardConfig();
                var rData = Game.Manager.rewardMan.BeginReward(r.Id, r.Count, ReasonString.free);
                Game.Manager.mergeEnergyMan.ClaimEnergy(board.boardId);
                UIFlyUtility.FlyReward(rData, pos);
                success = true;
            }
            else {
                success = Game.Manager.shopMan.TryBuyShopEnergyGoods(item, pos);
            }
            if (success) ConfirmUsed();
        }

        private void AdClick() {
            var ads = Game.Manager.adsMan;
            var pos = icon.transform.position;
            DataTracker.TrackAdIconClick(eConf.AdId);
            ads.TryPlayAdsVideo(eConf.AdId, (_, r_) => {
                if (!r_) return;
                var r = eConf.AdReward.ConvertToRewardConfig();
                DataTracker.TrackAdReward(eConf.AdId, r.Count, r.Id);
                var rData = Game.Manager.rewardMan.BeginReward(r.Id, r.Count, ReasonString.ad);
                UIFlyUtility.FlyReward(rData, pos);
                ConfirmUsed();
            });
        }
    }
}