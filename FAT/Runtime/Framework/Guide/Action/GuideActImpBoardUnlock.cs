/*
 * @Author: qun.chao
 * @Date: 2024-09-23 16:40:36
 */
using UnityEngine;

namespace FAT
{
    /// <summary>
    /// 对指定位置的item解除frozen状态
    /// </summary>
    public class GuideActImpBoardUnlock : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (param.Length == 2)
            {
                int.TryParse(param[0], out var pos_x);
                int.TryParse(param[1], out var pos_y);
                var item = Game.Manager.mergeBoardMan.activeWorld?.activeBoard?.GetItemByCoord(pos_x, pos_y);
                if (item != null)
                {
                    Game.Manager.mergeBoardMan.activeWorld?.activeBoard.UnfrozenItem(item);
                }
            }
            mIsWaiting = false;
        }
    }
}