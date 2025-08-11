using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIOrderChallengeTips : UITipsBase
    {
        public RectTransform textTrans;
        public TextMeshProUGUI text;
        public LayoutElement element;

        protected override void OnParse(params object[] items)
        {
            _SetCurExtraWidth(78);
            _SetCurTipsHeight(items.Length > 3 ? 1000f : 0f);
            text.text = items[2] as string;
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.GetChild(0) as RectTransform);
            if (items.Length >= 2) _SetTipsPosInfo(items);
        }

        protected override void OnPreOpen()
        {
            _RefreshTipsPos(20);
        }
    }
}