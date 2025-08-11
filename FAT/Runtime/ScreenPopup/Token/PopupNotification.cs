using System;
using System.Collections.Generic;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT {
    public class PopupNotification : IScreenPopup {

        public void Setup(ActivityVisual visual_, UIResource res_) {
            if (!visual_.Valid) {
                Clear();
                return;
            }
            PopupId = visual_.PopupId;
            PopupConf = visual_.Popup;
            PopupRes = res_;
            RequireValid = true;
        }

        public virtual void Clear() {
            PopupConf = null;
            RequireValid = false;
        }

        public override bool OpenPopup() {
            if (!Game.Manager.screenPopup.CheckState(PopupState)) return false;
            UIManager.Instance.OpenWindow(PopupRes);
            // DataTracker.notification_popup.Track();
            return true;
        }
    }
}