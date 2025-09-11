/**
 * @Author: zhangpengjian
 * @Date: 2025/8/7 18:13:14
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/8/7 18:13:14
 * Description: 拼图活动过期奖励兑换界面
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System.Collections;
using static EL.PoolMapping;

namespace FAT
{
    public class UIActivityPuzzleConvert : UIBase
    {
        public MapButton mask;
        public RectTransform root;
        public TextMeshProUGUI title;
        public TextMeshProUGUI desc;
        public MBRewardLayout convert;
        public MapButton confirm;
        public float[] size;

        private ActivityPuzzle activity;
        private MBRewardLayout.CommitList result;
        internal Ref<List<RewardCommitData>> list;

        public void OnValidate()
        {
            if (Application.isPlaying) return;
            mask = transform.FindEx<MapButton>("Mask");
            root = (RectTransform)transform.Find("Content/root");
            title = root.FindEx<TextMeshProUGUI>("title");
            desc = root.FindEx<TextMeshProUGUI>("desc");
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
            activity = (ActivityPuzzle)items[0];
            list = (Ref<List<RewardCommitData>>)items[1];
            result = new() { list = list.obj };
        }

        protected override void OnPreOpen()
        {
            Refresh();
        }

        protected override void OnPreClose()
        {
            list.Free();
            result.list = null;
        }

        private void Refresh()
        {
            var anyConvert = result.Count > 0;
            convert.gameObject.SetActive(anyConvert);
            var rSize = root.sizeDelta;
            if (anyConvert)
            {
                root.sizeDelta = new(rSize.x, size[0]);
                convert.Refresh(result);
                confirm.text.Select(0);
            }
            else
            {
                root.sizeDelta = new(rSize.x, size[1]);
                confirm.text.Select(1);
            }
        }

        private void ConfirmClick()
        {
            for (var k = 0; k < result.list.Count; ++k)
            {
                var d = result.list[k];
                var n = convert.list[k];
                UIFlyUtility.FlyReward(d, n.icon.transform.position);
            }
            Close();
        }

        private IEnumerator CoDelayLeave()
        {
            yield return new WaitForSeconds(1.5f);
            UIDiggingUtility.LeaveActivity();
        }
    }
}