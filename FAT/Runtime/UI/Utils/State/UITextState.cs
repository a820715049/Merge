using UnityEngine;
using UnityEngine.UI;
using System;
using EL;
using TMPro;

namespace UnityEngine.UI
{
    public class UITextState : OptionState
    {
        [Serializable]
        public struct State
        {
            public bool hide;
            public string text;
            public Color color;
            public Material mat;
            public int style;
            public int size;
        }

        public override int Count => state.Length;
        public TextMeshProUGUI text;
        public State[] state;

        public string Text {
            get => text.text;
            set => text.text = value;
        }

        #if UNITY_EDITOR

        private void OnValidate()
        {
            static int S(ReadOnlySpan<char> n_) {
                var p = n_.LastIndexOf('_');
                n_ = n_[(p + 1)..];
                if (int.TryParse(n_, out var v)) return v;
                n_ = n_[..^1];
                if (int.TryParse(n_, out v)) return v;
                return -1;
            }
            if (Application.isPlaying) return;
            if (state == null || state.Length == 0)
            {
                state = new State[2];
                state[1].color = Color.white;
            }
            if (text != null || TryGetComponent(out text))
            {
                state[0].color = text.color;
                var style = S(text.fontSharedMaterial.name);
                if (style < 0) state[0].mat = text.fontSharedMaterial;
                else state[0].style = style;
            }
        }

        #endif

        public override void Select(int i_) => Setup(i_);
        public void Setup(int i_, string text_ = null)
        {
            var s = state[i_];
            text.gameObject.SetActive(!s.hide);
            if (s.hide) return;
            var mat = text.fontSharedMaterial;
            if (s.mat != null && s.mat != mat) text.fontSharedMaterial = s.mat;
            if (s.size > 0) {
                text.fontSizeMax = s.size;
                text.fontSize = s.size;
            }
            text.color = s.color;
            if (s.style > 0) {
                var matR = FontMaterialRes.Instance.GetFontMatResConf(s.style);
                matR.ApplyFontMatResConfig(text);
            }
            if (text_ != null) Text = text_;
            else if (!string.IsNullOrEmpty(s.text)) {
                Text = s.text.StartsWith('#') ? I18N.Text(s.text) : s.text;
            }
        }

        public void Enabled(bool v_, string text_ = null)
        {
            var i = v_ ? 0 : 1;
            Setup(i, text_);
        }
    }
}