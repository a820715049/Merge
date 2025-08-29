using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using static fat.conf.Data;
using fat.rawdata;

namespace FAT {
    public class UIEnergyBoxTips : UITipsBase {
        public RectTransform anchor;
        public TextMeshProUGUI title;
        public TextMeshProUGUI title1;
        public TextMeshProUGUI desc;
        private ObjBasic conf;

        internal virtual void OnValidate() {
            if (Application.isPlaying) return;
            var root = transform.Find("Content");
            anchor = (RectTransform)root.Find("anchor");
            root = anchor;
            title = root.FindEx<TextMeshProUGUI>("title");
            title1 = root.FindEx<TextMeshProUGUI>("title1");
            desc = root.FindEx<TextMeshProUGUI>("desc");
        }

        protected override void OnCreate() {
            // transform.FindEx<MapButton>("mask").WhenClick = Close;
        }

        protected override void OnParse(object[] item) {
            _SetTipsPosInfo(item[1], item[2]);
            conf = (ObjBasic)item[0];
        }
        
        protected override void OnPreOpen() {
            Refresh();
            _RefreshTipsPos(16, needFitWidth:false);
        }

        public void Refresh() {
            var g = Game.Manager.configMan.globalConfig;
            if (!g.TapSourceTips.TryGetValue(conf.Id, out var c)) {
                DebugEx.Warning($"tips for {conf.Id} not found");
            }
            var name = I18N.Text(conf.Name);
            title.text = name;
            title1.text = name;
            desc.text = I18N.FormatText("#SysComDesc427", c);
        }
    }
}