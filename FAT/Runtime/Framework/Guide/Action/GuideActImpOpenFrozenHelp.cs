namespace FAT
{
    public class GuideActImpOpenFrozenHelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            UIManager.Instance.OpenWindow(UIConfig.UIFrozenItemHelp);
            mIsWaiting = false;
        }
    }
}