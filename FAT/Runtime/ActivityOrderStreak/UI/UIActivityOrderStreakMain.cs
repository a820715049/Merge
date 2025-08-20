/**
 * @Author: zhangpengjian
 * @Date: 2025/6/30 14:11:23
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/6/30 14:11:23
 * Description: 连续订单活动主界面
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityOrderStreakMain : UIBase
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnHelp;
        [SerializeField] private Button btnGo;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI desc;
        [SerializeField] private TextMeshProUGUI time;
        [SerializeField] private TextMeshProUGUI reward;
        [SerializeField] private float duration;
        [SerializeField] private Transform fish;
        [SerializeField] private Transform box;
        [SerializeField] private Button btnBox;
        [SerializeField] private SkeletonGraphic fishSpine;
        [SerializeField] private AnimationCurve curve;
        [SerializeField] private GameObject cell;
        [SerializeField] private Transform cellRoot;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private GameObject block;
        [SerializeField] private Transform boxPos;
        [SerializeField] private Animator boxAnim;

        private ActivityOrderStreak _activity;
        private List<RewardCommitData> _reward;
        private bool _needAnim;
        private List<GameObject> cellList = new();
        private bool _isCurrentOrderVisible = true;

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(OnClickClose);
            btnHelp.onClick.AddListener(OnClickHelp);
            btnGo.onClick.AddListener(OnClickGo);
            btnBox.onClick.AddListener(OnClickBox);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.ORDER_STREAK_CELL, cell);
            
            scroll.onValueChanged.AddListener(OnScrollValueChanged);
        }

        private void OnScrollValueChanged(Vector2 value)
        {
            CheckCurrentOrderVisibility();
        }

        private void CheckCurrentOrderVisibilityDelayed()
        {
            StartCoroutine(CoCheckCurrentOrderVisibility());
        }

        private IEnumerator CoCheckCurrentOrderVisibility()
        {
            // 等待一帧，确保布局更新完成
            yield return null;
            CheckCurrentOrderVisibility();
        }

        /// <summary>
        /// 检查当前订单cell是否在scroll视野范围内
        /// </summary>
        private void CheckCurrentOrderVisibility()
        {
            if (_activity == null || _activity.OrderIdx < 0 || _activity.OrderIdx >= cellList.Count)
                return;

            var currentCell = cellList[_activity.OrderIdx];
            if (currentCell == null) return;

            // 强制更新Canvas，确保坐标计算正确
            Canvas.ForceUpdateCanvases();

            var cellRectTransform = currentCell.GetComponent<RectTransform>();
            var isVisible = IsRectTransformVisibleInScrollView(cellRectTransform, scroll);

            if (isVisible != _isCurrentOrderVisible)
            {
                _isCurrentOrderVisible = isVisible;
                fish.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(isVisible);
            }
        }

        /// <summary>
        /// 检查RectTransform是否在ScrollRect的视野范围内
        /// </summary>
        private bool IsRectTransformVisibleInScrollView(RectTransform target, ScrollRect scrollRect)
        {
            if (target == null || scrollRect == null) return false;

            // 获取目标RectTransform的世界坐标边界
            Vector3[] targetCorners = new Vector3[4];
            target.GetWorldCorners(targetCorners);

            // 获取ScrollRect视口的世界坐标边界
            Vector3[] viewportCorners = new Vector3[4];
            scrollRect.viewport.GetWorldCorners(viewportCorners);

            // 检查目标是否在视口范围内
            float targetMinY = Mathf.Min(targetCorners[0].y, targetCorners[1].y);
            float targetMaxY = Mathf.Max(targetCorners[2].y, targetCorners[3].y);
            float viewportMinY = Mathf.Min(viewportCorners[0].y, viewportCorners[1].y);
            float viewportMaxY = Mathf.Max(viewportCorners[2].y, viewportCorners[3].y);

            // 计算目标的高度和视口的高度
            float targetHeight = targetMaxY - targetMinY;
            
            // 计算重叠部分
            float overlapMinY = Mathf.Max(targetMinY, viewportMinY);
            float overlapMaxY = Mathf.Min(targetMaxY, viewportMaxY);
            float overlapHeight = Mathf.Max(0, overlapMaxY - overlapMinY);
            
            // 如果重叠部分超过目标高度的70%，则认为可见
            bool isVisible = overlapHeight > targetHeight * 0.7f;
            
            return isVisible;
        }

        private void OnClickBox()
        {
            boxAnim.SetTrigger("Tips");
            var list = Enumerable.ToList(_activity.confD.Reward.Select(s => s.ConvertToRewardConfig()));
            UIManager.Instance.OpenWindow(UIConfig.UIActivityOrderStreakRewardTips, boxPos.transform.position, 35f, list, true);
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as ActivityOrderStreak;
            if (items.Length > 1 && items[1] != null)
            {
                _needAnim = (bool)items[1];
                if (items.Length > 2 && items[2] != null)
                {
                    _reward = (List<RewardCommitData>)items[2];
                }
            }
        }

        protected override void OnPreOpen()
        {
            OnOneSecond();
            RefreshList();
            block.SetActive(false);
            if (_needAnim)
            {
                fish.transform.SetParent(cellRoot.GetChild(_activity.OrderIdx - 1));
                fish.localPosition = new Vector3(454, 0, 0);
                fishSpine.AnimationState.SetAnimation(0, "idle", true);
                OnMoveFish();
            }
            else
            {
                fish.transform.SetParent(cellRoot.GetChild(_activity.OrderIdx));
                fish.localPosition = new Vector3(454, 0, 0);
                fishSpine.AnimationState.SetAnimation(0, "idle", true);
            }
            _isCurrentOrderVisible = true;
        }

        private void RefreshList()
        {
            var dic = Game.Manager.mainMergeMan.worldTracer.GetCurrentActiveBoardAndInventoryItemCount();
            var list = _activity.OrderList;
            cellList.Clear();
            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, (220 + 60) * list.Count);
            scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, 1f);
            for (int i = 0; i < list.Count; i++)
            {
                var cellSand = GameObjectPoolManager.Instance.CreateObject(PoolItemType.ORDER_STREAK_CELL, cellRoot.transform);
                var c = list[i];
                cellSand.GetComponent<UIActivityOrderStreakCell>().UpdateContent(i, c, _needAnim ? _activity.OrderIdx - 1 : _activity.OrderIdx, dic, i == list.Count - 1);
                cellList.Add(cellSand);
            }

            // 滚动到当前正在完成的条目
            ScrollToCurrentOrder();
            
            // 延迟检查当前订单的可见性
            CheckCurrentOrderVisibilityDelayed();
        }

        private void ScrollToCurrentOrder()
        {
            if (_activity.OrderIdx >= 0 && _activity.OrderIdx < _activity.OrderList.Count)
            {
                // 计算目标位置：每个 cell 高度为 220 + 60 = 280
                float cellHeight = 280f;
                float targetY = _activity.OrderIdx * cellHeight;

                // 计算 normalized position (0-1 范围)
                float contentHeight = scroll.content.sizeDelta.y;
                float viewportHeight = scroll.viewport.rect.height;
                float maxScroll = contentHeight - viewportHeight;

                if (maxScroll > 0)
                {
                    float centerOffset = viewportHeight * 0.5f;
                    float adjustedTargetY = targetY - centerOffset;
                    float normalizedTargetY = 1f - (adjustedTargetY / maxScroll);
                    normalizedTargetY = Mathf.Clamp01(normalizedTargetY);
                    scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, normalizedTargetY);
                }
            }
            else
            {
                scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, 0f);
            }
        }

        protected override void OnPostClose()
        {
            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.ORDER_STREAK_CELL, item);
            }
            cellList.Clear();
            _reward = null;
            _needAnim = false;
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(OnOneSecond);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(OnOneSecond);
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act is ActivityOrderStreak)
            {
                Close();
            }
        }

        private void OnOneSecond()
        {
            if (_activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            time.SetCountDown(diff);
        }

        private void OnMoveFish()
        {
            block.SetActive(true);
            var p = cellRoot.GetChild(_activity.OrderIdx - 1);
            p.GetComponent<UIActivityOrderStreakCell>().PlayCurrent2Finish();
            StartCoroutine(CoPlayEfx());
        }

        private IEnumerator CoPlayEfx()
        {
            yield return new WaitForSeconds(0.4f);
            if (_activity.OrderIdx < _activity.OrderList.Count)
            {
                cellRoot.GetChild(_activity.OrderIdx).GetComponent<UIActivityOrderStreakCell>().PlayEfx();
                yield return new WaitForSeconds(0.36f);
                var p1 = cellRoot.GetChild(_activity.OrderIdx);
                p1.GetComponent<UIActivityOrderStreakCell>().PlayFuture2Current();
                fishSpine.AnimationState.SetAnimation(0, "move", false).Complete += delegate (TrackEntry entry)
                {
                    fishSpine.AnimationState.SetAnimation(0, "idle", true);
                };
                Game.Manager.audioMan.TriggerSound("OrderStreakDown");
                var currentLocalPos = fish.localPosition;
                var to = new Vector3(currentLocalPos.x, currentLocalPos.y - 280f, currentLocalPos.z);
                fish.DOLocalMove(to, duration)
                    .SetEase(curve)
                    .OnComplete(OnMoveFishComplete);
            }
            else
            {
                StartCoroutine(CoShowReward());
            }
        }

        private IEnumerator CoShowReward()
        {
            boxAnim.SetTrigger("Punch");
            Game.Manager.audioMan.TriggerSound("OrderStreakWin");
            yield return new WaitForSeconds(1f);
            var pos = box.position;
            var image = _activity.conf.ChestTheme;
            UIManager.Instance.OpenWindow(UIConfig.UIActivityReward, pos, _reward, image, I18N.Text("#SysComDesc1334"));
            StartCoroutine(CoWaitReward());
            block.SetActive(false);
        }

        private void OnMoveFishComplete()
        {
            var p = cellRoot.GetChild(_activity.OrderIdx);
            fish.transform.SetParent(p);
            fish.localPosition = new Vector3(454, 0, 0);
            
            // 向下滚动一点，显示更多内容
            float currentY = scroll.normalizedPosition.y;
            float targetY = Mathf.Max(0f, currentY - 0.1f); // 向下滚动 10% 的距离
            DOTween.To(() => currentY, x => scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, x), targetY, 0.5f)
                .SetEase(Ease.OutQuad).OnComplete(() =>
                {
                    block.SetActive(false);
                    // 检查当前订单的可见性
                    CheckCurrentOrderVisibility();
                });
        }

        private IEnumerator CoWaitReward()
        {
            yield return new WaitForSeconds(1f);
            Close();
        }

        private void OnClickClose()
        {
            Close();
        }

        private void OnClickHelp()
        {
            _activity.VisualResHelp.res.ActiveR.Open();
        }

        private void OnClickGo()
        {
            if (Game.Manager.mapSceneMan.scene.Active)
            {
                GameProcedure.SceneToMerge();
                Close();
            }
            else
            {
                Close();
            }
        }
    }
}