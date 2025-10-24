using System;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UISeaRaceFail : UISeaRaceBase
    {
        [SerializeField] private Button m_BtnConfirm;
        private Action m_CloseCallback;

        protected override void OnCreate()
        {
            m_BtnConfirm.WithClickScale().FixPivot().onClick.AddListener(OnBtnConfirmClick);
        }

        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            m_CloseCallback = items[1] as Action;
        }

        protected override void OnPostClose()
        {
            base.OnPostClose();
            m_CloseCallback?.Invoke();
            m_CloseCallback = null;
        }

        private void OnBtnConfirmClick()
        {
            Close();
        }
    }
}