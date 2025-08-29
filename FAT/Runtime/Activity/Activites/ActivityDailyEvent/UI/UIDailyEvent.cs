using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;
using EL;
using static fat.conf.Data;
using fat.rawdata;

namespace FAT {
    public class UIDailyEvent : UIBase {
        public class Entry {
            public GameObject obj;
            public GameObject objLock;
            public RectTransform root;
            public TextMeshProUGUI name;
            public UIStateGroup state;
            public MBRewardIcon rewardM;
            public MBRewardIcon reward;
            public MBRewardProgress progress;
            public Image tick;
            public UIVisualGroup visual;
        }
        public struct EntryNext {
            public GameObject obj;
            public RectTransform root;
            public TextMeshProUGUI name;
        }
        public struct EntryTip {
            public GameObject obj;
            public RectTransform root;
            public TextMeshProUGUI tip;
        }

        public Vector2 padding;
        public float spacing;
        public UIVisualGroup visualGroup;
        private float contentX;
        private float contentD;
        private float entryHeight;
        private float entryTipHeight;
        private float entryNextHeight;
        private TextMeshProUGUI cd;
        private TextMeshProUGUI desc1;
        private MBRewardIcon reward;
        private MBRewardProgress progress;
        public UIStateGroup groupR;
        private ScrollRect scroll;
        private readonly List<Entry> list = new();
        private EntryNext entryNext;
        private EntryTip entryTip;
        private MilestoneDE milestone;
        private DailyEvent de;
        private readonly List<DailyEvent.Task> taskList = new();
        private Action WhenTick;
        private Action WhenUpdate;
        private readonly string desc1Key = "#SysComDesc99";

