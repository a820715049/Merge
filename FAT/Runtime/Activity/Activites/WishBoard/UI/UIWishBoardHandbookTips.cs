/*
 * @Author: yanfuxing
 * @Date: 2025-06-20 11:40:05
 */
using System.Collections;
using Cysharp.Text;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIWishBoardHandbookTips : UIBase
    {
        // Image字段
        private UIImageRes _Bg;
        private UIImageRes _Left;
        // Text字段
        private TextMeshProUGUI _Desc;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            // Image绑定
            transform.Access("Root/Bg_img", out _Bg);
            transform.Access("Root/Left_img", out _Left);
            // Text绑定
            transform.Access("Root/Desc_txt", out _Desc);
        }

        protected override void OnParse(params object[] items)
        {
            IEnumerator coroutine()
            {
                yield return new WaitForSeconds(2f);
                Close();
            }
            Game.Instance.StartCoroutineGlobal(coroutine());
            Game.Manager.audioMan.TriggerSound("MineBanner");
            if (items.Length < 2) return;
            if (items[0] is WishBoardActivity activity)
            {
                var text = I18N.Text("#SysComDesc910");
                _Desc.text = ZString.Concat(text, "\n", (string)items[1]);
            }
            if (items.Length > 2)
            {
                transform.Find("Mask").GetComponent<Animation>().Play();
            }
        }
    }
}