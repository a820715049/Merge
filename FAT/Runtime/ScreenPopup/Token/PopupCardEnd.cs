using fat.rawdata;

namespace FAT
{
    public class PopupCardEnd : PopupActivity
    {
        public int Param;
        
        public PopupCardEnd(ActivityLike acti_, ActivityVisual visual_, UIResAlt ui_, bool check_ = false,
            bool active_ = true)
        {
            Setup(acti_, visual_, ui_, check_, active_);
        }

        public override void Setup(ActivityLike acti_, ActivityVisual visual_, UIResAlt ui_, bool check_ = false,
            bool active_ = true)
        {
            base.Setup(acti_, visual_, ui_, check_, active_);
            Param = visual_.Theme.Id;
        }

        public override bool OpenPopup()
        {
            if (checkState && !Game.Manager.screenPopup.CheckState(PopupState)) return false;
            UIManager.Instance.OpenWindow(PopupRes, activity, Param);
            Custom = null;
            DataTracker.event_popup.Track(activity);
            return true;
        }
    }
}