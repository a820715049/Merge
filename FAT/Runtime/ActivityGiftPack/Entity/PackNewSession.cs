using System;
using System.Collections.Generic;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT {
    public class PackNewSession : GiftPack {
        public NewSessionPack confD;
        public override int PackId { get; set; }
        public override int ThemeId => confD.EventTheme;
        public override int StockTotal => confD.Paytimes;

        public static (bool, string) ReadyToCreate(int id_) {
            var confD = GetNewSessionPack(id_);
            var r = confD != null && Activity.LevelValid(confD.ActiveLevel, confD.ShutdownLevel);
            return (r, r ? "not ready by config" : null);
        }

        public PackNewSession() { }


        public PackNewSession(ActivityLite lite_) {
            Lite = lite_;
            confD = GetNewSessionPack(lite_.Param);
            RefreshTheme();
        }

        public override void SetupFresh() {
            PackId = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.PackGrpId);
            RefreshContent();
        }
    }
}