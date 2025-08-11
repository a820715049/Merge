/*
 * @Author: qun.chao
 * @Date: 2021-02-22 17:20:49
 */
namespace FAT
{
    using UnityEngine;
    using DG.Tweening;
    using Merge;

    public class MergeItemMergeState : MergeItemBaseState
    {
        private bool isTweenFinished;

        private Tween tweenA;
        private Tween tweenB;

        private ItemInteractContext context;
        private int mSrcId;
        private int mDstId;

        public MergeItemMergeState(MBItemView v) : base(v)
        {}

        public override void OnEnter()
        {
            base.OnEnter();

            view.TryResolveNewItemTip();

            context = view.interactContext;

            mSrcId = context.src.data.id;
            mDstId = context.dst.data.id;

            BoardViewManager.Instance.ReAnchorItemForMove(context.src.transform);
            BoardViewManager.Instance.ReAnchorItemForMove(context.dst.transform);

            var coord = view.data.coord;
            BoardViewManager.Instance.HoldItem(coord.x, coord.y, view.transform as RectTransform);
            view.gameObject.SetActive(true);

            // 置顶
            context.dst.transform.SetAsLastSibling();

            // _PlayMove();
            _PlayMergeAnim();
        }

        public override void OnLeave()
        {
            base.OnLeave();

            tweenA?.Kill(false);
            tweenB?.Kill(false);

            tweenA = null;
            tweenB = null;

            // 尝试不重置位置
            // var coord = view.data.coord;
            // BoardViewManager.Instance.HoldItem(coord.x, coord.y, view.transform as RectTransform);

            // release
            BoardViewManager.Instance.ReleaseItem(mSrcId);
            BoardViewManager.Instance.ReleaseItem(mDstId);

            view.transform.localScale = Vector3.one;
        }

        public override ItemLifecycle Update(float dt)
        {
            if (isTweenFinished)
                return ItemLifecycle.Idle;
            return base.Update(dt);
        }

        private void _PlayMergeAnim()
        {
            isTweenFinished = false;

            // 隐藏 等待merge演出
            view.transform.localScale = Vector3.zero;

            // 生成
            var seq = DOTween.Sequence();
            seq.SetDelay(0.1f);
            seq.Append(view.transform.DOScale(Vector3.one * 1.2f, 0.2f).From(Vector3.one * 0.2f, true));
            seq.Append(view.transform.DOScale(Vector3.one, 0.1f));
            seq.OnComplete(_OnMergeComplete);
            tweenA = seq;
            seq.Play();

            var cellSize = BoardUtility.cellSize;
            var coord = view.data.coord;
            var targetPos = BoardUtility.CalcItemLocalPosInMoveRootByCoord(view.data.coord);

            // 合成双方
            seq = DOTween.Sequence();
            seq.Append((context.src.transform as RectTransform).DOAnchorPos(targetPos, 0.1f));
            seq.Join((context.dst.transform as RectTransform).DOAnchorPos(targetPos, 0.1f));
            seq.Append(context.src.transform.DOScale(Vector3.zero, 0.1f));
            seq.Join(context.dst.transform.DOScale(Vector3.zero, 0.1f));
            tweenB = seq;
            seq.Play();
        }

        private void _OnMergeComplete()
        {
            isTweenFinished = true;
        }
    }
}