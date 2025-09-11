/*
 * @Author: qun.chao
 * @Date: 2021-02-19 16:07:33
 */
namespace FAT
{
    using UnityEngine;
    using DG.Tweening;
    using Merge;

    public class MergeItemDieState : MergeItemBaseState
    {
        private Tween tween;

        public MergeItemDieState(MBItemView v) : base(v)
        {}

        public override void OnEnter()
        {
            base.OnEnter();
            _PlayAnim();
            _PlaySound();
        }

        public override void OnLeave()
        {
            base.OnLeave();
            tween?.Kill();
        }

        private void _PlayAnim()
        {
            //棋子延迟播放死亡表现的时间 默认0
            var dieDelayTime = 0f; 
            //如果棋子带有MBResHolderTrig则使用其配置的延迟时间
            var resHolderTrig = view.GetResHolder() as MBResHolderTrig;
            if (resHolderTrig != null)
            {
                dieDelayTime = resHolderTrig.DieDelayTime;
            }
            //冰冻棋子死亡时有单独的动画
            if (ItemUtility.IsFrozenItem(view.data))
            {
                BoardViewManager.Instance.boardView.boardEffect.ShowFrozenMergeEffect(view.transform.position);
                tween = DOVirtual.Float(view.GetCurIconAlpha(), 0f, 0.3f, view.TweenSetAlpha).SetEase(Ease.OutCirc).OnComplete(() =>
                {
                    view.TweenSetAlpha(0f);
                    _OnTweenFinished();
                });
            }
            //普通棋子死亡动画
            else
            {
                // tween to die
                tween = view.transform.DOScale(Vector3.zero, 0.3f).SetDelay(dieDelayTime).SetEase(Ease.InCirc).OnComplete(_OnTweenFinished);
            }
        }

        private void _PlaySound()
        {
            //带有气泡组件和触发式组件的棋子 死亡时不播音效
            if (!view.data.HasComponent(ItemComponentType.Bubble) && !view.data.HasComponent(ItemComponentType.TrigAutoSource))
            {
                Game.Manager.audioMan.TriggerSound("BoardDie");
            }
        }

        private void _OnTweenFinished()
        {
            tween = null;

            // notify to release
            BoardViewManager.Instance.ReleaseItem(view.data.id);
        }
    }
}