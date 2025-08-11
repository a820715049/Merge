/*
 * @Author: qun.chao
 * @Date: 2021-08-13 16:18:10
 */
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpMaskShowBoard : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            int tid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            Item target = BoardViewManager.Instance.FindItem(tid, param.Length > 1);
            if (target != null)
            {
                Game.Manager.guideMan.ActionShowBoardMask(target.coord.x, target.coord.y);
            }
            mIsWaiting = false;
        }
    }
}