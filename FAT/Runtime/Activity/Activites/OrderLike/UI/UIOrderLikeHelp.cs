/*
 * @Author: qun.chao
 * @Date: 2025-03-26 18:16:55
 */
using UnityEngine;
using TMPro;
using EL;

namespace FAT
{
    public class UIOrderLikeHelp : UIBase
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI desc1;
        [SerializeField] private TextMeshProUGUI desc2;

        private float closeDelay = 1f;
        private float openTime;

        private MapButton mask;
        private Animator uiAnim;
        private ActivityOrderLike _actInst;

        protected override void OnCreate()
        {
            uiAnim = transform.Access<Animator>();
            mask = transform.Access<MapButton>("Mask");
            mask.WhenClick = MaskClick;
        }

        protected override void OnParse(params object[] items)
        {
            _actInst = items[0] as ActivityOrderLike;
        }

        protected override void OnPreOpen()
        {
            UIUtility.FadeIn(this, uiAnim);
            openTime = Time.realtimeSinceStartup;
            title.SetText(I18N.Text("#SysComDesc921"));
            desc1.SetText(I18N.FormatText("#SysComDesc946", TextSprite.FromToken(_actInst.TokenId)));
            desc2.SetText(I18N.FormatText("#SysComDesc947", TextSprite.FromToken(_actInst.TokenId)));
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