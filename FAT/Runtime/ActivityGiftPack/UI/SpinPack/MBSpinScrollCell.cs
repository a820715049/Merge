using Cysharp.Text;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBSpinScrollCell : MonoBehaviour
    {
        public UICommonItem item1;
        public UICommonItem item2;
        public GameObject complete;
        public TextMeshProUGUI rate;
        public void Refresh(SpinCellData data)
        {
            item1.gameObject.SetActive(data.reward1 != null);
            item2.gameObject.SetActive(data.reward2 != null);
            complete.SetActive(data.complete == 0);
            rate.gameObject.SetActive(data.complete != 0);
            rate.text = ZString.Concat((data.rate * 100).ToString("0.00"), '%');
            if (data.rate * 100 < 0.01) rate.text = "0.01%";
            if (data.reward1 != null) { item1.Refresh(data.reward1); }
            if (data.reward2 != null) { item2.Refresh(data.reward2); }
        }
    }
}