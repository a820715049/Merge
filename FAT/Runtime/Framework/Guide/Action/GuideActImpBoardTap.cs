/*
 * @Author: qun.chao
 * @Date: 2021-03-04 14:11:25
 */
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpBoardTap : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            int itemTid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            Item tapItem = BoardViewManager.Instance.FindItem(itemTid, param.Length > 1);
            if (tapItem != null)
                EL.MessageCenter.Get<MSG.UI_GUIDE_FINGER_TAP>().Dispatch(tapItem);
            mIsWaiting = false;
        }
    }
}