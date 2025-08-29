/*
 * @Author: yanfuxing
 * @Date: 2025-04-23 18:24:15
 */
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    // 里程碑帮助
    public class UIActivityRankingMilestonHelp : UIBase
    {
        public UIVisualGroup visualGroup;
        private ActivityRanking activityRanking;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Application.isPlaying) return;
            transform.Access(out visualGroup);
            transform.Access("Content", out Transform root);
            visualGroup.Prepare(root.Access<TextProOnACircle>("title2"), "mainTitle");
            visualGroup.Prepare(root.Access<TMP_Text>("list/text"), "entry1");
            visualGroup.Prepare(root.Access<TMP_Text>("list/text3"), "entry2");
            visualGroup.Prepare(root.Access<TMP_Text>("list/text4"), "entry3");
            visualGroup.Prepare(root.Access<TMP_Text>("list/text2"), "entry4");
            visualGroup.CollectTrim();
        }
#endif
        protected override void OnCreate()
        {
            transform.AddButton("Mask", OnClose).FixPivot();
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
                var requireScoreId = activityRanking.confD.RequireScoreId;
                var stmpStr = UIUtility.FormatTMPString(requireScoreId);
                var visual = activityRanking.VisualMilestonHelp.visual;
                visual.Refresh(visualGroup);
                visual.RefreshText(visualGroup, "mainTitle", stmpStr);
                visual.RefreshText(visualGroup, "entry1", stmpStr);
                visual.RefreshText(visualGroup, "entry2", stmpStr);
                visual.RefreshText(visualGroup, "entry4", stmpStr);
            }
        }

        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }
    }
}

