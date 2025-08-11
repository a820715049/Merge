/*
 * @Author: qun.chao
 * @Date: 2021-03-08 11:04:32
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpBoardDialogHide : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            Game.Manager.guideMan.ActionBoardHideDialog();
            mIsWaiting = false;
        }
    }
}