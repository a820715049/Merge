// ==================================================
// // File: MBTrainMissionProgressItem.cs
// // Author: liyueran
// // Date: 2025-07-29 15:07:45
// // Desc: $火车任务 火车头
// // ==================================================

using System;
using DG.Tweening;
using EL;
using FAT.MSG;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBTrainMissionTrainItemHead : MBTrainMissionTrainItem
    {
        public RectTransform headIcon;
        public bool bubble = true;
        public Animator bubbleAnim;
        public UIImageRes rewardIcon;
        public TextMeshProUGUI rewardCount;
        public SkeletonGraphic spine;
        public ParticleSystem smokeEff;


        #region Mono
        protected override void OnEnable()
        {
            base.OnEnable();
            MessageCenter.Get<UI_TRAIN_MISSION_COMPLETE_TRAIN_MISSION>().AddListener(OnCompleteTrainMission);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            flySeq?.Kill();
            MessageCenter.Get<UI_TRAIN_MISSION_COMPLETE_TRAIN_MISSION>().RemoveListener(OnCompleteTrainMission);
        }
        #endregion

        public override void Init(TrainMissionActivity act, UITrainMissionTrainModule module, MBTrainMissionTrain train,
            int index)
        {
            base.Init(act, module, train, index);
            var reward = Train.TrainOrder.rewardConfigs[0];

            var conf = Game.Manager.objectMan.GetBasicConfig(reward.Id);
            if (conf == null) return;

            bubbleAnim.gameObject.SetActive(false);
            rewardIcon.SetImage(conf.Icon);
            rewardCount.SetText($"{reward.Count}");
            headIcon.gameObject.SetActive(false);
        }


        public override void Release()
        {
            base.Release();
            HideSmoke();
            spine.AnimationState.SetAnimation(0, "idle", false);
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
            // 火车移动动画
            spine.AnimationState.SetAnimation(0, "move", true);
        }

        protected override void StopMove(MBTrainMissionTrain.TrainType type)
        {
            if (!CheckBelongTrain(type))
            {
                return;
            }


            if (bubble)
            {
                DOTween.Sequence().AppendInterval(0.4f).AppendCallback(() =>
                {
                    bubbleAnim.gameObject.SetActive(true);
                    bubbleAnim.SetTrigger("Show");
                });
                bubble = false;
            }

            // 火车停止移动
            spine.AnimationState.SetAnimation(0, "stop", false).Complete += delegate(TrackEntry entry) { HideSmoke(); };
            spine.AnimationState.AddAnimation(0, "idle", false, 0);
        }

        protected override void MoveOut(MBTrainMissionTrain.TrainType type)
        {
            if (!CheckBelongTrain(type))
            {
                return;
            }

            ShowSmoke();
            // 火车移动动画
            spine.AnimationState.SetAnimation(0, "move", true);
        }

        private Sequence flySeq;

        // 完成火车所有任务后的表现
        private void OnCompleteTrainMission(MBTrainMissionTrain.TrainType type, int index)
        {
            if (!CheckBelongTrain(type))
            {
                return;
            }

            Game.Manager.audioMan.TriggerSound("TrainComplete"); // 火车-完成火车

            // 发放火车奖励
            var rewards = Activity.GetOrderWaitCommitReward();
            foreach (var reward in rewards)
            {
                UIFlyUtility.FlyReward(reward, bubbleAnim.transform.position);
            }

            bubbleAnim.SetTrigger("Hide");
            bubble = true;

            var fromPos = headIcon.transform.position;
            var toPos = Train.Main.ProgressModule.headIcon.transform.position;
            headIcon.gameObject.SetActive(true);

            flySeq?.Kill();
            flySeq = DOTween.Sequence();

            // 火车车头飞到进度条的火车位置
            flySeq.Append(headIcon.DOMove(toPos, 1f));
            flySeq.Join(headIcon.DOSizeDelta(new Vector2(92, 92), 1f));

            flySeq.OnComplete(() =>
            {
                headIcon.gameObject.SetActive(false);
                // 进度条增长 发放里程碑奖励
                var progress = Train.Main.ProgressModule;
                progress.TrainPunch();

                var to = progress.CalProgressValue();

                // 火车全部完成 开走
                if (Train.TrainOrder.CheckAllFinish())
                {
                    Train.Main.TrainModule.ScrollOut(type);
                }

                progress.ProgressAnim(to);
            });

            flySeq.OnKill(() =>
            {
                headIcon.gameObject.SetActive(false);
                headIcon.transform.position = fromPos;
            });
        }
    }
}