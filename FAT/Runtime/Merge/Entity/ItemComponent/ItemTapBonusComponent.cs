/*
 * @Author: tang.yan
 * @Description: ComTapBonus组件 点击该棋子会戳破气泡获得其中奖励
 * @Date: 2024-08-14 14:08:23
 */
using EL;
using fat.rawdata;

namespace FAT.Merge
{
    public class ItemTapBonusComponent: ItemComponentBase
    {
        public int bonusId => mConfig.ItemId;
        public int bonusCount => mConfig.Count;
        public FuncType funcType => mConfig.FuncType;
        private ComTapBonus mConfig;

        public static bool Validate(ItemComConfig config)
        {
            return config?.tapBonusConfig != null;
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).tapBonusConfig;
        }
    }
}