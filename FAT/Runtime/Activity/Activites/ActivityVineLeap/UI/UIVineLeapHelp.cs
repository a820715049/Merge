// ===================================================
// Author: mengqc
// Date: 2025/09/23
// ===================================================

using EL;
using TMPro;

namespace FAT
{
    public class UIVineLeapHelp : UIActivityHelp
    {
        public TextMeshProUGUI tfDesc1;

        private ActivityVineLeap _activity;

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as ActivityVineLeap;
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            if (_activity == null)
            {
                return;
            }

            var id = _activity.TokenId;
            var s = UIUtility.FormatTMPString(id);
            tfDesc1.SetText(I18N.FormatText("#SysComDesc1731", s));
        }
    }
}