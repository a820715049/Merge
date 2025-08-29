using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using static fat.conf.Data;
using fat.rawdata;

namespace FAT {
    public class UIToolExchange : PackUI {
        private Action<CoinType> WhenCoinChange;
        private ExchangeTool pack;
        internal override ActivityLike Pack => pack;

        protected override void OnParse(params object[] items) {
            pack = (ExchangeTool)items[0];
        }

        protected override void OnPreOpen() {
            RefreshPack();
            RefreshPrice();
            RefreshCD();
            WhenEnd ??= RefreshEnd;
            WhenTick ??= RefreshCD;
            WhenCoinChange ??= RefreshPriceState;
            WhenPurchase ??= r_ => PurchaseComplete(r_);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().AddListener(WhenCoinChange);
        }

        protected override void OnPreClose() {
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().RemoveListener(WhenCoinChange);
        }

        public void RefreshPack() {
            layout.Refresh(pack.goods);
            stock.text = I18N.FormatText("#SysComDesc115", $"{pack.Stock}/{pack.StockTotal}");
        }

        public void RefreshPrice() {
            confirm.text.Text = $"{pack.price}{TextSprite.Coin}";
            RefreshPriceState(CoinType.MergeCoin);
        }

        public void RefreshPriceState(CoinType type_) {
            if (type_ != CoinType.MergeCoin) return;
            var coinMan = Game.Manager.coinMan;
            var valid = coinMan.CanUseCoin(CoinType.MergeCoin, pack.price);
            confirm.State(valid);
        }

        internal override void ConfirmClick() {
            Game.Manager.activity.exchange.Purchase(pack, WhenPurchase);
        }
    }
}