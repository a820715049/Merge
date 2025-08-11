/*
 * @Author: qun.chao
 * @Date: 2022-03-11 20:07:50
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpHand : GuideActImpBase
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
                Game.Manager.guideMan.ActionShowHand(trans, _StopWait);
            }
        }
    }
}