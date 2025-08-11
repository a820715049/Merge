/*
 *@Author:chaoran.zhang
 *@Desc:引导强制点击可以能量加倍的物体，若棋盘上没有则判定为直接完成
 *@Created Time:2024.02.19 星期一 10:49:55
 */

using UnityEngine;

namespace FAT
{
    public class GuideActImpBoardTapBoost : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            var target = BoardViewManager.Instance.FindBoostItem();
            if (target != null)
            {
                if (param.Length < 2)
                {
                    Debug.LogError("[GUIDE] board_tap_boost params less than 2");
                    return;
                }

                var block = param[0].Contains("true");
                var mask = param[1].Contains("true");
                Game.Manager.guideMan.ActionShowBoardUseTarget(target.coord.x, target.coord.y, _StopWait, mask);
                Game.Manager.guideMan.ActionSetBlock(block);
            }
            else
            {
                Game.Manager.guideMan.ActionSaveProgress();
                mIsWaiting = false;
                Game.Manager.guideMan.DropGuide();
            }
        }
    }
}