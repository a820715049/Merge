/*
 * @Author: qun.chao
 * @Date: 2023-10-26 14:35:17
 */
using UnityEngine;

namespace FAT.Merge
{
    public class MBItemUsageDelete : MBItemUsageBase
    {
        protected override void OnBtnClick()
        {
            base.OnBtnClick();
            var _item = mItem;
            if (BoardViewManager.Instance.board.SellItem(mItem))
            {
                if (_item != null)
                {
                    Debug.LogFormat("sell suc {0}", _item.id);
                    // BoardViewManager.Instance.ShowSellTip(_item.coord);
                }
            }
            else
            {
                if (_item != null)
                    Debug.LogWarningFormat("sell fail {0}", _item.id);
            }
        }
    }
}