/*
 * @Author: qun.chao
 * @Date: 2021-10-21 16:03:58
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpWaitSelect : GuideActImpBase
    {
        private int tid;

        protected override bool IsWaiting()
        {
            return BoardViewManager.Instance.GetCurrentBoardInfoItemTid() != tid;
        }

        public override void Play(string[] param)
        {
            tid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}