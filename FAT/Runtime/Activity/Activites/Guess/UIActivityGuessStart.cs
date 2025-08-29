using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEngine.UI;
using System;
using TMPro;
using FAT.MSG;

namespace FAT
{
    using static MessageCenter;

    public class UIActivityGuessStart : UIBase
    {
        internal TextMeshProUGUI cd;
        internal MBRewardLayout prize;
        public UIVisualGroup visualGroup;

        private ActivityGuess activity;
        private Action WhenTick;
        private bool restart;

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            transform.Access(out visualGroup);
            var root = transform.Find("Content");
            visualGroup.Prepare(root.Access<UIImageRes>("bg", try_: true), "bgPrefab");
            visualGroup.Prepare(root.Access<UIImageRes>("bg1"), "titleBg");
            visualGroup.Prepare(root.Access<TextProOnACircle>("title"), "mainTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "subTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc1", try_: true), "desc");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_cd/text"), "time");
            visualGroup.Prepare(root.Access<UIImageRes>("_cd/frame"), "time");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("confirm/text"), "confirm");
            visualGroup.Prepare(root.Access<UIImageRes>("prize_preview/button"), "prize");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("prize_preview/button/text"), "prize1");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("prize_preview/group/title"), "prize");
            visualGroup.CollectTrim();
        }

        protected override void OnCreate()
        {
            var root = transform.Find("Content");
            root.Access("prize_preview/group", out prize);
            root.Access("_cd/text", out cd);
            WhenTick ??= RefreshCD;
            restart = name.Contains("Restart");
            root.AddButton("close", Close);
            root.AddButton("confirm", Confirm);
        }

        protected override void OnParse(params object[] items)
        {
            activity = (ActivityGuess)items[0];
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
            RefreshReward();
            RefreshCD();
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnPreClose()
        {
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        public void RefreshTheme()
        {
            var visual = restart ? activity.VisualRestart : activity.VisualStart;
            visual.Refresh(visualGroup);
        }

        public void RefreshReward()
        {
            prize.Refresh(activity.Prize.reward);
        }

        public void RefreshCD()
        {
            var t = Game.TimestampNow();
            var diff = (long)Mathf.Max(0, activity.endTS - t);
            UIUtility.CountDownFormat(cd, diff);
        }

        internal void Confirm()
        {
            Close();
            if (activity.Token > 0) activity.Open();
        }
    }
}
