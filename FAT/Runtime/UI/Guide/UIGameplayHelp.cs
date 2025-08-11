using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT {
    public class UIGameplayHelp : UIBase {
        public MapButton mask;
        public Animator uiAnim;
        public float closeDelay = 1f;
        internal float openTime;

#if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;
            uiAnim = GetComponent<Animator>();
            mask = transform.FindEx<MapButton>("Mask");
        }
#endif

        protected override void OnCreate() {
            mask.WhenClick = MaskClick;
        }

        protected override void OnPreOpen() {
            UIUtility.FadeIn(this, uiAnim);
            openTime = Time.realtimeSinceStartup;
        }

        private void MaskClick() {
            var time = Time.realtimeSinceStartup;
            if (time - openTime < closeDelay) return;
            UserClose();
        }

        private void UserClose() {
            UIUtility.FadeOut(this, uiAnim);
        }
    }
}