/*
 * @Author: qun.chao
 * @Date: 2021-07-27 14:48:37
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpMaskShow : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            var trans = Game.Manager.guideMan.FindByPath(param);
            if (trans != null)
            {
                Game.Manager.guideMan.ActionShowMask(trans);
            }
            mIsWaiting = false;
        }
    }
}