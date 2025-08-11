/*
 * @Author: qun.chao
 * @Date: 2022-04-26 17:37:09
 */
namespace FAT
{
    using UnityEngine;
    using DG.Tweening;
    using Merge;

    public class ResetState : MergeItemBaseState
    {
        public ResetState(MBItemView v) : base(v)
        { }

        public override void OnEnter()
        {
            base.OnEnter();

            var coord = view.data.coord;
            BoardViewManager.Instance.HoldItem(coord.x, coord.y, view.transform as RectTransform);
            view.transform.localScale = Vector3.one;
            view.gameObject.SetActive(true);
        }

        public override void OnLeave()
        {
            base.OnLeave();
        }

        public override ItemLifecycle Update(float dt)
        {
            return ItemLifecycle.Idle;
        }
    }
}