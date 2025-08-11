/*
 *@Author:chaoran.zhang
 *@Desc:卡册活动重复奖励选卡界面滚动列表
 *@Created Time:2024.09.02 星期一 15:22:03
 */

using System;
using UnityEngine;
using UnityEngine.UI.Extensions;
using static FAT.CardMan;

namespace FAT
{
    public class UICardExchangeScroll : FancyGridView<UICardStarExchangeData, UICardExchangeContext>
    {
        private class CellGroup : DefaultCellGroup
        {
        }

        [SerializeField] private UICardExchangeItemCell cellPrefab;

        protected override void SetupCellTemplate()
        {
            Setup<CellGroup>(cellPrefab);
        }

        #region 界面参数

        public float PaddingTop
        {
            get => paddingHead;
            set
            {
                paddingHead = value;
                Relayout();
            }
        }

        public float PaddingBottom
        {
            get => paddingTail;
            set
            {
                paddingTail = value;
                Relayout();
            }
        }

        public float SpacingY
        {
            get => spacing;
            set
            {
                spacing = value;
                Relayout();
            }
        }

        public float SpacingX
        {
            get => startAxisSpacing;
            set
            {
                startAxisSpacing = value;
                Relayout();
            }
        }

        #endregion

        public void UpdateSelection(int index)
        {
            if (Context.SelectedID == index) return;

            Context.SelectedID = index;
            Refresh();
        }

        public void OnCellClicked(Action<int> callback)
        {
            Context.OnCellClicked = callback;
        }

        public void OnSelectNumChange(Action<int> callback)
        {
            Context.OnSelectNumChange = callback;
        }
    }
}