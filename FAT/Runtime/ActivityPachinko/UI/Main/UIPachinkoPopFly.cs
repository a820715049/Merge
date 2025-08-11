/*
 * @Author: tang.yan
 * @Description: 弹珠游戏中的飘字
 * @Date: 2024-12-18 14:12:41
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace FAT
{
    public class UIPachinkoPopFly : UIBase
    {
        [SerializeField] 
        [Tooltip("最多可同时显示的tips数量")]
        private int maxShowTipsNum;
        [SerializeField] 
        [Tooltip("提示出现后持续的时间")]
        private float toastLifetime;
        [SerializeField] private RectTransform rootTrans;
        [SerializeField] private GameObject toastCell;
        //记录当前正在显示的cell数量
        private int _curShowTipsNum = 0;
        //int:每个创建的toastCell对应的instanceId   Tween：每个cell对应的tween动画
        private Queue<(GameObject, Sequence)> _cacheQueue = new Queue<(GameObject, Sequence)>();
        //缓存当前各个cell的显示Coroutine
        private Queue<Coroutine> _cacheCo = new Queue<Coroutine>();

        protected override void OnCreate()
        {
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.PACHINKO_FLY_ITEM, toastCell);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 3)
            {
                int flyIconId = (int)items[0];
                string flyNum = (string)items[1];
                Vector3 showPos = new Vector3(0,0,0);
                if (items[2] is Vector3 pos)
                {
                    showPos.Set(pos.x, pos.y, pos.z);
                }
                //创建并显示cell 并播tween
                var co = StartCoroutine(_ShowFlyTips(flyIconId, flyNum, showPos));
                _cacheCo.Enqueue(co);
            }
        }

        protected override void OnPreOpen()
        {
            
        }

        protected override void OnPostClose()
        {
            foreach (var cell in _cacheQueue)
            {
                cell.Item2?.Kill();
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.PACHINKO_FLY_ITEM, cell.Item1);
            }
            _cacheQueue.Clear();
            _curShowTipsNum = 0;
            foreach (var co in _cacheCo)
            {
                StopCoroutine(co);
            }
            _cacheCo.Clear();
        }

        //队列个数如果超过10个 则出列 并强制销毁对应cell
        private void _CheckQueue()
        {
            if (_curShowTipsNum >= maxShowTipsNum)
            {
                var cell = _cacheQueue.Dequeue();
                cell.Item2?.Kill();
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.PACHINKO_FLY_ITEM, cell.Item1);
                var co = _cacheCo.Dequeue();
                StopCoroutine(co);
            }
            else
            {
                _curShowTipsNum++;
            }
        }

        private IEnumerator _ShowFlyTips(int flyIconId, string flyNum, Vector3 showPos)
        {
            //队列个数如果超过10个 则出列 并强制销毁对应cell
            _CheckQueue();
            yield return null;
            //创建cell
            var cell = GameObjectPoolManager.Instance.CreateObject(PoolItemType.PACHINKO_FLY_ITEM);
            cell.SetActive(false);
            cell.transform.SetParent(rootTrans);
            cell.transform.localScale = Vector3.zero;   //初始时设为0 后续tween动画中会变为1
            cell.transform.localPosition = Vector3.zero;
            yield return null;
            cell.SetActive(true);
            //获取到组件并刷新显示
            var cellComp = cell.GetComponent<UIPachinkoPopFlyCell>();
            cellComp.ShowFlyTips(flyIconId, flyNum);
            cellComp.SetVisible(false);
            yield return null;
            cellComp.RefreshTipsPos(showPos);
            yield return null;
            cellComp.SetVisible(true);
            var tween = _StartTweenSeq(cell);
            _cacheQueue.Enqueue((cell, tween));
        }

        private Sequence _StartTweenSeq(GameObject cell)
        {
            var rectTrans = cell.GetComponent<RectTransform>();
            var canvasGroup = cell.GetComponent<CanvasGroup>();
            var anchorPos = rectTrans.anchoredPosition;
            float startX = anchorPos.x;
            Vector2 targetV2 = new Vector2(startX, anchorPos.y + 30f);
            Sequence tweenSeq = DOTween.Sequence();
            tweenSeq.Append(rectTrans.DOAnchorPos(targetV2, toastLifetime * 0.2f).SetEase(Ease.OutQuart));
            tweenSeq.Join(rectTrans.DOScale(Vector3.one, toastLifetime * 0.2f).SetEase(Ease.OutQuart));
            targetV2.Set(startX, anchorPos.y + 80f);
            tweenSeq.Append(rectTrans.DOAnchorPos(targetV2, toastLifetime * 0.2f).SetEase(Ease.InOutSine));
            tweenSeq.Insert(toastLifetime * 0.2f, canvasGroup.DOFade(0f, toastLifetime * 0.2f).SetEase(Ease.InOutSine));
            tweenSeq.OnComplete(() =>
            {
                rectTrans.anchoredPosition = targetV2;
                rectTrans.localScale = Vector3.one;
                //tween动画结束后 回收
                var finishCell = _cacheQueue.Dequeue();
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.PACHINKO_FLY_ITEM, finishCell.Item1);
                finishCell.Item2?.Kill();
                _curShowTipsNum--;
            });
            return tweenSeq;
        }
    }
}