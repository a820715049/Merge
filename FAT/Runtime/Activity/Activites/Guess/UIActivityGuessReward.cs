using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static EL.PoolMapping;

namespace FAT {
    public class UIActivityGuessReward : UIBase {
        internal MBRewardLayout reward;
        public UIVisualGroup visualGroup;
        
        private ActivityGuess activity;
        private Ref<List<RewardCommitData>> list;
        private Action WhenClose;

        public void OnValidate() {
            if (Application.isPlaying) return;
            visualGroup = transform.GetComponent<UIVisualGroup>();
            transform.Access("Content", out Transform root);
            visualGroup.Prepare(root.Access<UIImageRes>("icon"), "bg");
            visualGroup.CollectTrim();
        }

        protected override void OnCreate() {
            transform.Access("Content", out Transform root);
            transform.Access<MapButton>("Mask").WhenClick = ConfirmClick;
            root.Access<MapButton>("confirm").WithClickScale().WhenClick = ConfirmClick;
            root.Access("reward", out reward);
        }

        protected override void OnParse(params object[] items) {
            activity = (ActivityGuess)items[0];
            list = (Ref<List<RewardCommitData>>)items[1];
            WhenClose = (Action)items[2];
        }

        protected override void OnPreOpen() {
            Refresh();
        }

        protected override void OnPostClose() {
            WhenClose?.Invoke();
        }

        public void Refresh() {
            var visual = activity.VisualReward;
            visual.Refresh(visualGroup);
            reward.Refresh(list.obj);
        }

        internal void ConfirmClick() {
            var rList = list.obj;
            for(var k = 0; k < rList.Count; ++k) {
                var pos = reward.list[k].icon.transform.position;
                UIFlyUtility.FlyReward(rList[k], pos);
            }
            list.Free();
            Close();
        }
    }
}