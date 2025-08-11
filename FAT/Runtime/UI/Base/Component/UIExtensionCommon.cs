/*
 * @Author: qun.chao
 * @Date: 2020-10-27 11:22:18
 */
using System;
using UnityEngine.UI.Extensions;
using UnityEngine.UI.Extensions.EasingCore;

namespace UnityEngine.UI.Extensions
{
    class GridContext : FancyGridViewContext
    {
        public int SelectedIndex = -1;
        public Action<int> OnCellClicked;
    }

    public class ScrollRectContext : FancyScrollRectContext
    {
        public int SelectedIndex = -1;
        public Action<int> OnCellClicked;
    }

    public class ScrollViewContext
    {
        public int SelectedIndex = -1;
        public Action<int> OnCellClicked;
    }

    public enum Alignment
    {
        Upper,
        Middle,
        Lower,
    }
}