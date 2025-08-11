/*
 * @Author: qun.chao
 * @Date: 2022-04-14 15:46:22
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpWaitProduceCount : GuideActImpBase
    {
        private int tid;
        private int count;

        protected override bool IsWaiting()
        {
            var cid = Game.Manager.mergeItemMan.GetItemCategoryId(tid);
            var outputMap = Game.Manager.mergeBoardMan.globalData.FixedOutputId;
            var hasProducedCount = 0;
            if (outputMap.ContainsKey(cid))
                hasProducedCount = outputMap[cid];
            return hasProducedCount < count;
        }

        public override void Play(string[] param)
        {
            tid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            count = Mathf.RoundToInt(float.Parse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}