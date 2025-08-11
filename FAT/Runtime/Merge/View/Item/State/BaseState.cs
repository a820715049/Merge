/*
 * @Author: qun.chao
 * @Date: 2021-02-19 16:05:01
 */
namespace FAT
{
    using UnityEngine;
    using Merge;

    public enum ItemLifecycle
    {
        None,
        Born,
        Drag,
        Reset,
        Spawn,              // 原地产出
        SpawnWait,
        SpawnPop,           // 生成器产出
        SpawnReward,        // 奖励队列 / 背包
        SpawnMerge,
        SpawnChange,
        Consume,
        Die,
        // Override,       // 用于 merge/change 等场合item不能直接die，需要被协同动画接管
        Idle,
        Frozen,
        Move,
        MixOutput,
        DelayUnlock,    //延迟播解锁表现
        MoveToRewardBox,//棋子被收进奖励箱
    }

    public class MergeItemBaseState
    {
        protected MBItemView view;

        public MergeItemBaseState(MBItemView v)
        {
            view = v;
        }

        public virtual void OnEnter()
        { }

        public virtual void OnLeave()
        { }

        public virtual ItemLifecycle Update(float dt)
        {
            return ItemLifecycle.None;
        }
    }
}