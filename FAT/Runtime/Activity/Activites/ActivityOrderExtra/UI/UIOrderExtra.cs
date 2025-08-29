/*
 * @Author: pengjian.zhang
 * @Description: 订单额外奖励活动界面及弹脸
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/kc7e5p7c7gti54gg?inner=JybC5
 * @Date: 2024-03-19 16:42:25
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT
{
    public class UIOrderExtra : UIBase
    {
        [SerializeField] private TMP_Text countDown;
        [SerializeField] private TextProOnACircle title;
        [SerializeField] private TMP_Text desc;
        [SerializeField] private UIImageRes titleBg;
        [SerializeField] private UIImageRes bg;
        private Action<ActivityLike, bool> WhenEnd;
        private Action WhenTick;
        private ActivityExtraRewardOrder _activity;

        protected override void OnParse(params object[] items)
        {
            _activity = (ActivityExtraRewardOrder)items[0];
        }

        protected override void OnCreate()
        {
            transform.FindEx<Button>("Content/close").WithClickScale().FixPivot().onClick.AddListener(Close);
            transform.FindEx<Button>("Content/confirm").WithClickScale().onClick.AddListener(Close);
        }

        protected override void OnPreOpen()
        {
            _activity.VisualPanel.Refresh(title, "mainTitle");
            _activity.VisualPanel.Refresh(desc, "subTitle");
            _activity.VisualPanel.Refresh(titleBg, "titleImage");
            _activity.VisualPanel.Refresh(bg, "bgImage");
            _activity.VisualPanel.RefreshStyle(countDown, "time");
            UIUtility.CountDownFormat(countDown, _activity.Countdown);
        }

        protected override void OnAddListener()
        {
            WhenEnd ??= _RefreshEnd;
            WhenTick ??= _RefreshCD;
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        private void _RefreshCD()
        {
            UIUtility.CountDownFormat(countDown, _activity.Countdown);
        }

        private void _RefreshEnd(ActivityLike pack_, bool expire_)
        {
            if (pack_ != _activity) return;
            Close();
        }
    }
}