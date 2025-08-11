namespace FAT
{
    public class GuideActImpOpenUIEnergy : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            Game.Manager.screenPopup.WhenOutOfEnergy();
            mIsWaiting = false;
        }
    }
}