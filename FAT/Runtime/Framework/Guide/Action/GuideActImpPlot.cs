/*
 * @Author: qun.chao
 * @Date: 2022-03-14 14:33:30
 */
using System.Collections.Generic;
using UnityEngine;
using EL;

namespace FAT
{
    public class GuideActImpPlot : GuideActImpBase
    {
        private static List<int> intCache = new List<int>();

        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            intCache.Clear();
            foreach (var item in param)
            {
                if (float.TryParse(item, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var _id))
                {
                    intCache.Add((int)_id);
                }
            }
            Game.Manager.guideMan.ActionShowTalk(intCache, _StopWait);
        }
    }
}