using EL;
using TMPro;
using UnityEngine.UI;

namespace FAT
{
    public class UIOrderBonusTips : UITipsBase
    {
        public UIStateGroup uIStateGroup;
        protected override void OnParse(params object[] items)
        {
            _SetCurTipsWidth(906);
            _SetTipsPosInfo(items);
            int.TryParse(items[2].ToString(), out var id);
            uIStateGroup.Select(id);
        }
        protected override void OnPreOpen()
        {
            //刷新tips位置
            _RefreshTipsPos(18, false);
        }
    }
}