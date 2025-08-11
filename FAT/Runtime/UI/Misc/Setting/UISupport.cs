/*
 * @Author: qun.chao
 * @Date: 2024-10-23 17:28:17
 */
using UnityEngine;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class UISupport : UIBase
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnContact;
        [SerializeField] private Button btnFAQ;

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            btnClose.onClick.AddListener(base.Close);
            btnContact.onClick.AddListener(_OnBtnContact);
            btnFAQ.onClick.AddListener(_OnBtnFAQ);
        }

        private void _OnBtnContact()
        {
            Platform.PlatformSDK.Instance.Adapter.ShowConversation();
        }

        private void _OnBtnFAQ()
        {
            Platform.PlatformSDK.Instance.Adapter.ShowFAQ();
        }
    }
}