using UnityEngine;
using BezierSolution;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

namespace FAT
{
    public class BezierTweenCtrl : MonoBehaviour
    {
        public BezierSpline spline;
        public bool is2D = true;

        [SerializeField] private float _value = 0;

        public float value
        {
            get => _value;
            set
            {
                _value = value;
                if (spline != null)
                {
                    var pt = spline.GetPoint(_value);
                    if (is2D)
                    {
                        pt.z = 0;
                    }

                    transform.position = pt;
                }
            }
        }

        public TweenerCore<float, float, FloatOptions> DOValue(float to, float duration)
        {
            return DOTween.To(() => value, (v) => value = v, to, duration);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            value = _value;
        }
#endif
    }
}