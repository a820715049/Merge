// ===================================================
// Author: mengqc
// Date: 2025/09/02
// ===================================================

using System;
using System.Collections.Generic;
using Config;
using EL;
using TMPro;
using UnityEngine;
using DG.Tweening;

namespace FAT
{
    public class UIVineLeapLevelReward : UIBase
    {
        public Transform rewardRoot;
        public MapButton btnClaim;
        public float durationIn = 0.5f;

        private List<RewardCommitData> rewards;
        private bool isEntering;
        private List<RewardConfig> showRewards = new();
        private Action _onFlyEnd;

        protected override void OnCreate()
        {
            btnClaim.WhenClick = _OnBtnClaim;
            UIUtility.CommonItemSetup(rewardRoot);
        }

        protected override void OnParse(params object[] items)
        {
            rewards = items[0] as List<RewardCommitData>;
            _onFlyEnd = items[1] as Action;
        }

        protected override void OnPreOpen()
        {
            // isEntering = true;

            Game.Manager.audioMan.TriggerSound("DiggingLevelReward");
            _ShowRewards();
        }

        protected override void OnPostClose()
        {
            base.OnPostClose();
            isEntering = false;
        }

        private void _ShowRewards()
        {
            showRewards.Clear();
            foreach (var item in rewards)
            {
                var r = new RewardConfig
                {
                    Id = item.rewardId,
                    Count = item.rewardCount
                };
                showRewards.Add(r);
            }

            UIUtility.CommonItemRefresh(rewardRoot, showRewards);
        }

        private void _OnBtnClaim()
        {
            if (isEntering) return;
            var root = rewardRoot;
            for (int i = 0; i < root.childCount; i++)
            {
                if (i < rewards.Count)
                {
                    var from = root.GetChild(i).position;
                    UIFlyUtility.FlyReward(rewards[i], from, () => { _onFlyEnd?.Invoke(); }, 66f);
                }
            }

            Close();
        }
    }
}