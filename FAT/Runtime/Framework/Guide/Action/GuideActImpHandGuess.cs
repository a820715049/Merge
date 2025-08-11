using System.Linq;
using UnityEngine;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class GuideActImpHandGuess : GuideActImpBase
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
                Debug.LogError("[GUIDE] hand_pro params less than 3");
                return;
            }

            float.TryParse(param[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var angle);
            var block = param[1].Contains("true");
            var mask = param[2].Contains("true");
            float.TryParse(param[3], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var offset);
            Game.Manager.activity.LookupAny(EventType.Guess, out var act);
            var actInst = act as ActivityGuess;
            var trans = actInst?.PutRepeatedItemTarget;
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