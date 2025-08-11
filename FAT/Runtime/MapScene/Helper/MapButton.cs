using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using System;
using EL;

namespace FAT {
    [DisallowMultipleComponent, ExecuteInEditMode]
    public sealed class MapButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, ISubmitHandler, IEventSystemHandler {
        public static float ResponseInterval = 0.35f;
        public static float PressIntervalMax = 1.6f;
        private float lastResponseTime;
        private float pressTime;

        public UIImageState image;
        public UITextState text;
        public Action WhenClick { get; set; } = () => {};
        public Action WhenClickDown { get; set; } = () => {};
        public Action WhenClickUp { get; set; } = () => {};
        public Action<bool> WhenHover { get; set; } = v => {};

        public bool CancelClickOnce { get; set; } = false;

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;
            image ??= GetComponent<UIImageState>() ?? GetComponentInChildren<UIImageState>();
            text ??= GetComponentInChildren<UITextState>() ?? transform.parent?.Access<UITextState>($"{name}_text", try_:true);
        }
        #endif

        public void State(bool v_, string text_ = null) {
            image?.Enabled(v_);
            text?.Enabled(v_, text_);
        }

        public bool InvalidInput(PointerEventData eventData)
            => eventData.button != PointerEventData.InputButton.Left || Input.touchCount > 1;

        public void Click() {
            WhenClick?.Invoke();
        }

        public void CancelClick() {
            CancelClickOnce = true;
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (CancelClickOnce || InvalidInput(eventData)) {
                CancelClickOnce = false;
                return;
            }
            if (Time.realtimeSinceStartup - lastResponseTime < ResponseInterval
             || Time.realtimeSinceStartup - pressTime > PressIntervalMax) {
                return;
            }
            lastResponseTime = Time.realtimeSinceStartup;
            WhenClick?.Invoke();
        }

        public void SetPointerDown() {
            CancelClickOnce = false;
            WhenPointerDown();
        }

        public void OnPointerDown(PointerEventData eventData) {
            CancelClickOnce = false;
            if (InvalidInput(eventData))
                return;
            WhenPointerDown();
        }

        private void WhenPointerDown() {
            pressTime = Time.realtimeSinceStartup;
            WhenClickDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (InvalidInput(eventData))
                return;
            WhenClickUp?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData) => WhenHover?.Invoke(true);

        //this is called when enter play mode, see StandaloneInputModule implementation
        public void OnPointerExit(PointerEventData eventData) {
            CancelClick();
            WhenHover?.Invoke(false);
        }

        public void OnBeginDrag(PointerEventData eventData) {
            CancelClick();
        }

        public void OnSubmit(BaseEventData eventData) => WhenClick?.Invoke();
    }
}