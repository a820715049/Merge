/*
 * @Author: qun.chao
 * @Date: 2025-07-30 16:25:15
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpClawOrderMaskShow : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (Game.Manager.activity.LookupAny(fat.rawdata.EventType.ClawOrder) is not ActivityClawOrder act)
                return;
            if (act.SelectedOrderId > 0)
            {
                EL.MessageCenter.Get<MSG.UI_ORDER_QUERY_RANDOMER_TRANSFORM>().Dispatch(act.SelectedOrderId, _OnOrderFound);
            }
            mIsWaiting = false;
        }

        private void _OnOrderFound(Transform order)
        {
            if (order != null)
            {
                Game.Manager.guideMan.ActionShowMask(order);
                EL.MessageCenter.Get<MSG.UI_ORDER_REQUEST_SCROLL>().Dispatch(order);
            }
        }
    }
}