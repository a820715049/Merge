using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using static EL.MessageCenter;
using FAT.MSG;
using EL.Resource;

namespace FAT {
    public class ActivityAsset : AssetDependency {
        public bool popupLogin;

        public override void WhenReady() {
            var acti = (ActivityLike)target;
            var popup = Game.Manager.screenPopup;
            if (popupLogin) acti.TryPopup(popup, PopupType.Login);
            Game.Manager.activity.AcceptPending(acti);
        }
    }
}