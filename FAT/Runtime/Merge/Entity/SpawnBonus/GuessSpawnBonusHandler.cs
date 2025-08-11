/**
 * @Author: zhangpengjian
 * @Date: 2025/2/12 14:38:49
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/2/12 14:38:49
 * Description: 猜颜色活动消耗体力后 生成对应积分
 */

namespace FAT.Merge
{
    public class GuessSpawnBonusHandler : ISpawnBonusHandler
    {
        public int priority;
        private ActivityGuess activityGuess;

        public GuessSpawnBonusHandler(ActivityGuess activity)
        {
            activityGuess = activity;
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
                if (activityGuess != null)
                {
                    if (activityGuess.confD != null && activityGuess.confD.Cost == comp.firstCost.Cost)
                        activityGuess.AddScore(score);
                }
            }
        }
    }
}