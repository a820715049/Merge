using System.Reflection;
using fat.rawdata;

namespace FAT
{
    public class GuideActImpUIMultihelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {

            var multi = Game.Manager.activity.LookupAny(EventType.MultiplierRanking) as ActivityMultiplierRanking;
            if (multi != null) UIManager.Instance.OpenWindow(multi.VisualUIRankingHelp.res.ActiveR, multi, true);
            mIsWaiting = false;
        }
    }
}