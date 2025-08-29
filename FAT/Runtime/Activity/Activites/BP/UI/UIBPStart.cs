// ==================================================
// // File: UIBPStart.cs
// // Author: liyueran
// // Date: 2025-06-23 17:06:37
// // Desc: bp活动开启界面
// // ==================================================

using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIBPStart : UIBase
    {
        private UIVisualGroup visualGroup;

        private TextProOnACircle _titleUnderCircle;
        private TextProOnACircle _titleCircle;

        // 活动实例 
        private BPActivity _activity;

        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("", out visualGroup);

            transform.Access("Content/bg/title/title_under", out _titleUnderCircle);
            transform.Access("Content/bg/title/title_under/title", out _titleCircle);
        }

        private void AddButton()
        {
            transform.AddButton("Content/bg/title/close", Close).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/claimBtn", OnClickClaimBtn).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (BPActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
        }


        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }


        private void RefreshTheme()
        {
            if (_activity == null)
            {
                return;
            }

            var visual = _activity.StartPopup;
            visual.Refresh(visualGroup);
        }


        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is BPActivity)
            {
                Close();
            }
        }

        private void OnClickClaimBtn()
        {
            _activity.Open();
            Close();
        }
    }
}