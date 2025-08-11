/*
 * @Author: qun.chao
 * @Date: 2025-01-09 18:31:00
 */
using UnityEngine;
using DG.Tweening;
using FAT.Merge;

namespace FAT
{
    public class MergeItemMixOutputState : MergeItemBaseState
    {
        private const float lifeTimeTotal = 0.3f;
        private float lifeTime;

        public MergeItemMixOutputState(MBItemView v) : base(v)
        { }

        public override void OnEnter()
        {
            base.OnEnter();
            lifeTime = lifeTimeTotal;
        }

        public override void OnLeave()
        {
            base.OnLeave();
        }

        public override ItemLifecycle Update(float dt)
        {
            lifeTime -= dt;
            if (lifeTime < 0f)
                return ItemLifecycle.Idle;
            return base.Update(dt);
        }
    }
}