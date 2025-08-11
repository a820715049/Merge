/**
 * @Author: zhangpengjian
 * @Date: 2025/2/18 17:35:48
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/2/18 17:35:48
 * Description: 砍价礼包帮助
 */

using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIPackDiscountHelp : UIBase
    {
        [SerializeField]
        private TextMeshProUGUI help1;

        [SerializeField]
        private TextMeshProUGUI help3;

        private PackDiscount pack;
        protected override void OnCreate()
        {
            transform.AddButton("Mask", OnClose).FixPivot();
            transform.AddButton("close", OnClose).FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                pack = items[0] as PackDiscount;
            }
        }

        protected override void OnPreOpen()
        {
            transform.GetComponent<Animator>().SetTrigger("Show");
            if (pack != null)
            {
                help1.text = I18N.FormatText("#SysComDesc848", UIUtility.FormatTMPString(pack.ConfD.TokenId));
                help3.text = I18N.FormatText("#SysComDesc913", pack.ConfProgress.DiscountShow);
            }
        }

        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }
    }
}