using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using fat.msg;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;
using Ease = UnityEngine.UI.Extensions.EasingCore.Ease;

namespace FAT
{
    public class MBRankingContext : FancyScrollRectContext
    {
        public ActivityRanking ActivityRanking;
        public int LastOrder;
        private Transform _targetTrans;
        private Dictionary<int, Vector3> cellPos = new();
        public float maxSize;
        public ulong Order => ActivityRanking.Cache.Data?.Me.RankingOrder ?? 0;
        private float _size;
        private RectTransform _content;
        private MBRankingCell _top;
        private MBRankingCell _bottom;
        private MBRankingCell _move;
        private float spacing;
        private bool _blockVisual;
        private float Critical => (_size - spacing) / _content.rect.height;
        private float _offset;
        private float _topOffset;
        public AnimType CurType = AnimType.None;

        public enum AnimType
        {
            None = 0,
            Top = 1,
            Normal = 2,
            Up = 3
        }

        public void UpdateTargetTrans(ulong order, Transform trans)
        {
            if (!ActivityRanking.RankingValid()) return;
            if (order == Order + 1) _targetTrans = trans;
        }

        public void RemoveTarget()
        {
            _targetTrans = null;
        }

        /// <summary>
        /// 设置动画参数
        /// </summary>
        /// <param name="space">cell间距</param>
        public void SetParameter(float space)
        {
            spacing = space;
            _offset = _size / _content.rect.height;
        }

        public void SetActivity(ActivityRanking activityRanking)
        {
            ActivityRanking = activityRanking;
            _top.UpdateContent(this, ActivityRanking.Cache?.Data?.Me);
            _bottom.UpdateContent(this, ActivityRanking.Cache?.Data?.Me);
            _move.UpdateContent(this, ActivityRanking.Cache?.Data?.Me);
        }

        /// <summary>
        /// 是否是玩家本身
        /// </summary>
        /// <param name="id">cell自身持有的id</param>
        /// <returns></returns>
        public bool MatchPlayer(ulong id)
        {
            if (ActivityRanking.Cache == null || ActivityRanking.Cache.Data == null) return false;
            return ActivityRanking.Cache.Data.Me.RankingOrder == id;
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
        public void SetCell(MBRankingCell top, MBRankingCell bottom, MBRankingCell move)
        {
            _top = top;
            _top.Initialize();
            _bottom = bottom;
            _bottom.Initialize();
            _move = move;
            _move.Initialize();
        }

        /// <summary>
        /// 设置是否阻止滑动动画
        /// </summary>
        /// <param name="block">是否阻止，true为阻止动画</param>
        public void SetBlock(bool block)
        {
            _blockVisual = block;
        }

        public void UpdateVisual(float pos)
        {
            if (_blockVisual) return;
            if (pos < Critical)
            {
                _top.transform.localScale = Vector3.one * Mathf.Lerp(1f, maxSize, (_offset - pos) / Critical);
                _bottom.transform.localScale = Vector3.zero;
            }
            else if (pos > 1 - (_size - 2 * spacing) / _content.rect.height)
            {
                _top.transform.localScale = Vector3.zero;
                _bottom.transform.localScale = Vector3.one * Mathf.Lerp(1f, maxSize, (pos - 1 + _offset) / Critical);
            }
            else
            {
                _top.transform.localScale = Vector3.zero;
                _bottom.transform.localScale = Vector3.zero;
            }
        }

        public void CheckType()
        {
            var maxShow = (int)(_content.rect.height / _size);
            if (LastOrder <= maxShow) CurType = AnimType.Top;
            else if (LastOrder - (int)Order <= maxShow - 1) CurType = AnimType.Normal;
            else CurType = AnimType.Up;
        }

        public void PlayMoveAnim(float duration)
        {
            _top.transform.localScale = Vector3.zero;
            _bottom.transform.localScale = Vector3.zero;
            _move.transform.localScale = Vector3.one;
            var maxShow = (int)(_content.rect.height / _size);
            switch (CurType)
            {
                case AnimType.Up:
                {
                    _move.transform.DOLocalMove(_top.transform.localPosition - Vector3.up * _size, duration)
                        .From(_bottom.transform.localPosition)
                        .SetEase(DG.Tweening.Ease.InOutCirc)
                        .OnStepComplete(() => _move.transform.localScale = Vector3.zero);
                    break;
                }
                case AnimType.Top:
                {
                    var startPos = _top.transform.localPosition - LastOrder * Vector3.up * _size;
                    var offsetNum = (int)Order;
                    var offset = ActivityRanking.Cache.Data.Players.Count <= maxShow
                        ? Vector3.down * spacing
                        : Vector3.zero;
                    _move.transform
                        .DOLocalMove(
                            _top.transform.localPosition - offsetNum * Vector3.up * _size + offset,
                            duration)
                        .From(startPos)
                        .SetEase(DG.Tweening.Ease.InOutCirc)
                        .OnStepComplete(() => _move.transform.localScale = Vector3.zero);
                    break;
                }
                case AnimType.Normal:
                {
                    var offsetNum = LastOrder - (int)Order;
                    _move.transform.DOLocalMove(_bottom.transform.localPosition + offsetNum * Vector3.up * _size,
                            duration)
                        .From(_bottom.transform.localPosition)
                        .SetEase(DG.Tweening.Ease.InOutCirc)
                        .OnStepComplete(() => _move.transform.localScale = Vector3.zero);
                    break;
                }
            }
        }
    }

