/*
 * @Author: qun.chao
 * @Date: 2022-03-11 20:26:17
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpHandHide : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            Game.Manager.guideMan.ActionHideHand();
            mIsWaiting = false;
        }
    }
}