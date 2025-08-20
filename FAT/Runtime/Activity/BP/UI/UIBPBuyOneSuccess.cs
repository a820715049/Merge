// ==================================================
// // File: UIBPBuyOneSuccess.cs
// // Author: liyueran
// // Date: 2025-06-23 17:06:37
// // Desc: bp活动购买付费一成功界面
// // ==================================================

using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIBPBuyOneSuccess : UIBase
    {
        private UIVisualGroup _visualGroup;
        private TextMeshProUGUI _desc;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _luxuryPrice;
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
            transform.Access("", out _visualGroup);

            transform.Access("Content/bg/title/title_under", out _titleUnderCircle);
            transform.Access("Content/bg/title/title_under/title", out _titleCircle);
            transform.Access("Content/bg/Rewards/luxury/claimBtn/price", out _luxuryPrice);
        }

        private void AddButton()
        {
            transform.AddButton("Content/bg/title/close", Close).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/Rewards/luxury/claimBtn", OnClickClaimBtn).WithClickScale().FixPivot();
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
            _luxuryPrice.SetText(_activity.GetIAPPriceByType(BPActivity.BPPurchaseType.Up));

            Game.Manager.audioMan.TriggerSound("BattlePassBought");
        }


        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<GAME_BP_BUY_SUCCESS>().AddListener(OnBpBuySuccess);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<GAME_BP_BUY_SUCCESS>().RemoveListener(OnBpBuySuccess);
        }

        protected override void OnPostOpen()
        {
            var ui = UIManager.Instance.TryGetUI(UIConfig.UIBPMain);
            if (ui != null && ui is UIBPMain main)
            {
                // 里程碑跳转
                main.JumpToCurLvMilestone();
            }
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

            var visual = _activity.VisualBuyOneSuccess;
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
            _activity.TryPurchase(BPActivity.BPPurchaseType.Up);
        }

        private void OnBpBuySuccess(BPActivity.BPPurchaseType type, PoolMapping.Ref<List<RewardCommitData>> container,
            bool late_)
        {
            if (type == BPActivity.BPPurchaseType.Up)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBPBuyTwoSuccess, _activity, container);
            }

            Close();
        }
    }
}