using System;
using System.Collections.Generic;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT {
    public abstract class IScreenPopup {
        public struct Option {
            public bool ignoreLimit;
            public bool delay;
        }
        public int PopupId { get; internal set; }
        public int PopupCount { get; internal set; }
        public virtual int PopupWeight => PopupConf?.Weight ?? 0;
        public virtual int PopupLimit => PopupConf?.LimitCount ?? 0;
        public virtual bool IgnoreLimit => PopupConf?.NoLimitCount ?? false;
        public virtual UIResource PopupRes { get; internal set; }
        public PopupType PopupState { get; internal set; }
        public PopupType QueueState { get; internal set; }
        public bool PopupValid => CheckValid(out _);
        public bool RequireValid { get; internal set; } = true;
        public Popup PopupConf { get; internal set; }
        public Option option;
        public object Custom { get; set; }

        public virtual bool CheckValid(out string rs_) {
            if (PopupConf == null) { rs_ = "no conf"; return false; }
            if (PopupRes  == null) { rs_ = "no res"; return false; }
            rs_ = null;
            return true;
        }

        public virtual bool StateValid(PopupType state_) {
            var s = (int)state_;
            return s < 0 || (PopupConf != null && PopupConf.PopupType.Contains(s));
        }

        public virtual bool Ready() => true;

        public virtual bool OpenPopup() {
            ++PopupCount;
            UIManager.Instance.OpenWindow(PopupRes);
            return true;
        }

        public override string ToString() => $"{GetType().Name}|{PopupId}";
    }
}