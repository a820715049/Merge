/*
 * @Author: qun.chao
 * @Description: 寻宝活动进度奖励tips
 * @Date: 2024-04-17 11:21:58
 */
using System.Collections;
using UnityEngine;
using EL;
using UnityEngine.UI;

namespace FAT
{
    public class UITreasureHuntRewardTips : UITipsBase
    {
        [SerializeField] private Transform itemRoot;
        [SerializeField] private Vector2 fallbackSize;
        [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;
        [SerializeField] private HorizontalLayoutGroup horizontalLayoutGroup;
        [SerializeField] private float extraWidth;  // 额外宽度 用于在右侧屏幕预留空间 避免贴边
        private ActivityTreasure eventInst;

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
            if (items.Length >= 2)
            {
                _SetTipsPosInfo(items);
            }
        }

        protected override void OnPreOpen()
        {
            UITreasureHuntUtility.TryGetEventInst(out eventInst);
            if (eventInst == null)
            {
                DebugEx.Error($"[treasurehunt] activity inst not found");
                itemRoot.gameObject.SetActive(false);
            }
            else
            {
                itemRoot.gameObject.SetActive(true);
                _RefreshReward();
            }
            _SetCurExtraWidth(extraWidth);
            _SetCurTipsHeight(_GetTipAreaHeight());
            _RefreshTipsPos(20f);
        }

        private float _GetTipAreaHeight()
        {
            var panel = transform.GetChild(0) as RectTransform;
            if (panel.rect.height <= 0f)
            {
                return fallbackSize.y;
            }
            return panel.rect.height;
        }

        private void _RefreshReward()
        {
            var cellWidth = 0f;
            int showCellNum = 0;  //记录最终显示的cell个数
            var cfg = eventInst.GetCurrentTreasureGroup();
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                var item = itemRoot.GetChild(i);
                if (i >= cfg.MilestoneReward.Count)
                {
                    item.gameObject.SetActive(false);
                }
                else
                {
                    var rect = item.gameObject.transform as RectTransform;
                    cellWidth = rect.rect.width;
                    showCellNum++;
                    item.gameObject.SetActive(true);
                    item.GetComponent<UICommonItem>().Refresh(cfg.MilestoneReward[i].ConvertToRewardConfig());
                }
            }
            var paddingH = horizontalLayoutGroup.padding;
            var paddingV = verticalLayoutGroup.padding;
            var finalWidth = paddingV.left + paddingV.right + paddingH.left + paddingH.right + showCellNum * cellWidth + horizontalLayoutGroup.spacing * (showCellNum - 1);
            _SetCurTipsWidth(finalWidth);
        }
    }
}