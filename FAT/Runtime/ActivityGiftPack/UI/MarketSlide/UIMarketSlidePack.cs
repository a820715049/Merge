/*
 * @Author: tang.yan
 * @Description: 商店轮播礼包界面逻辑 
 * @Date: 2024-08-28 11:08:01
 */

using System.Collections.Generic;
using EL;
using UnityEngine;
using TMPro;

namespace FAT
{
    public class UIMarketSlidePack : UIModuleBase
    {
        private GameObject _noPack;
        private GameObject _hasPack;
        //活动已解锁但没有开启相关节点
        private GameObject _comingGo;
        //活动已解锁已开启且购买了所有礼包相关节点
        private GameObject _inCdGo;
        private UIImageRes _inCdBg;
        private TMP_Text _inCdText;
        private UIImageRes _inCdTimeBg;
        private TMP_Text _inCdTimeText;
        //礼包相关
        private UIMarketSlideDrag _slideDrag;
        private List<UIMarketSlideCell> _packCellList = new List<UIMarketSlideCell>();
        private PackMarketSlide _packSlideAct;
        //界面刷新用到的所有可以购买的礼包
        private List<PackMarketSlide.MarketSlidePkgData> _canBuyPackList = new();
        //底部页签点
        private List<UIMarketSlidePoint> _pointList = new List<UIMarketSlidePoint>();
        private class UIMarketSlidePoint
        {
            public GameObject PointGo;
            public GameObject SelectGo;
            public GameObject UnSelectGo;

            public void Prepare(Transform root)
            {
                PointGo = root.gameObject;
                root.FindEx("Select", out SelectGo);
                root.FindEx("UnSelect", out UnSelectGo);
            }
        }

        private int _autoDragCd = 0;     //每隔多少秒尝试自动拖拽
        private int _curWaitTime = 0;    //目前已等待了多久
        private bool _canAutoDrag = false;    //目前是否可以自动拖拽 购买流程中以及手动拖拽时都不允许自动拖拽
        
        public UIMarketSlidePack(Transform root) : base(root)
        {
        }

        protected override void OnCreate()
        {
            ModuleRoot.FindEx("NoPack", out _noPack);
            ModuleRoot.FindEx("NoPack/Coming", out _comingGo);
            var cdPath = "NoPack/InCd";
            ModuleRoot.FindEx(cdPath, out _inCdGo);
            _inCdBg = ModuleRoot.FindEx<UIImageRes>(cdPath + "/Bg");
            _inCdText = ModuleRoot.FindEx<TMP_Text>(cdPath + "/Text");
            _inCdTimeBg = ModuleRoot.FindEx<UIImageRes>(cdPath + "/_cd/frame");
            _inCdTimeText = ModuleRoot.FindEx<TMP_Text>(cdPath + "/_cd/text");
            ModuleRoot.FindEx("Pack", out _hasPack);
            _slideDrag = ModuleRoot.FindEx<UIMarketSlideDrag>("Pack/SlideDrag");
            _slideDrag.Prepare();
            var path = "Pack/SlidePoint/PointCell";
            for (int i = 1; i <= 5; i++)
            {
                ModuleRoot.FindEx(path + i, out Transform root);
                var point = new UIMarketSlidePoint();
                point.Prepare(root);
                _pointList.Add(point);
            }
        }

        protected override void OnParse(params object[] items)
        {
            _packSlideAct = null;
            if (items.Length > 0)
            {
                _packSlideAct = items[0] as PackMarketSlide;
            }
        }
        
        protected override void OnShow()
        {
            _autoDragCd = _packSlideAct?.confD?.Duration ?? 5;
            _Refresh();
        }

