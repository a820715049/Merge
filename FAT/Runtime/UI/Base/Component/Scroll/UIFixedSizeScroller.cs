/*
 * @Author: qun.chao
 * @Date: 2022-03-22 11:48:49
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace FAT
{
    public class FixedSizeItemProxy<T, C>
    {
        public T data { get; private set; }
        // from < to / left -> right / bottom -> top
        public float from { get; private set; }
        public float to { get; private set; }
        public RectTransform trans { get; set; }
        public PoolItemType type { get; set; }

        public FixedSizeItemProxy(T _data, PoolItemType _type, float _from, float _to)
        {
            data = _data;
            type = _type;
            from = _from;
            to = _to;
        }
    }

    public class UIFixedSizeScroller<T, C> : MonoBehaviour where C : new()
    {
        [SerializeField] protected RectTransform viewport;
        [SerializeField] protected RectTransform content;
        /* 序列排列方向 默认 left->right / top->bottom | true 表示反转 */
        [SerializeField] protected bool verticle;
        [SerializeField] protected bool reverse;
        [SerializeField] protected int childIndexOffset;
        protected List<FixedSizeItemProxy<T, C>> mProxyList = new List<FixedSizeItemProxy<T, C>>();
        protected C Context { get; } = new C();

        protected int mHighIdx = -1;
        protected int mLowIdx = -1;
        protected float mContentSize;
        // 下上/左右 边界
        protected float mViewLowEdge;
        protected float mViewHighEdge;

        public virtual void Build()
        {
            if (verticle)
            {
                var size = viewport.rect.height;
                if (reverse)
                {
                    mViewLowEdge = 0f;
                    mViewHighEdge = size;
                }
                else
                {
                    mViewLowEdge = -size;
                    mViewHighEdge = 0f;
                }
            }
            else
            {
            }
        }

        public virtual void Clear()
        {
            mHighIdx = -1;
            mLowIdx = -1;
            foreach (var item in mProxyList)
            {
                _Unload(item);
            }
            mProxyList.Clear();
        }

        private void Update()
        {
            if (mProxyList.Count < 1)
                return;
            _UpdateLayout_Verticle();
        }

        private void _UpdateLayout_Verticle()
        {
            _CalcActiveItemRangeIdx(content.anchoredPosition.y, out int lowIdx, out int highIdx);
            _Apply(lowIdx, highIdx);
        }

        private int _FindVisibleItemIdx(float offset)
        {
            int low = 0;
            int high = mProxyList.Count - 1;
            int mid;
            if (reverse)
            {
                // bottom -> top / right -> left
                while (low <= high)
                {
                    mid = low + ((high - low) >> 1);
                    var ret = _CheckVisible(mProxyList[mid], offset);
                    if (ret > 0) { high = mid - 1; }
                    else if (ret < 0) { low = mid + 1; }
                    else return mid;
                }
            }
            else
            {
                // top -> bottom / left -> right
                while (low <= high)
                {
                    mid = low + ((high - low) >> 1);
                    var ret = _CheckVisible(mProxyList[mid], offset);
                    if (ret > 0) { low = mid + 1; }
                    else if (ret < 0) { high = mid - 1; }
                    else return mid;
                }
            }
            return -1;
        }

        /* 计算当前应该显示的topIdx和bottomIdx */
        private void _CalcActiveItemRangeIdx(float offset, out int lowIdx, out int highIdx)
        {
            // 在当前viewport中查找任一个可见item
            int visibleItemIdx = _FindVisibleItemIdx(offset);
            if (visibleItemIdx < 0)
            {
                lowIdx = 0;
                highIdx = -1;
                return;
            }
            // 推断可见区域的边界
            int maxIdx = mProxyList.Count - 1;
            lowIdx = visibleItemIdx - 1;
            highIdx = visibleItemIdx + 1;
            while (lowIdx >= 0)
            {
                if (_CheckVisible(mProxyList[lowIdx], offset) == 0) --lowIdx;
                else break;
            }
            while (highIdx <= maxIdx)
            {
                if (_CheckVisible(mProxyList[highIdx], offset) == 0) ++highIdx;
                else break;
            }
            lowIdx = Mathf.Clamp(lowIdx, 0, maxIdx);
            highIdx = Mathf.Clamp(highIdx, 0, maxIdx);
        }

        private void _Apply(int lowIdx, int highIdx)
        {
            if (lowIdx > mHighIdx || highIdx < mLowIdx)
            {
                _MergeFast(lowIdx, highIdx);
            }
            else
            {
                _Merge(lowIdx, highIdx);
            }
            mLowIdx = lowIdx;
            mHighIdx = highIdx;
        }

        private void _MergeFast(int lowIdx, int highIdx)
        {
            // unload
            if (mLowIdx >= 0 || mHighIdx >= 0)
            {
                for (int i = mLowIdx; i <= mHighIdx; ++i)
                {
                    _Unload(mProxyList[i]);
                }
            }
            // load
            bool appendToHighIndex = !reverse;
            for (int i = lowIdx; i <= highIdx; ++i)
            {
                _Load(mProxyList[i], appendToHighIndex);
            }
        }

        private void _Merge(int lowIdx, int highIdx)
        {
            var low = Mathf.Min(mLowIdx, lowIdx);
            var high = Mathf.Max(mHighIdx, highIdx);
            var mid = mLowIdx + ((mHighIdx - mLowIdx) >> 1);
            if (low < 0) low = 0;
            if (mid < 0) mid = 0;
            // 分两个方向处理的目的是为了保证item的sibling次序
            bool appendToHighIndex = !reverse;
            bool appendToLowIndex = reverse;
            // low -> high
            for (int i = mid; i <= high; ++i)
            {
                if (_Between(i, lowIdx, highIdx))
                {
                    if (!_Between(i, mLowIdx, mHighIdx))
                    {
                        _Load(mProxyList[i], appendToHighIndex);
                    }
                }
                else
                {
                    _Unload(mProxyList[i]);
                }
            }
            // high -> low
            for (int i = mid - 1; i >= low; --i)
            {
                if (_Between(i, lowIdx, highIdx))
                {
                    if (!_Between(i, mLowIdx, mHighIdx))
                    {
                        _Load(mProxyList[i], appendToLowIndex);
                    }
                }
                else
                {
                    _Unload(mProxyList[i]);
                }
            }
        }

        private int _CheckVisible(FixedSizeItemProxy<T, C> item, float offset)
        {
            if (item.from + offset > mViewHighEdge)
                return 1;
            if (item.to + offset < mViewLowEdge)
                return -1;
            return 0;
        }

        private bool _Between(int val, int a, int b)
        {
            if (a <= b)
                return val >= a && val <= b;
            else
                return val >= b && val <= a;
        }

        private void _Load(FixedSizeItemProxy<T, C> proxy, bool tailOrHead)
        {
            if (proxy.trans != null)
                return;
            var trans = GameObjectPoolManager.Instance.CreateObject(proxy.type).GetComponent<RectTransform>();
            proxy.trans = trans;
            trans.SetParent(content);
            if (tailOrHead)
                trans.SetAsLastSibling();
            else
                trans.SetSiblingIndex(childIndexOffset); // 跳过一些固定设施
            trans.localScale = Vector3.one;
            if (verticle)
            {
                // fix anchor
                if (reverse)
                {
                    trans.anchorMin = new Vector2(trans.anchorMin.x, 0f);
                    trans.anchorMax = new Vector2(trans.anchorMax.x, 0f);
                }
                else
                {
                    trans.anchorMin = new Vector2(trans.anchorMin.x, 1f);
                    trans.anchorMax = new Vector2(trans.anchorMax.x, 1f);
                }
                // pos
                trans.anchoredPosition = new Vector2(0f, proxy.from + (proxy.to - proxy.from) * trans.pivot.y);
                // horizontal stretch
                if (trans.offsetMax.x < 0)
                {
                    trans.offsetMax = new Vector2(0f, trans.offsetMax.y);
                    trans.offsetMin = new Vector2(0f, trans.offsetMin.y);
                }
            }
            else
            {
                // TODO: horizontal
            }
            trans.gameObject.SetActive(true);
            trans.GetComponent<UIGenericItemBase<(T, C)>>().SetData((proxy.data, Context));
        }

        private void _Unload(FixedSizeItemProxy<T, C> proxy)
        {
            if (proxy.trans != null)
            {
                GameObjectPoolManager.Instance.ReleaseObject(proxy.type, proxy.trans.gameObject);
                proxy.trans = null;
            }
        }

        #region utility

        public void JumpToMatch(System.Predicate<FixedSizeItemProxy<T, C>> match, System.Action cb)
        {
            var idx = mProxyList.FindIndex(match);
            if (idx < 0)
            {
                idx = 0;
            }
            ScrollToIdx(idx);
        }

        public void ScrollToMatch(System.Predicate<FixedSizeItemProxy<T, C>> match, System.Action cb)
        {
            var idx = mProxyList.FindIndex(match);
            if (idx < 0)
            {
                idx = 0;
            }
            var from = content.anchoredPosition;
            ScrollToIdx(idx);
            var to = content.anchoredPosition;
            content.DOAnchorPos(to, 0.5f).From(from).OnComplete(() => cb?.Invoke());
        }

        // 简单实现 只适用垂直方向 从上到下排列
        protected void ScrollToIdx(int idx)
        {
            // to 表示item的上边界
            var item = mProxyList[idx];

            // 中部对齐
            // var pos = (item.to + item.from) * 0.5f;
            // var workingAreaSize = (transform as RectTransform).rect.height;
            // var scrollPos = -pos - workingAreaSize * 0.5f;
            // scrollPos = Mathf.Max(0f, scrollPos);
            // scrollPos = Mathf.Min((mContentSize - workingAreaSize), scrollPos);

            // 顶部对齐
            var scrollPos = -item.to;
            var workingAreaSize = (transform as RectTransform).rect.height;
            if (scrollPos > mContentSize - workingAreaSize)
                scrollPos = mContentSize - workingAreaSize;
            transform.GetComponent<ScrollRect>().velocity = Vector2.zero;
            content.anchoredPosition = new Vector2(0f, scrollPos);
        }

        #endregion
    }
}