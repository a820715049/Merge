/*
 * @Author: tang.yan
 * @Description: 万能卡选卡界面卡牌cell
 * @Date: 2024-03-29 12:03:14
 */
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using EL;
using fat.rawdata;

namespace FAT
{
    public class UICardJokerItemCell : UIGenericItemBase<int>
    {
        [SerializeField] private GameObject lockGo;
        [SerializeField] private GameObject lockNormalGo;
        [SerializeField] private GameObject lockGoldGo;
        [SerializeField] private CardStar lockStar;
        [SerializeField] private GameObject lockSelectGo;
        [SerializeField] private GameObject normalGo;
        [SerializeField] private UIImageRes cardIcon;
        [SerializeField] private CardStar normalStar;
        [SerializeField] private GameObject newCardGo;
        [SerializeField] private GameObject cardCountGo;
        [SerializeField] private TMP_Text cardNum;
        [SerializeField] private TMP_Text cardName;
        [SerializeField] private GameObject cardSelectGo;
        [SerializeField] private UIImageState cardSelectState;
        [SerializeField] private GameObject completeTipsGo;
        [SerializeField] private Button selectBtn;

        private int _curCardId;
        private bool _canSelect;
        private CardJokerData _curJokerCardData;
        private bool _isGold;
        
        protected override void InitComponents()
        {
            _Reset();
            selectBtn.onClick.AddListener(_OnBtnSelect);
        }
        
        protected override void UpdateOnDataChange()
        {
            _Reset();
            _Refresh();
        }

        protected override void UpdateOnForce()
        {
            _RefreshSelectState();
        }

        protected override void UpdateOnDataClear()
        {
            _Reset();
        }

        private void _Refresh()
        {
            _curCardId = mData;
            if (_curCardId <= 0) return;
            _RefreshInfo();
            _RefreshSelectState();
        }

        private void _RefreshInfo()
        {
            var roundData = Game.Manager.cardMan.GetCardRoundData();
            if (roundData == null) return;
            var albumData = roundData.TryGetCardAlbumData();
            if (albumData == null) return;
            _curJokerCardData = roundData.GetCurIndexJokerData();
            if (_curJokerCardData == null) return;
            var cardData = albumData.TryGetCardData(_curCardId);
            if (cardData == null) return;
            var cardConfig = cardData.GetConfig();
            if (cardConfig == null) return;
            var objBasicConfig = cardData.GetObjBasicConfig();
            if (objBasicConfig == null) return;
            bool isNormalJoker = _curJokerCardData.IsGoldCard == 0;
            bool isOwn = cardData.IsOwn;
            _isGold = cardConfig.IsGold;
            _canSelect = isNormalJoker ? !_isGold : true;//普通万能卡只能兑换普通卡 金卡万能卡都能兑换
            newCardGo.SetActive(!isOwn);
            lockSelectGo.SetActive(!_canSelect);
            if (!_canSelect && !isOwn)
            {
                lockGo.SetActive(true);
                normalGo.SetActive(false);
                lockNormalGo.SetActive(!_isGold);
                lockGoldGo.SetActive(_isGold);
                lockStar.Setup(cardConfig.Star);
            }
            else
            {
                lockGo.SetActive(false);
                normalGo.SetActive(true);
                cardIcon.SetImage(objBasicConfig.Icon);
                normalStar.Setup(cardConfig.Star);
                cardCountGo.SetActive(_canSelect && isOwn && cardData.OwnCount > 1);
                cardNum.text = (_canSelect && isOwn) ? $"+{cardData.OwnCount - 1}" : "";
                //能选择且没有且卡组中只查这一张卡时显示
                albumData.GetCollectProgress(cardData.BelongGroupId, out var ownCount, out var allCount);
                completeTipsGo.SetActive(_canSelect && !isOwn && ownCount == allCount - 1);
            }
            //刷新卡片名称与文本颜色
            cardName.text = I18N.Text(objBasicConfig.Name);
            var fontResIndex = (_canSelect || isOwn) ? (_isGold ? 38 : 8) : (_isGold ? 39 : 8);
            var config = FontMaterialRes.Instance.GetFontMatResConf(fontResIndex);
            config?.ApplyFontMatResConfig(cardName);
        }

        private void _RefreshSelectState()
        {
            var isSelect = _curJokerCardData != null && _curJokerCardData.GetCurSelectCardId() == _curCardId;
            cardSelectGo.SetActive(isSelect);
            if (isSelect)
            {
                cardSelectState.Setup(_isGold ? 1 : 0);
            }
        }

        private void _OnBtnSelect()
        {
            if (_curJokerCardData == null)
                return;
            if (_canSelect)
            {
                _curJokerCardData.SetCurSelectCardId(_curCardId);
            }
            else
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.ShinyJokerRequire);
            }
        }
        

        private void _Reset()
        {
            _curCardId = 0;
            _canSelect = false;
            _curJokerCardData = null;
            _isGold = false;
        }
        
        public void ForceRefresh(int cardId)
        {
            _curCardId = cardId;
            _RefreshInfo();
            cardSelectGo.SetActive(false);
            selectBtn.interactable = false;
        }

        public void ForceClear()
        {
            _Reset();
        }
    }
}