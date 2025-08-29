using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    /// <summary>
    /// 矿车奖励气泡组件
    /// 负责奖励气泡的显示和位置更新
    /// </summary>
    public class MineCartRewardBubble : MonoBehaviour
    {
        [SerializeField] private UIImageRes rewardImageRes;

        private const float NORMAL_SCALE = 0.88f;
        private const float FINAL_SCALE = 1f;

        private RectTransform rectTransform;
        private float targetDistance;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rewardImageRes == null)
            {
                rewardImageRes = GetComponentInChildren<UIImageRes>();
            }
        }

        public void Init(float distance, string icon, bool isFinal = false)
        {
            gameObject.SetActive(true);
            if (rewardImageRes != null)
            {
                rewardImageRes.SetImage(icon);
            }
            targetDistance = distance;
            // 设置缩放
            transform.localScale = Vector3.one * (isFinal ? FINAL_SCALE : NORMAL_SCALE);
        }

        public void UpdatePosition(RectTransform cartRect, float currentDistance)
        {
            float remainingDistance = targetDistance - currentDistance;
            Vector2 rewardPos = Vector2.zero;
            rewardPos.x = cartRect.anchoredPosition.x + remainingDistance;
            rewardPos.y = rectTransform.anchoredPosition.y;
            rectTransform.anchoredPosition = rewardPos;
        }

        public void DestroyBubble()
        {
            Destroy(gameObject);
        }
        public RectTransform RectTransform => rectTransform;
    }
}
