using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.UI.Extensions;

namespace FAT {
    using static Input;

    [DisallowMultipleComponent, ExecuteInEditMode]
    public class MapBlock : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IEventSystemHandler {
        public Graphic target;

        #if UNITY_EDITOR
        private void OnValidate() {
            target = GetComponent<Graphic>();
        }
        #endif

        public void OnPointerClick(PointerEventData eventData) {

        }

        public void OnPointerDown(PointerEventData eventData) {

        }

        public void OnPointerUp(PointerEventData eventData) {

        }

        public void OnPointerEnter(PointerEventData eventData) {

        }

        public void OnPointerExit(PointerEventData eventData) {

        }

        public void OnBeginDrag(PointerEventData eventData) {

        }

        public void OnDrag(PointerEventData eventData) {

        }

        public void OnEndDrag(PointerEventData eventData) {

        }
    }
}