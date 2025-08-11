using System.Collections.Generic;
using System;
using UnityEngine;
using EL;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FAT {
    using static GUILayout;

    public class MapSceneInfo : MonoBehaviour {
        public MapSetting setting;
        public DecorateSetting decorate;
        public Canvas canvas;
        public MapReceiver control;
        public Transform root;
        public AssetGroup assetEnv;
        public List<MapAreaInfo> areaList;
        public List<MapBuildingInfo> buildingList;
        public List<MapBuildingInfo> buildingForEvent;
        public AssetInfo templateBanner;

#if UNITY_EDITOR
        public bool skipInactive;
        [HideInInspector] public bool export;

        public void OnValidate() {
            if (export || Application.isPlaying) return;
            setting = GetComponentInChildren<MapSetting>();
            decorate = GetComponentInChildren<DecorateSetting>();
            canvas = transform.FindEx<Canvas>("_canvas");
            control = transform.FindEx<MapReceiver>("_canvas/_control");
            root = transform.Find("_canvas/_control/root");
            areaList ??= new();
            areaList ??= new();
            buildingList ??= new();
            buildingForEvent ??= new();
            SyncCanvas();
        }

        public void SyncCanvas() {
            canvas.transform.position = setting.transform.position;
            var root = (RectTransform)canvas.transform;
            var scale = root.localScale.x;
            root.sizeDelta = setting.extent * 2 / scale;
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MapSceneInfo))]
    public class MapSceneInfoEdit : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            var info = (MapSceneInfo)target;
            if (info.export) return;
            BeginHorizontal();
            if (Button("Refresh", Width(72), Height(36))) {
                Refresh();
            }
            if (Button("Export", Width(72), Height(36))) {
                Export();
            }
            //if (Button("Apply10X", Width(72), Height(36))) {
            //    Apply10X();
            //}
            EndHorizontal();
        }

        public void Refresh() {
            static int ExtractId(string name_) {
                var n = name_.AsSpan();
                var s = n.IndexOf('_');
                if (s <= 0) return 0;
                n = n[(s+1)..];
                s = n.IndexOf('_');
                if (s > 0) n = n[..s];
                int.TryParse(n, out var v);
                return v;
            }
            var info = (MapSceneInfo)target;
            info.OnValidate();
            info.setting.CheckCamera();
            info.areaList.Clear();
            info.buildingList.Clear();
            info.buildingForEvent.Clear();
            foreach(Transform area in info.transform) {
                var areaName = area.name;
                if (areaName == "_env") {
                    info.assetEnv = AssetGroup(area);
                    continue;
                }
                if (areaName.StartsWith('_')) continue;
                var areaId = ExtractId(areaName);
                var viewRoot = area.Find("ground");
                if (!area.TryGetComponent(out MapAreaInfo a)) a = area.gameObject.AddComponent<MapAreaInfo>();
                a.id = areaId;
                a.flex = areaId >= 10000;
                a.assetGround = AssetGroup(viewRoot);
                var anchor = area.Find("anchor");
                if (anchor != null) {
                    var focus = anchor.Find("_focus");
                    if (focus != null) {
                        a.center = focus.position;
                        if (focus.TryGetComponent<Collider2D>(out var c)) {
                            var b = c.bounds;
                            a.center = b.center;
                            a.zoom = b.extents.y;
                        }
                    }
                    var overview = anchor.Find("_overview");
                    if (overview != null && overview.TryGetComponent<MapSetting>(out var s)) {
                        a.overview = s;
                        s.cameraPos = overview.position;
                        s.cameraRest = a.zoom;
                        s.cameraMax = Mathf.Max(a.zoom, s.cameraMax);
                    }
                    a.cloud = AssetOf(anchor.Find("_cloud"), 0);
                }
                info.areaList.Add(a);
                void Collect(Transform root, List<MapBuildingInfo> list) {
                    if (root == null) return;
                    foreach(Transform building in root) {
                        if (info.skipInactive && !building.gameObject.activeSelf) continue;
                        var buildingName = building.name;
                        if (buildingName.StartsWith('_')) continue;
                        if(!building.TryGetComponent<MapBuildingInfo>(out var bInfo)) {
                            bInfo = building.gameObject.AddComponent<MapBuildingInfo>();
                        }
                        bInfo.area = areaId;
                        bInfo.setting = info.setting;
                        bInfo.Refresh();
                        list.Add(bInfo);
                    }
                }
                Collect(area.Find("building"), info.buildingList);
                Collect(area.Find("event"), info.buildingForEvent);
            }
            EditorUtility.SetDirty(this);
        }

        public void Export() {
            GameObject copy = null;
            try {
                Refresh();
                var info = (MapSceneInfo)target;
                copy = GameObject.Instantiate(info.gameObject);
                var infoC = copy.GetComponent<MapSceneInfo>();
                infoC.control.gameObject.SetActive(true);
                infoC.export = true;
                ExportG(ref infoC.assetEnv);
                RecordA(ref infoC.templateBanner);
                var areaList = infoC.areaList;
                for (var k = 0; k < areaList.Count; ++k) {
                    var area = areaList[k];
                    ExportA(ref area.cloud);
                    ExportG(ref area.assetGround);
                    areaList[k] = area;
                }
                var root = copy.transform;
                for (var n = 0; n < root.childCount; ++n) {
                    var c = root.GetChild(n);
                    if (c.name.StartsWith("__")) {
                        GameObject.DestroyImmediate(c.gameObject);
                        --n;
                    }
                }
                static void Export(MapBuildingInfo b) {
                    b.Refresh();
                    b.export = true;
                    var root = b.transform;
                    for(var n = 0; n < root.childCount; ++n) {
                        var c = root.GetChild(n);
                        if (!c.name.StartsWith("_")) {
                            GameObject.DestroyImmediate(c.gameObject);
                            --n;
                        }
                    }
                }
                foreach(var b in infoC.buildingList) {
                    Export(b);
                }
                foreach(var b in infoC.buildingForEvent) {
                    Export(b);
                    b.gameObject.GetComponent<Collider>().enabled = false;
                }
                PrefabUtility.SaveAsPrefabAsset(copy, $"Assets/Bundle/map/bundle_scene/{info.name}.prefab");
            }
            catch(Exception e) {
                Debug.LogError($"scene export failed: {e}");
            }
            finally {
                if (copy != null) GameObject.DestroyImmediate(copy);
            }
        }

        public static AssetGroup AssetGroup(Transform tr_) {
            if (tr_ == null) return default;
            var list = new List<AssetInfo>();
            foreach(Transform c in tr_) {
                list.Add(AssetOf(c));
            }
            return new AssetGroup() { root = tr_, list = list };
        }

        public static AssetInfo AssetOf(Transform tr_, int i_) {
            if (tr_ == null) return default;
            if (i_ < 0 || i_ >= tr_.childCount) {
                Debug.LogError($"child index out of range {i_} {tr_.childCount}");
                return default;
            }
            var t = tr_.GetChild(i_);
            return AssetOf(t);
        }

        public static AssetInfo AssetOf(Transform tr_) {
            if (tr_ == null) return default;
            if (!PrefabUtility.IsPartOfAnyPrefab(tr_)) {
                Debug.LogError($"asset {tr_.name} is not prefab");
                return default;
            }
            if (!PrefabUtility.IsPartOfPrefabAsset(tr_) && !PrefabUtility.IsAnyPrefabInstanceRoot(tr_.gameObject)) {
                Debug.LogError($"asset {tr_.name} is not root of prefab");
                return default;
            }
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(tr_);
            var bundle = AssetDatabase.GetImplicitAssetBundleName(path).Replace(".ab", string.Empty);
            var sp = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
            if (sp <= 0) {
                Debug.LogError($"failed to parse asset name from {path}");
                return default;
            }
            var asset = path[(sp + 1)..];
            return new() {
                name = $"{bundle}:{asset}",
                bundle = bundle, asset = asset, obj = tr_.gameObject,
            };
        }

        public static void ExportG(ref AssetGroup g_) {
            var list = g_.list;
            for (var k = 0; k < list.Count; ++k) {
                var a = list[k];
                if (!a.Ready) {
                    list.RemoveAt(k);
                    --k;
                    continue;
                }
                ExportA(ref a);
                list[k] = a;
            }
        }

        public static void ExportA(ref AssetInfo a_) {
            if (!a_.Ready) return;
            var tr = a_.obj.transform;
            if (PrefabUtility.HasPrefabInstanceAnyOverrides(tr.gameObject, false)) {
                Debug.LogWarning($"asset {tr.name} has unapplied override");
            }
            var o = new GameObject(a_.name);
            a_.wrap = o;
            var r = o.transform;
            r.SetParent(tr.parent, false);
            r.SetLocalPositionAndRotation(tr.localPosition, tr.localRotation);
            r.localScale = tr.localScale;
            GameObject.DestroyImmediate(a_.obj);
            a_.obj = null;
        }

        public static void RecordA(ref AssetInfo a_) {
            if (!a_.Ready) return;
            a_ = AssetOf(a_.obj.transform);
            a_.obj = null;
        }
    }
#endif

}