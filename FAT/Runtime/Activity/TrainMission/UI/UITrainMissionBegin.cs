// ================================================
// File: UITrainOrderMain.cs
// Author: yueran.li
// Date: 2025/07/28 17:57:11 星期一
// Desc: 火车任务开启界面
// ================================================

using System.Collections.Generic;
using EL;
using FAT.Merge;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UITrainMissionBegin : UIBase
    {
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _desc1;
        private TextMeshProUGUI _desc2;
        private TextMeshProUGUI _desc3;
        private TextMeshProUGUI _cd;
        private RectTransform _group;
        private Image _descBg;

        // 活动实例 
        private TrainMissionActivity _activity;

        #region UI基础
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("Content/Root/Frame/Title", out _title);
            transform.Access("Content/Root/Frame/desc1", out _desc1);
            transform.Access("Content/Root/Frame/group1/desc2", out _desc2);
            transform.Access("Content/Root/Frame/group1/desc3", out _desc3);
            transform.Access("Content/Root/descBg", out _descBg);
            transform.Access("Content/Root/Frame/group1", out _group);
            transform.Access("Content/Root/cd/_cd", out _cd);
        }

        private void AddButton()
        {
            transform.AddButton("Content/Root/BtnClose", Close).WithClickScale().FixPivot();
            transform.AddButton("Content/Root/BtnConfirm", OnClickConfirm).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (TrainMissionActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
            SwitchVisual(!_activity.NeedChooseGroup());
            RefreshCd();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
        }

        protected override void OnPostOpen()
        {
        }


        protected override void OnPreClose()
        {
        }

        protected override void OnPostClose()
        {
        }
        #endregion

        private void SwitchVisual(bool joined)
        {
            _group.gameObject.SetActive(!joined);
            _descBg.gameObject.SetActive(!joined);

            _desc1.gameObject.SetActive(joined);
        }


        #region 事件
        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is TrainMissionActivity)
            {
                Close();
            }
        }

        private void RefreshCd()
        {
            if (_activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            _cd.SetCountDown(diff);
        }

        private void OnClickConfirm()
        {
            _activity.Open();
            Close();
        }
        #endregion

        private void RefreshTheme()
        {
            _title.SetText(I18N.Text("#SysComDesc1538"));
        }
    }
}