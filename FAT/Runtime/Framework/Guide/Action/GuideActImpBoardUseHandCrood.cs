using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpBoardUseHandCrood : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }
        public override void Play(string[] param)
        {

            var x = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            var y = Mathf.RoundToInt(float.Parse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            var showMask = false;
            Game.Manager.guideMan.ActionShowBoardUseTarget(x, y, _StopWait, showMask);
        }
    }
}