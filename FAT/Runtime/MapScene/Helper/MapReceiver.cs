using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.UI.Extensions;

namespace FAT {
    using static Input;

    [DisallowMultipleComponent, ExecuteInEditMode]
    public class MapReceiver : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IEventSystemHandler {
        public MapViewport view;
        public Graphic target;
        private Vector2 start;
        private Vector2 positionLast;
        private float distanceStart;
        private float distanceLast;
        private int touchCount;
        private int fingerId0;
        private int fingerId1;
        public bool Hover { get; private set; }
        public bool CancelClickOnce { get; set; } = false;

        #if UNITY_EDITOR
        private void OnValidate() {
            target = GetComponent<Graphic>();
        }
        #endif

        private void Update() => view?.Update();

        public bool InvalidInput(PointerEventData eventData)
            => eventData.button != PointerEventData.InputButton.Left || touchCount > 1;

        public void CancelClick() {
            CancelClickOnce = true;
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (CancelClickOnce || InvalidInput(eventData)) {
                CancelClickOnce = false;
                return;
            }
            view?.WhenClick();
        }

        public void OnPointerDown(PointerEventData eventData) {
            CancelClickOnce = false;
            if (InvalidInput(eventData)) return;
            view?.WhenClickDown();
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (InvalidInput(eventData)) return;
            view?.WhenClickUp();
        }

        public void OnPointerEnter(PointerEventData eventData) {
            Hover = true;
        }

        public void OnPointerExit(PointerEventData eventData) {
            Hover = false;
            CancelClick();
        }

        private bool MatchingTouch(int id) => fingerId0 == id;

        private bool MatchingTouch2(int id0, int id1) => (fingerId0 == id0 && fingerId1 == id1)
                                                    || (fingerId0 == id1 && fingerId1 == id0);

        private (bool, float) TouchDistance() {
            if (Input.touchCount < 2) return default;
            var touch0 = GetTouch(0);
            var touch1 = GetTouch(1);
            if (!MatchingTouch2(touch0.fingerId, touch1.fingerId)) {
                return default;
            }
            return (true, (touch0.position - touch1.position).magnitude);
        }

        private (bool, Vector2) TouchPosition() {
            if (Input.touchCount < 1) return default;
            var touch = Input.GetTouch(0);
            if (!MatchingTouch(touch.fingerId)) {
                return default;
            }
            return (true, touch.position);
        }

        public void OnBeginDrag(PointerEventData eventData) {
            CancelClick();
            if (!touchSupported) {
                start = eventData.position;
                view?.WhenDragBegin();
                return;
            }
            touchCount = Input.touchCount;
            switch (touchCount) {
                case >= 2: {
                    var touch0 = GetTouch(0);
                    var touch1 = GetTouch(1);
                    fingerId0 = touch0.fingerId;
                    fingerId1 = touch1.fingerId;
                    distanceLast = (touch0.position - touch1.position).magnitude;
                    distanceStart = distanceLast;
                    view?.WhenPinchBegin();
                } break;
                case 1: {
                    var touch = GetTouch(0);
                    fingerId0 = touch.fingerId;
                    start = eventData.position;
                view?.WhenDragBegin();
                } break;
            }
        }

        public void OnDrag(PointerEventData eventData) {
            if (!touchSupported) {
                view?.WhenDrag(eventData.position - start);
                return;
            }
            if (Input.touchCount != touchCount) {
                OnEndDrag(eventData);
                OnBeginDrag(eventData);
            }
            switch (touchCount) {
                case >= 2: {
                    var (valid, distance) = TouchDistance();
                    if (!valid) return;
                    view?.WhenPinch(distance - distanceStart);
                    distanceLast = distance;
                } break;
                case 1: {
                    var (valid, position) = TouchPosition();
                    if (!valid) return;
                    view?.WhenDrag(position - start);
                    positionLast = position;
                } break;
            }
        }

        public void OnEndDrag(PointerEventData eventData) {
            if (!touchSupported) {
                view?.WhenDragEnd(eventData.delta);
                return;
            }
            switch (touchCount) {
                case >= 2: {
                    var (valid, distance) = TouchDistance();
                    if (!valid) distance = distanceLast;
                    view?.WhenPinchEnd(distance - distanceLast);
                } break;
                case 1: {
                    var (valid, position) = TouchPosition();
                    if (!valid) position = positionLast;
                    view?.WhenDragEnd(position - positionLast);
                } break;
            }
        }
    }
}