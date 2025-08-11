/*
 * @Author: tang.yan
 * @Description: 物品信息cell
 * @Date: 2023-11-24 14:11:48
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
    public class UIItemInfoGroupCell : FancyScrollRectCell<List<int>, UICommonScrollRectDefaultContext>
    {
        private class UIItemInfoItemCell
        {
             public GameObject ItemGo;
             public GameObject NormalGo;
             public GameObject NormalNextGo;
             public GameObject LockGo;
             public GameObject LockNextGo;
             public GameObject SelectGo;
             public GameObject SelectNextGo;
             public GameObject NextLevelGo;
             public UIImageRes ItemIcon;
             public GameObject BuyBtnGo;
             public TMP_Text BuyBtnPrice;
        }
        
        [SerializeField]private GameObject girdGroupGo; 
        [SerializeField]private GameObject produceTitleGo; 
        [SerializeField]private TMP_Text produceTitle;
        [SerializeField]private GameObject goStoreGo;
        [SerializeField]private Button goStoreBtn;
        [SerializeField]private HorizontalLayoutGroup layoutGroup;
        [SerializeField]private Image leftLineImage;
        [SerializeField]private Image rightLineImage;

        private List<UIItemInfoItemCell> _girdCellList;
        private List<int> _curItemIdList;

        public override void Initialize()
        {
            _girdCellList = new List<UIItemInfoItemCell>();
            for (int i = 1; i <= 4; i++)
            {
                string path = "Gird/UIItemInfoItemCell" + i;
                int index = i - 1;
                var cell = new UIItemInfoItemCell();
                transform.FindEx(path, out cell.ItemGo);
                path += "/Content/";
                transform.FindEx(path + "NormalGo", out cell.NormalGo);
                transform.FindEx(path + "NormalGo/NextArrow", out cell.NormalNextGo);
                transform.FindEx(path + "LockGo", out cell.LockGo);
                transform.FindEx(path + "LockGo/NextArrow", out cell.LockNextGo);
                transform.FindEx(path + "SelectGo", out cell.SelectGo);
                transform.FindEx(path + "SelectGo/NextArrow", out cell.SelectNextGo);
                transform.FindEx(path + "NextLevelGo", out cell.NextLevelGo);
                transform.FindEx(path + "BuyBtn", out cell.BuyBtnGo);
                transform.AddButton(path + "NextLevelGo", () => OnItemNextLevelTipsBtnClick(index));
                transform.AddButton(path + "BuyBtn", () => OnItemBuyBtnClick(index));
                cell.ItemIcon = transform.FindEx<UIImageRes>(path + "Icon");
                cell.BuyBtnPrice = transform.FindEx<TMP_Text>(path + "BuyBtn/Normal/Num");
                _girdCellList.Add(cell);
            }
            goStoreBtn.WithClickScale().FixPivot().onClick.AddListener(OnGoStoreBtnClick);
        }

        public void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().AddListener(_RefreshItemBuyState);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_SHOP_ITEM_INFO_CHANGE>().RemoveListener(_RefreshItemBuyState);
        }

        public override void UpdateContent(List<int> itemIdList)
        {
            _curItemIdList = itemIdList;
            var itemInfoMan = Game.Manager.itemInfoMan;
            var curShowItemData = itemInfoMan.CurShowItemData;
            var curChainProgress = itemInfoMan.GetCurItemChainProgress();
            var handbookMan = Game.Manager.handbookMan;
            var objectMan = Game.Manager.objectMan;
            int length = itemIdList.Count;
            //判断使用什么布局
            if (Index == 0 && _curItemIdList.Count < 4)    //如果为第一行且cell数量不足4个 则居中显示
            {
                layoutGroup.childAlignment = TextAnchor.UpperCenter;
            }
            else if (Index == curShowItemData.EmptyIndex + 1 && _curItemIdList.Count < 4)    //如果为空行之后的第一行且cell数量不足4个 则居中显示
            {
                layoutGroup.childAlignment = TextAnchor.UpperCenter;
            }
            else
            {
                layoutGroup.childAlignment = TextAnchor.UpperLeft;
            }
            if (length <= 0)
            {
                var shopMan = Game.Manager.shopMan;
                girdGroupGo.SetActive(false);
                //链条上任意棋子在商店出售时 就显示跳转按钮
                bool isShowGoStore = false;
                foreach (var itemId in curChainProgress)
                {
                    if (shopMan.TryGetChessOrderDataById(itemId) != null)
                    {
                        isShowGoStore = true;
                        break;
                    }
                }
                goStoreGo.SetActive(isShowGoStore);
                var (canShow, title) = itemInfoMan.CheckCanShowProduceTitle();
                produceTitleGo.SetActive(canShow);
                if (canShow)
                {
                    produceTitle.text = title;
                    var config = FontMaterialRes.Instance.GetFontMatResConf(curShowItemData.CanShowBoost ? 6 : 5);
                    if (config != null)
                    {
                        //刷新文本颜色
                        produceTitle.color = config.color;
                        //刷新左右横线颜色
                        leftLineImage.color = config.color;
                        rightLineImage.color = config.color;
                    }
                }
            }
            else
            {
                girdGroupGo.SetActive(true);
                produceTitleGo.SetActive(false);
                goStoreGo.SetActive(false);
                
                int index = 0; 
                foreach (var cell in _girdCellList)
                {
                    if (index < length)
                    {
                        int itemId = itemIdList[index];
                        //是否是当前显示的棋子
                        bool isCur = itemId == curShowItemData.ItemId;
                        bool isOutputItem = itemInfoMan.CheckIsCurOutputItem(itemId);
                        bool isChainItem = curChainProgress?.Contains(itemId) ?? false;
                        //指向下一个的箭头是否显示 (不是产出棋子+是合成链上的棋子+不是最后一级)
                        bool isShowArrow = !isOutputItem && isChainItem && (curChainProgress.IndexOf(itemId) < curChainProgress.Count - 1);
                        //棋子在图鉴中的状态
                        bool isLock = handbookMan.IsItemLock(itemId);
                        bool isPreview = handbookMan.IsItemPreview(itemId);
                        //棋子是否在商店出售
                        var (isSell, price) = itemInfoMan.CheckIsSellInShop(itemId);
                        //棋子是否被订单需要
                        bool isNeedInorder = itemInfoMan.CheckItemIsNeedInOrder(itemId);
                        
                        //刷新
                        cell.ItemGo.SetActive(true);
                        cell.NextLevelGo.SetActive(false);
                        if (isCur)
                        {
                            cell.SelectGo.SetActive(true);
                            cell.SelectNextGo.SetActive(isShowArrow);
                            cell.NormalGo.SetActive(false);
                            cell.LockGo.SetActive(false);
                        }
                        else
                        {
                            cell.SelectGo.SetActive(false);
                            if (!isOutputItem)
                            {
                                if (isLock && !isSell && !isNeedInorder)
                                {
                                    cell.LockGo.SetActive(true);
                                    cell.LockNextGo.SetActive(isShowArrow);
                                    cell.NormalGo.SetActive(false);
                                }
                                else
                                {
                                    //棋子在图鉴中的状态
                                    bool isUnlocked = handbookMan.IsItemUnlocked(itemId);
                                    cell.LockGo.SetActive(false);
                                    cell.NormalGo.SetActive(isUnlocked || isNeedInorder || isPreview || isSell);
                                    cell.NormalNextGo.SetActive((isUnlocked || isNeedInorder || isPreview || isSell) && isShowArrow);
                                    //如果不是产出链且也不是合成链的物品 则显示tips按钮
                                    cell.NextLevelGo.SetActive(!isChainItem);
                                }
                            }
                            else
                            {
                                cell.LockGo.SetActive(false);
                                cell.NormalGo.SetActive(true);
                                cell.NormalNextGo.SetActive(false);
                                cell.NextLevelGo.SetActive(false);
                            }
                        }
                        if (!isLock || isCur || isSell || isOutputItem || isNeedInorder)
                        {
                            var cfg = objectMan.GetBasicConfig(itemId);
                            if (cfg != null)
                            {
                                cell.ItemIcon.gameObject.SetActive(true);
                                cell.ItemIcon.SetImage(cfg.Icon.ConvertToAssetConfig());
                                Color color = Color.white;
                                color.a = !isPreview ? (!isCur && isNeedInorder && isLock ? 0.5f : 1f) : 0.5f;
                                cell.ItemIcon.color = color;
                            }
                        }
                        else
                        {
                            cell.ItemIcon.gameObject.SetActive(false);
                        }
                        cell.BuyBtnGo.SetActive(isSell);
                        cell.BuyBtnPrice.text = price.ToString();
                    }
                    else
                    {
                        cell.ItemGo.SetActive(false);
                    }
                    index++;
                }
            }
        }

        //刷新物品的可购买状态
        private void _RefreshItemBuyState()
        {
            int length = _curItemIdList.Count;
            if (length > 0)
            {
                int index = 0;
                foreach (var cell in _girdCellList)
                {
                    if (index < length)
                    {
                        int itemId = _curItemIdList[index];
                        //棋子是否在商店出售
                        var (isSell, price) = Game.Manager.itemInfoMan.CheckIsSellInShop(itemId);
                        cell.BuyBtnGo.SetActive(isSell);
                        cell.BuyBtnPrice.text = price.ToString();
                    }
                    index++;
                }
            }
        }
        
        private void OnItemNextLevelTipsBtnClick(int index)
        {
            if (_curItemIdList == null || !_curItemIdList.TryGetByIndex(index, out var itemId))
                return;
            //传入图标位置和偏移值 偏移值=cell宽度的一半 152/2 + 23
            UIManager.Instance.OpenWindow(UIConfig.UIItemInfoTips, _girdCellList[index].ItemIcon.transform.position, 99f);
        }

        private void OnItemBuyBtnClick(int index)
        {
            if (_curItemIdList == null || !_curItemIdList.TryGetByIndex(index, out var itemId))
                return;
            Game.Manager.itemInfoMan.TryBuyShopChessGoods(itemId, _girdCellList[index].ItemIcon.transform.position);
        }

        private void OnGoStoreBtnClick()
        {
            UIManager.Instance.CloseWindow(UIConfig.UIItemInfo);
            Game.Manager.shopMan.TryOpenUIShop(ShopTabType.Chess);
        }
    }
}