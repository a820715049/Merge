/*
 * @Author: tang.yan
 * @Description: 商城界面
 * @Date: 2023-11-07 15:11:35
 */

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;
using FAT.Merge;

namespace FAT
{
    public class UIShop : UIBase
    {
        //toggle
        [SerializeField] private GameObject tabGo;
        [SerializeField] private List<UISimpleToggle> tabToggleList;
        [SerializeField] private List<GameObject> shopGoList;
        [SerializeField] private List<Animator> tabAnimatorList;
        //钻石商店页面逻辑 单独拿出来相对干净点
        private UIGemShopModule _gemShopModule;
        //能量商店
        [SerializeField] private List<UIShopEnergyCell> energyItemList;
        [SerializeField] private Transform shopLinkItemCell;
        [SerializeField] private Transform shopLinkItemRoot;
        [SerializeField] private Transform shopLinkRoot;
        private List<GameObject> cellList = new();
        private PoolItemType linkItemType = PoolItemType.SETTING_COMMUNITY_SHOP_ITEM;
        //棋子商店
        [SerializeField] private ScrollRect chessShopScroll;
        [SerializeField] private List<UIShopChessCell> chessTopItemList;
        [SerializeField] private TMP_Text cdText;
        [SerializeField] private Button refreshBtn;
        [SerializeField] private TMP_Text refreshPrice;
        [SerializeField] private List<MBShopOrderItem> orderItemList;
        [SerializeField] private float moveDuration;    //点击页签后列表的滑动时间
        [SerializeField] private float originXLeft;    //由左到右滑动起始位置(滑动到界面中心前)
        [SerializeField] private float originXRight;    //由右到左滑动起始位置

