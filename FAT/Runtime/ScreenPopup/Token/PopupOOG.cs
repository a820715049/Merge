using System;
using System.Collections.Generic;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT {
    public class PopupOOG : IScreenPopup {
        public override int PopupWeight => 0;
        public override int PopupLimit => -1;

        public PopupOOG() {
            PopupId = 10001;
            PopupRes = UIConfig.UIShop;
        }

        public override bool CheckValid(out string rs_) {
            if(!Game.Manager.shopMan.CheckShopTabIsUnlock(ShopTabType.Gem)) {
                rs_ = "tab locked";
                return false;
            }
            rs_ = null;
            return true;
        }

        public override bool OpenPopup() {
            if (!Game.Manager.screenPopup.CheckState(PopupState)) return false;
            Game.Manager.shopMan.TryOpenUIShop(ShopTabType.Gem);
            return true;
        }
    }
}