using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace UnityEngine.UI {
    [ExecuteInEditMode]
    public class SlideIcon : MonoBehaviour {
        public RectTransform anchor;
        public UIImageState[] image;
        public float p, t;
        public int n, target;
        public float duration;
        public float speed, offset;
        public AnimationCurve curve;
        
        public bool preview;
        internal bool previewA;

        public void OnValidate() {
            if (Application.isPlaying) return;
            anchor = (RectTransform)transform.Find("anchor");
            image = GetComponentsInChildren<UIImageState>();
            offset = Mathf.Abs(((RectTransform)image[0].transform).anchoredPosition.y);
        }

        public void Reset() {
            foreach(var i in image) {
                i.Select(0);
            }
            t = 0;
            p = 0;
            n = 0;
        }

        public void Preview(int target_) {
            target = target_;
            preview = true;
            previewA = true;
            Reset();
        }

        public void Update() {
            if (preview != previewA) {
                previewA = preview;
                Reset();
            }
            if (!previewA) return;
            var delta = Time.deltaTime;
            if (delta <= 0) delta = 0.2f;
            t += delta;
            var tEnd = t >= duration;
            if (tEnd && p == 0) {
                preview = false;
                previewA = false;
                return;
            }
            var s = speed * curve.Evaluate(t / duration);
            p += delta * s;
            if (p >= offset) {
                var sM = (s + speed * curve.Evaluate(1)) / 2;
                var tWillEnd = sM * (duration - t) < offset;
                var nn = (int)(p / offset);
                p %= offset;
                var ni = image[0].state.Length;
                for (var k = 0; k < image.Length; ++k) {
                    var nk = Mathf.Max(0, n + k - 1);
                    var kk = nk < ni ? nk : (1 + (nk - ni) % (ni - 1));
                    image[k].Select(kk);
                }
                n += nn;
                if (tWillEnd) {
                    image[^1].Select(target);
                }
                if (tEnd) {
                    image[1].Select(target);
                    p = 0;
                }
            }
            anchor.anchoredPosition = new(0, -p);
        }
    }
}