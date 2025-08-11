using System.Collections.Generic;
using UnityEngine.UI;
using EL;
using fat.rawdata;
using Config;
using EL.Resource;

namespace FAT {
    public class UIResAlt {
        public UIResource RefR;
        public UIResource ActiveR;

        public UIResAlt(UIResource ui_) {
            RefR = ui_;
            ActiveR = ui_;
        }

        public void Replace(string v_) {
            if (string.IsNullOrEmpty(v_)) return;
            Replace(v_.ConvertToAssetConfig());
        }
        public void Replace(AssetConfig v_) {
            if (v_ == null || v_.Group == null || v_.Asset == null || ActiveR.Match(v_)) return;
            if (ReferenceEquals(ActiveR, RefR)) {
                ActiveR = new(v_.Asset, v_.Group, RefR);
            }
            else {
                ActiveR.Replace(v_);
            }
        }
    }

    public readonly ref struct VisualMap {
        public readonly IDictionary<string, string> target;
        public static Dictionary<string, string> dummy;

        public VisualMap(IDictionary<string, string> target_) {
            target = target_ ?? ( dummy ??= new());
        }

        public static VisualMap Access(IDictionary<string, string> target_) => new(target_);

        public bool TryGetValue(string k_, out string v_) => target.TryGetValue(k_, out v_);

        public void TryReplace(string k_, string v_) {
            if (target.ContainsKey(k_)) return;
            target[k_] = v_;
        }

        public void Replace(string k_, string v_) {
            target[k_] = v_;
        }

        public void TryCopy(string k_, string k1_) {
            if (!target.TryGetValue(k_, out var v) || target.ContainsKey(k1_)) return;
            target[k1_] = v;
        }
    }

    public readonly struct VisualRes {
        public readonly UIResAlt res;
        public readonly ActivityVisual visual;

        public VisualRes(UIResource ui_) {
            res = new(ui_);
            visual = new();
        }

        public void Setup(int theme_) {
            if (visual == null) {
                DebugEx.Error($"uninitialized {nameof(VisualRes)}");
                return;
            }
            visual.Setup(theme_, res);
        }

        public void Refresh(UIVisualGroup group_) => visual.Refresh(group_);

        public IEnumerable<(string, AssetTag)> ResEnumerate() => visual.ResEnumerate();

        public void Open(ActivityLike acti_) {
            UIManager.Instance.OpenWindow(res.ActiveR, acti_);
        }
    }

    public readonly struct VisualPopup {
        public readonly UIResAlt res;
        public readonly ActivityVisual visual;
        public readonly PopupActivity popup;

        public VisualPopup(UIResource ui_) {
            res = new(ui_);
            visual = new();
            popup = new();
        }

        public void Setup(int theme_, ActivityLike acti_, bool check_ = false, bool active_ = true) {
            if (visual == null) {
                DebugEx.Error($"uninitialized {nameof(VisualPopup)}");
                return;
            }
            if (visual.Setup(theme_, res)) {
                popup.Setup(acti_, visual, res, check_:check_, active_:active_);
            }
        }

        public void Refresh(UIVisualGroup group_) => visual.Refresh(group_);

        public IEnumerable<(string, AssetTag)> ResEnumerate() => visual.ResEnumerate();

        public void Popup(ScreenPopup popup_, PopupType state_, int limit_ = 0, object custom_ = null) {
            if (limit_ > 0 && popup.PopupCount > limit_ - 1) return;
            popup_.TryQueue(popup, state_, custom_);
        }

        public void Popup(ScreenPopup popup_, int limit_ = 0, object custom_ = null) {
            Popup(popup_, popup_.PopupNone, limit_, custom_);
        }

        public void Popup(int limit_ = 0, object custom_ = null) {
            var popup = Game.Manager.screenPopup;
            Popup(popup, popup.PopupNone, limit_, custom_);
        }

        public void Open(ActivityLike acti_) {
            UIManager.Instance.OpenWindow(res.ActiveR, acti_);
        }
    }
}