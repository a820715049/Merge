/*
 * @Author: yanfuxing
 * @Date: 2025-07-18 11:20:05
 */
using Cysharp.Text;
using EL;
using TMPro;

namespace FAT
{
    public class UIMultiplyRankingMainTips : UITipsBase
    {
        public TextMeshProUGUI _desc;
        private string _str;

        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            if (items.Length > 0)
            {
                _str = items[2] as string;
            }
            _SetTipsPosInfo(items);
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            _SetCurTipsHeight(800);
            _RefreshTipsPos(0, false);
            _desc.SetTextFormat(I18N.Text("#SysComDesc1460"), _str);
        }
    }
}