/*
 * @Author: yanfuxing
 * @Date: 2025-07-22 15:40:09
 */
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMultiplyRankingHelp : UIBase
    {
        public UIVisualGroup visualGroup;
        private ActivityMultiplierRanking _activityRanking;
        private bool _isGuide;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Application.isPlaying) return;
            transform.Access(out visualGroup);
            transform.Access("Content", out Transform root);
            visualGroup.Prepare(root.Access<TextProOnACircle>("title1"), "mainTitle");
            visualGroup.Prepare(root.Access<TMP_Text>("list/text"), "entry1");
            visualGroup.Prepare(root.Access<TMP_Text>("list/text2"), "entry2");
            visualGroup.Prepare(root.Access<TMP_Text>("list/text3"), "entry3");
            visualGroup.Prepare(root.Access<TMP_Text>("list/text4"), "entry4");
            visualGroup.Prepare(root.Access<TMP_Text>("LeftDes"), "entry5");
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
                _activityRanking = (ActivityMultiplierRanking)items[0];
            }
            if (items.Length > 1)
            {
                _isGuide = (bool)items[1];
            }
        }

        protected override void OnPreOpen()
        {
            transform.GetComponent<Animator>().SetTrigger("Show");
            if (_activityRanking != null)
            {
                var visual = _activityRanking.VisualUIRankingHelp.visual;
                visual.Refresh(visualGroup);
                var id = _activityRanking.conf.Token;
                var s = UIUtility.FormatTMPString(id);
                visual.RefreshText(visualGroup, "entry1", s);
            }
        }

        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }

        protected override void OnPostClose()
        {
            if (!_isGuide) { return; }
            var ui = UIManager.Instance.TryGetUI(_activityRanking?.VisualUIRankingMain.res.ActiveR);
            if (ui == null || !(ui is UIMultiplyRankingMain main)) { return; }
            main.GuideTurntableClick();
        }
    }
}