// ==================================================
// // File: UITrainMissionProgress.cs
// // Author: liyueran
// // Date: 2025-07-29 15:07:28
// // Desc: $火车棋盘火车轨道子模块
// // ==================================================

using System;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UITrainMissionTrainModule : UIModuleBase
    {
        public MBTrainMissionTrain TopTrain;
        public MBTrainMissionTrain BottomTrain;

        private TrainMissionActivity _activity;
        private UITrainMissionMain _main;

        public string PoolKeyCarriageItem => $"train_mission_train_carriage";
        public string PoolKeyHeadItem => $"train_mission_train_head";
        public string PoolKeyRecycleItem => $"train_mission_train_recycle";


        public UITrainMissionTrainModule(Transform root) : base(root)
        {
        }

        #region module
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            ModuleRoot.Access("TrainTop", out TopTrain);
            ModuleRoot.Access("TrainBottom", out BottomTrain);
        }

        private void AddButton()
        {
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (TrainMissionActivity)items[0];
            _main = (UITrainMissionMain)items[1];

            TopTrain.OnParse(_activity, _main, this);
            BottomTrain.OnParse(_activity, _main, this);
        }

        protected override void OnShow()
        {
            TopTrain.OnShow();
            BottomTrain.OnShow();
        }


        protected override void OnHide()
        {
            TopTrain.OnHide();
            BottomTrain.OnHide();
        }

        protected override void OnAddListener()
        {
        }

        protected override void OnRemoveListener()
        {
        }

        protected override void OnAddDynamicListener()
        {
        }

        protected override void OnRemoveDynamicListener()
        {
        }

        protected override void OnClose()
        {
        }
        #endregion


        public void ScrollOut(MBTrainMissionTrain.TrainType type, Action callback = null)
        {
            if (type == MBTrainMissionTrain.TrainType.Bottom)
            {
                BottomTrain.ScrollOut(callback);
            }
            else
            {
                TopTrain.ScrollOut(callback);
            }
        }
    }
}