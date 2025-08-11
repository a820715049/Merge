/**
 * @Author: zhangpengjian
 * @Date: 2024-05-15 18:45:17
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/9/27 16:49:58
 * Description: 消耗体力后 生成对应积分
 */

namespace FAT.Merge
{
    public class DiggingSpawnBonusHandler : ISpawnBonusHandler
    {
        public int priority;
        private ActivityDigging activityDigging;

        public DiggingSpawnBonusHandler(ActivityDigging activity)
        {
            activityDigging = activity;
        }

        int ISpawnBonusHandler.priority => priority;        //越小越先出
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
                if (activityDigging != null)
                {
                    if (activityDigging.diggingConfig != null && activityDigging.diggingConfig.Cost == comp.firstCost.Cost)
                        activityDigging.AddScore(score);
                }
            }
        }
    }
}