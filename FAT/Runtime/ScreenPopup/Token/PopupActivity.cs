using System;
using System.Collections.Generic;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT
{
    public class PopupActivity : IScreenPopup
    {
        internal ActivityLike activity;
        public UIResAlt PopupUI { get; internal set; }
        public override UIResource PopupRes { get => PopupUI?.ActiveR; internal set => throw new NotSupportedException(); }
        public bool checkState;
        public bool checkActive;

        public PopupActivity() { }
        public PopupActivity(ActivityLike acti_, ActivityVisual visual_, UIResAlt ui_, bool check_ = false, bool active_ = true)
        {
            Setup(acti_, visual_, ui_, check_, active_);
        }

        public virtual void Setup(ActivityLike acti_, ActivityVisual visual_, UIResAlt ui_, bool check_ = false, bool active_ = true)
        {
            if (!visual_.Valid)
            {
                Clear();
                return;
            }
            activity = acti_;
            PopupId = visual_.PopupId;
            PopupConf = visual_.Popup;
            PopupUI = ui_;
            checkState = check_;
            checkActive = active_;
            RequireValid = true;
        }

        public virtual void Clear()
        {
            PopupConf = null;
            RequireValid = false;
        }

        public override bool CheckValid(out string rs_)
        {
            if (!base.CheckValid(out rs_)) return false;
            if (checkActive && !activity.Active) { rs_ = "activity inactive"; return false; }
            return true;
        }

        public override bool Ready() => UIManager.Instance.CheckUIIsIdleStateForPopup();

        public override bool OpenPopup()
        {
            if (checkState && !Game.Manager.screenPopup.CheckState(PopupState)) return false;
            UIManager.Instance.OpenWindow(PopupRes, activity, Custom);
            Custom = null;
            DataTracker.event_popup.Track(activity);
            return true;
        }

        public override string ToString() => $"{GetType().Name}|{PopupId}|{activity?.Info3}";
    }
}