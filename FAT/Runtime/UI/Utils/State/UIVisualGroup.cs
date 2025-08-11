using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.UI {
    using static GUILayout;
    using static UIVisualGroup;

    public class UIVisualGroup : MonoBehaviour {
        [Serializable]
        public struct VisualPair {
            public readonly (Component, string, int) EditKey => (target, key, index);
            #if UNITY_EDITOR
            public string name;
            public string conf;
            #endif
            public Component target;
            public string key;
            public int index;
        }

        #if UNITY_EDITOR
        [Serializable]
        public struct KeyMapping {
            public string name;
            public string from;
            public string to;
        }
        #endif

        public static List<Component> cache;
        public static Dictionary<(Component, string, int), VisualPair> map;
        public List<VisualPair> list = new();

        [HideInInspector]
        public bool filter = true;
        [HideInInspector]
        public bool tRes = true;
        [HideInInspector]
        public bool tImage = false;
        [HideInInspector]
        public bool tText = true;
        [HideInInspector]
        public bool tState = false;

        #if UNITY_EDITOR
        public List<KeyMapping> mapping = new();
        
        public void OnValidate() {
            if (Application.isPlaying) return;
            for (var k = 0; k < list.Count; ++k) {
                var n = list[k];
                if (!string.IsNullOrEmpty(n.key) && n.target == null) {
                    list.RemoveAt(k);
                    --k;
                    continue;
                }
                var nn = Name(n.target, n.key, n.index);
                if (nn != n.name) {
                    n.name = nn;
                    list[k] = n;
                    EditorUtility.SetDirty(this);
                }
            }
            for (var k = 0; k < mapping.Count; ++k) {
                var n = mapping[k];
                var nn = $"{n.from}:{n.to}";
                if (nn != n.name) {
                    n.name = nn;
                    mapping[k] = n;
                    EditorUtility.SetDirty(this);
                }
            }
        }
        #endif

        private string Name(Component c_, string v_, int i_) {
            var t = c_.GetType();
            var ss = IsState(t) || i_ > 0 ? $"|{i_}" : string.Empty;
            return $"{v_}:{c_.name}({t.Name}{ss})";
        }

        public void Prepare(Component t_, string k_, int i_ = 0) {
            if (t_ is not MonoBehaviour m || string.IsNullOrEmpty(k_)) return;
            if (!m.enabled ) {
                Debug.LogWarning($"trying to prepare visual for disabled component: {m}", this);
                return;
            }
            if (filter && Filter(m)) {
                Debug.LogError($"trying to prepare visual for filtered component: {m}", this);
                return;
            }
            map ??= new();
            map[(t_, k_, i_)] = new() { target = t_, key = k_, index = i_ };
        }

        public void CollectTrim(bool check_ = true) {
            if (check_ && list.Count == map.Count) {
                map ??= new();
                var change = list.Count <= 0 && map.Count > 0;
                for (var k = 0; k < list.Count; ++k) {
                    var n = list[k];
                    if (!map.ContainsKey(n.EditKey) || string.IsNullOrEmpty(n.key)) {
                        change = true;
                        break;
                    }
                }
                if (!change) return;
            }
            Collect();
            Trim();
        }

        public void Collect() {
            cache ??= new();
            map ??= new();
            for (var k = 0; k < list.Count; ++k) {
                var n = list[k];
                map[n.EditKey] = n;
            }
            list.Clear();
            GetComponentsInChildren(true, cache);
            for (var k = 0; k < cache.Count; ++k) {
                var c = cache[k];
                if (c is not MonoBehaviour m || !m.enabled || (filter && Filter(m))) continue;
                foreach(var vv in map.Where(o => o.Value.target == m)) {
                    var v = vv.Value;
                    #if UNITY_EDITOR
                    v.name = Name(c, v.key, v.index);
                    #endif
                    list.Add(v);
                }
            }
            cache.Clear();
            map.Clear();
        }

        public void Add(Component t_, string k_, int i_ = 0) {
            list.Add(new() { target = t_, key = k_, index = i_ });
        }

        private bool Filter(MonoBehaviour m_) {
            var type = m_.GetType();
            return !(tImage && IsImage(type))
                && !(tRes && IsRes(type))
                && !(tText && IsText(type))
                && !(tState && IsState(type));
        }

        public static bool IsImage(Type type_) => typeof(Image).IsAssignableFrom(type_);
        public static bool IsRes(Type type_) => typeof(UIImageRes).IsAssignableFrom(type_);
        public static bool IsText(Type type_) => type_.IsSubclassOf(typeof(TMP_Text)) || typeof(TextProOnACurve).IsAssignableFrom(type_);
        public static bool IsState(Type type_) => type_.IsSubclassOf(typeof(OptionState));

        public void Trim() {
            for (var k = 0; k < list.Count; ++k) {
                var n = list[k];
                if (string.IsNullOrEmpty(n.key)) {
                    list.RemoveAt(k);
                    --k;
                }
            }
        }

        public bool TryAccess(string k_, out VisualPair v_) {
            for (var k = 0; k < list.Count; ++k) {
                var n = list[k];
                if (n.key == k_) {
                    v_ = n;
                    return true;
                }
            }
            v_ = default;
            return false;
        }

        public void Clear() {
            list.Clear();
        }
    }

    #if UNITY_EDITOR

    [CustomEditor(typeof(UIVisualGroup))]
    public class UIVisualGroupEditor : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            var t = (UIVisualGroup)target;
            EditorGUI.BeginChangeCheck();
            BeginHorizontal();
            if (Button("Collect")) t.Collect();
            if (Button("Trim")) t.Trim();
            if (Button("Refresh")) t.CollectTrim(check_:false);
            EndHorizontal();
            t.filter = Toggle(t.filter, "Filter");
            if (t.filter) {
                BeginHorizontal();
                t.tImage = Toggle(t.tImage, "img");
                t.tRes = Toggle(t.tRes, "res");
                t.tText = Toggle(t.tText, "text");
                t.tState = Toggle(t.tState, "state");
                EndHorizontal();
            }
            if (Button("ThemeInfo")) ExportTheme(t);
            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(t);
            }
        }

        private string Key(UIVisualGroup t_, string v_) {
            if (string.IsNullOrEmpty(v_)) return null;
            var map = t_.mapping;
            for(var k = 0; k < map.Count; ++k) {
                var n = map[k];
                if (n.from == v_) return n.to;
            }
            return null;
        }

        private string Extract(string v_) {
            if (string.IsNullOrEmpty(v_) || v_.Length < 3) return v_;
            var v = v_.AsSpan();
            if (v[0] == '{') v = v[1..];
            if (v[^1] == '}') v = v[..^1];
            return v.Length == v_.Length ? v_ : v.ToString();
        }

        public void ExportTheme(UIVisualGroup t_) {
            static bool E(HashSet<string> m_, string v_) {
                if (m_.Contains(v_)) return true;
                m_.Add(v_);
                return false;
            }
            var list = t_.list;
            var bA = new StringBuilder();
            var bT = new StringBuilder();
            var bS = new StringBuilder();
            var mA = new HashSet<string>();
            var mT = new HashSet<string>();
            var mS = new HashSet<string>();
            void Text(VisualPair n, TMP_Text text) {
                if (!E(mT, n.key)) {
                    var vT = Key(t_, n.key) ?? Extract(text.text);
                    bT.Append(n.key).Append(':').Append(vT).Append(',');
                }
                if (!E(mS, n.key)) {
                    var mat = text.fontSharedMaterial.name.AsSpan();
                    var p = mat.LastIndexOf('_');
                    if (int.TryParse(mat[(p+1)..], out var mi)
                    || int.TryParse(mat[(p+1)..^1], out mi)) {
                        bS.Append(n.key).Append(':').Append(mi).Append(',');
                    }
                    else {
                        var cs = ColorUtility.ToHtmlStringRGB(text.color);
                        bS.Append(n.key).Append(':').Append('#').Append(cs).Append(',');
                    }
                }
            }
            for(var k = 0; k < list.Count; ++k) {
                var n = list[k];
                switch (n.target) {
                    case Image img: {
                        if (E(mA, n.key)) break;
                        var cs = ColorUtility.ToHtmlStringRGB(img.color);
                        bS.Append(n.key).Append(':').Append('#').Append(cs).Append(',');
                    } break;
                    case TMP_Text text: Text(n, text); break;
                    case UITextState text: Text(n, text.text); break;
                    case TextProOnACircle text: Text(n, text.m_TextComponent); break;
                    case UIImageRes res: {
                        if (E(mA, n.key)) break;
                        if (!res.TryGetComponent<Image>(out var tar)) continue;
                        if (tar.sprite == null) {
                            var vS = Key(t_, n.conf) ?? "<none>";
                            bA.Append(n.key).Append(':').Append(vS).Append(',');
                            continue;
                        }
                        var path = AssetDatabase.GetAssetPath(tar.sprite);
                        var pathS = path.AsSpan();
                        var p = pathS.LastIndexOfAny('/', '\\');
                        var file = path[(p+1)..];
                        var bundle = AssetDatabase.GetImplicitAssetBundleName(path);
                        bA.Append(n.key).Append(':').Append(bundle[..^3]).Append('#').Append(file).Append(',');
                    } break;
                };
            }
            if (bA.Length > 0) --bA.Length;
            if (bT.Length > 0) --bT.Length;
            if (bS.Length > 0) --bS.Length;
            Debug.Log($"{target.name}.prefab\n{bA}\n{bT}\n{bS}");
        }
    }

    #endif
}