/*
*@Author:chaoran.zhang
*@Desc:集卡活动结束弹窗
*@Created Time:2024.01.22 星期一 15:01
*/
using EL;
using fat.conf;
using TMPro;

namespace FAT
{
    public class UICardActivityEnd: UIBase
    {
        private UIImageRes _bg;
        private TMP_Text _mainTitle;
        private TMP_Text _desc1;
        private TMP_Text _desc2;
        private ActivityVisual _eventTheme = new ActivityVisual();
        protected override void OnCreate()
        {
            transform.AddButton("Content/Panel/BtnConfirm", Close);
            _bg = transform.FindEx<UIImageRes>("Content/Panel");
            _mainTitle = transform.FindEx<TMP_Text>("Content/Panel/BgTitle/Title");
            _desc1 = transform.FindEx<TMP_Text>("Content/Panel/Desc1");
            _desc2 = transform.FindEx<TMP_Text>("Content/Panel/Desc2");
        }

        protected override void OnParse(params object[] items)
        {
            if (_eventTheme.Setup((int)items[1]))
            {
                _eventTheme.Refresh(_bg, "bgImage");
                _eventTheme.Refresh(_mainTitle, "mainTitle");
                _eventTheme.Refresh(_desc1, "desc1");
                _eventTheme.Refresh(_desc2, "desc2");
            }
        }
    }
}