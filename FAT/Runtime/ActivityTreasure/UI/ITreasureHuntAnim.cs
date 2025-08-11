/**
 * @Author: zhangpengjian
 * @Date: 2024/12/20 15:03:43
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/12/20 15:03:43
 * Description: 寻宝开箱表现抽象
 */

using Spine.Unity;
using UnityEngine;
using Spine;
using DG.Tweening;
using System;
using static FAT.UITreasureHuntUtility;

namespace FAT
{
    interface ITreasureHuntAnim
    {
        void PlayAnimation(string animationName, System.Action value);
        void StopAnimation();
    }

    public class SpineController : ITreasureHuntAnim
    {
        private SkeletonGraphic skeletonAnimation;

        public SpineController(SkeletonGraphic skeletonAnimation)
        {
            this.skeletonAnimation = skeletonAnimation;
        }

        public void PlayAnimation(string animationName, Action value)
        {
            if (animationName == State.Born.ToString())
            {
                skeletonAnimation.AnimationState.SetAnimation(0, "show", false);
            }
            else if (animationName == State.Open.ToString())
            {
                skeletonAnimation.AnimationState.SetAnimation(0, "open_a", false).Complete += delegate (TrackEntry entry)
                {
                    skeletonAnimation.AnimationState.SetAnimation(0, "open_b", false).Complete += delegate (TrackEntry entry)
                    {
                        skeletonAnimation.AnimationState.SetAnimation(0, "open_c", false);
                        value?.Invoke();
                    };
                };
            }
            else if (animationName == State.Idle.ToString())
            {
                skeletonAnimation.AnimationState.SetAnimation(0, "idle", true);
            }
            else if (animationName == State.Dead.ToString())
            {
                skeletonAnimation.AnimationState.SetAnimation(0, "hide", true);
            }
        }

        public void StopAnimation()
        {
        }
    }

    public class AnimatorController : ITreasureHuntAnim
    {
        private Animator animator;

        public AnimatorController(Animator animator)
        {
            this.animator = animator;
        }

        public void PlayAnimation(string animationName, Action value)
        {
            animator?.SetTrigger(animationName);
        }

        public void StopAnimation()
        {
        }
    }

}