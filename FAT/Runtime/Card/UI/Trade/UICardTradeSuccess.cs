/*
 *@Author:chaoran.zhang
 *@Desc:卡片赠送成功弹窗逻辑
 *@Created Time:2024.10.28 星期一 10:04:33
 */

using EL;
using TMPro;
using UnityEngine.UI;
using fat.gamekitdata;

namespace FAT
{
    public class UICardTradeSuccess : UIBase
    {
        private UIImageRes _cardIcon;
        private TextMeshProUGUI _cardName;
        private UITextState _cardNameState;
        private CardStar _starNode;
        private UIImageRes _playerIcon;
        private UITextExt _playerName;

        protected override void OnCreate()
        {
            transform.Access("Content/Panel/CardNode/CardIcon", out _cardIcon);
            transform.Access("Content/Panel/CardNode/CardName", out _cardName);
            transform.Access("Content/Panel/CardNode/CardName", out _cardNameState);
            transform.Access("Content/Panel/CardNode/Star", out _starNode);
            transform.Access("Content/Panel/EmptyIcon/UserIcon", out _playerIcon);
            transform.Access("Content/Panel/EmptyIcon/NameTxt", out _playerName);
            transform.AddButton("Content/Panel/CloseBtn", Close);
            transform.AddButton("Content/Panel/ConfirmBtn", Close);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 1)
            {
                RefreshCard((int)items[0]);
                RefreshPlayer((PlayerOpenInfo)items[1]);
            }
        }

        private void RefreshCard(int id)
        {
            var cardMan = Game.Manager.cardMan;
            var cardData = cardMan.GetCardData(id);
            var card = cardData?.GetConfig();
            var basicConfig = cardData?.GetObjBasicConfig();
            if (card == null || basicConfig == null) return;
            _cardNameState.Select(card.IsGold ? 1 : 0);
            _cardIcon.SetImage(basicConfig.Icon);
            _cardName.text = I18N.Text(basicConfig.Name);
            _starNode.Setup(card.Star);
        }

        private void RefreshPlayer(PlayerOpenInfo info)
        {
            var fbInfo = info?.FacebookInfo;
            if (fbInfo == null) return;
            _playerIcon.SetUrl(fbInfo.Avatar);
            _playerName.text = fbInfo.Name;
        }
    }
}