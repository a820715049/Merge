using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;
using static fat.conf.Data;
using DG.Tweening;
using Coffee.UIExtensions;
using FAT.MSG;
using System.Linq;

namespace FAT {
    using static EL.MessageCenter;

    public class ActivityDuelMilestone : RewardBar {
        public Animator animator;
        internal TextMeshProUGUI index;
        internal MBRewardIcon reward;
        internal TextMeshProUGUI valueL;
        internal readonly List<NodeInfo> segment = new();
        internal NodeOption option;
        private ActivityDuel activity;
        internal List<Config.RewardConfig> rList;
        internal IList<int> rScore;

        internal override void Awake() {
            base.Awake();
            var root = transform;
            root.Access("index", out index);
            root.Access("reward/icon", out reward);
            root.Access("reward/value", out valueL);
        }

        public void Refresh(ActivityDuel acti_, int offsetN_ = 0, int offsetV_ = 0) {
            activity = acti_;
            if (!gameObject.activeInHierarchy) return;
            var iConf = GetObjBasic(acti_.MilestoneTokenId);
            icon.SetImage(iConf?.Icon);
            rList = activity.ConfD.MilestoneReward.Select(s => s.ConvertToRewardConfig()).ToList();
            rScore = activity.ConfD.MilestoneScore;
            bar.Init();
            RefreshA(offsetN_, offsetV_);
            RefreshReward();
        }

        public void RefreshA(int offsetN_, int offsetV_) {
            Refresh(activity.MilestoneCur + offsetN_, activity.Round + offsetV_);
            RefreshIndex();
        }

        public void RefreshIndex() {
            index.text = EL.I18N.FormatText("#SysComDesc918", activity.Round + 1);
        }

        public void Refresh(int next_, int value_) {
            segment.Clear();
            var vv = 0;
            for (var k = 0; k < rList.Count; ++k) {
                var n = rList[k];
                var v = rScore[k];
                vv += v;
                segment.Add(new() { reward = n, value = vv, pos = vv, complete = value_ >= vv });
            }
            RefreshList(segment, value_, next_, option);
            list[^1].obj.SetActive(false);
            valueL.text = $"{vv}";
        }

        public void RefreshReward() {
            var r = activity.GetFinialReward();
            reward.Refresh(r.Id);
        }

        public void Reward(int n_) {
            var e = list[n_];
            var data = activity.MilestoneCommitReward;
            if (data == null) {
                EL.DebugEx.Warning($"{nameof(ActivityDuel)} milestone reward data not found");
                return;
            }
            if (!Game.Manager.specialRewardMan.IsBusy()) {
                UIFlyUtility.FlyReward(data, e.root.position);
                activity.MilestoneCommitReward = null;
            }
            e.icon.icon.GetComponent<Animator>().Play("UIActivityDuel_RewardItem_Punch");
            e.tick.GetComponent<Animator>().Play("UIActivityDuelReward_Right_Show");
        }

        public bool TargetStep() {
            var delta = Time.deltaTime;
            var pass = TargetStep(segment, delta, option_:option);
            if (pass) {
                Reward(next - 1);
                RefreshIndex();
            }
            return pass;
        }
    }
}