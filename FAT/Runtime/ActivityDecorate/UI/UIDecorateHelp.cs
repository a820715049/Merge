/*
 * @Author: pengjian.zhang
 * @Description: 装饰区活动教学
 * @Date: 2024-05-22 11:56:00
 */

using EL;
using UnityEngine;

namespace FAT
{
    public class UIDecorateHelp : UIBase
    {
        protected override void OnCreate()
        {
            transform.AddButton("Mask", OnClose).FixPivot();
            transform.AddButton("close", OnClose).FixPivot();
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