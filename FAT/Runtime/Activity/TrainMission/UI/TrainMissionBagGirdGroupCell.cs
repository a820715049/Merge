// ==================================================
// // File: TrainMissionBagGirdGroupCell.cs
// // Author: liyueran
// // Date: 2025-08-01 17:08:54
// // Desc: $
// // ==================================================

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
    public class
        TrainMissionBagGirdGroupCell : FancyScrollRectCell<List<BagMan.BagGirdData>, UICommonScrollRectDefaultContext>
    {
        [Serializable]
        public class BagGirdCell
        {
            [SerializeField] public GameObject girdGo;
            [SerializeField] public GameObject normalGo;

            [SerializeField] public GameObject itemGo;
            [SerializeField] public Button itemBtn;
            [SerializeField] public UIImageRes itemIcon;
            [SerializeField] public TMP_Text itemNum;
            [SerializeField] public Button itemTipsBtn;
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
                bagGirdCell.itemBtn.onClick.AddListener(() => _OnClickItem(temp));
                bagGirdCell.itemTipsBtn.WithClickScale().FixPivot().onClick.AddListener(() => _OnClickBtnTips(temp));
                index++;
            }
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
            bagGirdCell.normalGo.SetActive(true);
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