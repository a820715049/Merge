/*
 * @Author: qun.chao
 * @Date: 2022-12-21 16:25:36
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpSpMergeEntryHijack : GuideActImpBase
    {
        private void _ResolveWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            // if (Game.Manager.guideMan.IsNewUser())
            // {
            //     // 劫持merge入口
            //     UIZodiacEventUtility.SetMergeEntryResolver(_ResolveWait);
            // }
            // else
            // {
            //     UIZodiacEventUtility.ResolveMergeEntry();
            //     mIsWaiting = false;
            // }
            mIsWaiting = false;
        }
    }
}