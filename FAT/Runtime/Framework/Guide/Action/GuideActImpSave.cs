/*
 * @Author: qun.chao
 * @Date: 2022-03-11 20:31:52
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpSave : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            Game.Manager.guideMan.ActionSaveProgress();
            mIsWaiting = false;
        }
    }
}