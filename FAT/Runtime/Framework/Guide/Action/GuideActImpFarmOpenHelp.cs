using fat.rawdata;

namespace FAT
{
    public class GuideActImpFarmOpenHelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            var act = Game.Manager.activity.LookupAny(EventType.FarmBoard) as FarmBoardActivity;
            if (act == null)
            {
                mIsWaiting = false;
                return;
            }
            
            var ui = UIManager.Instance.TryGetUI(act.VisualBoard.res.ActiveR);
            if (ui != null && ui is UIFarmBoardMain main)
            {
                main.OnClickHelp();
            }
            mIsWaiting = false;
        }
    }
}