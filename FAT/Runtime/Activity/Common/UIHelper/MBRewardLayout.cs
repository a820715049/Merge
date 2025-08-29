using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Config;
using UnityEngine.UI;

namespace FAT {
    public class MBRewardLayout : MonoBehaviour {
        public interface IListAccess {
            (int, int) this[int n] { get; }
            int Count { get; }
        }

        public struct ReverseAccess : IListAccess {
            public IListAccess list;
            public readonly (int, int) this[int n] => list[^(n + 1)];
            public readonly int Count => list.Count;
        }

        public struct RewardList : IListAccess {
            public IList<RewardConfig> list;
            public readonly (int, int) this[int n] {
                get {
                    var r = list[n];
                    return (r.Id, r.Count);
                }
            }
            public readonly int Count => list.Count;
        }

        public struct CommitList : IListAccess {
            public IList<RewardCommitData> list;
            public readonly (int, int) this[int n] {
                get {
                    var r = list[n];
                    return (r.rewardId, r.rewardCount);
                }
            }
            public readonly int Count => list.Count;
        }

        public struct KVList : IListAccess {
            public IList<KeyValuePair<int, int>> list;
            public readonly (int, int) this[int n] {
                get {
                    var r = list[n];
                    return (r.Key, r.Value);
                }
            }
            public readonly int Count => list.Count;
        }

        public struct TupleList : IListAccess {
            public IList<(int, int)> list;
            public readonly (int, int) this[int n] => list[n];
            public readonly int Count => list.Count;
        }

        [Serializable]
        public struct LayoutInfo {
            #if UNITY_EDITOR
            public string name;
            #endif
            public Vector2 area;
            public Vector2 center;
            public Vector2 spacing;
            public Vector2 pos;
            public bool sizeOverride;
            public Vector2 size;
            public int col;
            public int row;
            public int count;
        }

        public enum AlignmentX {
            Left, Center, Right,
        }
        public enum AlignmentY {
            Top, Middle, Bottom,
        }

        public List<LayoutInfo> layout = new();
        public List<LayoutInfo> layoutN = new();
        public RectTransform area;
        public RectTransform anchor;
        public List<MBRewardIcon> list = new();
        public Vector2 size;
        public Vector2 offset;
        public bool reverse;
        internal LayoutInfo active;
        internal int count;
        internal string templateName;
        public AlignmentX alignX = AlignmentX.Center;
        public AlignmentY alignY = AlignmentY.Middle;

#if UNITY_EDITOR
        private void OnValidate() {
            if (Application.isPlaying) return;
            static LayoutInfo V(LayoutInfo v) {
                var row = Mathf.CeilToInt((float)v.count / v.col);
                var size = v.sizeOverride ? $" {v.size.x}x{v.size.y}" : string.Empty;
                v.row = row;
                v.name = $"{v.count}:{v.col}x{row}{size}";
                return v;
            }
            for(var n = 0; n < layout.Count; ++n) {
                layout[n] = V(layout[n]);
            }
            for(var n = 0; n < layoutN.Count; ++n) {
                layoutN[n] = V(layoutN[n]);
            }
            var root = transform;
            area = (RectTransform)root;
            if (root.Access("anchor", out anchor, try_:true)) root = anchor;
            list.Clear();
            root.GetComponentsInChildren(list);
            if (list.Count > 0) {
                var rt = (RectTransform)list[0].transform;
                size = rt.rect.size;
                var pivotB = alignX switch {
                    AlignmentX.Right => new Vector2(1, 1),
                    _ => new Vector2(0, 1),
                };
                offset = (rt.pivot - pivotB) * size;
            }
        }
#endif

        public void Refresh(IList<RewardConfig> list_, bool delay_ = false)
            => Refresh(new RewardList { list = list_ }, delay_);
        public void Refresh(IList<RewardCommitData> list_, bool delay_ = false)
            => Refresh(new CommitList { list = list_ }, delay_);

