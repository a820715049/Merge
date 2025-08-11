using System;
using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIMiniBoardReward : UIBase
    {
        public MapButton mask;
        public RectTransform root;
        public TextMeshProUGUI title;
        public MBRewardLayout convert;
        public MapButton confirm;
        public float[] size;

        private MBRewardLayout.CommitList result;

        public void OnValidate()
        {
            if (Application.isPlaying) return;
            mask = transform.FindEx<MapButton>("Mask");
            root = (RectTransform)transform.Find("Content/root");
            title = root.FindEx<TextMeshProUGUI>("title");
            convert = root.FindEx<MBRewardLayout>("_group");
            confirm = root.FindEx<MapButton>("confirm");
        }

        protected override void OnCreate()
        {
            Action CloseRef = ConfirmClick;
            confirm.WithClickScale().WhenClick = CloseRef;
        }

        protected override void OnParse(params object[] items)
        {
            var list = (List<RewardCommitData>)items[1];
            result = new MBRewardLayout.CommitList { list = list };
        }

        protected override void OnPreOpen()
        {
            Refresh();
        }

        protected override void OnPreClose()
        {
            ObjectPool<List<RewardCommitData>>.GlobalPool.Free(result.list);
            result.list = null;
        }

        public void Refresh()
        {
            var anyConvert = result.Count > 0;
            convert.gameObject.SetActive(anyConvert);
            var rSize = root.sizeDelta;
            if (anyConvert)
            {
                if (result.Count <= 4)
                    root.sizeDelta = new Vector2(rSize.x, size[0]);
                else
                    root.sizeDelta = new Vector2(rSize.x, size[2]);
                convert.Refresh(result);
            }
            else
            {
                root.sizeDelta = new Vector2(rSize.x, size[1]);
            }
            convert.RefreshActive(result.Count);
            var multi = result.Count > convert.active.col;
            convert.alignX = multi ? MBRewardLayout.AlignmentX.Left : MBRewardLayout.AlignmentX.Center;
            convert.RefreshList(result);
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