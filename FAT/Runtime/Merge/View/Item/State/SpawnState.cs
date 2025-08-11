/*
 * @Author: qun.chao
 * @Date: 2021-02-22 17:03:02
 */
namespace FAT
{
    using UnityEngine;
    using DG.Tweening;
    using Merge;

    public class MergeItemSpawnState : MergeItemBaseState
    {
        private bool isTweenFinished;
        private Tween scaleTween;

        public MergeItemSpawnState(MBItemView v) : base(v)
        { }

        public override void OnEnter()
        {
            base.OnEnter();

            var coord = view.data.coord;
            BoardViewManager.Instance.HoldItem(coord.x, coord.y, view.transform as RectTransform);
            view.gameObject.SetActive(true);

            isTweenFinished = false;
            scaleTween = view.transform.DOScale(Vector3.one, 0.3f).From(Vector3.zero, true).SetEase(Ease.InOutBack).OnComplete(_OnTweenFinished);
        }

        public override void OnLeave()
        {
            base.OnLeave();
            scaleTween?.Kill();
            scaleTween = null;

            isTweenFinished = true;
            view.transform.localScale = Vector3.one;
            if (view.spawnContext.type == ItemSpawnContext.SpawnType.Bubble)
                view.AddOnBoardEffectForBubble();
            else
                view.AddOnBoardEffect();
        }

        public override ItemLifecycle Update(float dt)
        {
            if (isTweenFinished)
                return ItemLifecycle.Idle;
            return base.Update(dt);
        }

        private void _OnTweenFinished()
        {
            isTweenFinished = true;
        }
    }
}