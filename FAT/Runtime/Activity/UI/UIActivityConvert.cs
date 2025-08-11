using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using static EL.PoolMapping;

namespace FAT {
    public abstract class UIActivityConvert : UIBase {
        internal RectTransform root;
        internal TextMeshProUGUI desc;
        internal MBRewardLayout convert;
        internal MapButton confirm;
        public float[] size;
        public UIVisualGroup visualGroup;

        internal Ref<List<RewardCommitData>> list;
        internal MBRewardLayout.CommitList result;
        public abstract ActivityVisual Visual { get; }
        public abstract bool Complete { get;}

        public void OnValidate() {
            if (Application.isPlaying) return;
            visualGroup = transform.GetComponent<UIVisualGroup>();
            transform.Access("Content/root", out Transform root);
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("title"), "mainTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "subTitle");
            visualGroup.Prepare(root.Access<UIImageRes>("bg1"), "bg");
            visualGroup.CollectTrim();
        }

        protected override void OnCreate() {
            transform.Access("Content/root", out root);
            root.Access("desc", out desc);
            root.Access("_group", out convert);
            root.Access("confirm", out confirm);
            var template = convert.list[0];
            template.objRef = new[] { template.transform.Access<UIImageRes>("frame") };
            Action CloseRef = ConfirmClick;
            // root.Access<MapButton>("close").WithClickScale().WhenClick = CloseRef;
            confirm.WithClickScale().WhenClick = CloseRef;
        }

        protected override void OnParse(params object[] items) {
            list = (Ref<List<RewardCommitData>>)items[1];
            result = new() { list = list.obj };
        }

        protected override void OnPreOpen() {
            RefreshTheme();
            Refresh();
        }

        protected override void OnPreClose() {
            list.Free();
            result.list = null;
        }

        public virtual void RefreshTheme() {
            var visual = Visual;
            visual.Refresh(visualGroup);
            foreach(var e in convert.list) {
                visual.Refresh((UIImageRes)e.objRef[0], "bg1");
                visual.Refresh(e.count, "icon");
            }
        }

        public void Refresh() {
            var anyConvert = result.Count > 0;
            convert.gameObject.SetActive(anyConvert);
            var rSize = root.sizeDelta;
            var visual = Visual;
            if (anyConvert) {
                root.sizeDelta = new(rSize.x, size[0]);
                desc.fontSizeMax = 50;
                visual.RefreshText(desc, "subTitle3");
                convert.Refresh(result);
                confirm.text.Select(0);
            }
            else {
                root.sizeDelta = new(rSize.x, size[1]);
                var (s, f) = Complete ? ("subTitle1", 50) : ("subTitle2", 70);
                desc.fontSizeMax = f;
                visual.RefreshText(desc, s);
                confirm.text.Select(1);
            }
        }

        internal void ConfirmClick() {
            for(var k = 0; k < result.list.Count; ++k) {
                var d = result.list[k];
                var n = convert.list[k];
                UIFlyUtility.FlyReward(d, n.icon.transform.position);
            }
            Close();
        }
    }
}