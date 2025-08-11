using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using TMPro;
using EL;
using static fat.conf.Data;
using DG.Tweening;

namespace FAT {
    public class UIHelpDEM : UIBase {
        protected override void OnCreate() {
            transform.Access("Content", out Transform root);
            root.Access("confirm", out MapButton confirm);
            // transform.FindEx<MapButton>("Mask").WhenClick = Close;
            confirm.WithClickScale().FixPivot().WhenClick = ConfirmClick;
        }

        protected override void OnPreOpen() {

        }

        protected override void OnPreClose() {

        }

        public void ConfirmClick() {
            Close();
            var ui = Game.Manager.dailyEvent.OpenTask();
            Game.Manager.screenPopup.Wait(ui);
        }
    }
}