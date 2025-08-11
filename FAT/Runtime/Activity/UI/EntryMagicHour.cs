using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using EL;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using FAT.MSG;

namespace FAT {
    using static MessageCenter;

    public class EntryMagicHour : MonoBehaviour {
        internal TextMeshProUGUI cd;
        public UIVisualGroup vGroup;
        
        private ActivityMagicHour activity;
        private Action WhenTick;

        private void OnValidate() {
            transform.Access(out vGroup);
            vGroup.Prepare(transform.Access<UIImageRes>("icon"), "entryIcon");
            vGroup.Prepare(transform.Access<TextMeshProUGUI>("title"), "entryTitle");
            vGroup.Prepare(transform.Access<TextMeshProUGUI>("cd"), "entryCD");
            vGroup.CollectTrim();
        }

        public void Awake() {
            transform.Access("cd", out cd);
            transform.Access(out MapButton button);
            button.WhenClick = Click;
            WhenTick = RefreshCD;
        }

        public void OnEnable() {
            activity = (ActivityMagicHour)Game.Manager.activity.LookupAny(fat.rawdata.EventType.Wishing);
            RefreshTheme();
            RefreshCD();
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        public void OnDisable() {
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        public void RefreshTheme() {
            var visual = activity.Visual;
            visual.Refresh(vGroup);
        }

        public void RefreshCD() {
            var t = Game.TimestampNow();
            var diff = (long)Mathf.Max(0, activity.endTS - t);
            UIUtility.CountDownFormat(cd, diff);
        }

        public void Click() {
            activity.Open();
        }
    }
}