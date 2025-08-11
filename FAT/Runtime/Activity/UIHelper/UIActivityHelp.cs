/**
 * @Author: zhangpengjian
 * @Date: 2025/3/14 15:56:54
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/3/14 15:56:54
 * Description: 帮助界面通用脚本 
 */

using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityHelp : UIBase
    {
        [SerializeField] private Button mask;
        protected override void OnCreate()
        {
            mask.onClick.AddListener(OnClose);
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
