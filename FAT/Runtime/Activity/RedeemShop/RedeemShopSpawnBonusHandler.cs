/*
 * @Author: yanfuxing
 * @Date: 2025-05-20 11:20:05
 */

namespace FAT.Merge
{
    public class RedeemShopSpawnBonusHandler : ISpawnBonusHandler
    {
        public int _priority;
        private ActivityRedeemShopLike _activityRedeemShop;

        public RedeemShopSpawnBonusHandler(ActivityRedeemShopLike activity)
        {
            _activityRedeemShop = activity;
        }

        int ISpawnBonusHandler.priority => _priority;        //越小越先出
        void ISpawnBonusHandler.OnRegister()
        {
        }

        void ISpawnBonusHandler.OnUnRegister()
        {
        }

        void ISpawnBonusHandler.Process(SpawnBonusContext context)
        {
            //和能量加倍无关 
            // 1. 点击耗体生成器，可收集积分
            // a. 耗1体=1积分；
            // b. 能量加倍时，耗2体=2积分
            if (context.from != null && context.energyCost > 0)
            {
                context.from.TryGetItemComponent<ItemClickSourceComponent>(out var comp, true);
                var score = context.energyCost;
                if (_activityRedeemShop != null)
                {
                    if (_activityRedeemShop.EventRedeemConfig != null && Constant.kMergeEnergyObjId == comp.firstCost.Cost)
                        _activityRedeemShop.UpdateMilestoneScore(score);
                }
            }
        }
    }
}