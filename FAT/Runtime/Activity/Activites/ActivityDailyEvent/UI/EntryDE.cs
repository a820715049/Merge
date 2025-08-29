using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System;
using fat.rawdata;
using fat.gamekitdata;
using FAT.MSG;

namespace FAT {
    public class EntryDE : MonoBehaviour {
        private LayoutElement element;
        private GameObject group;
        private UIImageState bg;
        private MBRewardIcon reward1;
        private MBRewardIcon reward2;
        private MBRewardProgress progress;
        private TextMeshProUGUI cd;
        private GameObject claim;
        private GameObject claimEffect;
        private GameObject next;
        private Action<int> WhenLevelup;
        private Action WhenUpdate;
        private Action WhenCD;

        public void Awake() {
            transform.Access(out element);
            var root = transform.Find("group");
            group = root.gameObject;
            root.Access("button", out bg);
            root.Access("reward1", out reward1);
            root.Access("reward2", out reward2);
            root.Access("progress", out progress);
            root.Access("cd", out cd);
            claim = root.TryFind("claim");
            claimEffect = root.TryFind("fx_entry_group_glow");
            next = root.TryFind("next");
            var button = root.Access<MapButton>().WithClickScale().FixPivot();
            button.WhenClick = EntryClick;
            MessageCenter.Get<MSG.ACTIVITY_STATE>().AddListener(RefreshActive);
            RefreshActive(Game.Manager.dailyEvent.ActivityD);
        }

        public void OnDestroy() {
            MessageCenter.Get<MSG.ACTIVITY_STATE>().RemoveListener(RefreshActive);
        }

        public void OnEnable() {
            Refresh();
            WhenLevelup ??= _ => Refresh();
            WhenUpdate ??= Refresh;
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.DAILY_EVENT_TASK_UPDATE_ANY>().AddListener(WhenUpdate);
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().AddListener(WhenLevelup);
        }

        public void OnDisable() {
            MessageCenter.Get<MSG.DAILY_EVENT_TASK_UPDATE_ANY>().RemoveListener(WhenUpdate);
            MessageCenter.Get<MSG.GAME_MERGE_LEVEL_CHANGE>().RemoveListener(WhenLevelup);
        }

        public void RefreshActive(ActivityLike acti_) {
            if (acti_ is not ActivityDE) return;
            Visible(acti_.Active);
            if (acti_.Active) Refresh();//redundantly ensures refresh
        }

        public void Refresh() {
            var de = Game.Manager.dailyEvent;
            var valid = de.Valid && de.Unlocked && de.ActivityD.Active;
            Visible(valid);
            if (!valid) return;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListenerUnique(WhenCD);
            RefreshCD();
            var gValid = de.GroupValid;
            next.SetActive(!gValid);
            var vLast = claim.activeSelf;
            var vNow = gValid && de.GroupComplete;
            claim.SetActive(vNow);
            claimEffect.SetActive(vNow);
            if (vLast != vNow && vNow) {
                MessageCenter.Get<BOARD_ORDER_SCROLL_RESET>().Dispatch();
            }
            bg.Select(gValid ? de.iconReward2 == null ? 0 : 1 : 2);
            static void RefreshReward(Config.RewardConfig r_, MBRewardIcon icon_) {
                var v = r_ != null;
                icon_.gameObject.SetActive(v);
                if (v) icon_.Refresh(r_);
            }
            RefreshReward(de.iconReward1, reward1);
            RefreshReward(de.iconReward2, reward2);
            progress.gameObject.SetActive(gValid);
            if (gValid) progress.Refresh(de.TaskComplete, de.TaskCount);
        }

        public void RefreshCD() {
            var v = Game.Manager.dailyEvent.ActivityD.Countdown;
            UIUtility.CountDownFormat(cd, v);
        }

        private void Visible(bool v_) {
            group.SetActive(v_);
            element.ignoreLayout = !v_;
        }

        private void EntryClick() {
            var de = Game.Manager.dailyEvent;
            if (de.GroupValid && de.GroupComplete) {
                var data = de.ClaimGroup();
                if (data != null) {
                    var pos = reward1.icon.transform.position;
                    UIFlyUtility.FlyReward(data, pos);
                }
                return;
            }
            de.OpenTask();
        }
    }
}