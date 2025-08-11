using System;
using System.Collections.Generic;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT {
    public class PopupOOE : IScreenPopup {
        public override int PopupWeight => int.MaxValue;
        public override int PopupLimit => -1;

        public PopupOOE() {
            PopupId = 10000;
            PopupRes = UIConfig.UIOutOfEnergy;
        }

        public override bool CheckValid(out string _) {
            _ = null;
            return true;
        }
    }
}