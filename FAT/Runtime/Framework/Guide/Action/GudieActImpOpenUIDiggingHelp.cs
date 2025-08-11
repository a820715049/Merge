using fat.rawdata;

namespace FAT
{
    public class GuideActImpOpenUIDiggingHelp: GuideActImpBase
    {
        public override void Play(string[] param)
        {
            var act = Game.Manager.activity.LookupAny(EventType.Digging) as ActivityDigging;
            if (act == null)
            {
                mIsWaiting = false;
                return;
            }
            UIManager.Instance.OpenWindow(act.HelpRes.ActiveR);
            mIsWaiting = false;
        }
    }
}