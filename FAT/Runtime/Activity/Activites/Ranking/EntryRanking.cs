using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEngine.UI;
using System;
using TMPro;
using FAT.MSG;
using fat.rawdata;

namespace FAT {
    using static MessageCenter;

    public class EntryRanking : MonoBehaviour {
        internal TextMeshProUGUI cd;
        internal UITextState rank;
        internal GameObject up;
        internal UIStateGroup state;
        internal UIStateGroup stateR;
        internal Animator anim;
        public UIVisualGroup vGroup;
        public string rankUpAnim;
        
        private ActivityRanking activity;
        private Action<ActivityRanking, RankingType> WhenRefresh;
        private Action WhenTick;

        private void OnValidate() {
            transform.Access(out vGroup);
            transform.Access("frame", out UIImageState frame);
            vGroup.Prepare(frame, "rank1b", 0);
            vGroup.Prepare(frame, "rank2b", 1);
            vGroup.Prepare(frame, "rank3b", 2);
            vGroup.Prepare(frame, "rank4b", 3);
            transform.Access("rank", out UITextState rank);
            vGroup.Prepare(rank, "rank1", 0);
            vGroup.Prepare(rank, "rank2", 1);
            vGroup.Prepare(rank, "rank3", 2);
            vGroup.Prepare(rank, "rank4", 3);
            vGroup.CollectTrim();
        }

        public void Awake() {
            transform.Access("cd", out cd);
            transform.Access("rank", out rank);
            transform.Access("rank", out stateR);
            up = transform.TryFind("_up");
            transform.Access(out state);
            transform.Access(out anim);
            transform.Access(out MapButton button);
            button.WhenClick = Click;
            WhenRefresh = Refresh;
            WhenTick = RefreshCD;
        }

        public void OnEnable() {
            activity = (ActivityRanking)Game.Manager.activity.LookupAny(fat.rawdata.EventType.Rank);
            activity.Sync();
            RefreshTheme();
            RefreshRank();
            RefreshCD();
            Get<ACTIVITY_RANKING_DATA>().AddListener(WhenRefresh);
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        public void OnDisable() {
            Get<ACTIVITY_RANKING_DATA>().RemoveListener(WhenRefresh);
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        public void RefreshTheme() {
            var visual = activity.VisualRanking;
            visual.Refresh(vGroup);
        }

        public void Refresh(ActivityRanking e_, RankingType _) {
            var cache = activity.Cache;
            var me = cache.Data?.Me;
            var valid = me != null;
            var rUp = activity.entry.CheckRankUp(cache);
            state.Enabled(valid);
            up.SetActive(rUp);
            if (!valid) {
                return;
            }
            var r = (int)me.RankingOrder;
            stateR.SelectNear(r - 1);
            rank.text.text = $"{r}";
            if (rUp)
            {
                anim.Play(rankUpAnim);
                Game.Manager.audioMan.TriggerSound("HotAirGetPointUp");
            }
            DataTracker.TrackLogInfo("ranking EntryData Refresh --->" + r);
        }

        public void RefreshRank() => Refresh(activity, activity.Cache.Type);

        public void RefreshCD() {
            var t = Game.TimestampNow();
            var diff = (long)Mathf.Max(0, activity.endTS - t);
            cd.text = activity.entry.TextCD(diff);
        }

        public void Click() {
            activity.Open();
            RefreshRank();
        }
    }
}