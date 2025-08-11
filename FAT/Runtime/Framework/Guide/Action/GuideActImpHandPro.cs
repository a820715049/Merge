/*
 * @Author: qun.chao
 * @Date: 2023-09-07 11:47:37
 */

using System.Linq;
using UnityEngine;

namespace FAT
{
    public class GuideActImpHandPro : GuideActImpBase
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
                Debug.LogError("[GUIDE] hand_pro params less than 4");
                return;
            }

            float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var angle);
            bool block = param[1].Contains("true");
            bool mask = param[2].Contains("true");
            float.TryParse(param[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var offset);
            var trans = Game.Manager.guideMan.FindByPath(param.Skip(4).ToList());
            if (trans != null)
            {
                Game.Manager.guideMan.ActiveGuideContext?.ShowPointerPro(trans, block, mask, _StopWait, offset);
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