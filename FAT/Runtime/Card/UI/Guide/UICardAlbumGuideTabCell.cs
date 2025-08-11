/**
 * @Author: zhangpengjian
 * @Date: 2024-01-16 17:25:51
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/25 18:27:38
 * Description: 集卡教学界面tabCell
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class CardAlbumGuideTabCellData
    {
        public int Index;
        public bool IsSelect;
        public Action<int> OnClickCb;
    }

    public class UICardAlbumGuideTabCell : FancyScrollRectCell<CardAlbumGuideTabCellData, UICommonScrollRectDefaultContext>
    {

        [SerializeField] private GameObject normalGo;
        [SerializeField] private GameObject selectGo;
        [SerializeField] private Button clickBtn;

        private CardAlbumGuideTabCellData _curCellData;

        public override void Initialize()
        {
            clickBtn.onClick.AddListener(_OnClickBtnTab);
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        public override void UpdateContent(CardAlbumGuideTabCellData tabCellData)
        {
            if (tabCellData == null)
                return;
            _curCellData = tabCellData;
            normalGo.SetActive(!_curCellData.IsSelect);
            selectGo.SetActive(_curCellData.IsSelect);
        }

        private void _OnClickBtnTab()
        {
            if (_curCellData != null)
            {
                _curCellData.OnClickCb?.Invoke(_curCellData.Index);
            }
        }
    }
}