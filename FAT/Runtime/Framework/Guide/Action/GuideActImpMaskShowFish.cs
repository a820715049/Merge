// ================================================
// File: GuideActImpMaskShowFish.cs
// Author: yueran.li
// Date: 2025/04/16 20:27:50 星期三
// Desc: 遮罩显示鱼图鉴所在位置
// ================================================


using UnityEngine;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class GuideActImpMaskShowFish : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (!Game.Manager.activity.LookupAny(EventType.Fish, out var acti) || acti is not ActivityFishing a)
            {
                return;
            }

            // 获取主界面中图鉴位置对应transform
            UIManager.Instance.TryGetCache(UIConfig.UIActivityFishMain, out var ui);
            if (ui == null) return;
            var fishMain = (UIActivityFishMain)ui;

            // 获得解锁图鉴所在位置
            RectTransform trans = null;

            foreach (var fishInfo in a.FishInfoList)
            {
                if (a.IsFishUnlocked(fishInfo.Id))
                {
                    trans = (fishMain).FindFishItem(fishInfo.Id);
                    break;
                }
            }

            if (trans != null)
            {
                Game.Manager.guideMan.ActionShowMask(trans);
            }

            mIsWaiting = false;
        }
    }
}