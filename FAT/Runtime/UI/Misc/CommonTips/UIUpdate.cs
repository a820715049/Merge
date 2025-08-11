/*
 * @Author: qun.chao
 * @Date: 2025-06-13 17:23:47
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT
{
    public class UIUpdate : UIBase
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private TextMeshProUGUI txtDesc;
        private bool isForceUpdate;

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(_OnBtnClose);
            btnConfirm.onClick.AddListener(_OnBtnConfirm);
        }
        
        protected override void OnParse(params object[] items)
        {
            isForceUpdate = (bool)items[0];
        }
        
        protected override void OnPreOpen()
        {
            btnClose.gameObject.SetActive(!isForceUpdate);
            txtDesc.text = I18N.Text("#SysComDesc1235");
#if FAT_PIONEER
            txtDesc.text = I18N.Text("#SysComDesc1309");
#endif
        }

        private void _OnBtnClose()
        {
            Close();
        }

        private void _OnBtnConfirm()
        {
#if FAT_PIONEER
            CommonUtility.QuitApp();
            return;
#endif
            UIBridgeUtility.OpenAppStore();
        }
    }
}