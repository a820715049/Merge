using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;
using EL;
using static fat.conf.Data;

namespace FAT {
    public class MilestoneDE : RewardBar {
        internal TextMeshProUGUI cd;

        internal override void Awake() {
            base.Awake();
            transform.Access("cd/text", out cd);
        }

        public void Refresh() {
            var de = Game.Manager.dailyEvent;
            var valid = de.MilestoneValid && de.MilestoneUnlocked;
            gameObject.SetActive(valid);
            if (!valid) return;
            var list = de.listM;
            var value = de.valueM;
            var next = de.MilestoneNext(value);
            var iConf = GetObjBasic(de.milestone.RequireCoinId);
            icon.SetImage(iConf.Icon);
            RefreshList(list, value, next);
            RefreshCD();
        }

        public void RefreshCD() {
            var de = Game.Manager.dailyEvent;
            string text;
            if (de.MilestoneComplete(de.valueM)) {
                text = I18N.Text("#SysComDesc733");
            }
            else {
                var v = de.ActivityM.Countdown;
                text = UIUtility.CountDownFormat(v);
                text = I18N.FormatText("#SysComDesc105", text);
            }
            cd.text = text;
            cd.rectTransform.sizeDelta = cd.GetPreferredValues();
        }
    }
}