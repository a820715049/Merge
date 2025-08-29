/**
 * @Author: zhangpengjian
 * @Date: 2025/4/11 10:53:33
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/4/11 10:53:33
 * Description: 钓鱼奖励tips
 */

using System.Collections.Generic;
using Config;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIFishRewardTips : UITipsBase
    {
        [SerializeField] private Transform itemRoot;
        [SerializeField] private Vector2 fallbackSize;
        [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;
        [SerializeField] private HorizontalLayoutGroup horizontalLayoutGroup;
        [SerializeField] private float extraWidth; // 额外宽度 用于在右侧屏幕预留空间 避免贴边

        protected override void OnCreate()
        {
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                var item = itemRoot.GetChild(i);
                item.GetComponent<UICommonItem>().Setup();
            }
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 2) _SetTipsPosInfo(items);
            if (items.Length >= 3) _RefreshReward(items[2] as List<RewardConfig>);
        }

        protected override void OnPreOpen()
        {
            _SetCurExtraWidth(extraWidth);
            _SetCurTipsHeight(_GetTipAreaHeight());
            _RefreshTipsPos(20f);
        }

        private float _GetTipAreaHeight()
        {
            var panel = transform.GetChild(0) as RectTransform;
            if (panel.rect.height <= 0f) return fallbackSize.y;
            return panel.rect.height;
        }

        private void _RefreshReward(List<RewardConfig> rewardConfigs)
        {
            var cellWidth = 0f;
            var showCellNum = 0; //记录最终显示的cell个数
            if (rewardConfigs == null) return;
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                var item = itemRoot.GetChild(i);
                if (i >= rewardConfigs.Count)
                {
                    item.gameObject.SetActive(false);
                }
                else
                {
                    var rect = item.gameObject.transform as RectTransform;
                    cellWidth = rect.rect.width;
                    showCellNum++;
                    item.gameObject.SetActive(true);
                    item.GetComponent<UICommonItem>().Refresh(rewardConfigs[i]);
                }
            }

            var paddingH = horizontalLayoutGroup.padding;
            var paddingV = verticalLayoutGroup.padding;
            var finalWidth = paddingV.left + paddingV.right + paddingH.left + paddingH.right + showCellNum * cellWidth +
                             horizontalLayoutGroup.spacing * (showCellNum - 1);
            _SetCurTipsWidth(finalWidth);
        }
    }
}