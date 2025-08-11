/**
 * @Author: zhangpengjian
 * @Date: 2024/12/11 16:21:55
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/12/11 16:21:55
 * Description: 弹珠活动帮助
 */
 
using UnityEngine;
using EL;

namespace FAT
{
    public class UIPachinkoHelp : UIBase
    {
        public MapButton mask;
        public Animator uiAnim;
        public float closeDelay = 1f;
        internal float openTime;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Application.isPlaying) return;
            uiAnim = GetComponent<Animator>();
            mask = transform.FindEx<MapButton>("Mask");
        }
#endif

        protected override void OnCreate()
        {
            mask.WhenClick = MaskClick;
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