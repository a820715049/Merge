/**
 * @Author: zhangpengjian
 * @Date: 2024/8/19 10:27:30
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/19 10:27:30
 * Description: 挖沙活动开启界面
 */

using System;
using System.Collections.Generic;
using Config;
using EL;
using TMPro;

namespace FAT
{
    public class UIDiggingBegin : UIBase
    {
        private TextProOnACircle title;
        private TextMeshProUGUI leftTime;
        private TextMeshProUGUI desc;
        private MBRewardLayout rewardLayout;
        private Action whenTick;
        private List<RewardConfig> rewardConfigList = new();
        private UIImageRes bg;
        private UIImageRes titleBg;

        private ActivityDigging activityDigging;
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
            activityDigging = (ActivityDigging)items[0];
        }

        protected override void OnPreOpen()
        {
            var config = activityDigging.diggingConfig;
            if (config == null) return;
            rewardConfigList.Clear();
            var rL = activityDigging.GetLastLevel().LevelReward;
            foreach (var item in rL)
            {
                var r = item.ConvertToRewardConfig();
                rewardConfigList.Add(r);
            }
            var c = Game.Manager.objectMan.GetTokenConfig(activityDigging.diggingConfig.TokenId);
            desc.SetText(I18N.FormatText("#SysComDesc760", c.SpriteName));
            rewardLayout.Refresh(rewardConfigList);
            whenTick ??= _RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(whenTick);
            activityDigging.VisualStart.Refresh(bg, "bg");
            activityDigging.VisualStart.Refresh(titleBg, "titleBg");
            activityDigging.VisualStart.Refresh(title, "mainTitle");
            title.SetText(I18N.Text(config.Name));
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(whenTick);
        }

        private void _RefreshCD()
        {
            var v = activityDigging?.Countdown ?? 0;
            UIUtility.CountDownFormat(leftTime, v);
            if (v <= 0)
                Close();
        }

        private void _ClickConfirm()
        {
            if (activityDigging != null)
            {
                if (activityDigging.GetKeyNum() > 0)
                {
                    UIDiggingUtility.EnterActivity();
                }
            }
            Close();
        }
    }
}