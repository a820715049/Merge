/*
 * @Author: qun.chao
 * @Date: 2025-07-08 12:41:41
 */

using UnityEngine;
using UnityEngine.UI.Extensions;
using TMPro;
using System.Collections.Generic;

namespace FAT
{
    public class UIBingoSpawnerInfoItem : FancyScrollRectCell<int, UICommonScrollRectDefaultContext>
    {
        [SerializeField] private TextMeshProUGUI txtRoundIdx;
        [SerializeField] private UICommonItem itemA;
        [SerializeField] private UICommonItem itemB;

        public override void UpdateContent(int itemData)
        {
            txtRoundIdx.text = $"{this.Index + 1}";
            var board = ItemBingoUtility.GetBoardConfig(itemData);
            // 生成器链条ID
            var cats = board.ConnectSpawner;
            RefreshItem(itemA, cats, 0);
            RefreshItem(itemB, cats, 1);
        }

        private void RefreshItem(UICommonItem item, IList<int> cats, int idx)
        {
            if (cats.Count > idx)
            {
                var itemId = ItemBingoUtility.GetHighestLevelItemIdInCategory(cats[idx]);
                if (itemId > 0)
                {
                    item.Refresh(itemId, 0);
                    item.ExtendTipsForMergeItem(itemId);
                    item.gameObject.SetActive(true);
                    return;
                }
            }
            item.gameObject.SetActive(false);
        }
    }
}