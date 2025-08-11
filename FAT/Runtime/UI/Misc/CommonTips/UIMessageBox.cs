/*
 * @Author: tang.yan
 * @Description: 普通二次确认框UI 
 * @Date: 2023-10-23 16:10:17
 */
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT
{
    public class UIMessageBox : UIBase, INavBack
    {
        private CommonTipsMan.TipsData _tipsData;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnCancel;
        [SerializeField] private Button btnHelp;    //帮助按钮 默认显示时会隐藏取消按钮
        [SerializeField] private TMP_Text btnConfirmText;
        [SerializeField] private TMP_Text btnCancelText;
        [SerializeField] private TMP_Text btnHelpText;
        private Vector2 _originSize = Vector2.zero;

        protected override void OnCreate()
        {
            btnCancel.onClick.AddListener(_OnBtnCancel);
            btnConfirm.onClick.AddListener(_OnBtnConfirm);
            btnHelp.onClick.AddListener(_OnBtnHelp);
            _originSize = panel.sizeDelta;
        }

        protected override void OnPreOpen()
        {
            _tipsData = Game.Manager.commonTipsMan.CurTips;
            _RefreshInfo();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_COMMON_CLOSE_CUR_TIPS>().AddListener(base.Close);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_COMMON_CLOSE_CUR_TIPS>().RemoveListener(base.Close);
        }

        protected override void OnPostClose()
        {
            if (_tipsData.IsFullScreen)
                panel.sizeDelta = _originSize;
            Game.Manager.commonTipsMan.TryShowNextTips();
        }

        private void _RefreshInfo()
        {
            if (_tipsData == null)
                return;
            titleText.text = _tipsData.Title;
            contentText.text = _tipsData.Content;
            btnCancel.gameObject.SetActive(_tipsData.IsShowLeftBtn && !_tipsData.IsShowHelpBtn);
            btnConfirm.gameObject.SetActive(_tipsData.IsShowRightBtn);
            btnHelp.gameObject.SetActive(_tipsData.IsShowHelpBtn);
            btnCancelText.text = _tipsData.LeftBtnName;
            btnConfirmText.text = _tipsData.RightBtnName;
            btnHelpText.text = I18N.Text("#SysComBtn26");
            if (_tipsData.IsFullScreen)
                panel.sizeDelta = UIManager.Instance.SafeRoot.rect.size;
        }
        
        private void _OnBtnCancel()
        {
            if (_tipsData != null && _tipsData.IsShowLeftBtn && !_tipsData.IsShowHelpBtn)
            {
                _tipsData.LeftBtnCb?.Invoke();
            }
            base.Close();
        }

        private void _OnBtnConfirm()
        {
            if (_tipsData != null && _tipsData.IsShowRightBtn)
            {
                _tipsData.RightBtnCb?.Invoke();
            }
            base.Close();
        }
        
        private void _OnBtnHelp()
        {
            if (_tipsData != null && _tipsData.IsShowHelpBtn)
            {
                Platform.PlatformSDK.Instance.ShowCustomService();
            }
        }

        void INavBack.OnNavBack()
        {
            _OnBtnCancel();
        }
    }
}