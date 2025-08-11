using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace UnityEngine.UI {
    public class UIColorGroup : MonoBehaviour {
        [Serializable]
        public struct ColorPair {
            public Graphic target;
            public Color color;
        }

        public static List<Graphic> cache;
        public List<ColorPair> list = new();
        public List<Graphic> exclude = new();
        public Color[] color;
        [Range(0, 1)]
        public float mix;
        private float mixV;
        public int active = -1;

        #if UNITY_EDITOR
        public bool collect;

        public void OnValidate() {
            if (collect) {
                Collect();
                collect = false;
            }
            if (Application.isPlaying) return;
            for (var k = 0; k < list.Count; ++k) {
                if (list[k].target == null) {
                    list.RemoveAt(k);
                    --k;
                }
            }
        }
        #endif

        public void Update() {
            if (active >= 0 && mixV != mix) {
                Mix(mix);
            }
        }

        public void Collect() {
            list.Clear();
            cache ??= new();
            GetComponentsInChildren(cache);
            for (var k = 0; k < cache.Count; ++k) {
                var c = cache[k];
                if (exclude.Contains(c)) continue;
                list.Add(new() { target = c, color = c.color });
            }
            cache.Clear();
        }

        public void Select(int i_) {
            if (color.Length == 0) return;
            active = i_;
            mixV = mix = 0;
            if (i_ < 0) Revert();
            else Refresh(color[i_]);
        }

        public void Refresh(Color color_) {
            for (var k = 0; k < list.Count; ++k) {
                list[k].target.color = color_;
            }
        }

        public void MixTo(int k_, float v_) {
            active = k_;
            mix = v_;
            Mix(v_);
        }
        public void MixTo(Color color_, float v_) {
            active = -1;
            mix = v_;
            Mix(color_, v_);
        }

        private void Mix(float v_) {
            mixV = v_;
            Mix(color[active], v_);
        }
        private void Mix(Color color_, float v_) {
            mixV = mix;
            for (var k = 0; k < list.Count; ++k) {
                var n = list[k];
                n.target.color = Color.Lerp(n.color, color_, v_);
            }
        }

        public void Revert() {
            active = -1;
            for (var k = 0; k < list.Count; ++k) {
                var e = list[k];
                e.target.color = e.color;
            }
        }
    }
}