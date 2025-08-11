/*
 * @Author: qun.chao
 * @Date: 2023-12-01 15:15:07
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpTalkHide : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            Game.Manager.guideMan.ActiveGuideContext?.HideTalk();
            mIsWaiting = false;
        }
    }
}