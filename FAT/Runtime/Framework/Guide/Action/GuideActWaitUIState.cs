/*
 * @Author: qun.chao
 * @Date: 2021-04-21 17:18:29
 */
using UnityEngine;

namespace FAT
{
    public class GuideActWaitUIState : GuideActImpBase
    {
        private int state;
        private int extra;

        protected override bool IsWaiting()
        {
            return !Game.Manager.guideMan.IsMatchUIState(state, extra);
        }

        public override void Play(string[] param)
        {
            state = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            if (param.Length > 1)
            {
                extra = Mathf.RoundToInt(float.Parse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }
}