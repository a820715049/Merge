using System;
using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using fat.rawdata;

namespace FAT {
    public class MetaPopup {
        public bool Notify() {
            var gConf = Game.Manager.configMan.globalConfig;
            var level = Game.Manager.mergeLevelMan.displayLevel;
            var popup = Game.Manager.screenPopup;
            var activity = Game.Manager.activity;
            var trigger = Game.Manager.activityTrigger;
            var ret = false;
            // 升级流程完成时 如果预告入口正在显示，那么需要尝试强制打开一次集卡预告tips
            if (gConf.LvPopupCardAlbum == level && UIManager.Instance.TryGetCache(UIConfig.UISceneHud, out var ui) && ui is UISceneHud uiHud) {
                ret = ret || uiHud.TryOpenCardActivityTips();
            }
            if (level == gConf.LvPopupNewSession
                && trigger.info.TryGetValue(gConf.LvPopupNewSessionId, out var infoT)
                && activity.LookupAny(infoT.conf.EventType, infoT.conf.EventParam, out var acti)) {
                acti.TryPopup(popup, popup.PopupNone);
                popup.Check();
                ret = true;
            }
            if (gConf.LvPopupDE == level && Game.Manager.dailyEvent.Valid) {
                Game.Manager.dailyEvent.OpenTask();
                ret = true;
            }
            if (gConf.LvPopupCloseDialog == level && SettingManager.Instance.PlotIsOn) {
                UIManager.Instance.OpenWindow(UIConfig.UIStorySwitchNotice);
                ret = true;
            }
            return ret;
        }
    }
}