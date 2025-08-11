/*
 * @Author: qun.chao
 * @Date: 2025-03-03 18:19:55
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FAT.Merge;
using EL;

namespace FAT
{
    public class MBBingoGroupSelector : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private GameObject btnNoSelect;
        [SerializeField] private GameObject btnSelected;

        private List<(int, List<int>)> groupInfo = new();
        private Action<int> onSelected;
        private UIBingoMain uiMain;

        public void InitOnPreOpen(UIBingoMain main)
        {
            uiMain = main;
        }

        public void CleanupOnPostClose()
        {
            uiMain = null;
        }

        public void Refresh(Action<int> onSelected)
        {
            this.onSelected = onSelected;
            var groups = uiMain.ActInst.GetOptionalBingoGroup();
            groupInfo.Clear();
            foreach (var group in groups)
            {
                groupInfo.Add((group.Key, group.Value));
            }
            groupInfo.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            for (var i = 0; i < root.childCount; i++)
            {
                var item = root.GetChild(i);
                if (i < groupInfo.Count)
                {
                    item.gameObject.SetActive(true);
                    ShowGroup(item, i);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }
            ClearSelection();
        }

        private void ShowGroup(Transform groupRoot, int idx)
        {
            var btn = groupRoot.Access<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnSelect(idx));

            var itemRoot = groupRoot.Find("Anchor/Root");
            var categoryIds = groupInfo[idx].Item2;

            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<int>>(out var itemIds);
            // 找到每个链条上的最高等级的item
            BingoUtility.FillHighestLeveItemByCategory(categoryIds, itemIds);
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                var item = itemRoot.GetChild(i);
                if (i < categoryIds.Count)
                {
                    if (itemIds[i] <= 0)
                    {
                        item.gameObject.SetActive(false);
                        continue;
                    }
                    item.gameObject.SetActive(true);
                    var uiItem = item.GetComponent<UICommonItem>();
                    uiItem.Refresh(itemIds[i], 0);
                    uiItem.ExtendTipsForMergeItem(itemIds[i]);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }
        }

        private void ClearSelection()
        {
            for (var i = 0; i < root.childCount; i++)
            {
                SetSelection(i, false);
            }
        }

        private void SetSelection(int idx, bool selected)
        {
            var item = root.GetChild(idx);
            item.Find("Anchor/Normal").gameObject.SetActive(!selected);
            item.Find("Anchor/Selected").gameObject.SetActive(selected);
        }

        private void OnSelect(int index)
        {
            // 切换选中状态
            ClearSelection();
            SetSelection(index, true);
            // 切换确认按钮状态
            btnNoSelect.SetActive(false);
            btnSelected.SetActive(true);
            onSelected?.Invoke(groupInfo[index].Item1);
        }
    }
}