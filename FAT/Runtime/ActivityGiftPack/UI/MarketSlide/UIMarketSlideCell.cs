/*
 * @Author: tang.yan
 * @Description: 商店轮播礼包界面cell 
 * @Date: 2024-08-28 16:08:14
 */

using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class UIMarketSlideCell : UIModuleBase
    {
        private UIImageRes _bg;
        private TMP_Text _stockText;
        private UIImageRes _timeBg;
        private TMP_Text _timeText;
        private Button _buyBtn;
        private UITextState _iapPriceState;
        private HorizontalLayoutGroup _layoutGroup;
        private List<UICommonItem> _rewardItemList = new List<UICommonItem>(); //奖励Item List
        private Transform _label;//折扣标签节点
        private UIIAPLabel _iapLabel = new();

        //礼包数据
        private PackMarketSlide.MarketSlidePkgData _pkgData;
        //是否是空组
        private bool _isTemp;
        
        public UIMarketSlideCell(Transform root, bool isTemp) : base(root)
        {
            _isTemp = isTemp;
        }
        
        protected override void OnCreate()
        {
            if (_isTemp)
            {
                ModuleRoot.gameObject.SetActive(false);
                return;
            }
            _bg = ModuleRoot.FindEx<UIImageRes>("Bg");
            _stockText = ModuleRoot.FindEx<TMP_Text>("Text");
            _timeBg = ModuleRoot.FindEx<UIImageRes>("_cd/frame");
            _timeText = ModuleRoot.FindEx<TMP_Text>("_cd/text");
            _buyBtn = ModuleRoot.FindEx<Button>("BuyBtn");
            _iapPriceState = ModuleRoot.FindEx<UITextState>("BuyBtn/IAP/Num");
            _buyBtn.WithClickScale().FixPivot().onClick.AddListener(_OnBuyBtnClick);
            ModuleRoot.FindEx("BuyBtn/Label", out _label);
            _layoutGroup = ModuleRoot.FindEx<HorizontalLayoutGroup>("Group");
            //奖励Item 界面最多显示5个
            for (int i = 1; i < 6; i++)
            {
                var icon = ModuleRoot.FindEx<UICommonItem>("Group/Item" + i);
                _rewardItemList.Add(icon);
            }
        }

        protected override void OnParse(params object[] items)
        {
            _pkgData = items[0] as PackMarketSlide.MarketSlidePkgData;
        }
        
        protected override void OnShow()
        {
            if (_isTemp)
            {
                ModuleRoot.gameObject.SetActive(false);
                return;
            }
            if (_pkgData == null)
                return;
            _pkgData.PackTheme.Refresh(_bg, "bg");
            _pkgData.PackTheme.Refresh(_stockText, "desc");
            _pkgData.PackTheme.Refresh(_timeBg, "time");
            _pkgData.PackTheme.Refresh(_timeText, "time");
            _RefreshBuyBtn();
            _RefreshStock();
            _RefreshRewardItemList();
            _RefreshLabel();
        }

        protected override void OnHide()
        {
            
        }

        protected override void OnAddListener() { }

        protected override void OnRemoveListener() { }

        protected override void OnAddDynamicListener() { }

        protected override void OnRemoveDynamicListener() { }

        protected override void OnClose() { }

        public void Release()
        {
            if (_isTemp) { return; }
            //因为外部会频繁移除添加module 添加是会走OnCreate中的AddListener 所以在移除时主动Remove
            _buyBtn.onClick.RemoveListener(_OnBuyBtnClick);
        }

        private void _RefreshStock()
        {
            if (_pkgData == null)
                return;
            var count = _pkgData.TotalStock - _pkgData.BuyCount;
            //X/N：X代表剩余次数，N代表总次数
            _stockText.text = I18N.FormatText("#SysComDesc115", $"{count}/{_pkgData.TotalStock}");
        }
        
        public void OnMessageOneSecond(string timeText)
        {
            _RefreshCD(timeText);
        }
        
        public void OnMessageInitIAP()
        {
            _RefreshBuyBtn();
        }

        //刷新cd显示
        private void _RefreshCD(string timeText)
        {
            if (_pkgData == null)
                return;
            _timeText.text = timeText;
        }
        
        //刷新购买按钮
        private void _RefreshBuyBtn()
        {
            if (_pkgData == null)
                return;
            var valid = Game.Manager.iap.Initialized;
            _iapPriceState.Enabled(valid, _pkgData.Price);
            if (valid)
            {
                _buyBtn.interactable = true;
                GameUIUtility.SetDefaultShader(_buyBtn.image);
            }
            else
            {
                _buyBtn.interactable = false;
                GameUIUtility.SetGrayShader(_buyBtn.image);
            }
        }
        
        //刷新奖励UI
        private void _RefreshRewardItemList()
        {
            if (_pkgData == null)
                return;
            var payInfo = _pkgData.Goods.reward;
            _layoutGroup.spacing = _GetLayoutSpace(payInfo.Count);
            for (int i = 0; i < _rewardItemList.Count; i++)
            {
                if (i < payInfo.Count)
                {
                    _rewardItemList[i].gameObject.SetActive(true);
                    if (_pkgData.PackTheme.Theme.StyleInfo.TryGetValue("desc", out var style))
                    {
                        _rewardItemList[i].Refresh(payInfo[i], int.Parse(style));
                    }
                    else
                    {
                        _rewardItemList[i].Refresh(payInfo[i]);
                    }
                }
                else
                {
                    _rewardItemList[i].gameObject.SetActive(false);
                }
            }
        }
        
        private float _GetLayoutSpace(int goodsCount)
        {
            return goodsCount switch
            {
                5 => 26f,
                4 => 54f,
                3 => 70f,
                2 => 130f,
                _ => 26f
            };
        }
        
        private void _RefreshLabel()
        {
            _iapLabel.Clear();
            _label.gameObject.SetActive(false);
            if (_pkgData == null)
                return;
            var labelId = _pkgData.DetailConf.Label;
            if (labelId > 0)
            {
                _label.gameObject.SetActive(true);
                _iapLabel.Setup(_label, labelId, _pkgData.PackId);
            }
        }
        
        private void _OnBuyBtnClick()
        {
            if (_pkgData == null)
                return;
            var packSlideAct = Game.Manager.activity.LookupAny(fat.rawdata.EventType.MarketSlidePack) as PackMarketSlide;
            packSlideAct?.TryPurchase(_pkgData);
        }

        public void OnPurchaseComplete(int detailId, IList<RewardCommitData> rewardList)
        {
            if (_pkgData == null)
                return;
            if (_pkgData.DetailId != detailId)
                return;
            var maxCount = _rewardItemList.Count;
            for (var n = 0; n < rewardList.Count; ++n)
            {
                if (n >= maxCount) continue;
                var pos = _rewardItemList[n].transform.position;
                UIFlyUtility.FlyReward(rewardList[n], pos);
            }
        }
    }
}