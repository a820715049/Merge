using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening;

namespace FAT {
    public class MapViewport {
        public MapInteract interact;
        public Camera camera;
        public Canvas canvas;
        public MapSetting setting;
        public MapReceiver control;
        public Transform scaleRoot;
        public Vector2 CameraPos {
            get => camera.transform.position;
            set => camera.transform.position = new(value.x, value.y, cameraZ);
        }
        private Vector2 inputPos;
        private readonly float cameraZ = -1;
        private float xBorderMin, xBorderMax, yBorderMin, yBorderMax;
        private float xMin, xMax, yMin, yMax;
        private float dragInertiaValue;
        private Vector2 dragInertiaDelta;
        public float Zoom {
            get => camera.orthographicSize;
            set => camera.orthographicSize = value;
        }
        private float inputZoom;
        private float zoomTarget;
        private float zoomStart;
        private float zoomRange;
        private float zoomDuration;
        private float zoomTime;
        private float zoomSpeed;
        private bool zoomFree;
        public float ZoomMin => setting.cameraMin * scaleFactor;
        public float ZoomMax => setting.cameraMax * scaleFactor;
        private float zoomInertiaValue;
        private float zoomInertiaSign;
        private bool limitUnstable;
        private float diffX, diffY, diffXSign, diffYSign;
        private float borderX, borderY;
        private float centerX, centerY;
        private bool centerOnX, centerOnY;
        private float elasticity;
        private int screenHeight;
        private int screenWidth;
        private Vector2 screenOffset;
        private float baseRatioXY;
        private float ratioXY;
        private float scaleFactor = 1f;
        private float heightInverse;
        private float unitPerPixel;
        private float distS;

        public MapViewport(Camera camera_, MapInteract interact_) {
            camera = camera_;
            interact = interact_;
        }

        public void SetupCamera(MapSetting setting_) {
            CheckScreenSize();
            SetupAs(setting_);
        }

        public void SetupAs(MapSetting setting_) {
            setting = setting_;
            setting.cameraRef = camera;
            CameraPos = setting.cameraPos;
            var center = setting.transform.position;
            xBorderMin = center.x - setting.extent.x;
            xBorderMax = center.x + setting.extent.x;
            yBorderMin = center.y - setting.extent.y;
            yBorderMax = center.y + setting.extent.y;
            zoomRange = ZoomMax - ZoomMin;
            ZoomTo(setting.cameraRest);
            elasticity = Mathf.Clamp01(1 / (1 + setting.elasticity));
        }

        public void SetupCanvas(Canvas canvas_, Transform root_) {
            canvas = canvas_;
            scaleRoot = root_;
            ScaleCanvas();
        }

        public void ScaleCanvas() {
            if (canvas == null) return;
            var ratioScale = ratioXY / baseRatioXY;
            var rV = Mathf.Clamp(ratioScale, 0.8f, 1.2f);
            scaleRoot.localScale = Vector3.one * rV;
        }

        public bool CheckScreenSize() {
            if (screenWidth == Screen.width && screenHeight == Screen.height)
                return false;
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            screenOffset = new(screenWidth / 2f, screenHeight / 2f);
            ratioXY = (float)screenWidth / screenHeight;
            heightInverse = 1f / screenHeight;
            //根据目前适配方式计算缩放因子
            var matchValue = UIManager.Instance.GetCanvasMatchValue();
            var refRes = UIManager.Instance.GetCanvasRefRes();
            baseRatioXY = refRes.x / refRes.y;
            if (matchValue < 1)
            {
                scaleFactor = baseRatioXY / ratioXY;
            }
            else
            {
                var baseRatioYX = refRes.y / refRes.x;
                var ratioYX = (float)screenHeight / screenWidth;
                scaleFactor = baseRatioYX / ratioYX;
            }
            ScaleCanvas();
            return true;
        }

        public void SetupControl(MapReceiver control_) {
            control = control_;
            control.view = this;
            control.gameObject.SetActive(true);
        }

        internal void WhenClick() {
            if(interact.WhenClick(this)) return;
        }
        
        internal void WhenClickDown() {
            //nothing
        }

        internal void WhenClickUp() {
            //nothing
        }

        internal void WhenDrag(Vector2 offset_) {
            var pos = inputPos - offset_ * unitPerPixel;
            MoveValue(pos);
        }

        internal void WhenDragBegin() {
            inputPos = CameraPos;
            dragInertiaValue = 0;
        }

        internal void WhenDragEnd(Vector2 delta_) {
            delta_ *= 2f;
            dragInertiaValue = delta_.magnitude;
            dragInertiaDelta = -delta_ / dragInertiaValue;
        }

