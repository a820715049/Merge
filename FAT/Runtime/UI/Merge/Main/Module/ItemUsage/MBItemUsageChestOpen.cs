/*
 * @Author: qun.chao
 * @Date: 2023-11-29 11:44:57
 */
using UnityEngine;

namespace FAT.Merge
{
    public class MBItemUsageChestOpen : MBItemUsageBase
    {
        protected override void OnBtnClick()
        {
            if (mItem.TryGetItemComponent(out ItemChestComponent chest))
            {
                chest.StartWait();
                Game.Manager.audioMan.TriggerSound("UnlockNow");
            }
        }
    }
}