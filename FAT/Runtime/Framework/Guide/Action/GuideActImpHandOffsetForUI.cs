/*
 * @Author: qun.chao
 * @Date: 2023-12-14 11:17:41
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpHandOffsetForUI : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var offset_x);
            float.TryParse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var offset_y);

            Game.Manager.guideMan.ActiveGuideContext?.ShowPointerOffsetForUI(new Vector2(offset_x, offset_y));
            mIsWaiting = false;
        }
    }
}