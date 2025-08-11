/*
 * @Author: qun.chao
 * @Date: 2021-03-03 20:13:15
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpWaitItem : GuideActImpBase
    {
        private int tid;
        private int num;

        protected override bool IsWaiting()
        {
            return !BoardViewManager.Instance.HasActiveItem(tid, num);
        }

        public override void Play(string[] param)
        {
            tid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            num = Mathf.RoundToInt(float.Parse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}