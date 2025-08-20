/*
 * @Author: qun.chao
 * @Date: 2025-07-30 16:46:48
 */
namespace FAT
{
    public class GuideActImpClawOrderHelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            UIConfig.UIClawOrderHelp.Open();
            mIsWaiting = false;
        }
    }
}