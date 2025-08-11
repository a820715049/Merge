/*
 * @Author: qun.chao
 * @Date: 2022-09-06 13:37:01
 */
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpBoardSelectHandPos : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            int pos_x = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            int pos_y = Mathf.RoundToInt(float.Parse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            bool showMask = true;
            Game.Manager.guideMan.ActionShowBoardSelectTarget(pos_x, pos_y, _StopWait, showMask);
        }
    }
}