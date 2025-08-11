/*
 * @Author: qun.chao
 * @Date: 2021-02-19 16:07:53
 */
namespace FAT
{
    using UnityEngine;
    using Merge;
    using Cysharp.Threading.Tasks;

    public class MergeItemIdleState : MergeItemBaseState
    {
        public MergeItemIdleState(MBItemView v) : base(v)
        {}

        public override void OnEnter()
        {
            base.OnEnter();
            view.TryResolveNewItemTip();
        }
    }
}