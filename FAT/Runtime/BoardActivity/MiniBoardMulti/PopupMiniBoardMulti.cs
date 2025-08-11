namespace FAT
{
    public class PopupMiniBoardMulti : PopupActivity
    {
        public override bool OpenPopup()
        {
            if (checkState && !Game.Manager.screenPopup.CheckState(PopupState)) return false;
            if (Game.Manager.activity.mapR.ContainsKey(activity))
                Game.Manager.miniBoardMultiMan.EnterMiniBoard();
            else
                UIManager.Instance.OpenWindow(PopupUI.ActiveR, activity);

            DataTracker.event_popup.Track(activity);
            return true;
        }
    }
}