using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT {
    using static PoolMapping;

    public class UIActivitySurvey : UIBase {
        private MBRewardIcon reward;
        public UIVisualGroup vGroup;
        
        private ActivitySurvey activity;

        private void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access(out vGroup);
            var root = transform.Find("Content/root");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("title"), "mainTitle");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "subTitle");
            vGroup.CollectTrim();
        }

        protected override void OnCreate() {
            transform.Access("Mask", out MapButton mask);
            var root = transform.Find("Content/root");
            root.Access("close", out MapButton close);
            root.Access("reward", out reward);
            root.Access("confirm", out MapButton confirm);
            mask.WhenClick = Close;
            close.WithClickScale().WhenClick = Close;
            confirm.WithClickScale().WhenClick = ConfirmClick;
        }

        protected override void OnParse(params object[] items) {
            activity = (ActivitySurvey)items[0];
        }

        protected override void OnPreOpen() {
            Refresh();
        }

        public void Refresh() {
            RefreshTheme();
            reward.Refresh(activity.SurveyReward);
        }

        public void RefreshTheme() {
            var visual = activity.Visual;
            visual.Refresh(vGroup);
        }

        internal void ConfirmClick() {
            activity.OpenSurvey(r => {
                if (!r) return;
                Close();
                var list = PoolMappingAccess.Take<List<RewardCommitData>>();
                activity.ClaimReward(list.obj);
                UIManager.Instance.OpenWindow(activity.RewardRes.ActiveR, activity, list);
            });
        }
    }
}