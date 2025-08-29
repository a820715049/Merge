/*
 * @Author: yanfuxing
 * @Date: 2025-06-13 15:40:09
 */

using System;
using System.Collections.Generic;
using Config;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIWishBoardStartNotice : UIBase
    {
        private TextProOnACircle title;
        private TextMeshProUGUI desc;
        private TextMeshProUGUI leftTime;
        private UIImageRes bg;
        private UIImageRes titleBg;
        private MBRewardLayout rewardLayout;
        private List<RewardConfig> rewardConfigList = new();
        private WishBoardActivity wishBoardActivity;
        private Action whenTick;
        protected override void OnCreate()
        {
            base.OnCreate();
            title = transform.Find("Content/Panel/TitleBg/Title").GetComponent<TextProOnACircle>();
            desc = transform.Find("Content/Panel/Desc3").GetComponent<TextMeshProUGUI>();
            rewardLayout = transform.Find("Content/Panel/_group").GetComponent<MBRewardLayout>();
            transform.AddButton("Content/Panel/BtnConfirm", _ClickConfirm);
            bg = transform.Find("Content/Panel/Bg").GetComponent<UIImageRes>();
            titleBg = transform.Find("Content/Panel/TitleBg").GetComponent<UIImageRes>();
            leftTime = transform.Find("Content/Panel/_cd/text").GetComponent<TextMeshProUGUI>();
        }

        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            wishBoardActivity = (WishBoardActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            whenTick ??= RefreshCD;
            InitReward();
            wishBoardActivity.Visual.Refresh(title, "mainTitle");
            wishBoardActivity.Visual.Refresh(desc, "desc1");
            RefreshCD();
        }
        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(whenTick);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(whenTick);
        }

        protected override void OnPostClose()
        {
            base.OnPostClose();
        }

        private void RefreshCD()
        {
            var v = wishBoardActivity?.Countdown ?? 0;
            UIUtility.CountDownFormat(leftTime, v);
            if (v <= 0)
            {
                Close();
            }
        }

        private void InitReward()
        {
            rewardConfigList = wishBoardActivity.GetMileStoneLastStageReward();
            rewardLayout.Refresh(rewardConfigList);
            var item = rewardLayout.list[rewardLayout.list.Count - 1];
            if (item != null)
            {
                var mbRewardIcon = item.transform.GetComponent<MBRewardIcon>();
                if (mbRewardIcon != null)
                {
                    mbRewardIcon.WhenClick = null;
                }
                var iconImage = item.transform.Find("icon").GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.raycastTarget = true;
                    iconImage.enabled = true;
                    var info = iconImage.transform.Find("info");
                    if (info != null)
                    {
                        info.gameObject.SetActive(true);
                    }
                }
                //刷新最后一个物品,跟策划约定最后一个物品为循环宝箱：做特殊Tips处理
                //判断是否已经添加过监听，如果没有就添加一个
                if (mbRewardIcon.WhenClick == null)
                {
                    mbRewardIcon.WhenClick = (id, custom) =>
                    {
                        var itemRect = item.transform as RectTransform;
                        var itemHeight = itemRect.rect.height * 0.5f;
                        UIManager.Instance.OpenWindow(UIConfig.UIWishBoardMilestoneTips, iconImage.transform.position, itemHeight);
                        return true;
                    };
                }
            }
        }
        private void _ClickConfirm()
        {
            if (wishBoardActivity != null)
            {
                wishBoardActivity.EnterWishBoard();
            }
            Close();
        }
    }
}