/*
 * @Author: qun.chao
 * @Date: 2022-03-07 12:33:05
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EL;

namespace FAT
{
    public class MBBoardInventoryEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Animator animator;
        private GameObject RedPoint;

        public void Setup()
        {
            animator = transform.GetComponent<Animator>();
            RedPoint = transform.Find("RedPoint").gameObject;
            transform.AddButton(null, _OnBtnInventory).FixPivot();
        }

        public void InitOnPreOpen()
        {
            _RefreshEntry();
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().AddListener(_RefreshEntry);
            MessageCenter.Get<MSG.UI_INVENTORY_ENTRY_FEEDBACK>().AddListener(_OnMessageInventoryFeedback);
            MessageCenter.Get<MSG.UI_INVENTORY_REFRESH_RED_POINT>().AddListener(_RefreshRedPoint);
        }

        public void CleanupOnPostClose()
        {
            MessageCenter.Get<MSG.GAME_FEATURE_STATUS_CHANGE>().RemoveListener(_RefreshEntry);
            MessageCenter.Get<MSG.UI_INVENTORY_ENTRY_FEEDBACK>().RemoveListener(_OnMessageInventoryFeedback);
            MessageCenter.Get<MSG.UI_INVENTORY_REFRESH_RED_POINT>().RemoveListener(_RefreshRedPoint);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            // if (!BoardViewManager.Instance.IsItemCanPutInInventory())
            //     return;
            // BoardViewManager.Instance.SetInventoryEntry(true);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            // BoardViewManager.Instance.SetInventoryEntry(false);
        }
        
        private void _RefreshEntry()
        {
            bool isUnlock = Game.Manager.bagMan.CheckBagUnlock(BagMan.BagType.Item, BagMan.BagType.Producer, BagMan.BagType.Tool);
            gameObject.SetActive(isUnlock);
            if (isUnlock)
                _RefreshInventoryEntryScreenPos();
        }

        private void _RefreshInventoryEntryScreenPos()
        {
            var sp = GuideUtility.CalcRectTransformScreenPos(null, transform);
            BoardViewManager.Instance.RefreshInventoryEntryScreenPos(sp);
            _RefreshRedPoint();
        }

        private void _RefreshRedPoint()
        {
            RedPoint.SetActive(Game.Manager.mainMergeMan.world.inventory.GetBagByType(BagMan.BagType.Producer).NeedRedPoint());
        }
        
        private void _OnBtnInventory()
        {
            Game.Manager.bagMan.TryOpenUIBag();
        }

        private void _OnMessageInventoryFeedback()
        {
            if (animator != null)
            {
                animator.SetTrigger("Punch");
            }
        }
    }
}