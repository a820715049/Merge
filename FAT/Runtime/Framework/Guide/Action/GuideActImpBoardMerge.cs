/*
 * @Author: qun.chao
 * @Date: 2023-11-23 18:29:57
 */

using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpBoardMerge : GuideActImpBase
    {
        private int tidForMerge;

        private void _OnItemMerged(Item src, Item dst, Item result)
        {
            if (src.tid == tidForMerge)
            {
                BoardViewManager.Instance.checker.SetMatchTid(0);
                mIsWaiting = false;
                Game.Manager.guideMan.OnItemMerge -= _OnItemMerged;
                Game.Manager.guideMan.ActiveGuideContext?.SetRedirector(null);
                BoardViewManager.Instance.Guide_OverrideDraggableItemBehaviour();
            }
        }

        public override void Play(string[] param)
        {
            int itemTid = 0;
            if (param != null && param.Length > 0)
            {
                itemTid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            }

            if (itemTid <= 0)
            {
                mIsWaiting = false;
                return;
            }

            bool forceDragItemToMatchHintItem = false;
            if (param.Length > 1)
            {
                forceDragItemToMatchHintItem = true;
            }

            var checker = BoardViewManager.Instance.checker;
            checker.SetMatchTid(itemTid);
            checker.CheckHint(true);
            if (checker.HasMatchPair())
            {
                tidForMerge = itemTid;
                Game.Manager.guideMan.OnItemMerge -= _OnItemMerged;
                Game.Manager.guideMan.OnItemMerge += _OnItemMerged;
                EL.MessageCenter.Get<MSG.UI_GUIDE_FINGER_MATCH>().Dispatch();
                Game.Manager.guideMan.ActiveGuideContext?.SetRedirector(BoardViewManager.Instance.boardView.boardDrag);

                // 只允许操作合成提示对应的item
                if (forceDragItemToMatchHintItem)
                {
                    var (src, _) = checker.GetMatchPairCoords();
                    var realItem = BoardViewManager.Instance.board.GetItemByCoord(src.x, src.y);
                    BoardViewManager.Instance.Guide_OverrideDraggableItemBehaviour(0, realItem.id,
                        MergeHelper.MergeAction.Merge);
                }
                else
                {
                    BoardViewManager.Instance.Guide_OverrideDraggableItemBehaviour(itemTid, 0,
                        MergeHelper.MergeAction.Merge);
                }

                mIsWaiting = true;
            }
            else
            {
                mIsWaiting = false;
            }
        }
    }
}