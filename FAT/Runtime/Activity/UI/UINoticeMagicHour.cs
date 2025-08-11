using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT
{
    public class UINoticeMagicHour : UIBase
    {
        public UIVisualGroup visualGroup;
        internal TextMeshProUGUI cd;
        private Action<ActivityLike, bool> WhenEnd;
        private Action WhenTick;
        private ActivityMagicHour activity;

        public void OnValidate() {
            if (Application.isPlaying) return;
            transform.Access("Content", out Transform root);
            transform.Access(out visualGroup);
            visualGroup.Prepare(root.Access<UIImageRes>("bg"), "bgImage");
            visualGroup.Prepare(root.Access<UIImageRes>("bg1"), "titleImage");
            visualGroup.Prepare(root.Access<TextProOnACircle>("title"), "mainTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("group/desc"), "desc1");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_cd/text"), "time");
            visualGroup.Prepare(root.Access<UIImageRes>("_cd/frame"), "time");
            visualGroup.CollectTrim();
        }

        protected override void OnCreate()
        {
            transform.Access("Content", out Transform root);
            root.Access("_cd/text", out cd);
            root.Access<MapButton>("close").WithClickScale().FixPivot().WhenClick = Close;
            root.Access<MapButton>("info").WithClickScale().FixPivot().WhenClick = InfoClick;
            root.Access<MapButton>("confirm").WithClickScale().WhenClick = ConfirmClick;
        }

        protected override void OnParse(params object[] items)
        {
            activity = (ActivityMagicHour)items[0];
        }

        protected override void OnPreOpen()
        {
            RefreshCD();
            RefreshTheme();
        }

        protected override void OnAddListener()
        {
            WhenEnd ??= RefreshEnd;
            WhenTick ??= RefreshCD;
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
        }

        private void RefreshCD()
        {
            UIUtility.CountDownFormat(cd, activity.Countdown);
        }

        public void RefreshTheme() {
            var visual = activity.Visual;
            visual.Refresh(visualGroup);
        }

        private void RefreshEnd(ActivityLike pack_, bool expire_)
        {
            if (pack_ != activity) return;
            Close();
        }

        public void InfoClick() {
            UIManager.Instance.OpenWindow(UIConfig.UIMagicHourHelp);
        }

        public void ConfirmClick() {
            Close();
            GameProcedure.SceneToMerge();
        }
    }
}