/**
 * @Author: zhangpengjian
 * @Date: 2024-04-25 10:18:02
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/12/13 14:22:49
 * Description: 寻宝开启弹窗
 */

using System;
using System.Collections.Generic;
using Config;
using EL;
using TMPro;

namespace FAT
{
    public class UITreasureHuntStartNotice : UIBase
    {
        private TextProOnACircle title;
        private TextMeshProUGUI leftTime;
        private TextMeshProUGUI desc;
        private MBRewardLayout rewardLayout;
        private Action whenTick;
        private List<RewardConfig> rewardConfigList = new();
        private UIImageRes bg;
        private UIImageRes titleBg;
        private ActivityTreasure activityTreasure;

        protected override void OnCreate()
        {
            title = transform.Find("Content/Panel/TitleBg/Title").GetComponent<TextProOnACircle>();
            leftTime = transform.Find("Content/Panel/_cd/text").GetComponent<TextMeshProUGUI>();
            desc = transform.Find("Content/Panel/Desc3").GetComponent<TextMeshProUGUI>();
            rewardLayout = transform.Find("Content/Panel/_group").GetComponent<MBRewardLayout>();
            transform.AddButton("Content/Panel/BtnConfirm", _ClickConfirm);
            bg = transform.Find("Content/Panel/Bg").GetComponent<UIImageRes>();
            titleBg = transform.Find("Content/Panel/TitleBg").GetComponent<UIImageRes>();
        }

        protected override void OnParse(params object[] items)
        {
            activityTreasure = (ActivityTreasure)items[0];
        }

        protected override void OnPreOpen()
        {
            rewardConfigList.Clear();
            activityTreasure.GetMileStoneReward(rewardConfigList);
            whenTick ??= _RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(whenTick);
            rewardLayout.Refresh(rewardConfigList);
            activityTreasure.VisualStart.Refresh(bg, "bgImage");
            activityTreasure.VisualStart.Refresh(titleBg, "titleImage");
            activityTreasure.VisualStart.Theme.TextInfo.TryGetValue("desc", out string d);
            var c = Game.Manager.objectMan.GetTokenConfig(activityTreasure.ConfD.RequireCoinId);
            desc.SetText(I18N.FormatText(d, c.SpriteName));
            activityTreasure.Visual.Refresh(title, "mainTitle");
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(whenTick);
        }

        private void _RefreshCD()
        {
            var v = activityTreasure?.Countdown ?? 0;
            UIUtility.CountDownFormat(leftTime, v);
            if (v <= 0)
                Close();
        }

        private void _ClickConfirm()
        {
            if (activityTreasure != null && activityTreasure.GetKeyNum() > 0)
            {
                UITreasureHuntUtility.EnterActivity();
            }
            Close();
        }
    }
}