/**
 * @Author: zhangpengjian
 * @Date: 2024/11/25 15:20:10
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/11/25 15:20:10
 * Description: 闪卡必得礼包ui
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using EL;
using fat.rawdata;
using TMPro;

namespace FAT
{
    public class UIPackShinnyGuar : PackUI
    {
        [SerializeField] public Transform label;
        [SerializeField] public Transform card3Root;
        [SerializeField] public Transform card2Root;
        [SerializeField] public TMP_Text topTitle;
        [SerializeField] public TMP_Text title;
        [SerializeField] public List<UIShinnyGuarCardCell> card2List;
        [SerializeField] public List<UIShinnyGuarCardCell> card3List;
        private Action WhenInit;
        private PackShinnyGuar pack;
        internal override ActivityLike Pack => pack;
        private UIIAPLabel iapLabel = new();
        private List<int> cardInfos = new();

        protected override void OnCreate()
        {
            base.OnCreate();
            foreach (var item in card2List)
            {
                item.Init();
            }
            foreach (var item in card3List)
            {
                item.Init();
            }
        }

        protected override void OnParse(params object[] items)
        {
            pack = (PackShinnyGuar)items[0];
        }

        protected override void OnPreOpen()
        {
            pack.Visual.Refresh(topTitle, "topTitle");
            pack.Visual.Refresh(title, "mainTitle");
            pack.Visual.Theme.TextInfo.TryGetValue("topTitle", out var c);
            var cardPackConfig = Game.Manager.objectMan.GetCardPackConfig(pack.confD.SpecialPackId);
            topTitle.SetText(I18N.FormatText(c, pack.confD.CardDisplayNum, cardPackConfig.CardNum));
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
                if (cardInfos.Count == 2)
                {
                    card3Root.gameObject.SetActive(false);
                    card2Root.gameObject.SetActive(true);
                    for (int i = 0; i < cardInfos.Count; i++)
                    {
                        card2List[i].Refresh(cardInfos[i]);
                    }
                }
                else if (cardInfos.Count == 3)
                {
                    card3Root.gameObject.SetActive(true);
                    card2Root.gameObject.SetActive(false);
                    for (int i = 0; i < cardInfos.Count; i++)
                    {
                        card3List[i].Refresh(cardInfos[i]);
                    }
                }
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

        internal void ConfirmClick(IAPPack content_)
        {
            Game.Manager.activity.giftpack.Purchase(pack, content_, WhenPurchase);
        }

        internal override void ConfirmClick()
        {
            Game.Manager.activity.giftpack.Purchase(pack, pack.Content, WhenPurchase);
        }
    }
}