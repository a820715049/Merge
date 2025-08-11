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
    public class UIStorySwitchNotice : UIBase {
        protected override void OnCreate() {
            transform.AddButton("Content/Panel/close", Close).FixPivot();
            transform.AddButton("Content/Panel/confirm", ConfirmClick).FixPivot();
        }

        private void ConfirmClick() {
            Close();
            UIManager.Instance.OpenWindow(UIConfig.UISetting);
        }
    }
}