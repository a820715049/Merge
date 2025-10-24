// ===================================================
// Author: mengqc
// Date: 2025/09/09
// ===================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MCVineScrollList : MonoBehaviour
    {
        public ScrollRect scrollRect;
        public RectTransform vineContent;
        public GameObject imgTemplate;
        public GameObject vineTemplate;
        public int leafNum = 10;
        public float paddingTop;
        public float paddingBottom;
        public int focusItemIndex;
        [Range(0f, 1f)] public float focusOffset = 0.9f;
        public List<UIVineLeapStepItem> Item { get; } = new();

        private List<RectTransform> _vineList = new();
        private List<RectTransform> _bgList = new();
        private int _cellNum = 4;
        private Vector2 _vineSize;

        private void Awake()
        {
            imgTemplate.SetActive(false);
            vineTemplate.SetActive(false);
            _vineSize = imgTemplate.GetComponent<RectTransform>().sizeDelta;
            _cellNum = vineTemplate.transform.childCount;
        }

        private void Start()
        {
            SetLeafNum(leafNum);
        }

        public void SetLeafNum(int num)
        {
            leafNum = num;
            var pageNum = Mathf.CeilToInt((float)leafNum / _cellNum);
            var contentHeight = pageNum * _vineSize.y;
            if (leafNum % _cellNum > 0)
            {
                contentHeight -= (_cellNum - (leafNum % _cellNum)) * (_vineSize.y / _cellNum);
            }

            contentHeight += paddingTop + paddingBottom;
            vineContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

            for (var i = 0; i < pageNum + 2; i++)
            {
                if (i >= _bgList.Count)
                {
                    var bgImg = Instantiate(imgTemplate, vineContent).GetComponent<RectTransform>();
                    bgImg.gameObject.SetActive(true);
                    bgImg.SetAsLastSibling();
                    _bgList.Add(bgImg);
                }
            }

            for (var i = pageNum + 2; i < _bgList.Count; i++)
            {
                if (i >= _bgList.Count)
                {
                    _bgList[i].gameObject.SetActive(false);
                }
            }

            for (var i = 0; i < pageNum; i++)
            {
                if (i >= _vineList.Count)
                {
                    var vine = Instantiate(vineTemplate, vineContent).GetComponent<RectTransform>();
                    vine.gameObject.name = vineTemplate.name + (i + 1);
                    vine.gameObject.SetActive(true);
                    _vineList.Add(vine);
                }
            }

            for (var i = pageNum; i < _vineList.Count; i++)
            {
                _vineList[i].gameObject.SetActive(false);
            }

            var bottomImg = _bgList[0];
            bottomImg.anchoredPosition = new Vector2(bottomImg.anchoredPosition.x, -_vineSize.y + paddingBottom);
            var topImg = _bgList[pageNum + 1];
            var y = paddingBottom;
            for (var i = 0; i < _vineList.Count; i++)
            {
                var vine = _vineList[i];
                var bg = _bgList[i + 1];
                if (vine.gameObject.activeSelf)
                {
                    vine.anchoredPosition = new Vector2(vine.anchoredPosition.x, y);
                    bg.anchoredPosition = new Vector2(bg.anchoredPosition.x, y);
                    y += _vineSize.y;
                }
            }

            topImg.anchoredPosition = new Vector2(topImg.anchoredPosition.x, y);
            Item.Clear();
            for (var i = 0; i < pageNum; i++)
            {
                var vine = _vineList[i];
                for (var j = 0; j < _cellNum; j++)
                {
                    var n = i * _cellNum + j;
                    var stepItem = vine.GetChild(j).GetComponent<UIVineLeapStepItem>();
                    if (n < leafNum)
                    {
                        stepItem.gameObject.SetActive(true);
                        Item.Add(stepItem);
                    }
                    else
                    {
                        stepItem.gameObject.SetActive(false);
                    }
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }

        public void FocusToItem(int index)
        {
            var item = Item[index];
            var gpos = item.transform.TransformPoint(item.GetComponent<RectTransform>().rect.center);
            var lpos = vineContent.InverseTransformPoint(gpos);
            var contentHeight = vineContent.rect.height;
            var viewportHeight = scrollRect.viewport.rect.height;
            var pos = ((-lpos.y + viewportHeight * (1 - focusOffset)) - viewportHeight) / (contentHeight - viewportHeight);
            pos = 1 - Mathf.Clamp01(pos);
            scrollRect.normalizedPosition = new Vector2(0, pos);
        }

        public Vector3 GetPlayerLocalPosition(RectTransform playerContent, int levelIndex, int index = 0)
        {
            var item = Item[levelIndex];
            return item.GetStandPos(playerContent, index);
        }

        public void SetClickAction(Action<int, UIVineLeapStepItem> action)
        {
            foreach (var item in Item)
            {
                item.SetClickAction(action);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            // SetLeafNum(leafNum);
            // FocusToItem(focusItemIndex);
        }
#endif
    }
}