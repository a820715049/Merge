using System.Collections.Generic;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpMiniBoardMerge : GuideActImpBase
    {
        private int tidForMerge;
        private float offsetCoe;
        private List<int> list = new();

        private void _OnItemMerged(Item src, Item dst, Item result)
        {
            if (src.tid == tidForMerge)
            {
                BoardViewManager.Instance.checker.SetMatchTid(0);
                mIsWaiting = false;
                Game.Manager.guideMan.OnItemMerge -= _OnItemMerged;
                Game.Manager.guideMan.ActiveGuideContext?.SetRedirector(null);
                BoardViewManager.Instance.Guide_OverrideDraggableItemBehaviour();
                Game.Manager.guideMan.ActiveGuideContext?.HideBoardCommonMask();
            }
        }

        public override void Play(string[] param)
        {
            if (param.Length >= 1)
                float.TryParse(param[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out offsetCoe);

            if (!Game.Manager.miniBoardMan.IsValid && !Game.Manager.miniBoardMultiMan.IsValid)
            {
                mIsWaiting = false;
                return;
            }

            var checker = BoardViewManager.Instance.checker;
            checker.FindMatch();
            checker.CheckHint(true);
            if (checker.HasMatchPair())
            {
                tidForMerge = checker.GetMatchItem();
                list.Add(tidForMerge);
                if (tidForMerge == 0)
                {
                    mIsWaiting = false;
                    return;
                }

                Game.Manager.guideMan.OnItemMerge -= _OnItemMerged;
                Game.Manager.guideMan.OnItemMerge += _OnItemMerged;
                EL.MessageCenter.Get<MSG.UI_GUIDE_FINGER_MATCH>().Dispatch();
                Game.Manager.guideMan.ActiveGuideContext?.SetRedirector(BoardViewManager.Instance.boardView.boardDrag);
                var (src, _) = checker.GetMatchPairCoords();
                var realItem = BoardViewManager.Instance.board.GetItemByCoord(src.x, src.y);
                BoardViewManager.Instance.Guide_OverrideDraggableItemBehaviour(0, realItem.id,
                    MergeHelper.MergeAction.Merge);

                Game.Manager.guideMan.ActiveGuideContext?.ShowBoardCommonMask(list, null, offsetCoe);
                mIsWaiting = true;
            }
            else
            {
                mIsWaiting = false;
            }
        }
    }
}