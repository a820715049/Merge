// ================================================
// File: UIActivityFishMilestone.cs
// Author: yueran.li
// Date: 2025/04/03 18:11:31 星期四
// Desc: 钓鱼棋盘进度界面
// ================================================


using System.Collections.Generic;
using Config;
using EL;
using FAT.MSG;
using UnityEngine;
using UnityEngine.UI;
using static fat.conf.Data;

namespace FAT
{
    public class UIActivityFishMilestone : UIBase
    {
        // UI
        public GameObject milestoneItem;
        private Transform itemRoot;
        private ScrollRect scrollRect;

        // 活动实例
        private ActivityFishing activityFish;

        private List<GameObject> itemList = new();

        public class MilestoneItemData
        {
            public int index; // 索引
            public RewardConfig[] reward; // 显示奖励
        }

        #region UI
        protected override void OnCreate()
        {
            itemRoot = transform.Find("Content/Scroll View/Viewport/Content");
            transform.Access("Content/Scroll View", out scrollRect);
            transform.AddButton("Content/close", _ClickConfirm);

            GameObjectPoolManager.Instance.PreparePool(PoolItemType.FISH_BOARD_MILESTONE_ITEM, milestoneItem);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;
            activityFish = (ActivityFishing)items[0];
        }

        protected override void OnPreOpen()
        {
            RefreshList();

            // 滚动到当前里程碑位置
            ScrollToCurrentMilestone(activityFish.MilestoneIdx);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        protected override void OnPostClose()
        {
            // 里程碑Item 释放
            foreach (var item in itemList)
            {
                item.GetComponent<UIFishMilestoneItem>().ReleaseOnPostClose();
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.FISH_BOARD_MILESTONE_ITEM, item);
            }

            itemList.Clear();
        }
        #endregion

        #region Listener
        private void _ClickConfirm()
        {
            Close();
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != activityFish || !expire) return;
            Close();
        }
        #endregion

        private void RefreshList()
        {
            if (activityFish == null) return;

            var milestones = activityFish.ConfDetail.Milestones;

            // 根据配置数 初始化item
            for (var i = 0; i < milestones.Count; i++)
            {
                int milestoneId = milestones[i];
                var item = GameObjectPoolManager.Instance.CreateObject(PoolItemType.FISH_BOARD_MILESTONE_ITEM,
                    itemRoot.transform);

                // 获得配置奖励
                var mileStone = GetEventFishMilestone(milestoneId);
                var reward = new RewardConfig[mileStone.Reward.Count];
                for (var j = 0; j < mileStone.Reward.Count; j++)
                {
                    reward[j] = mileStone.Reward[j].ConvertToRewardConfig();
                }

                // 根据配置构建数据
                var itemData = new MilestoneItemData()
                {
                    index = milestoneId,
                    reward = reward
                };

                item.gameObject.SetActive(true);
                item.GetComponent<UIFishMilestoneItem>().InitOnPreOpen(itemData);
                itemList.Add(item);
            }
        }

        /// <summary>
        /// 滚动到当前里程碑位置
        /// </summary>
        /// <param name="milestoneIdx">当前里程碑索引</param>
        private void ScrollToCurrentMilestone(int milestoneIdx)
        {
            if (milestoneIdx < 0 || milestoneIdx >= itemList.Count || scrollRect == null)
                return;

            // 确保布局已刷新
            Canvas.ForceUpdateCanvases();

            // 获取目标里程碑项的RectTransform
            var targetItem = itemList[milestoneIdx].GetComponent<RectTransform>();
            var content = scrollRect.content;
            var viewport = scrollRect.viewport;

            // 计算视口和内容的实际高度
            var viewportHeight = viewport.rect.height;
            var contentHeight = content.rect.height;

            // 计算目标元素在content中的位置（顶部位置）
            var targetPosition = -(targetItem.localPosition.y - targetItem.rect.height * targetItem.pivot.y);

            // 目标位置应在视口中居中
            var targetCenter = targetPosition - viewportHeight * 0.5f + targetItem.rect.height * 0.5f;

            // 确保不超出边界（顶部和底部限制）
            var normalizedPosition = Mathf.Clamp01(targetCenter / (contentHeight - viewportHeight));

            // 设置滚动位置（方法1：使用normalizedPosition）
            scrollRect.verticalNormalizedPosition = 1 - normalizedPosition;
        }
    }
}