    public class MBRankingScroll : FancyScrollRect<PlayerRankingInfo, MBRankingContext>
    {
        public float duration;
        public float maxSize;
        public GameObject setCellPrefab;
        public float setCellSize;
        public MBRankingCell topVisual;
        public MBRankingCell bottomVisual;
        public MBRankingCell moveVisual;
        public RectTransform viewRect;
        public GameObject bg;
        public GameObject viewPort;
        public GameObject emptyNode;
        public UIImageRes tokenImageRes;
        public TextMeshProUGUI tokenNum;
        public TextMeshProUGUI tips;
        public GameObject specialNode;
        protected override GameObject CellPrefab => setCellPrefab;
        protected override float CellSize => setCellSize;
        protected ulong RankIndex = 0;
        protected Action<bool> Invoke;
        private readonly List<PlayerRankingInfo> _trueList = new();
        private bool _duringAnim;
        private bool _newActivity;
        private int _listCount;

        public void InitContext(ActivityRanking activityRanking)
        {
            Context.maxSize = maxSize;
            Context.SetView(setCellSize, viewRect, paddingHead);
            Context.SetCell(topVisual, bottomVisual, moveVisual);
            Context.SetParameter(paddingHead);
            if (Context.ActivityRanking == null || Context.ActivityRanking != activityRanking)
            {
                Context.SetActivity(activityRanking);
                _newActivity = true;
            }
        }

        public void OnEnable()
        {
            _trueList.Clear();
            UpdateContents(_trueList);
        }

