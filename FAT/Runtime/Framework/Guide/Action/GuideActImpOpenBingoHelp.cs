using fat.rawdata;

namespace FAT
{
    public class GuideActImpOpenBingoHelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            var act = Game.Manager.activity.LookupAny(EventType.ItemBingo) as ActivityBingo;
            if (act == null)
            {
                mIsWaiting = false;
                return;
            }
            UIManager.Instance.OpenWindow(UIConfig.UIBingoHelp, act);
            mIsWaiting = false;
        }
    }
}