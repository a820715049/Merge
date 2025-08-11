/*
 * @Author: qun.chao
 * @Date: 2022-12-19 15:58:05
 */
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIGuideRectMask : MonoBehaviour
    {
        [SerializeField] private RectTransform mask;
        [SerializeField] private RectTransform fill;
        private Transform mTarget;
        private float curScale = 1f;

        public void Setup()
        { }

        public void InitOnPreOpen()
        {
            Hide();
        }

        public void CleanupOnPostClose()
        {
            Hide();
        }

        void Update()
        {
            if (mTarget == null)
            {
                mask.gameObject.SetActive(false);
                fill.gameObject.SetActive(false);
            }
            else
            {
                if (mTarget.hasChanged)
                {
                    mTarget.hasChanged = false;
                    Show(mTarget, curScale);
                }
            }
        }

        public void Show(Transform target, float size = 1f)
        {
            mTarget = target;
            curScale = size;
            fill.SetParent(mask.parent);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = Vector2.zero;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;

            var tarTrans = target as RectTransform;
            mask.position = tarTrans.position;
            mask.pivot = tarTrans.pivot;
            mask.sizeDelta = tarTrans.sizeDelta;
            mask.localScale = curScale * Vector3.one;

            fill.SetParent(mask, true);

            mask.gameObject.SetActive(true);
            fill.gameObject.SetActive(true);
        }

        public void Hide()
        {
            mask.gameObject.SetActive(false);
            fill.gameObject.SetActive(false);
            mTarget = null;
            curScale = 1f;
        }

    }
}