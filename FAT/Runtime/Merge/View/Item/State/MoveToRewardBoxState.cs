/*
 * @Author: tang.yan
 * @Description: 棋子移到奖励箱时的状态
 * @Date: 2025-06-05 18:06:48
 */
using UnityEngine;
using DG.Tweening;
using FAT.Merge;

namespace FAT
{
    public class MoveToRewardBoxState : MergeItemBaseState
    {
        private Sequence seq;

        public MoveToRewardBoxState(MBItemView v) : base(v) { }

        public override void OnEnter()
        {
            base.OnEnter();
            view.gameObject.SetActive(true);
            var trans = view.transform;
            BoardViewManager.Instance.ReAnchorItemForMove(trans);

            var rewardBoxPos = UIFlyFactory.ResolveFlyTarget(FlyType.MergeItemFlyTarget);
            seq = DOTween.Sequence();
            seq.Pause();
            view.transform.localScale = Vector3.one * UIFlyConfig.Instance.scaleRewardElasticStart;
            UIFlyFactory.CreateElasticTween(seq, trans, true, UIFlyConfig.Instance.scaleRewardElasticStartTo,
                UIFlyConfig.Instance.durationRewardElasticStart, UIFlyConfig.Instance.curveRewardElasticStart);
            UIFlyFactory.CreateStraightTween(seq, trans, rewardBoxPos);
            UIFlyFactory.CreateElasticTween(seq, trans, false, UIFlyConfig.Instance.scaleRewardElasticEnd,
                UIFlyConfig.Instance.durationFly, UIFlyConfig.Instance.curveRewardElasticEnd);
            seq.OnComplete(_OnTweenFinished);
            seq.Play();
        }

        public override void OnLeave()
        {
            base.OnLeave();
            if (seq != null && seq.IsActive())
            {
                seq.Complete();
                seq.Kill();
            }
            seq = null;
        }

        private void _OnTweenFinished()
        {
            BoardViewManager.Instance.ReleaseItem(view.data.id);
        }
    }
}