using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT {
    using static PoolMapping;

    public class UIActivitySurveyReward : UIBase {
        private MBRewardLayout reward;
        public UIVisualGroup vGroup;
        
        private ActivitySurvey activity;
        private Ref<List<RewardCommitData>> listRef;
        private MBRewardLayout.CommitList result;

        private void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access(out vGroup);
            var root = transform.Find("Content/root");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("title"), "mainTitle");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "subTitle");
            vGroup.CollectTrim();
        }

        protected override void OnCreate() {
             var root = transform.Find("Content/root");
            root.Access("close", out MapButton close);
            root.Access("_group", out reward);
            root.Access("confirm", out MapButton confirm);
            confirm.WithClickScale().WhenClick = ConfirmClick;
        }

        protected override void OnParse(params object[] items) {
            activity = (ActivitySurvey)items[0];
            listRef = (Ref<List<RewardCommitData>>)items[1];
            result = new() { list = listRef.obj };
        }

        protected override void OnPreOpen() {
            Refresh();
        }

        protected override void OnPreClose() {
            listRef.Free();
            result.list = null;
        }

        public void Refresh() {
            RefreshTheme();
            reward.Refresh(result);
        }

        public void RefreshTheme() {
            var visual = activity.VisualReward;
            visual.Refresh(vGroup);
        }

        internal void ConfirmClick() {
            for(var k = 0; k < result.list.Count; ++k) {
                var d = result.list[k];
                var n = reward.list[k];
                UIFlyUtility.FlyReward(d, n.icon.transform.position);
            }
            Close();
        }
    }
}