/*
 * @Author: tang.yan
 * @Description: 商店轮播礼包拖拽组件
 * @Date: 2024-08-28 16:08:22
 */

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class UIMarketSlideDrag : MonoBehaviour
{
    //layout cell
    [SerializeField] private GameObject layoutPoolItem;
    //layout挂点 上面挂了RectMask2D用于遮罩其他layout
    [SerializeField] private Transform layoutRoot;
    //UI触摸相关事件监听
    [SerializeField] private UIEventTrigger eventTrigger;
    
    [SerializeField][Range(0f, 1f)] private float DragPower;         //拖拽手感力度 值越小越难拖拽 越大越容易 范围0-1
    [SerializeField][Range(0f, 0.1f)] private float DragAddProgress;   //拖拽结束时需要额外附加的进度值 值越大越容易成功翻页 范围0-0.1
    [SerializeField] private bool IsFollow;         //拖拽时UI是否跟随滑动 默认跟随
    [SerializeField] private float TweenDuration;   //拖拽成功后播放的tween动画的持续时间
    
    private float _layoutWidth;     //每个layout的宽度
    private int _totalLayoutNum;    //当前创建的layout的数量
    private float _dragProgress;    //当前拖拽的进度
    private int _dragStartIndex;    //拖拽开始时的index
    private float _dragStartProgress;   //拖拽开始时的进度值
    private double _onePageProgress;    //一页所占的进度百分比值
    private bool _isDragging;       //记录目前是否正在拖拽中
    private Vector3 _cachePos = new Vector3();  //缓存v3 避免频繁new
    private Tween _dragTween;       //拖拽成功后播放的tween动画
    private Action<bool> _onDragSuccess;        //外部注册一次拖拽成功后的回调 参数 true为向左拖拽 false为向右拖拽
    private Action<bool> _onDragStateChange;    //外部注册拖拽状态变化时的回调 参数 true为开始拖拽 false为结束拖拽
    
    public void Prepare()
    {
        GameObjectPoolManager.Instance.PreparePool(PoolItemType.PACK_MARKET_SLIDE_CELL, layoutPoolItem);
        _layoutWidth = (layoutPoolItem.transform as RectTransform)?.rect.width ?? 0;
        //监听触摸相关事件
        eventTrigger.onBeginDrag = OnBeginDrag;
        eventTrigger.onDrag = OnDrag;
        eventTrigger.onEndDrag = OnEndDrag;
    }

    public void RefreshLayoutNum(int layoutNum)
    {
        _totalLayoutNum = layoutNum;
        // 1/_totalLayoutNum 表示每一页所占的进度百分比值
        _onePageProgress = Math.Round(1f / _totalLayoutNum, 2); 
    }
    
    public Transform CreateLayout(bool isTemp)
    {
        if (isTemp)
        {
            var tempObj = new GameObject("temp");
            var rect = tempObj.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
            rect.sizeDelta = (layoutPoolItem.transform as RectTransform)?.sizeDelta ?? Vector2.zero;
            rect.SetParent(layoutRoot);
            return tempObj.transform;
        }
        var obj = GameObjectPoolManager.Instance.CreateObject(PoolItemType.PACK_MARKET_SLIDE_CELL);
        if (obj == null)
            return null;
        var trans = obj.transform;
        trans.SetParent(layoutRoot);
        trans.localScale = Vector3.one;
        trans.localPosition = Vector3.zero;
        return trans;
    }

    public void ReleaseLayout(GameObject obj)
    {
        if (obj.name == "temp")
        {
            DestroyImmediate(obj);
        }
        else
        {
            GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.PACK_MARKET_SLIDE_CELL, obj);
        }
    }
    
    public void InitDrag(Action<bool> onDragSuccess, Action<bool> onDragStateChange)
    {
        _onDragSuccess = onDragSuccess;
        _onDragStateChange = onDragStateChange;
    }

    public void SetDragStartProgress(int index)
    {
        _Apply((float)index / _totalLayoutNum);
    }

    public void ClearDrag()
    {
        _ClearAnim();
        _onDragSuccess = null;
        _onDragStateChange = null;
    }
    
    public void GetCurShowIndex(out int curIndex, out int leftIndex, out int rightIndex)
    {
        curIndex = _CalcCurIdx(_totalLayoutNum, _dragProgress);
        leftIndex = (curIndex - 1 + _totalLayoutNum) % _totalLayoutNum;
        rightIndex = (curIndex + 1) % _totalLayoutNum;
    }

    #region 拖拽相关逻辑

    private bool _IsDragging()
    {
        return _isDragging;
    }
    
    private void OnBeginDrag(PointerEventData evt)
    {
        _dragStartIndex = _CalcCurIdx(_totalLayoutNum, _dragProgress);
        _dragStartProgress = _dragProgress;
        _onDragStateChange?.Invoke(true);
    }

    private void OnDrag(PointerEventData evt)
    {
        if (_IsPlayTween())
            return;
        //左右边界限制
        if (_dragProgress < _onePageProgress * 0.8f || _dragProgress > (1 - _onePageProgress * 1.8f))
            return;
        //一次最多只可滑动一页 且下一页显示的内容不超过该页的80%(乘0.8表示缩小80%)
        var deltaProgress = Mathf.Abs(_dragStartProgress - _dragProgress);
        if (deltaProgress > _onePageProgress * 0.8f)
            return;
        //拖拽力度
        var diff = evt.delta.x * DragPower;
        var p = _dragProgress - (diff / _layoutWidth / _totalLayoutNum);
        _isDragging = true;
        //控制是否跟手
        if (IsFollow)
        {
            _Apply(p);
        }
    }

    private void OnEndDrag(PointerEventData evt)
    {
        _isDragging = false;
        if (_IsPlayTween())
        {
            return;
        }
        var total = _totalLayoutNum;
        var deltaProgress = _dragStartProgress - _dragProgress;
        var tarIdx = _CalcCurIdx(total, _dragProgress, deltaProgress);
        // 拖拽未能产生元素切换时 尝试触发惯性
        if (_dragStartIndex == tarIdx)
        {
            // inertia
            var dragThreshold = EventSystem.current.pixelDragThreshold;
            if (evt.delta.x > dragThreshold)
            {
                tarIdx = (tarIdx - 1 + total) % total;
            }
            else if (evt.delta.x < -dragThreshold)
            {
                tarIdx = (tarIdx + 1) % total;
            }
        }
        //不循环切页 -2说明：做了left和right两个空白页 
        if (_dragStartIndex == _totalLayoutNum - 2 && tarIdx == _totalLayoutNum - 1)
        {
            tarIdx = _totalLayoutNum - 2;
        }
        else if (_dragStartIndex == 1 && tarIdx != _dragStartIndex + 1)
        {
            tarIdx = 1;
        }
        bool isDragSucc = _dragStartIndex != tarIdx;
        //手势上是否是向左滑动  向左滑动说明页签id需要右移+1
        bool isTryLeft = tarIdx > _dragStartIndex;
        //tween动画
        _PlayRoll(tarIdx, total, isDragSucc, isTryLeft);
        _onDragStateChange?.Invoke(false);
    }
    #endregion

    #region tween动画相关

    private void _PlayRoll(int tarIdx, int total, bool isDragSucc, bool isLeft)
    {
        var targetProgress = 1f * tarIdx / total;
        var curProgress = _dragProgress;
        var diff = targetProgress - curProgress;
        if (diff > 0.5f)
        {
            targetProgress = curProgress - (1f - diff);
        }
        else if (diff < -0.5f)
        {
            targetProgress = curProgress + (diff + 1);
        }
        _PlayTween(curProgress, targetProgress, isDragSucc, isLeft);
    }

    private bool _IsPlayTween()
    {
        return _dragTween != null;
    }

    private void _PlayTween(float from, float to, bool isDragSucc, bool isLeft)
    {
        _dragTween = DOTween.To(() => from, x => from = x, to, TweenDuration).OnUpdate(() => _Apply(from)).OnKill(() =>
        {
            _Apply(to);
            if (isDragSucc)
                _onDragSuccess?.Invoke(isLeft);
            _dragTween = null;
        });
    }

    private void _ClearAnim()
    {
        if (_dragTween != null)
            _dragTween.Kill();
        _dragTween = null;
    }

    #endregion

    #region 位置计算相关

    private void _Apply(float progress)
    {
        progress -= Mathf.FloorToInt(progress);
        _dragProgress = Mathf.Clamp(progress, 0f, 1f);;
        
        var pos = _totalLayoutNum * progress;
        var cur = _CalcCurIdx(_totalLayoutNum, progress);
        var left = (cur - 1 + _totalLayoutNum) % _totalLayoutNum;
        var right = (cur + 1) % _totalLayoutNum;

        for (int i = 0; i < _totalLayoutNum; ++i)
        {
            if (i != cur && i != left && i != right)
                layoutRoot.GetChild(i).gameObject.SetActive(false);
        }

        var posDiff = cur - pos;
        float offset;
        if (posDiff >= 0)
        {
            offset = (posDiff - Mathf.FloorToInt(posDiff)) * _layoutWidth;
        }
        else
        {
            if (Mathf.Abs(posDiff) > 1f)
                offset = (posDiff - Mathf.FloorToInt(posDiff)) * _layoutWidth;
            else
                offset = (posDiff - Mathf.FloorToInt(posDiff) - 1) * _layoutWidth;
        }
        _SetItemPos(cur, offset);
        _SetItemPos(left, offset - _layoutWidth);
        _SetItemPos(right, offset + _layoutWidth);
    }

    private int _CalcCurIdx(int total, float progress, float delta = 0)
    {
        if (delta != 0)
        {
            //delta <0向左划 delta>0向右滑
            progress = delta < 0 ? progress + DragAddProgress : progress - DragAddProgress;
        }
        var pos = total * progress;
        var cur = Mathf.FloorToInt(pos + 0.5f);
        cur = cur % total;
        return cur;
    }

    private void _SetItemPos(int idx, float offset)
    {
        layoutRoot.GetChild(idx).gameObject.SetActive(true);
        _cachePos.Set(offset, 0f, 0f);
        layoutRoot.GetChild(idx).transform.localPosition = _cachePos;
    }

    #endregion

    #region 自动拖拽相关

    public void MoveToPage(int targetPage)
    {
        // 计算目标进度
        float targetProgress = (float)targetPage / _totalLayoutNum;

        // 如果当前已经在进行拖拽或 tween 动画，先清除动画
        if (_IsDragging() || _IsPlayTween())
        {
            _ClearAnim();
        }

        // 开始 tween 动画
        _dragTween = DOTween.To(() => _dragProgress, x => _dragProgress = x, targetProgress, TweenDuration)
            .OnUpdate(() => { _Apply(_dragProgress); }).OnKill(() =>
            {
                _Apply(targetProgress);
                _onDragSuccess?.Invoke(false);
                // 动画完成后清除 tween 引用
                _dragTween = null;
            });
    }

    #endregion
}
