/*
 * @Author: tang.yan
 * @Description:  集卡活动-卡片组拖拽组件
 * @Date: 2024-01-29 15:01:29
 */

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class UICardCellDrag : MonoBehaviour
{
    //左右按钮
    [SerializeField] private Button leftBtn;
    [SerializeField] private Button rightBtn;
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
    private Action<int, int> _onClickSuccess;   //外部注册一次点击成功后的回调
    private bool _canDragLeft;      //目前是否可以向左拖拽
    private bool _canDragRight;     //目前是否可以向右拖拽

    public void Prepare(int layoutNum)
    {
        GameObjectPoolManager.Instance.PreparePool(PoolItemType.CARD_CELL_LAYOUT, layoutPoolItem);
        _layoutWidth = (layoutPoolItem.transform as RectTransform)?.rect.width ?? 0;
        _totalLayoutNum = layoutNum;
        // 1/_totalLayoutNum 表示每一页所占的进度百分比值
        _onePageProgress = Math.Round(1f / _totalLayoutNum, 2); 
        //监听触摸相关事件
        eventTrigger.onBeginDrag = OnBeginDrag;
        eventTrigger.onDrag = OnDrag;
        eventTrigger.onEndDrag = OnEndDrag;
        eventTrigger.onClick = OnPointerClick;
        //左右按钮添加事件
        leftBtn.WithClickScale().FixPivot().onClick.AddListener(_OnLeftBtnClick);
        rightBtn.WithClickScale().FixPivot().onClick.AddListener(_OnRightBtnClick);
    }

    public Transform CreateCardCellLayout(bool isTemp)
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
        var obj = GameObjectPoolManager.Instance.CreateObject(PoolItemType.CARD_CELL_LAYOUT);
        if (obj == null)
            return null;
        var trans = obj.transform;
        trans.SetParent(layoutRoot);
        trans.localScale = Vector3.one;
        trans.localPosition = Vector3.zero;
        return trans;
    }
    
    //暂时无需释放
    public void ReleaseCardCellLayout(GameObject obj)
    {
        GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.CARD_CELL_LAYOUT, obj);
    }

    public void InitDrag(Action<bool> onDragSuccess, Action<int, int> onClickSuccess)
    {
        _onDragSuccess = onDragSuccess;
        _onClickSuccess = onClickSuccess;
    }

    public void SetDragStartProgress(int index)
    {
        _Apply((float)index / _totalLayoutNum);
    }

    public void ClearDrag()
    {
        _ClearAnim();
        _onDragSuccess = null;
        _onClickSuccess = null;
    }
    
    public void GetCurShowIndex(out int curIndex, out int leftIndex, out int rightIndex)
    {
        curIndex = _CalcCurIdx(_totalLayoutNum, _dragProgress);
        leftIndex = (curIndex - 1 + _totalLayoutNum) % _totalLayoutNum;
        rightIndex = (curIndex + 1) % _totalLayoutNum;
    }

    public void SetCanDragLeft(bool canDragLeft)
    {
        _canDragLeft = canDragLeft;
        rightBtn.gameObject.SetActive(_canDragLeft);
    }
    
    public void SetCanDragRight(bool canDragRight)
    {
        _canDragRight = canDragRight;
        leftBtn.gameObject.SetActive(_canDragRight);
    }

    private void _OnLeftBtnClick()
    {
        if (_IsDragging() || _IsPlayTween())
            return;
        FAT.Game.Manager.audioMan.TriggerSound("PageLeft");
        GetCurShowIndex(out _, out int tarIdx, out _);
        //tween动画
        _PlayRoll(tarIdx, _totalLayoutNum, true, false);
    }

    private void _OnRightBtnClick()
    {
        if (_IsDragging() || _IsPlayTween())
            return;
        FAT.Game.Manager.audioMan.TriggerSound("PageRight");
        GetCurShowIndex(out _, out _, out int tarIdx);
        //tween动画
        _PlayRoll(tarIdx, _totalLayoutNum, true, true);
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
        _dragTween = DOTween.To(() => from, x => from = x, to, TweenDuration).OnUpdate(() => _Apply(from)).OnComplete(() =>
        {
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

    #region 点击相关逻辑

    private void OnPointerClick(PointerEventData eventData)
    {
        if (_IsDragging() || _IsPlayTween()) return;    //拖拽发生时不算点击
        var curPageIndex = _CalcCurIdx(_totalLayoutNum, _dragProgress);
        var rectTrans = layoutRoot.GetChild(curPageIndex).transform as RectTransform;
        if (rectTrans == null) return;
        //根据点击的位置计算出目前是点到了哪个cell 考虑了padding和spacing
        //这里的rectTrans因为拖拽逻辑的存在，其锚点位置只能设为中心位置，这样转换出来的localPos的坐标原点就会在rectTrans的中心位置
        //而为了好计算 后续逻辑中 会将localPos调整为相对于容器左上角的坐标
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTrans, eventData.position, null, out var localPos))
        {
            //获取GirdLayoutGroup
            var gridLayoutGroup = rectTrans.gameObject.GetComponent<GridLayoutGroup>();
            if (gridLayoutGroup == null)
                return;
            var rect = rectTrans.rect;
            var padding = gridLayoutGroup.padding;
            //将localPos调整为相对于容器左上角的坐标 同时考虑左和上的 Padding
            //另外右和下的 Padding如果有设置 则需要调整容器的有效宽度和高度，但为何不直接在prefab上就设置好呢 所以这一步不作代码处理
            localPos.x += rect.width / 2 - padding.left;
            localPos.y -= rect.height / 2 - padding.top;
            // 确保localPos位于 GridLayoutGroup 内容区域内（排除 Padding 区域）
            float contentWidth = rect.width - padding.left - padding.right;
            float contentHeight = rect.height - padding.top - padding.bottom;
            // 点击在内容区域外，忽略此次点击
            if (localPos.x < 0 || localPos.x > contentWidth || localPos.y > 0 || localPos.y < -contentHeight)
            {
                return;
            }
            //计算一个cell的宽高 先考虑CellSize和Spacing
            float cellWidth = gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x;
            float cellHeight = gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y;
            //计算点击位置的行列索引 因为目前坐标是基于左上角开始的 所以直接可以计算出点击位置位于哪一行哪一列
            int column = Mathf.FloorToInt(localPos.x / cellWidth);
            int row = Mathf.FloorToInt(-localPos.y / cellHeight); // Y坐标是向下增长的，所以需要取反
            //检查行列索引是否在有效范围内
            if (column < 0 || column >= gridLayoutGroup.constraintCount || row < 0)
            {
                return;
            }
            //检查点击坐标是否在有效的 Cell 内（不在 Spacing 区域）
            //这里用%的方式相当于将localPos限制在了一个cell在算上space之后的宽高范围内
            float offsetX = localPos.x % cellWidth;
            float offsetY = -localPos.y % cellHeight;
            //如果偏移值超出了cell本身宽高cellSize，说明点击位置在cell之间的空隙区域  这里y轴上额外-10是因为真正显示出来的cell背景图会比实际高度短一点(美术切图多切了一块)
            if (offsetX > gridLayoutGroup.cellSize.x || offsetY > gridLayoutGroup.cellSize.y - 10) {
                return;
            }
            // 根据行列索引获取具体的 Cell
            int cellIndex = row * gridLayoutGroup.constraintCount + column; // 假设使用 FixedColumnCount 约束
            if (cellIndex >= 0 && cellIndex < rectTrans.childCount)
            {
                _onClickSuccess?.Invoke(curPageIndex, cellIndex);  //点击成功时触发
            }
        }
    }

    #endregion
}
