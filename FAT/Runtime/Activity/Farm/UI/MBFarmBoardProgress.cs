// ================================================
// File: MBFarmBoardProgress.cs
// Author: yueran.li
// Date: 2025/04/24 20:31:23 星期四
// Desc: 农场棋盘进度条
// ================================================

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FAT.Merge;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBFarmBoardProgress : MonoBehaviour
    {
        public GameObject _Item;

        [SerializeField] private RectMask2D _mask;
        [SerializeField] private Transform _content;
        [SerializeField] private ScrollRect scrollRect;

        // private bool _hasInit;
        // private float _delay = 0.16f;
        private List<MBFarmBoardProgressItem> _list = new();

        private float itemWidth;

        // 活动实例
        private FarmBoardActivity _activity;

        public void SetUpOnCreate()
        {
            itemWidth = _Item.GetComponent<LayoutElement>().preferredWidth;
        }

        public void InitOnPreOpen(FarmBoardActivity act)
        {
            _activity = act;
            Init();
        }

        private void Init()
        {
            var mileStone = GetMileStoneItems();
            for (var i = 0; i < mileStone.Count; i++)
            {
                if (_list.Count > i)
                {
                    _list[i].Refresh(_activity, i);
                    continue;
                }

                var progressItem = Instantiate(_Item, _content.transform)
                    .GetComponent<MBFarmBoardProgressItem>();
                progressItem.gameObject.SetActive(true);
                progressItem.Init(mileStone[i], i, _activity);
                _list.Add(progressItem);
            }
        }

        private List<int> GetMileStoneItems()
        {
            var idList = _activity.GetAllItemIdList();
            return idList;
        }


        public void RefreshProgress()
        {
            if (_activity == null)
            {
                return;
            }

            var count = GetMileStoneItems().Count; // 进度条动画
            var index = _activity.UnlockMaxLevel;
            var right = index == 0 ? CalProgressRight(count, -1) : CalProgressRight(count, index - 1);

            _mask.padding = new Vector4(0, 0, right, 0);
        }

        private float CalProgressRight(int count, int index)
        {
            float right = 0;
            Canvas.ForceUpdateCanvases();

            // 获取mask相对于canvas的缩放
            var maskRect = _mask.GetComponent<RectTransform>();
            var canvas = _mask.GetComponentInParent<Canvas>();
            var maskScale = maskRect.lossyScale;
            var canvasScale = canvas.transform.lossyScale;
            var relativeScale = new Vector3(
                maskScale.x / canvasScale.x,
                maskScale.y / canvasScale.y,
                maskScale.z / canvasScale.z
            );

            var max = ((RectTransform)_content.transform).rect.width;

            var hor = _content.GetComponent<HorizontalLayoutGroup>();
            if (max == 0)
            {
                max = hor.padding.left + hor.padding.right + (hor.spacing * (count - 1)) + (itemWidth * count);
            }

            if (index < 0)
            {
                return max * relativeScale.x;
            }

            // 第一个
            if (index == 0)
            {
                // 考虑缩放因素计算实际宽度
                right = max - itemWidth / 2f;
                right *= relativeScale.x; // 应用水平缩放
                return right;
            }

            // 最后一个
            if (index == count - 1)
            {
                return 0;
            }

            if (count > 1)
            {
                // 考虑缩放因素计算实际宽度
                right = (itemWidth + hor.spacing) * (count - index - 1) + itemWidth / 2f;
                right *= relativeScale.x; // 应用水平缩放
            }

            right = Mathf.Clamp(right, 0, max);

            return right;
        }

        private void ProgressAnim(float to, Action onComplete = null)
        {
            // 进度条动画只能单向改变
            if (to > _mask.padding.z)
            {
                onComplete?.Invoke();
                return;
            }

            DOTween.To(() => _mask.padding, x => _mask.padding = x,
                    new Vector4(0, 0, to, 0), 1f)
                .OnComplete(() => { onComplete?.Invoke(); });
        }

        // 从棋子位置飞图标到进度条 进度条item Punch
        public IEnumerator CoUnlockItem(Item item)
        {
            var index = GetMileStoneItems().IndexOf(item.config.Id);
            ScrollToItem(index);
            // yield return new WaitForSeconds(0.5f);

            var from = BoardViewManager.Instance.CoordToWorldPos(item.coord);
            if (index == -1) yield break;

            // 进度条动画
            var right = CalProgressRight(GetMileStoneItems().Count, index);

            ProgressAnim(right, () =>
            {
                UIFlyUtility.FlyCustom(item.config.Id, 1, from, _list[index].transform.position, FlyStyle.Common,
                    FlyType.None, () => { _list[index].PlayUnlock(); }, size: 136f);
            });
        }

        // 云层解锁之后的进度条表现
        public IEnumerator CoUnlockItemAfterCloud(Item item)
        {
            var index = GetMileStoneItems().IndexOf(item.config.Id);
            if (index == -1) yield break;

            ScrollToItem(index);
            yield return new WaitForSeconds(0.5f);

            // 进度条动画
            var right = CalProgressRight(GetMileStoneItems().Count, index);
            ProgressAnim(right, () => { _list[index].PlayUnlock(); });
        }

        /// <summary>
        /// 获取指定索引的项的RectTransform
        /// </summary>
        private RectTransform GetItemAtIndex(int index)
        {
            // 根据您的UI结构获取指定索引的项的RectTransform
            // 示例实现，您需要根据实际UI结构调整
            if (index < 0 || index >= scrollRect.content.childCount) return null;

            return scrollRect.content.GetChild(index) as RectTransform;
        }

        /// <summary>
        /// 将滚动视图滚动到最新解锁的棋子，使其居中显示
        /// </summary>
        public void ScrollToItem(int itemIndex)
        {
            if (itemIndex < 0)
            {
                itemIndex = 0;
            }

            var totalItems = GetMileStoneItems().Count;

            if (itemIndex >= totalItems)
            {
                itemIndex = totalItems - 1;
            }

            // 获取目标项的RectTransform
            var targetItem = GetItemAtIndex(itemIndex);
            if (targetItem == null) return;

            // 计算目标位置，使item居中
            float targetHorizontalPosition;

            if (itemIndex == 0)
            {
                // 第一个item，滚动到最左
                targetHorizontalPosition = 0;
            }
            else if (itemIndex == totalItems - 1)
            {
                // 最后一个item，滚动到最右
                targetHorizontalPosition = 1;
            }
            else
            {
                // 其他情况，居中显示
                var hor = _content.GetComponent<HorizontalLayoutGroup>();
                float width = itemWidth + hor.spacing; // 获取单个项的宽度（包含间距）
                float contentWidth = scrollRect.content.rect.width;
                if (contentWidth == 0)
                {
                    // left + right + (n-1)*space + n*itemWidth
                    contentWidth = hor.padding.left + hor.padding.right + totalItems * itemWidth +
                                   (totalItems - 1) * hor.spacing;
                }

                float viewportWidth = scrollRect.viewport.rect.width;

                // 计算目标项中心位置
                float itemCenter = (itemIndex * width) + (width / 2);

                // 计算归一化的位置（0-1之间的值）
                targetHorizontalPosition = (itemCenter - (viewportWidth / 2)) / (contentWidth - viewportWidth);

                // 确保位置在0-1之间
                targetHorizontalPosition = Mathf.Clamp01(targetHorizontalPosition);
            }

            // 使用DOTween平滑滚动到目标位置
            DOTween.To(
                () => scrollRect.horizontalNormalizedPosition,
                x => scrollRect.horizontalNormalizedPosition = x,
                targetHorizontalPosition,
                1f
            ).SetEase(Ease.OutCubic);
        }
    }
}