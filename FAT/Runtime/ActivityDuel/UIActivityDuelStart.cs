using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEngine.UI;
using System;
using TMPro;
using FAT.MSG;

namespace FAT {
    using static MessageCenter;
    using static PoolMapping;

    public class UIActivityDuelStart : UIBase {
        internal TextMeshProUGUI cd;
        internal MBRewardIcon prize;
        public UIVisualGroup visualGroup;

        private ActivityDuel activity;
        private Action WhenTick;

        private void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access(out visualGroup);
            var root = transform.Find("Content");
            visualGroup.Prepare(root.Access<UIImageRes>("bg", try_:true), "bgPrefab");
            visualGroup.Prepare(root.Access<UIImageRes>("bg1"), "titleBg");
            visualGroup.Prepare(root.Access<TextProOnACircle>("title"), "mainTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "subTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc1", try_:true), "desc");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_cd/text"), "time");
            visualGroup.Prepare(root.Access<UIImageRes>("_cd/frame"), "time");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("confirm/text"), "confirm");
            visualGroup.Prepare(root.Access<UIImageRes>("prize_preview/entry/icon"), "prize");
            visualGroup.CollectTrim();
        }

        protected override void OnCreate() {
            var root = transform.Find("Content");
            root.Access("prize_preview/entry", out prize);
            root.Access("_cd/text", out cd);
            root.Access("close", out MapButton close);
            root.Access("info", out MapButton info);
            root.Access("confirm", out MapButton confirm);
            close.WhenClick = Close;
            info.WhenClick = Info;
            confirm.WhenClick = Confirm;
            WhenTick ??= RefreshCD;
        }

        protected override void OnParse(params object[] items) {
            activity = (ActivityDuel)items[0];
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
            visual.Refresh(visualGroup);
            visual.visual.RefreshText(visualGroup, "subTitle", "\u0301");
        }

        public void RefreshReward() {
            var r = activity.GetFinialReward();
            prize.RefreshInfo(r.Id);
        }

        public void RefreshCD() {
            var t = Game.TimestampNow();
            var diff = (long)Mathf.Max(0, activity.endTS - t);
            UIUtility.CountDownFormat(cd, diff);
        }

        internal void Info() {
            UIManager.Instance.OpenWindow(activity.VisualHelp.res.ActiveR);
        }

        internal void Confirm() {
            Close();
            activity.SetRoundStart();
            activity.Open();
            Game.Manager.screenPopup.Wait(activity.VisualMain.res.ActiveR);
        }
    }
}