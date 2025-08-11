/*
 * @Author: tang.yan
 * @Description: 图鉴界面scroll 
 * @Date: 2023-11-17 11:11:50
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using DG.Tweening;

namespace FAT
{
    public class UIHandbookContext
    { }

    public class UIHandbookScrollView : UIFixedSizeScroller<HandbookGroupCellData, UIHandbookContext>
    {
        [SerializeField] private GameObject goGroupItem;
        [SerializeField] private GameObject goChainItem;
        private PoolItemType _groupCellType = PoolItemType.HANDBOOK_GROUP_CELL;
        private PoolItemType _itemCellType = PoolItemType.HANDBOOK_ITEM_CELL;

        public void Setup()
        {
            GameObjectPoolManager.Instance.PreparePool(_groupCellType, goGroupItem);
            GameObjectPoolManager.Instance.PreparePool(_itemCellType, goChainItem);
        }
        
        public void BuildByGroupData(List<HandbookGroupCellData> groupDataList)
        {
            if (groupDataList.Count <= 0)
                return;
            Build();
            var _emptySize = (goGroupItem.transform as RectTransform).rect.height;
            goGroupItem.transform.FindEx("Info/ItemRoot", out GridLayoutGroup _grid);
            var _elementHight = _grid.cellSize.y;
            var _elementSpace = _grid.spacing.y;
            float from, to;
            from = 0f;
            to = from;

            foreach (var groupData in groupDataList)
            {
                var _count = groupData.ItemList.Count;
                var _sizeCount = (_count + 3) / 4;
                // 根据合成链长度计算ui元素尺寸
                var _size = _emptySize + _sizeCount * _elementHight;
                if (_sizeCount > 0)
                    _size += (_sizeCount - 1) * _elementSpace;
                from -= _size;
                _AddGroupItem(groupData, from, to);
                to = from;
            }

            // content
            mContentSize = -from;
            content.sizeDelta = new Vector2(0f, mContentSize);
            content.anchoredPosition = Vector2.zero;
        }

        public void RefreshGroup()
        {
            foreach (var item in mProxyList)
            {
                if (item.trans != null)
                {
                    item.trans.GetComponent<UIGenericItemBase<(HandbookGroupCellData, UIHandbookContext)>>().ForceRefresh();
                }
            }
        }
        
        private void _AddGroupItem(HandbookGroupCellData groupData, float from, float to)
        {
            mProxyList.Add(new FixedSizeItemProxy<HandbookGroupCellData, UIHandbookContext>(groupData, _groupCellType, from, to));
        }
    }
}