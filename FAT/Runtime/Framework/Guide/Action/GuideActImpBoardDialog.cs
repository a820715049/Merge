/*
 * @Author: qun.chao
 * @Date: 2021-03-03 21:10:48
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpBoardDialog : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (param.Length > 1)
            {
                Game.Manager.guideMan.ActionBoardShowDialog(param[0], param[1]);
            }
            else if (param.Length > 0)
            {
                Game.Manager.guideMan.ActionBoardShowDialog(param[0], null);
            }
            mIsWaiting = false;
        }
    }
}