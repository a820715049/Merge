/*
 * @Author: tang.yan
 * @Description: 背包格子组cell 
 * @Date: 2023-11-01 10:11:51
 */

using System;
using EL;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using System.Collections.Generic;
using Config;
using TMPro;

namespace FAT
{
    public class HandbookTabCellData
    {
        public int Index;
        public bool IsSelect;
        public bool HasDot;
        public AssetConfig ImageConfig;
        public Action<int> OnClickCb;
    }
    
    public class UIHandbookTabCell : FancyScrollRectCell<HandbookTabCellData, UICommonScrollRectDefaultContext>
    {

        [SerializeField] private GameObject normalGo;
        [SerializeField] private GameObject selectGo;
        [SerializeField] private UIImageRes icon;
        [SerializeField] private GameObject dot;
        [SerializeField] private Button clickBtn;

        private HandbookTabCellData _curCellData;

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
        
        public override void UpdateContent(HandbookTabCellData tabCellData)
        {
            if (tabCellData == null)
                return;
            _curCellData = tabCellData;
            normalGo.SetActive(!_curCellData.IsSelect);
            selectGo.SetActive(_curCellData.IsSelect);
            dot.SetActive(_curCellData.HasDot);
            icon.SetImage(_curCellData.ImageConfig);
        }
        
        private void _OnClickBtnTab()
        {
            if (_curCellData != null)
            {
                _curCellData.OnClickCb?.Invoke(_curCellData.Index);
                transform.Find("Content/Icon").GetComponent<Animator>().SetTrigger("Punch");
                Game.Manager.audioMan.TriggerSound("BoxItem");
            }
        }
    }
}