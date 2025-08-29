// ==================================================
// // File: UIBPScrollRect.cs
// // Author: liyueran
// // Date: 2025-06-18 16:06:38
// // Desc: $
// // ==================================================

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using FAT.MSG;
using fat.rawdata;
using UnityEngine;
using Ease = UnityEngine.UI.Extensions.EasingCore.Ease;

namespace FAT
{
    public class BPMileStoneCellViewData
    {
        // 里程碑状态
        public enum UIBpCellViewState
        {
            UnAchieve, // 未激活
            Achieved, // 已激活
            Claimed, // 已领取
        }

        public int ShowNum { get; set; } = -1;
        public bool IsIcon { get; set; } = false;
        public bool IsEmpty { get; set; } = false;
        public bool IsCycle { get; set; } = false;

        public int ProgressViewLv { get; set; }

        public UIBpCellViewState FreeCellViewState { get; set; }
        public UIBpCellViewState LuxuryCellViewState { get; set; }


        public BpMilestone Config { get; set; }

        public BPActivity Activity { get; set; }
    }

    public class UIBPMileStoneScrollRect : UICommonScrollRect<BPMileStoneCellViewData, UICommonScrollRectDefaultContext>
    {
        public int CellCount => ItemsSource.Count;

        public UIBPMain main;

        // 里程碑跳转 并且排序
        public void JumpToWithSort(int index, float alignment = 0.5f)
        {
            UpdateSelection(index);
            JumpTo(index, alignment);
        }

        public void JumpToBottom()
        {
            UpdateSelection(ItemsSource.Count - 1);
            Scroller.Position = ItemsSource.Count - 1;
        }


        // 因为升级触发的滚动
        public void BpLvUpScrollTo(int toIndex, float duration, Ease easing, float alignment = 0.5f,
            Action onComplete = null)
        {
            var cellIndex = toIndex;

            var allMilestoneInfo = main.Activity.GetCurDetailConfig()?.MileStones;
            if (allMilestoneInfo != null)
            {
                if (cellIndex > allMilestoneInfo.Count)
                {
                    cellIndex = allMilestoneInfo.Count;
                }
            }

            main.SetBlock(true);
            var total = duration * (toIndex - Context.SelectedIndex + 1);
            MessageCenter.Get<UI_BP_MILESTONECELL_PLAY_PROGRESS>().Dispatch(Context.SelectedIndex, duration, Context.SelectedIndex, cellIndex);

            UpdateSelection(toIndex);

            base.ScrollTo(toIndex, total, easing, alignment, () =>
            {
                foreach (var data in ItemsSource)
                {
                    data.ProgressViewLv = main.Activity.GetCurMilestoneLevel() + 1;
                }

                main.SetBlock(false);
                onComplete?.Invoke();
            });

            CoSafeBlock(total);
        }

        // 外部dotween调用 此处不能用协程，可能会界面关闭无法调用协程
        private void CoSafeBlock(float seconds)
        {
            var seq = DOTween.Sequence();
            seq.AppendInterval(seconds);
            seq.AppendCallback(() => main.SetBlock(false));
            seq.Play();
        }
    }
}