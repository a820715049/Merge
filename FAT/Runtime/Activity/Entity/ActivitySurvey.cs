using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using FAT.Platform;
using Config;
using EL.Resource;
using System.Linq;

namespace FAT {
    using static PoolMapping;

    public class ActivitySurvey : ActivityLike {
        public EventSurvey confD;
        public override bool Valid => confD != null;
        public UIResAlt Res { get; } = new(UIConfig.UIActivitySurvey);
        public UIResAlt RewardRes { get; } = new(UIConfig.UIActivitySurveyReward);
        public ActivityVisual VisualReward { get; } = new();
        public PopupActivity Popup { get; internal set; }
        public RewardConfig SurveyReward { get; private set; }

        public ActivitySurvey() { }


        public ActivitySurvey(ActivityLite lite_) {
            Lite = lite_;
            confD = GetEventSurvey(lite_.Param);
            if (confD == null) return;
            if (Visual.Setup(confD.EventTheme, Res)) {
                Popup = new(this, Visual, Res, false);
            }
            VisualReward.Setup(confD.RewardTheme, RewardRes);
            SurveyReward = confD.Reward.ConvertToRewardConfig();
        }

        public override void SaveSetup(ActivityInstance data_) {
            // var any = data_.AnyState;
        }

        public override void LoadSetup(ActivityInstance data_) {
            // var any = data_.AnyState;
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate() {
            if (!Valid) yield break;
            foreach(var v in Visual.ResEnumerate()) yield return v;
            foreach(var v in VisualReward.ResEnumerate()) yield return v;
        }
        
        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            popup_.TryQueue(Popup, state_);
        }

        public override void Open() => Open(Res);

        public void OpenSurvey(Action<bool> WhenComplete_) {
            DataTracker.Survey.TrackEnter(this);
            var sdk = PlatformSDK.Instance.Adapter;
            var iap = Game.Manager.iap;
            var level = Game.Manager.mergeLevelMan;
            var param = $@"
            {{
                ""Fpid"":""{sdk.SessionId}"",
                ""Level"":""{level.displayLevel}"",
                ""Total_pay"":""{iap.TotalIAPServer}"",
                ""Country"":""{sdk.GetCountryCode()}""
            }}";
            DebugEx.Info($"survey: {confD.Hash}\n{param}");
            sdk.OpenSurvey(confD.Hash, param, (r, e) => {
                if (!r) {
                    DebugEx.Warning(e.Message);
                }
                WhenComplete_?.Invoke(r);
            });
        }

        public void ClaimReward(IList<RewardCommitData> list_) {
            var rewardMan = Game.Manager.rewardMan;
            var r = SurveyReward;
            var d = rewardMan.BeginReward(r.Id, r.Count, ReasonString.survey);
            list_.Add(d);
            DataTracker.Survey.TrackReward(this);
            Game.Manager.activity.EndImmediate(this, false);
        }
    }
}