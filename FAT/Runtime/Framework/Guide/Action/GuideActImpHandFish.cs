using System.Globalization;
using UnityEngine;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class GuideActImpHandFish : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            // 参考 handProp 和 挖矿
            if (param.Length < 4)
            {
                _StopWait();
                Debug.LogError("[GUIDE] hand_fish params less than 4");
                return;
            }

            if (!Game.Manager.activity.LookupAny(EventType.Fish, out var acti) || acti is not ActivityFishing a)
            {
                return;
            }

            // 获取主界面中图鉴位置对应transform
            UIManager.Instance.TryGetCache(UIConfig.UIActivityFishMain, out var ui);
            if (ui == null) return;
            var fishMain = (UIActivityFishMain)ui;

            // 获得解锁图鉴所在位置
            RectTransform handBook = null;

            foreach (var fishInfo in a.FishInfoList)
            {
                if (a.IsFishUnlocked(fishInfo.Id))
                {
                    handBook = (fishMain).FindFishItem(fishInfo.Id);
                    break;
                }
            }

            // 旋转角度; block; mask; offset
            float.TryParse(param[0], NumberStyles.Float,
                CultureInfo.InvariantCulture, out var angle);
            var block = param[1].Contains("true");
            var mask = param[2].Contains("true");
            float.TryParse(param[3], NumberStyles.Float,
                CultureInfo.InvariantCulture, out var offset);

            // 3.显示手指
            if (handBook != null)
            {
                Game.Manager.guideMan.ActiveGuideContext?.ShowPointerPro(handBook, block, mask, _StopWait, offset);
                Game.Manager.guideMan.ActiveGuideContext?.SetAngleOffset(angle);
            }
            else
            {
                _StopWait();
                Debug.LogError("[GUIDE] hand_fish path fail");
            }
        }
    }
}