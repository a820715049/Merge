using System.Collections.Generic;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FAT {
    public class MapBuildingInfo : MapSelectable {
        [Serializable]
        public struct LevelInfo {
            public string name;
            public Vector3 offset;
            public Bounds bounds;
        }
        public int id;
        public int area;
        public float zoom;
        public Vector3 focus;
        public Vector3 banner;
        public Vector3 cloud;
        public List<LevelInfo> levelInfo;
        [HideInInspector] public BoxCollider select;
        internal IMapBuilding buildingRef;
        
        #if UNITY_EDITOR
        [Range(1, 10)]
        public int preview;
        public bool previewFocus;
        [HideInInspector] public MapSetting setting;
        [HideInInspector] public Transform view;
        [HideInInspector] public bool export;

        public void OnValidate() {
            if (export || Application.isPlaying) return;
            name = $"building_{id}";
            var anchor = transform.Find("anchor");
            focus = anchor.Find("_focus").position;
            banner = anchor.Find("_banner").position;
            cloud = anchor.Find("_cloud").position;
            levelInfo ??= new();
            view = transform.Find("view");
            var count = view.childCount;
            if (count == 0) {
                preview = 1;
                return;
            }
            if (count > 0 && preview > count) {
                preview = count;
            }
            var target = preview - 1;
            for (var n = 0; n < count; ++n) {
                var c = view.GetChild(n);
                c.gameObject.SetActive(n == target);
            }
            if (select == null && !TryGetComponent(out select)) {
                select = gameObject.AddComponent<BoxCollider>();
            }
        }

        public void Refresh() {
            OnValidate();
            levelInfo.Clear();
            var pos = transform.position;
            var bounds = new Bounds(pos, Vector3.zero);
            var rList = new List<Renderer>();
            foreach(Transform level in view) {
                var name = level.name;
                if (name.StartsWith('_')) continue;
                var lBounds = new Bounds(level.position, Vector3.zero);
                rList.Clear();
                level.GetComponentsInChildren(rList);
                foreach(var r in rList) {
                    bounds.Encapsulate(r.bounds);
                    lBounds.Encapsulate(r.bounds);
                }
                levelInfo.Add(new() {
                    name = name,
                    offset = level.localPosition,
                    bounds = lBounds,
                });
            }
            select.isTrigger = true;
            select.center = bounds.center - pos;
            var size = bounds.extents * 2;
            size.z = 0f;
            select.size = size;
        }

        public void OnDrawGizmos() {
            if (!previewFocus || setting == null || setting.cameraRef == null) return;
            var y = zoom <= 0 ? setting.zoomFocus : zoom;
            var ratio = setting.cameraRef.aspect;
            var x = y * ratio;
            var c = focus;
            var sX = new Vector3(x, 0, 0);
            var sY = new Vector3(0, y, 0);
            MapSetting.DrawRect(c, sX, sY);
        }
        #endif
    }
}