        public void UpdateInfoList(ActivityRanking activityRanking, Action<bool> invoke = null)
        {
            if (_duringAnim) return;
            if (IsEmpty()) return;
            if (Context.ActivityRanking.Cache.Data == null || Context.ActivityRanking.Cache.Data.Me == null ||
                Context.ActivityRanking.Cache.Data.Players == null) return;
            _duringAnim = true;
            _trueList.Clear();
            _trueList.AddRange(Context.ActivityRanking.Cache.Data.Players);
            Invoke = invoke;
            if (!_trueList.Any() || _newActivity)
            {
                Context.SetBlock(true);
                UpdateContents(_trueList);
                _duringAnim = false;
                invoke?.Invoke(true);
                _listCount = _trueList.Count;
                _newActivity = false;
                RankIndex = Context.Order;
                Context.LastOrder = (int)RankIndex;
                ScrollTo((int)RankIndex - 1, 0.1f, 0.5f, () => { Context.SetBlock(false); });
                return;
            }

            if (_listCount > _trueList.Count)
            {
                Context.SetActivity(activityRanking);
                UpdateContents(_trueList);
                _duringAnim = false;
                invoke?.Invoke(true);
                _listCount = _trueList.Count;
                RankIndex = Context.Order;
                Context.LastOrder = (int)RankIndex;
                return;
            }

            _listCount = _trueList.Count;
            Context.SetActivity(activityRanking);
            if (RankIndex > 0 && RankIndex > Context.Order)
            {
                Context.LastOrder = (int)RankIndex;
                CheckAnim();
            }
            else
            {
                Context.SetBlock(true);
                UpdateContents(_trueList);
                RankIndex = Context.Order;
                ScrollTo((int)RankIndex - 1, 0.1f, 0.5f, () => { Context.SetBlock(false); });
                invoke?.Invoke(true);
                _duringAnim = false;
                Context.LastOrder = (int)RankIndex;
            }
        }

        public void CheckAnim()
        {
            Context.SetBlock(true);
            Context.CheckType();
            var origin = _trueList.Where(kv => kv.RankingOrder != Context.Order);
            var list = Enumerable.ToList(origin.Select(x => x));
            if ((int)RankIndex - 1 > Context.ActivityRanking.Cache.Data.Players.Count)
            {
                UpdateContents(_trueList);
                RankIndex = Context.Order;
                Context.SetBlock(false);
                _duringAnim = false;
                return;
            }

            list.Insert((int)RankIndex - 1, new PlayerRankingInfo());
            UpdateContents(list);
            UIManager.Instance.Block(true);
            ScrollTo((int)RankIndex - 1, 0f, 1f, () => PlayMoveAnim());
        }

        /// <summary>
        /// 判断是否开启排行榜，以及更新UI显示
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            var isEmpty = Context.ActivityRanking.RankingValid();
            emptyNode.SetActive(!isEmpty);
            viewPort.SetActive(isEmpty);
            tips.text = I18N.FormatText("#SysComDesc630", Context.ActivityRanking.confD.ScoreNum,
                UIUtility.FormatTMPString(Context.ActivityRanking.confD.RequireScoreId));
            tokenImageRes.SetImage(Game.Manager.objectMan.GetBasicConfig(Context.ActivityRanking.confD.RequireScoreId)
                .Image);
            tokenNum.text = Context.ActivityRanking.RankingScore.ToString();
            return !isEmpty;
        }

        public void PlayMoveAnim()
        {
            RankIndex = Context.Order;
            Game.Manager.audioMan.TriggerSound("HotAirRankUp");
            Context.PlayMoveAnim(duration + 0.05f);
            switch ((int)Context.CurType)
            {
                case 1:
                {
                    Game.Instance.StartCoroutineGlobal(DelayInvoke(duration));
                    break;
                }
                case 2:
                {
                    Game.Instance.StartCoroutineGlobal(DelayInvoke(duration));
                    break;
                }
                case 3:
                {
                    ScrollTo((int)RankIndex - 1, duration, Ease.OutQuad,
                        spacing / viewRect.rect.height, () =>
                        {
                            Context.SetBlock(false);
                            UpdateContents(_trueList);
                            Invoke?.Invoke(true);
                            _duringAnim = false;
                            UIManager.Instance.Block(false);
                        });
                    break;
                }
            }
        }

        private IEnumerator DelayInvoke(float delay)
        {
            yield return new WaitForSeconds(delay);
            Context.SetBlock(false);
            UpdateContents(_trueList);
            Invoke?.Invoke(true);
            _duringAnim = false;
            UIManager.Instance.Block(false);
        }
    }
}