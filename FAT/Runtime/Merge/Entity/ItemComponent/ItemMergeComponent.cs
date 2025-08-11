/**
 * @Author: handong.liu
 * @Date: 2021-02-19 15:27:35
 */
using fat.rawdata;

namespace FAT.Merge
{
    public class ItemMergeComponent : ItemComponentBase
    {
        private int mNext;
        public int PeekMergeResult(ItemMergeComponent src)
        {
            if(src.item.TryGetItemComponent<ItemSkillComponent>(out var skillSrc) && item.TryGetItemComponent<ItemSkillComponent>(out var skillDst)
                && skillSrc.type == SkillType.SandGlass && skillDst.type == SkillType.SandGlass)
            {
                return item.tid;            //需求：沙漏类型的东西可以堆叠
            }
            if(src.item.tid == item.tid)
            {
                return mNext;
            }
            else
            {
                return 0;           //0 means not mergeable
            }
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mNext = ItemUtility.GetNextItem(item.tid);
        }
    }
}