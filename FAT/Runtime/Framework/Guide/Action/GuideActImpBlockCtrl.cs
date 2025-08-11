/*
 * @Author: qun.chao
 * @Date: 2021-03-03 16:43:02
 */

using UnityEngine;

namespace FAT
{
    public class GuideActImpBlockCtrl : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (bool.TryParse(param[0], out bool b))
            {
                Game.Manager.guideMan.ActionSetBlock(b);
            }
            else
            {
                Game.Manager.guideMan.ActionSetBlock(false);
            }
            mIsWaiting = false;
        }
    }
}