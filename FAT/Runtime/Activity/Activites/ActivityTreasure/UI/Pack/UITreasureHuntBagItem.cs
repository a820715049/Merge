/*
 * @Author: qun.chao
 * @Date: 2024-04-19 16:10:57
 */
using UnityEngine;
using Config;
using EL;

namespace FAT
{
    public class UITreasureHuntBagItem : UIGenericItemBase<RewardConfig>
    {
        private UICommonItem item;

        protected override void InitComponents()
        {
            item = transform.GetComponent<UICommonItem>();
            item.Setup();
        }

        protected override void UpdateOnDataChange()
        {
            item.Refresh(mData);
        }
    }
}