        internal void WhenPinch(float diff_) {
            var zoom = inputZoom - 2 * diff_ * setting.zoomStep * unitPerPixel;
            SpringZoomTo(zoom, setting.zoomTransitTime);
        }

        internal void WhenPinchBegin() {
            inputZoom = Zoom;
        }

        internal void WhenPinchEnd(float delta_) {
            InertiaZoom(-delta_ * 0.2f);
        }

        private void DragInertia() {
            if (dragInertiaValue > setting.dragInertiaThreshold) {
                dragInertiaValue *= setting.dragInertiaDamping;
                var inertia = dragInertiaValue * unitPerPixel * dragInertiaDelta;
                var pos = CameraPos + inertia;
                MoveValue(pos);
            }
        }

        private void MoveValue(Vector2 pos_) {
            CameraPos = LimitElastic(pos_.x, pos_.y);
        }

        public void MoveTo(Vector2 pos_, float duration_ = 0, TweenCallback WhenFocus_ = null) {
            if (duration_ == 0) {
                CameraPos = pos_;
                WhenFocus_?.Invoke();
                return;
            }
            var pos = (Vector3)pos_;
            pos.z = cameraZ;
            DOTween.Kill(camera);
            camera.transform.DOMove(pos, duration_).OnComplete(WhenFocus_).SetId(camera);
        }

        private void CalculateLimit() {
            var vY = Zoom;
            var vX = vY * ratioXY;
            xMin = xBorderMin + vX;
            xMax = xBorderMax - vX;
            yMin = yBorderMin + vY;
            yMax = yBorderMax - vY;
            centerX = (xMin + xMax) * 0.5f;
            centerY = (yMin + yMax) * 0.5f;
            centerOnX = vX >= setting.extent.x;
            centerOnY = vY >= setting.extent.y;
        }

        private float ElasticForceFactor(float diff) {
            var percent = diff / setting.extend;
            return elasticity * Mathf.Pow(percent, 1.5f - percent);
        }

        public Vector2 LimitElastic(float x, float y) {
            limitUnstable = false;
            borderX = x switch {
                _ when x < xMin => xMin,
                _ when x > xMax => xMax,
                _ => x
            };
            borderY = y switch {
                _ when y < yMin => yMin,
                _ when y > yMax => yMax,
                _ => y
            };
            if (borderX == x && borderY == y) {
                limitUnstable = false;
                return new(x, y);
            }
            limitUnstable = true;
            diffX = x - borderX;
            diffY = y - borderY;
            diffXSign = Mathf.Sign(diffX);
            diffYSign = Mathf.Sign(diffY);
            diffX = Mathf.Min(diffX * diffXSign, setting.extend);
            diffY = Mathf.Min(diffY * diffYSign, setting.extend);
            return DiffLimit();
        }

        private Vector2 LimitComposite(float x, float y) {
            if (centerOnX) x = centerX;
            if (centerOnY) y = centerY;
            return new(x, y);
        }

        private Vector2 DiffLimit() {
            var extendX = ElasticForceFactor(diffX) * setting.extend;
            var extendY = ElasticForceFactor(diffY) * setting.extend;
            return LimitComposite(
                borderX + extendX * diffXSign,
                borderY + extendY * diffYSign);
        }

        private Vector2 ElasticPull() {
            if (!limitUnstable) return CameraPos;
            var pull = (1 - elasticity) * setting.extend;
            diffX = Mathf.Max(diffX - 0.02f - pull * ElasticForceFactor(diffX), 0f);
            diffY = Mathf.Max(diffY - 0.02f - pull * ElasticForceFactor(diffY), 0f);
            if (diffX <= 0 && diffY <= 0) {
                limitUnstable = false;
                return LimitComposite(borderX, borderY);
            }
            return DiffLimit();
        }

        private void LimitElasticPull() {
            if (limitUnstable && !Input.anyKey && Input.touchCount == 0) {
                CameraPos = ElasticPull();
            }
        }

        private void ZoomValue(float v_) {
            //若种种原因导致产生了无效值 则不设置zoom
            if (float.IsNaN(v_) || float.IsInfinity(v_))
                return;
            Zoom = v_;
            unitPerPixel = v_ * heightInverse * 2f;
            CalculateLimit();
            if (!zoomFree) MoveValue(CameraPos);
        }

        public float TargetZoom(float v_, bool free_, bool needAdapt) {
            zoomFree = free_;
            var result = needAdapt ? v_ * scaleFactor : v_;
            if (!free_) result = Mathf.Clamp(result, ZoomMin, ZoomMax);
            zoomTarget = result;
            return result;
        }

