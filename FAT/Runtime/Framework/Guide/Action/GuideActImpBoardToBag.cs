/*
 * @Author: qun.chao
 * @Date: 2023-12-01 22:33:56
 */
using System.Collections.Generic;
using UnityEngine;
using FAT.Merge;
using EL;

namespace FAT
{
    public class GuideActImpBoardToBag : GuideActImpBase
    {
        private void _Clear()
        {
            mIsWaiting = false;
            Game.Manager.guideMan.OnItemPutIntoInventory -= _OnItemPutIntoInventory;
            Game.Manager.guideMan.ActiveGuideContext?.SetRedirector(null);
            Game.Manager.guideMan.ActiveGuideContext?.HideBoardCommonMask();
            BoardViewManager.Instance.Guide_OverrideDraggableItemBehaviour();
        }

        private void _OnItemPutIntoInventory(Item item)
        {
            _Clear();
        }

        public override void Clear()
        {
            _Clear();
        }

        public override void Play(string[] param)
        {
            // 优先寻找一个常规物品 排除 宝箱/点击器/奖励/气泡 ..
            var board = BoardViewManager.Instance.board;
            var width = board.size.x;
            var height = board.size.y;
            Item fallback = null;
            bool keepSearching = true;
            for (int i = 0; i < width && keepSearching; ++i)
            {
                for (int j = 0; j < height && keepSearching; j++)
                {
                    var item = board.GetItemByCoord(i, j);
                    if (item != null && item.isActive && !ItemUtility.HasBubbleComponent(item))
                    {
                        fallback ??= item;
                        if (item.HasComponent(ItemComponentType.Bonus) ||
                            item.HasComponent(ItemComponentType.TapBonus) ||
                            item.HasComponent(ItemComponentType.Chest) ||
                            item.HasComponent(ItemComponentType.Dying) ||
                            item.HasComponent(ItemComponentType.ClickSouce) ||
                            item.HasComponent(ItemComponentType.AutoSouce))
                        {
                            continue;
                        }
                        // 找到较优方案
                        fallback = item;
                        keepSearching = false;
                    }
                }
            }

            if (fallback == null)
            {
                DebugEx.Warning("[GUIDE] no valid item for bag guide");
                mIsWaiting = false;
                return;
            }

            // bag path
            var bagTrans = Game.Manager.guideMan.FindByPath(param);
            if (bagTrans == null)
            {
                DebugEx.Warning("[GUIDE] bag not found");
                mIsWaiting = false;
                return;
            }
            // 显示mask
            Game.Manager.guideMan.ActiveGuideContext?.ShowBoardCommonMask(new List<int>() { fallback.tid }, new List<Transform>() { bagTrans }, 0.5f);
            // 显示拖拽手
            var sp = GuideUtility.CalcRectTransformScreenPos(null, bagTrans);
            var coord = BoardUtility.GetRealCoordByScreenPos(sp);
            EL.MessageCenter.Get<MSG.UI_GUIDE_FINGER_DRAG_POS>().Dispatch(fallback, coord);

            Game.Manager.guideMan.OnItemPutIntoInventory -= _OnItemPutIntoInventory;
            Game.Manager.guideMan.OnItemPutIntoInventory += _OnItemPutIntoInventory;
            Game.Manager.guideMan.ActiveGuideContext?.SetRedirector(BoardViewManager.Instance.boardView.boardDrag);
            BoardViewManager.Instance.Guide_OverrideDraggableItemBehaviour(0, 0, MergeHelper.MergeAction.Inventory);
            mIsWaiting = true;
        }
    }
}