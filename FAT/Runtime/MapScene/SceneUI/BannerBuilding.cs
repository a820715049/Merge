using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;
using uTools;
using fat.rawdata;

namespace FAT {
    public class BannerBuilding : MapSceneUI {
        private UIImageState imgFrame;
        private GameObject groupLock;
        private GameObject groupNormal;
        private ButtonScaleFAT buttonScale;
        private UIImageRes imgIcon;
        private UIImageRes imgIconChar;
        private UIImageRes imgIconRes;
        private Image bar;
        private UITextState barText;

        public void Init(MapBuildingInfo target_) {
            var root = transform.Find("root");
            root.Access(out MapButton button);
            root.Access(out buttonScale);
            root.Access("frame", out imgFrame);
            groupLock = root.TryFind("lock");
            groupNormal = root.TryFind("normal");
            root.Access("text", out barText);
            root = groupNormal.transform;
            root.Access("icon", out imgIcon);
            root.Access("icon_char", out imgIconChar);
            root.Access("icon_res", out imgIconRes);
            root.Access("progress", out bar);
            button.WithClickScale().FixPivot().WhenClick = () => {
                var t = (MapBuilding)target_.buildingRef;
                if (t.Locked) return;
                Game.Manager.mapSceneMan.Select(t);
            };
        }

        public void Activate(MapBuilding target_, float delay_ = 0) {
            var visible = !target_.Maxed && target_.VisibleCheck;
            Visible(visible);
            if (!visible) return;
            Open(delay_);
        }

        public void Refresh(MapBuilding target_) {
            if (!Active) return;
            var locked = target_.Locked;
            groupLock.SetActive(locked);
            groupNormal.SetActive(!locked);
            buttonScale.enabled = locked;
            if (target_.Locked) {
                RefreshLocked(target_);
                return;
            }
            RefreshByCost(target_);
        }

        public void RefreshLocked(MapBuilding target_) {
            imgFrame.Setup(2);
            barText.Setup(2);
            barText.Text = I18N.FormatText("#SysComDesc18", target_.UnlockLevel);
        }

        public void RefreshByCost(MapBuilding target_) {
            var objMan = Game.Manager.objectMan;
            var cConf = target_.confCost;
            var p = target_.CostPercent;
            var p1 = target_.CoinPercent;
            var resIcon = cConf.ShowResIcon;
            if (resIcon > 0) {
                var bConf = objMan.GetBasicConfig(resIcon);
                imgIconRes.SetImage(bConf.Image);
            }
            imgIconRes.gameObject.SetActive(p < 1 && resIcon > 0);
            var pStr = (p1, p) switch {
                (< 1, _) => $"{(int)(p1 * 100)}%",
                (_, < 1) => "99%",
                _ => null,//unused
            };
            RefreshProgress(p, p1, pStr);
            var bannerIcon = cConf.BannerIcon.ConvertToAssetConfig();
            var (show, hide) = bannerIcon.Asset.StartsWith("r_s_") switch {
                true => (imgIconChar, imgIcon),
                false => (imgIcon, imgIconChar)
            };
            show.SetImage(bannerIcon);
            hide.Hide();
        }

        private void RefreshProgress(float p, float p1, string text_) {
            bar.fillAmount = p1;
            bar.enabled = p < 1;
            if (p >= 1) barText.Text = I18N.Text("#SysComDesc19");
            else barText.Text = text_;
            var k = p >= 1 ? 1 : 0;
            imgFrame.Setup(k);
            barText.Setup(k);
        }
    }
}