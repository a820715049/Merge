using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using EL;
using fat.rawdata;
using FAT.MSG;
using static fat.conf.Data;

namespace FAT {
    public class PackEnergy : GiftPack, IActivityRedirect {
        public EnergyPack confD;

        public override int PackId { get; set; }
        public override int LabelId => confD.Label;
        public override int ThemeId => confD.EventTheme;
        public override int StockTotal => confD.Paytimes;
        public string SubType => confD.SubType;
        public IList<int> Pool => confD.ApiPackPool;
        public bool UseAPI => confD.IsApiUse;
        public string ModelVersion => confD.ModelVersion;

        public PackEnergy(ActivityLite lite_) {
            Lite = lite_;
            confD = GetEnergyPack(lite_.Param);
            RefreshTheme(popupCheck_:true);
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