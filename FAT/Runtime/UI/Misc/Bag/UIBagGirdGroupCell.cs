/*
 * @Author: tang.yan
 * @Description: 背包格子组cell
 * @Date: 2023-11-01 10:11:51
 */

using System;
using EL;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using System.Collections.Generic;
using System.Linq;
using fat.rawdata;
using TMPro;

namespace FAT
{
    public class UIBagGirdGroupCell : FancyScrollRectCell<List<BagMan.BagGirdData>, UICommonScrollRectDefaultContext>
    {
        [Serializable]
        public class BagGirdCell
        {
            [SerializeField] public GameObject girdGo;
            [SerializeField] public GameObject normalGo;
            [SerializeField] public GameObject lockGo;
            [SerializeField] public GameObject buyBtnGo;
            [SerializeField] public GameObject buyTipsGo;
            [SerializeField] public Button buyBtn;
            [SerializeField] public TMP_Text buyBtnPrice;
            [SerializeField] public GameObject itemGo;
            [SerializeField] public Button itemBtn;
            [SerializeField] public UIImageRes itemIcon;
            [SerializeField] public TMP_Text itemNum;
            [SerializeField] public Button itemTipsBtn;
            [SerializeField] public TextMeshProUGUI unlockLevel;
            [SerializeField] public UIImageRes producerIcon;
            [SerializeField] public GameObject mask;
        }

        [SerializeField] private GameObject girdGroupGo;
        [SerializeField] private TMP_Text tipsText;
        [SerializeField] private List<BagGirdCell> girdCellList;

        private List<BagMan.BagGirdData> _curGirdDataList;
        private int _curItemBagUnlockId = 0;

