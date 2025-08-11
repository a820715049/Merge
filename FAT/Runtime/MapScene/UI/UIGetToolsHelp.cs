/*
 * @Author: pengjian.zhang
 * @Description: 获取工具教学
 * @Date: 2024-06-24 16:02:00
 */

using EL;
using UnityEngine;

namespace FAT
{
    public class UIGetToolsHelp : UIBase
    {
        [SerializeField] private UIImageRes toolImg;

        private int toolId;
        
        protected override void OnCreate()
        {
            transform.AddButton("Mask", OnClose).FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            toolId = (int)(items[0]);
        }

        protected override void OnPreOpen()
        {
            var toolConfig = Game.Manager.configMan.GetObjToolConfig(toolId);
            if (toolConfig != null)
            {
                toolImg.SetImage(toolConfig.TutorialImage);
            }
            transform.GetComponent<Animator>().SetTrigger("Show");
        }
        
        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }
    }
}