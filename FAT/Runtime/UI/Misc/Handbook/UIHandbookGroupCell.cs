/*
 * @Author: tang.yan
 * @Description: 图鉴界面链条组cell 
 * @Date: 2023-11-17 11:11:13
 */
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class HandbookGroupCellData
    {
        public int Index;       //界面index
        public int SeriesId;     //链条组id
        public int UnlockNum;   //当前已解锁(包括已领奖和未领奖)的棋子数量
        public List<int> ItemList;  //链条组里包含的所有棋子id 顺序为配置顺序和棋子在链条上的顺序
    }
    
    public class UIHandbookGroupCell : UIGenericItemBase<(HandbookGroupCellData groupCellData, UIHandbookContext context)>
    {
        [SerializeField] private TMP_Text groupName;
        [SerializeField] private TMP_Text collectInfo;
        [SerializeField] private Transform itemRoot;
        
        private PoolItemType _itemCellType = PoolItemType.HANDBOOK_ITEM_CELL;

        protected override void InitComponents()
        {
            
        }
        
        protected override void UpdateOnDataChange()
        {
            _Refresh();
        }

        protected override void UpdateOnForce()
        {
            _RefreshChain();
        }
        
        protected override void UpdateOnDataClear()
        {
            _Clear();
        }

        private void _Refresh()
        {
            if (mData.groupCellData == null)
                return;
            _Clear();
            var groupConfig = Game.Manager.mergeItemMan.GetCategoryConfig(mData.groupCellData.SeriesId);
            if (groupConfig == null)
                return;
            groupName.text = I18N.Text(groupConfig.Name);
            collectInfo.text = mData.groupCellData.UnlockNum + "/" + mData.groupCellData.ItemList.Count;
            _ShowChain();
        }

        private void _ShowChain()
        {
            if (mData.groupCellData == null)
                return;
            foreach (var itemId in mData.groupCellData.ItemList)
            {
                var go = GameObjectPoolManager.Instance.CreateObject(_itemCellType, itemRoot);
                go.GetComponent<UIGenericItemBase<int>>().SetData(itemId);
                go.SetActive(true);
            }
        }

        private void _RefreshChain()
        {
            for (int i = itemRoot.childCount - 1; i >= 0; --i)
            {
                var item = itemRoot.GetChild(i);
                item.GetComponent<UIGenericItemBase<int>>().ForceRefresh();
            }
        }
        
        private void _Clear()
        {
            UIUtility.ReleaseClearableItem(itemRoot, _itemCellType);
        }
        
    }
}
