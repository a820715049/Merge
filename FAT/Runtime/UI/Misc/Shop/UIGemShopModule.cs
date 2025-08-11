/*
 * @Author: tang.yan
 * @Description: 钻石商店页面相关逻辑 
 * @Date: 2024-08-27 16:08:02
 */

using EL;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class UIGemShopModule : UIModuleBase
    {
        private ScrollRect _scrollRect;
        private GameObject _tileGo;
        private GameObject _slidePackGo;
        private UIMarketSlidePack _slidePackModule;
        private UIShopGemScrollRect _gemScrollRect;
        private bool _isPackSlideUnlock;

        public UIGemShopModule(Transform root) : base(root) { }

        protected override void OnCreate()
        {
            _scrollRect = ModuleRoot.FindEx<ScrollRect>("ScrollRect");
            var path = "ScrollRect/ViewPort/Content";
            ModuleRoot.FindEx(path + "/Tile", out _tileGo);
            ModuleRoot.FindEx(path + "/SlidePack", out _slidePackGo);
            var packRoot = ModuleRoot.Find(path + "/SlidePack/UIMarketSlidePack");
            _slidePackModule = AddModule(new UIMarketSlidePack(packRoot));
            _gemScrollRect = ModuleRoot.FindEx<UIShopGemScrollRect>(path + "/Gem/ScrollRect");
            _gemScrollRect.InitLayout();
        }

        protected override void OnParse(params object[] items) { }
        
        protected override void OnShow()
        {
            _isPackSlideUnlock = Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureMarketSlidePack);
            _RefreshScrollRect();
            _RefreshPackSlide();
            _RefreshGemScroll();
        }

        protected override void OnHide()
        {
            _slidePackModule?.Hide();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().AddListener(_RefreshPackSlide);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().RemoveListener(_RefreshPackSlide);
        }

        protected override void OnAddDynamicListener() { }

        protected override void OnRemoveDynamicListener() { }

        protected override void OnClose() { }

        private void _RefreshScrollRect()
        {
            _tileGo.SetActive(!_isPackSlideUnlock);
            _slidePackGo.SetActive(_isPackSlideUnlock);
            //布局变化后立即强制刷新
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
            var viewHeight = _scrollRect.viewport.rect.height;
            var contentHeight = _scrollRect.content.rect.height;
            _scrollRect.vertical = contentHeight > viewHeight;
            _scrollRect.horizontal = false;
            _scrollRect.verticalNormalizedPosition = 1; //每次刷新默认置顶
        }
        
        private void _RefreshPackSlide()
        {
            if (!_isPackSlideUnlock)
                return;
            var packSlideAct = Game.Manager.activity.LookupAny(EventType.MarketSlidePack) as PackMarketSlide;
            _slidePackModule?.Show(packSlideAct);
        }

        private void _RefreshGemScroll()
        {
            var gemTabData = (ShopTabGemData)Game.Manager.shopMan.GetShopTabData(ShopTabType.Gem);
            _gemScrollRect.UpdateData(gemTabData.GemDataList);
        }
    }
}