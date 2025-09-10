using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using static fat.conf.Data;
using fat.rawdata;
using FAT.MSG;

namespace FAT
{
    public class UIScoreConvert_mic : UIActivityConvert {
        private ActivityScoreMic activity;
        public override ActivityVisual Visual => activity.Visual;
        // public override ActivityVisual Visual => activity.ReContinueVisual;
        public override bool Complete => true;

        protected override void OnParse(params object[] items) {
            activity = (ActivityScoreMic)items[0];
            base.OnParse(items);
        }
    }
}