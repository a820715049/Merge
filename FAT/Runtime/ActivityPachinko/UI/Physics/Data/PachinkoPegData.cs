/*
 * @Author: tang.yan
 * @Description: 弹珠游戏-UI层面钉柱相关数据类 
 * @Date: 2024-12-10 16:12:58
 */

using EL;
using UnityEngine;

namespace FAT
{
    public class PachinkoPegData : PachinkoEntityData
    {
        private Animator _animator;

        protected override void AfterBindRoot()
        {
            base.AfterBindRoot();
            _animator = _root.GetComponent<Animator>();
        }

        protected override void OnColliderBegin(bool isDebug)
        {
            if (isDebug) return;
            //播碰撞音效
            Game.Manager.audioMan.TriggerSound("PachinkoHitStock");
            _PlayColliderEffect();
        }

        //播放钉柱碰撞时的特效
        private void _PlayColliderEffect()
        {
            _animator.ResetTrigger("Punch");
            _animator.SetTrigger("Punch");
        }
    }
}