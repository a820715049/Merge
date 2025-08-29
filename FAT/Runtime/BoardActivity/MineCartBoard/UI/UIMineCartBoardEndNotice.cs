
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using EL;
using TMPro;
using UnityEngine;
using static EL.PoolMapping;

namespace FAT
{
    public class UIMineCartBoardEndNotice : UIBase, INavBack
    {
        // Image字段
        [SerializeField] private UIImageRes _Bg;
        [SerializeField] private UIImageRes _TitleBg;
        // Text字段
        [SerializeField] private TextProOnACircle _Title;
        [SerializeField] private TextMeshProUGUI _SubTitle;

        private MineCartActivity _activity;

        protected override void OnCreate()
        {
            base.OnCreate();
            transform.AddButton("Content/CloseBtn", ClickBtn);
            transform.AddButton("Content/Confirm", ClickBtn);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 2) return;
            _activity = items[0] as MineCartActivity;
        }

        protected override void OnPreOpen()
        {
            _activity.EndPopup.visual.Refresh(_Title, "mainTitle");
            _activity.EndPopup.visual.Refresh(_TitleBg, "titleBg");
            _activity.EndPopup.visual.Refresh(_Bg, "bg");
            _activity.EndPopup.visual.Refresh(_SubTitle, "subTitle");
        }

        private void ClickBtn()
        {
            Close();
        }

        public void OnNavBack()
        {
            ClickBtn();
        }
    }
}