        private int _curSelectTabIndex = 0;
        private ShopTabType _curShowTabType = ShopTabType.None;
        private Color _colorNormal;
        private Color _colorHighlight;
        private Vector3 _doMoveOriginPos = new Vector3();   //滑动起始位置
        private Vector3 _linkdoMoveOriginPos = new Vector3();

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/BtnClose/Btn", base.Close);
            _InitToggle();
            var gemShopRoot = transform.Find("Content/Panel/Root/GemShop");
            _gemShopModule = AddModule(new UIGemShopModule(gemShopRoot));
            //注册能量购买按钮
            int index = 0;
            foreach (var energyCell in energyItemList)
            {
                int temp = index;
                energyCell.buyBtn.WithClickScale().FixPivot().onClick.AddListener(() => _OnEnergyBtnClick(temp));
                index++;
            }
            //注册随机棋子商品购买按钮
            index = 0;
            foreach (var chessCell in chessTopItemList)
            {
                int temp = index;
                chessCell.buyBtn.WithClickScale().FixPivot().onClick.AddListener(() => _OnRandomChessBtnClick(temp));
                chessCell.tipsBtn.WithClickScale().FixPivot().onClick.AddListener(() => _OnRandomChessTipsBtnClick(temp));
                index++;
            }
            //棋子商店刷新按钮
            refreshBtn.WithClickScale().FixPivot().onClick.AddListener(_OnRefreshChessBtnClick);
            //注册订单棋子商品购买按钮
            foreach (var orderItem in orderItemList)
            {
                orderItem.Setup();
            }
            ColorUtility.TryParseHtmlString("#4A73AD", out _colorNormal);
            ColorUtility.TryParseHtmlString("#C0701E", out _colorHighlight);
            GameObjectPoolManager.Instance.PreparePool(linkItemType, shopLinkItemCell.gameObject);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                var type = (ShopTabType)items[0];
                _curShowTabType = type;
                _curSelectTabIndex = (int)type - 1;
            }
        }

        protected override void OnPreOpen()
        {
            //商店打开时尝试Adjust打点
            Game.Manager.shopMan.AdjustTrackerShopOpen();
            //调整顶部资源栏显示状态
            _ChangeTopBarState(true);
            //通知资源栏播放入场动画
            UIManager.Instance.ForceChangeStatusUI(true);
            //刷新toggle页签
            _RefreshTabToggle();
            //暂时默认打开ui后从左滑入
            _DoMoveShopList(originXLeft);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().AddListener(_OnShopItemInfoChange);
            MessageCenter.Get<MSG.IAP_INIT>().AddListener(_OnIapInit);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnSecondUpdate);
            MessageCenter.Get<MSG.APP_ENTER_FOREGROUND_EVENT>().AddListener(_RefreshCommunityLinkReward);
            MessageCenter.Get<MSG.GAME_ORDER_TOKEN_MULTI_BEGIN>().AddListener(_OnMessageTokenMultiBegin);
            MessageCenter.Get<MSG.GAME_ORDER_TOKEN_MULTI_END>().AddListener(_OnMessageTokenMultiEnd);
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().AddListener(_OnMessageActivityUpdate);
        }

        protected override void OnRefresh()
        {
            _RefreshTabToggle();
            _DoMoveShopList(originXLeft);
        }

        protected override void OnPause()
        {
            //调整顶部资源栏显示状态
            _ChangeTopBarState(false);
        }

        protected override void OnResume()
        {
            //调整顶部资源栏显示状态
            _ChangeTopBarState(true);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().RemoveListener(_OnShopItemInfoChange);
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(_OnIapInit);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnSecondUpdate);
            MessageCenter.Get<MSG.APP_ENTER_FOREGROUND_EVENT>().RemoveListener(_RefreshCommunityLinkReward);
            MessageCenter.Get<MSG.GAME_ORDER_TOKEN_MULTI_BEGIN>().RemoveListener(_OnMessageTokenMultiBegin);
            MessageCenter.Get<MSG.GAME_ORDER_TOKEN_MULTI_END>().RemoveListener(_OnMessageTokenMultiEnd);
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().RemoveListener(_OnMessageActivityUpdate);
        }

        protected override void OnPreClose()
        {
            //界面关闭时通知资源栏播出场动画
            UIManager.Instance.ForceChangeStatusUI(false);
        }

        protected override void OnPostClose()
        {
            BoardViewManager.Instance.OnInventoryClose();
            //调整顶部资源栏显示状态
            _ChangeTopBarState(false);
            // 商城关闭时尝试发起推送开启提醒
            Game.Manager.notification.TryRemindShopClose();
            _ClearCommunityShopLinkItem();
        }

        //避免重复进入相同的状态
        private bool _topBarState = false;
        private void _ChangeTopBarState(bool isTop)
        {
            if (_topBarState == isTop)
                return;
            _topBarState = isTop;
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(!isTop);
            if (isTop)
                MessageCenter.Get<MSG.UI_TOP_BAR_PUSH_STATE>().Dispatch(UIStatus.LayerState.SubStatus);
            else
                MessageCenter.Get<MSG.UI_TOP_BAR_POP_STATE>().Dispatch();
        }

        private void _InitToggle()
        {
            //注册点击事件
            int tempIndex = 0;
            foreach (var toggle in tabToggleList)
            {
                int index = tempIndex;
                toggle.onValueChanged.AddListener(isSelect => _OnToggleSelect(index, isSelect));
                tempIndex++;
            }
        }

        private void _OnToggleSelect(int index, bool isSelect)
        {
            if (isSelect && _curSelectTabIndex != index)
            {
                _curShowTabType = (ShopTabType)(index + 1);
                float offsetX;
                //判断是从左-右 还是右-左
                if (index > _curSelectTabIndex)
                {
                    offsetX = originXRight;
                }
                else
                {
                    offsetX = originXLeft;
                }
                _curSelectTabIndex = index;
                tabAnimatorList[_curSelectTabIndex].SetTrigger("Punch");
                _DoMoveShopList(offsetX);
                _RefreshUI(true);
            }
        }

        /// <summary>
        /// 滑动商品列表
        /// </summary>
        /// <param name="offsetX">X轴偏移</param>
        private void _DoMoveShopList(float offsetX)
        {
            _doMoveOriginPos.Set(offsetX, shopGoList[_curSelectTabIndex].transform.position.y, shopGoList[_curSelectTabIndex].transform.position.z);
            shopGoList[_curSelectTabIndex].transform.position = _doMoveOriginPos;
            shopGoList[_curSelectTabIndex].transform.DOKill();
            shopGoList[_curSelectTabIndex].transform.DOLocalMoveX(0, moveDuration);
            if (_curShowTabType == ShopTabType.Energy)
            {
                _DoMoveShopLinkList(offsetX);
            }
        }

        private void _RefreshTabToggle()
        {
            var shopMan = Game.Manager.shopMan;
            int tempIndex = 1;
            int showTabCount = 0;
            foreach (var toggle in tabToggleList)
            {
                bool temp = shopMan.CheckShopTabIsUnlock((ShopTabType)tempIndex);
                if (temp)
                    showTabCount++;
                toggle.gameObject.SetActive(temp);
                tempIndex++;
            }
            tabToggleList[_curSelectTabIndex].SetIsOnWithoutNotify(true);
            //显示的标签数小于等于1个的时候 整个toggle不显示
            tabGo.SetActive(showTabCount > 1);
            _RefreshUI(true);
        }

        private void _OnShopItemInfoChange()
        {
            _RefreshUI();
        }

        private void _OnIapInit()
        {
            _RefreshUI();
        }

        private void _OnSecondUpdate()
        {
            _RefreshChessShopCd();
        }
        
        private void _OnMessageTokenMultiBegin(Item item)
        {
            _RefreshUI();
        }
        
        private void _OnMessageTokenMultiEnd()
        {
            _RefreshUI();
        }

        //活动开启/结束/刷新
        private void _OnMessageActivityUpdate()
        {
            _RefreshUI();
        }

        private void _RefreshUI(bool isMoveTop = false)
        {
            for (int i = 0; i < shopGoList.Count; i++)
            {
                shopGoList[i].gameObject.SetActive(i == _curSelectTabIndex);
            }
            _ClearCommunityShopLinkItem();
            var communityLinkMan = Game.Manager.communityLinkMan;
            shopLinkRoot.gameObject.SetActive(_curShowTabType == ShopTabType.Energy && communityLinkMan.IsShowShopLink());
            if (_curShowTabType == ShopTabType.Gem)
                _gemShopModule.Show();
            else
                _gemShopModule.Hide();
            if (_curShowTabType == ShopTabType.Energy)
            {
                _RefreshTabEnergy();
                _OnRefreshCommunityShopLinkItem();
            }
            if (_curShowTabType == ShopTabType.Chess)
            {
                _RefreshTabChess(isMoveTop);
            }
        }

        private void _RefreshTabEnergy()
        {
            var energyTabData = (ShopTabEnergyData)Game.Manager.shopMan.GetShopTabData(_curShowTabType);
            int index = 0;
            foreach (var energyCell in energyItemList)
            {
                var energyData = energyTabData.EnergyDataList[index];
                var curSellConfig = energyData?.CurSellGoodsConfig;
                if (curSellConfig != null)
                {
                    var image = energyData.GetCurSellGoodsImage();
                    if (image != null)
                    {
                        energyCell.energyIcon.SetImage(image.Group, image.Asset);
                    }
                    energyCell.energyName.text = energyData.GetCurSellGoodsName();
                    energyCell.energyNum.text = energyData.GetCurSellGoodsNumStr();
                    var price = curSellConfig.Price.ConvertToRewardConfig();
                    energyCell.normalPrice.text = price != null ? price.Count.ToString() : "";
                }
                index++;
            }
        }

        private void _RefreshTabChess(bool isMoveTop = false)
        {
            if (isMoveTop)
                chessShopScroll.verticalNormalizedPosition = 1; //根据外部传参决定刷新时scroll是否置顶
            var chessTabData = (ShopTabChessData)Game.Manager.shopMan.GetShopTabData(_curShowTabType);
            _RefreshChessShopCd(chessTabData);
            refreshPrice.text = I18N.Text("#SysComDesc71") + Game.Manager.configMan.globalConfig.MarketRefreshNum;
            int index = 0;
            //顶部权重随机棋子
            foreach (var chessCell in chessTopItemList)
            {
                chessTabData.GetChessRandomData(index, out var chessRandomData);
                var curSellConfig = chessRandomData?.CurSellGoodsConfig;
                if (curSellConfig != null)
                {
                    chessCell.chessGo.SetActive(true);
                    bool isHighlight = chessRandomData.IsHighlight;
                    chessCell.bgNormalGo.SetActive(!isHighlight);
                    chessCell.chessFrameBg.Setup(0);
                    chessCell.bgHighlightGo.SetActive(isHighlight);
                    var image = chessRandomData.GetCurSellGoodsImage();
                    if (image != null)
                    {
                        chessCell.chessIcon.SetImage(image.Group, image.Asset);
                    }
                    chessCell.chessName.color = isHighlight ? _colorHighlight : _colorNormal;
                    chessCell.chessName.text = chessRandomData.GetCurSellGoodsName();
                    string stock = I18N.FormatText("#SysComDesc72", chessRandomData.GetStockNum());
                    chessCell.chessStockHighlight.text = isHighlight ? stock : "";
                    chessCell.chessStock.text = isHighlight ? "" : stock;
                    chessCell.tipsBtn.gameObject.SetActive(true);
                    if (!chessRandomData.CheckCanBuy())
                    {
                        chessCell.buyBtn.interactable = false;
                        GameUIUtility.SetGrayShader(chessCell.buyBtn.image);
                        chessCell.normalGo.SetActive(false);
                        chessCell.soldOutGo.SetActive(true);
                    }
                    else
                    {
                        chessCell.buyBtn.interactable = true;
                        GameUIUtility.SetDefaultShader(chessCell.buyBtn.image);
                        chessCell.normalGo.SetActive(true);
                        chessCell.soldOutGo.SetActive(false);
                        var price = curSellConfig.Price.ConvertToRewardConfig();
                        chessCell.normalPrice.text = price != null ? price.Count.ToString() : "";
                    }
                }
                else
                {
                    chessCell.chessGo.SetActive(false);
                }
                index++;
            }
            //底部订单随机棋子
            index = 0;
            foreach (var orderItem in orderItemList)
            {
                //当格子商品数据存在 且 格子上目前卖的商品合法时 格子才会显示
                if (chessTabData.GetChessOrderData(index, out var chessOrderData) && chessOrderData.CurSellGoodsId > 0)
                {
                    orderItem.SetVisible(true);
                    orderItem.Refresh(chessOrderData);
                }
                else
                {
                    orderItem.SetVisible(false);
                }
                index++;
            }
        }

        private void _RefreshChessShopCd(ShopTabChessData tabChessData = null)
        {
            if (_curShowTabType != ShopTabType.Chess)
                return;
            if (tabChessData == null)
                tabChessData = (ShopTabChessData)Game.Manager.shopMan.GetShopTabData(_curShowTabType);
            var remainTime = tabChessData.GetRefreshRemainTime();
            string cdStr = UIUtility.CountDownFormat(remainTime > 0 ? remainTime : 0);
            cdText.text = I18N.Text("#SysComDesc70") + cdStr;
        }

        private void _OnEnergyBtnClick(int index)
        {
            var energyTabData = (ShopTabEnergyData)Game.Manager.shopMan.GetShopTabData(_curShowTabType);
            var energyData = energyTabData.EnergyDataList[index];
            if (energyData == null)
                return;
            var from = energyItemList[index].energyIcon.transform.position - new Vector3(0, (energyItemList[index].energyIcon.transform as RectTransform).sizeDelta.y / 2, 0);
            Game.Manager.shopMan.TryBuyShopEnergyGoods(energyData, from, size: 256f);
        }

        private void _OnRandomChessBtnClick(int index)
        {
            var chessTabData = (ShopTabChessData)Game.Manager.shopMan.GetShopTabData(_curShowTabType);
            if (!chessTabData.GetChessRandomData(index, out var chessRandomData))
                return;
            var from = chessTopItemList[index].chessIcon.transform.position - new Vector3(0, (chessTopItemList[index].chessIcon.transform as RectTransform).sizeDelta.y / 2, 0);
            Game.Manager.shopMan.TryBuyShopChessRandomGoods(chessRandomData, from, 196f);
        }

        private void _OnRandomChessTipsBtnClick(int index)
        {
            var chessTabData = (ShopTabChessData)Game.Manager.shopMan.GetShopTabData(_curShowTabType);
            if (!chessTabData.GetChessRandomData(index, out var chessRandomData))
                return;
            var reward = chessRandomData.CurSellGoodsConfig.Reward.ConvertToRewardConfig();
            if (reward != null)
            {
                UIItemUtility.ShowItemPanelInfo(reward.Id);
            }
        }

        private void _OnRefreshChessBtnClick()
        {
            var chessTabData = (ShopTabChessData)Game.Manager.shopMan.GetShopTabData(_curShowTabType);
            if (chessTabData == null)
                return;
            Game.Manager.shopMan.TryRefreshShopChessGoods(chessTabData);
        }

        private void _OnRefreshCommunityShopLinkItem()
        {
            var communityLinkMan = Game.Manager.communityLinkMan;
            if (!communityLinkMan.IsShowShopLink())
            {
                return;
            }
            var communityShopList = communityLinkMan.GetCommunityShopList();
            foreach (var communityShop in communityShopList)
            {
                var cellItem = GameObjectPoolManager.Instance.CreateObject(linkItemType, shopLinkItemRoot);
                cellItem.SetActive(true);
                cellItem.gameObject.SetActive(communityLinkMan.IsShowShopLinkItem(communityShop));
                var btnItem = cellItem.GetComponent<UIShopLinkItemCell>();
                btnItem.UpdateContent(communityShop);
                cellList.Add(cellItem);
            }
        }
        private void _DoMoveShopLinkList(float offsetX)
        {
            _linkdoMoveOriginPos.Set(offsetX, shopLinkRoot.transform.position.y, shopLinkRoot.transform.position.z);
            shopLinkRoot.transform.position = _linkdoMoveOriginPos;
            shopLinkRoot.transform.DOKill();
            shopLinkRoot.transform.DOLocalMoveX(0, moveDuration);
        }
        private void _ClearCommunityShopLinkItem()
        {
            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(linkItemType, item);
            }
            cellList.Clear();
        }
        private void _RefreshCommunityLinkReward()
        {
            var communityLinkMan = Game.Manager.communityLinkMan;
            if (!communityLinkMan.IsShowRewardUI())
            {
                return;
            }
            CommunityLinkRewardData data = new CommunityLinkRewardData()
            {
                CommunityPopupType = CommunityPopupType.CommunityLinkReward,
                LinkId = communityLinkMan.RecordClickLinkId
            };
            UIManager.Instance.OpenWindow(UIConfig.UICommunityPlanReward, data);
            communityLinkMan.RecordClickLinkId = -1;
            _ClearCommunityShopLinkItem();
            _OnRefreshCommunityShopLinkItem();
            shopLinkRoot.gameObject.SetActive(_curShowTabType == ShopTabType.Energy && communityLinkMan.IsShowShopLink());
        }
    }
}
