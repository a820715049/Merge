/*
 * @Author: pengjian.zhang
 * @Description: 寻宝教学/寻宝新手引导弹窗
 * @Date: 2024-04-23 20:15:02
 */

using System.Collections.Generic;
using UnityEngine;
using EL;
using TMPro;

namespace FAT
{
    public class UITreasureHuntHelp : UIBase
    {
        [SerializeField] private UITreasureHuntHelpTabScroll tabScrollView;
        [SerializeField] private int maxTabNum; //最大tab数
        [SerializeField] private TextProOnACircle helpTitle;
        [SerializeField] private TextMeshProUGUI help1;
        [SerializeField] private TextMeshProUGUI help2;
        [SerializeField] private TextMeshProUGUI help3;
        [SerializeField] private TextMeshProUGUI help4;
        [SerializeField] private TextMeshProUGUI tip;
        [SerializeField] private MBHelpGuideDrag drag;
        private int _curSelectTabId;
        private List<TreasureHuntHelpTabCellData> _tabCellDataList = new List<TreasureHuntHelpTabCellData>();   //底部页签cell数据
        private int page;

        protected override void OnCreate()
        {
            transform.AddButton("Mask", OnClose);
            transform.AddButton("Content/BtnClose", OnClose).FixPivot();
            tabScrollView.InitLayout();
            _tabCellDataList.Clear();
            for (int i = 0; i < maxTabNum; i++)
            {
                var data = new TreasureHuntHelpTabCellData()
                {
                    Index = i + 1,
                    IsSelect = i == _curSelectTabId,
                    OnClickCb = _OnTabBtnClick,
                };
                _tabCellDataList.Add(data);
            }
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
                page = (int)items[0];
        }

        protected override void OnPreOpen()
        {
            var p = page > 0 ? page : 1;
            drag.Init(p);
            _RefreshTabScroll();
            _OnTabBtnClick(p);
            page = 0;
            UITreasureHuntUtility.TryGetEventInst(out var act);
            act.VisualHelp.Refresh(helpTitle, "helpTitle");
            act.VisualHelpTab.Refresh(help4, "help2");
            var c = Game.Manager.objectMan.GetTokenConfig(act.ConfD.RequireCoinId);
            help1.SetText(I18N.FormatText("#SysComDesc771", c.SpriteName));
            help2.SetText(I18N.FormatText("#SysComDesc767", c.SpriteName));
            help3.SetText(I18N.FormatText("#SysComDesc768", c.SpriteName));
            tip.SetText(I18N.FormatText("#SysComDesc772", c.SpriteName));
            transform.GetComponent<Animator>().SetTrigger("Show");
        }

        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_HELP_GUIDE_DRAG_END>().AddListener(_OnTabDragxEnd);
        }

        protected override void OnRefresh()
        {
            _RefreshTabScroll();
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_HELP_GUIDE_DRAG_END>().RemoveListener(_OnTabDragxEnd);
        }

        private void _OnTabDragxEnd(int targeIdx)
        {
            if (targeIdx < 1 || targeIdx > maxTabNum)
            {
                return;
            }
            _curSelectTabId = targeIdx - 1;
            _tabCellDataList[_curSelectTabId].OnClickCb?.Invoke(_tabCellDataList[_curSelectTabId].Index);
        }

        private void _OnTabBtnClick(int index)
        {
            if (_curSelectTabId != index)
            {
                _curSelectTabId = index;
                _RefreshTabScroll();
            }
        }

        private void _RefreshTabScroll()
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