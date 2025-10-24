// ===================================================
// Author: mengqc
// Date: 2025/09/26
// ===================================================

using EL;
using UnityEngine;
using Enum = System.Enum;

namespace FAT
{
    interface IGuideActEntryMaskShow
    {
        bool IsActEntryMaskCanShow();
    }

    public class GuideActImpEntryMaskShow : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (param.Length < 1)
            {
                return;
            }

            Enum.TryParse<fat.rawdata.EventType>(param[0], out var eventType);
            var act = Game.Manager.activity.LookupAny(eventType);
            if (act is not IGuideActEntryMaskShow guideAct) return;
            if (!guideAct.IsActEntryMaskCanShow()) return;
            MessageCenter.Get<MSG.ACTIVITY_QUERY_ENTRY>().Dispatch(act, OnEntryFound);

            mIsWaiting = false;
        }

        private void OnEntryFound(Transform target)
        {
            if (target == null) return;
            Game.Manager.guideMan.ActionShowMask(target);
            MessageCenter.Get<MSG.UI_ORDER_REQUEST_SCROLL>().Dispatch(target);
        }
    }
}