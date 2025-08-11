/*
 * @Author: qun.chao
 * @Date: 2021-10-11 10:55:19
 */
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpBoardDrag : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            int from = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            Item fromItem = BoardViewManager.Instance.FindItem(from, false);
            int to = Mathf.RoundToInt(float.Parse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            Item toItem = BoardViewManager.Instance.FindItem(to, false);
            if (fromItem != null && toItem != null)
            {
                EL.MessageCenter.Get<MSG.UI_GUIDE_FINGER_DRAG>().Dispatch(fromItem, toItem);
            }
            mIsWaiting = false;
        }
    }
}