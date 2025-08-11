using System.Linq;
using FAT.Merge;
using UnityEngine;

namespace FAT
{
    public class GuideActImpHandMine : GuideActImpBase
    {

        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            var act = Game.Manager.activity.LookupAny(fat.rawdata.EventType.Mine) as MineBoardActivity;
            if (act == null)
            {
                _StopWait();
                Debug.LogError("[GUIDE] mine path fail");
                return;
            }
            Item target = null;
            foreach (var item in act.ConfD.BonusItemMax)
            {
                target = BoardViewManager.Instance.FindItem(item, false);
                if (target != null)
                {
                    break;
                }
            }
            if (target != null)
            {
                Game.Manager.guideMan.ActionShowBoardUseTarget(target.coord.x, target.coord.y, _StopWait, false);
            }
            else
            {
                mIsWaiting = false;
            }
        }
    }
}