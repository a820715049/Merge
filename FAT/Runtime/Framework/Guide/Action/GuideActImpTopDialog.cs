/*
 * @Author: qun.chao
 * @Date: 2022-11-02 12:06:59
 */

using UnityEngine;

namespace FAT
{
    public class GuideActImpTopDialog : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (param.Length > 1)
            {
                Game.Manager.guideMan.ActionTopShowDialog(param[0], param[1]);
            }
            else if (param.Length > 0)
            {
                Game.Manager.guideMan.ActionTopShowDialog(param[0], null);
            }
            mIsWaiting = false;
        }
    }
}