using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using FAT.MSG;
using DG.Tweening;

namespace FAT {
    using static MessageCenter;

    public class UIActivityStep : UIBase {
        public struct TrackNode {
            public GameObject obj;
            public RectTransform root;
            public UIStateGroup state;
            public UIImageState bg;
            public UIColorGroup color;
            public TextMeshProUGUI badge;
            public MBRewardLayout item;
            public GameObject arrow;
            public GameObject tick;
            public MBRewardLayout reward;
            public Animation anim;
            public GameObject effect1;
            public GameObject effect2;
            public GameObject effect3;
            public GameObject effect4;
        }

        public float nodeTransitDelay = 1.0f;
        public float nodeLockDelay = 0.15f;
        public float progressDuration = 0.3f;
        public float scrollDuration = 0.3f;
        public string animComplete = "node_complete";
        public string animUnlock = "node_unlock";
        internal TextMeshProUGUI cd;
        internal MBRewardIcon preview;
        internal HelpInfo infoPreviewN;
        internal MBRewardLayout rewardPreviewN;
        internal HelpInfo infoPreviewD;
        internal MBRewardLayout rewardPreviewD;
        internal MBRewardProgress progress;
        internal ScrollRect scroll;
        internal List<TrackNode> list;
        internal MapButton confirm;
        internal GameObject objTipBoost;
        internal HelpInfo info;
        internal GameObject bgDecorate;
        internal TextMeshProUGUI textDecorate;
        internal UIImageState help3;
        public Vector2 padding = new(20, 10);
        public float spacing = 2;
        internal float size;
        internal float halfView;
        public float contentSize;
        internal float offsetMin;
        internal float relocatePos;
        public UIVisualGroup visualGroup;
        
        private ActivityStep activity;
        private bool decorateMode;
        private Action<ActivityLike, bool> WhenEnd;
        private Action WhenTick;
        private bool inBoard;
        private bool animate;
        private HelpInfo infoPreview;
        private MBRewardLayout rewardPreview;
        private Func<int, object, bool> InfoPreviewRef;

