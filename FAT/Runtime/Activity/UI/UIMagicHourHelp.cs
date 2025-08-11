/**
 * @Author: zhangpengjian
 * @Date: 2025/1/15 15:28:19
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/1/15 15:28:19
 * Description: 星想事成帮助界面
 */

using UnityEngine;
using EL;
using UnityEngine.UI;

namespace FAT
{
    public class UIMagicHourHelp : UIBase
    {
        public Button mask;
        public Animator uiAnim;
        public float closeDelay = 1f;
        internal float openTime;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Application.isPlaying) return;
            uiAnim = GetComponent<Animator>();
            mask = transform.FindEx<Button>("Content/BtnClaim");
        }
#endif

        protected override void OnCreate()
        {
            mask.onClick.AddListener(MaskClick);
        }

        protected override void OnPreOpen()
        {
            UIUtility.FadeIn(this, uiAnim);
            openTime = Time.realtimeSinceStartup;
        }

        private void MaskClick()
        {
            var time = Time.realtimeSinceStartup;
            if (time - openTime < closeDelay) return;
            UserClose();
        }

        private void UserClose()
        {
            UIUtility.FadeOut(this, uiAnim);
        }
    }
}