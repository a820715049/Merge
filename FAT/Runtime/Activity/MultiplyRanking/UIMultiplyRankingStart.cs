/*
 * @Author: yanfuxing
 * @Date: 2025-07-18 11:20:05
 */
using System;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMultiplyRankingStart : UIBase
    {
        private TextMeshProUGUI leftTime;
        public UIVisualGroup visualGroup;
        private ActivityMultiplierRanking activity;
        private Action whenTick;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Application.isPlaying) return;
            transform.Access(out visualGroup);
            transform.Access("Content", out Transform root);
            visualGroup.Prepare(root.Access<TextProOnACircle>("title"), "mainTitle");
            visualGroup.Prepare(root.Access<TMP_Text>("desc"), "desc1");
            visualGroup.Prepare(root.Access<TMP_Text>("desc2"), "desc2");
            visualGroup.CollectTrim();
        }
#endif

        protected override void OnCreate()
        {
            base.OnCreate();
            transform.AddButton("Content/confirm", _ClickConfirm);
            leftTime = transform.Find("Content/_cd/text").GetComponent<TextMeshProUGUI>();
        }

        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            if (items.Length > 0)
            {
                activity = (ActivityMultiplierRanking)items[0];
            }
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            whenTick ??= RefreshCD;
            var visual = activity.VisualUIRankingStart.visual;
            visual.Refresh(visualGroup);
            var id = activity.conf.Token;
            var s = UIUtility.FormatTMPString(id);
            visual.RefreshText(visualGroup, "desc2", s);
        }
        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(whenTick);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(whenTick);
        }

        private void RefreshCD()
        {
            var v = activity?.Countdown ?? 0;
            UIUtility.CountDownFormat(leftTime, v);
            if (v <= 0)
            {
                Close();
            }
        }

        private void _ClickConfirm()
        {
            if (activity != null)
            {
                activity.VisualUIRankingMain.res.ActiveR.Open(activity, RankingOpenType.Main);
            }
            Close();
        }
    }
}