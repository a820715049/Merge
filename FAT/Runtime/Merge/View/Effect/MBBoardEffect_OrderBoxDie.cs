/*
 * @Author: qun.chao
 * @Date: 2024-01-05 20:34:22
 */
using UnityEngine;
using UnityEngine.UI;

namespace FAT.Merge
{
    public class MBBoardEffect_OrderBoxDie : MonoBehaviour
    {
        [SerializeField] private UIImageRes iconRes;
        [SerializeField] private TMPro.TextMeshProUGUI txtDuration;

        public void Setup(int tid)
        {
            var basicCfg = Env.Instance.GetItemConfig(tid);
            var cfg = Env.Instance.GetItemComConfig(tid);
            iconRes.SetImage(basicCfg.Icon);
            UIUtility.CountDownFormat(txtDuration, cfg.orderBoxConfig.Time / 1000, UIUtility.CdStyle.OmitZero);
        }
    }
}