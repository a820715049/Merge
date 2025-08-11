/**
 * @Author: handong.liu
 * @Date: 2021-02-23 11:29:22
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;

namespace FAT.Merge
{
    public class ItemBonusCompoent: ItemComponentBase
    {
        public int bonusId => mConfig.BonusId;
        public int bonusCount => mConfig.BonusCount;
        public FuncType funcType => mConfig.FuncType;
        public bool autoUse => mConfig.AutoPickUp == 1;
        public bool scaleAnim => mConfig.IsScaleAnim;
        public bool inventoryAutoUse => mConfig.IsInventoryAutoUse;
        private ComMergeBonus mConfig;

        public static bool Validate(ItemComConfig config)
        {
            return config?.bonusConfig != null;
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).bonusConfig;

            _TryAutoUse();
        }

        protected override void OnUpdate(int dt)
        {
            _TryAutoUse();
        }

        private void _TryAutoUse()
        {
            if (autoUse && item.parent != null && !item.isDead && item.isActive)
            {
                DebugEx.FormatInfo("Merge::ItemBonusComponent auto use ----> {0}", item.tid);
                item.parent.UseBonusItem(item);
            }
        }
    }
}