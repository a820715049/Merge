// ==================================================
// // File: MBTrainMissionTrain.cs
// // Author: liyueran
// // Date: 2025-07-30 14:07:12
// // Desc: $
// // ==================================================

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using FAT.MSG;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class MBTrainMissionTrain : MonoBehaviour
    {
        public enum TrainType
        {
            None = 0,
            Top = 1,
            Bottom = 2
        }

        public enum TrainPos
        {
            From = 1260,
            Middle = 0,
            To = -1260
        }

        public TrainType trainType;
        public RectTransform viewport;
        public RectTransform content;
        public ScrollRect scroll;
        public GameObject carriagePrefab;
        public GameObject headPrefab;
        public GameObject recycleTrainPrefab;
        public NonDrawingGraphic block;

        private TrainMissionActivity _activity;
        private UITrainMissionMain _main;
        public UITrainMissionMain Main => _main;
        private UITrainMissionTrainModule _module;

        private Dictionary<int, MBTrainMissionTrainItemCarriage> _itemMap = new();

        private MBTrainMissionTrainItemHead _headTrain;
        [HideInInspector] public MBTrainMissionTrainItemRecycle recycleTrain;

        public Dictionary<int, MBTrainMissionTrainItemCarriage> ItemMap => _itemMap;

        public TrainMissionOrder TrainOrder => GetTrainOrder();
        public int consumeIdx = -1;

        public void OnParse(params object[] items)
        {
            _activity = (TrainMissionActivity)items[0];
            _main = (UITrainMissionMain)items[1];
            _module = (UITrainMissionTrainModule)items[2];
        }

        public void OnShow()
        {
            EnsurePool();
            RefreshTrainOrder(trainType);
            ReleaseTrain();

            // 判断是否是第一次进入活动 
            var pos = _activity.NeedPlayEnterAnim ? TrainPos.From : TrainPos.Middle;

            // 判断轨道上是否有火车 和 什么类型的火车
            if (!_activity.waitRecycle)
            {
                if (CheckNextTrain(trainType))
                {
                    // 生成火车
                    CreateTrain(pos);
                }
                else
                {
                    // 空轨道
                    // 所有火车全部完成时 进入下一轮前
                }
            }

            if (_activity.NeedPlayEnterAnim)
            {
                ScrollIn();
            }
            else
            {
                if (!_activity.waitRecycle)
                {
                    // 自动定位逻辑 界面开启时 延时0.1s 以防那一帧还没创建完对象
                    StartCoroutine(CoAutoScroll());
                }
            }
        }

        private void EnsurePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(_module.PoolKeyCarriageItem))
                return;
            GameObjectPoolManager.Instance.PreparePool(_module.PoolKeyCarriageItem, carriagePrefab);
            GameObjectPoolManager.Instance.PreparePool(_module.PoolKeyHeadItem, headPrefab);
            GameObjectPoolManager.Instance.PreparePool(_module.PoolKeyRecycleItem, recycleTrainPrefab);
        }

        public void OnHide()
        {
            _scrollInSeq?.Kill();
            _scrollOutSeq?.Kill();
            _autoScrollSeq?.Kill();

            ReleaseTrain();

            if (IsBlock())
            {
                SetBlock(false);
            }
        }

        private TrainMissionOrder _topOrder;
        private TrainMissionOrder _bottomOrder;

        private TrainMissionOrder GetTrainOrder()
        {
            switch (trainType)
            {
                case TrainType.Top:
                    return _topOrder;
                case TrainType.Bottom:
                    return _bottomOrder;
                default:
                    return null;
            }
        }


        public void RefreshTrainOrder(TrainType type)
        {
            if (type == TrainType.Top)
            {
                _topOrder = _activity.topOrder;
            }
            else if (type == TrainType.Bottom)
            {
                _bottomOrder = _activity.bottomOrder;
            }
        }

        // 普通火车
        private void CreateTrain(TrainPos pos)
        {
            if (_headTrain != null)
            {
                _headTrain.Release();
                GameObjectPoolManager.Instance.ReleaseObject(_module.PoolKeyHeadItem, _headTrain.gameObject);
                _headTrain = null;
            }

            // 生成车头
            var head = GameObjectPoolManager.Instance.CreateObject(_module.PoolKeyHeadItem, content);
            head.SetActive(true);
            _headTrain = head.GetComponent<MBTrainMissionTrainItemHead>();
            _headTrain.Init(_activity, _module, this, -1); // 车头的索引是-1

            if (pos == TrainPos.Middle)
            {
                _headTrain.bubbleAnim.gameObject.SetActive(true);
                _headTrain.bubbleAnim.SetTrigger("Show");
                _headTrain.bubble = false;
            }

            // 生成车厢
            for (var i = 0; i < TrainOrder.ItemInfos.Count; i++)
            {
                var carriage = GameObjectPoolManager.Instance.CreateObject(_module.PoolKeyCarriageItem, content);
                carriage.SetActive(true);
                var carriageItem = carriage.GetComponent<MBTrainMissionTrainItemCarriage>();
                carriageItem.Init(_activity, _module, this, i);
                _itemMap.Add(i, carriageItem);
            }

            content.anchoredPosition = new Vector2((int)pos, content.anchoredPosition.y);
        }

        // 获得车厢对应的数据信息
        public bool TryGetItemInfo(int index, out TrainMissionItemInfo itemInfo)
        {
            itemInfo = null;

            if (index < 0 || index >= TrainOrder.ItemInfos.Count)
            {
                return false;
            }

            itemInfo = TrainOrder.ItemInfos[index];
            return true;
        }

        public void ReleaseTrain()
        {
            if (_headTrain != null)
            {
                _headTrain.Release();
                GameObjectPoolManager.Instance.ReleaseObject(_module.PoolKeyHeadItem, _headTrain.gameObject);
                _headTrain = null;
            }

            foreach (var item in _itemMap.Values)
            {
                item.Release();
                GameObjectPoolManager.Instance.ReleaseObject(_module.PoolKeyCarriageItem, item.gameObject);
            }

            _itemMap.Clear();

            if (recycleTrain != null)
            {
                recycleTrain.Release();
                GameObjectPoolManager.Instance.ReleaseObject(_module.PoolKeyRecycleItem, recycleTrain.gameObject);
            }
        }

        // 回收火车 
        public void CreateRecycleTrain(TrainPos pos)
        {
            // 生成车头
            var recycle = GameObjectPoolManager.Instance.CreateObject(_module.PoolKeyRecycleItem, content);
            recycle.SetActive(true);
            var train = recycle.GetComponent<MBTrainMissionTrainItemRecycle>();
            train.Init(_activity, _module, this, 0);
            this.recycleTrain = train;

            content.anchoredPosition = new Vector2((int)pos, content.anchoredPosition.y);
        }


        #region 火车动画
        private Sequence _scrollInSeq;
        private Sequence _scrollOutSeq;
        private Sequence _autoScrollSeq;

        // 火车驶入
        public void ScrollIn(Action onComplete = null)
        {
            Game.Manager.audioMan.TriggerSound("TrainCome"); // 火车-火车驶入

            _scrollInSeq?.Kill();
            _scrollInSeq = DOTween.Sequence();

            scroll.enabled = false;
            content.anchoredPosition = new Vector2(1260, content.anchoredPosition.y);

            // 通知 火车驶入
            _scrollInSeq.AppendCallback(() =>
            {
                SetBlock(true);
                MessageCenter.Get<UI_TRAIN_MISSION_SCROLLIN>().Dispatch(trainType);
            });

            var scrollInTime = recycleTrain == null ? Main.scrollInTime : Main.recycleInTime;

            // 位移
            _scrollInSeq.Append(
                content.DOAnchorPosX(0, scrollInTime).OnComplete(() =>
                {
                    SetBlock(false);
                    scroll.enabled = true;
                    // 通知 火车停止
                    MessageCenter.Get<UI_TRAIN_MISSION_SCROLLSTOP>().Dispatch(trainType);
                    onComplete?.Invoke();
                })
            );
            _scrollInSeq.OnKill(() =>
            {
                SetBlock(false);
                AutoScroll();
            });
        }

        // 火车驶出
        public void ScrollOut(Action onComplete = null)
        {
            SetBlock(true);

            Game.Manager.audioMan.TriggerSound("TrainLeft"); // 火车-火车驶出

            _scrollOutSeq?.Kill();
            _scrollOutSeq = DOTween.Sequence();

            scroll.enabled = false;
            // 通知 火车驶出
            _scrollOutSeq.AppendCallback(() =>
            {
                MessageCenter.Get<UI_TRAIN_MISSION_SCROLLOUT>().Dispatch(trainType);
            });

            // 等待1s后 开始位移
            _scrollOutSeq.AppendInterval(1);


            var scrollOutTime = recycleTrain == null ? Main.scrollOutTime : Main.recycleOutTime;

            var to = content.rect.width;

            // 位移
            _scrollOutSeq.Append(
                content.DOAnchorPosX(-to, scrollOutTime).OnComplete(() =>
                {
                    RefreshTrainOrder(trainType);

                    onComplete?.Invoke();

                    SetBlock(false);
                }).SetEase(Main.scrollOutCurve)
            );

            _scrollOutSeq.OnComplete(() =>
            {
                // 释放火车prefab
                ReleaseTrain();

                // 判断是否还有下一辆火车
                if (CheckNextTrain(trainType))
                {
                    // 如果有火车 创建火车+驶入
                    CreateTrain(TrainPos.From);
                    ScrollIn();
                }
                else
                {
                    // 判断是否活动结束
                    if (!_activity.Active)
                    {
                        return;
                    }
                }
            });

            _scrollOutSeq.OnKill(() => {  scroll.enabled = true;SetBlock(false); });
        }

        private bool CheckNextTrain(TrainType type)
        {
            if (_activity.waitEnterNextChallenge || _activity.waitRecycle)
            {
                return false;
            }

            switch (type)
            {
                case TrainType.Top:
                    return _activity.topOrder != null && _activity.topOrder.orderID != 0;
                case TrainType.Bottom:
                    return _activity.bottomOrder != null && _activity.bottomOrder.orderID != 0;
                case TrainType.None:
                    return false;
            }

            return false;
        }
        #endregion

        #region 自动定位
        // 检查火车是否完全显示在屏幕中
        private bool CheckCarriageShow(MBTrainMissionTrainItem item)
        {
            if (item == null || scroll == null || scroll.viewport == null)
                return false;

            // 如果content的宽度小于viewport宽度，说明整个火车都在屏幕中
            if (content.rect.width <= scroll.viewport.rect.width)
            {
                return true;
            }

            // 获取item的RectTransform
            var itemRect = item.GetComponent<RectTransform>();
            if (itemRect == null)
                return false;

            // 获取viewport的RectTransform
            var viewportRect = scroll.viewport;

            // 计算item相对于viewport的位置
            Vector2 itemRelativePos = GetRelativePosition(itemRect, viewportRect);

            // 获取item的尺寸
            var itemWidth = itemRect.rect.width;

            // 获取viewport的尺寸
            var viewportWidth = viewportRect.rect.width;

            // 计算item在viewport坐标系中的边界
            var itemLeft = itemRelativePos.x - itemWidth * 0.5f; // 考虑pivot
            var itemRight = itemRelativePos.x + itemWidth * 0.5f;

            // 判断item是否完全在viewport内
            bool isFullyVisible = itemLeft <= viewportWidth && itemRight <= viewportWidth;

            return isFullyVisible;
        }

        // 获取RectTransform相对于另一个RectTransform的位置
        private Vector2 GetRelativePosition(RectTransform from, RectTransform to)
        {
            // 获取from的中心点世界坐标
            var fromCenter = from.position;

            // 转换为to的本地坐标
            var localCenter = to.InverseTransformPoint(fromCenter);

            return localCenter;
        }

        // 检查是否需要自动滚动
        private bool CheckAutoScroll(out float to)
        {
            to = 0;

            var findIndex = -1;
            // 检查是否有可提交的棋子（状态为2）
            for (var i = 0; i < TrainOrder.ItemInfos.Count; i++)
            {
                if (_activity.CheckMissionState(TrainOrder, i) == 2)
                {
                    findIndex = i;
                    break;
                }
            }

            if (findIndex != -1)
            {
                var carriage = _itemMap[findIndex];
                if (CheckCarriageShow(carriage))
                {
                    // 可提交棋子出现在屏幕中，不需要滚动
                    return false;
                }

                // 可提交棋子不在屏幕中，计算定位位置
                var itemRect = carriage.GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    // 计算item相对于viewport的位置
                    Vector2 itemRelativePos = GetRelativePosition(itemRect, scroll.viewport);

                    var viewportWidth = scroll.viewport.rect.width;
                    var contentWidth = content.rect.width;
                    var itemWidth = itemRect.rect.width;

                    // 根据相对位置判断移动方向
                    float targetContentPosX;

                    // 计算item的左右边界
                    float itemLeft = itemRelativePos.x - itemWidth * 0.5f;
                    float itemRight = itemRelativePos.x + itemWidth * 0.5f;

                    // viewport的左右边界
                    float viewportLeft = -viewportWidth * 0.5f;
                    float viewportRight = viewportWidth * 0.5f;

                    if (itemLeft < 0)
                    {
                        // item完全在viewport左侧，需要向右滚动
                        // 让item左边界对齐viewport左边界
                        float moveDistance = viewportLeft - itemLeft;
                        targetContentPosX = content.anchoredPosition.x + moveDistance;
                    }
                    else if (itemRight > viewportWidth)
                    {
                        // item完全在viewport右侧，需要向左滚动
                        // 让item右边界对齐viewport右边界
                        float moveDistance = viewportRight - itemRight;
                        targetContentPosX = content.anchoredPosition.x + moveDistance;
                    }
                    else
                    {
                        // item在viewport内，不需要滚动
                        return false;
                    }

                    // 转换为归一化位置 (0-1)
                    to = Mathf.Clamp01(-targetContentPosX / (contentWidth - viewportWidth));
                }

                return true;
            }

            // 没有可提交的棋子，检查是否有未完成的棋子（状态为1）
            for (var i = 0; i < TrainOrder.ItemInfos.Count; i++)
            {
                if (_activity.CheckMissionState(TrainOrder, i) == 1)
                {
                    findIndex = i;
                    break;
                }
            }

            if (findIndex != -1)
            {
                var carriage = _itemMap[findIndex];
                if (CheckCarriageShow(carriage))
                {
                    // 未完成棋子出现在屏幕中，不需要滚动
                    return false;
                }

                // 未完成棋子不在屏幕中，计算定位位置
                var itemRect = carriage.GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    // 计算item相对于viewport的位置
                    Vector2 itemRelativePos = GetRelativePosition(itemRect, scroll.viewport);

                    var viewportWidth = scroll.viewport.rect.width;
                    var contentWidth = content.rect.width;
                    var itemWidth = itemRect.rect.width;

                    // 根据相对位置判断移动方向
                    float targetContentPosX;

                    // 计算item的左右边界
                    float itemLeft = itemRelativePos.x - itemWidth * 0.5f;
                    float itemRight = itemRelativePos.x + itemWidth * 0.5f;

                    // viewport的左右边界
                    float viewportLeft = -viewportWidth * 0.5f;
                    float viewportRight = viewportWidth * 0.5f;

                    if (itemLeft < 0)
                    {
                        // item完全在viewport左侧，需要向右滚动
                        // 让item左边界对齐viewport左边界
                        float moveDistance = viewportLeft - itemLeft;
                        targetContentPosX = content.anchoredPosition.x + moveDistance;
                    }
                    else if (itemRight > viewportWidth)
                    {
                        // item完全在viewport右侧，需要向左滚动
                        // 让item右边界对齐viewport右边界
                        float moveDistance = viewportRight - itemRight;
                        targetContentPosX = content.anchoredPosition.x + moveDistance;
                    }
                    else
                    {
                        // item在viewport内，不需要滚动
                        return false;
                    }

                    // 转换为归一化位置 (0-1)
                    to = Mathf.Clamp01(-targetContentPosX / (contentWidth - viewportWidth));
                }

                return true;
            }

            // 没有需要滚动的目标，返回false
            return false;
        }

        private IEnumerator CoAutoScroll()
        {
            yield return new WaitForSeconds(0.1f);
            AutoScroll();
        }

        // 自动定位到需要提交的车厢的位置
        public void AutoScroll()
        {
            // 判断是否需要自动定位
            if (!CheckAutoScroll(out var to))
            {
                // 不需要滚动，直接完成回调
                return;
            }

            // 定位动画
            _autoScrollSeq?.Kill();
            _autoScrollSeq = DOTween.Sequence();
            _autoScrollSeq.AppendCallback(() =>
            {
                MessageCenter.Get<UI_TRAIN_MISSION_SCROLLIN>().Dispatch(trainType);
            });

            _autoScrollSeq.AppendCallback(() =>
            {
                DOTween.To(
                    () => scroll.horizontalNormalizedPosition,
                    x => scroll.horizontalNormalizedPosition = x,
                    to,
                    1f);
            });

            _autoScrollSeq.OnComplete(() => { MessageCenter.Get<UI_TRAIN_MISSION_SCROLLSTOP>().Dispatch(trainType); });

            _autoScrollSeq.OnKill(() => { });
        }
        #endregion

        public bool IsBlock() => block.raycastTarget;

        public void SetBlock(bool value)
        {
            block.raycastTarget = value;
        }
    }
}