/*
 * @Author: qun.chao
 * @Date: 2021-07-27 14:50:31
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpMaskHide : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            Game.Manager.guideMan.ActionHideMask();
            mIsWaiting = false;
        }
    }
}