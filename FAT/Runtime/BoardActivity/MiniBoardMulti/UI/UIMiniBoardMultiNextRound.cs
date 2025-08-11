using System;
using System.Collections.Generic;
using System.Linq;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIMiniBoardMultiNextRound : UIBase
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
            transform.AddButton("Content/root/close", Close);
        }

        protected override void OnParse(params object[] items)
        {
            var dic = items[0] as Dictionary<int, int>;
            if (dic == null) return;
            var list = Enumerable.ToList(dic.Select(x => new RewardCommitData(x.Key, null, null)
            {
                rewardId = x.Key,
                rewardCount = x.Value
            }));
            result = new MBRewardLayout.CommitList { list = list };

            if (Game.Manager.miniBoardMultiMan.World?.nextRewardItem != null)
            {
                var id = Game.Manager.miniBoardMultiMan.World.nextRewardItem.tid;
                result.list.Add(new RewardCommitData(id, null, null)
                {
                    rewardId = Game.Manager.miniBoardMultiMan.World.nextRewardItem.tid,
                    rewardCount = -1
                });
            }
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
            MessageCenter.Get<MSG.UI_MINI_BOARD_MULTI_COLLECT>().Dispatch();
            Close();
        }
    }
}