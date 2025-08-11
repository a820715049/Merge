// ================================================
// File: GuideActImpHandMove.cs
// Author: yueran.li
// Date: 2025/05/22 10:46:43 星期四
// Desc: 手指从item移动到对应位置
// ================================================

using System.Collections.Generic;
using System.Linq;
using EL;
using FAT.Merge;
using FAT.MSG;
using UnityEngine;

namespace FAT
{
    public class GuideActImpBoardToCustom : GuideActImpBase
    {
        public GuideActImpBoardToCustom()
        {
            MessageCenter.Get<UI_BOARD_DRAG_ITEM_END_CUSTOM>().AddListener(_Clear);
        }

        private void _StopWait()
        {
            mIsWaiting = false;
        }

        private void _Clear(Vector2 pos, Item item)
        {
            mIsWaiting = false;
            Game.Manager.guideMan.ActiveGuideContext?.SetRedirector(null);
            Game.Manager.guideMan.ActiveGuideContext?.HideBoardCommonMask();
            BoardViewManager.Instance.Guide_OverrideDraggableItemBehaviour();
            MessageCenter.Get<UI_GUIDE_FINGER_Hide>().Dispatch();
        }

        public override void Clear()
        {
            MessageCenter.Get<UI_BOARD_DRAG_ITEM_END_CUSTOM>().RemoveListener(_Clear);
        }


        public override void Play(string[] param)
        {
            if (param.Length < 2)
            {
                _StopWait();
                Debug.LogError("[GUIDE] hand_fish params less than 4");
                return;
            }

            var id = int.Parse(param[0]);
            var item = BoardViewManager.Instance.FindItem(id, false);
            var temp = Enumerable.ToList(param.Skip(1));
            var trans = Game.Manager.guideMan.FindByPath(temp);


            // 显示mask
            Game.Manager.guideMan.ActiveGuideContext?.ShowBoardCommonMask(new List<int>() { item.tid },
                new List<Transform>() { trans }, 0.5f);

            // 显示拖拽手
            var sp = GuideUtility.CalcRectTransformScreenPos(null, trans);
            var coord = BoardUtility.GetRealCoordByScreenPos(sp);
            EL.MessageCenter.Get<MSG.UI_GUIDE_FINGER_DRAG_POS>().Dispatch(item, coord);

            Game.Manager.guideMan.ActiveGuideContext?.SetRedirector(BoardViewManager.Instance.boardView.boardDrag);
            BoardViewManager.Instance.Guide_OverrideDraggableItemBehaviour(0, 0, MergeHelper.MergeAction.Custom);
            mIsWaiting = true;
        }
    }
}