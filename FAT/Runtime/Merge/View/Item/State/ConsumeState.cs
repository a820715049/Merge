/*
 * @Author: qun.chao
 * @Date: 2024-01-10 11:19:53
 */
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class MergeItemConsumeState : MergeItemBaseState
    {
        private ItemInteractContext context;

        private float minDist = 1f;
        private float dampping = 10f;
        private Vector2 targetPos;
        private Item dstItem;

        public MergeItemConsumeState(MBItemView v) : base(v)
        { }

        public override void OnEnter()
        {
            base.OnEnter();

            context = view.interactContext;
            dstItem = context.dst.data;

            BoardViewManager.Instance.ReAnchorItemForMove(view.transform);
            targetPos = BoardUtility.CalcItemLocalPosInMoveRootByCoord(dstItem.coord);
        }

        public override ItemLifecycle Update(float dt)
        {
            var dst = dstItem;
            if (dst == null)
            {
                return ItemLifecycle.Die;
            }

            var trans = view.transform as RectTransform;
            trans.anchoredPosition = Vector2.Lerp(trans.anchoredPosition, targetPos, dampping * dt);

            if (Mathf.Abs(trans.anchoredPosition.x - targetPos.x) < minDist && Mathf.Abs(trans.anchoredPosition.y - targetPos.y) < minDist)
                return ItemLifecycle.Die;

            return base.Update(dt);
        }
    }
}