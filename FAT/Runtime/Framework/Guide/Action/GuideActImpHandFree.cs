/*
 * @Author: qun.chao
 * @Date: 2022-05-09 11:23:23
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpHandFree : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            var trans = Game.Manager.guideMan.FindByPath(param);
            if (trans != null)
            {
                Game.Manager.guideMan.ActionShowHandFree(trans, _StopWait);
            }
        }
    }
}