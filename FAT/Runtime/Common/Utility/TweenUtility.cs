/**
 * @Author: handong.liu
 * @Date: 2021-03-05 18:29:45
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using DG.Tweening;

public static class TweenUtility
{
    public static Tween CreateCollectTween(GameObject go, Vector3 tar, TweenCallback cb)
    {
        var item = go.transform;
        var seq = DOTween.Sequence();
        seq.Append(item.DOScale(Vector3.one * 1.2f, 0.3f).From(Vector3.one, true).SetEase(Ease.OutBack));
        seq.Append(item.DOScale(Vector3.one * 0.7f, 0.5f));
        seq.Join(item.DOMove(tar, 0.5f).SetEase(Ease.InOutCirc).OnComplete(cb));
        seq.Play();
        return seq;
    }
    public static Tween CreateNoticeTween(GameObject go, Vector3 startPoint, float dotHeight)       //dotHeight:0.29
    {
        const float shrinkTime = 0.05f;
        const float upWait = 0.7f;
        const float upTime = 0.2f;
        const float dropTime = 0.2f;
        const float dropWait = 0.5f;
        const float upSmallScaleTime = 0.1f;
        const float upBigScaleTime = 0.1f;
        const float jumpHeightMulti = 0.45f / 0.58f;
        const float diveHeightFactor = 0.05f / 0.58f;
        dotHeight *= go.transform.localScale.y;
        var moveTween = DOTween.Sequence();
        go.transform.localPosition = startPoint;//new Vector3(0.55f, 0.55f, 0);
        float startY = startPoint.y;
        moveTween.Append(go.transform.DOLocalMoveY(startY - diveHeightFactor * dotHeight, shrinkTime)).          //dive down
                Append(go.transform.DOLocalMoveY(startY + jumpHeightMulti * dotHeight, upTime)).                //up
                AppendInterval(dropWait).                                       //wait
                Append(go.transform.DOLocalMoveY(startY, dropTime - shrinkTime)).Append(go.transform.DOLocalMoveY(startY - diveHeightFactor * dotHeight, shrinkTime)).              //drop
                Append(go.transform.DOLocalMoveY(startY, shrinkTime * 2)).AppendInterval(upWait);        //spring back
        
        var xScaleTween = DOTween.Sequence();
        var scaleX = go.transform.localScale.x;
        xScaleTween.Append(go.transform.DOScaleX(1.2f * scaleX, shrinkTime)).
                Append(go.transform.DOScaleX(0.4f * scaleX, upSmallScaleTime)).Append(go.transform.DOScaleX(1 * scaleX, upBigScaleTime)).
                Append(go.transform.DOScaleX(0.85f * scaleX, dropWait)).
                Append(go.transform.DOScaleX(1.0f * scaleX, dropTime - shrinkTime)).Append(go.transform.DOScaleX(1.2f * scaleX, shrinkTime)).
                Append(go.transform.DOScaleX(1.0f * scaleX, shrinkTime * 2));

        var yScaleTween = DOTween.Sequence();
        var scaleY = go.transform.localScale.y;
        yScaleTween.Append(go.transform.DOScaleY(0.4f * scaleY, shrinkTime)).
                Append(go.transform.DOScaleY(1.3f * scaleY, upSmallScaleTime)).
                Append(go.transform.DOScaleY(1 * scaleY, upBigScaleTime)).Append(go.transform.DOScaleY(1.1f * scaleY, dropWait)).
                Append(go.transform.DOScaleY(0.7f * scaleY, dropTime - shrinkTime)).Append(go.transform.DOScaleY(0.4f * scaleY, shrinkTime)).
                Append(go.transform.DOScaleY(1.0f * scaleY, shrinkTime * 2));


        var ret = DOTween.Sequence();
        ret.Append(moveTween);
        ret.Join(xScaleTween);
        ret.Join(yScaleTween);
        ret.SetLink(go);
        ret.SetLoops(-1);
        return ret;
    }
}