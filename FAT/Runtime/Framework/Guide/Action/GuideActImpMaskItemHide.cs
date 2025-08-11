/*
 * @Author: qun.chao
 * @Date: 2023-11-24 14:37:18
 */
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpMaskItemHide : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            Game.Manager.guideMan.ActiveGuideContext?.HideBoardCommonMask();
            mIsWaiting = false;
        }
    }
}