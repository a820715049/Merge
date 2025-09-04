/*
 * @Author: yanfuxing
 * @Date: 2025-07-22 15:40:09
 */
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;
using Ease = UnityEngine.UI.Extensions.EasingCore.Ease;

namespace FAT
{
    public class RankingContext : FancyScrollRectContext
    {
        public ActivityMultiplierRanking ActivityRanking;
        public MultiplierRankingPlayerData RankingPlayerData;
        public float MaxSize;
        private float _size;
        private RectTransform _content;
        private UIMultiplyRankingCell _top;
        public UIMultiplyRankingCell _bottom;
        private UIMultiplyRankingCell _move;
        private float spacing;
        private bool _blockVisual;
        private float Critical => (_size - spacing) / _content.rect.height;
        private float _offset;
        private float _topOffset;
        public bool IsInit;
        public int LastRankNum;
        public RankingOpenType RankingOpenType;
        public AnimType CurType = AnimType.None;
        public enum AnimType
        {
            None = 0,
            Top = 1,
            Normal = 2,
            Up = 3
        }

        public void SetParameter(float space)
        {
            spacing = space;
            _offset = _size / _content.rect.height;
        }

        /// <summary>
        /// 设置显示属性
        /// </summary>
        /// <param name="size">cell大小</param>
        /// <param name="content">显示区域RectTransform</param>
        public void SetView(float size, RectTransform content, float top)
        {
            _size = size;
            _content = content;
            _topOffset = top;
        }

        /// <summary>
        /// 设置动画用object
        /// </summary>
        /// <param name="top">上方动画框体</param>
        /// <param name="bottom">下方动画框体</param>
        /// <param name="move">移动动画框体</param>
        public void SetCell(UIMultiplyRankingCell top, UIMultiplyRankingCell bottom, UIMultiplyRankingCell move)
        {
            _top = top;
            _top.Initialize();
            _bottom = bottom;
            _bottom.Initialize();
            _move = move;
            _move.Initialize();
        }

        /// <summary>
        /// 设置活动
        /// </summary>
        /// <param name="activityRanking"></param>
        public void SetActivity(ActivityMultiplierRanking activityRanking)
        {
            ActivityRanking = activityRanking;
            _top.UpdateContent(ActivityRanking.myself.data);
            _bottom.UpdateContent(ActivityRanking.myself.data);
            _move.UpdateContent(ActivityRanking.myself.data);
        }

        public void SetRankingOpenType(RankingOpenType type)
        {
            RankingOpenType = type;
        }

        public void Init(bool isInit)
        {
            IsInit = isInit;
        }

        public void CheckType()
        {
            var maxShow = (int)(_content.rect.height / _size);
            if (ActivityRanking.LastRank <= maxShow)
            {
                CurType = AnimType.Top;
            }
            else if (ActivityRanking.LastRank - (int)ActivityRanking.CurRank <= maxShow - 1)
            {
                CurType = AnimType.Normal;
            }
            else
            {
                CurType = AnimType.Up;
            }
        }

        public void UpdateVisual(float pos)
        {
            if (_blockVisual) return;
            if (pos < Critical)
            {
                // _top.transform.localScale = Vector3.one * Mathf.Lerp(1f, MaxSize, (_offset - pos) / Critical);
                _bottom.transform.localScale = Vector3.zero;
            }
            else if (pos > 1 - (_size - 30) / _content.rect.height)
            {
                //_top.transform.localScale = Vector3.zero;
                _bottom.transform.localScale = Vector3.one * Mathf.Lerp(1f, MaxSize, (pos - 1 + _offset) / Critical);
            }
            else
            {
                // _top.transform.localScale = Vector3.zero;
                _bottom.transform.localScale = Vector3.zero;
            }
        }

        public void PlayMoveAnim(float duration)
        {
            _top.transform.localScale = Vector3.zero;
            _bottom.transform.localScale = Vector3.zero;
            _blockVisual = true;
            _move.transform.localScale = Vector3.one;
            var maxShow = (int)(_content.rect.height / _size);
            switch (CurType)
            {
                case AnimType.Up:
                    {
                        _move.transform.DOLocalMove(_top.transform.localPosition - Vector3.up * _size, duration)
                            .From(_bottom.transform.localPosition)
                            .SetEase(DG.Tweening.Ease.InOutCirc)
                            .OnStepComplete(() => _move.transform.localScale = Vector3.zero)
                            .OnComplete(() => _blockVisual = false);
                        break;
                    }
                case AnimType.Top:
                    {
                        var startPos = _top.transform.localPosition - LastRankNum * Vector3.up * _size;
                        var offsetNum = (int)ActivityRanking.CurRank;
                        var offset = ActivityRanking.totalList.Count <= maxShow
                            ? Vector3.down * spacing
                            : Vector3.zero;
                        _move.transform
                            .DOLocalMove(
                                _top.transform.localPosition - offsetNum * Vector3.up * _size + offset,
                                duration)
                            .From(startPos)
                            .SetEase(DG.Tweening.Ease.InOutCirc)
                            .OnStepComplete(() => _move.transform.localScale = Vector3.zero)
                            .OnComplete(() => _blockVisual = false);
                        break;
                    }
                case AnimType.Normal:
                    {
                        var offsetNum = LastRankNum - ActivityRanking.CurRank;
                        _move.transform.DOLocalMove(_bottom.transform.localPosition + offsetNum * Vector3.up * _size,
                                duration)
                            .From(_bottom.transform.localPosition)
                            .SetEase(DG.Tweening.Ease.InOutCirc)
                            .OnStepComplete(() => _move.transform.localScale = Vector3.zero)
                            .OnComplete(() => _blockVisual = false);
                        break;
                    }
            }
        }
    }

