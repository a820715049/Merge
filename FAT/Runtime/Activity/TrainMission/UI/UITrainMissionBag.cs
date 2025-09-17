// ==================================================
// // File: UITrainMissionBag.cs
// // Author: liyueran
// // Date: 2025-08-01 17:08:09
// // Desc: 火车棋盘背包
// // ==================================================

using System.Collections.Generic;
using DG.Tweening;
using EL;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UITrainMissionBag : UIBase
    {
        [SerializeField] private UIBagGirdGroupScrollRect girdGroupRect;


        private TrainMissionActivity _activity;
        private List<List<BagMan.BagGirdData>> _gridGroupList = new(); // 用于记录每个格子的数据

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/BtnClose/Btn", base.Close);

            girdGroupRect.InitLayout();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (TrainMissionActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            _RefreshTabToggle();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_BAG_ITEM_INFO_CHANGE>().AddListener(_RefreshUI);
        }

        protected override void OnRefresh()
        {
            _RefreshTabToggle();
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_BAG_ITEM_INFO_CHANGE>().RemoveListener(_RefreshUI);
        }

        protected override void OnPostClose()
        {
            BoardViewManager.Instance.OnInventoryClose();
        }


        private void _RefreshTabToggle()
        {
            _RefreshUI();
        }

        // 初始化棋子背包
        private List<BagMan.BagGirdData> _InitItemGirdData()
        {
            var itemBag = _activity.World.inventory.GetBagByType(BagMan.BagType.Item);
            var itemBagGirdDataList = new List<BagMan.BagGirdData>();

            for (int i = 0; i < _activity.mission.Storage; i++)
            {
                var item = itemBag.PeekItem(i);
                var config = Game.Manager.configMan.GetInventoryItemConfigById(i + 1);
                var girdData = new BagMan.BagGirdData
                {
                    BelongBagType = BagMan.BagType.Item,
                    BelongBagId = 1,
                    //  Item = 1,       //棋子背包
                    GirdIndex = i,
                    IsUnlock = true,
                    ItemTId = item?.tid ?? 0
                };

                itemBagGirdDataList.Add(girdData);
            }

            return itemBagGirdDataList;
        }

        private void _RefreshUI()
        {
            _gridGroupList.Clear();
            var gidDataList = _InitItemGirdData();
            var tempList = new List<BagMan.BagGirdData>();
            if (gidDataList.Count < 4)
            {
                _gridGroupList.Add(new List<BagMan.BagGirdData>(gidDataList));
            }
            else
            {
                foreach (var girdData in gidDataList)
                {
                    int girdIndex = girdData.GirdIndex;
                    //将数据四个为一组划分
                    if (girdIndex % 4 < 3)
                    {
                        tempList.Add(girdData);
                    }
                    else
                    {
                        tempList.Add(girdData);
                        _gridGroupList.Add(new List<BagMan.BagGirdData>(tempList));
                        tempList.Clear();
                    }
                }

                //添加最后的末尾部分
                if (tempList.Count > 0)
                {
                    _gridGroupList.Add(new List<BagMan.BagGirdData>(tempList));
                    tempList.Clear();
                }
            }

            girdGroupRect.UpdateData(_gridGroupList);
        }
    }
}