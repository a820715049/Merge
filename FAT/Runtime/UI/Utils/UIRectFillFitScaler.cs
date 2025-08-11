/*
 * @Author: qun.chao
 * @Date: 2022-10-24 12:48:14
 */
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIRectFillFitScaler : UIBehaviour
{
    internal class DimensionsChangeCheck : UIBehaviour
    {
        public Action onDimensionsChange;
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            onDimensionsChange?.Invoke();
        }
    }

    private DimensionsChangeCheck mChecker;

    protected override void Start()
    {
        base.Start();
        _Bind();
        _Refresh();
    }

    protected override void OnDestroy()
    {
        _Unbind();
        base.OnDestroy();
    }

    protected override void OnTransformParentChanged()
    {
        base.OnTransformParentChanged();
        _Bind();
        _Refresh();
    }

    private void _Bind()
    {
        _Unbind();
        var parent = transform.parent;
        if (parent != null)
        {
            mChecker = parent.gameObject.AddComponent<DimensionsChangeCheck>();
            mChecker.onDimensionsChange = _Refresh;
        }
    }

    private void _Unbind()
    {
        if (mChecker != null)
        {
            mChecker.onDimensionsChange = null;
            Component.Destroy(mChecker);
        }
        mChecker = null;
    }

    private void _Refresh()
    {
        var parent = transform.parent as RectTransform;
        if (parent == null)
        {
            Debug.LogError("no parent");
            return;
        }
        var self = transform as RectTransform;

        var pw = parent.rect.width;
        var ph = parent.rect.height;
        var w = self.rect.width;
        var h = self.rect.height;
        var scale = 1f;

        if (w / h > pw / ph)
        {
            // 过宽
            scale = ph / h;
        }
        else
        {
            // 过高
            scale = pw / w;
        }
        transform.localScale = Vector3.one * scale;
    }
}