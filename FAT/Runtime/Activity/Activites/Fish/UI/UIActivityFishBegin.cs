// ================================================
// File: UIActivityFishBegin.cs
// Author: yueran.li
// Date: 2025/04/10 17:25:28 星期四
// Desc: 钓鱼活动开始界面
// ================================================

using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIActivityFishBegin : UIBase
    {
        // 活动实例
        private ActivityFishing activityFish;

        // UI
        private TextMeshProUGUI leftTime;
        private TextProOnACircle title;


        #region UI
        protected override void OnCreate()
        {
            transform.Access("Content/Panel/TitleBg/Title", out title);
            transform.Access("Content/Panel/_cd/text", out leftTime);
            transform.AddButton("Content/Panel/BtnConfirm", _ClickConfirm);
            transform.AddButton("Content/Panel/BtnClose", Close);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;

            // items[0]: activity;    items[1]: Custom
            activityFish = (ActivityFishing)items[0];
        }

        protected override void OnPreOpen()
        {
            if (activityFish == null) return;
            _RefreshCD();
            RefreshTheme();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(_RefreshCD);
        }


        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(_RefreshCD);
        }
        #endregion

        #region Listener
        private void _RefreshCD()
        {
            UIUtility.CountDownFormat(leftTime, activityFish?.Countdown ?? 0);
        }

        private void _ClickConfirm()
        {
            Close();

            // 进入钓鱼棋盘
            activityFish?.Open();
        }
        #endregion

        private void RefreshTheme()
        {
        }
    }
}