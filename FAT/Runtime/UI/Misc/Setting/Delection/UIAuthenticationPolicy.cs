/*
 * @Author: tang.yan
 * @Description: 删除账号-协议确认界面 
 * @Date: 2023-12-18 10:12:34
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT
{
    public class UIAuthenticationPolicy : UIBase
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private TMP_Text textDetail;

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            btnClose.WithClickScale().FixPivot().onClick.AddListener(base.Close);
            btnConfirm.WithClickScale().FixPivot().onClick.AddListener(_OnBtnConfirm);
        }

        protected override void OnPreOpen()
        {
            var trans = (textDetail.transform.parent as RectTransform);
            trans.anchoredPosition = Vector2.zero;

            var policy = AccountDelectionUtility.GetAccountDeletionPolicy();
            if (String.IsNullOrEmpty(policy))
            {
                policy = I18N.Text("#SysComDesc134");
            }
            policy += "\n\n\n"; //最后加点换行 避免显示不全
            textDetail.text = policy;
        }

        private void _OnBtnConfirm()
        {
            Close();
            UIManager.Instance.OpenWindow(UIConfig.UIAuthenticationSelect);
        }
    }
}