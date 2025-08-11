namespace FAT
{
    public class PopupMiniBoard : PopupActivity
    {
        public override bool OpenPopup()
        {
            if (checkState && !Game.Manager.screenPopup.CheckState(PopupState)) return false;
            if (activity.Active)
                Game.Manager.miniBoardMan.EnterMiniBoard();
            else
                UIManager.Instance.OpenWindow(PopupUI.ActiveR, activity);

            DataTracker.event_popup.Track(activity);
            return true;
        }
    }
}