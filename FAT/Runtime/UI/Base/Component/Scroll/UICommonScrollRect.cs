using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI.Extensions.EasingCore;

public interface ICommonScrollRectContext : IFancyScrollRectContext
{
    int SelectedIndex { get; set; }
    Action<int> OnCellSelected { get; set; }
}

public class UICommonScrollRectDefaultContext : ICommonScrollRectContext
{
    public int SelectedIndex { get; set; } = -1;
    public Action<int> OnCellSelected { get; set; }
    public ScrollDirection ScrollDirection { get; set; }
    public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }
}

public class UICommonScrollRect<_TData, _TContext> : FancyScrollRect<_TData, _TContext> where _TContext : class, ICommonScrollRectContext, new()
{
    [SerializeField] float cellSize = 100f;
    [SerializeField] GameObject cellPrefab = default;
    protected override GameObject CellPrefab => cellPrefab;
    protected override float CellSize => cellSize;
    public _TContext InnerContext => Context;

    public void InitLayout()
    {
        Relayout();
    }

    public void UpdateData(IList<_TData> items)
    {
        UpdateContents(items);
    }

    public void ClearData()
    {
        ItemsSource.Clear();
        Context.SelectedIndex = -1;
    }

    public void ScrollTo(int index, float duration, Ease easing = Ease.InOutQuint, Alignment alignment = Alignment.Middle, Action onComplete = null)
    {
        UpdateSelection(index);
        base.ScrollTo(index, duration, easing, GetAlignment(alignment), onComplete);
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

    protected void UpdateSelection(int index)
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