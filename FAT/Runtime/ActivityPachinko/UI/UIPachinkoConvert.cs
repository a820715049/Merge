using System;
using System.Collections.Generic;
using Cysharp.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class UIPachinkoConvert : UIBase
    {
        internal RectTransform root;
        internal TextMeshProUGUI desc;
        internal MBRewardLayout convert;
        internal MapButton confirm;
        public TextMeshProUGUI title;
        public float[] size;
        public UIVisualGroup visualGroup;
        public ActivityPachinko Activity;

        internal MBRewardLayout.CommitList result;
        public ActivityVisual Visual => Activity.ConvertVisual;
        public bool Complete { get; }

        public void OnValidate()
        {
            if (Application.isPlaying) return;
            visualGroup = transform.GetComponent<UIVisualGroup>();
            transform.Access("Content/root", out Transform root);
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("title"), "mainTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("subTitle"), "subTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "desc");
            visualGroup.Prepare(root.Access<UIImageRes>("bg1"), "bg");
            visualGroup.Prepare(root.Access<UIImageRes>("titleBg"), "titleBg");
            visualGroup.CollectTrim();
        }

        protected override void OnCreate()
        {
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

        protected override void OnParse(params object[] items)
        {
            Activity = items[0] as ActivityPachinko;
            var list = (PoolMapping.Ref<List<RewardCommitData>>)items[1];
            result = new MBRewardLayout.CommitList { list = list.obj };
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
            Refresh();
        }

        protected override void OnPreClose()
        {
            ObjectPool<List<RewardCommitData>>.GlobalPool.Free(result.list);
            result.list = null;
        }

        public virtual void RefreshTheme()
        {
            var visual = Visual;
            visual.Refresh(visualGroup);
            foreach (var e in convert.list)
            {
                visual.Refresh((UIImageRes)e.objRef[0], "bg2");
                visual.Refresh(e.count, "icon");
            }

            title.SetText(I18N.Text(Activity.Conf.Name));
        }

        public void Refresh()
        {
            var anyConvert = result.Count > 0;
            convert.gameObject.SetActive(anyConvert);
            var rSize = root.sizeDelta;
            var visual = Visual;
            if (anyConvert)
            {
                root.sizeDelta = new Vector2(rSize.x, size[0]);
                desc.fontSizeMax = 50;
                desc.SetTextFormat(I18N.Text("#SysComDesc728"),
                    ZString.Format("<sprite name=\"{0}\">",
                        Game.Manager.objectMan.GetTokenConfig(Activity.Conf.TokenId).SpriteName));
                convert.Refresh(result);
                confirm.text.Select(0);
            }
            else
            {
                root.sizeDelta = new Vector2(rSize.x, size[1]);
                visual.RefreshText(desc, "desc2");
                confirm.text.Select(1);
            }
        }

        internal void ConfirmClick()
        {
            for (var k = 0; k < result.list.Count; ++k)
            {
                var d = result.list[k];
                var n = convert.list[k];
                UIFlyUtility.FlyReward(d, n.icon.transform.position);
            }

            Close();
        }
    }
}