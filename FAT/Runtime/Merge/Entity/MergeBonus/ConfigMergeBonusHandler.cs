/**
 * @Author: handong.liu
 * @Date: 2023-02-17 10:30:35
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;


namespace FAT.Merge
{
    public class ConfigMergeBonusHandler : IMergeBonusHandler
    {
        int IMergeBonusHandler.priority => 100;
        void IMergeBonusHandler.Process(Merge.MergeBonusContext context)
        {
            if (Env.Instance.CanMergeProduceCoin())
            {
                var result = context.result;
                var config = Env.Instance.GetItemMergeConfig(result.tid);
                foreach (var bonus in config.MergeBonus)
                {
                    if (bonus > 0)
                    {
                        //尝试往棋盘上发棋子 如果棋盘满了发不上 则发到奖励箱
                        var bonusItem = context.world.activeBoard.SpawnItemMustWithReason(bonus, ItemSpawnContext.CreateWithSource(result, ItemSpawnContext.SpawnType.None), result.coord.x, result.coord.y, false, false);
                        if (bonusItem == null)
                        {
                            var d = Game.Manager.rewardMan.BeginReward(bonus, 1, ReasonString.merge_item);
                            var pos = BoardUtility.GetWorldPosByCoord(result.coord);
                            UIFlyUtility.FlyReward(d, pos);
                        }
                    }
                }
            }
        }
        void IMergeBonusHandler.OnRegister()
        {

        }

        void IMergeBonusHandler.OnUnRegister()
        {
            
        }
    }
}