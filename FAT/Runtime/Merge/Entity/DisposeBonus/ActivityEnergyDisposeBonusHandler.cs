/*
 * @Author: qun.chao
 * @Date: 2023-10-07 11:33:24
 */
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    /*
    用于在活动期间对ActivityEnergy组件进行结算
    */
    public class ActivityEnergyDisposeBonusHandler : IDisposeBonusHandler
    {
        private int activityId;
        public int priority;

        int IDisposeBonusHandler.priority => priority;        //越小越先出

        void IDisposeBonusHandler.OnRegister() { }

        void IDisposeBonusHandler.OnUnRegister() { }

        void IDisposeBonusHandler.Process(DisposeBonusContext context)
        {
            // var dt = context.deadType;
            // if (dt == ItemDeadType.BubbleTimeout || dt == ItemDeadType.None)
            // {
            //     // 气泡超时不能兑换体力奖励
            //     return;
            // }
            // if (!context.item.TryGetItemComponent(out ItemActivityComponent com) || com.activityId != activityId)
            // {
            //     return;
            // }
            // var data = context.world.CollectActivityEnergy(com, context.dieToTarget);
            // var fallback = data?.GrabReward();
            // if (fallback != null)
            // {
            //     Game.Instance.rewardMan.CommitReward(fallback);
            // }
        }

        public void InitConfig(int activityId)
        {
            this.activityId = activityId;
        }
    }
}