/*
 * @Author: qun.chao
 * @Date: 2025-07-09 11:28:29
 */
using fat.rawdata;

namespace FAT
{
    public class GuideActImpOpenBingoGuide : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            var act = Game.Manager.activity.LookupAny(EventType.ItemBingo) as ActivityBingo;
            if (act == null)
            {
                mIsWaiting = false;
                return;
            }
            UIManager.Instance.OpenWindowAndCallback(UIConfig.UIBingoGuide, () =>
            {
                mIsWaiting = false;
            }, act);
        }
    }
}