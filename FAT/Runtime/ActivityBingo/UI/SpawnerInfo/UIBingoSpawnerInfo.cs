/*
 * @Author: qun.chao
 * @Date: 2025-07-08 12:19:17
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class UIBingoSpawnerInfo : UIBase
    {
        [SerializeField] private UIBingoSpawnerInfoScrollRect scrollRect;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Transform spawnerRoot;

        private ActivityBingo actInst;

        protected override void OnCreate()
        {
            transform.Access<Button>("Mask").onClick.AddListener(Close);
            btnClose.onClick.AddListener(Close);
            btnConfirm.onClick.AddListener(Close);
            scrollRect.InitLayout();
        }

        protected override void OnParse(params object[] items)
        {
            actInst = items[0] as ActivityBingo;
        }

        protected override void OnPreOpen()
        {
            if (actInst == null)
                return;
            RefreshSpawnerGroup();
            RefreshRound();
        }

        private void RefreshSpawnerGroup()
        {
            var groupDetail = ItemBingoUtility.GetGroupDetail(actInst.BingoGroupID);
            var categoryIds = groupDetail.IncludeSpawner;
            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<int>>(out var itemIds);
            ItemBingoUtility.FillHighestLeveItemByCategory(categoryIds, itemIds);
            for (var i = 0; i < spawnerRoot.childCount; i++)
            {
                var item = spawnerRoot.GetChild(i);
                if (i < itemIds.Count)
                {
                    var itemId = itemIds[i];
                    if (itemId <= 0)
                    {
                        item.gameObject.SetActive(false);
                        continue;
                    }
                    item.gameObject.SetActive(true);
                    var uiItem = item.GetComponent<UICommonItem>();
                    uiItem.Refresh(itemId, 0);
                    uiItem.ExtendTipsForMergeItem(itemId);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshRound()
        {
            var groupDetail = ItemBingoUtility.GetGroupDetail(actInst.BingoGroupID);
            var boards = groupDetail.IncludeBoard;
            scrollRect.UpdateData(boards);
        }
    }
}