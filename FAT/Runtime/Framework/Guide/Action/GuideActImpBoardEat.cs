using UnityEngine;
using FAT.Merge;
namespace FAT
{
    public class GuideActImpBoardEat : GuideActImpBase
    {
        public int fromId;
        public int toId;
        public override void Play(string[] param)
        {
            fromId = Mathf.RoundToInt(float.Parse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            Item fromItem = BoardViewManager.Instance.FindItem(fromId, false);
            toId = Mathf.RoundToInt(float.Parse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
            Item toItem = BoardViewManager.Instance.FindItem(toId, false);
            if (fromItem != null && toItem != null)
            {
                mIsWaiting = true;
                Game.Manager.guideMan.OnItemConsume -= _OnItemEat;
                Game.Manager.guideMan.OnItemConsume += _OnItemEat;
                EL.MessageCenter.Get<MSG.UI_GUIDE_FINGER_DRAG>().Dispatch(fromItem, toItem);
                BoardViewManager.Instance.Guide_OverrideDraggableItemBehaviour(fromItem.tid, 0,
                    MergeHelper.MergeAction.None);
                return;
            }
            mIsWaiting = false;
        }
        private void _OnItemEat(Item src, Item dst)
        {
            mIsWaiting = false;
            Game.Manager.guideMan.OnItemConsume -= _OnItemEat;
            Game.Manager.guideMan.ActiveGuideContext?.SetRedirector(null);
            BoardViewManager.Instance.Guide_OverrideDraggableItemBehaviour();
        }
    }
}
