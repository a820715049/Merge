/*
*@Author:chaoran.zhang
*@Desc:根据参数id，添加一个物品到奖励箱中，并置于奖励队列到最前端
*@Created Time:2024.01.22 星期一 18:37
*/
using static fat.conf.Data;

namespace FAT
{
    public class GuideActImpAddReward: GuideActImpBase
    {
        public override void Play(string[] param)
        {
            int.TryParse(param[0], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var objID);
            int count = 1;
            if (param.Length == 2)
            {
                int.TryParse(param[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out count);
            }
            var conf = GetObjBasic(objID);
            if (conf != null)
            {
                var data = Game.Manager.rewardMan.BeginReward(conf.Id, count, ReasonString.card, RewardFlags._IsPriority);
                Game.Manager.rewardMan.CommitReward(data);
            }

            mIsWaiting = false;
        }
        
    }
}