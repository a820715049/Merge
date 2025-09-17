// ==================================================
// // File: MBTrainMisssionBagEntry.cs
// // Author: liyueran
// // Date: 2025-07-31 17:07:29
// // Desc: $
// // ==================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EL;


namespace FAT
{
    public class MBTrainMisssionBagEntry : MonoBehaviour
    {
        private TrainMissionActivity _activity;
        private Animator animator;
        public void Setup()
        {
            animator = transform.GetComponent<Animator>();
            transform.AddButton(null, _OnBtnInventory).FixPivot();
        }

        public void InitOnPreOpen(TrainMissionActivity act)
        {
            _activity = act;
            _RefreshEntry();
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().AddListener(_RefreshEntry);
            MessageCenter.Get<MSG.UI_INVENTORY_ENTRY_FEEDBACK>().AddListener(_OnMessageInventoryFeedback);
        }

        public void CleanupOnPostClose()
        {
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().RemoveListener(_RefreshEntry);
            MessageCenter.Get<MSG.UI_INVENTORY_ENTRY_FEEDBACK>().RemoveListener(_OnMessageInventoryFeedback);
        }

        private void _OnBtnInventory()
        {
            // TODO
            UIManager.Instance.OpenWindow(UIConfig.UITrainMissionBag, _activity);
        }

        private void _OnMessageInventoryFeedback()
        {
            if (animator != null)
            {
                animator.SetTrigger("Punch");
            }
        }

        private void _RefreshEntry()
        {
            _RefreshInventoryEntryScreenPos();
        }

        private void _RefreshInventoryEntryScreenPos()
        {
            var sp = GuideUtility.CalcRectTransformScreenPos(null, transform);
            BoardViewManager.Instance.RefreshInventoryEntryScreenPos(sp);
        }
    }
}