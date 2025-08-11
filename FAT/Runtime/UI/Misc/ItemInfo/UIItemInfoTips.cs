/*
 * @Author: tang.yan
 * @Description: 来源与产出Tips界面
 * @Date: 2023-11-27 19:11:30
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using fat.rawdata;

namespace FAT
{
    public class UIItemInfoTips : UITipsBase
    {
        private class UIItemInfoGroup
        {
            public GameObject GroupGo;
            public GameObject OneCellGo;
            public GameObject TwoCellGo;
            public UIItemInfoTipsCell OneCell;
            public List<UIItemInfoTipsCell> TwoCellList;
        }
        
        private class UIItemInfoTipsCell
        {
            public GameObject NormalGo;
            public GameObject LockGo;
            public UIImageRes Icon;
            public Button TipsBtn;
            public GameObject TipsGo;
        }
        
        [SerializeField] private GameObject nextLevelGo; 
        [SerializeField] private TMP_Text nextLevelText; 
        [SerializeField] private GameObject produceGo;
        private List<UIItemInfoGroup> _groupList = new List<UIItemInfoGroup>();
        private List<GameObject> _arrowList = new List<GameObject>();
        
        private int _curItemId; //来源与产出面板上当前正在查看的棋子id
        private int _showLevel; //tips上要显示的等级
        
        protected override void OnCreate()
        {
            string path1 = "Panel/ProduceTips/Info/Group/ItemGroup";
            string path2 = "Panel/ProduceTips/Info/Group/ArrowGo";
            int index = 0;
            for (int i = 3; i >= 0; i--)
            {
                string path = path1 + i;
                var group = new UIItemInfoGroup();
                transform.FindEx(path, out group.GroupGo);
                transform.FindEx(path + "/OneCell", out group.OneCellGo);
                transform.FindEx(path + "/TwoCell", out group.TwoCellGo);
                group.OneCell = new UIItemInfoTipsCell();
                _FillTipsCell(path + "/OneCell/UIItemInfoTipsCell/Content", group.OneCell);
                int tempGroupIndex = index;
                group.OneCell.TipsBtn.onClick.AddListener(() => _OnTipsBtnClick(tempGroupIndex, -1));
                group.TwoCellList = new List<UIItemInfoTipsCell>();
                for (int j = 0; j < 2; j++)
                {
                    var tempCell = new UIItemInfoTipsCell();
                    _FillTipsCell(path + "/TwoCell/UIItemInfoTipsCell" + j + "/Content", tempCell);
                    int tempItemIndex = j;
                    tempCell.TipsBtn.onClick.AddListener(() => _OnTipsBtnClick(tempGroupIndex, tempItemIndex));
                    group.TwoCellList.Add(tempCell);
                }
                _groupList.Add(group);
                index++;
            }
            for (int i = 3; i >= 1; i--)
            {
                transform.FindEx(path2 + i, out GameObject arrowGo);
                _arrowList.Add(arrowGo);
            }
        }

        private void _FillTipsCell(string path, UIItemInfoTipsCell cell)
        {
            transform.FindEx(path + "/NormalGo", out cell.NormalGo);
            transform.FindEx(path + "/LockGo", out cell.LockGo);
            cell.Icon = transform.FindEx<UIImageRes>(path + "/Icon");
            cell.TipsBtn = transform.FindEx<Button>(path + "/Icon"); 
            transform.FindEx(path + "/TipsBtn", out cell.TipsGo);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 2)
            {
                _SetTipsPosInfo(items);
            }
            if (items.Length == 3)
            {
                _curItemId = (int)items[2];
            }
            else if (items.Length == 4)
            {
                _curItemId = (int)items[2];
                _showLevel = (int)items[3];
            }
            else
            {
                _curItemId = 0;
                _showLevel = 0;
            }
        }

        protected override void OnPreOpen()
        {
            _RefreshNextLevel();
            _RefreshProduce();
            //刷新tips位置
            _SetCurTipsWidth(_curItemId <= 0 ? 804 : 866);
            _SetCurTipsHeight(_curItemId <= 0 ? 181 : 374);
            _RefreshTipsPos(0, false);
        }

        protected override void OnPostClose()
        {
            
        }

        private void _RefreshNextLevel()
        {
            if (_curItemId <= 0)
            {
                nextLevelGo.SetActive(true);
                nextLevelText.text = _showLevel > 0 ? I18N.FormatText("#SysComDesc482", _showLevel) : I18N.Text("#SysComDesc34");
            }
            else
            {
                nextLevelGo.SetActive(false);
            }
        }

        private void _RefreshProduce()
        {
            var tipsDataList = Game.Manager.itemInfoMan.CurShowItemData?.TipsDataList;
            if (_curItemId > 0 && tipsDataList != null)
            {
                produceGo.SetActive(true);
                var length = tipsDataList.Count;
                for (int i = 0; i < _groupList.Count; i++)
                {
                    var group = _groupList[i];
                    if (i < length)
                    {
                        var data = tipsDataList[i];
                        group.GroupGo.SetActive(true);
                        //不显示吃或消耗
                        if (!data.IsShowEat)
                        {
                            group.OneCellGo.SetActive(true);
                            group.TwoCellGo.SetActive(false);
                            _RefreshTipsCell(group.OneCell, data);
                        }
                        //显示吃或消耗
                        else
                        {
                            group.OneCellGo.SetActive(false);
                            group.TwoCellGo.SetActive(true);
                            int index = 0;  //用于区分是上面的生成器 还是下面的要被吃或消耗的棋子
                            foreach (var cell in group.TwoCellList)
                            {
                                _RefreshTipsCell(cell, data, index);
                                index++;
                            }
                        }
                    }
                    else
                    {
                        group.GroupGo.SetActive(false);   
                    }
                }
                for (int i = 0; i < _arrowList.Count; i++)
                {
                    if (i < length - 1)
                    {
                        _arrowList[i].SetActive(true);
                    }
                    else
                    {
                        _arrowList[i].SetActive(false);
                    }
                }
            }
            else
            {
                produceGo.SetActive(false);
            }
        }
        
        private void _RefreshTipsCell(UIItemInfoTipsCell cell, ItemInfoMan.ItemChainTipsData tipsData, int index = 0)
        {
            int itemId = index == 0 ? tipsData.ShowItemId : tipsData.EatShowItemId;
            int tipsLevel = index == 0 ? tipsData.TipsLevel : tipsData.EatTipsLevel;
            cell.NormalGo.SetActive(itemId > 0);
            cell.LockGo.SetActive(itemId <= 0);
            cell.TipsBtn.enabled = tipsLevel > 0;
            cell.TipsGo.gameObject.SetActive(tipsLevel > 0);
            var cfg = Game.Manager.objectMan.GetBasicConfig(itemId);
            if (cfg != null)
            {
                cell.Icon.gameObject.SetActive(true);
                cell.Icon.SetImage(cfg.Icon.ConvertToAssetConfig());
                Color color = Color.white;
                color.a = tipsLevel <= 0 ? 1f : 0.5f;
                cell.Icon.color = color;
            }
            else
            {
                cell.Icon.gameObject.SetActive(false);
            }
        }
        
        private void _OnTipsBtnClick(int groupIndex, int index)
        {
            var tipsDataList = Game.Manager.itemInfoMan.CurShowItemData?.TipsDataList;
            if (tipsDataList != null && tipsDataList.TryGetByIndex(groupIndex, out var data))
            {
                bool isLevelMax = index == 0 ? data.IsTipsLevelMax : data.IsEatTipsLevelMax;
                if (!isLevelMax)
                {
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.ItemInfoUpgrade, data.TipsLevel);
                }
                else
                {
                    Game.Manager.commonTipsMan.ShowPopTips(Toast.ItemInfoMaxLv);
                }
            }
        }
    }
}
