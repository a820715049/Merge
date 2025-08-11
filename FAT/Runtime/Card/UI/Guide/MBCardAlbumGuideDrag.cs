/**
 * @Author: zhangpengjian
 * @Date: 2021-02-19 11:00:53
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/25 18:28:33
 * Description: 卡册教学拖拽
 */

using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using EL;

public class MBCardAlbumGuideDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float animDuration = 0.5f;
    private float mItemWidth;   //一个滑动条目的宽度
    private Tween mTween;
    private int mTotalNum;
    private float mProgress;
    private bool mIsDragging;
    private int mBeginTarIdx = 1;
    private float mDragStartProgress;   //拖拽开始时的进度值

    private void OnDisable()
    {
        _Clear();
    }

    private void OnEnable()
    {
    }

    public void Init(float index = 1f)
    {
        mTotalNum = transform.childCount;
        mItemWidth = (transform.GetChild(0) as RectTransform).rect.width;
        // 打开界面显示首页（第二页 第一页是left占位空白页）
        _Apply(index / mTotalNum);
    }

    #region drag
    void IBeginDragHandler.OnBeginDrag(PointerEventData evt)
    {
        if (transform.childCount == mTotalNum && mTotalNum > 1)
        {
            mBeginTarIdx = _CalcCurIdx(mTotalNum, mProgress);
            mDragStartProgress = mProgress;
        }
    }

    void IDragHandler.OnDrag(PointerEventData evt)
    {
        if (_IsTweening())
            return;
        //一次最多只可滑动一页
        var deltaProgress = Mathf.Abs(mDragStartProgress - mProgress);
        if (deltaProgress > 0.2f)
            return;
        if (transform.childCount == mTotalNum && mTotalNum > 1)
        {
            var diff = evt.delta.x;
            var p = mProgress - diff / mItemWidth / mTotalNum;
            _Apply(p);
        }
        mIsDragging = true;
    }

    void IEndDragHandler.OnEndDrag(PointerEventData evt)
    {
        mIsDragging = false;
        if (_IsTweening())
            return;
        if (transform.childCount == mTotalNum && mTotalNum > 1)
        {
            var total = mTotalNum;
            var tarIdx = _CalcCurIdx(total, mProgress);
            // 拖拽未能产生元素切换时 尝试触发惯性
            if (mBeginTarIdx == tarIdx)
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
            if (mBeginTarIdx == transform.childCount - 2 && tarIdx == transform.childCount - 1)
            {
                tarIdx = transform.childCount - 2;
            }
            else if (mBeginTarIdx == 1 && tarIdx != mBeginTarIdx + 1)
            {
                tarIdx = 1;
            }
            MessageCenter.Get<FAT.MSG.GAME_CARD_GUIDE_DRAG_END>().Dispatch(tarIdx);
            _PlayRoll(tarIdx, total);
        }
    }
    #endregion

    private void _PlayRoll(int tarIdx, int total)
    {
        var targetProgress = 1f * tarIdx / total;
        var curProgress = mProgress;
        var diff = targetProgress - curProgress;
        if (diff > 0.5f)
        {
            targetProgress = curProgress - (1f - diff);
        }
        else if (diff < -0.5f)
        {
            targetProgress = curProgress + (diff + 1);
        }
        _PlayTween(curProgress, targetProgress);
    }

    private bool _IsTweening()
    {
        return mTween != null;
    }

    private void _Clear()
    {
        mTotalNum = 0;
        _ClearAnim();
    }

    private void _PlayTween(float from, float to)
    {
        mTween = DOTween.To(() => from, x => from = x, to, animDuration).OnUpdate(() => _Apply(from)).OnComplete(() =>
        {
            mTween = null;
        });
    }

    private void _ClearAnim()
    {
        if (mTween != null)
            mTween.Kill();
        mTween = null;
    }

    private void _Apply(float progress)
    {
        progress -= Mathf.FloorToInt(progress);
        mProgress = progress;

        var total = transform.childCount;
        var pos = total * progress;
        var cur = _CalcCurIdx(total, progress);
        var left = (cur - 1 + total) % total;
        var right = (cur + 1) % total;

        //只有当前和左右显示  其他隐藏
        for (int i = 0; i < total; ++i)
        {
            if (i != cur && i != left && i != right)
                transform.GetChild(i).gameObject.SetActive(false);
        }

        var posDiff = cur - pos;
        float offset;
        if (posDiff >= 0)
        {
            offset = (posDiff - Mathf.FloorToInt(posDiff)) * mItemWidth;
        }
        else
        {
            if (Mathf.Abs(posDiff) > 1f)
                offset = (posDiff - Mathf.FloorToInt(posDiff)) * mItemWidth;
            else
                offset = (posDiff - Mathf.FloorToInt(posDiff) - 1) * mItemWidth;
        }
        _SetItemPos(cur, offset);
        _SetItemPos(left, offset - mItemWidth);
        _SetItemPos(right, offset + mItemWidth);
    }

    private int _CalcCurIdx(int total, float progress)
    {
        var pos = total * progress;
        var cur = Mathf.FloorToInt(pos + 0.5f);
        cur = cur % total;
        return cur;
    }

    private void _SetItemPos(int idx, float offset)
    {
        transform.GetChild(idx).gameObject.SetActive(true);
        transform.GetChild(idx).transform.localPosition = new Vector3(offset, 0f);
    }
}