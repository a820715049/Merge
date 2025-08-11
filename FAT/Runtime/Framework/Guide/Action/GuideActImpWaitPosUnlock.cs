/*
 * @Author: qun.chao
 * @Date: 2024-05-22 11:06:48
 */

using UnityEngine;

namespace FAT
{
    public class GuideActImpWaitPosUnlock : GuideActImpBase
    {
        private int pos_x;
        private int pos_y;

        protected override bool IsWaiting()
        {
            var item = BoardViewManager.Instance.board.GetItemByCoord(pos_x, pos_y);
            // 指定位置没有item
            if (item == null)
                return false;
            //  item已经解锁且没有被特效锁定
            if (!item.isLocked &&
                !BoardViewManager.Instance.checker.IsCoordMarked(item.coord))
                return false;
            return true;
        }

        public override void Play(string[] param)
        {
            pos_x = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            pos_y = Mathf.RoundToInt(float.Parse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}