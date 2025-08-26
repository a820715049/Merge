using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using EL;

namespace FAT {
    public class ExchangeTool : ActivityLike {
        public ToolExchange conf;
        public override bool Valid => conf != null;
        public UIResAlt Res { get; } = new(UIConfig.UIToolExchange);
        public PopupActivity Popup { get; internal set; }
        public int price;
        public int buyCount;
        public int StockTotal => conf.LimitNum;
        public int Stock => StockTotal - buyCount;
        public bool WillEnd => Stock == 0;
        public readonly List<Config.RewardConfig> goods = new();

        public ExchangeTool() { }

        public ExchangeTool(ToolExchange conf_) {
            conf = conf_;
            var ts = Game.UtcNow;
            var te = ts.AddSeconds(conf_.Duration);
            Lite = new ActivityLiteFlex() {
                Id = conf_.Id,
                Type = EventType.ToolExchange,
                StartTS = (long)(ts - DateTime.UnixEpoch).TotalSeconds,
                EndTS = (long)(te - DateTime.UnixEpoch).TotalSeconds,
                WillRecord = true,
            };
            DebugEx.Info($"{nameof(Activity)} {nameof(ExchangeTool)} {ts}->{te}");
            if (Visual.Setup(conf_.EventTheme, Res)) {
                Popup = new(this, Visual, Res, false);
            }
            foreach (var r in conf.Reward) {
                goods.Add(r.ConvertToRewardConfig());
            }
        }

        public override void SaveSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            any.Add(ToRecord(1, buyCount));
            any.Add(ToRecord(2, price));
        }

        public override void LoadSetup(ActivityInstance data_) {
            var any = data_.AnyState;
            buyCount = ReadInt(1, any);
            price = ReadInt(2, any);
        }

        public override void SetupFresh() {
            var (_, c, m) = conf.Price.ConvertToInt3();
            price = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(c, m);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            popup_.TryQueue(Popup, state_);
        }

        public override void Open() => Open(Res);
    }
}