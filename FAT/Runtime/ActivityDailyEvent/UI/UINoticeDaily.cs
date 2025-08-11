using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using TMPro;
using EL;
using static fat.conf.Data;
using DG.Tweening;

namespace FAT {
    public class UINoticeDaily : UIBase {
        private TextMeshProUGUI cd;
        private HelpInfo info;
        public UIVisualGroup visualGroup;
        public UIVisualGroup visualNotice;
        private Action WhenTick;

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access("Content", out Transform root);
            transform.Access(out visualGroup);
            visualGroup.Prepare(root.Access<UIImageRes>("bg"), "bgImage");
            visualGroup.Prepare(root.Access<UIImageRes>("bg1"), "titleImage");
            visualGroup.Prepare(root.Access<TextProOnACircle>("title"), "mainTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc", try_:true), "desc1");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("group/desc", try_:true), "desc1");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc2", try_:true), "desc2");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("tip", try_:true), "desc2");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_cd/text"), "time");
            visualGroup.Prepare(root.Access<UIImageRes>("_cd/frame"), "time");
            visualGroup.CollectTrim();
            ValidateInfo(root.Find("info"));
        }

        public void ValidateInfo(Transform root) {
            if (root == null || !root.TryGetComponent(out visualNotice)) return;
            root = root.Find("group");
            if (root == null) goto end;
            visualNotice.Prepare(root.Access<TextMeshProUGUI>("title"), "mainTitle");
            visualNotice.Prepare(root.Access<TextMeshProUGUI>("desc"), "desc");
            visualNotice.Prepare(root.Access<Image>("line"), "line");
            root = root.Find("group");
            if (root == null) goto end;
            //cookie
            for (var k = 0; k < root.childCount; ++k) {
                var cc = root.GetChild(k);
                var icon1 = $"icon{2 * k + 1}";
                var icon2 = $"icon{2 * k + 2}";
                visualNotice.Prepare(cc.Access<UIImageRes>("icon1"), icon1);
                visualNotice.Prepare(cc.Access<UIImageRes>("icon2"), icon2);
                visualNotice.Prepare(cc.Access<UIImageRes>("icon3"), icon1);
            }
            end:
            visualNotice.CollectTrim();
        }
        #endif

        protected override void OnCreate() {
            transform.Access("Content", out Transform root);
            root.Access("close", out MapButton close);
            root.Access("confirm", out MapButton confirm);
            root.Access("info", out info);
            root.Access("_cd/text", out cd);
            Action ConfirmRef = ConfirmClick;
            transform.Access<MapButton>("Mask").WhenClick = Close;
            close.WithClickScale().FixPivot().WhenClick = Close;
            confirm.WithClickScale().FixPivot().WhenClick = ConfirmRef;
        }

        protected override void OnPreOpen() {
            Refresh();
            RefreshCD();
            WhenTick ??= RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnPreClose() {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        public void Refresh() {
            var de = Game.Manager.dailyEvent.ActivityD;
            var visual = de.Visual;
            visual.Refresh(visualGroup);
            visual = de.VisualNotice;
            visual?.Refresh(visualNotice);
            info.RefreshActive();
        }

        public void RefreshCD() {
            var v = Game.Manager.dailyEvent.ActivityD.Countdown;
            UIUtility.CountDownFormat(cd, v);
        }

        public void ConfirmClick() {
            Close();
            var ui = Game.Manager.dailyEvent.OpenTask();
            Game.Manager.screenPopup.Wait(ui);
        }
    }
}