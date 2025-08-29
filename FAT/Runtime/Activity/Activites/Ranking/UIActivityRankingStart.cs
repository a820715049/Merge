using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEngine.UI;
using System;
using TMPro;
using FAT.MSG;

namespace FAT {
    using static MessageCenter;

    public class UIActivityRankingStart : UIBase {
        internal TextMeshProUGUI cd;
        internal readonly List<MBRewardLayout> reward = new();
        public UIVisualGroup vGroup;

        private ActivityRanking activity;
        private Action WhenTick;

        private void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access(out vGroup);
            var root = transform.Find("Content");
            vGroup.Prepare(root.Access<UIImageRes>("bg", try_:true), "bgPrefab");
            vGroup.Prepare(root.Access<UIImageRes>("bg1"), "titleBg");
            vGroup.Prepare(root.Access<TextProOnACircle>("title"), "mainTitle");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "desc");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("_cd/text"), "time");
            vGroup.Prepare(root.Access<UIImageRes>("_cd/frame"), "time");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("confirm/text"), "button");
            vGroup.CollectTrim();
        }

        protected override void OnCreate() {
            var root = transform.Find("Content");
            for (var k = 0; k < 3; ++k) {
                root.Access($"_reward{k + 1}", out MBRewardLayout r);
                reward.Add(r);
            }
            root.Access("_cd/text", out cd);
            root.Access("close", out MapButton close);
            root.Access("confirm", out MapButton confirm);
            close.WhenClick = Close;
            confirm.WhenClick = Confirm;
            WhenTick ??= RefreshCD;
        }

        protected override void OnParse(params object[] items) {
            activity = (ActivityRanking)items[0];
        }

        protected override void OnPreOpen() {
            RefreshTheme();
            RefreshReward();
            RefreshCD();
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnPreClose() {
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        public void RefreshTheme() {
            var visual = activity.VisualStart;
            visual.Refresh(vGroup);
        }

        public void RefreshReward() {
            var rList = activity.reward;
            for(var k = 0; k < reward.Count; ++k) {
                var t = reward[k];
                if (k >= rList.Count) {
                    t.RefreshEmpty(0);
                }
                else {
                    t.Refresh(rList[k]);
                }
            }
        }

        public void RefreshCD() {
            var t = Game.TimestampNow();
            var diff = (long)Mathf.Max(0, activity.endTS - t);
            cd.text = activity.entry.TextCD(diff);
        }

        internal void Confirm() {
            Close();
            activity.Open();
        }
    }
}