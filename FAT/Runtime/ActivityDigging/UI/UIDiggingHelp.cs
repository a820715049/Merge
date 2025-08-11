/**
 * @Author: zhangpengjian
 * @Date: 2024/8/19 10:30:13
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/19 10:30:13
 * Description: 挖沙活动帮助界面
 */

using EL;
using UnityEngine;

namespace FAT
{
    public class UIDiggingHelp : UIBase
    {
        protected override void OnCreate()
        {
            transform.AddButton("Content/page0/BtnClaim", OnClose).FixPivot();
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