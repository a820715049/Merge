/**
 * @Author: zhangpengjian
 * @Date: 2024-01-16 10:14:02
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/25 18:26:22
 * Description: 卡册教学/信息介绍（i图标入口）界面
 */

using System.Collections.Generic;
using UnityEngine;
using EL;
using fat.rawdata;

namespace FAT
{
    public class UIAlbumGuide : UIBase
    {
        [SerializeField] private UICardAlbumGuideTabScroll tabScrollView;
        [SerializeField] private int maxTabNum; //最大tab数
        [SerializeField] private MBHelpGuideDrag drag;
        [SerializeField] private Transform pageRoot;
        [SerializeField] private Transform pageSpecial;
        private int _curSelectTabId = 0;
        private List<CardAlbumGuideTabCellData> _tabCellDataList = new List<CardAlbumGuideTabCellData>();   //底部页签cell数据
        private int page;
        private int _maxShowTabNum;  // 实际显示的最大页签数

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/BtnClose", base.Close).FixPivot();
            tabScrollView.InitLayout();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                page = (int)items[0];
            }
        }

        protected override void OnPreOpen()
        {
            _maxShowTabNum = maxTabNum;
            if (!Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureGiveCard))
            {
                _maxShowTabNum = maxTabNum - 1;
                pageSpecial.gameObject.SetActive(false);
                pageSpecial.SetParent(pageRoot.parent);
            }
            else
            {
                pageSpecial.gameObject.SetActive(true);
                pageSpecial.SetParent(pageRoot);
            }
            _tabCellDataList.Clear();
            for (int i = 0; i < _maxShowTabNum; i++)
            {
                CardAlbumGuideTabCellData data = new CardAlbumGuideTabCellData()
                {
                    Index = i + 1,
                    IsSelect = i == _curSelectTabId,
                    OnClickCb = OnTabBtnClick,
                };
                _tabCellDataList.Add(data);
            }
            var p = page > 0 ? page : 1;
            drag.Init(p);
            RefreshTabScroll();
            OnTabBtnClick(p);
            page = 0;
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_HELP_GUIDE_DRAG_END>().AddListener(OnTabDragxEnd);
        }

        protected override void OnRefresh()
        {
            RefreshTabScroll();
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_HELP_GUIDE_DRAG_END>().RemoveListener(OnTabDragxEnd);
        }

        private void OnTabDragxEnd(int targeIdx)
        {
            if (targeIdx < 1 || targeIdx > _maxShowTabNum)
            {
                return;
            }
            _curSelectTabId = targeIdx - 1;
            _tabCellDataList[_curSelectTabId].OnClickCb?.Invoke(_tabCellDataList[_curSelectTabId].Index);
        }

        private void OnTabBtnClick(int index)
        {
            if (_curSelectTabId != index)
            {
                _curSelectTabId = index;
                RefreshTabScroll();
            }
        }

        private void RefreshTabScroll()
        {
            //更新页签的选中状态
            foreach (var data in _tabCellDataList)
            {
                data.IsSelect = data.Index == _curSelectTabId;
            }
            tabScrollView.UpdateData(_tabCellDataList);
        }
    }
}