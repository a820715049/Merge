/*
 * @Author: qun.chao
 * @Date: 2023-11-23 18:52:57
 */
using UnityEngine;
using UnityEngine.EventSystems;

namespace FAT
{
    public class UIGuideDrag : MonoBehaviour, /*IPointerClickHandler,*/ IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private IEventSystemHandler redirector;

        public void Setup()
        { }

        public void InitOnPreOpen()
        {
            Hide();
        }

        public void CleanupOnPostClose()
        {
            Hide();
        }

        // void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        // {
        //     EL.DebugEx.Info("click on drag board");
        // }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.pointerId > 0) return;
            if (redirector is IBeginDragHandler handler)
            {
                handler?.OnBeginDrag(eventData);
            }
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId > 0) return;
            if (redirector is IDragHandler handler)
            {
                handler?.OnDrag(eventData);
            }
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId > 0) return;
            if (redirector is IEndDragHandler handler)
            {
                handler?.OnEndDrag(eventData);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            redirector = null;
        }

        public void SetRedirector(IEventSystemHandler handler)
        {
            redirector = handler;
            gameObject.SetActive(handler != null);
        }
    }
}