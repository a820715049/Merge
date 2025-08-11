/*
 * @Author: qun.chao
 * @Date: 2021-03-04 15:08:46
 */

using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpBoardMatch : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            int itemTid = 0;
            if (param != null && param.Length > 0)
            {
                itemTid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            }
            BoardViewManager.Instance.checker.SetMatchTid(itemTid);
            EL.MessageCenter.Get<MSG.UI_GUIDE_FINGER_MATCH>().Dispatch();
            mIsWaiting = false;
        }
    }
}