        #if UNITY_EDITOR
        public void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access(out visualGroup);
            transform.Access("Content", out Transform root);
            root.Access("list", out ScrollRect scroll);
            root.Access("list/bg1", out Transform corner);
            visualGroup.Prepare(root.Access<UIImageRes>("bg"), "bgImage");
            visualGroup.Prepare(root.Access<TextProOnACircle>("title1/title"), "mainTitle");
            visualGroup.Prepare(root.Access<TextProOnACircle>("title1"), "mainTitle1");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "subTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_cd/text"), "time");
            visualGroup.Prepare(root.Access<UIImageRes>("_cd/frame"), "time");
            visualGroup.Prepare(scroll.Access<UIImageRes>("bg"), "image1");
            visualGroup.Prepare(corner.Access<UIImageRes>("11"), "mask1");
            visualGroup.Prepare(corner.Access<UIImageRes>("12"), "mask1");
            visualGroup.Prepare(corner.Access<UIImageRes>("21"), "mask2");
            visualGroup.Prepare(corner.Access<UIImageRes>("22"), "mask2");
            visualGroup.Prepare(scroll.Access<UIImageRes>("bar/area/handle"), "image5");
            visualGroup.Prepare(scroll.Access<UIImageRes>("bar"), "image6");
            visualGroup.CollectTrim();
            root = scroll.content;
            root.Access("entry", out UIVisualGroup eVisual);
            root = eVisual.transform;
            root.Access("bg", out UIImageState eBg);
            root.Access("reward/group/back", out UIImageState ePb);
            root.Access("reward/group/text", out UITextState ePt);
            root.Access("name", out UITextState eNm);
            eVisual.Prepare(eBg, "image2", 0);
            eVisual.Prepare(eBg, "image3", 1);
            eVisual.Prepare(ePb, "image4", 0);
            eVisual.Prepare(ePb, "image4", 1);
            eVisual.Prepare(ePt, "bar", 0);
            eVisual.Prepare(ePt, "bar", 1);
            eVisual.Prepare(eNm, "name", 0);
            eVisual.Prepare(eNm, "name", 1);
            eVisual.Prepare(root.Access<UITextState>("rewardM/count"), "task", 0);
            eVisual.Prepare(root.Access<UITextState>("reward/count"), "task", 0);
            eVisual.CollectTrim();
        }
        #endif

        public static Entry ParseEntry(GameObject obj_) {
            var root = (RectTransform)obj_.transform;
            var reward = root.Find("reward");
            return new() {
                obj = obj_, root = root,
                objLock = root.TryFind("lock"),
                name = root.Access<TextMeshProUGUI>("name"),
                state = root.GetComponent<UIStateGroup>(),
                rewardM = root.Access<MBRewardIcon>("rewardM"),
                reward = root.Access<MBRewardIcon>("reward"),
                progress = root.Access<MBRewardProgress>("reward/group", try_:true),
                tick = reward.Access<Image>("tick"),
                visual = root.Access<UIVisualGroup>(),
            };
        }

        public static EntryNext ParseEntryNext(GameObject obj_) {
            var root = (RectTransform)obj_.transform;
            return new() {
                obj = obj_, root = root,
                name = root.Access<TextMeshProUGUI>("name"),
            };
        }

        public static EntryTip ParseEntryTip(GameObject obj_) {
            var root = (RectTransform)obj_.transform;
            return new() {
                obj = obj_, root = root,
                tip = root.Access<TextMeshProUGUI>("text"),
            };
        }

        protected override void OnCreate() {
            transform.Access("Content", out Transform root);
            root.Access("close", out MapButton close);
            root.Access("_cd/text", out cd);
            root.Access("desc1", out desc1);
            root.Access("reward", out reward);
            root.Access("progress", out progress);
            root.Access("progress", out groupR);
            root.Access("list", out scroll);
            var content = scroll.content;
            contentD = content.sizeDelta.x;
            contentX = content.anchoredPosition.x;
            var template = ParseEntry(content.TryFind("entry"));
            entryHeight = template.root.rect.height;
            list.Clear();
            list.Add(template);
            entryNext = ParseEntryNext(content.TryFind("next"));
            entryNextHeight = entryNext.root.rect.height;
            entryTip = ParseEntryTip(content.TryFind("tip"));
            entryTipHeight = entryTip.root.rect.height;
            root.Access("milestone", out milestone);
            // transform.Access<MapButton>("Mask").WhenClick = Close;
            close.WithClickScale().FixPivot().WhenClick = Close;
        }

        protected override void OnPreOpen() {
            Refresh();
            RefreshCD();
            WhenTick ??= RefreshCD;
            WhenUpdate ??= Refresh;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            MessageCenter.Get<MSG.DAILY_EVENT_TASK_UPDATE_ANY>().AddListener(WhenUpdate);
        }

        protected override void OnPreClose() {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            MessageCenter.Get<MSG.DAILY_EVENT_TASK_UPDATE_ANY>().RemoveListener(WhenUpdate);
        }

        public void Refresh() {
            de = Game.Manager.dailyEvent;
            RefreshTheme();
            RefreshGroup();
            RefreshTask();
            RefreshMilestone();
        }

        public void RefreshTheme() {
            var visual = de.ActivityD.VisualTask;
            var vT = new VisualMap(visual.Theme.TextInfo);
            vT.TryReplace("complete", "#SysComDesc99");
            vT.TryCopy("mainTitle", "mainTitle1");
            visual.Refresh(visualGroup);
            foreach(var e in list) {
                visual.Refresh(e.visual);
            }
        }

        public void RefreshCD() {
            var t = UIUtility.CountDownFormat(de.ActivityD.Countdown);
            if (cd.text != t) {
                cd.text = t;
                desc1.text = I18N.FormatText(desc1Key, t);
            }
            milestone.RefreshCD();
        }

        public void RefreshGroup() {
            var r = de.groupReward;
            var valid = r != null && de.GroupValid;
            groupR.Enabled(valid);
            if (valid) {
                reward.Refresh(r);
                progress.Refresh(de.TaskComplete, de.TaskCount);
            }
        }

        public void RefreshTask() {
            static int TaskSort(DailyEvent.Task a_, DailyEvent.Task b_) {
                if (a_.complete != b_.complete) return a_.complete ? 1 : -1;
                return a_.Priority - b_.Priority;
            }
            var vN = de.groupIndex == 0;
            taskList.Clear();
            taskList.AddRange(de.list);
            if (!vN) taskList.AddRange(de.listN);
            taskList.Sort(TaskSort);
            var template = list[0];
            var parent = template.root.parent;
            for (var n = list.Count; n < taskList.Count; ++n) {
                var obj = GameObject.Instantiate(template.obj, parent);
                list.Add(ParseEntry(obj));
            }
            entryTip.obj.SetActive(false);
            var offset = padding.x;
            var next = false;
            var locked = false;
            for (var n = 0; n < taskList.Count; ++n) {
                var task = taskList[n];
                var e = list[n];
                if (!next && task.complete) {
                    RefreshEntryNext(ref offset, vN);
                    next = true;
                }
                var lockedN = de.listN.Contains(task);
                if (!locked && lockedN) {
                    RefreshEntryTip(ref offset, de.listN.Count);
                }
                locked = lockedN;
                RefreshEntry(e, task, ref offset, lockedN);
            }
            if (!next) {
                RefreshEntryNext(ref offset, vN);
            }
            offset += padding.y - spacing;
            var content = scroll.content;
            content.sizeDelta = new(contentD, offset);
            content.anchoredPosition = new(contentX, 0);
            for (var n = taskList.Count; n < list.Count; ++n) {
                list[n].obj.SetActive(false);
            }
        }

        public static void RefreshEntry(Entry e_, DailyEvent.Task task_, bool lock_, bool complete_) {
            var gold = task_.conf.IsGold;
            e_.obj.SetActive(true);
            e_.name.text = task_.Name;
            e_.progress.gameObject.SetActive(!complete_);
            e_.tick.gameObject.SetActive(complete_);
            e_.state.Select((gold, complete_) switch {
                (false, false) => 0,
                (false, true) => 1,
                (true, false) => 2,
                (true, true) => 3,
            });
            e_.objLock.SetActive(lock_);
            if (!complete_) {
                e_.progress.Refresh(task_.value, task_.require);
            }
            e_.reward.Refresh(task_.reward);
            e_.rewardM.Refresh(task_.rewardM);
        }

        public void RefreshEntry(Entry e_, DailyEvent.Task task_, ref float offset, bool lock_) {
            RefreshEntry(e_, task_, lock_, task_.complete);
            e_.root.anchoredPosition = new(0, -offset);
            offset += entryHeight + spacing;
        }

        public void RefreshEntryTip(ref float offset, int c_) {
            entryTip.obj.SetActive(true);
            entryTip.tip.text = I18N.FormatText("#SysComDesc519", c_);
            entryTip.root.anchoredPosition = new(0, -offset);
            offset += entryTipHeight + spacing;
        }

        public void RefreshEntryNext(ref float offset, bool v_) {
            var valid = de.NextGroupValid && v_;
            entryNext.obj.SetActive(valid);
            if (!valid) {
                offset += spacing;
                return;
            }
            entryNext.name.text = I18N.FormatText("#SysComDesc103", de.TaskCount - de.TaskComplete);
            entryNext.root.anchoredPosition = new(0, -offset);
            offset += entryNextHeight + spacing;
        }

        public void RefreshMilestone() {
            milestone.Refresh();
        }
    }
}