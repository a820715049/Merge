/*
 * @Author: qun.chao
 * @Date: 2024-07-03 11:30:24
 */
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    public class ItemSpecialBoxComponent : ItemComponentBase
    {
        public bool willDead => mItemCount <= 1;
        // public int energyCost => Constant.kMergeEnergyCostNum;
        public bool canOutput => mItemCount > 0;
        public ComMergeSpecialBox config => mConfig;
        private ComMergeSpecialBox mConfig = null;
        private int mItemCount = 0;

        public static bool Validate(ItemComConfig config)
        {
            return config?.specialBoxConfig != null;
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).specialBoxConfig;
            mItemCount = config.LimitCount;
        }

        public override void OnSerialize(MergeItem itemData)
        {
            base.OnSerialize(itemData);
            itemData.ComSpecialBox = new ComSpecialBox
            {
                ItemCount = mItemCount,
            };
        }

        public override void OnDeserialize(MergeItem itemData)
        {
            base.OnDeserialize(itemData);
            if (itemData.ComSpecialBox != null)
            {
                mItemCount = itemData.ComSpecialBox.ItemCount;
            }
        }

        public int ConsumeNextItem()
        {
            var itemId = Game.Manager.mergeItemDifficultyMan.CalcSpecialBoxOutput(config.ActDiffRange[0], config.ActDiffRange[1]);
            --mItemCount;
            return itemId;
        }
    }
}