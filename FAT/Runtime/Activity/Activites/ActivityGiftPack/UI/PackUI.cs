using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT {
    public abstract class PackUI : UIBase {
        public float closeDelay = 1.2f;
        internal TextMeshProUGUI cd;
        internal TextMeshProUGUI stock;
        internal MBRewardLayout layout;
        internal MapButton confirm;
        public UIVisualGroup visualGroup;
        internal Action<ActivityLike, bool> WhenEnd;
        internal Action<ActivityLike> WhenRefresh;
        internal Action<IList<RewardCommitData>> WhenPurchase;
        internal Action WhenTick;
        internal abstract ActivityLike Pack { get; }

        internal virtual void OnValidate() {
            if (Application.isPlaying) return;
            transform.TryGetComponent(out visualGroup);
            transform.Access("Content", out Transform root);
            if (visualGroup != null) {
                visualGroup.Prepare(root.Access<UIImageRes>("bg", try_:true), "bgPrefab");
                visualGroup.Prepare(root.Access<UIImageRes>("bg1"), "titleBg");
                visualGroup.Prepare(root.Access<TextProOnACircle>("title"), "mainTitle");
                visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "subTitle");
                visualGroup.Prepare(root.Access<TextMeshProUGUI>("_cd/text"), "time");
                visualGroup.Prepare(root.Access<UIImageRes>("_cd/frame"), "time");
                visualGroup.CollectTrim();
            }
        }

        protected override void OnCreate() {
            transform.Access("Content", out Transform root);
            root.Access("_cd/text", out cd);
            root.Access("stock", out stock, try_:true);
            root.Access("_group", out layout, try_:true);
            if (root.Access("confirm", out confirm, try_:true)) confirm.WithClickScale().FixPivot().WhenClick = ConfirmClick;
            if (transform.Access<MapButton>("Content/close", out var close, try_:true)) close.WithClickScale().FixPivot().WhenClick = Close;
            
        }

        public void RefreshCD() {
            var t = Game.TimestampNow();
            var diff = (long)Mathf.Max(0, Pack.endTS - t);
            UIUtility.CountDownFormat(cd, diff);
        }

        public virtual void RefreshTheme() {
            var visual = Pack.Visual;
            visual.Refresh(visualGroup);
        }

        internal void RefreshEnd(ActivityLike pack_, bool expire_) {
            if (pack_ != Pack || !expire_) return;
            Close();
        }

        internal virtual void PurchaseComplete(IList<RewardCommitData> list_) {
            Pack.ResetPopup();
            for (var n = 0; n < list_.Count; ++n) {
                var pos = layout.list[n].icon.transform.position;
                UIFlyUtility.FlyReward(list_[n], pos);
            }
            IEnumerator DelayClose() {
                yield return new WaitForSeconds(closeDelay);
                Close();
            }
            StartCoroutine(DelayClose());
        }

        internal abstract void ConfirmClick();
    }
}