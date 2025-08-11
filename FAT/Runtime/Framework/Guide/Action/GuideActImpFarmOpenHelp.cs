namespace FAT
{
    public class GuideActImpFarmOpenHelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            var ui = UIManager.Instance.TryGetUI(UIConfig.UIFarmBoardMain);
            if (ui != null && ui is UIFarmBoardMain main)
            {
                main.OnClickHelp();
            }
            mIsWaiting = false;
        }
    }
}