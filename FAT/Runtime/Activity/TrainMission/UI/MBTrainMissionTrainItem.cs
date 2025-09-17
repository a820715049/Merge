// ==================================================
// // File: MBTrainMissionProgressItem.cs
// // Author: liyueran
// // Date: 2025-07-29 15:07:45
// // Desc: $火车任务
// // ==================================================

using System;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;

namespace FAT
{
    public abstract class MBTrainMissionTrainItem : MonoBehaviour
    {
        protected TrainMissionActivity Activity;
        protected UITrainMissionTrainModule Module;
        protected MBTrainMissionTrain Train;

        protected int Index = -1;

        public virtual void Init(TrainMissionActivity act, UITrainMissionTrainModule module, MBTrainMissionTrain train,
            int index)
        {
            this.Module = module;
            this.Activity = act;
            this.Train = train;
            this.Index = index;
        }

        protected virtual void OnEnable()
        {
            MessageCenter.Get<UI_TRAIN_MISSION_SCROLLIN>().AddListener(MoveIn);
            MessageCenter.Get<UI_TRAIN_MISSION_SCROLLSTOP>().AddListener(StopMove);
            MessageCenter.Get<UI_TRAIN_MISSION_SCROLLOUT>().AddListener(MoveOut);
        }

        protected virtual void OnDisable()
        {
            MessageCenter.Get<UI_TRAIN_MISSION_SCROLLIN>().RemoveListener(MoveIn);
            MessageCenter.Get<UI_TRAIN_MISSION_SCROLLSTOP>().RemoveListener(StopMove);
            MessageCenter.Get<UI_TRAIN_MISSION_SCROLLOUT>().RemoveListener(MoveOut);
        }


        public int GetIndex()
        {
            return Index;
        }


        public virtual void Release()
        {
        }

        protected bool CheckBelongTrain(MBTrainMissionTrain.TrainType type)
        {
            return type == Train.trainType;
        }

        protected abstract void MoveIn(MBTrainMissionTrain.TrainType type);
        protected abstract void StopMove(MBTrainMissionTrain.TrainType type);
        protected abstract void MoveOut(MBTrainMissionTrain.TrainType type);
    }
}