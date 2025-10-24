// ===================================================
// Author: mengqc
// Date: 2025/09/10
// ===================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using fat.conf;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIVineLeapStepItem : UIVineLeapStandPlace
    {
        public enum EVineLeapStepItemState
        {
            INACTIVE,
            ACTIVE,
            FINISHED,
        }

        public TextMeshProUGUI tfStep;
        public Transform gpRewards;
        public GameObject imgComplete;
        public Slider progressBar;
        public TextMeshProUGUI tfProgress;
        public Button btnClickArea;
        public Animator animator;
        public SkeletonGraphic leapSpine;

        public int Index { get; private set; }
        public EVineLeapStepItemState State { get; private set; } = EVineLeapStepItemState.INACTIVE;

        private ActivityVineLeap _activity;

        private void Awake()
        {
            ButtonExt.TryAddClickScale(btnClickArea);
            UIUtility.CommonItemSetup(gpRewards);
        }

        public void SetClickAction(Action<int, UIVineLeapStepItem> action)
        {
            btnClickArea.onClick.RemoveAllListeners();
            btnClickArea.onClick.AddListener(() => action(Index, this));
        }

        public void Refresh(int index, EVineLeapStepItemState state, ActivityVineLeap activity, bool isCurStepRunning)
        {
            _activity = activity;
            Index = index;
            State = state;
            tfStep.text = (Index + 1).ToString();
            gpRewards.gameObject.SetActive(false);
            btnClickArea.gameObject.SetActive(false);
            animator.SetTrigger("Idle");
            if (State == EVineLeapStepItemState.FINISHED)
            {
                imgComplete.SetActive(true);
                progressBar.gameObject.SetActive(false);
            }
            else if (State == EVineLeapStepItemState.ACTIVE)
            {
                if (isCurStepRunning)
                {
                    progressBar.gameObject.SetActive(true);
                }
                else
                {
                    progressBar.gameObject.SetActive(false);
                }

                var targetScore = _activity.GetCurLevelTargetScore();
                progressBar.value = (float)activity.GetTokenNum() / targetScore;
                tfProgress.text = $"{_activity.GetTokenNum()}/{targetScore}";
                imgComplete.SetActive(false);
                btnClickArea.gameObject.SetActive(true);
            }
            else
            {
                imgComplete.SetActive(false);
                progressBar.gameObject.SetActive(false);
                var levelCfg = _activity.GetLevelConf(Index);
                if (levelCfg.RewardId > 0)
                {
                    gpRewards.gameObject.SetActive(true);
                }
            }

            if (gpRewards.gameObject.activeSelf)
            {
                var levelCfg = _activity.GetLevelConf(Index);
                var rewards = _activity.GetRewardsById(levelCfg.RewardId).ToList();
                UIUtility.CommonItemRefresh(gpRewards, rewards);
            }
        }

        public IEnumerator PlayPunch(float delay)
        {
            yield return new WaitForSeconds(delay);
            animator.SetTrigger("Hide");
            leapSpine.AnimationState.SetAnimation(0, "punch", false);
            leapSpine.AnimationState.AddAnimation(0, "idle", true, 0);
        }

        public IEnumerator PlayComplete(float delay)
        {
            yield return new WaitForSeconds(delay);
            imgComplete.SetActive(true);
            animator.SetTrigger("Complete");
        }
    }
}