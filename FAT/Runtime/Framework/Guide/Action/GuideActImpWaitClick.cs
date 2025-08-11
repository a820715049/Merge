/*
 * @Author: qun.chao
 * @Date: 2021-03-03 16:29:13
 */

namespace FAT
{
    public class GuideActImpWaitClick : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            Game.Manager.guideMan.ActionWaitClick(_StopWait);
        }
    }
}