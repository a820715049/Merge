/*
 * @Author: qun.chao
 * @Date: 2022-09-06 12:45:32
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpWaitItemPos : GuideActImpBase
    {
        private int tid;
        private int pos_x;
        private int pos_y;

        protected override bool IsWaiting()
        {
            var item = BoardViewManager.Instance.board.GetItemByCoord(pos_x, pos_y);
            if (item != null && item.tid == tid)
            {
                // 指定位置存在指定item 不用继续等待
                return false;
            }
            return true;
        }

        public override void Play(string[] param)
        {
            tid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            pos_x = Mathf.RoundToInt(float.Parse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            pos_y = Mathf.RoundToInt(float.Parse(param[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}