        public void Refresh(IListAccess list_, bool delay_ = false) {
            RefreshActive(list_.Count);
            RefreshList(list_, delay_);
        }

        public void RefreshEmpty(int n_) {
            RefreshActive(n_);
            foreach (var e in list) {
                e.gameObject.SetActive(false);
            }
        }

        public void RefreshList(IListAccess list_, bool delay_ = false, Action<MBRewardIcon, (int, int), bool> Refresh_ = null) {
            count = list_.Count;
            var template = list[0].gameObject;
            for (var n = list.Count; n < count; ++n) {
                var obj = GameObject.Instantiate(template, template.transform.parent);
                var icon = obj.GetComponent<MBRewardIcon>();
                list.Add(icon);
            }
            for (var n = count; n < list.Count; ++n) {
                list[n].gameObject.SetActive(false);
            }
            static float Offset(int n_, float s_, float p_)
                => (n_ * (s_ + p_) - p_) * 0.5f;
            static int Dir(int a_)
                => a_ == 2 ? -1 : 1;
            var row = Math.Max(active.row, count / active.col);
            var sizeA = active.sizeOverride ? active.size : size;
            var oP = active.center + offset;
            var oY = alignY switch {
                AlignmentY.Top => active.area.y * 0.5f,
                AlignmentY.Bottom => 0,
                _ => Offset(row, sizeA.y, active.spacing.y),
            } + oP.y;
            var sizeX1 = (sizeA.x + active.spacing.x) * Dir((int)alignX);
            var sizeY1 = sizeA.y + active.spacing.y * Dir((int)alignY);
            if (reverse) list_ = new ReverseAccess { list = list_ };
            for (var j = 0; j < row; ++j) {
                var colO = j * active.col;
                var colV = Mathf.Min(active.col, count - colO);
                var oX = alignX switch {
                    AlignmentX.Left => -active.area.x * 0.5f,
                    AlignmentX.Right => active.area.x * 0.5f,
                    _ => -Offset(colV, sizeA.x, active.spacing.x),
                } + oP.x;
                for (var i = 0; i < colV; ++i) {
                    var n = colO + i;
                    var r = list_[n];
                    var e = list[n];
                    e.gameObject.SetActive(true);
                    e.Resize(sizeA);
                    if (Refresh_ != null) {
                        Refresh_(e, r, delay_);
                    }
                    else if (delay_) e.RefreshEmpty();
                    else e.Refresh(r);
                    var root = (RectTransform)e.transform;
                    root.anchoredPosition = new(
                        oX + sizeX1 * i,
                        oY - sizeY1 * j);
                }
            }
        }

        public void RefreshFrame(int state)
        {
            foreach (var icon in list)
            {
                var imgState = icon.transform.GetChild(0).GetComponent<UIImageState>();
                if(imgState != null) imgState.Select(state);
            }
        }

        private void RefreshArea() {
            area.sizeDelta = active.area;
            if (anchor != null) anchor.anchoredPosition = active.pos;
        }

        public void RefreshActiveN(int count_) => RefreshActive(count_, layoutN);
        public void RefreshActive(int count_) => RefreshActive(count_, layout);
        public void RefreshActive(int count_, List<LayoutInfo> list_) {
            var match = false;
            foreach (var l in list_) {
                if (count_ <= l.count) {
                    match = true;
                    active = l;
                    break;
                }
            }
            if (!match) active = list_[^1];
            RefreshArea();
        }

        public void SelectActive(LayoutInfo layout_) {
            active = layout_;
            RefreshArea();
        }

        public void HideIcon() {
            for (var n = 0; n < count; ++n) {
                list[n].Hide();
            }
        }

        public void RenameByIndex() {
            templateName ??= list[0].name;
            for (var k = 0; k < list.Count; ++k) {
                var e = list[k];
                e.name = $"{templateName}{k}";
            }
        }
    }
}