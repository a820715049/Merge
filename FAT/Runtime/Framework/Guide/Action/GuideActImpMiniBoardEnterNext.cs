namespace FAT
{
    public class GuideActImpMiniBoardEnterNext : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            mIsWaiting = true;
            Game.Manager.miniBoardMultiMan.TryOpenUIEnterNextRoundTips();
            mIsWaiting = false;
        }
    }
}