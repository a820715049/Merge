/*
 * @Author: qun.chao
 * @Date: 2021-05-26 10:03:42
 */
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class GuideActImpBoardSelectHand : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            int tid = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            var isBubble = false;
            //第二个参数代表是否包含泡泡棋子，0表示不包含，1表示包含
            if (param.Length > 1)
            {
                var needBubble = Mathf.RoundToInt(float.Parse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
                isBubble = needBubble > 0;
            }
            //新增3个参数时的处理逻辑 会尝试覆盖param[0]对应的tid
            if (param.Length > 2)
            {
                var extraParam = param[2];
                if (extraParam == "FrozenItem")
                {
                    var frozenItemId = BoardViewManager.Instance.GetFirstFrozenItemId();
                    if (frozenItemId > 0)
                    {
                        tid = frozenItemId;
                    }
                }
            }
            Item target = BoardViewManager.Instance.FindItem(tid, isBubble);
            if (target != null)
            {
                bool showMask = false;
                Game.Manager.guideMan.ActionShowBoardSelectTarget(target.coord.x, target.coord.y, _StopWait, showMask);
            }
            else
            {
                mIsWaiting = false;
            }
        }
    }
}