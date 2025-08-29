namespace FAT
{
    public class PopupDeco:PopupActivity
    {
        public override bool OpenPopup()
        {
            if (checkState && !Game.Manager.screenPopup.CheckState(PopupState)) return false;
            if ((activity as DecorateActivity).NeedComplete)
            {
                GameProcedure.MergeToSceneArea(Game.Manager.decorateMan.Activity.CurArea, () =>
                {
                    Game.Instance.StartCoroutineGlobal(Game.Manager.mapSceneMan.AreaCompleteVisual(Game.Manager.decorateMan.Activity.CurArea));
                    (activity as DecorateActivity).SetCompleteState(false);
                });
            }
            else
            {
                UIManager.Instance.OpenWindow(PopupRes, activity, Custom);
            }
            Custom = null;
            DataTracker.event_popup.Track(activity);
            return true;
        }
    }
}