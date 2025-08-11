// ================================================
// File: UIFishMilestoneItem.cs
// Author: yueran.li
// Date: 2025/04/03 15:01:13 星期四
// Desc: 钓鱼里程碑内部item
// ================================================


using System.Collections.Generic;
using Config;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIFishMilestoneItem : MonoBehaviour
    {
        // UI
        private TextMeshProUGUI ranktext;
        public List<UICommonItem> itemList;

        private List<GameObject> rewardList = new();

        public void InitOnPreOpen(UIActivityFishMilestone.MilestoneItemData data)
        {
            transform.Access("Rank/RankText", out ranktext);
            ranktext.SetText($"{data.index}");

            ReleaseItems();
            for (var i = 0; i < data.reward.Length; i++)
            {
                SetupItem(itemList[i], data.reward[i]);
            }
        }

        public void ReleaseOnPostClose()
        {
            ReleaseItems();
            rewardList.Clear();
        }

        private void ReleaseItems()
        {
            foreach (var item in itemList)
            {
                ReleaseItem(item);
            }
        }

        private void ReleaseItem(UICommonItem item)
        {
            item.Clear();
            if (item.gameObject.TryGetComponent<CanvasGroup>(out var group))
            {
                group.alpha = 0;
            }
        }

        private void SetupItem(UICommonItem item, RewardConfig rewardConfig)
        {
            if (item.gameObject.TryGetComponent<CanvasGroup>(out var group))
            {
                group.alpha = 1;
            }

            item.Setup();
            item.Refresh(rewardConfig);
        }
    }
}