
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
    public class UISevenDayTaskConvert : UIActivityConvert
    {
        private ActivitySevenDayTask activity;
        public override ActivityVisual Visual => activity.visualEndPanel.visual;
        public override bool Complete => true;

        protected override void OnParse(params object[] items)
        {
            activity = (ActivitySevenDayTask)items[0];
            base.OnParse(items);
        }

        public override void Refresh()
        {
            var anyConvert = result.Count > 0;
            convert.gameObject.SetActive(anyConvert);
            var rSize = root.sizeDelta;
            var visual = Visual;
            if (anyConvert)
            {
                var i = 1;
                while (i * 4 < result.Count) { i++; }
                root.sizeDelta = new(rSize.x, 763 + (i - 1) * 210);
                desc.fontSizeMax = 50;
                visual.RefreshText(desc, "subTitle3");
                convert.Refresh(result);
                confirm.text?.Select(0);
            }
            else
            {
                root.sizeDelta = new(rSize.x, size[0]);
                var (s, f) = Complete ? ("subTitle1", 50) : ("subTitle2", 70);
                desc.fontSizeMax = f;
                visual.RefreshText(desc, s);
                confirm.text?.Select(1);
            }
        }
    }
}