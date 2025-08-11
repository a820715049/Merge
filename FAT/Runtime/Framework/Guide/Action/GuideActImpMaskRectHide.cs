/*
 * @Author: qun.chao
 * @Date: 2022-12-19 12:45:58
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpMaskRectHide : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            Game.Manager.guideMan.ActionHideRectMask();
            mIsWaiting = false;
        }
    }
}