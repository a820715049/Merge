using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using FAT.Platform;
using System.Collections;
using fat.msg;
using FAT.MSG;

namespace FAT {
    using static EL.PoolMapping;
    using static MessageCenter;

    public class ActivityInvite : ActivityLike {
        public EventInvite confD;
        public override bool Valid => confD != null;
        public UIResAlt Res { get; } = new(UIConfig.UIActivityInvite);
        public PopupActivity Popup { get; internal set; }
        public List<RewardBar.NodeInfo> Node { get; } = new();
        public Dictionary<int, RewardCommitData> Cache { get; } = new();
        public int value;
        private long refreshTS;

        public static void DebugReset() {
            var acti = (ActivityInvite)Game.Manager.activity.LookupAny(EventType.Invite);
            acti.UpdateValue(acti.confD.LieNum);
        }

        public static void DebugAdvance() {
            var acti = (ActivityInvite)Game.Manager.activity.LookupAny(EventType.Invite);
            acti.UpdateValue(acti.value + 1);
        }

        public ActivityInvite() { }


        public ActivityInvite(ActivityLite lite_) {
            Lite = lite_;
            confD = GetEventInvite(lite_.Param);
            if (confD == null) return;
            if (Visual.Setup(confD.EventTheme, Res)) {
                Popup = new(this, Visual, Res, false);
            }
            SetupTheme();
            value = confD.LieNum;
            var c = Math.Min(confD.Num.Count, confD.Reward.Count);
            if (c == 0) return;
            for(var k = 0; k < c; ++k) {
                Node.Add(new() {
                    value = confD.Num[k],
                    pos = k + 1,
                    reward = confD.Reward[k].ConvertToRewardConfig(),
                });
            }
        }

        public void SetupTheme() {
            var map = new VisualMap(Visual.Theme.TextInfo);
            map.TryReplace("entry_tip", "#SysComDesc592");
            map = new VisualMap(Visual.Theme.StyleInfo);
            map.TryReplace("entry_tip", "9");
        }

        public override void SaveSetup(ActivityInstance data_) {
            // var any = data_.AnyState;
        }

        public override void LoadSetup(ActivityInstance data_) {
            // var any = data_.AnyState;
        }
        
        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            popup_.TryQueue(Popup, state_);
        }

        public override void Open() => Open(Res);

        public override void WhenActive(bool new_) {
            RefreshAdd();
        }

        public override void WhenEnd() {
            RefreshRemove();
        }

        public override void WhenReset() {
            RefreshRemove();
        }

        public void RefreshAdd() {
            Get<MERGE_TO_SCENE>().AddListener(SyncStat);
            Get<SCENE_TO_MERGE>().AddListener(SyncStat);
            Get<GAME_BOARD_ITEM_MERGE>().AddListener(SyncStat1);
            Get<ORDER_FINISH>().AddListener(SyncStat);
        }

        public void RefreshRemove() {
            Get<MERGE_TO_SCENE>().RemoveListener(SyncStat);
            Get<SCENE_TO_MERGE>().RemoveListener(SyncStat);
            Get<GAME_BOARD_ITEM_MERGE>().RemoveListener(SyncStat1);
            Get<ORDER_FINISH>().RemoveListener(SyncStat);
        }

        public void ShareLink(bool share_, Action<bool, SDKError> WhenComplete_) {
            PlatformSDK.Instance.shareLink.TrySend(this, share_, WhenComplete_);
        }

        public void SyncStat() => SyncStat(null);
        public void SyncStat1(Merge.Item _) => SyncStat(null);
        public void SyncStat(Action WhenComplete_) {
            var now = Game.TimestampNow();
            if (now - refreshTS < 1) return;
            IEnumerator R() {
                var task = Game.Manager.networkMan.DeeplinkStat(startTS);
                yield return task;
                if (!task.isSuccess || task.result is not GetInviteeStatResp resp) {
                    DebugEx.Error($"failed to check invite stat ({task.errorCode}){task.error} {task.result}");
                    yield break;
                }
                UpdateValue(confD.LieNum + resp.InviteeNum);
                WhenComplete_?.Invoke();
            }
            Game.StartCoroutine(R());
            refreshTS = now;
        }

        public void UpdateValue(int value_) {
            var oValue = value;
            value = value_;
            var ui = UIManager.Instance;
            if (value_ > oValue) {
                for (var k = 0; k < Node.Count; ++k) {
                    var n = Node[k];
                    if (n.value > oValue && n.value <= value_) {
                        var r = n.reward;
                        var data = Game.Manager.rewardMan.BeginReward(r.Id, r.Count, ReasonString.invite);
                        Cache[k] = data;
                    }
                }
                if (value >= Node[^1].value) {
                    Game.Manager.activity.EndImmediate(this, false);
                }
                if (Cache.Count > 0 && !ui.IsOpen(Res.ActiveR)) {
                    Open();
                }
            }
        }

        public ListActivity.IEntrySetup SetupEntry(ListActivity.Entry e_) {
            e_.token.gameObject.SetActive(true);
            var vis = e_.visual;
            vis.Clear();
            vis.Add(e_.token, "entry_tip");
            Visual.Refresh(e_.visual);
            return null;
        }
    }
}