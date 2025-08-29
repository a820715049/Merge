/*
 * @Author: yanfuxing
 * @Date: 2025-05-08 11:25:40
 */

using System;
using System.Collections.Generic;
using Config;
using EL;
using TMPro;

namespace FAT
{
    public class UINoticeRedeemShop : UIBase
    {
        private TextProOnACircle _title;
        private TextMeshProUGUI _subTitle;
        private TextMeshProUGUI _leftTime;
        private TextMeshProUGUI _desc;
        private Action _whenTick;
        private ActivityRedeemShopLike _activityRedeemShopLike;
        protected override void OnCreate()
        {
            base.OnCreate();
            _title = transform.Find("Content/title").GetComponent<TextProOnACircle>();
            _leftTime = transform.Find("Content/_cd/text").GetComponent<TextMeshProUGUI>();
            _subTitle = transform.Find("Content/helpBg/subTitle").GetComponent<TextMeshProUGUI>();
            _desc = transform.Find("Content/desc").GetComponent<TextMeshProUGUI>();
            transform.AddButton("Content/btnPlay", _ClickConfirm);
            transform.AddButton("Content/close", Close);
        }
        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            if (items.Length > 0 && items[0] is ActivityRedeemShopLike redeemShopLike)
            {
                _activityRedeemShopLike = redeemShopLike;
            }
        }
        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            _whenTick ??= RefreshCD;

            //mainTitle:#SysComDesc1148,subTitle:#SysComDesc485,desc1:#SysComDesc1149,button:#SysComDesc1150
            _activityRedeemShopLike.VisualUINoticeRedeemShop.visual.Refresh(_desc, "desc1");
            _activityRedeemShopLike.VisualUINoticeRedeemShop.visual.Refresh(_subTitle, "subTitle");
            _activityRedeemShopLike.Visual.Refresh(_title, "mainTitle");
            RefreshCD();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_whenTick);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_whenTick);
        }

        private void RefreshCD()
        {
            var v = _activityRedeemShopLike?.Countdown ?? 0;
            UIUtility.CountDownFormat(_leftTime, v);
            if (v <= 0)
                Close();
        }
        private void _ClickConfirm()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIRedeemShopMain, _activityRedeemShopLike);
            Game.Manager.screenPopup.Wait(UIConfig.UIRedeemShopMain);
            Close();
        }
    }
}


