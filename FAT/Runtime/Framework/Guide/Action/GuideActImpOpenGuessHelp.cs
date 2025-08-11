using fat.rawdata;

namespace FAT
{
    public class GuideActImpOpenGuessHelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (!Game.Manager.activity.LookupAny(EventType.Guess, out var acti))
            {
                mIsWaiting = false;
                return;
            }

            if (acti as ActivityGuess == null)
            {
                mIsWaiting = false;
                return;
            }

            UIManager.Instance.OpenWindow((acti as ActivityGuess).VisualHelp.res.ActiveR);
            mIsWaiting = false;
        }
    }
}