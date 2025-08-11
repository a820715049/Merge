using UnityEngine;
using UnityEngine.UI;
using System;
using EL;
using Spine.Unity;

namespace UnityEngine.UI
{
    public class UIPreviewControl : MonoBehaviour
    {
        [Serializable]
        public struct Extra {
            public SkeletonGraphic spine;
            public string[] state;
        }

        public GameObject[] node;
        public SkeletonGraphic spine;
        public string[] spineState;
        public Extra[] spineList;

        public void Enabled(bool v_)
        {
            foreach (var n in node) {
                n.SetActive(v_);
            }
            var state = v_ ? 0 : 1;
            var anim = spineState[state];
            spine.AnimationState.SetAnimation(0, anim, true);
            foreach(var e in spineList) {
                if (e.spine == null) continue;
                var anim1 = anim;
                if (e.state != null && e.state.Length > state) anim1 = e.state[state];
                e.spine.AnimationState.SetAnimation(0, anim1, true);
            }
        }
    }
}