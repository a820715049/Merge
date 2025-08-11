/*
 * @Author: qun.chao
 * @Date: 2022-04-12 18:29:24
 */
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBImageFillFitInSafeArea : MonoBehaviour
    {
        [SerializeField] private bool safeTop;
        [SerializeField] private bool safeBottom;

        private (int width, int height) screenSize;

        // private void Awake()
        // {
        //     Debug.LogFormat("screen awake width {0}, height {1}", Screen.width, Screen.height);
        // }

        // https://issuetracker.unity3d.com/issues/screen-dot-width-and-screen-dot-height-values-in-onenable-function-are-incorrect
        private void Start()
        {
            // 只能在此处得到正确的screenSize
            screenSize = (Screen.width, Screen.height);
            // Debug.LogFormat("screen start width {0}, height {1}", Screen.width, Screen.height);
        }

        private void OnEnable()
        {
            _RefreshSafeArea();
            _RefreshFillFit();
        }

        /// <summary>
        /// 安全区设置 截取自UIManager
        /// </summary>
        private void _RefreshSafeArea()
        {
            var trans = transform as RectTransform;

            var area = Screen.safeArea;
            Vector2 anchorMin = area.position;
            Vector2 anchorMax = area.position + area.size;

            // Debug.LogFormat("screen width {0}, height {1}", Screen.width, Screen.height);

            anchorMin.x /= screenSize.width;
            anchorMin.y /= screenSize.height;
            anchorMax.x /= screenSize.width;
            anchorMax.y /= screenSize.height;

            // Fix for some Samsung devices (e.g. Note 10+, A71, S20) where Refresh gets called twice and the first time returns NaN anchor coordinates
            // See https://forum.unity.com/threads/569236/page-2#post-6199352
            if (anchorMin.x >= 0 && anchorMin.y >= 0 && anchorMax.x >= 0 && anchorMax.y >= 0)
            {
                if (safeBottom) trans.anchorMin = anchorMin;
                else trans.anchorMin = Vector2.zero;
                if (safeTop) trans.anchorMax = anchorMax;
                else trans.anchorMax = Vector2.one;
            }
        }

        private void _RefreshFillFit()
        {
            var tex = transform.GetComponent<RawImage>();
            if (tex == null)
                return;
            var trans = transform as RectTransform;
            // var aspect = 1.0f * tex.texture.width / tex.texture.height;
            var aspect = 1.0f * 386 / 600;  // 原图比例暂时无从得知 写死 386 * 600
            var w = trans.rect.width;
            var h = trans.rect.height;
            if (w / h > aspect)
            {
                var tarH = w / aspect;
                tex.uvRect = new Rect(0f, (tarH - h) * 0.5f / tarH, 1f, h / tarH);
            }
            else
            {
                var tarW = h * aspect;
                tex.uvRect = new Rect((tarW - w) * 0.5f / tarW, 0f, w / tarW, 1f);
            }
        }
    }
}