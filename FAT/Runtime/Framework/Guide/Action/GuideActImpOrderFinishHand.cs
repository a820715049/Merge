/*
 * @Author: qun.chao
 * @Date: 2024-09-04 17:12:37
 */
using System.Linq;
using UnityEngine;
using EL;

namespace FAT
{
    public class GuideActImpOrderFinishHand : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            if (param.Length < 1)
            {
                // 任意可完成订单
                MessageCenter.Get<MSG.UI_ORDER_QUERY_COMMON_FINISHED_TRANSFORM>().Dispatch(-1, _OnOrderFound);
            }
            else
            {
                // 指定id的可完成订单
                float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value);
                // 任意可完成订单
                MessageCenter.Get<MSG.UI_ORDER_QUERY_COMMON_FINISHED_TRANSFORM>().Dispatch(Mathf.RoundToInt(value), _OnOrderFound);
            }
        }

        private void _OnOrderFound(Transform order)
        {
            if (order?.TryGetComponent<MBBoardOrder>(out var bo) != true)
            {
                _StopWait();
                Debug.LogError("[GUIDE] order finish hand fail");
                return;
            }
            Game.Manager.guideMan.ActiveGuideContext?.ShowPointerPro(bo.FindFinishButton(), true, true, _StopWait, 0.5f);
            Game.Manager.guideMan.ActiveGuideContext?.SetAngleOffset(0f);
        }
    }
}