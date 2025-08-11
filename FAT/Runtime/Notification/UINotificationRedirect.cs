using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using DG.Tweening;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT {
    public class UINotificationRedirect : UIBase {
        protected override void OnCreate() {
            transform.Access("Content/Panel", out Transform root);
            // transform.AddButton("Mask", Close);
            root.AddButton("close", Close).FixPivot();
            root.Access<MapButton>("confirm").WithClickScale().FixPivot().WhenClick = ConfirmClick;
        }

        private void ConfirmClick() {
            Game.Manager.notification.RedirectToSetting();
        }
    }
}