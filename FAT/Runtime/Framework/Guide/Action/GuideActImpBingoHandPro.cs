using System.Linq;
using fat.gamekitdata;
using UnityEngine;
using UnityEngine.Animations;

namespace FAT
{
    public class GuideActImpBingoHandPro : GuideActImpBase
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
                Debug.LogError("[GUIDE] Bingo hand_pro params less than 4");
                return;
            }
            if (Game.Manager.activity.LookupAny(fat.rawdata.EventType.ItemBingo, out var act) && act is ActivityBingo)
            {
                var actInst = act as ActivityBingo;
                if (actInst == null)
                {
                    _StopWait();
                    Debug.LogError("[GUIDE] Bingo hand_pro activity not found");
                    return;
                }
                float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var angle);
                bool block = param[1].Contains("true");
                bool mask = param[2].Contains("true");
                float.TryParse(param[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var offset);
                var trans = Game.Manager.guideMan.FindByPath(param.Skip(4).ToList());
                if (trans != null)
                {
                    var index = actInst.FindFirstSubmitItem();
                    if (index == -1)
                    {
                        _StopWait();
                        Debug.LogError("[GUIDE] Bingo hand_pro index not found");
                        return;
                    }
                    trans = trans.GetChild(index);
                    Game.Manager.guideMan.ActiveGuideContext?.ShowPointerPro(trans, block, mask, _StopWait, offset);
                    Game.Manager.guideMan.ActiveGuideContext?.SetAngleOffset(angle);
                }
                else
                {
                    _StopWait();
                    Debug.LogError("[GUIDE] Bingo hand_pro path fail");
                }
            }
            else
            {
                _StopWait();
                Debug.LogError("[GUIDE] Bingo hand_pro activity not found");
            }
        }
    }
}