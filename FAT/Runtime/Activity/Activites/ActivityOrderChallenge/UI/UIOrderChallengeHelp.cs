/**
 * @Author: zhangpengjian
 * @Date: 2024/10/30 16:23:32
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/30 16:23:32
 * Description: 连续限时订单帮助界面
 */

using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIOrderChallengeHelp : UIBase
    {
        public UIVisualGroup visualGroup;
        private ActivityOrderChallenge activityOrderChallenge;
        private TextMeshProUGUI desc;
        [SerializeField] private UIImageRes rewardIcon;
        [SerializeField] private TextMeshProUGUI rewardCount;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Application.isPlaying) return;
            transform.Access(out visualGroup);
            transform.Access("Content", out Transform root);
            visualGroup.Prepare(root.Access<TextProOnACircle>("page1/titleRoot/title1"), "mainTitle");
            visualGroup.Prepare(root.Access<TMP_Text>("page1/text"), "entry1");
            visualGroup.Prepare(root.Access<TMP_Text>("page1/text2"), "entry2");
            visualGroup.Prepare(root.Access<TMP_Text>("page1/text3"), "entry3");
            visualGroup.CollectTrim();
        }
#endif
        protected override void OnCreate()
        {
            transform.AddButton("Mask", OnClose).FixPivot();
            transform.Access("Content/page1/tip (1)", out desc);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                activityOrderChallenge = (ActivityOrderChallenge)items[0];
            }
        }

        protected override void OnPreOpen()
        {
            transform.GetComponent<Animator>().SetTrigger("Show");
            if (activityOrderChallenge != null)
            {
                rewardIcon.SetImage(Game.Manager.rewardMan.GetRewardIcon(activityOrderChallenge.TotalReward.Id, activityOrderChallenge.TotalReward.Count));
                rewardCount.SetText(activityOrderChallenge.TotalReward.Count.ToString());
                activityOrderChallenge.VisualHelp.Refresh(visualGroup);
                activityOrderChallenge.VisualHelp.RefreshText(visualGroup, "entry1", activityOrderChallenge.TotalNum);
                var name = Game.Manager.rewardMan.GetRewardName(activityOrderChallenge.TotalReward.Id);
                activityOrderChallenge.VisualHelp.RefreshText(visualGroup, "entry3", activityOrderChallenge.TotalReward.Count, name);
                desc.SetText(I18N.FormatText("#SysComDesc697", activityOrderChallenge.LevelCount));
            }
        }

        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }
    }
}