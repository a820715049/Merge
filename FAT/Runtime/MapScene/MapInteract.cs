using System.Collections.Generic;
using System;
using UnityEngine;
using EL;

namespace FAT {
    public abstract class MapSelectable : MonoBehaviour {
        public int pickPriority;
        internal RaycastHit HitInfo { get; set; }
    }

    public class MapInteract {
        public Camera camera;
        private readonly RaycastHit[] raycastBuffer = new RaycastHit[16];
        private readonly List<MapSelectable> buffer = new(8);
        internal IMapBuilding selected;
        private readonly MSG.MAP_FOCUS_CHANGE msgFocus;
        public bool SkipFocusPopupOnce { get; set; }

        public MapInteract(Camera camera_) {
            camera = camera_;
            msgFocus = MessageCenter.Get<MSG.MAP_FOCUS_CHANGE>();
        }

        public void Clear() {
            selected = null;
        }

        public bool PickSceneObject<T>(List<T> buffer_) where T : MapSelectable {
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            Physics.SyncTransforms();
            var count = Physics.RaycastNonAlloc(ray, raycastBuffer, float.MaxValue);
            buffer_.Clear();
            for (var k = 0; k < count; ++k) {
                var valid = raycastBuffer[k].transform.TryGetComponent<T>(out var t);
                if (valid) {
                    t.HitInfo = raycastBuffer[k];
                    buffer_.Add(t);
                }
            }
            return buffer_.Count > 0;
        }

        private MapSelectable PrioritySelect(List<MapSelectable> list_) {
            if (list_.Count == 1) return list_[0];
            var v = int.MinValue;
            MapSelectable ret = null;
            foreach(var b in list_) {
                if (b.pickPriority > v) {
                    ret = b;
                    v = b.pickPriority;
                }
            }
            return ret;
        }

        public bool TrySelect(out MapSelectable select_) {
            if(PickSceneObject(buffer)) {
                select_ = PrioritySelect(buffer);
                buffer.Clear();
                return true;
            }
            select_ = null;
            return false;
        }

        public bool WhenClick(MapViewport view_) {
            if (!TrySelect(out var select)) {
                ClearSelect();
                return false;
            }
            // Debug.Log($"click picked {select}");
            return select switch {
                MapBuildingInfo b => Select(view_, b.buildingRef),
                _ => false,
            };
        }

        public bool Select(MapViewport view_, IMapBuilding b_, float zoom_ = -1, float duration_ = 0.8f, bool delayPopup_ = false, bool skipCheck_ = false, Action WhenPopup_ = null) {
            var same = b_ == selected;
            if (skipCheck_) {
                if (!same) ClearSelect();
                goto select;
            }
            ClearSelect();
            if (same) return true;
            if (!b_.WillSelect) return false;
        select:
            var uiMgr = UIManager.Instance;
            uiMgr.Block(true);
            void SelectPopup(IMapBuilding b_) {
                uiMgr.Block(false);
                MessageCenter.Get<MSG.MAP_FOCUS_POPUP>().Dispatch();
                if (SkipFocusPopupOnce) SkipFocusPopupOnce = false;
                else b_.WhenSelect();
                WhenPopup_?.Invoke();
            }
            if (delayPopup_) {
                view_.Focus(b_, v_: zoom_, duration_: duration_, WhenFocus_: () => SelectPopup(b_));
            }
            else {
                view_.Focus(b_, v_: zoom_, duration_: duration_);
                SelectPopup(b_);
            }
            selected = b_;
            msgFocus.Dispatch(b_);
            return true;
        }

        public void ClearSelect() {
            selected?.OpenBanner();
            selected = null;
            msgFocus.Dispatch(null);
        }

        public void ClearSelect(IMapBuilding target_) {
            if (selected != target_) return;
            selected = null;
            msgFocus.Dispatch(null);
        }
    }
}