        private void InertiaZoom(float delta) {
            zoomInertiaSign = Mathf.Sign(delta);
            zoomInertiaValue = delta * zoomInertiaSign;
        }

        private void SpringZoomTo(float v, float duration, bool free_ = false) {
            zoomStart = Zoom;
            v = TargetZoom(v, free_, false);
            zoomTime = 0;
            zoomDuration = duration;
            if (duration == 0) ZoomValue(v);
            else ZoomSpeed(v);
        }

        private void ZoomSpeed(float v) {
            zoomSpeed = (v - zoomStart) / zoomDuration;
        }

        public void ZoomTo(float v, float duration = 0, bool free_ = false) {
            v = TargetZoom(v, free_, true);
            zoomStart = Zoom;
            zoomDuration = duration;
            zoomTime = duration;
            if (duration == 0) ZoomValue(v);
        }

        private void ZoomTransit(float deltaTime) {
            if (zoomInertiaValue > setting.zoomInertiaThreshold) {
                zoomInertiaValue *= setting.zoomInertiaDamping;
                var inertia = zoomInertiaValue * zoomInertiaSign;
                var v = TargetZoom(zoomTarget + inertia, false, false);
                ZoomSpeed(v);
            }
            if (zoomTarget != Zoom) {
                if (zoomTime > 0) {
                    zoomTime -= deltaTime;
                    if (zoomTime <= 0) {
                        ZoomValue(zoomTarget);
                    }
                    else {
                        var zoom = Mathf.SmoothStep(zoomTarget, zoomStart, zoomTime / zoomDuration);
                        ZoomValue(zoom);
                    }
                }
                else {
                    var zoom = Zoom + zoomSpeed * deltaTime;
                    if (zoom < zoomTarget != Zoom < zoomTarget) {
                        ZoomValue(zoomTarget);
                    }
                    else {
                        ZoomValue(zoom);
                    }
                }
            }
        }

        private void ZoomByMouse() {
            if (!control.Hover) return;
            var deltaY = Input.mouseScrollDelta.y;
            if (deltaY != 0) {
                // 限制 scroll 值 防止跳变过大
                deltaY = Mathf.Clamp(deltaY, -50f, 50f);
                var delta = deltaY * setting.zoomScrollStep;
                WhenPinchBegin();
                WhenPinch(delta * zoomRange);
                WhenPinchEnd(delta * 0.5f);
            }
            var key = KeyCode.LeftControl;
            if (Input.GetKeyDown(key)) {
                distS = ((Vector2)Input.mousePosition - screenOffset).magnitude;
                WhenPinchBegin();
            }
            else if (Input.GetKey(key)) {
                var dist = ((Vector2)Input.mousePosition - screenOffset).magnitude;
                var offset = dist - distS;
                WhenPinch(offset);
            }
            else if (Input.GetKeyUp(key)) {
                WhenPinchEnd(0);
            }
        }

        internal void Update() {
            var deltaTime = Time.deltaTime;
            if (CheckScreenSize()) {
                ZoomValue(Zoom);
            }
            DragInertia();
            LimitElasticPull();
            ZoomTransit(deltaTime);
            #if UNITY_EDITOR || UNITY_STANDALONE    
            ZoomByMouse();
            #endif
        }

        public void Focus(Vector3 p_, float v_ = -1, float v1_ = 0, float duration_ = 0.8f, float speed_ = 0f, TweenCallback WhenFocus_ = null, bool free_ = false) {
            if (speed_ > 0) {
                var dist = (CameraPos - (Vector2)p_).magnitude;
                duration_ = Mathf.Min(duration_, dist / speed_);
            }
            MoveTo(p_, duration_, WhenFocus_);
            if (v_ <= 0) v_ = setting.zoomFocus;
            ZoomTo(v_ + v1_, duration_, free_:free_);
        }
        public void Focus(IMapBuilding b_, float v_ = -1, float v1_ = 0, float duration_ = 0.8f, float speed_ = 0f, TweenCallback WhenFocus_ = null)
            => Focus(b_.info.focus, v_ < 0 ? b_.info.zoom : v_, v1_, duration_, speed_, WhenFocus_, free_:true);

        public Vector3 SceneToCanvasWorld(Vector3 pos_) {
            var sPos = (Vector2)camera.WorldToScreenPoint(pos_);
            // var cPos = sPos - screenOffset;
            var canvas = UIManager.Instance.CanvasRoot;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, sPos, null, out var lp);
            var wPos = canvas.TransformPoint(lp);
            return wPos;
        }
    }
}