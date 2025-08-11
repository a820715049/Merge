/*
 * @Author: qun.chao
 * @Date: 2022-03-11 20:58:16
 */
using UnityEngine;
using EL;

namespace FAT
{
    public class MBBoardDialog : MonoBehaviour
    {
        [SerializeField] private GameObject mTextObj;
        [SerializeField] private UIImageRes mIconRes;

        public void Setup()
        {
            transform.AddButton(null, _OnBtnClose);
        }

        public void InitOnPreOpen(bool isGlobalGuide = false)
        {
            _Hide();
            if (isGlobalGuide)
            {
                MessageCenter.Get<MSG.UI_GUIDE_TOP_SHOW_DIALOG>().AddListener(_OnMessageShowDialog);
                MessageCenter.Get<MSG.UI_GUIDE_TOP_HIDE_DIALOG>().AddListener(_OnMessageHideDialog);
            }
            else
            {
                MessageCenter.Get<MSG.UI_GUIDE_BOARD_SHOW_DIALOG>().AddListener(_OnMessageShowDialog);
                MessageCenter.Get<MSG.UI_GUIDE_BOARD_HIDE_DIALOG>().AddListener(_OnMessageHideDialog);
            }
        }

        public void CleanupOnPostClose()
        {
            _Hide();
            MessageCenter.Get<MSG.UI_GUIDE_BOARD_SHOW_DIALOG>().RemoveListener(_OnMessageShowDialog);
            MessageCenter.Get<MSG.UI_GUIDE_BOARD_HIDE_DIALOG>().RemoveListener(_OnMessageHideDialog);
            MessageCenter.Get<MSG.UI_GUIDE_TOP_SHOW_DIALOG>().RemoveListener(_OnMessageShowDialog);
            MessageCenter.Get<MSG.UI_GUIDE_TOP_HIDE_DIALOG>().RemoveListener(_OnMessageHideDialog);
        }

        private void _Show(string key)
        {
            _Hide();
            gameObject.SetActive(true);
            MBI18NText.SetKey(mTextObj.gameObject, key);
        }

        private void _Hide()
        {
            gameObject.SetActive(false);
        }

        private void _OnBtnClose()
        {
            gameObject.SetActive(false);
        }

        private void _OnMessageShowDialog(string contentKey, string img)
        {
            _Show(contentKey);
            if (!string.IsNullOrEmpty(img))
            {
                var res = img.ConvertToAssetConfig();
                mIconRes.SetImage(res.Group, res.Asset);
            }
        }

        private void _OnMessageHideDialog()
        {
            _Hide();
        }
    }
}