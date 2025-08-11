using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using fat.rawdata;

namespace FAT {
    public class UINotificationRemind : UIBase
    {
        internal TextMeshProUGUI cd;
        private long ts;
        private OpenNotifiPopType type;

        protected override void OnCreate() {
            transform.Access("Content/Panel", out Transform root);
            root.Access("cd", out cd);
            // transform.AddButton("Mask", Close);
            root.AddButton("close", Close).FixPivot();
            root.Access<MapButton>("confirm").WithClickScale().FixPivot().WhenClick = ConfirmClick;
        }

        protected override void OnParse(params object[] items)
        {
            type = (OpenNotifiPopType)items[0];
        }

        protected override void OnPreOpen() {
            DataTracker.notification_popup.Track(type.ToString());
            var t = Game.TimestampNow();
            ts = t + Game.Manager.mergeEnergyMan.FullRecoverCD();
            RefreshCD();
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnPreClose() {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        private void RefreshCD() {
            var t = Game.TimestampNow();
            UIUtility.CountDownFormat(cd, (long)Mathf.Max(0, ts - t));
        }

        private void ConfirmClick() {
            Close();
            DataTracker.notification_button.Track(type.ToString());
            Game.Manager.notification.TrySetEnabledWithNotify(true);
        }
    }
}