using System;
using System.Collections.Generic;
using fat.rawdata;
using static fat.conf.Data;
using EL;

namespace FAT {
    public class PackNU : GiftPack {
        public NewUserPack conf;
        public override int PackId { get => conf.PackId; set {} }
        public override int ThemeId => conf.EventTheme;
        public override UIResAlt Res { get; } = new(UIConfig.UIGiftPackNU);
        public override int StockTotal => conf.Paytimes;

        public PackNU() { }

        public PackNU(NewUserPack conf_) {
            conf = conf_;
            var ts = Game.UtcNow;
            var te = ts.AddSeconds(conf_.Duration);
            Lite = new ActivityLiteFlex() {
                Id = conf_.Id,
                Type = conf_.EventType,
                Param = conf_.Id,
                StartTS = (long)(ts - DateTime.UnixEpoch).TotalSeconds,
                EndTS = (long)(te - DateTime.UnixEpoch).TotalSeconds,
            };
            DebugEx.Info($"{nameof(Activity)} {nameof(PackNU)} {ts}->{te}");
            RefreshTheme(popupCheck_:true);
            RefreshContent();
        }
    }
}