/*
 * @Author: pengjian.zhang
 * @Description: 卡片详情界面
 * @Date: 2024-01-16 10:14:02
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT
{
    public class UICardInfo : UIBase
    {
        [SerializeField] private Image goldBg; //未获得 金卡背景
        [SerializeField] private Image normalBg; //未获得 卡片背景
        [SerializeField] private TMP_Text cardName;
        [SerializeField] private TMP_Text goldCardName;
        [SerializeField] private TMP_Text getGoldCardName;
        [SerializeField] private TMP_Text cardNum;
        [SerializeField] private GameObject cardNumGo;
        [SerializeField] private GameObject newCardGo;
        [SerializeField] private GameObject emptyStarRoot;
        [SerializeField] private GameObject starRoot;
        [SerializeField] private UIImageRes cardIcon;
        [SerializeField] private List<GameObject> emptyStarList; //为获得卡片时 灰色星级展示
        [SerializeField] private List<GameObject> starList; //星级展示
        [SerializeField] private GameObject cardNode;
        [SerializeField] private GameObject tradeNode;
        [SerializeField] private GameObject emptyNode;
        [SerializeField] private GameObject forbidNode;
        [SerializeField] private TextMeshProUGUI tradeTips;
        [SerializeField] private UIStateGroup stateGroup;
        [SerializeField] private Transform posTrade;
        [SerializeField] private Transform posNormal;

        private int curCardId; //当前卡片id

        protected override void OnCreate()
        {
            transform.AddButton("Mask", Close);
            transform.AddButton("Content/TradeNode/SendBtn", ClickSend);
            transform.AddButton("Content/EmptyNode/GroupBtn", ClickGroup);
        }

        protected override void OnPreOpen()
        {
            _RefreshInfoPanel();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0) curCardId = (int)items[0];
        }

        protected override void OnAddListener()
        {
        }

        protected override void OnRefresh()
        {
        }

        protected override void OnRemoveListener()
        {
        }

        protected override void OnPreClose()
        {
            var cardMan = Game.Manager.cardMan;
            if (!cardMan.IsNeedFakeAlbumData)
                //关闭界面时标记该卡片为已查看
                cardMan.SetCardSee(curCardId);
        }

        protected override void OnPostClose()
        {
        }

        private void _RefreshInfoPanel()
        {
            var cardMan = Game.Manager.cardMan;
            var cardData = cardMan.GetCardData(curCardId, cardMan.IsNeedFakeAlbumData);
            var card = Game.Manager.objectMan.GetCardConfig(curCardId);
            var basicConfig = Game.Manager.objectMan.GetBasicConfig(curCardId);
            cardNode.transform.position = cardMan.IsCardTradeUnlock && !cardMan.IsNeedFakeAlbumData
                ? posTrade.position
                : posNormal.position;
            //金卡无论是否拥有都显示无法赠送卡片的提示
            forbidNode.SetActive(cardMan.IsCardTradeUnlock && !cardMan.IsNeedFakeAlbumData && card.IsGold);
            //玩家预览未获得卡片
            if (!cardData.IsOwn)
            {
                emptyNode.SetActive(cardMan.IsCardTradeUnlock && !card.IsGold);
                tradeNode.SetActive(false);
                cardIcon.gameObject.SetActive(false);
                //金卡预览
                if (card.IsGold)
                {
                    goldCardName.gameObject.SetActive(true);
                    cardName.gameObject.SetActive(false);
                    getGoldCardName.gameObject.SetActive(false);
                }
                else
                {
                    goldCardName.gameObject.SetActive(false);
                    cardName.gameObject.SetActive(true);
                    getGoldCardName.gameObject.SetActive(false);
                }

                goldBg.gameObject.SetActive(card.IsGold);
                normalBg.gameObject.SetActive(!card.IsGold);
            }
            else
            {
                tradeNode.SetActive(cardMan.IsCardTradeUnlock && !cardMan.IsNeedFakeAlbumData && !card.IsGold);
                emptyNode.SetActive(false);
                tradeTips.text = I18N.FormatText("#SysComDesc652",
                    cardMan.CurGiveCardNum + "/" + Game.Manager.configMan.globalConfig.GiveCardNum);
                stateGroup.Select(cardData.OwnCount > 1 &&
                                  cardMan.CurGiveCardNum < Game.Manager.configMan.globalConfig.GiveCardNum
                    ? 0
                    : 1);
                cardIcon.gameObject.SetActive(true);
                goldBg.gameObject.SetActive(false);
                normalBg.gameObject.SetActive(false);
                if (card.IsGold)
                {
                    getGoldCardName.gameObject.SetActive(true);
                    cardName.gameObject.SetActive(false);
                    goldCardName.gameObject.SetActive(false);
                }
                else
                {
                    getGoldCardName.gameObject.SetActive(false);
                    cardName.gameObject.SetActive(true);
                    goldCardName.gameObject.SetActive(false);
                }

                if (cardData.OwnCount > 1)
                {
                    var cardLeftNum = cardData.OwnCount - 1;
                    cardNum.text = string.Format("+{0}", cardLeftNum.ToString());
                }

                cardIcon.SetImage(basicConfig.Icon.ConvertToAssetConfig());
            }

            newCardGo.gameObject.SetActive(cardData.IsOwn ? cardData.CheckIsNew() : false);
            cardNumGo.gameObject.SetActive(cardData.IsOwn ? cardData.OwnCount > 1 : false);
            cardName.text = I18N.Text(basicConfig.Name);
            goldCardName.text = I18N.Text(basicConfig.Name);
            getGoldCardName.text = I18N.Text(basicConfig.Name);
            starRoot.gameObject.SetActive(cardData.IsOwn);
            emptyStarRoot.gameObject.SetActive(!cardData.IsOwn);
            for (var i = 0; i < starList.Count; i++) starList[i].gameObject.SetActive(false);
            for (var i = 0; i < card.Star; i++) starList[i].gameObject.SetActive(true);
            for (var i = 0; i < emptyStarList.Count; i++) emptyStarList[i].gameObject.SetActive(false);
            for (var i = 0; i < card.Star; i++) emptyStarList[i].gameObject.SetActive(true);
        }

        private void ClickSend()
        {
            var cardMan = Game.Manager.cardMan;
            var cardData = cardMan.GetCardData(curCardId, cardMan.IsNeedFakeAlbumData);
            if (cardData.OwnCount <= 1)
            {
                UIManager.Instance.OpenWindow(UIConfig.UICardSendTips, true,
                    transform.Find("Content/TradeNode/SendBtn").position, 0f);
                return;
            }

            if (cardMan.CurGiveCardNum >= Game.Manager.configMan.globalConfig.GiveCardNum)
            {
                UIManager.Instance.OpenWindow(UIConfig.UICardSendTips, false,
                    transform.Find("Content/TradeNode/SendBtn").position, 0f);
                return;
            }

            //打开赠卡界面
            Game.Manager.cardMan.TryOpenUICardGifting(cardData.CardId);
            Close();
        }


        private void ClickGroup()
        {
            //打开交流群
            Game.Manager.cardMan.JumpTradingGroup(1);
            Close();
        }
    }
}