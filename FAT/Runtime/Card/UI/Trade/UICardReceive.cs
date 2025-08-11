/*
 *@Author:chaoran.zhang
 *@Desc:收到卡牌界面逻辑
 *@Created Time:2024.10.28 星期一 10:46:48
 */

using EL;
using TMPro;
using UnityEngine;
using fat.gamekitdata;

namespace FAT
{
    public class UICardReceive : UIBase
    {
        private UIImageRes _playerImg;
        private TextMeshProUGUI _tips;
        private UIImageRes _cardIcon;
        private Transform _starNode;
        private TextMeshProUGUI _normalName;
        private TextMeshProUGUI _goldName;
        private Transform _new;
        private TextMeshProUGUI _cardNum;
        private Transform _cardNumNode;

        protected override void OnCreate()
        {
            transform.AddButton("Content/CloseBtn", _OnBtnCloseClick);
            transform.AddButton("Content/ViewSetBtn", _OnBtnViewSetClick);
            transform.Access("Content/EmptyIcon/UserIcon", out _playerImg);
            transform.Access("Content/Tips", out _tips);
            transform.Access("Content/CardNode/icon", out _cardIcon);
            transform.Access("Content/CardNode/name", out _normalName);
            transform.Access("Content/CardNode/getGoldName", out _goldName);
            transform.Access("Content/CardNode/starRoot", out _starNode);
            transform.Access("Content/CardNode/newCardBg", out _new);
            transform.Access("Content/CardNode/numBg/num", out _cardNum);
            transform.Access("Content/CardNode/numBg", out _cardNumNode);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 1)
            {
                RefreshCard((int)items[0]);
                if (items[1] is PlayerOpenInfo info) RefreshPlayer(info);
            }

            Game.Manager.audioMan.TriggerSound("ReceiveCardSuccess");
        }

        private void RefreshCard(int id)
        {
            var cardMan = Game.Manager.cardMan;
            var cardData = cardMan.GetCardData(id);
            var card = Game.Manager.objectMan.GetCardConfig(id);
            var basicConfig = Game.Manager.objectMan.GetBasicConfig(id);
            _normalName.text = I18N.Text(basicConfig.Name);
            _goldName.text = I18N.Text(basicConfig.Name);
            _normalName.gameObject.SetActive(!card.IsGold);
            _goldName.gameObject.SetActive(card.IsGold);
            _cardIcon.SetImage(basicConfig.Icon);
            _tips.text = I18N.FormatText("#SysComDesc669", I18N.Text(basicConfig.Name));
            for (var i = 0; i < 5; i++) _starNode.GetChild(i).gameObject.SetActive(i < card.Star);
            _new.gameObject.SetActive(cardData.IsOwn ? cardData.CheckIsNew() : false);
            _cardNumNode.gameObject.SetActive(cardData.IsOwn ? cardData.OwnCount > 1 : false);
            _cardNum.text = $"+{cardData.OwnCount - 1}";
        }

        private void RefreshPlayer(PlayerOpenInfo info)
        {
            var fbInfo = info?.FacebookInfo;
            if (fbInfo == null) return;
            _playerImg.SetUrl(fbInfo.Avatar);
        }

        private void _OnBtnCloseClick()
        {
            _Display(false);
        }

        private void _OnBtnViewSetClick()
        {
            _Display(true);
        }

        private void _Display(bool isShowAnim)
        {
            Game.Manager.cardMan.TryDisplayReceiveCard(isShowAnim);
            //收卡成功后拉一下卡片收取箱信息
            Game.Manager.cardMan.TryPullPendingCardInfo();
            Close();
        }
    }
}