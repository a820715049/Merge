/*
 * @Author: qun.chao
 * @Date: 2023-10-26 14:54:02
 */
using UnityEngine;
using EL;

namespace FAT.Merge
{
    public class MBItemUsageUndo : MBItemUsageBase
    {
        protected override void OnBtnClick()
        {
            if (BoardViewManager.Instance.board.UndoSellItem())
            {
                MessageCenter.Get<MSG.UI_BOARD_SELECT_ITEM>().Dispatch(null);
                // 撤销音效
                Game.Manager.audioMan.TriggerSound("BoardUndo");
            }
        }
    }
}