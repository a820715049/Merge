/*
 * @Author: qun.chao
 * @Date: 2023-12-14 14:54:18
 */

using UnityEngine;

namespace FAT
{
    public class GuideActImpSpShowGameplayHelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            UIUtility.ShowGameplayHelp();
            mIsWaiting = false;
        }
    }
}