/*
 * @Author: pengjian.zhang
 * @Description: 寻宝教学界面tabCell
 * @Date: 2024-04-23 20:15:02
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class TreasureHuntHelpTabCellData
    {
        public int Index;
        public bool IsSelect;
        public Action<int> OnClickCb;
    }

    public class
        UITreasureHuntHelpTabCell : FancyScrollRectCell<TreasureHuntHelpTabCellData, UICommonScrollRectDefaultContext>
    {
        [SerializeField] private GameObject normalGo;
        [SerializeField] private GameObject selectGo;
        [SerializeField] private Button clickBtn;

        private TreasureHuntHelpTabCellData _curCellData;

        public override void Initialize()
        {
            clickBtn.onClick.AddListener(_OnClickBtnTab);
        }

        public override void UpdateContent(TreasureHuntHelpTabCellData tabCellData)
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