// ==================================================
// // File: UIBPBuyTwoSuccess.cs
// // Author: liyueran
// // Date: 2025-06-23 17:06:37
// // Desc: bp活动购买付费二成功界面
// // ==================================================

using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIBPBuyTwoSuccess : UIBase, INavBack
    {
        private UIVisualGroup _visualGroup;
        private TextMeshProUGUI _desc;
        private TextMeshProUGUI _title;

        private TextProOnACircle _titleUnderCircle;
        private TextProOnACircle _titleCircle;

        // 活动实例 
        private BPActivity _activity;
        private PoolMapping.Ref<List<RewardCommitData>> _container;

        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("", out _visualGroup);

            transform.Access("Content/bg/title/title_under", out _titleUnderCircle);
            transform.Access("Content/bg/title/title_under/title", out _titleCircle);
        }

        private void AddButton()
        {
            transform.AddButton("Content/bg/title/close", OnClickClaimBtn).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/claimBtn", OnClickClaimBtn).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (BPActivity)items[0];
            _container = (PoolMapping.Ref<List<RewardCommitData>>)items[1];
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();

            Game.Manager.audioMan.TriggerSound("BattlePassBought");
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
            Game.Manager.audioMan.TriggerSound("CloseWindow");
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

            var visual = _activity.VisualBuyTwoSuccess;
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

        private void OnClickClaimBtn()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIBPReward, _activity, _container);
            Close();
        }

        public void OnNavBack()
        {
            OnClickClaimBtn();
        }
    }
}