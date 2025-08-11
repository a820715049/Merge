/*
 * @Author: qun.chao
 * @Date: 2025-01-24 13:57:22
 */

using UnityEngine;
using DG.Tweening;
using FAT.Merge;

namespace FAT
{
    public class MergeItemSpawnWaitState : MergeItemBaseState
    {
        public MergeItemSpawnWaitState(MBItemView v) : base(v)
        { }

        public override void OnEnter()
        {
            base.OnEnter();

            var coord = view.data.coord;
            BoardViewManager.Instance.HoldItem(coord.x, coord.y, view.transform as RectTransform);
            view.gameObject.SetActive(false);
        }

        public override void OnLeave()
        {
            base.OnLeave();
        }

        public override ItemLifecycle Update(float dt)
        {
            return base.Update(dt);
        }
    }
}