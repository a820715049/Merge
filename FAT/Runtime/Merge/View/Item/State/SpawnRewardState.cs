/*
 * @Author: qun.chao
 * @Date: 2021-03-08 12:01:53
 */

namespace FAT
{
    using UnityEngine;
    using DG.Tweening;
    using Merge;

    public class MergeItemSpawnRewardState : MergeItemBaseState
    {
        private bool isTweenFinished;
        private Tween scaleTween;

        private Vector2 p0;
        private Vector2 p1;
        private Vector2 p2;
        private float tweenTime = 0.4f;
        private float totalTime;

        public MergeItemSpawnRewardState(MBItemView v) : base(v)
        { }

        public override void OnEnter()
        {
            base.OnEnter();

            view.gameObject.SetActive(true);
            BoardViewManager.Instance.ReAnchorItemForMove(view.transform);

            var (fromPosInBoard, delay) = BoardUtility.GetRequestedSpawnPos(view);
            // 拖尾效果需要落点不能偏移
            var ignoreOffset = view.spawnContext?.type == ItemSpawnContext.SpawnType.OrderLike ||
                view.spawnContext?.type == ItemSpawnContext.SpawnType.OrderRate ||
                view.spawnContext?.type == ItemSpawnContext.SpawnType.Fight ||
                view.spawnContext?.type == ItemSpawnContext.SpawnType.WishBoard ||
                view.spawnContext?.type == ItemSpawnContext.SpawnType.MineCart;
            var (a, b, c) = BoardUtility.CalcBezierControlPos(BoardUtility.GetRealCoordByBoardPos(fromPosInBoard), view.data.coord, ignoreOffset);
            p0 = BoardUtility.CalcItemLocalPosInMoveRoot(a);
            p1 = BoardUtility.CalcItemLocalPosInMoveRoot(b);
            p2 = BoardUtility.CalcItemLocalPosInMoveRoot(c);

            (view.transform as RectTransform).anchoredPosition = p0;
            view.transform.localScale = Vector3.one;
            totalTime = tweenTime;
            isTweenFinished = false;

            if (view.spawnContext?.spawnEffect != null)
            {
                if (view.spawnContext.spawnEffect is ISpawnEffectWithTrail trailEff)
                {
                    view.transform.localScale = Vector3.zero;
                    scaleTween = view.transform.DOScale(1f, tweenTime).SetDelay(delay).SetEase(Ease.InOutSine).OnComplete(_OnTweenFinished);
                    totalTime += delay;
                    trailEff.AddTrail(view, scaleTween);
                }
            }
            else
            {
                // TODO: delay通用化 应该让常规领奖也能应用delay
                // setdelay / setfrom
                if (delay > 0)
                {
                    totalTime += delay;
                    view.transform.localScale = Vector3.zero;
                    scaleTween = view.transform.DOScale(Vector3.one, tweenTime).SetDelay(delay).SetEase(Ease.OutBack).OnComplete(_OnTweenFinished);
                }
                else
                    scaleTween = view.transform.DOPunchScale(Vector3.one * 0.25f, tweenTime).SetEase(Ease.OutBack).OnComplete(_OnTweenFinished);
            }
        }

        public override void OnLeave()
        {
            base.OnLeave();
            if (scaleTween.IsActive())
            {
                scaleTween.Kill();
            }
            scaleTween = null;

            isTweenFinished = true;
            view.transform.localScale = Vector3.one;
            view.AddOnBoardEffect();
        }

        public override ItemLifecycle Update(float dt)
        {
            if (totalTime > 0f)
            {
                totalTime -= Time.deltaTime;
                if (totalTime <= tweenTime)
                {
                    float t = Mathf.InverseLerp(tweenTime, 0f, totalTime);
                    (view.transform as RectTransform).anchoredPosition = BoardUtility.CalculateBezierPoint(t, p0, p1, p2);
                }
            }
            if (isTweenFinished)
                return ItemLifecycle.Move;
            return base.Update(dt);
        }

        private void _OnTweenFinished()
        {
            // 新物品提示优先级高
            if (view.hasNewTip)
            {
                view.TryResolveNewItemTip();
            }
            isTweenFinished = true;
        }
    }
}
