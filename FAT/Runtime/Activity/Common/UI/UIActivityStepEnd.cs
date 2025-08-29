using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT {
    public class UIActivityStepEnd : UIActivityConvert {
        private ActivityStep activity;
        public override ActivityVisual Visual => activity.VisualEnd.visual;
        public override bool Complete => activity.Complete;

        protected override void OnParse(params object[] items) {
            activity = (ActivityStep)items[0];
            base.OnParse(items);
        }
    }
}