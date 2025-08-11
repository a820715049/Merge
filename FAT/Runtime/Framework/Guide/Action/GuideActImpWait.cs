/*
 * @Author: qun.chao
 * @Date: 2021-03-04 16:26:02
 */

using UnityEngine;

namespace FAT
{
    public class GuideActImpWait : GuideActImpBase
    {
        private float mTarTime = 0f;

        protected override bool IsWaiting()
        {
            return Time.timeSinceLevelLoad < mTarTime;
        }

        public override void Play(string[] param)
        {
            if (float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float sec))
            {
                mTarTime = Time.timeSinceLevelLoad + sec;
            }
            else
            {
                mTarTime = Time.timeSinceLevelLoad;
            }
        }
    }
}