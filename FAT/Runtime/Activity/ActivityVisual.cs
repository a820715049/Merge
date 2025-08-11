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
using Config;
using EL.Resource;

namespace FAT {
    public class ActivityVisual : IAssetDependent {
        public EventTheme Theme { get; set; }
        public Entrance Entrance { get; set; }
        public Popup Popup { get; set; }
        public bool Valid => Theme != null;
        public int PopupId => Theme != null ? Theme.PopupId : 0;
        public string TargetAsset => Theme?.BasePrefab;
        public string EntryIcon => Theme?.EntranceImage;
        public bool EntryVisible => Entrance != null;
        public bool EntryOnLeft => Entrance.IsLeft;
        public int Priority => Entrance != null ? Entrance.Weight : 0;
        public VisualMap AssetMap => new(Theme?.AssetInfo);
        public VisualMap TextMap => new(Theme?.TextInfo);
        public VisualMap StyleMap => new(Theme?.StyleInfo);

        public bool Setup(int id_, UIResAlt ui_ = null) {
            Theme = GetEventTheme(id_);
            if (Theme == null) {
                if (id_ > 0) DebugEx.Error($"{nameof(Activity)} failed to find theme with id {id_}");
                return false;
            }
            Entrance = GetEntrance(Theme.EntranceId);
            if (ui_ != null)
            {
                Popup = GetPopup(Theme.PopupId);
                ui_.Replace(TargetAsset);
            }
            return true;
        }

        public void Clear() {
            Theme = null;
            Entrance = null;
            Popup = null;
        }

        public IEnumerable<(string, AssetTag)> ResEnumerate() {
            if (Theme == null) {
                DebugEx.Warning($"asset_dep info not ready:{nameof(Theme)}");
                yield break;
            }
            yield return (Theme.BasePrefab, AssetTag.Required);
            yield return (Theme.EntranceImage, AssetTag.Required);
            foreach(var (_, r) in Theme.AssetInfo) yield return (r, AssetTag.Required);
        }

        public void Refresh(UIVisualGroup group_) {
            if (group_ == null) return;
            var list = group_.list;
            for (var k = 0; k < list.Count; ++k) {
                var n = list[k];
                RefreshC(n.target, n.key, n.index);
            }
        }

        public void RefreshC(Component target_, string key_, int index_) {
            switch (target_) {
                case UIImageRes ir: Refresh(ir, key_); break;
                case UITextState ts: Refresh(ts, key_, index_); break;
                case UIImageState ims: Refresh(ims, key_, index_); break;
                case TextProOnACircle tc: Refresh(tc, key_); break;
                case TMP_Text tt: Refresh(tt, key_); break;
                case Graphic gp: Refresh(gp, key_); break;
            };
        }

        public void Refresh(Graphic target_, string key_) {
            if (!Valid || target_ == null || !Theme.AssetInfo.TryGetValue(key_, out var v)
                || !ColorUtility.TryParseHtmlString(v, out var c)) return;
            target_.color = c;
        }

        public void Refresh(UIImageRes target_, string key_) {
            if (!Valid || target_ == null || !Theme.AssetInfo.TryGetValue(key_, out var v)) return;
            target_.SetImage(v);
        }

        public void Refresh(UIImageState target_, string key_, int index_) {
            if (!Valid || target_ == null || !Theme.AssetInfo.TryGetValue(key_, out var v)
                || index_ >= target_.state.Length) return;
            target_.state[index_].key = v;
        }

        public void Refresh(UITextState target_, string key_, int index_) {
            if (!Valid || target_ == null || index_ >= target_.state.Length) return;
            if (Theme.TextInfo.TryGetValue(key_, out var v)) {
                target_.state[index_].text = v;
            }
            if (Theme.StyleInfo.TryGetValue(key_, out var s)) {
                if (TryGetMaterial(target_.text, s, out var mat)) {
                    target_.state[index_].style = mat.index;
                }
                else if (TryGetColor(target_.text, key_, s, out var c)) {
                    target_.state[index_].color = c;
                }
            }
        }
        public void Refresh(TextProOnACircle target_, string key_) {
            if (target_ == null) return;
            RefreshStyle(target_.m_TextComponent, key_);
            RefreshText(target_.m_TextComponent, key_, target_);
        }
        public void Refresh(TMP_Text target_, string key_) {
            RefreshStyle(target_, key_);
            RefreshText(target_, key_);
        }

        public void RefreshText(UIVisualGroup g_, string k_, params object[] v_)
        {
            if (!g_.TryAccess(k_, out var v) || (v.target is not TMP_Text && v.target is not TextProOnACircle)
            || !Theme.TextInfo.TryGetValue(k_, out var c)) return;
            if (v.target is TextProOnACircle text) text.SetText(I18N.FormatText(c, v_));
            else if (v.target is TMP_Text t) t.text = I18N.FormatText(c, v_);
        }

        public void RefreshText(TMP_Text target_, string key_, TextProOnACircle text_ = null) {
            if (!Valid || target_ == null || !Theme.TextInfo.TryGetValue(key_, out var t)) return;
            t = t.StartsWith('#') ? I18N.Text(t) : t;
            if (text_ != null) text_.SetText(t);
            else target_.SetText(t);
        }

        public void RefreshStyle(TMP_Text target_, string key_) {
            if (!Valid || target_ == null || !Theme.StyleInfo.TryGetValue(key_, out var s)) return;
            if (TryGetColor(target_, key_, s, out var c)) {
                FontMaterialRes.Instance.GetFontMatResConf(0)?.ApplyFontMatResConfig(target_);
                target_.color = c;
                return;
            }
            if (TryGetMaterial(target_, s, out var mat)) {
                mat.ApplyFontMatResConfig(target_);
                return;
            }
            DebugEx.Warning($"invalid style format {key_}:{s}");
        }

        public bool TryGetMaterial(TMP_Text target_, string s_, out FontMatResConfig mat_) {
            if (int.TryParse(s_, out var k)) {
                mat_ = FontMaterialRes.Instance.GetFontMatResConf(k);
                return mat_ != null;
            }
            mat_ = null;
            return false;
        }

        public bool TryGetColor(TMP_Text target_, string key_, string s_, out Color c_) {
            if (s_.StartsWith('#') && ColorUtility.TryParseHtmlString(s_, out c_)) {
                return true;
            }
            if (s_.Length == 6 && ColorUtility.TryParseHtmlString($"#{s_}", out c_)) {
                // DebugEx.Warning($"color format lacks # for {key_}:{s_}");
                return true;
            }
            c_ = default;
            return false;
        }
    }
}