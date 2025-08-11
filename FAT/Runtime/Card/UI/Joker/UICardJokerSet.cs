/*
 * @Author: tang.yan
 * @Description: 万能卡使用后获得卡片界面 
 * @Date: 2024-03-27 14:03:01
 */
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using EL;
using fat.rawdata;

namespace FAT
{
    public class UICardJokerSet : UIBase
    {
        [SerializeField] private UIImageRes cardIcon;
        [SerializeField] private CardStar normalStar;
        [SerializeField] private GameObject newCardGo;
        [SerializeField] private GameObject cardCountGo;
        [SerializeField] private TMP_Text cardNum;
        [SerializeField] private TMP_Text cardName;
        [SerializeField] private GameObject completeTipsGo;
        [SerializeField] private Button claimBtn;
        
        private int _curCardId;
        private bool _isOwn;
        private bool _isComplete;
        private int _ownCount;
        
        protected override void OnCreate()
        {
            claimBtn.onClick.AddListener(_OnBtnClaim);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 4)
            {
                _curCardId = (int)items[0];
                _isOwn = (bool)items[1];
                _isComplete = (bool)items[2];
                _ownCount = (int)items[3];
            }
        }

        protected override void OnPreOpen()
        {
            _RefreshInfo();
        }

        protected override void OnAddListener()
        {
        }

        protected override void OnRemoveListener()
        {
        }

        protected override void OnPostClose()
        {
            _curCardId = 0;
            _isOwn = false;
            _isComplete = false;
            _ownCount = 0;
            Game.Manager.cardMan.TryOpenPackDisplay();
        }

        private void _RefreshInfo()
        {
            var albumData = Game.Manager.cardMan.GetCardAlbumData();
            if (albumData == null) return;
            var cardData = albumData.TryGetCardData(_curCardId);
            if (cardData == null) return;
            var cardConfig = cardData.GetConfig();
            if (cardConfig == null) return;
            var objBasicConfig = cardData.GetObjBasicConfig();
            if (objBasicConfig == null) return;
            var isGold = cardConfig.IsGold;
            newCardGo.SetActive(!_isOwn);
            cardIcon.SetImage(objBasicConfig.Icon);
            normalStar.Setup(cardConfig.Star);
            cardCountGo.SetActive(_isOwn && _ownCount > 1);
            cardNum.text = _isOwn ? $"+{_ownCount - 1}" : "";
            completeTipsGo.SetActive(_isComplete);
            //刷新卡片名称与文本颜色
            cardName.text = I18N.Text(objBasicConfig.Name);
            var fontResIndex = isGold ? 38 : 8;
            var config = FontMaterialRes.Instance.GetFontMatResConf(fontResIndex);
            config?.ApplyFontMatResConfig(cardName);
        }

        private void _OnBtnClaim()
        {
            Close();
        }
    }
}