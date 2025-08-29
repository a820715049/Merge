// ================================================
// File: MBDotPosFit.cs
// Author: yueran.li
// Date: 2025/08/26 14:40:02 星期二
// Desc: 适配红点位置
// ================================================

using EL;
using UnityEngine;

public class MBDotPosFit : MonoBehaviour
{
    private RectTransform _rectTransform;
    private RectTransform _entryRectTransform;
    private GameObject _entryPrefab;
    public Vector2 offset = new Vector2(-2, 0);

    private void OnEnable()
    {
        if (_entryPrefab != null)
        {
            FitPos(_entryPrefab);
        }
    }

    // 适配红点位置
    public void FitPos(GameObject entry)
    {
        _entryPrefab = entry;
        _rectTransform = GetComponent<RectTransform>();

        if (entry == null)
        {
            DebugEx.Warning("MBDotPosFit: 未找到IActivityBoardEntry父组件");
            return;
        }

        // 获取entry的RectTransform
        _entryRectTransform = entry.GetComponent<RectTransform>();

        if (_entryRectTransform == null)
        {
            DebugEx.Warning("MBDotPosFit: 无法获取entry的RectTransform");
            return;
        }

        // 计算红点相对于entry的位置
        SetTopRightPosition();
    }


    private void SetTopRightPosition()
    {
        if (_rectTransform == null || _entryRectTransform == null) return;

        // 获取entry的右上角世界坐标
        var entryRect = _entryRectTransform.rect;
        var entryTopRightLocal = new Vector2(entryRect.xMax, entryRect.yMax);
        var entryTopRightWorld = _entryRectTransform.TransformPoint(entryTopRightLocal);

        // 计算红点自身的宽度和高度的一半
        var dotWidth = _rectTransform.rect.width * 0.5f;
        var dotHeight = _rectTransform.rect.height * 0.5f;

        // 将红点尺寸的一半转换为世界坐标偏移
        var dotSizeWorld = _rectTransform.TransformVector(new Vector3(dotWidth + offset.x, dotHeight + offset.y, 0));

        // 设置红点的世界坐标位置：entry右上角 + 红点尺寸的一半
        _rectTransform.position = entryTopRightWorld + dotSizeWorld;
    }
}