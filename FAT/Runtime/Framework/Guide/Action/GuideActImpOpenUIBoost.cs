/*
 *@Author:chaoran.zhang
 *@Desc:
 *@Created Time:2024.02.19 星期一 16:20:50
 */

namespace FAT
{
    public class GuideActImpOpenUIBoost : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            UIManager.Instance.OpenWindow(UIConfig.UIGuideBoost);
            mIsWaiting = false;
        }
    }
}