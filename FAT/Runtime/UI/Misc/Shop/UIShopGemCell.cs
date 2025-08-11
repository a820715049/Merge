/*
 * @Author: tang.yan
 * @Description: 商城界面格子cell 
 * @Date: 2023-11-07 15:11:07
 */

using System;
using EL;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using System.Collections.Generic;
using TMPro;

namespace FAT
{
    public class UIShopGemCell : FancyGridViewCell<ShopGemData, UICommonScrollGridDefaultContext>
    {
        //钻石
        [SerializeField] public GameObject gemInfoGo;
        [SerializeField] public UIImageRes gemIcon;
        [SerializeField] public TMP_Text gemNum;
        [SerializeField] public TMP_Text gemName;
        //通用
        [SerializeField] public Button tipsBtn;
        [SerializeField] public Button buyBtn;
        [SerializeField] public GameObject buyNormalGo;
        [SerializeField] public TMP_Text normalPrice;
        [SerializeField] public GameObject buyIapGo;
        [SerializeField] public UITextState iapPriceState;
        [SerializeField] public GameObject discountGo;
        public MBRewardIcon bonus;

        private ShopGemData _shopGemData;
        private UIIAPLabel _iapLabel = new();
        private int _lastLabelId = -1;

        public override void Initialize()
        {
            buyBtn.WithClickScale().FixPivot().onClick.AddListener(_OnClickBtnBuy);
        }
        
        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            
        }
        
        public override void UpdateContent(ShopGemData shopGemData)
        {
            if (shopGemData == null)
                return;
            _shopGemData = shopGemData;
            gemInfoGo.SetActive(true);
            if (_shopGemData.Image != null)
            {
                gemIcon.SetImage(shopGemData.Image.Group, shopGemData.Image.Asset);
            }
            var reward = shopGemData.Reward[0];
            if (reward != null)
            {
                gemNum.text = reward.Count.ToString();
            }
            gemName.text = I18N.Text(shopGemData.Name);
            tipsBtn.gameObject.SetActive(false);
            buyNormalGo.SetActive(false);
            normalPrice.text = "";
            buyIapGo.SetActive(true);
            var valid = Game.Manager.iap.Initialized;
            iapPriceState.Enabled(valid, Game.Manager.iap.PriceInfo(shopGemData.IapId));
            if (valid)
            {
                buyBtn.interactable = true;
                GameUIUtility.SetDefaultShader(buyBtn.image);
            }
            else
            {
                buyBtn.interactable = false;
                GameUIUtility.SetGrayShader(buyBtn.image);
            }
            bonus.Refresh(shopGemData.Reward[1]);
            //记录上次显示的标签id 避免重复加载
            var labelId = shopGemData.LabelId;
            if (_lastLabelId != labelId)
            {
                _lastLabelId = labelId;
                _iapLabel.Clear();
                discountGo.SetActive(_lastLabelId > 0);
                if (_lastLabelId > 0)
                    _iapLabel.Setup(discountGo.transform, _lastLabelId, shopGemData.PackId);
            }
        }

        private void _OnClickBtnBuy()
        {
            if (_shopGemData == null)
                return;
            Game.Manager.shopMan.TryBuyShopGemGoods(_shopGemData, buyBtn.transform.position);
        }
    }
}