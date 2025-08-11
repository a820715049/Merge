/*
 * @Author: qun.chao
 * @Date: 2022-03-15 11:54:15
 */
using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public class GuideActImpFinishGuide : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            foreach (var item in param)
            {
                if (int.TryParse(item, out var _id))
                {
                    Game.Manager.guideMan.FinishGuideById(_id);
                }
            }
            mIsWaiting = false;
        }
    }
}