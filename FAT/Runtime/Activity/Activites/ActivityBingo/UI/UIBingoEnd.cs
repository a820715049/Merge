/*
 * @Author: qun.chao
 * @Date: 2025-03-06 16:01:28
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class UIBingoEnd : UIBase
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private TextMeshProUGUI txtTitle;

        private ActivityBingo actInst;

        protected override void OnCreate()
        {
            transform.Access<Button>("Mask").onClick.AddListener(Close);
            btnClose.onClick.AddListener(Close);
            btnConfirm.onClick.AddListener(Close);
        }

        protected override void OnParse(params object[] items)
        {
            actInst = items[0] as ActivityBingo;
        }

        protected override void OnPreOpen()
        {
            var visual = actInst.MainVisual;
            visual.Refresh(txtTitle, "mainTitle");
        }

        protected override void OnPostClose()
        {
            actInst = null;
        }
        
        
        
    }
}