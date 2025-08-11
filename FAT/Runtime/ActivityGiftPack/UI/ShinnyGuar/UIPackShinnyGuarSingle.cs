/*
 * @Author: tang.yan
 * @Description: 闪卡必得礼包新模板UI
 * @Doc: https://centurygames.feishu.cn/wiki/SNnywB9LsiQ3WSk97qWce70Unj0
 * @Date: 2025-06-09 14:06:43
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using EL;

namespace FAT
{
    public class UIPackShinnyGuarSingle : PackUI
    {
        [SerializeField] public Transform label;
        [SerializeField] public UIShinnyGuarCardCell cardInfo;
        private Action WhenInit;
        private PackShinnyGuar pack;
        internal override ActivityLike Pack => pack;
        private UIIAPLabel iapLabel = new();
        private List<int> cardInfos = new();

        protected override void OnCreate()
        {
            base.OnCreate();
            cardInfo.Init();
        }

        protected override void OnParse(params object[] items)
        {
            pack = (PackShinnyGuar)items[0];
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
            RefreshPack();
            RefreshPrice();
            RefreshCD();
            RefreshLabel();
            RefreshCardList();
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

        internal override void PurchaseComplete(IList<RewardCommitData> list_)
        {
            base.PurchaseComplete(list_);
            pack.Track();
        }

        private void RefreshCardList()
        {
            cardInfos.Clear();
            Game.Manager.cardMan.FillShowCardIdList(pack.confD.CardDisplayNum, cardInfos);
            if (cardInfos.Count > 0)
            {
                cardInfo.Refresh(cardInfos[0]);
            }
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().RemoveListener(WhenRefresh);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        protected override void OnPostClose()
        {
            iapLabel.Clear();
        }

        private void RefreshLabel()
        {
            iapLabel.Setup(label, pack.confD.Label, pack.PackId);
        }

        public void RefreshPack()
        {
            iapLabel.Setup(label, pack.LabelId, pack.PackId);
            var r = pack.Goods.reward;
            layout.Refresh(r);
            stock.text = I18N.FormatText("#SysComDesc115", $"{pack.Stock}/{pack.StockTotal}");
        }

        public void RefreshPrice()
        {
            var valid = Game.Manager.iap.Initialized;
            confirm.State(valid, pack.Price);
        }

        internal override void ConfirmClick()
        {
            Game.Manager.activity.giftpack.Purchase(pack, pack.Content, WhenPurchase);
        }
    }
}