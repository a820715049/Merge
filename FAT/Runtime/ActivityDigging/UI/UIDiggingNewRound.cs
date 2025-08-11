/**
 * @Author: zhangpengjian
 * @Date: 2024/8/28 10:52:17
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/28 10:52:17
 * Description: 挖沙新一轮
 */

using System;
using EL;
using fat.conf;
using TMPro;

namespace FAT
{
    public class UIDiggingNewRound : UIBase
    {
        private TextProOnACircle title;
        private TextMeshProUGUI leftTime;
        private Action whenTick;
        private UIImageRes bg;
        private UIImageRes titleBg;

        private ActivityDigging activityDigging;
        protected override void OnCreate()
        {
            title = transform.Find("Content/Panel/TitleBg/Title").GetComponent<TextProOnACircle>();
            leftTime = transform.Find("Content/Panel/_cd/text").GetComponent<TextMeshProUGUI>();
            transform.AddButton("Content/Panel/BtnConfirm", _ClickConfirm);
            bg = transform.Find("Content/Panel/Bg").GetComponent<UIImageRes>();
            titleBg = transform.Find("Content/Panel/TitleBg").GetComponent<UIImageRes>();
        }

        protected override void OnPreOpen()
        {
            Game.Manager.activity.LookupAny(fat.rawdata.EventType.Digging, out var activity);
            if (activity == null)
            {
                return;
            }
            activityDigging = (ActivityDigging)activity;
            var config = activityDigging.diggingConfig;
            if (config == null) return;
            whenTick ??= _RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(whenTick);
            activityDigging.VisualNewRound.Refresh(bg, "bg");
            activityDigging.VisualNewRound.Refresh(titleBg, "titleBg");
            activityDigging.VisualNewRound.Refresh(title, "mainTitle");
            title.SetText(I18N.Text(config.Name));
        }

        protected override void OnPreClose() {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(whenTick);
        }

        protected override void OnPostClose()
        {
            MessageCenter.Get<MSG.DIGGING_LEVE_ROUND_CLEAR>().Dispatch();
        }
        
        private void _RefreshCD()
        {
            var v = activityDigging?.Countdown ?? 0;
            UIUtility.CountDownFormat(leftTime, v);
            if(v <= 0)
                Close();
        }

        private void _ClickConfirm()
        {
            Close();
        }
    }
}
