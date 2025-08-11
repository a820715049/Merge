/*
 * @Author: qun.chao
 * @Date: 2023-12-19 10:50:51
 */
namespace FAT
{
    using Merge;

    public class MergeItemBornState : MergeItemBaseState
    {
        public MergeItemBornState(MBItemView v) : base(v)
        {}

        public override ItemLifecycle Update(float dt)
        {
            return ItemLifecycle.Idle;
        }
    }
}