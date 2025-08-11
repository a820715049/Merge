using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace UnityEngine.UI {
    public abstract class OptionState : MonoBehaviour {
        [HideInInspector]
        public OptionState parent;
        public abstract int Count { get; }
        public int groupFlag;
        public abstract void Select(int v_);
        public virtual void SelectNear(int v_) => Select(Math.Clamp(v_, 0, Count - 1));
    }

    public class UIStateGroup : MonoBehaviour {
        public static List<OptionState> cache;
        public List<OptionState> state = new();
        public int groupFlag;
        public bool strict;

        #if UNITY_EDITOR
        public bool collect;

        public void OnValidate() {
            if (collect) {
                Collect();
                collect = false;
            }
            if (Application.isPlaying) return;
            for (var k = 0; k < state.Count; ++k) {
                if (state[k] == null) {
                    state.RemoveAt(k);
                    --k;
                }
            }
        }
        #endif

        public void Collect() {
            state.Clear();
            cache ??= new();
            GetComponentsInChildren(cache);
            for (var k = 0; k < cache.Count; ++k) {
                var c = cache[k];
                var g = c.groupFlag;
                if (c.parent != null || g switch {
                    > 0 => (g & groupFlag) == 0,
                    _ => strict,
                }) continue;
                state.Add(cache[k]);
            }
        }

        public void Select(int i_) {
            for (var k = 0; k < state.Count; ++k) {
                state[k].Select(i_);
            }
        }

        public void Enabled(bool v_) {
            var i = v_ ? 0 : 1;
            Select(i);
        }

        public void SelectNear(int i_) {
            for (var k = 0; k < state.Count; ++k) {
                state[k].SelectNear(i_);
            }
        }
    }
}