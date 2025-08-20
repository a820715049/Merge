using Cysharp.Text;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBSpinBuyButton : MonoBehaviour
    {
        public TextMeshProUGUI price;
        public GameObject extra;
        public UICommonItem item;
        public void Refresh(PackSpin pack)
        {
            gameObject.SetActive(!pack.WillEnd);
            if (pack.Content == null)
            {
                extra.SetActive(false);
                price.text = I18N.Text("#SysComDesc1313");
            }
            else
            {
                price.text = ZString.Format("{0}\n<size=80>{1}</size>", I18N.Text("#SysComDesc1313"), pack.Price);
                if (pack.Goods.reward.Count == 0) { return; }
                extra.SetActive(true);
                item.Refresh(pack.Goods.reward[0]);
            }
        }
    }
}