        public override void Initialize()
        {
            var index = 0;
            foreach (var bagGirdCell in girdCellList)
            {
                var temp = index;
                bagGirdCell.buyBtn.WithClickScale().FixPivot().onClick.AddListener(() => _OnClickBtnBuy(temp));
                bagGirdCell.itemBtn.onClick.AddListener(() => _OnClickItem(temp));
                bagGirdCell.itemTipsBtn.WithClickScale().FixPivot().onClick.AddListener(() => _OnClickBtnTips(temp));
                index++;
            }
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        public override void UpdateContent(List<BagMan.BagGirdData> girdDataList)
        {
            if (girdDataList == null)
                return;
            _curItemBagUnlockId = Game.Manager.bagMan.CurItemBagUnlockId;
            _curGirdDataList = girdDataList;
            var length = _curGirdDataList.Count;
            if (length <= 0)
            {
                girdGroupGo.SetActive(false);
                tipsText.gameObject.SetActive(true);
            }
            else
            {
                girdGroupGo.SetActive(true);
                tipsText.gameObject.SetActive(false);
            }

            var index = 0;
            foreach (var bagGirdCell in girdCellList)
            {
                if (index < length)
                {
                    var girdData = girdDataList[index];
                    bagGirdCell.girdGo.SetActive(true);
                    _RefreshGirdCell(bagGirdCell, girdData);
                }
                else
                {
                    bagGirdCell.girdGo.SetActive(false);
                }

                index++;
            }
        }

        private void _RefreshGirdCell(BagGirdCell bagGirdCell, BagMan.BagGirdData girdData)
        {
            var type = girdData.BelongBagType;
            bagGirdCell.itemIcon.color = Color.white;
            if (type == BagMan.BagType.Item)
            {
                bagGirdCell.lockGo.SetActive(false);
                bagGirdCell.unlockLevel.gameObject.SetActive(false);
                bagGirdCell.producerIcon.gameObject.SetActive(false);
                if (girdData.IsUnlock)
                {
                    bagGirdCell.normalGo.SetActive(true);
                    bagGirdCell.buyBtnGo.SetActive(false);
                    if (girdData.ItemTId > 0)
                    {
                        bagGirdCell.itemNum.text = "";
                        var cfg = Game.Manager.objectMan.GetBasicConfig(girdData.ItemTId);
                        if (cfg != null)
                        {
                            bagGirdCell.itemGo.SetActive(true);
                            var res = cfg.Icon.ConvertToAssetConfig();
                            bagGirdCell.itemIcon.SetImage(res.Group, res.Asset);
                            bagGirdCell.itemTipsBtn.gameObject.SetActive(true);
                        }
                        else
                        {
                            bagGirdCell.itemGo.SetActive(false);
                        }
                    }
                    else
                    {
                        bagGirdCell.itemGo.SetActive(false);
                    }
                }
                else
                {
                    bagGirdCell.normalGo.SetActive(false);
                    bagGirdCell.buyBtnGo.SetActive(true);
                    bagGirdCell.itemGo.SetActive(false);
                    if (girdData.GirdIndex + 1 == _curItemBagUnlockId + 1)
                    {
                        bagGirdCell.buyTipsGo.SetActive(true);
                        bagGirdCell.buyBtn.gameObject.SetActive(true);
                        bagGirdCell.buyBtnPrice.text = girdData.BuyCostNum.ToString();
                    }
                    else
                    {
                        bagGirdCell.buyTipsGo.SetActive(false);
                        bagGirdCell.buyBtn.gameObject.SetActive(false);
                    }
                }
            }
            else if (type == BagMan.BagType.Producer)
            {
                bagGirdCell.buyBtnGo.SetActive(false);
                var cfg = Game.Manager.objectMan.GetBasicConfig(girdData.BelongItemId);
                if (girdData.IsUnlock)
                {
                    bagGirdCell.lockGo.SetActive(false);
                    bagGirdCell.normalGo.SetActive(true);
                    bagGirdCell.itemNum.text = "";
                    if (cfg != null)
                    {
                        bagGirdCell.itemGo.SetActive(true);
                        var res = cfg.Icon.ConvertToAssetConfig();
                        bagGirdCell.itemIcon.SetImage(res.Group, res.Asset);
                        var color = bagGirdCell.itemIcon.color;
                        color.a = girdData.ItemTId > 0 ? 1f : 0.5f;
                        bagGirdCell.itemIcon.color = color;
                        bagGirdCell.itemTipsBtn.gameObject.SetActive(girdData.ItemTId > 0);
                        bagGirdCell.unlockLevel.gameObject.SetActive(false);
                        bagGirdCell.producerIcon.gameObject.SetActive(false);
                    }
                    else
                    {
                        bagGirdCell.itemGo.SetActive(false);
                    }
                }
                else
                {
                    var unlockConf = Game.Manager.configMan.GetInventoryProducerConfig();
                    var curConf = unlockConf.FirstOrDefault(x => x.Value.ObjBasicId == cfg.Id);
                    bagGirdCell.lockGo.SetActive(true);
                    bagGirdCell.mask.SetActive(false);
                    bagGirdCell.unlockLevel.gameObject.SetActive(true);
                    bagGirdCell.producerIcon.gameObject.SetActive(true);
                    bagGirdCell.producerIcon.SetImage(cfg.Icon.ConvertToAssetConfig());
                    bagGirdCell.unlockLevel.text = I18N.FormatText("#SysComDesc616",
                        curConf.Value?.UnlockLevel);
                    bagGirdCell.normalGo.SetActive(false);
                    bagGirdCell.itemGo.SetActive(false);
                }
            }
            else if (type == BagMan.BagType.Tool)
            {
                bagGirdCell.buyBtnGo.SetActive(false);
                bagGirdCell.unlockLevel.gameObject.SetActive(false);
                bagGirdCell.producerIcon.gameObject.SetActive(false);
                if (girdData.IsUnlock)
                {
                    bagGirdCell.lockGo.SetActive(false);
                    bagGirdCell.normalGo.SetActive(true);
                    bagGirdCell.itemNum.text = $"{Game.Manager.coinMan.GetCoin(girdData.ItemTId)}";
                    var cfg = Game.Manager.objectMan.GetBasicConfig(girdData.ItemTId);
                    if (cfg != null)
                    {
                        bagGirdCell.itemGo.SetActive(true);
                        var res = cfg.Icon.ConvertToAssetConfig();
                        bagGirdCell.itemIcon.SetImage(res.Group, res.Asset);
                        bagGirdCell.itemTipsBtn.gameObject.SetActive(true);
                    }
                    else
                    {
                        bagGirdCell.itemGo.SetActive(false);
                    }
                }
                else
                {
                    bagGirdCell.lockGo.SetActive(true);
                    bagGirdCell.mask.SetActive(true);
                    bagGirdCell.normalGo.SetActive(false);
                    bagGirdCell.itemGo.SetActive(false);
                }
            }
        }

        private void _OnClickBtnBuy(int index)
        {
            var girdData = _curGirdDataList?[index];
            if (girdData == null)
                return;
            if (girdData.GirdIndex + 1 == _curItemBagUnlockId + 1)
                Game.Manager.bagMan.PurchaseItemBagGird(girdData.BuyCostNum);
        }

        private void _OnClickBtnTips(int index)
        {
            var girdData = _curGirdDataList?[index];
            if (girdData == null)
                return;
            var type = girdData.BelongBagType;
            var itemId = type == BagMan.BagType.Tool ? girdData.RelateItemId : girdData.ItemTId;
            UIItemUtility.ShowItemPanelInfo(itemId);
        }

        private void _OnClickItem(int index)
        {
            var girdData = _curGirdDataList?[index];
            if (girdData == null)
                return;
            var type = girdData.BelongBagType;
            //工具类背包点击无反应  格子上没有物品时点击无反应
            if (type == BagMan.BagType.Tool || girdData.ItemTId <= 0)
                return;
            // 没有空位 不用取出了
            if (BoardViewManager.Instance.board.emptyGridCount < 1)
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.BoardFullUi);
                Game.Manager.audioMan.TriggerSound("BoardFull");
                return;
            }

            UIFlyFactory.GetFlyTarget(FlyType.Inventory, out var worldPos);
            BoardUtility.RegisterSpawnRequest(girdData.ItemTId, worldPos);

            // 取出
            if (BoardViewManager.Instance.board.GetItemFromInventory(girdData.GirdIndex, girdData.BelongBagId))
            {
                Game.Manager.audioMan.TriggerSound("UIClick");
            }
            else
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.BoardFullUi);
                Game.Manager.audioMan.TriggerSound("BoardFull");
            }
        }
    }
}