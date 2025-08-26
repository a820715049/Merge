using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using fat.rawdata;
using UnityEngine.Rendering;
using static fat.conf.Data;

namespace FAT {
    public class PackDaily : GiftPack, IActivityRedirect {
        public DailyPopPack confD;
        public override int PackId { get; set; }
        public override int ThemeId => confD.EventTheme;
        public override int LabelId => confD.Label;
        public override int StockTotal => confD.Paytimes;
        public string SubType => confD.SubType;
        public IList<int> Pool => confD.ApiPackPool;
        public bool UseAPI => confD.IsApiUse;
        public string ModelVersion => confD.ModelVersion;

        public static (bool, string) ReadyToCreate(int id_) {
            var confD = GetDailyPopPack(id_);
            var r = confD != null && Activity.LevelValid(confD.ActiveLevel, confD.ShutdownLevel);
            return (r, r ? "not ready by config" : null);
        }

        public PackDaily() { }


        public PackDaily(ActivityLite lite_) {
            Lite = lite_;
            confD = GetDailyPopPack(lite_.Param);
            RefreshTheme();
        }

        public override bool SetupPending() {
            if (confD == null) return false;
            var wait = confD.ApiPackPool.Count > 0;
            if (wait) {
                async UniTask R() {
                    var activity = Game.Manager.activity;
                    Popup.option.delay = true;
                    await activity.redirect.TryRequest(this, this);
                    activity.AcceptPending(this);
                    Popup.option.delay = false;
                };
                _ = R();
            }
            return wait;
        }

        public void RedirectApply(int id_) {
            PackId = id_;
            RefreshContent();
        }

        public override void SetupFresh() {
            PackId = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.PackGrpId);
            RefreshContent();
        }
    }
}