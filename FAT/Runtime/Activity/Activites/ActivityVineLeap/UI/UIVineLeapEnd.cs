// ===================================================
// Author: mengqc
// Date: 2025/09/02
// ===================================================

using EL;
using TMPro;

namespace FAT
{
    public class UIVineLeapEnd : UIBase
    {
        public TextMeshProUGUI tfDesc1;
        public TextMeshProUGUI tfDesc2;

        private ActivityVineLeap _activity;

        protected override void OnCreate()
        {
            base.OnCreate();
            transform.AddButton("Content/Panel/BtnConfirm", Close);
            transform.AddButton("Content/Panel/btnClose", Close);
        }

        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            _activity = (ActivityVineLeap)items[0];
            if (_activity.IsFinalWin())
            {
                tfDesc1.text = I18N.Text("#SysComDesc1748");
                tfDesc2.text = I18N.FormatText("#SysComDesc1746", I18N.Text("#SysComDesc1720"));
            }
            else
            {
                tfDesc1.text = I18N.Text("#SysComDesc1749");
                tfDesc2.text = I18N.Text("#SysComDesc1747");
            }
        }
    }
}