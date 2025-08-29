using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIRaceHelp : UITipsBase
    {

        protected override void OnParse(params object[] items)
        {
            _SetTipsPosInfo(items[0], items[1]);
        }

        protected override void OnPreOpen()
        {
            _RefreshTipsPos(20);
        }
    }
}
