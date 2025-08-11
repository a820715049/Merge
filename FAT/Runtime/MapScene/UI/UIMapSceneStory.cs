using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using DG.Tweening;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT {
    public class UIMapSceneStory : UIBase {
        public struct Entry {
            public GameObject obj;
            public RectTransform root;
            public RectTransform anchor;
            public CanvasGroup group;
            public Animation anim;
            public string animIn;
            public float animDuration;
            public UIImageRes icon;
            public GameObject typing;
            public GameObject plain;
            public TextMeshProUGUI name;
            public TextMeshProUGUI line;
        }

        public struct EntryDone {
            public GameObject obj;
            public RectTransform root;
            public UITextState text;
        }

        public struct EntryCost {
            public GameObject obj;
            public RectTransform root;
            public Animation anim;
            public BuildCostGroup group;
        }

        public struct AnimateState {
            public PlotStory sConf;
            public int n;
            public Entry cell;
            public float height;
        }

        internal Animator uiAnim;
        internal UIImageRes icon;
        internal TextMeshProUGUI title;
        internal TextMeshProUGUI level;
        internal ScrollRect scroll;
        internal MapButton scrollNext;
        public float spacing;
        public float spacingSame;
        public float spacingLine;
        public float costSpacing;
        public float lineWidthMin;
        public float lineWidthMax;
        internal float lineWidthR;
        internal float lineHeightR;
        internal float lineTopTolerance;
        internal float lineHeightTolerance;
        internal float textWidthMax;
        internal float entryHeight;
        internal float entryDoneHeight;
        internal float costHeight;
        internal List<Entry> entryL = new();
        internal List<Entry> entryR = new();
        internal List<EntryDone> entryDone = new();
        internal EntryCost entryCost;
        internal GameObject tool;
        internal MapButton skip;
        internal MapButton next;

        private bool ready;
        private MapBuilding target;
        private int storyId;
        private int elCount, erCount, edCount;
        private float offset;
        private AnimateState animate;
        private int tweenId;
        private int lastEntry;
        private float lastSpacing;
        private Action WhenClose;

        public float lineDelay = 0.1f;
        public float lineDuration = 0.5f;
        public float lineInterval = 0.8f;

        private void SetupSize() {
            var template = entryL[0];
            entryHeight = template.root.rect.height;
            var rect = template.line.rectTransform.rect;
            var margin = template.line.margin;
            lineWidthR = rect.width;
            lineHeightR = rect.height;
            textWidthMax = lineWidthMax - margin.x - margin.z;
            lineTopTolerance = margin.y - spacingLine;
            lineHeightTolerance =  lineHeightR + (margin.y + margin.w) / 2;
            var templateD = entryDone[0];
            entryDoneHeight = templateD.root.rect.height;
            costHeight = entryCost.root.rect.height;
        }

        public static Entry ParseEntry(GameObject obj_, string anim_) {
            var root = (RectTransform)obj_.transform;
            var anchor = (RectTransform)root.Find("anchor");
            var plain = anchor.Find("plain");
            var anim = root.GetComponent<Animation>();
            var animS = anim[anim_];
            return new() {
                obj = obj_, root = root, anchor = anchor,
                group = plain.GetComponent<CanvasGroup>(),
                anim = anim,
                animIn = anim_,
                animDuration = animS != null ? animS.length : 0,
                icon = anchor.Access<UIImageRes>("icon"),
                name = anchor.Access<TextMeshProUGUI>("icon/name"),
                typing = anchor.Find("typing").gameObject,
                plain = plain.gameObject,
                line = plain.Access<TextMeshProUGUI>("line"),
            };
        }

        public static EntryDone ParseEntryDone(GameObject obj_) {
            var root = (RectTransform)obj_.transform;
            return new() {
                obj = obj_,
                root = root,
                text = root.Access<UITextState>("name"),
            };
        }

        public static EntryCost ParseEntryCost(GameObject obj_) {
            var root = (RectTransform)obj_.transform;
            return new() {
                obj = obj_,
                root = root,
                anim = root.GetComponent<Animation>(),
                group = root.Access<BuildCostGroup>("_cost"),
            };
        }

        private IEnumerator Start() {
            yield return null;
            SetupSize();
            var readyV = ready;
            ready = true;
            if (!readyV && target != null) Refresh(storyId);
        }

        protected override void OnCreate() {
            tweenId = GetHashCode();
            transform.Access(out uiAnim);
            transform.Access("Content", out Transform root);
            root.Access("bg", out Transform bg);
            bg.Access("icon", out icon);
            bg.Access("title", out title);
            bg.Access("level", out level);
            root.Access("scroll", out scroll);
            root.Access("scroll", out scrollNext);
            root = scroll.content;
            var obj = root.Find("entry_l").gameObject;
            entryL.Clear();
            entryL.Add(ParseEntry(obj, obj.name));
            obj = root.TryFind("entry_r");
            entryR.Clear();
            entryR.Add(ParseEntry(obj, obj.name));
            obj = root.TryFind("done");
            entryDone.Clear();
            entryDone.Add(ParseEntryDone(obj));
            obj = root.TryFind("cost");
            entryCost = ParseEntryCost(obj);
            transform.Access("Content/tool", out root);
            tool = root.gameObject;
            root.Access("skip", out skip);
            root.Access("next", out next);
            Action CloseRef = UserClose;
            bg.Access<MapButton>("close").WithClickScale().FixPivot().WhenClick = CloseRef;
            skip.WithClickScale().FixPivot().WhenClick = SkipClick;
            next.WithClickScale().FixPivot().WhenClick = NextClick;
            scrollNext.WhenClick = NextClick;
            entryCost.group.Init(CloseRef);
            // SetupSize();
        }

        protected override void OnParse(params object[] items) {
            target = (MapBuilding)items[0];
            storyId = target.storyPending;
            target.ConfirmStory();
            if (items.Length > 1) WhenClose = items[1] as Action;
        }

        protected override void OnPreOpen() {
            UIUtility.FadeIn(this, uiAnim);
            Refresh(storyId);
        }

        protected override void OnPostClose() {
            StopAnimating();
            AccountMan.TryRate(storyId, target);
            WhenClose?.Invoke();
        }

        private void UserClose() {
            UIUtility.FadeOut(this, uiAnim);
        }

        private void Refresh(int storyId_) {
            if (!ready) return;
            RefreshInfo(target);
            RefreshStory(target, storyId_);
            ToolVisible(false);
            if (storyId_ > 0) {
                Animate(storyId_);
            }
        }

        private void RefreshInfo(MapBuilding target_) {
            icon.SetImage(target_.StoryIcon);
            title.text = I18N.Text(target_.Name);
            level.text = level.text = I18N.FormatText("#SysComDesc18", $"{target_.DisplayLevel}/{target.MaxLevel}");
        }

        private void ToolVisible(bool v_) {
            tool.SetActive(v_);
            scrollNext.enabled = v_;
        }

        private void Resize() {
            scroll.content.sizeDelta = new(0, offset);
            scroll.normalizedPosition = Vector2.zero;
        }

        private void RefreshStory(MapBuilding target_, int pending_) {
            var list = target_.storyList;
            var lCount = list.Count;
            var ddCount = lCount - 1;
            lastEntry = 0;
            (elCount, erCount, edCount) = (0, 0, 0);
            offset = 0f;
            entryCost.obj.SetActive(false);
            for(var n = 0; n < lCount; ++n) {
                var sId = list[n];
                if (sId == pending_) continue;
                var sConf = GetPlotStory(sId);
                foreach(var lId in sConf.IncludeDialog) {
                    var lConf = GetPlotDialog(lId);
                    var e = NextEntry(lConf, ref elCount, ref erCount);
                    RefreshEntry(ref offset, e, lConf);
                }
                if (n < ddCount || target_.Maxed) {
                    var e = NextEntryD(ref edCount);
                    RefreshEntryDone(ref offset, e, n >= ddCount);
                }
                else {
                    RefreshCost(target_, ref offset);
                }
            }
            for (var n = elCount; n < entryL.Count; ++n) {
                var e = entryL[n];
                e.obj.SetActive(false);
            }
            for (var n = erCount; n < entryR.Count; ++n) {
                var e = entryR[n];
                e.obj.SetActive(false);
            }
            for (var n = edCount; n < entryDone.Count; ++n) {
                var e = entryDone[n];
                e.obj.SetActive(false);
            }
            Resize();
        }

        private float RefreshEntry(ref float offset_, Entry e_, PlotDialog lConf_, bool typing_ = false) {
            e_.obj.SetActive(true);
            e_.typing.SetActive(typing_);
            e_.plain.SetActive(!typing_);
            var nConf = GetNpcConfig(lConf_.NpcId);
            e_.icon.SetImage(nConf.DialogImage);
            e_.name.text = I18N.Text(nConf.Name);
            float h;
            if (!typing_) {
                var text = I18N.Text(lConf_.Text);
                var line = e_.line;
                line.text = text;
                var size = line.GetPreferredValues(text);
                float diffX;
                var diffY = 0f;
                if (size.x > lineWidthMax || size.y > lineHeightTolerance) {
                    var size1 = line.GetPreferredValues(text, textWidthMax, float.MaxValue);
                    diffX = lineWidthMax - lineWidthR;
                    diffY = Mathf.Max(0, size1.y - lineHeightR);
                }
                else {
                    diffX = Mathf.Max(lineWidthMin, size.x) - lineWidthR;
                }
                h = entryHeight + diffY;
                var diffH = Mathf.Min(diffY, lineTopTolerance);
                if (diffY > lineTopTolerance) {
                    lastSpacing += spacingLine;
                }
                offset_ += lastSpacing;
                var posX = diffX / 2f;
                if (!lConf_.IsLeft) posX = -posX;
                e_.root.sizeDelta = new(diffX, h);
                e_.root.anchoredPosition = new(posX, -(offset_ - diffH));
                h -= diffH;
            }
            else {
                offset_ += lastSpacing;
                h = entryHeight;
                e_.root.sizeDelta = new(0, h);
                e_.root.anchoredPosition = new(0, -offset);
            }
            offset_ += h;
            return h + lastSpacing;
        }

        private void RefreshEntryDone(ref float offset_, EntryDone e_, bool last_) {
            offset_ += lastSpacing;
            e_.obj.SetActive(true);
            e_.root.anchoredPosition = new(0, -offset_);
            e_.text.Enabled(!last_);
            lastSpacing = spacing;
            offset_ += entryDoneHeight;
        }

        private void RefreshCost(MapBuilding target_, ref float offset_) {
            entryCost.obj.SetActive(true);
            offset_ += costSpacing;
            entryCost.group.Refresh(target_);
            var root = entryCost.root;
            root.anchoredPosition = new(0, -offset_);
            root.localScale = Vector3.one;
            offset_ += costHeight;
        }

        private void Animate(int storyId_) {
            ToolVisible(true);
            animate.sConf = GetPlotStory(storyId_);
            animate.n = 0;
            TryAnimateNext();
        }

        private void TryAnimateNext() {
            TryAnimateTyping();
            TryAnimatePlain();
        }

        private void TryAnimateTyping() {
            var sConf = animate.sConf;
            var n = animate.n;
            if (AnimateEnd(sConf, n)) return;
            AnimateTyping(sConf, n);
        }

        private void AnimateTyping(PlotStory sConf_, int n_) {
            var id = sConf_.IncludeDialog[n_];
            var lConf = GetPlotDialog(id);
            var e = NextEntry(lConf, ref elCount, ref erCount);
            animate.cell = e;
            animate.height = RefreshEntry(ref offset, e, lConf, typing_: true);
            Resize();
            Game.Manager.audioMan.TriggerSound("Dialog");
        }

        private void TryAnimatePlain() {
            var sConf = animate.sConf;
            var n = animate.n;
            if (AnimateEnd(sConf, n)) return;
            AnimatePlain(sConf, n);
            ++animate.n;
        }

        private void AnimatePlain(PlotStory sConf_, int n_) {
            var e = animate.cell;
            var id = sConf_.IncludeDialog[n_];
            var lConf = GetPlotDialog(id);
            offset -= animate.height;
            RefreshEntry(ref offset, e, lConf, typing_: false);
            Resize();
            e.anim.Play(e.animIn);
            var t = e.anchor;
            t.localScale = Vector3.one;
            t.DOScale(Vector3.one, e.animDuration + lineInterval)
                .SetId(tweenId)
                .OnComplete(() => TryAnimateNext());
        }

        private void AnimateCost() {
            entryCost.anim.Play();
        }

        private bool AnimateEnd(PlotStory sConf_, int n_) {
            if (sConf_ == null) return true;
            if (n_ >= sConf_.IncludeDialog.Count) {
                if (target.Maxed) {
                    var e = NextEntryD(ref edCount);
                    RefreshEntryDone(ref offset, e, true);
                }
                else {
                    RefreshCost(target, ref offset);
                }
                Resize();
                AnimateCost();
                ToolVisible(false);
                animate.sConf = null;
                return true;
            }
            return false;
        }

        private Entry NextEntry(PlotDialog lConf, ref int elCount, ref int erCount) {
            Entry NextEntry(List<Entry> list_, ref int count_, int v_) {
                lastSpacing = lastEntry switch {
                    0 => 0,
                    var _ when lastEntry == v_ => spacingSame,
                    _ => spacing,
                };
                lastEntry = v_;
                if (count_ >= list_.Count) AddEntry(list_);
                return list_[count_++];
            }
            static void AddEntry(List<Entry> list_) {
                var template = list_[0].obj;
                var obj = GameObject.Instantiate(template, template.transform.parent);
                list_.Add(ParseEntry(obj, template.name));
            }
            return lConf.IsLeft ? NextEntry(entryL, ref elCount, 1) : NextEntry(entryR, ref erCount, 2);
        }

        private EntryDone NextEntryD(ref int edCount) {
            if (edCount >= entryDone.Count) {
                var template = entryDone[0].obj;
                var obj = GameObject.Instantiate(template, template.transform.parent);
                entryDone.Add(ParseEntryDone(obj));
            }
            lastSpacing = spacing;
            lastEntry = 3;
            return entryDone[edCount++];
        }

        private void StopAnimating() {
            DOTween.Kill(tweenId, complete: false);
            var e = animate.cell;
            if (e.anim == null) return;
            var state = e.anim[e.animIn];
            state.normalizedTime = 1;
            e.icon.transform.localScale = Vector3.one;
            e.typing.transform.localScale = Vector3.one;
            e.plain.transform.localScale = Vector3.one;
        }

        private void SkipClick() {
            StopAnimating();
            Refresh(0);
        }

        private void NextClick() {
            StopAnimating();
            TryAnimateNext();
        }
    }
}