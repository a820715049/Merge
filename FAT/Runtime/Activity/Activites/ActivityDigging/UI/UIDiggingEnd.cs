/**
 * @Author: zhangpengjian
 * @Date: 2024/8/19 10:30:31
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/19 10:30:31
 * Description: 挖沙活动结束界面 兑换奖励
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
    public class UIDiggingEnd : UIBase
    {
        public MapButton mask;
        public RectTransform root;
        public TextMeshProUGUI title;
        public UITextState desc;
        public MBRewardLayout convert;
        public MapButton confirm;
        public float[] size;

        private ActivityDigging activity;
        private MBRewardLayout.CommitList result;
        internal Ref<List<RewardCommitData>> list;

        public void OnValidate()
        {
            if (Application.isPlaying) return;
            mask = transform.FindEx<MapButton>("Mask");
            root = (RectTransform)transform.Find("Content/root");
            title = root.FindEx<TextMeshProUGUI>("title");
            desc = root.FindEx<UITextState>("desc");
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
            activity = (ActivityDigging)items[0];
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
            title.text = I18N.Text(activity.diggingConfig.Name);
            activity.VisualEnd.Refresh(title, "mainTitle");
            var anyConvert = result.Count > 0;
            convert.gameObject.SetActive(anyConvert);
            var rSize = root.sizeDelta;
            if (anyConvert)
            {
                root.sizeDelta = new(rSize.x, size[0]);
                desc.text.fontSizeMax = 50;
                convert.Refresh(result);
                desc.Select(1);
                var c = Game.Manager.objectMan.GetTokenConfig(activity.diggingConfig.TokenId);
                desc.text.SetText(I18N.FormatText("#SysComDesc761", c.SpriteName));
                confirm.text.Select(0);
            }
            else
            {
                root.sizeDelta = new(rSize.x, size[1]);
                desc.Select(0);
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
            if (UIManager.Instance.IsShow(activity.Res.ActiveR))
            {
                if (result.list.Count > 0)
                {
                    (UIManager.Instance.TryGetUI(UIConfig.UICommonShowRes) as UICommonShowRes)?.ShowOtherRes();
                    Game.Instance.StartCoroutineGlobal(CoDelayLeave());
                }
                else
                {
                    UIDiggingUtility.LeaveActivity();
                }
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