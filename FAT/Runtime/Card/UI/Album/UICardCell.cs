/*
 * @Author: tang.yan
 * @Description: 集卡活动-卡册界面 卡片cell
 * @Date: 2024-01-28 15:51:11
 */

using EL;
using UnityEngine;
using TMPro;

namespace FAT
{
    public class UICardCell : UIModuleBase
    {
        private GameObject _lockGo;
        private GameObject _lockNormalGo;
        private GameObject _lockGoldGo;
        private CardStar _lockStar;
        private GameObject _normalGo;
        private UIImageRes _cardIcon;
        private CardStar _normalStar;
        private GameObject _newCardGo;
        private GameObject _cardCountGo;
        private TMP_Text _cardNum;
        private TMP_Text _cardName;
        private int _curCardId;
        private bool _isHideCount = false;

        public UICardCell(Transform root) : base(root)
        {
        }

        protected override void OnCreate()
        {
            ModuleRoot.FindEx("Lock", out _lockGo);
            ModuleRoot.FindEx("Lock/NormalLock", out _lockNormalGo);
            ModuleRoot.FindEx("Lock/GoldLock", out _lockGoldGo);
            _lockStar = ModuleRoot.FindEx<CardStar>("Lock/Star");
            ModuleRoot.FindEx("Normal", out _normalGo);
            _cardIcon = ModuleRoot.FindEx<UIImageRes>("Normal/Icon");
            _normalStar = ModuleRoot.FindEx<CardStar>("Normal/Star");
            ModuleRoot.FindEx("Normal/New", out _newCardGo);
            ModuleRoot.FindEx("Normal/Count", out _cardCountGo);
            _cardNum = ModuleRoot.FindEx<TMP_Text>("Normal/Count/Num");
            _cardName = ModuleRoot.FindEx<TMP_Text>("Name");
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                _curCardId = (int)items[0];
            }
            if (items.Length > 1)
            {
                _isHideCount = (bool)items[1];
            }
        }
        
        protected override void OnShow()
        {
            if (_curCardId <= 0) return;
            var cardMan = Game.Manager.cardMan;
            var albumData = cardMan.GetCardAlbumData(cardMan.IsNeedFakeAlbumData);
            if (albumData == null) return;
            var cardData = albumData.TryGetCardData(_curCardId);
            if (cardData == null) return;
            var cardConfig = cardData.GetConfig();
            if (cardConfig == null) return;
            var objBasicConfig = cardData.GetObjBasicConfig();
            if (objBasicConfig == null) return;
            bool isOwn = cardData.IsOwn;
            bool isGold = cardConfig.IsGold;
            if (!isOwn)
            {
                _lockGo.SetActive(true);
                _normalGo.SetActive(false);
                _lockNormalGo.SetActive(!isGold);
                _lockGoldGo.SetActive(isGold);
                _lockStar.Setup(cardConfig.Star);
            }
            else
            {
                _lockGo.SetActive(false);
                _normalGo.SetActive(true);
                _cardIcon.SetImage(objBasicConfig.Icon);
                _normalStar.Setup(cardConfig.Star);
                _newCardGo.SetActive(cardData.CheckIsNew());
                _cardCountGo.SetActive(cardData.OwnCount > 1 && !_isHideCount);
                _cardNum.text = $"+{cardData.OwnCount - 1}";
            }
            //刷新卡片名称与文本颜色
            _cardName.text = I18N.Text(objBasicConfig.Name);
            var fontResIndex = isOwn ? (isGold ? 38 : 8) : (isGold ? 39 : 8);
            var config = FontMaterialRes.Instance.GetFontMatResConf(fontResIndex);
            config?.ApplyFontMatResConfig(_cardName);
        }

        protected override void OnHide()
        {
            _isHideCount = false;
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_CARD_REDPOINT_UPDATE>().AddListener(_RefreshCardRP);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_CARD_REDPOINT_UPDATE>().RemoveListener(_RefreshCardRP);
        }

        protected override void OnAddDynamicListener() { }

        protected override void OnRemoveDynamicListener() { }

        protected override void OnClose()
        {
            _isHideCount = false;
        }

        private void _RefreshCardRP(int cardId)
        {
            if (_curCardId <= 0 || cardId != _curCardId) return;
            var cardMan = Game.Manager.cardMan;
            var albumData = cardMan.GetCardAlbumData(cardMan.IsNeedFakeAlbumData);
            if (albumData == null) return;
            var cardData = albumData.TryGetCardData(_curCardId);
            if (cardData == null) return;
            bool isOwn = cardData.IsOwn;
            if (isOwn)
            {
                _newCardGo.SetActive(cardData.CheckIsNew());
            }
        }

        public void OnClickCard()
        {
            if (_curCardId <= 0) return;
            UIManager.Instance.OpenWindow(UIConfig.UICardInfo, _curCardId);
        }
    }
}