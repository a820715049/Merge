/*
 * @Author: qun.chao
 * @Date: 2021-11-03 17:05:06
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI.Extensions.EasingCore;

public interface ICommonScrollGridContext : IFancyGridViewContext
{
    int SelectedIndex { get; set; }
    Action<int> OnCellSelected { get; set; }
}

public class UICommonScrollGridDefaultContext : ICommonScrollGridContext
{
    public int SelectedIndex { get; set; } = -1;
    public Action<int> OnCellSelected { get; set; }

    ScrollDirection IFancyScrollRectContext.ScrollDirection { get; set; }
    Func<(float ScrollSize, float ReuseMargin)> IFancyScrollRectContext.CalculateScrollSize { get; set; }
    GameObject IFancyCellGroupContext.CellTemplate { get; set; }
    Func<int> IFancyCellGroupContext.GetGroupCount { get; set; }
    Func<float> IFancyGridViewContext.GetStartAxisSpacing { get; set; }
    Func<float> IFancyGridViewContext.GetCellSize { get; set; }
}

public class UICommonScrollGrid<_TData, _TContext> : FancyGridView<_TData, _TContext> where _TContext : class, ICommonScrollGridContext, new()
{
    protected override void SetupCellTemplate()
    {
        throw new NotImplementedException();
    }

    public _TContext InnerContext => Context;

    public void InitLayout()
    {
        Relayout();
    }

    public void UpdateData(IList<_TData> items)
    {
        UpdateContents(items);
    }

    // public void ClearData()
    // {
    //     ItemsSource.Clear();
    //     Context.SelectedIndex = -1;
    // }

    public void ScrollTo(int index, float duration, Ease easing, Alignment alignment = Alignment.Middle)
    {
        UpdateSelection(index);
        ScrollTo(index, duration, easing, GetAlignment(alignment));
    }

    public void JumpTo(int index, Alignment alignment = Alignment.Middle)
    {
        UpdateSelection(index);
        JumpTo(index, GetAlignment(alignment));
    }

    float GetAlignment(Alignment alignment)
    {
        switch (alignment)
        {
            case Alignment.Upper: return 0.0f;
            case Alignment.Middle: return 0.5f;
            case Alignment.Lower: return 1.0f;
            default: return GetAlignment(Alignment.Middle);
        }
    }

    void UpdateSelection(int index)
    {
        if (Context.SelectedIndex == index)
        {
            return;
        }

        Context.SelectedIndex = index;
        Context.OnCellSelected?.Invoke(index);
        Refresh();
    }

}