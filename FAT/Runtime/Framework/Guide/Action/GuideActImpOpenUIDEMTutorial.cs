namespace FAT
{
    public class GuideActImpOpenUIDEMTutorial:GuideActImpBase
    {
        public override void Play(string[] param)
        {
            UIManager.Instance.OpenWindow(UIConfig.UIHelpDEM);
            mIsWaiting = false;
        }
    }
}