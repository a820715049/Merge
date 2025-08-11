namespace FAT
{
    public class GuideActImpOpenWishHelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            var ui = UIManager.Instance.TryGetUI(UIConfig.UIWishBoardMain);
            if (ui != null && ui is UIWishBoardMain main)
            {
                main.OnClickHelp();
            }
            mIsWaiting = false;
        }
    }
}
