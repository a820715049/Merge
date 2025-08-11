// ================================================
// File: UIFarmBoardComplete.cs
// Author: yueran.li
// Date: 2025/04/29 14:50:30 星期二
// Desc: 农场活动完成界面
// ================================================


using System.Collections;
using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIFarmBoardComplete : UIBase
    {
        // 活动实例
        private FarmBoardActivity _activity;

        // UI
        private TextMeshProUGUI leftTime;
        private TextProOnACircle title;

        // 进度条
        private MBFarmBoardProgress progress;

        #region UI
        protected override void OnCreate()
        {
            transform.Access("Content/Panel/TitleBg/Title", out title);
            transform.Access("Content/Panel/TitleBg/_cd/text", out leftTime);
            transform.Access("Content/Panel/Progress/UIFarmProgress", out progress);
            transform.AddButton("Content/Panel/BtnConfirm", Close);
            transform.AddButton("Content/Panel/BtnClose", Close);
            progress.SetUpOnCreate();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;

            // items[0]: activity;    items[1]: Custom
            _activity = (FarmBoardActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            if (_activity == null) return;
            _RefreshCD();
            RefreshTheme();
            progress.InitOnPreOpen(_activity);
        }

        protected override void OnPostOpen()
        {
            progress.RefreshProgress();
            progress.ScrollToItem(_activity.UnlockMaxLevel);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(_RefreshCD);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(_RefreshCD);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }
        #endregion

        #region Listener
        private void _RefreshCD()
        {
            UIUtility.CountDownFormat(leftTime, _activity?.Countdown ?? 0);
        }
        #endregion

        private void RefreshTheme()
        {
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is FarmBoardActivity)
            {
                Close();
            }
        }
    }
}