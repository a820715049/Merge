/**
 * @Author: zhangpengjian
 * @Date: 2025/2/13 15:41:21
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/2/13 15:41:21
 * Description: 猜颜色活动教学
 */

using EL;
using UnityEngine;

namespace FAT
{
    public class UIActivityGuessHelp : UIBase
    {
        protected override void OnCreate()
        {
            transform.AddButton("Mask", OnClose).FixPivot();
            transform.AddButton("Content/page1/close", OnClose).FixPivot();
        }

        protected override void OnPreOpen()
        {
            transform.GetComponent<Animator>().SetTrigger("Show");
        }
        
        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }
    }
}