/*
 * @Author: qun.chao
 * @Date: 2023-11-29 19:41:49
 */
using System.Linq;
using UnityEngine;

namespace FAT
{
    public class GuideActImpHandProScene : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            if (param.Length < 4)
            {
                _StopWait();
                Debug.LogError("[GUIDE] hand_pro_scene params less than 4");
                return;
            }
            float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var angle);
            bool block = param[1].Contains("true");
            bool mask = param[2].Contains("true");
            var trans = Game.Manager.guideMan.FindByPathForSceneUI(param.Skip(3).ToList());
            if (trans != null)
            {
                Game.Manager.guideMan.ActiveGuideContext?.ShowPointerPro(trans, block, mask, _StopWait);
                Game.Manager.guideMan.ActiveGuideContext?.SetAngleOffset(angle);
            }
            else
            {
                _StopWait();
                Debug.LogError("[GUIDE] hand_pro path fail");
            }
        }
    }
}