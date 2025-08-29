using System.Collections;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using System;
using DataDE = fat.gamekitdata.DailyEvent;
using static DataTracker;

namespace FAT {
    public class ActivityDEM : ActivityLike {
        public DailyEventMilestone confD;
        public override bool Valid => confD != null;
        public bool SetupValid => Lite.Valid && confD != null && endTS > 0;
        public UIResAlt Res { get; } = new(UIConfig.UIHelpDEM);
        public PopupDE Popup { get; internal set; } = new();

        public bool Match(LiteInfo lite_, DailyEventMilestone confD_) => Lite.Match(lite_) && confD == confD_;

        public void Setup(LiteInfo lite_, DailyEventMilestone confD_) {
            Lite = ActivityLite.TrySetup(Lite, lite_, EventType.Dem, replace_:true);
            Lite.WillRecord = false;
            confD = confD_;
            if (Visual.Setup(confD.PopupTheme, Res)) {
                Popup.Setup(this, Visual, Res);
            }
        }

        public override void SetupClear() {
            confD = null;
            Lite?.Clear();
        }

        public bool KeepLegacy() {
            return Id < 0;
        }

        public override void SaveSetup(ActivityInstance data_) {
            // var any = data_.AnyState;
            // any.Add(ToRecord(1, buyCount));
        }

        public override void LoadSetup(ActivityInstance data_) {
            // var any = data_.AnyState;
            // buyCount = ReadInt(1, any);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            var level = Game.Manager.mergeLevelMan.displayLevel;
            var de = Game.Manager.dailyEvent;
            if (!de.Valid || !de.MilestoneValid
                || level < confD.PopupActiveLv || level >= confD.PopupShutdownLv) return;
            if (Popup.PopupValid) popup_.TryQueue(Popup, state_);
        }

        public override void Open() => Open(Res);

        public override void WhenEnd() {
            SetupClear();
        }
    }
}