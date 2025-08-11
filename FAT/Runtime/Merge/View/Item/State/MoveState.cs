/*
 * @Author: qun.chao
 * @Date: 2021-02-19 16:06:56
 */
namespace FAT
{
    using UnityEngine;
    using Merge;

    public class MergeItemMoveState : MergeItemBaseState
    {
        private float minDist = 1f;
        private float dampping = 10f;
        private Vector2 targetPos;

        public MergeItemMoveState(MBItemView v) : base(v)
        {}

        public override void OnEnter()
        {
            base.OnEnter();

            BoardViewManager.Instance.ReAnchorItemForMove(view.transform);
            // targetPos = BoardUtility.CalcItemLocalPosInMoveRootByCoord(view.data.coord);
        }

        public override void OnLeave()
        {
            base.OnLeave();

            var coord = view.data.coord;
            BoardViewManager.Instance.HoldItem(coord.x, coord.y, view.transform as RectTransform);
        }

        public override ItemLifecycle Update(float dt)
        {
            var trans = view.transform as RectTransform;
            // move状态允许灵活移动 | 比如在落地前被其他棋子挤离位置
            targetPos = BoardUtility.CalcItemLocalPosInMoveRootByCoord(view.data.coord);
            trans.anchoredPosition = Vector2.Lerp(trans.anchoredPosition, targetPos, dampping * dt);

            if (Mathf.Abs(trans.anchoredPosition.x - targetPos.x) < minDist && Mathf.Abs(trans.anchoredPosition.y - targetPos.y) < minDist)
                return ItemLifecycle.Idle;

            return base.Update(dt);
        }
    }
}