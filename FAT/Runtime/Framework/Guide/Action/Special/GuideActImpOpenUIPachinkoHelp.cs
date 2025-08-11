namespace FAT
{
    public class GuideActImpOpenUIPachinkoHelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            UIManager.Instance.OpenWindow(Game.Manager.pachinkoMan.GetActivity().HelpResAlt.ActiveR);
            mIsWaiting = false;
        }
    }
}