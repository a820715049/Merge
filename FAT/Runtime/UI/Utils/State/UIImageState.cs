using UnityEngine;
using UnityEngine.UI;
using System;
using EL;

namespace UnityEngine.UI
{
    public class UIImageState : OptionState
    {
        [Serializable]
        public struct State
        {
            public bool enabled;
            public bool raycast;
            public Sprite sprite;
            public Color color;
            public Material material;
            public string key;
        }

        public override int Count => state.Length;
        public Image image;
        public UIImageRes res;
        public State[] state;

        #if UNITY_EDITOR
        public int preview;
        internal int previewL;

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            if (state == null || state.Length == 0)
            {
                state = new State[2];
                state[1].enabled = false;
                state[1].color = new Color(0.4f, 0.4f, 0.4f, 1f);
            }
            if (preview != previewL) {
                preview = Mathf.Clamp(preview, 0, state.Length - 1);
                Select(preview);
                previewL = preview;
            }
            if (preview <= 0 && (image != null || TryGetComponent(out image))) {
                ref var state0 = ref state[0];
                state0.enabled = image.enabled;
                state0.raycast = image.raycastTarget;
                state0.sprite = image.sprite;
                state0.color = image.color;
            }
            if (res == null) TryGetComponent(out res);
        }

        #endif

        public override void Select(int i_) => Setup(i_);
        public void Setup(int i_)
        {
            var s = state[i_];
            image.enabled = s.enabled;
            image.raycastTarget = s.raycast;
            if (!s.enabled) return;
            image.color = s.color;
            if (image.material != s.material) image.material = s.material;
            if (res != null) {
                if (!string.IsNullOrEmpty(s.key)) res.SetImage(s.key);
                else if (s.sprite != null) res.SetImage(s.sprite);
            }
            else if (s.sprite != null) {
                image.sprite = s.sprite;
            }
        }

        public void Enabled(bool v_)
        {
            var i = v_ ? 0 : 1;
            Setup(i);
        }

        public void SelectLoop(int i_) => Setup(i_ % state.Length);
    }
}