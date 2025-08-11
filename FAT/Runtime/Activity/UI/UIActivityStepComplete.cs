using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static EL.PoolMapping;

namespace FAT {
    public class UIActivityStepComplete : UIBase {
        internal MBRewardLayout reward;
        public UIVisualGroup visualGroup;
        
        private ActivityStep activity;
        private Ref<List<RewardCommitData>> list;

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
            activity = (ActivityStep)items[0];
            list = (Ref<List<RewardCommitData>>)items[1];
        }

        protected override void OnPreOpen() {
            Refresh();
        }

        protected override void OnPreClose() {
            
        }

        public void Refresh() {
            var visual = activity.VisualComplete;
            visual.Refresh(visualGroup);
            reward.Refresh(list.obj);
        }

        internal void ConfirmClick() {
            var l = list.obj;
            for(var k = 0; k < l.Count; ++k) {
                var pos = reward.list[k].icon.transform.position;
                UIFlyUtility.FlyReward(l[k], pos);
            }
            list.Free();
            Close();
            Game.Manager.activity.EndImmediate(activity, false);
        }
    }
}