namespace FAT
{
    public class GuideActImpOpenHelpCardTrade : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            int.TryParse(param[0], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var num);
            UIManager.Instance.OpenWindow(UIConfig.UICardAlbumGuide, num);
            mIsWaiting = false;
        }
    }
}