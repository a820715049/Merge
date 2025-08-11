using System;

namespace UnityEngine.UI {
    public class UIRectState : OptionState {
        [Serializable]
        public struct State {
            public Vector2 sizeV;
            public bool size;
            public Vector2 posV;
            public bool pos;
            public bool hide;
        }

        public override int Count => state.Length;
        public RectTransform target;
        public State[] state;

        #if UNITY_EDITOR

        private void OnValidate() {
            if (Application.isPlaying) return;
            if (state == null || state.Length == 0) {
                state = new State[2];
            }
            if (target != null || TryGetComponent(out target)) {
                state[0].posV = target.anchoredPosition;
                state[0].sizeV = target.sizeDelta;
            }
        }

        #endif

        public override void Select(int i_) => Setup(i_);
        public void Setup(int i_) {
            var s = state[i_];
            gameObject.SetActive(!s.hide);
            if (s.hide) return;
            if (s.pos) target.anchoredPosition = s.posV;
            if (s.size) target.sizeDelta = s.sizeV;
        }

        public void Enabled(bool v_) {
            var i = v_ ? 0 : 1;
            Setup(i);
        }
    }
}