/*
 * @Author: tang.yan
 * @Description: 技能类棋子使用确认弹窗(万能卡 分割器) 
 * @Date: 2024-04-25 11:04:55
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using fat.rawdata;
using TMPro;
using EL;

namespace FAT
{
    public class UIItemUseConfirm : UIBase
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
        }
        
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text desc;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnCancel;
        
        private List<UIItemInfoGroup> _groupList = new List<UIItemInfoGroup>();
        private string _desc;
        private Action _confirmCb;
        private int _skillItemId;   //使用的棋子id 如万能卡 分割器
        private int _targetItemId;  //目标棋子id 

        protected override void OnCreate()
        {
            transform.AddButton("Mask", Close);
            transform.AddButton("Content/Root/BtnClose", Close).FixPivot();
            btnCancel.WithClickScale().FixPivot().onClick.AddListener(_OnBtnCancel);
            btnConfirm.WithClickScale().FixPivot().onClick.AddListener(_OnBtnConfirm);
            //item链条相关
            var basePath = "Content/Root/Panel/Info/Item/Group/ItemGroup";
            for (int i = 0; i <= 2; i++)
            {
                var path = basePath + i;
                var group = new UIItemInfoGroup();
                transform.FindEx(path, out group.GroupGo);
                transform.FindEx(path + "/OneCell", out group.OneCellGo);
                transform.FindEx(path + "/TwoCell", out group.TwoCellGo);
                group.OneCell = new UIItemInfoTipsCell();
                _FillTipsCell(path + "/OneCell/UIItemInfoTipsCell/Content", group.OneCell);
                group.TwoCellList = new List<UIItemInfoTipsCell>();
                for (int j = 0; j < 2; j++)
                {
                    var tempCell = new UIItemInfoTipsCell();
                    _FillTipsCell(path + "/TwoCell/UIItemInfoTipsCell" + j + "/Content", tempCell);
                    group.TwoCellList.Add(tempCell);
                }
                _groupList.Add(group);
            }
        }
        
        private void _FillTipsCell(string path, UIItemInfoTipsCell cell)
        {
            transform.FindEx(path + "/NormalGo", out cell.NormalGo);
            transform.FindEx(path + "/LockGo", out cell.LockGo);
            cell.Icon = transform.FindEx<UIImageRes>(path + "/Icon");
            cell.TipsBtn = transform.FindEx<Button>(path + "/Icon");
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 4)
            {
                _desc = items[0] as string;
                _confirmCb = items[1] as Action;
                _skillItemId = (int)items[2];
                _targetItemId = (int)items[3];
            }
        }

        protected override void OnPreOpen()
        {
            _Refresh();
        }

        protected override void OnPostClose()
        {
            _desc = "";
            _confirmCb = null;
            _skillItemId = 0;
            _targetItemId = 0;
        }

        private void _Refresh()
        {
            if (_skillItemId <= 0 || _targetItemId <= 0)
                return;
            var basicConfig = Game.Manager.objectMan.GetBasicConfig(_skillItemId);
            if (basicConfig == null)
                return;
            var skillConfig = Game.Manager.mergeItemMan.GetItemComConfig(_skillItemId)?.skillConfig;
            if (skillConfig == null)
                return;
            var catCfg = Merge.Env.Instance.GetCategoryByItem(_targetItemId);
            if (catCfg == null)
                return;
            //刷新描述
            desc.text = _desc;
            //刷新标题
            title.text = I18N.Text(basicConfig.Name);
            //找到目标棋子在所属链条中的index
            var idx = catCfg.Progress.IndexOf(_targetItemId);
            var afterItemId = -1;
            var isTwo = false;
            if (skillConfig.Type == SkillType.Upgrade)
            {
                //是万能卡时找链条中的高一级
                afterItemId = idx + 1 < catCfg.Progress.Count ? catCfg.Progress[idx + 1] : -1;
            }
            else if (skillConfig.Type == SkillType.Degrade)
            {
                //是分割器时找链条中的底一级 并显示两个
                afterItemId = idx - 1 >= 0 ? catCfg.Progress[idx - 1] : -1;
                isTwo = true;
            }
            _ShowItemInfo(_skillItemId, _targetItemId, afterItemId, isTwo);
        }

        private void _ShowItemInfo(int leftId, int middleId, int rightId, bool isTwo)
        {
            var objectMan = Game.Manager.objectMan;
            //第一列
            var leftCfg = objectMan.GetBasicConfig(leftId);
            if (leftCfg != null)
            {
                _groupList[0].OneCell.Icon.SetImage(leftCfg.Icon.ConvertToAssetConfig());
            }
            //第二列
            var middleCfg = objectMan.GetBasicConfig(middleId);
            if (middleCfg != null)
            {
                _groupList[1].OneCell.Icon.SetImage(middleCfg.Icon.ConvertToAssetConfig());
            }
            //第三列
            var rightCfg = objectMan.GetBasicConfig(rightId);
            if (rightCfg != null)
            {
                if (isTwo)
                {
                    _groupList[2].OneCellGo.SetActive(false);
                    _groupList[2].TwoCellGo.SetActive(true);
                    foreach (var cell in _groupList[2].TwoCellList)
                    {
                        cell.Icon.SetImage(rightCfg.Icon.ConvertToAssetConfig());
                    }
                }
                else
                {
                    _groupList[2].OneCellGo.SetActive(true);
                    _groupList[2].TwoCellGo.SetActive(false);
                    _groupList[2].OneCell.Icon.SetImage(rightCfg.Icon.ConvertToAssetConfig());
                }
            }
        }

        private void _OnBtnCancel()
        {
            Close();
        }

        private void _OnBtnConfirm()
        {
            _confirmCb?.Invoke();
            Close();
        }
    }
}