using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEngine.UI;
using System;
using TMPro;
using FAT.MSG;

namespace FAT {
    using static PoolMapping;

    public class UIActivityRankingEnd : UIBase {
        internal MBRewardLayout layout;
        internal UIStateGroup rankGroup;
        internal TextMeshProUGUI rank;
        public UIVisualGroup vGroup;
        public float closeDelay = 1f;
        internal float openTime;

        private ActivityRanking activity;
        private Ref<List<RewardCommitData>> list;

        private void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access(out vGroup);
            var root = transform.Find("Content");
            var root1 = root.Find("Panel");
            vGroup.Prepare(root1.Access<UIImageRes>("bg1"), "bg1");
            vGroup.Prepare(root1.Access<UIImageRes>("bg2"), "bg2");
            vGroup.Prepare(root1.Access<UIImageRes>("bg2a"), "bg2a");
            vGroup.Prepare(root1.Access<UIImageRes>("bg2a"), "bg2a");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("title"), "mainTitle");
            root1.Access("comment", out UITextState comment);
            vGroup.Prepare(comment, "rank1", 0);
            vGroup.Prepare(comment, "rank2", 1);
            vGroup.Prepare(comment, "rank3", 2);
            vGroup.Prepare(comment, "rank4", 3);
            root1.Access("icon", out UIImageState icon);
            vGroup.Prepare(icon, "rank1", 0);
            vGroup.Prepare(icon, "rank2", 1);
            vGroup.Prepare(icon, "rank3", 2);
            vGroup.Prepare(icon, "rank4", 3);
            vGroup.CollectTrim();
        }

        protected override void OnCreate() {
            var root = transform.Find("Content");
            var root1 = root.Find("Panel");
            transform.Access(out rankGroup);
            transform.Access(out rankGroup);
            root1.Access("rank", out rank);
            root1.Access("_group", out layout);
            root.Access("_confirm", out MapButton close);
            close.WhenClick = UserClose;
        }

        protected override void OnParse(params object[] items) {
            activity = (ActivityRanking)items[0];
            list = (Ref<List<RewardCommitData>>)items[1];
        }

        protected override void OnPreOpen() {
            DataTracker.RankingEndUI2(activity);
            openTime = Time.realtimeSinceStartup;
            Game.Manager.audioMan.TriggerSound("HotAirRewardOpen");
            RefreshTheme();
            RefreshReward();
            RefreshRank();
        }

        protected override void OnPreClose() {
            
        }

        public void RefreshTheme() {
            var visual = activity.VisualEnd;
            visual.Refresh(vGroup);
        }

        public void RefreshReward() {
            var valid = list.obj.Count > 0;
            layout.gameObject.SetActive(valid);
            if (valid) {
                layout.Refresh(list.obj);
            }
        }

        public void RefreshRank()
        {
            var r = activity.Rank;
            rank.text = $"{r}";
            rankGroup.SelectNear(r - 1);
            DataTracker.TrackLogInfo("ranking EndData Refresh--->" + r);
        }

        public void MaskClick() {
            var time = Time.realtimeSinceStartup;
            if (time - openTime < closeDelay) return;
            UserClose();
        }

        public void UserClose() {
            Close();
            var l = list.obj;
            for (var k = 0; k < l.Count; ++k) {
                var r = l[k];
                var e = layout.list[k];
                UIFlyUtility.FlyReward(r, e.icon.transform.position);
            }
            list.Free();
            list = default;
        }
    }
}