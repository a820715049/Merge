using System.Collections;
using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UICardSendTips : UITipsBase
    {
        private TextMeshProUGUI _desc;

        protected override void OnCreate()
        {
            transform.Access("Content/Node/Text", out _desc);
        }

        protected override void OnParse(params object[] items)
        {
            _SetTipsPosInfo(items[1], items[2]);
            _desc.text = I18N.Text((bool)items[0] ? "#SysComDesc650" : "#SysComDesc651");
        }

        protected override void OnPreOpen()
        {
            _RefreshTipsPos(80, false);

            IEnumerator enumerator()
            {
                yield return new WaitForSeconds(2f);
                OnNavBack();
            }

            Game.Instance.StartCoroutineGlobal(enumerator());
        }
    }
}