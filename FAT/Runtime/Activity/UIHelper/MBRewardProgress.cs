using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace FAT
{
    public class MBRewardProgress : MonoBehaviour
    {
        internal TextMeshProUGUI text;
        internal RectMask2D bar;
        internal RectTransform back;
        internal RectTransform fore;
        internal float sizeX;

        private void Awake() => Init();

        public void Init()
        {
            if (bar == null)
            {
                transform.Access("text", out text, try_: true);
                transform.Access("mask", out bar);
                transform.Access("back", out back);
                bar.Access("fore", out fore);
            }
            sizeX = back.rect.width;
            var rect = bar.rectTransform;
            rect.anchorMax = new(0, rect.anchorMax.y);
            fore.anchorMax = new(0, 1);
            fore.sizeDelta = new(sizeX, fore.sizeDelta.y);
        }

        public void Refresh(int v, int t, float duration_ = 0, System.Action onComplete = null)
        {
            var p = Mathf.Clamp01((float)v / t);
            TryRefreshP(p, $"{v}/{t}", duration_, onComplete);
        }

        public void RefreshSegment(int v, int t, int o_, float duration_ = 0, float showNum = 0)
        {
            var p = Mathf.Clamp01((float)(v - o_) / (t - o_));
            TryRefreshP(p, $"{v - showNum}/{t - showNum}", duration_);
        }

        public void RefreshSize(float size_, float duration_ = 0)
        {
            size_ = Mathf.Clamp(size_, 0, sizeX);
            TryRefreshS(size_, null, duration_);
        }

        private void TryRefreshP(float p_, string text_, float duration_, System.Action onComplete = null)
            => TryRefreshS(p_ * sizeX, text_, duration_, onComplete);
        private void TryRefreshS(float s_, string text_, float duration_, System.Action onComplete = null)
        {
            Init();
            var rect = bar.rectTransform;
            var v = new Vector2(s_, rect.sizeDelta.y);
            void R()
            {
                rect.sizeDelta = v;
                if (text != null && text_ != null) text.text = text_;
                onComplete?.Invoke();
            }
            if (duration_ == 0) R();
            else rect.DOSizeDelta(v, duration_).OnComplete(R);
        }

        /// <summary>
        /// 刷新进度条并播放Text递增动画
        /// 自动从当前文本状态获取起始值
        /// </summary>
        public void RefreshWithTextAnimation(int endValue, int targetValue, float duration, System.Action onComplete = null)
        {
            // 从当前文本状态获取起始值
            int startValue = 0;
            if (text != null && !string.IsNullOrEmpty(text.text))
            {
                var textParts = text.text.Split('/');
                if (textParts.Length >= 1 && int.TryParse(textParts[0], out int parsedValue))
                {
                    startValue = parsedValue;
                }
            }

            var p = Mathf.Clamp01((float)endValue / targetValue);

            // 复用现有逻辑，text_传空，让进度条动画使用原有逻辑
            TryRefreshP(p, null, duration, onComplete);

            // 同步播放Text递增动画
            if (text != null)
            {
                DOTween.To(() => startValue, x =>
                {
                    text.text = $"{x}/{targetValue}";
                }, endValue, duration)
                .SetEase(Ease.OutCubic);
            }
        }
    }
}
