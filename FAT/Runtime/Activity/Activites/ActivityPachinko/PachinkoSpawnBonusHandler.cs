/*
 *@Author:chaoran.zhang
 *@Desc:pachinko活动消耗体力获得代币
 *@Created Time:2024.12.12 星期四 14:21:26
 */

using FAT.Merge;

namespace FAT
{
    public class PachinkoSpawnBonusHandler : ISpawnBonusHandler
    {
        public int priority;
        private ActivityPachinko activityPachinko;

        public PachinkoSpawnBonusHandler(ActivityPachinko activity)
        {
            activityPachinko = activity;
        }

        int ISpawnBonusHandler.priority => priority; //越小越先出

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
                var id = activityPachinko.Conf.RequireScoreId;
                if (activityPachinko != null)
                    if (activityPachinko.Conf != null && activityPachinko.Conf.Cost == comp.firstCost.Cost)
                        Game.Manager.rewardMan.BeginReward(id, score, ReasonString.pachinko_energy);
            }
        }
    }
}