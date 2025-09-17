// ==================================================
// // File: MBTrainMissionProgressItem.cs
// // Author: liyueran
// // Date: 2025-07-29 15:07:45
// // Desc: $火车任务 回收火车
// // ==================================================

using System;
using System.Collections.Generic;
using EL;
using FAT.MSG;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBTrainMissionTrainItemRecycle : MBTrainMissionTrainItem
    {
        public SkeletonGraphic spine;
        public Transform boxPos;
        public ParticleSystem smokeEff;
        public Animator animator;
        public GameObject sweep;
        public GameObject glowFx;

        #region Mono
        private void Start()
        {
        }
        #endregion

        public override void Release()
        {
            base.Release();
            spine.AnimationState.SetAnimation(0, "idle", false);
            
            animator.ResetTrigger("BlueComplete");
            sweep.SetActive(false);
            glowFx.SetActive(false);
        }

        private void ShowSmoke()
        {
            var emission = smokeEff.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(14f);
        }

        private void HideSmoke()
        {
            var emission = smokeEff.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
        }
        
        protected override void MoveIn(MBTrainMissionTrain.TrainType type)
        {
            if (!CheckBelongTrain(type))
            {
                return;
            }
            ShowSmoke();
            // 火车驶入 移动动画
            spine.AnimationState.SetAnimation(0, "move", true);
        }

        protected override void StopMove(MBTrainMissionTrain.TrainType type)
        {
            if (!CheckBelongTrain(type))
            {
                return;
            }

            HideSmoke();
            // 火车停止移动
            spine.AnimationState.SetAnimation(0, "stop", false);
            spine.AnimationState.AddAnimation(0, "idle", false, 0);
        }

        protected override void MoveOut(MBTrainMissionTrain.TrainType type)
        {
            if (!CheckBelongTrain(type))
            {
                return;
            }
            ShowSmoke();
            // 火车驶出 移动动画
            spine.AnimationState.SetAnimation(0, "openmove", true);
        }
        
        public void BlueComplete()
        {
            animator.SetTrigger("BlueComplete");
        }

        public void OpenCover(float time, Action onComplete)
        {
            Game.Manager.audioMan.TriggerSound("TrainCarriageOpen"); // 火车-棋子回收-车厢开启
            
            // 宝箱动画
            spine.AnimationState.SetAnimation(0, "open", false).Complete += delegate(TrackEntry entry)
            {
                // 播放开箱奖励动画
                var trackEntry = spine.AnimationState.SetAnimation(0, "openreward", false);

                // 设置播放速度来控制时长
                trackEntry.TimeScale = 1.667f / time;
                
                onComplete.Invoke();
                
                trackEntry.Complete += delegate(TrackEntry entry)
                {
                    Game.Manager.audioMan.TriggerSound("TrainChestLight"); // 火车-棋子回收-宝箱填满
                };
            };
        }
    }
}