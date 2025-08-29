/**
 * @Author: zhangpengjian
 * @Date: 2024/10/22 14:11:17
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/22 14:11:17
 * Description: 排行榜帮助界面
 */

using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityRankingHelp : UIBase
    {
        public UIVisualGroup visualGroup;
        private ActivityRanking activityRanking;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Application.isPlaying) return;
            transform.Access(out visualGroup);
            transform.Access("Content", out Transform root);
            visualGroup.Prepare(root.Access<TextProOnACircle>("page1/title1"), "mainTitle");
            visualGroup.Prepare(root.Access<TMP_Text>("page1/text"), "entry1");
            visualGroup.Prepare(root.Access<TMP_Text>("page1/text2"), "entry2");
            visualGroup.Prepare(root.Access<TMP_Text>("page1/text3"), "entry3");
            visualGroup.CollectTrim();
        }
#endif
        protected override void OnCreate()
        {
            transform.AddButton("Mask", OnClose).FixPivot();
            transform.AddButton("close", OnClose).FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                activityRanking = (ActivityRanking)items[0];
            }
        }

        protected override void OnPreOpen()
        {
            transform.GetComponent<Animator>().SetTrigger("Show");
            if (activityRanking != null)
            {
                var id = activityRanking.confD.RequireScoreId;
                var s = UIUtility.FormatTMPString(id);
                var visual = activityRanking.VisualHelp.visual;
                visual.Refresh(visualGroup);
                visual.RefreshText(visualGroup, "mainTitle", s);
                visual.RefreshText(visualGroup, "entry1", s);
                visual.RefreshText(visualGroup, "entry2", s);
            }
        }

        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }
    }
}