/**
 * @Author: zhangpengjian
 * @Date: 2024/10/11 14:30:52
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/11 14:30:52
 * Description: 集卡-商店赠品活动
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT
{
    public class UINoticeCardXL : UIBase
    {
        [SerializeField] private TMP_Text countDown;
        private Action<ActivityLike, bool> WhenEnd;
        private Action WhenTick;
        private ActivityMIG _activity;

        protected override void OnParse(params object[] items)
        {
            _activity = (ActivityMIG)items[0];
        }

        protected override void OnCreate()
        {
            transform.FindEx<Button>("Content/close").WithClickScale().FixPivot().onClick.AddListener(Close);
            transform.FindEx<Button>("Content/confirm").WithClickScale().onClick.AddListener(Close);
        }

        protected override void OnPreOpen()
        {
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