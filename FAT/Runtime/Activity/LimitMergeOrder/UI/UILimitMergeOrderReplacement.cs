
using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static EL.PoolMapping;

namespace FAT
{
    public class UILimitMergeOrderReplacement : UIBase
    {

        private Ref<List<RewardCommitData>> list;
        private MBRewardLayout.CommitList _result;
        private MBRewardLayout _convert;
        public float[] size;
        private RectTransform _root;


        protected override void OnCreate()
        {
            base.OnCreate();
            // Object绑定
            transform.Access("Content/root/_group", out _convert);
            transform.Access("Content/root", out _root);
            transform.AddButton("Content/root/confirm", ConfirmClick);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 2) return;
            list = (Ref<List<RewardCommitData>>)items[1];
            _result = new() { list = list.obj };
        }

        protected override void OnPreOpen()
        {
            Refresh();
        }

        public void Refresh()
        {
            var anyConvert = _result.Count > 0;
            _convert.gameObject.SetActive(anyConvert);
            var rSize = _root.sizeDelta;
            if (anyConvert)
            {
                if (_result.Count <= 4)
                    _root.sizeDelta = new Vector2(rSize.x, size[0]);
                else
                    _root.sizeDelta = new Vector2(rSize.x, size[2]);
                _convert.Refresh(_result);
            }
            else
            {
                _root.sizeDelta = new Vector2(rSize.x, size[1]);
            }
            _convert.RefreshActive(_result.Count);
            var multi = _result.Count > _convert.active.col;
            _convert.alignX = multi ? MBRewardLayout.AlignmentX.Left : MBRewardLayout.AlignmentX.Center;
            _convert.RefreshList(_result);
        }

        internal void ConfirmClick()
        {
            for (var k = 0; k < _result.list.Count; ++k)
            {
                var d = _result.list[k];
                var n = _convert.list[k];
                UIFlyUtility.FlyReward(d, n.icon.transform.position);
            }

            Close();
        }

        protected override void OnPostClose()
        {
            ObjectPool<List<RewardCommitData>>.GlobalPool.Free(_result.list);
            _result.list = null;
        }
    }
}
