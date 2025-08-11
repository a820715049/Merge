using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT {
    public class UIActivityGuessEnd : UIActivityConvert {
        private ActivityGuess activity;
        public override ActivityVisual Visual => activity.VisualEnd.visual;
        public override bool Complete => true;

        protected override void OnParse(params object[] items) {
            activity = (ActivityGuess)items[0];
            base.OnParse(items);
        }
    }
}