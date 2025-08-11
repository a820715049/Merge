/*
 * @Author: tang.yan
 * @Description: 集卡活动-卡册界面 卡片cell
 * @Date: 2024-01-28 15:51:11
 */

using EL;
using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public class UICardCellLayout : UIModuleBase
    {
        private static readonly int MAX_SHOW_CARD_NUM = 9;    //默认卡组中最多显示9张卡
        private List<UICardCell> _uiCardInfoList = new List<UICardCell>();    //卡组中卡片List
        //当前显示的卡组id
        private int _curShowGroupId = 0;
        //是否是空组
        private bool _isTemp;
        
        public UICardCellLayout(Transform root, bool isTemp) : base(root)
        {
            _isTemp = isTemp;
        }
        
        protected override void OnCreate()
        {
            if (_isTemp)
            {
                ModuleRoot.gameObject.SetActive(false);
                return;
            }
            //初始化卡组list
            string path = "UICardCell";
            for (int i = 0; i < MAX_SHOW_CARD_NUM; i++)
            {
                var root = ModuleRoot.Find(path + (i + 1));
                _uiCardInfoList.Add(AddModule(new UICardCell(root)));
            }
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                _curShowGroupId = (int)items[0];
            }
        }
        
        protected override void OnShow()
        {
            if (_curShowGroupId <= 0 || _isTemp)
            {
                ModuleRoot.gameObject.SetActive(false);
                return;
            }
            _RefreshCardUIList();
        }
        
        //刷新卡组中的卡片信息
        private void _RefreshCardUIList()
        {
            var groupConfig = Game.Manager.cardMan.GetCardGroupData(_curShowGroupId)?.GetConfig();
            if (groupConfig == null) return;
            int cardLength = groupConfig.CardInfo.Count;
            for (int i = 0; i < _uiCardInfoList.Count; i++)
            {
                if (i < cardLength)
                {
                    _uiCardInfoList[i].Show(groupConfig.CardInfo[i]);
                }
                else
                {
                    _uiCardInfoList[i].Hide();
                }
            }
        }

        protected override void OnHide()
        {
            
        }

        protected override void OnAddListener()
        {
        }

        protected override void OnRemoveListener()
        {
        }

        protected override void OnAddDynamicListener() { }

        protected override void OnRemoveDynamicListener() { }

        protected override void OnClose()
        {
            
        }
        
        public void OnClickCardCell(int cellIndex)
        {
            if (_isTemp || _curShowGroupId <= 0) return;
            if (_uiCardInfoList.TryGetByIndex(cellIndex, out var uiCardCell))
            {
                uiCardCell.OnClickCard();
            }
        }

        public Vector3 GetCardCellPos(int cellIndex)
        {
            if (_isTemp || _curShowGroupId <= 0) return Vector3.zero;
            if (_uiCardInfoList.TryGetByIndex(cellIndex, out var uiCardCell))
            {
                return uiCardCell.ModuleRoot.transform.position;
            }
            return Vector3.zero;
        }
    }
}