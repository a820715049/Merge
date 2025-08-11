/*
 * @Author: tang.yan
 * @Description: 卡片收取界面 cell 
 * @Date: 2024-10-24 19:10:08
 */

using UnityEngine.UI.Extensions;
using fat.msg;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FAT
{
    public class UICardPendingCell : FancyScrollRectCell<ItemsFromOther, UICommonScrollRectDefaultContext>
    {
        [SerializeField] private Button clickBtn;
        [SerializeField] private UIImageRes userIcon;
        [SerializeField] private UITextExt userName;

        private ItemsFromOther _curInfo;
        
        public override void Initialize()
        {
            clickBtn.onClick.AddListener(_OnClickBtn);
        }

        public override void UpdateContent(ItemsFromOther data)
        {
            if (data == null)
                return;
            _curInfo = data;
            var friendInfo = Game.Manager.cardMan.GetSentCardFriendInfo(_curInfo.FromUid)?.FacebookInfo;
            if (friendInfo != null)
            {
                userIcon.SetUrl(friendInfo.Avatar);
                userName.text = friendInfo.Name;
            }
        }
        
        private void _OnClickBtn()
        {
            if (_curInfo == null)
                return;
            StartCoroutine(_TryGiftCardToFriend());
        }
        
        private IEnumerator _TryGiftCardToFriend()
        {
            UIManager.Instance.Block(true);
            yield return Game.Manager.cardMan.TryClaimPendingCard(_curInfo);
            UIManager.Instance.Block(false);
            UIManager.Instance.CloseWindow(UIConfig.UICardPending);
        }
    }
}