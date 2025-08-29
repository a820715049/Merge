using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using TMPro;
using EL;
using static fat.conf.Data;
using DG.Tweening;
using Config;

namespace FAT {
    public class UIDEMReward : UIBase {
        private MBRewardIcon icon;
        private RewardCommitData reward;

        protected override void OnCreate() {
            transform.Access("Mask", out MapButton mask);
            mask.WhenClick = Close;
            transform.Access("Content", out Transform root);
            root.Access("entry", out icon);
        }

        protected override void OnParse(object[] v_) {
            reward = (RewardCommitData)v_[0];
        }

        protected override void OnPreOpen() {
            icon.Refresh(reward.rewardId, reward.rewardCount);
        }

        protected override void OnPreClose() {
            var pos = icon.icon.transform.position;
            UIFlyUtility.FlyReward(reward, pos);
            UIManager.Instance.CloseWindow(UIConfig.UIDailyEvent);
            reward = null;
        }
    }
}