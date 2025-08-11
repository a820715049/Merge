/*
 * @Author: tang.yan
 * @Description: 选择好友赠卡界面 Cell
 * @Date: 2024-10-24 18:10:33
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using fat.gamekitdata;
using EL;

namespace FAT
{
    public class UICardGiftingCell : FancyScrollRectCell<(int, PlayerOpenInfo), UICommonScrollRectDefaultContext>
    {
        [SerializeField] private Button clickBtn;
        [SerializeField] private UIImageRes userIcon;
        [SerializeField] private GameObject selectGo;
        [SerializeField] private UITextExt userName;

        private int _curIndex;
        private PlayerOpenInfo _curInfo;
        
        public override void Initialize()
        {
            clickBtn.onClick.AddListener(_OnClickBtn);
        }

        public override void UpdateContent((int, PlayerOpenInfo) data)
        {
            _curIndex = data.Item1;
            _curInfo = data.Item2;
            if (_curInfo == null) return;
            var fbInfo = _curInfo.FacebookInfo;
            if (fbInfo != null)
            {
                userIcon.SetUrl(fbInfo.Avatar);
                userName.text = fbInfo.Name;
            }
            selectGo.SetActive(_curIndex == Game.Manager.cardMan.CurSelectFriendIndex);
        }
        
        private void _OnClickBtn()
        {
            if (_curInfo == null)
                return;
            Game.Manager.cardMan.CurSelectFriendIndex = _curIndex;
            MessageCenter.Get<MSG.UI_CARD_GIFTING_SELECT>().Dispatch();
        }
    }
}