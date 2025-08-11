/*
 * @Author: qun.chao
 * @Date: 2021-05-26 10:10:17
 */
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpBoardUseHand : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            int tid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            Item target = BoardViewManager.Instance.FindItem(tid, param.Length > 1);
            if (target != null)
            {
                bool showMask = false;
                Game.Manager.guideMan.ActionShowBoardUseTarget(target.coord.x, target.coord.y, _StopWait, showMask);
            }
            else
            {
                mIsWaiting = false;
            }
        }
    }
}