using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using FAT.MSG;

namespace FAT {
    using static MessageCenter;

    public class UIActivityInvite : UIBase {
        private InviteProgress bar;
        private TextMeshProUGUI cd;
        public UIVisualGroup vGroup;
        
        private ActivityInvite activity;

        private Action<ActivityLike, bool> WhenEnd;
        private Action WhenTick;
        private Action WhenRefresh;

        private void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access(out vGroup);
            var root = transform.Find("Content/root");
            vGroup.Prepare(root.Access<TextProOnACircle>("title"), "mainTitle");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("e1/desc"), "step1");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("e2/desc"), "step2");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("e3/desc"), "step3");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("desc1"), "subTitle1");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("desc2"), "subTitle2");
            vGroup.Prepare(root.Access<TextMeshProUGUI>("desc3"), "subTitle3");
            vGroup.CollectTrim();
        }

        protected override void OnCreate() {
            transform.Access("Mask", out MapButton mask);
            var root = transform.Find("Content/root");
            root.Access("_cd/text", out cd);
            root.Access("close", out MapButton close);
            root.Access("reward", out bar);
            root.Access("confirmR", out MapButton confirmR);
            root.Access("confirmL", out MapButton confirmL);
            mask.WhenClick = Close;
            close.WithClickScale().WhenClick = Close;
            confirmR.WithClickScale().WhenClick = () => Confirm(share_:false);
            confirmL.WithClickScale().WhenClick = () => Confirm(share_:true);
            WhenEnd ??= RefreshEnd;
            WhenTick ??= RefreshCD;
            WhenRefresh ??= SyncRefresh;
        }

        protected override void OnParse(params object[] items) {
            activity = (ActivityInvite)items[0];
        }

        protected override void OnPreOpen() {
            activity.SyncStat(WhenRefresh);
            Refresh();
            Get<ACTIVITY_END>().AddListener(WhenEnd);
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnPostClose() {
            CheckReward();
        }

        protected override void OnPreClose() {
            Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        public void Refresh() {
            RefreshTheme();
            RefreshCD();
            RefreshBar();
        }

        public void RefreshBar() {
            bar.Refresh(activity);
        }

        public void RefreshTheme() {
            var visual = activity.Visual;
            visual.Refresh(vGroup);
            visual.RefreshText(vGroup, "subTitle2", activity.confD.Level);
        }

        public void RefreshCD() {
            var t = Game.TimestampNow();
            var diff = (long)Mathf.Max(0, activity.endTS - t);
            UIUtility.CountDownFormat(cd, diff);
        }

        internal void RefreshEnd(ActivityLike acti_, bool expire_) {
            if (acti_ != activity || !expire_) return;
            Close();
        }

        public void SyncRefresh() {
            RefreshBar();
            CheckReward();
        }

        public void CheckReward() {
            var cache = activity.Cache;
            var mk = 0;
            foreach(var (k, r) in cache) {
                mk = Mathf.Max(mk, k);
                var e = bar.list[k];
                UIFlyUtility.FlyReward(r, e.icon.transform.position);
            }
            cache.Clear();
            if (mk >= activity.Node.Count - 1) {
                IEnumerator R() {
                    yield return new WaitForSeconds(1.5f);
                    Close();
                }
                StartCoroutine(R());
            }
        }

        internal void Confirm(bool share_) {
            activity.ShareLink(share_, null);
        }
    }
}