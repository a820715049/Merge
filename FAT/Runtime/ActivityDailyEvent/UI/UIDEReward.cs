using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using TMPro;
using EL;
using static fat.conf.Data;
using DG.Tweening;

namespace FAT {
    public class UIDEReward : UIBase {
        private RectTransform rectRef;
        private RectTransform borderRef;
        private RectTransform de;
        private NoticeDE notice;
        private ProgressDEM dem;
        private MBBoardFly fly;

        protected override void OnCreate() {
            transform.Access("Content", out Transform root);
            root.Access("_NoticeDE", out notice);
            root.Access("EntryDE", out de);
            root.Access("BoardFly/ScoreBoardFly", out fly);
            fly.Setup();
            if (!UIManager.Instance.TryGetCache(UIConfig.UIMergeBoardMain, out var c) || c is not UIMergeBoardMain ui) return;
            ui.Access("Adapter/Root/CompOrder/SV", out borderRef);
            borderRef.Access("Viewport/Content/Misc/DEAndRewardRoot/_EntryDE", out rectRef);
            rectRef.Access("_ProgressDEM", out dem);
            notice.rectRef = rectRef;
            notice.borderRef = borderRef;
            notice.fly = fly;
            dem.fly = fly;
        }

        protected override void OnPreOpen() {
            MessageCenter.Get<MSG.MapState>().AddListener(Refresh);
        }

        protected override void OnPreClose() {
            MessageCenter.Get<MSG.MapState>().RemoveListener(Refresh);
        }

        private void Refresh(bool v_) {
            if (v_) {
                dem.transform.SetParent(de, false);
                dem.borderRef = null;
                var posR = rectRef.position;
                var pos = de.position;
                pos.y = posR.y;
                de.position = pos;
            }
            else {
                dem.transform.SetParent(rectRef, false);
                dem.transform.SetAsFirstSibling();
                dem.borderRef = borderRef;
            }
            dem.transform.localPosition = dem.posL;
        }
    }
}