        public void OnValidate() {
            if (Application.isPlaying) return;
            visualGroup = transform.GetComponent<UIVisualGroup>();
            transform.Access("Content", out Transform root);
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("title"), "mainTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "subTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("tip_boost/text"), "tip1");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("tip_bottom"), "tip2");
            visualGroup.Prepare(root.Access<UIImageRes>("_bg_decorate/b"), "decorateSale");
            visualGroup.CollectTrim();
        }

        public static TrackNode ParseNode(Transform root_) {
            return new() {
                obj = root_.gameObject,
                root = (RectTransform)root_,
                state = root_.Access<UIStateGroup>(),
                bg = root_.Access<UIImageState>("bg"),
                color = root_.Access<UIColorGroup>(),
                badge = root_.Access<TextMeshProUGUI>("badge/text"),
                item = root_.Access<MBRewardLayout>("item"),
                arrow = root_.TryFind("arrow"),
                tick = root_.TryFind("tick"),
                reward = root_.Access<MBRewardLayout>("reward"),
                anim = root_.Access<Animation>(),
                effect1 = root_.TryFind("effect_complete"),
                effect2 = root_.TryFind("effect_unlock"),
                effect3 = root_.TryFind("effect_01"),
            };
        }

        protected override void OnCreate() {
            transform.Access("Content", out Transform root);
            root.Access("_cd/text", out cd);
            root.Access("_track_progress", out preview);
            root.Access("_info_preview", out infoPreviewN);
            root.Access("_info_preview/group", out rewardPreviewN);
            root.Access("_info_preview_d", out infoPreviewD);
            root.Access("_info_preview_d/group", out rewardPreviewD);
            root.Access("_track_progress/group", out progress);
            root.Access("track", out scroll);
            var node = ParseNode(scroll.content.Find("_track_node"));
            var nodeT = node.item.list[0];
            var rLock = nodeT.transform.Find("lock");
            nodeT.objRef = new Component[] { rLock.Access<Animation>() };
            list = new() { node };
            root.Access("confirm", out confirm);
            objTipBoost = root.TryFind("tip_boost");
            root.Access("_info", out info);
            bgDecorate = root.TryFind("_bg_decorate");
            root.Access("_bg_decorate/text", out textDecorate);
            root.Access("_info/group/root/_tip_entry3/icon", out help3);
            size = node.root.rect.size.y;
            halfView = scroll.viewport.rect.size.y / 2;
            Action CloseRef = Close;
            transform.Access<MapButton>("Mask").WhenClick = CloseRef;
            root.Access<MapButton>("close").WithClickScale().FixPivot().WhenClick = CloseRef;
            confirm.WithClickScale().FixPivot().WhenClick = ConfirmClick;
            WhenEnd ??= RefreshEnd;
            WhenTick ??= RefreshCD;
        }

        protected override void OnParse(params object[] items) {
            activity = (ActivityStep)items[0];
            decorateMode = activity.DecorateScore > 0;
        }

        protected override void OnPreOpen() {
            inBoard = UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain);
            RefreshTheme();
            RefreshInfo();
            RefreshPreview();
            RefreshTrack();
            RefreshCD();
            RelocateTrack();
            TryAnimate();
            Get<ACTIVITY_END>().AddListener(WhenEnd);
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnPreClose() {
            Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            activity.VisualIndex = activity.TaskIndex;
            if (animate && activity.Complete) activity.TryComplete();
            animate = false;
            StopAllCoroutines();
        }

        public void RefreshTheme() {
            var visual = activity.Visual;
            visual.Refresh(visualGroup);
        }

        public void RefreshInfo() {
            objTipBoost.SetActive(inBoard);
            confirm.gameObject.SetActive(!inBoard);
            info.Hide();
            bgDecorate.SetActive(decorateMode);
            textDecorate.text = I18N.FormatText("#SysComDesc423", activity.DecorateScore);
            help3.Select(decorateMode ? 1 : 0);
        }

        public void RefreshPreview() {
            infoPreviewN.gameObject.SetActive(false);
            infoPreviewD.gameObject.SetActive(false);
            (infoPreview, rewardPreview) = decorateMode
                ? (infoPreviewD, rewardPreviewD)
                : (infoPreviewN, rewardPreviewN);
            infoPreview.gameObject.SetActive(true);
            InfoPreviewRef ??= InfoPreview;
            progress.Refresh(activity.VisualIndex, activity.list.Count);
            preview.Refresh(activity.rewardIcon, InfoPreviewRef);
            infoPreview.RefreshActive();
            rewardPreview.Refresh(activity.rewardM);
        }

        public void RefreshTrack() {
            var tList = activity.list;
            var vIndex = activity.VisualIndex;
            var tIndex = activity.TaskIndex;
            var nCount = list.Count;
            var tCount = tList.Count;
            var template = list[0].obj;
            for (var k = nCount; k < tCount; ++k) {
                var obj = GameObject.Instantiate(template);
                obj.transform.SetParent(template.transform.parent, false);
                list.Add(ParseNode(obj.transform));
            }
            var p = padding.y;
            for (var k = 0; k < tCount; ++k) {
                var node = list[k];
                var task = tList[k];
                node.obj.SetActive(true);
                node.effect1?.SetActive(false);
                node.effect2?.SetActive(false);
                node.effect3?.SetActive(false);
                node.root.anchoredPosition = new(0, p);
                var s = RefreshNode(node, task, k, vIndex, tIndex);
                if (s == 1) relocatePos = p + size / 2;
                p += size + spacing;
            }
            p += padding.x - spacing;
            contentSize = p;
            offsetMin = halfView * 2 - contentSize;
            scroll.content.sizeDelta = new(0, p);
            for (var k = tCount; k < nCount; ++k) {
                list[k].obj.SetActive(false);
            }
        }

        public int RefreshNode(TrackNode node, ActivityStep.Task task, int k, int vIndex, int tIndex) {
            var (s, lA, aA, tA) = (k - vIndex) switch {
                < 0 => (0, false, false, true),
                0 => (1, false, true, false),
                > 0 => (2, true, true, false),
            };
            node.arrow.SetActive(aA);
            node.tick.SetActive(tA);
            node.badge.text = $"{k + 1}";
            node.item.Refresh(task.item, delay_: lA);
            ObjRefActive(node.item, 0, lA);
            node.reward.Refresh(task.reward);
            node.bg.Select(k % node.bg.state.Length);
            node.state.Collect();
            node.state.Select(s);
            node.color?.Collect();
            node.color?.Select(tA ? 0 : -1);
            return s;
        }

        public void ObjRefActive(MBRewardLayout l_, int k_, bool v_) {
            for (var n = 0; n < l_.count; ++n) {
                var obj = l_.list[n].objRef[k_].gameObject;
                obj.SetActive(v_);
                obj.transform.localScale = Vector3.one;
            }
        }

        public void ObjRefAnimate(MBRewardLayout l_, int k_, string name_) {
            for (var n = 0; n < l_.count; ++n) {
                var obj = (Animation)l_.list[n].objRef[k_];
                obj.Play(name_);
            }
        }
        
        public void RefreshCD() {
            var t = Game.TimestampNow();
            var diff = (long)Mathf.Max(0, activity.endTS - t);
            UIUtility.CountDownFormat(cd, diff);
        }

        public void RelocateTrack() {
            var o = Mathf.Clamp(-(relocatePos - halfView), offsetMin, 0);
            var sPos = new Vector2(0, o);
            scroll.content.anchoredPosition = sPos;
        }

        private float RelocatePos(int n_) {
            relocatePos = padding.y + n_ * (size + spacing) + size / 2;
            return Mathf.Clamp(-(relocatePos - halfView), offsetMin, 0);
        }

        public void TryAnimate() {
            if (activity.VisualIndex >= activity.TaskIndex) return;
            IEnumerator Animate() {
                next:
                var v = activity.VisualIndex;
                var count = activity.list.Count;
                if (v >= count) {
                    Close();
                    yield break;
                }
                var dL = nodeLockDelay;
                var waitL = new WaitForSeconds(dL);
                var dN = nodeTransitDelay;
                var waitN = new WaitForSeconds(dN);
                var node = list[v];
                var task = activity.list[v];
                node.color?.MixTo(0, 0);
                node.anim.Play(animComplete);
                var d = node.anim[animComplete].length;
                node.root.SetAsLastSibling();
                yield return waitN;
                var n = v + 1;
                RefreshNode(node, task, v, n, n);
                yield return new WaitForSeconds(d - dN);
                if (n >= count) {
                    Close();
                    yield break;
                }
                scroll.content.DOAnchorPosY(RelocatePos(n), scrollDuration);
                node = list[n];
                task = activity.list[n];
                node.anim.Play(animUnlock);
                d = node.anim[animUnlock].length;
                node.root.SetAsLastSibling();
                yield return waitL;
                ObjRefAnimate(node.item, 0, "unlock");
                yield return new WaitForSeconds(d - dL);
                RefreshNode(node, task, n, n, n);
                d = progressDuration;
                progress.Refresh(n, activity.list.Count, d);
                yield return new WaitForSeconds(d);
                if (++activity.VisualIndex < activity.TaskIndex) {
                    goto next;
                }
            }
            animate = true;
            StartCoroutine(Animate());
        }

        internal void RefreshEnd(ActivityLike acti_, bool expire_) {
            if (acti_ != activity || !expire_) return;
            Close();
        }

        internal void ConfirmClick() {
            Close();
            if (!inBoard) GameProcedure.SceneToMerge();
        }

        internal bool InfoPreview(int id, object _) {
            infoPreview.Active();
            return true;
        }
    }
}