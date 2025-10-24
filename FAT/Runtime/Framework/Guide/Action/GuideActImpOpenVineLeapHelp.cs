using fat.rawdata;

namespace FAT
{
    public class GuideActImpOpenVineLeapHelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            var activity = (ActivityVineLeap)Game.Manager.activity.LookupAny(EventType.VineLeap);
            var mainConfig = activity.VisualMain.res.ActiveR ?? UIConfig.UIVineLeapMain;
            var ui = UIManager.Instance.TryGetUI(mainConfig);
            if (ui && ui is UIVineLeapMain main)
            {
                UIManager.Instance.OpenWindow(activity.VisualHelp.res.ActiveR, activity);
            }

            mIsWaiting = false;
        }
    }
}