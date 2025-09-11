/**
 * @Author: zhangpengjian
 * @Date: 2025/8/11 17:03:27
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/8/11 17:03:50
 * @Description: 拼图活动 里程碑样式
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class PuzzleMilestoneCell : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private UIImageRes image;
        [SerializeField] private Button btn;
        [SerializeField] private TextMeshProUGUI num;
        [SerializeField] private Transform finish;
        [SerializeField] private Animator anim;

        private ActivityPuzzle _activity;
        private int _idx;
        
        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            btn.onClick.AddListener(OnClick);
        }

        public void SetFinish()
        {
            anim.SetTrigger("Punch");
        }

        public void SetUnFinish()
        {
            anim.enabled = true;
            anim.SetTrigger("Num");
        }

        public void SetData(ActivityPuzzle activity, int milestoneValue, int idx)
        {
            _activity = activity;
            _idx = idx;
            // 根据里程碑值设置礼盒在进度条上的位置
            SetPosition(activity, milestoneValue);
            var reward = fat.conf.EventPuzzleRewardsVisitor.GetOneByFilter(x => x.Id == _activity.confD.RewardId[_idx].ConvertToInt());
            image.SetImage(reward.Image);
            num.text = milestoneValue.ToString();
            finish.gameObject.SetActive(milestoneValue <= activity.PuzzleProgress);
            num.gameObject.SetActive(milestoneValue > activity.PuzzleProgress);
            if (milestoneValue <= activity.PuzzleProgress)
            {
                anim.enabled = false;
            }
            else
            {
                SetUnFinish();
            }
        }

        private void OnClick()
        {
            var reward = fat.conf.EventPuzzleRewardsVisitor.GetOneByFilter(x => x.Id == _activity.confD.RewardId[_idx].ConvertToInt());
            UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips,
                    image.transform.position,
                    image.transform.GetComponent<RectTransform>().rect.size.y * 0.5f,
                    reward.RewardId);
        }
        
        private void SetPosition(ActivityPuzzle activity, int milestoneValue)
        {
            // 总进度为16，根据里程碑值计算位置比例
            float progressRatio = (float)milestoneValue / activity.MaxProgress;
            
            // 设置礼盒在进度条上的位置
            if (rectTransform != null)
            {
                // 获取父容器（进度条）的宽度
                var parentRect = transform.parent as RectTransform;
                if (parentRect != null)
                {
                    float progressBarWidth = parentRect.rect.width;
                    float targetX = progressRatio * progressBarWidth;
                    
                    // 设置礼盒位置，使其相对于进度条定位
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(0, 0.5f);
                    rectTransform.anchoredPosition = new Vector2(targetX, 0);
                }
                else
                {
                    // 备用方案：使用锚点定位
                    rectTransform.anchorMin = new Vector2(progressRatio, 0.5f);
                    rectTransform.anchorMax = new Vector2(progressRatio, 0.5f);
                    rectTransform.anchoredPosition = Vector2.zero;
                }
            }
        }
    }
}