using System.Collections;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using System;
using DataDE = fat.gamekitdata.DailyEvent;
using static DataTracker;
using EL.Resource;
using System.Linq;

namespace FAT {
    using static PoolMapping;

    public class ActivityDE : ActivityLike {
        public DailyEventList confD;
        public override bool Valid => confD != null;
        public bool SetupValid => Lite.Valid && confD != null && endTS > 0;
        public ActivityVisual VisualNotice { get; } = new();
        public ActivityVisual VisualTask { get; } = new();
        public UIResAlt NoticeRes { get; } = new(UIConfig.UINoticeDaily);
        public UIResAlt TaskRes { get; } = new(UIConfig.UIDailyEvent);
        public PopupDE Popup { get; internal set; } = new();

        public bool Match(LiteInfo lite_, DailyEventList confD_) => Lite.Match(lite_) && confD == confD_;

        public void Setup(LiteInfo lite_, DailyEventList confD_) {
            SetupClear();
            Lite = ActivityLite.TrySetup(Lite, lite_, EventType.De, replace_:true);
            Lite.WillRecord = false;
            confD = confD_;
            if (Visual.Setup(confD.EventTheme, NoticeRes)) {
                Popup.Setup(this, Visual, NoticeRes);
            }
            VisualNotice.Setup(confD.NoticeTheme);
            VisualTask.Setup(confD.TaskTheme, TaskRes);
        }

        public override void SetupClear() {
            confD = null;
            Lite?.Clear();
            Visual.Clear();
            Popup.Clear();
        }

        public bool KeepLegacy() {
            var day = DailyEvent.DayOfWeek(Game.UtcNow, -10);//NOTE fixed value used, keep it the same as actual refresh time
            return Id < 0 && confD != null && confD.ActiveWeekday == day;
        }

        public override void SaveSetup(ActivityInstance data_) {
            // var any = data_.AnyState;
            // any.Add(ToRecord(1, buyCount));
        }

        public override void LoadSetup(ActivityInstance data_) {
            // var any = data_.AnyState;
            // buyCount = ReadInt(1, any);
        }

        public override IEnumerable<(string, AssetTag)> ResEnumerate() {
            if (!Valid) yield break;
            foreach(var v in Visual.ResEnumerate()) yield return v;
            foreach(var v in VisualNotice.ResEnumerate()) yield return v;
            foreach(var v in VisualTask.ResEnumerate()) yield return v;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            if (Popup.PopupValid) popup_.TryQueue(Popup, state_);
        }

        public override void Open() => Open(NoticeRes);
        public bool OpenTask() { Open(TaskRes); return true; }

        public override void WhenEnd() {
            SetupClear();
        }
    }
}