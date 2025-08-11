using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using EL;

namespace FAT {
    //MarketIAPGift
    public class ActivityMIG : ActivityLike {
        public EventMarketIAPGift confD;
        public override bool Valid => confD != null;
        public UIResAlt Res { get; } = new(UIConfig.UINoticeMIG);
        public PopupActivity Popup { get; internal set; }

        public ActivityMIG(ActivityLite lite_) {
            Lite = lite_;
            confD = GetEventMarketIAPGift(lite_.Param);
            if (confD != null && Visual.Setup(confD.EventTheme, Res)) {
                Popup = new(this, Visual, Res, false);
            }
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

        public int ReplaceSlot(int slot_, int id_) {
            if (confD == null) return id_;
            if(confD.SlotPack.TryGetValue(slot_, out var v)) return v;
            return id_;
        }
    }
}