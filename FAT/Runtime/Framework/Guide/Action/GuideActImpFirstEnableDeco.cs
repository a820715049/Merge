/*
 *@Author:chaoran.zhang
 *@Desc:新手引导：找到装饰页面第一个可以解锁的装饰
 *@Created Time:2024.06.05 星期三 10:49:33
 */

using UnityEngine;

namespace FAT
{
    public class GuideActImpFirstEnableDeco : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            if (param.Length < 3)
            {
                _StopWait();
                Debug.LogError("[GUIDE] first_enable_deco params less than 3");
                return;
            }

            float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var angle);
            bool block = param[1].Contains("true");
            bool mask = param[2].Contains("true");
            float.TryParse(param[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var offset);
            var ui = UIManager.Instance.TryGetUI(Game.Manager.decorateMan.Panel) as UIDecoratePanel;
            if (ui == null)
            {
                _StopWait();
                return;
            }

            Transform trans = null;
            trans = ui.FindFirstEnable();
            if (trans == null)
            {
                Game.Manager.guideMan.ActionSaveProgress();
                mIsWaiting = false;
                Game.Manager.guideMan.DropGuide();
                Debug.LogError("[GUIDE] can't find enable decoration");
                return;
            }

            Game.Manager.guideMan.ActiveGuideContext?.ShowPointerPro(trans, block, mask, _StopWait, offset);
            Game.Manager.guideMan.ActiveGuideContext?.SetAngleOffset(angle);
        }
    }
}