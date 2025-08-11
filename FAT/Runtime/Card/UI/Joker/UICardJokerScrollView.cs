/*
 * @Author: tang.yan
 * @Description: 万能卡选卡界面scroll 
 * @Date: 2024-03-29 12:03:34
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using DG.Tweening;

namespace FAT
{
    public class UICardJokerContext
    { }

    public class UICardJokerScrollView : UIFixedSizeScroller<CardJokerGroupCellData, UICardJokerContext>
    {
        [SerializeField] private GameObject goGroupItem;
        [SerializeField] private GameObject goChainItem;
        private PoolItemType _groupCellType = PoolItemType.CARD_JOKER_GROUP_CELL;
        private PoolItemType _itemCellType = PoolItemType.CARD_JOKER_ITEM_CELL;

        public void Setup()
        {
            GameObjectPoolManager.Instance.PreparePool(_groupCellType, goGroupItem);
            GameObjectPoolManager.Instance.PreparePool(_itemCellType, goChainItem);
        }
        
        public void BuildByGroupData(List<CardJokerGroupCellData> groupCellDataDict)
        {
            if (groupCellDataDict.Count <= 0)
                return;
            Build();
            var _emptySize = (goGroupItem.transform as RectTransform).rect.height;
            goGroupItem.transform.FindEx("Info/ItemRoot", out GridLayoutGroup _grid);
            var _elementHight = _grid.cellSize.y;
            var _elementSpace = _grid.spacing.y;
            float from, to;
            from = 0f;
            to = from;

            foreach (var cellData in groupCellDataDict)
            {
                var _count = cellData.CardIdList.Count;
                var _sizeCount = (_count + 2) / 3;
                //计算ui元素尺寸
                var _size = _emptySize + _sizeCount * _elementHight;
                if (_sizeCount > 0)
                    _size += (_sizeCount - 1) * _elementSpace;
                from -= _size;
                _AddGroupItem(cellData, from, to);
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
                    item.trans.GetComponent<UIGenericItemBase<(CardJokerGroupCellData, UICardJokerContext)>>().ForceRefresh();
                }
            }
        }


        private void _AddGroupItem(CardJokerGroupCellData cellData, float from, float to)
        {
            mProxyList.Add(new FixedSizeItemProxy<CardJokerGroupCellData, UICardJokerContext>(cellData, _groupCellType, from, to));
        }
    }
}