// ================================================
// File: UITrainOrderMain.cs
// Author: yueran.li
// Date: 2025/07/28 17:57:11 星期一
// Desc: 火车任务 棋子Tip
// ================================================

using System;
using System.Collections.Generic;
using EL;
using FAT.Merge;
using FAT.MSG;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UITrainMissionItemInfo : UIBase
    {
        public GameObject check;
        
        // 活动实例 
        private TrainMissionActivity _activity;

        private UICommonItem _targetItem;
        private UICommonItem _rewardItem;

        private TrainMissionItemInfo _itemInfo;
        private int _state;

        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("Content/Panel/Item", out _targetItem);
            transform.Access("Content/Panel/TotalReward/Item", out _rewardItem);
        }

        private void AddButton()
        {
            transform.AddButton("Content/Panel/BtnClose", Close).WithClickScale().FixPivot();
            transform.AddButton("Content/Panel/BtnCommit", Close).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as TrainMissionActivity;
            _itemInfo = (TrainMissionItemInfo)items[1];
            _state = (int)items[2];
        }

        protected override void OnPreOpen()
        {
            check.SetActive(_state == 2);
            
            _targetItem.Setup();
            _targetItem.Refresh(_itemInfo.itemID, 1);
            _targetItem.ExtendTipsForMergeItem(_itemInfo.itemID);

            // 显示奖励
            _rewardItem.Refresh(_itemInfo.rewardID, _itemInfo.rewardCount);
        }

        protected override void OnPostClose()
        {
        }
    }
}