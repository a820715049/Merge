/**
 * @Author: zhangpengjian
 * @Date: 2025/7/10 16:17:40
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/7/10 16:17:40
 * Description: 沙堡里程碑活动过期奖励兑换界面
 */

using UnityEngine;
using System.Collections.Generic;
using EL;
using TMPro;
using static EL.PoolMapping;

namespace FAT
{
    public class UIActivityCastleConvert : UIBase
    {
        // Image字段
        private UIImageRes _bg1;
        private UIImageRes _TitleBg;
        private UIImageRes _frame;

        // Text字段
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _desc;
        private TextMeshProUGUI _count;

        private Ref<List<RewardCommitData>> list;
        private MBRewardLayout.CommitList _result;
        private MBRewardLayout _convert;
        public float[] size;
        private RectTransform _root;


        protected override void OnCreate()
        {
            base.OnCreate();
            // Image绑定
            transform.Access("Content/root/bg1_img", out _bg1);
            transform.Access("Content/root/bg1_img/TitleBg_img", out _TitleBg);
            transform.Access("Content/root/_group/entry/frame_img", out _frame);
            // Text绑定
            transform.Access("Content/root/title_txt", out _title);
            transform.Access("Content/root/desc_txt", out _desc);
            // Object绑定
            transform.Access("Content/root/_group", out _convert);
            transform.Access("Content/root", out _root);
            transform.AddButton("Content/root/confirm", ConfirmClick);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 2) 
            {
                return;
            }
            var activity = items[0] as ActivityCastle;
            if (activity == null) 
            {
                return;
            }
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