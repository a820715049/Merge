// ==================================================
// // File: UIBPDoubleCheck.cs
// // Author: liyueran
// // Date: 2025-06-23 17:06:37
// // Desc: bp活动 二次确认界面
// // ==================================================

using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIBPDoubleCheck : UIBase, INavBack
    {
        private UIVisualGroup _visualGroup;
        private TextMeshProUGUI _desc;
        private TextMeshProUGUI _title;

        // 活动实例 
        private BPActivity _activity;

        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("", out _visualGroup);
        }

        private void AddButton()
        {
            transform.AddButton("Content/root/confirm", OnClickClose).WithClickScale().FixPivot();
            transform.AddButton("Content/root/giveUp", OnClickGiveUp).WithClickScale().FixPivot();
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
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
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

        private void RefreshTheme()
        {
            if (_activity == null)
            {
                return;
            }

            var visual = _activity.VisualDoubleCheck;
            visual.Refresh(_visualGroup);
        }

        private void RefreshCd()
        {
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is BPActivity)
            {
                Close();
            }
        }

        private void OnClickClose()
        {
            Close();
        }

        private void OnClickGiveUp()
        {
            var ui = UIManager.Instance.TryGetUI(UIConfig.UIBPEnd);
            if (ui != null && ui is UIBPEnd end)
            {
                end.giveUpBuy = true;
                end.CheckUIState();
            }

            Close();
        }

        public void OnNavBack()
        {
            OnClickClose();
        }
    }
}