    public class UIMultiplyRankingListScroll : FancyScrollRect<MultiplierRankingPlayerData, RankingContext>
    {
        public float Duration;
        public float MaxSize;
        public GameObject SetCellPrefab;
        public float SetCellSize;
        public UIMultiplyRankingCell TopCellItem;
        public UIMultiplyRankingCell BottomCellItem;
        public UIMultiplyRankingCell MoveCellItem;
        public RectTransform ViewRect;
        public GameObject Bg;
        public GameObject ViewPort;
        public GameObject EmptyNode;
        public UIImageRes TokenImageRes;
        public TextMeshProUGUI TokenNum;
        public TextMeshProUGUI Tips;
        public GameObject SpecialNodeTrans;
        protected override GameObject CellPrefab => SetCellPrefab;
        protected override float CellSize => SetCellSize;
        protected Action<bool> InvokeCallBack;
        private ActivityMultiplierRanking _activityRanking;
        private List<MultiplierRankingPlayerData> _allPlayerlist = new();
        private List<MultiplierRankingPlayerData> _allBotList = new();

        public void UpdateInfoList(ActivityMultiplierRanking activity, Action<bool> invoke = null)
        {
            _activityRanking = activity;
            InvokeCallBack = invoke;
            activity.FillPlayerList(_allPlayerlist);
            Context.CheckType();
            var tempLastRank = activity.GetAndRefreshLastRanking();
            // 如果当前排名大于上次排名，则播放动画
            if (tempLastRank > activity.CurRank)
            {
                UIManager.Instance.Block(true);
                var botList = GetBotList();
                botList.Insert(tempLastRank - 1, null);
                UpdateContents(botList);
                ScrollTo(tempLastRank - 1, Context.CurType == RankingContext.AnimType.Top ? 0.01f : 0f, 1f, () => PlayMoveAnim());
            }
            else
            {
                UpdateContents(_allPlayerlist);
                ScrollTo(0, 0.5f, 0.5f, () =>
                {
                    Context.UpdateVisual(1f);
                    if (Context.RankingOpenType == RankingOpenType.End)
                    {
                        Context._bottom.transform.localScale = Vector3.zero;
                    }
                });
            }
        }

        private List<MultiplierRankingPlayerData> GetBotList()
        {
            _allBotList.Clear();
            foreach (var item in _allPlayerlist)
            {
                if (item.isBot)
                {
                    _allBotList.Add(item);
                }
            }
            return _allBotList;
        }

        public void InitContext(ActivityMultiplierRanking activityRanking, RankingOpenType openType, bool isInit = false)
        {
            Context.MaxSize = MaxSize;
            Context.SetView(SetCellSize, ViewRect, paddingHead);
            Context.SetCell(TopCellItem, BottomCellItem, MoveCellItem);
            Context.SetParameter(paddingHead);
            Context.Init(isInit);
            Context.LastRankNum = activityRanking.LastRank;
            Context.SetRankingOpenType(openType);
            if (Context.ActivityRanking == null)
            {
                _activityRanking = activityRanking;
            }
            Context.SetActivity(activityRanking);
        }

        public void PlayMoveAnim()
        {
            Game.Manager.audioMan.TriggerSound("HotAirRankUp");
            Context.PlayMoveAnim(Duration + 0.05f);
            switch (Context.CurType)
            {
                case RankingContext.AnimType.Top:
                    {
                        Game.Instance.StartCoroutineGlobal(DelayInvoke(Duration));
                        break;
                    }
                case RankingContext.AnimType.Normal:
                    {
                        Game.Instance.StartCoroutineGlobal(DelayInvoke(Duration));
                        break;
                    }
                case RankingContext.AnimType.Up:
                    {
                        ScrollTo(_activityRanking.CurRank - 1, Duration, Ease.OutQuad, paddingHead / ViewRect.rect.height, () =>
                        {
                            UIManager.Instance.Block(false);
                            UpdateContents(_allPlayerlist);
                        });
                        break;
                    }
            }
        }

        private IEnumerator DelayInvoke(float delay)
        {
            yield return new WaitForSeconds(delay);
            UpdateContents(_allPlayerlist);
            InvokeCallBack?.Invoke(true);
            UIManager.Instance.Block(false);
        }
    }
}