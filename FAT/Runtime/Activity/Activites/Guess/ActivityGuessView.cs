using System.Collections.Generic;
using UnityEngine;
using EL;
using UnityEngine.UI;
using System;
using TMPro;
using FAT.MSG;
using fat.rawdata;

namespace FAT {
    using static MessageCenter;

    public class GuessAnswerEntry {
        public UIImageState slot;
        public MapButton button;
        public RectTransform anchor;
        public UIImageState card;
        public UIRectState state;
        public UIStateGroup match;
        public Animator anim;
        public GameObject effect;
        public List<MapButton> record = new();

        public GuessAnswerEntry(Transform root_) {
            root_.Access("slot", out slot);
            root_.Access("_card", out anchor);
            root_.Access("_card", out button);
            root_.Access("_card", out card);
            root_.Access("state", out state);
            root_.Access("state", out match);
            root_.Access("state", out anim);
            effect = root_.TryFind("Effect03");
            record.Clear();
            root_.Find("record").GetComponentsInChildren(record);
        }

        public void AlignRecord(int count_) {
            EnsureRecord(count_);
            HideRecord(count_);
        }

        public void EnsureRecord(int count_) {
            for (var k = record.Count; k < count_; ++k) {
                AddRecord();
            }
        }

        public void HideRecord(int from_, int to_ = 0) {
            if (to_ == 0) to_ = record.Count;
            for (var k = from_; k < to_; ++k) {
                record[k].image.Select(0);
            }
        }

        public MapButton AddRecord() {
            var template = record[0];
            var obj = GameObject.Instantiate(template.gameObject, template.transform.parent);
            var b = obj.GetComponent<MapButton>();
            record.Add(b);
            return b;
        }
    }

    public class GuessHandEntry {
        public Transform root;
        public RectTransform anchor;
        public MapButton button;
        public UIImageState card;

        public GuessHandEntry(Transform root_) {
            root = root_;
            root_.Access("card", out anchor);
            anchor.Access("_card", out button);
            anchor.Access("_card", out card);
        }
    }

    public struct AnswerAccess : MBRewardLayout.IListAccess {
        public List<GuessAnswer> list;

        public readonly (int, int) this[int n_] => (n_, 0);
        public readonly int Count => list.Count;
    }
}