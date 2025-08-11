
using System.Collections;
using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIMineTokenTips : UITipsBase
    {

        protected override void OnCreate()
        {
        }

        protected override void OnParse(params object[] items)
        {
            _SetTipsPosInfo(items[0], items[1]);
        }

        protected override void OnPreOpen()
        {
            _RefreshTipsPos(0f);
        }
    }
}