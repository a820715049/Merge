/*
 * @Author: qun.chao
 * @Date: 2022-11-02 12:07:38
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpTopDialogHide : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            Game.Manager.guideMan.ActionTopHideDialog();
            mIsWaiting = false;
        }
    }
}