        protected override void OnHide()
        {
            _ResetDragState(false);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnSecondUpdate);
            MessageCenter.Get<MSG.IAP_INIT>().AddListener(_RefreshPrice);
            MessageCenter.Get<MSG.GAME_MARKET_SLIDE_TRY_BUY>().AddListener(_OnTryBuyPack);
            MessageCenter.Get<MSG.GAME_MARKET_SLIDE_PGK_REC_SUCC>().AddListener(_PurchaseComplete);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnSecondUpdate);
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(_RefreshPrice);
            MessageCenter.Get<MSG.GAME_MARKET_SLIDE_TRY_BUY>().RemoveListener(_OnTryBuyPack);
            MessageCenter.Get<MSG.GAME_MARKET_SLIDE_PGK_REC_SUCC>().RemoveListener(_PurchaseComplete);
        }

        protected override void OnAddDynamicListener() { }

        protected override void OnRemoveDynamicListener() { }

        protected override void OnClose()
        {
            _slideDrag.ClearDrag();
            _ResetDragState(false);
        }

        private void _Refresh()
        {
            if (_packSlideAct == null)
            {
                //活动解锁但没有开启
                _noPack.SetActive(true);
                _hasPack.SetActive(false);
                _inCdGo.SetActive(false);
                _comingGo.SetActive(true);
                _ResetDragState(false);
            }
            else
            {
                var isSoldOut = _packSlideAct.CheckIsAllSoldOut();
                _ResetDragState(!isSoldOut);
                if (isSoldOut)
                {
                    //活动解锁已开启且所有礼包都卖完
                    _noPack.SetActive(true);
                    _hasPack.SetActive(false);
                    _inCdGo.SetActive(true);
                    _comingGo.SetActive(false);
                    _packSlideAct.InCdTheme.Refresh(_inCdBg, "bg");
                    _packSlideAct.InCdTheme.Refresh(_inCdText, "desc");
                    _packSlideAct.InCdTheme.Refresh(_inCdTimeBg, "time");
                    _packSlideAct.InCdTheme.Refresh(_inCdTimeText, "time");
                }
                else
                {
                    _noPack.SetActive(false);
                    _hasPack.SetActive(true);
                    _RefreshCanBuyList();
                    _RefreshSlideDragNum();
                    _RefreshSlidePointNum();
                    _RefreshPack();
                    _RefreshCD();
                }
            }
        }

        private void _RefreshCanBuyList()
        {
            _canBuyPackList.Clear();
            _packSlideAct?.FillCanBuyPack(_canBuyPackList);
            //可购买礼包小于等于1时不执行自动拖拽逻辑
            if (_canBuyPackList.Count <= 1)
                _ResetDragState(false);
        }

        //刷新数量
        private void _RefreshSlideDragNum()
        {
            _slideDrag.ClearDrag();
            _slideDrag.InitDrag(_OnDragSuccess, _OnDragStateChange);
            var totalCount = _canBuyPackList.Count + 2;
            //数量相等时不刷新 但是重置一下位置
            if (totalCount == _packCellList.Count)
            {
                _slideDrag.GetCurShowIndex(out var curIndex, out _, out _);
                _slideDrag.SetDragStartProgress(curIndex);
                return;
            }
            //全部清空
            foreach (var cell in _packCellList)
            {
                cell.Release();
                _slideDrag.ReleaseLayout(cell.ModuleRoot.gameObject);
            }
            ClearModule();
            _packCellList.Clear();
            //重新生成
            for (int i = 1; i <= totalCount; i++)
            {
                bool isTemp = i == 1 || i == totalCount;
                var trans = _slideDrag.CreateLayout(isTemp);
                if (trans != null)
                {
                    _packCellList.Add(AddModule(new UIMarketSlideCell(trans, isTemp)));
                }
            }
            //刷新目前的总数量
            _slideDrag.RefreshLayoutNum(totalCount);
            //默认从第1个礼包开始
            _slideDrag.SetDragStartProgress(1);
        }

        //刷新页签点
        private void _RefreshSlidePointNum()
        {
            var totalCount = _canBuyPackList.Count;
            for (int i = 0; i < _pointList.Count; i++)
            {
                //如果只剩1个礼包 则圆点不显示
                _pointList[i].PointGo.SetActive(i < totalCount && totalCount > 1);
            }
        }

        private void _RefreshPack()
        {
            if (_packSlideAct == null)
                return;
            _slideDrag.GetCurShowIndex(out int curIndex, out int leftIndex, out int rightIndex);
            //刷新对应cell
            _canBuyPackList.TryGetByIndex(curIndex - 1, out var curPkgData);
            _packCellList[curIndex].Show(curPkgData);
            _canBuyPackList.TryGetByIndex(leftIndex - 1, out var leftPkgData);
            _packCellList[leftIndex].Show(leftPkgData);
            _canBuyPackList.TryGetByIndex(rightIndex - 1, out var rightPkgData);
            _packCellList[rightIndex].Show(rightPkgData);
            //刷新底部页签点
            _RefreshSlidePoint(curIndex - 1);
        }

        private void _RefreshSlidePoint(int selectIndex)
        {
            for (int i = 0; i < _pointList.Count; i++)
            {
                var point = _pointList[i];
                point.SelectGo.SetActive(i == selectIndex);
                point.UnSelectGo.SetActive(i != selectIndex);
            }
        }

        private void _OnSecondUpdate()
        {
            _RefreshCD();
            _TryAutoDrag();
        }

        private void _RefreshCD()
        {
            if (_packSlideAct == null)
                return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _packSlideAct.endTS - t);
            var timeText = UIUtility.CountDownFormat(diff);
            foreach (var cell in _packCellList)
            {
                cell.OnMessageOneSecond(timeText);
            }
            _inCdTimeText.text = timeText;
        }
        
        private void _RefreshPrice() {
            if (_packSlideAct == null)
                return;
            foreach (var cell in _packCellList)
            {
                cell.OnMessageInitIAP();
            }
        }
        
        private void _PurchaseComplete(int detailId, IList<RewardCommitData> list_) {
            if (_packSlideAct == null)
                return;
            foreach (var cell in _packCellList)
            {
                cell.OnPurchaseComplete(detailId, list_);
            }
            //购买流程结束后允许自动拖拽
            _ResetDragState(true);
            _Refresh();
        }
        
        private void _OnDragSuccess(bool isLeft)
        {
            _RefreshPack();
        }
        
        //手动拖拽时不允许发生自动拖拽
        private void _OnDragStateChange(bool isDrag)
        {
            _ResetDragState(!isDrag);
        }

        //发起购买流程时不允许发生自动拖拽
        private void _OnTryBuyPack()
        {
            _ResetDragState(false);
        }

        private void _ResetDragState(bool canDrag)
        {
            _canAutoDrag = canDrag;
            _curWaitTime = 0;
        }

        //每秒尝试自动拖拽
        private void _TryAutoDrag()
        {
            if (!_canAutoDrag) 
                return;
            _curWaitTime++;
            if (_curWaitTime < _autoDragCd)
                return;
            _curWaitTime = 0;
            _slideDrag.GetCurShowIndex(out _, out _, out int rightIndex);
            var count = _canBuyPackList.Count;
            var targetPage = rightIndex <= count ? rightIndex : 1;
            _slideDrag.MoveToPage(targetPage);
            //自动拖拽时关闭tips
            MessageCenter.Get<MSG.UI_CLOSE_LAYER>().Dispatch(UIConfig.UIShop.layer);
        }
    }
}