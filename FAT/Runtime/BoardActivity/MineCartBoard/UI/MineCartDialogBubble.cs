using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EL;
using DG.Tweening;

namespace FAT
{
    /// <summary>
    /// 矿车气泡对话框，复杂的排序和自动大小都集中在Prefab的各种Layout里
    /// 这个Prefab结构可以作为变体通用
    /// 根节点的size决定文本框最大大小
    /// Text节点的LayoutElement里的MinSize决定最小大小
    /// 具体细节参考MineCart_s001内的引用
    /// </summary>
    public class MineCartDialogBubble : MonoBehaviour
    {
        [SerializeField] private TMP_Text idleText;                 // 待机文本

        [Header("动画配置")]
        [SerializeField] private float scaleTime = 0.3f;            // 缩放动画时间
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // 缩放曲线

        private bool isVisible = false;
        private bool isAnimating = false;
        private bool isShowing = false;  // 明确区分显示和隐藏动画
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            // 初始化时隐藏气泡
            gameObject.SetActive(false);
            rectTransform.localScale = Vector3.zero;
        }

        /// <summary>
        /// 显示待机气泡（带动画）
        /// </summary>
        public void Show(string key)
        {
            // 防止重复调用
            if (ShouldSkipShow()) return;

            // 设置文本
            if (!string.IsNullOrEmpty(key) && idleText != null)
            {
                idleText.gameObject.SetActive(true);
                idleText.text = I18N.Text(key);
            }

            // 播放显示动画
            PlayShowAnimation();
        }

        /// <summary>
        /// 隐藏气泡（带动画）
        /// </summary>
        public void Hide()
        {
            Debug.Log($"Hide() 被调用，当前状态: isVisible={isVisible}, isAnimating={isAnimating}, isShowing={isShowing}");

            // 防止重复调用
            if (ShouldSkipHide())
            {
                Debug.Log("Hide() 被跳过");
                return;
            }

            Debug.Log("开始播放隐藏动画");
            // 播放隐藏动画
            PlayHideAnimation();
        }

        /// <summary>
        /// 检查是否应该跳过显示调用
        /// </summary>
        private bool ShouldSkipShow()
        {
            return (isAnimating && isShowing) || (isVisible && !isAnimating);
        }

        /// <summary>
        /// 检查是否应该跳过隐藏调用
        /// </summary>
        private bool ShouldSkipHide()
        {
            return (isAnimating && !isShowing) || (!isVisible && !isAnimating);
        }

        /// <summary>
        /// 播放显示动画
        /// </summary>
        private void PlayShowAnimation()
        {
            if (rectTransform == null) return;

            SetAnimationState(true, true);
            rectTransform.DOKill();

            gameObject.SetActive(true);
            rectTransform.localScale = Vector3.zero;

            rectTransform.DOScale(Vector3.one, scaleTime)
                .SetEase(scaleCurve)
                .OnComplete(() =>
                {
                    SetAnimationState(false, false);
                    isVisible = true;  // 设置显示状态
                });
        }

        /// <summary>
        /// 播放隐藏动画
        /// </summary>
        private void PlayHideAnimation()
        {
            if (rectTransform == null) return;

            SetAnimationState(true, false);
            rectTransform.DOKill();

            rectTransform.DOScale(Vector3.zero, scaleTime)
                .SetEase(scaleCurve)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    SetAnimationState(false, false);
                    isVisible = false;
                });
        }

        /// <summary>
        /// 设置动画状态
        /// </summary>
        private void SetAnimationState(bool animating, bool showing)
        {
            isAnimating = animating;
            isShowing = showing;
        }

        #region 公共属性
        /// <summary>
        /// 是否正在显示
        /// </summary>
        public bool IsVisible => isVisible;

        /// <summary>
        /// 是否正在播放动画
        /// </summary>
        public bool IsAnimating => isAnimating;

        /// <summary>
        /// 是否正在播放显示动画
        /// </summary>
        public bool IsShowing => isShowing;

        /// <summary>
        /// 是否正在播放隐藏动画
        /// </summary>
        public bool IsHiding => isAnimating && !isShowing;
        #endregion
    }
}
