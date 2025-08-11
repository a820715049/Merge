/*
 * @Author: qun.chao
 * @Date: 2022-09-06 12:34:51
 */

using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpBoardDragPos : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            int from = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            Item fromItem = BoardViewManager.Instance.FindItem(from, false);
            int pos_x = Mathf.RoundToInt(float.Parse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            int pos_y = Mathf.RoundToInt(float.Parse(param[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            if (fromItem != null)
            {
                EL.MessageCenter.Get<MSG.UI_GUIDE_FINGER_DRAG_POS>().Dispatch(fromItem, new Vector2(pos_x, pos_y) + Vector2.one * 0.5f);
            }
            mIsWaiting = false;
        }
    }
}