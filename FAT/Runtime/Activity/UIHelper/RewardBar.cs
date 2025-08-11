using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;
using EL;
using static fat.conf.Data;

namespace FAT {
    public abstract class RewardBar : MonoBehaviour {
        public struct NodeInfo {
            public Config.RewardConfig reward;
            public int value;
            public float pos;
            public bool complete;
            public bool effect;
        }

        public struct NodeOption {
            public int value;
            public float pos;
            public float range;
        }

        public class Node {
            public GameObject obj;
            public RectTransform root;
            public MBRewardIcon icon;
            public GameObject objBg;
            public TextMeshProUGUI value;
            public GameObject tick;
            public GameObject effect;
            public UIColorGroup color;
        }
        
        internal MBRewardProgress bar;
        internal TextMeshProUGUI count;
        internal float nodeY;
        internal readonly List<Node> list = new();
        internal UIImageRes icon;
        public Vector2 padding;
        internal int active;
        internal int next;
        internal float value;
        internal float target;
        internal float speed;
        internal float preV, preP;
        public bool Stable => value >= target;
        public bool Pause { get; set; }
        public bool Maxed => next >= active;

        internal virtual void Awake() {
            var root = transform.Find("list");
            root.Access(out bar);
            root.Access("icon", out icon);
            root.Access("value", out count);
            var template = ParseNode(root.TryFind("group/node"));
            nodeY = template.root.anchoredPosition.y;
            list.Clear();
            list.Add(template);
        }

        public static Node ParseNode(GameObject obj_) {
            var root = (RectTransform)obj_.transform;
            return new() {
                obj = obj_, root = root,
                icon = root.Access<MBRewardIcon>("icon"),
                objBg = root.Find("bg").gameObject,
                value = root.Access<TextMeshProUGUI>("value"),
                tick = root.TryFind("icon/tick"),
                effect = root.TryFind("icon/effect"),
                color = root.Access<UIColorGroup>(try_:true),
            };
        }

        public static int Next(IList<NodeInfo> list_, int v_) {
            var ret = list_.Count;
            for (var n = 0; n < list_.Count; ++n) {
                var node = list_[n];
                var ready = v_ >= node.value;
                if (!ready) {
                    ret = n;
                    break;
                }
            }
            return ret;
        }

        public static bool Complete(IList<NodeInfo> list_, int v_) {
            if (list_.Count == 0) return true;
            return list_[^1].value <= v_;
        }

        public (float, float) SizeInfo(IList<NodeInfo> list_, NodeOption option_) {
            var range = option_.range > 0 ? option_.range : (list_[^1].pos - option_.pos);
            return (bar.sizeX - padding.x - padding.y, range);
        }

        public void RefreshList(IList<NodeInfo> list_, float value_, int next_, NodeOption option_ = default) {
            active = list_.Count;
            value = value_;
            target = value_;
            next = next_;
            bar.Init();
            var (sizeX, range) = SizeInfo(list_, option_);
            count.text = $"{value_}";
            var template = list[0];
            var parent = template.root.parent;
            for (var n = list.Count; n < list_.Count; ++n) {
                var obj = GameObject.Instantiate(template.obj, parent);
                list.Add(ParseNode(obj));
            }
            for (var n = 0; n < list_.Count; ++n) {
                var r = list_[n];
                var e = list[n];
                e.obj.SetActive(true);
                e.root.anchoredPosition = new(sizeX * r.pos / range, nodeY);
                e.objBg.SetActive(n < list_.Count - 1);
                RefreshNode(e, r);
            }
            Resize(list_, value_, next_, option_);
            for (var n = list_.Count; n < list.Count; ++n) {
                list[n].obj.SetActive(false);
            }
        }

        public void Resize(IList<NodeInfo> list_, float value_, int next_, NodeOption option_, bool refresh_ = true) {
            if (next_ >= list_.Count) {
                bar.RefreshSize(bar.sizeX);
                return;
            }
            if (refresh_) {
                var prev = next_ - 1;
                (preV, preP) = prev >= 0 ? (list_[prev].value, list_[prev].pos) : (option_.value, option_.pos);
            }
            var (sizeX, range) = SizeInfo(list_, option_);
            var node = list_[next_];
            var p = (value_ - preV) / (node.value - preV)  * (node.pos - preP) / range + preP / range;
            var barSize = padding.x + sizeX * p;
            bar.RefreshSize(barSize);
        }

        public void RefreshNode(Node e_, NodeInfo r_) => RefreshNode(e_, r_.value, r_.reward, r_.complete, r_.effect);
        public void RefreshNode(Node e_, int v_, Config.RewardConfig reward_, bool complete_, bool effect_) {
            e_.value.text = $"{v_}";
            e_.icon.count.gameObject.SetActive(!complete_);
            e_.tick.SetActive(complete_);
            e_.effect.SetActive(!complete_ && effect_);
            e_.icon.Refresh(reward_);
            e_.color?.Revert();
            if (!complete_) {
                e_.icon.custom = e_;
            }
        }

        public void Target(float target_, float speed_ = 80) {
            target = Mathf.Max(target, target_);
            speed = speed_;
        }

        public bool TargetStep(IList<NodeInfo> list_, float delta_, bool stopAtNext_ = false, NodeOption option_ = default) {
            if (Stable || Pause) return false;
            value += delta_ * speed;
            if (value > target) value = target;
            if (Maxed) return false;
            var n = list_[next];
            var nV = n.value;
            var pass = value >= nV;
            if (pass) {
                n.complete = true;
                RefreshNode(list[next], n);
                ++next;
                if (stopAtNext_) {
                    value = nV;
                    goto end;
                }
            }
            end:
            Resize(list_, value, next, option_, refresh_:false);
            return pass;
        }
    }
}