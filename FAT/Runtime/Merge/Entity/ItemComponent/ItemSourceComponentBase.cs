/**
 * @Author: handong.liu
 * @Date: 2021-03-03 10:49:36
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;

namespace FAT.Merge
{
    //物品产出类型
    public enum ItemSourceType
    {
        None,
        FixedItem,      //MergeFixedItem   item级的固定产出 如新手沙滩桶
        FixedOutput,    //MergeFixedOutput  链条级的固定产出 如新手能量箱
        Chest,          //ComMergeChest  板条箱逻辑
        RuledOutput,    //MergeRuledOutput 该逻辑目前暂未使用
        RandomFixed,    //ComMergeTapSource.OutputsFixed 随机产出中的保底逻辑 N次内必定要产出M个
        RandomWeight,   //ComMergeTapSource.costId  MergeTapCost 随机产出中的权重随机逻辑
    }
    
    public abstract class ItemSourceComponentBase : ItemComponentBase
    {
        public class OutputDynamicWeight
        {
            public int enhance;
            public int max;
            public int origin;
        }
        public class OutputSafeRareItem
        {
            public int id;
            public int nextOutputIdx;
            public int outputStride;            //产出数量
        }
        public class OutputRandomListItem
        {
            public int id;
            public int totalCount;
            public int targetCount;
        }
        protected int randomOutputNextIdx => mRandomList?.randomOutputNextIdx ?? 0;
        protected int randomOutputSeed => mRandomList?.randomOutputSeed ?? 0;
        protected int orderedOutputNextIdx => mOrderedOutputNextIdx;
        private Dictionary<int, int> mOutputs = new Dictionary<int, int>();
        private List<int> mOrderedOutputs = new List<int>();
        private int mOrderedOutputNextIdx = 0;
        private MergeFixedItem mFixedItemOutput;
        private MergeFixedOutput mFixedCategoryOutput;
        private int mOutputedCount = 0;

        private List<OutputSafeRareItem> mOutputSafeRareItem = new List<OutputSafeRareItem>();

        private ItemOutputRandomList mRandomList = null;
        private ItemOutputRandomList mRandomListForId = null;

        public int FillPossibleOutput(List<int> container)
        {
            var fixedItem = mFixedItemOutput;
            if (fixedItem != null)
            {
                var world = item.world;
                var nextId = world.PeekNextFixedItemOutpuIdx(item.tid);
                if (nextId < fixedItem.FixedOutputs.Count)
                {
                    container?.Add(fixedItem.FixedOutputs[nextId]);
                    return 1;
                }
            }

            var fixedCat = mFixedCategoryOutput;
            if (fixedCat != null)
            {
                var world = item.world;
                var nextId = world.PeekNextFixedCategoryOutputIdx(fixedCat.CategoryId);
                if (nextId < fixedCat.FixedOutputs.Count)
                {
                    container?.Add(fixedCat.FixedOutputs[nextId]);
                    return 1;
                }
            }

            if(mOrderedOutputs.Count > 0)
            {
                mOrderedOutputNextIdx = mOrderedOutputNextIdx % mOrderedOutputs.Count;
                container?.Add(mOrderedOutputs[mOrderedOutputNextIdx]);
                return 1;
            }

            if(mRandomListForId != null)
            {
                return mRandomListForId.FillPossibleOutput(container);
            }

            int ret = mRandomList?.FillPossibleOutput(container) ?? 0;
            foreach(var r in mOutputSafeRareItem)
            {
                if(container.Contains(r.id))
                {
                    ret++;
                    container?.Add(r.id);
                }
            }
            foreach(var o in mOutputs.Keys)
            {
                if(container.Contains(o))
                {
                    ret++;
                    container?.Add(o);
                }
            }
            return ret;
        }

        public void ResetOutputs(IDictionary<int, int> outputs)
        {
            mOutputs.Clear();
            foreach (var kv in outputs)
            {
                mOutputs.Add(kv.Key, kv.Value);
            }
        }

        protected int ConsumeNextOutput(out ItemSourceType sourceType)
        {
            sourceType = ItemSourceType.None;
            // 尝试item级的固定产出  MergeFixedItem 新手沙滩桶 逻辑
            var fixedItem = mFixedItemOutput;
            if (fixedItem != null)
            {
                var world = item.world;
                var nextId = world.PeekNextFixedItemOutpuIdx(item.tid);
                if (nextId < fixedItem.FixedOutputs.Count)
                {
                    world.ConsumeNextFixedItemOutputIdx(item.tid);
                    DebugEx.FormatInfo("ItemSourceComponentBase.ConsumeNextOutput ----> {0} output a fixed idx: {1}", item.tid, nextId);
                    sourceType = ItemSourceType.FixedItem;
                    return fixedItem.FixedOutputs[nextId];
                }
            }
            
            // 尝试链条级的固定产出  MergeFixedOutput 新手能量箱 逻辑
            var fixedCat = mFixedCategoryOutput;
            if (fixedCat != null)
            {
                var world = item.world;
                var nextId = world.PeekNextFixedCategoryOutputIdx(fixedCat.CategoryId);
                if (nextId < fixedCat.FixedOutputs.Count)
                {
                    world.ConsumeNextFixedCategoryOutputIdx(fixedCat.CategoryId);
                    DebugEx.FormatInfo("ItemSourceComponentBase.ConsumeNextOutput ----> {0} output a fixed idx: {1}", item.tid, nextId);
                    sourceType = ItemSourceType.FixedOutput;
                    return fixedCat.FixedOutputs[nextId];
                }
            }
            
            int targetItem = 0;
            
            //板条箱 逻辑
            if(mOrderedOutputs.Count > 0)
            {
                mOrderedOutputNextIdx = mOrderedOutputNextIdx % mOrderedOutputs.Count;
                DebugEx.FormatInfo("ItemSourceComponentBase.ConsumeNextOutput ----> {0} output a ordered idx: {1}", item, mOrderedOutputNextIdx);
                targetItem = mOrderedOutputs[mOrderedOutputNextIdx];
                sourceType = ItemSourceType.Chest;
                mOrderedOutputNextIdx ++;
            }
            
            //这里走的MergeRuledOutput 目前表里没配东西
            if(targetItem == 0)
            {
                if(mRandomListForId != null)
                {
                    targetItem = mRandomListForId.TakeNext();
                    sourceType = ItemSourceType.RuledOutput;
                    DebugEx.FormatInfo("ItemSourceComponentBase ----> item random list for id output idx {0}, item {1}", mRandomListForId.randomOutputNextIdx, targetItem);
                }
            }

            if(targetItem == 0)
            {
                if (mRandomList != null)
                {
                    targetItem = mRandomList.TakeNext();
                    sourceType = ItemSourceType.RandomFixed;
                    DebugEx.FormatInfo("ItemSourceComponentBase ----> item random list output idx {0}, item {1}", mRandomList.randomOutputNextIdx, targetItem);
                }
            }

            //根据权重随机
            if(targetItem == 0 && mOutputs.Count > 0)
            {
                targetItem = mOutputs.Keys.RandomChooseByWeight((e)=>mOutputs[e]);
                sourceType = ItemSourceType.RandomWeight;
            }

            // 有产出则log
            if (targetItem != 0)
            {
                mOutputedCount++;
                DebugEx.FormatInfo("ItemSourceComponentBase ----> item {0}({1}) consume {2}, nextOutputId:{3}", item.id, item.tid, targetItem, mOutputedCount);
            }

            return targetItem;
        }

        protected void SetRandomOutputParam(int seed, int outputedCount)
        {
            if(mRandomList != null)
            {
                mRandomList.SetParam(seed, outputedCount);
            }
        }

        protected void SetOrderedOutputParam(int nextIdx)
        {
            mOrderedOutputNextIdx = nextIdx;
        }

        private void _InitOutput()
        {
            mOutputs.Clear();
            OnInitOutputSet(mOutputs);
            //OnInitSafeRareItem(mOutputSafeRareItem);
            if(GetType() == typeof(ItemClickSourceComponent))
            {
                mRandomListForId = item.world.GetRandomListForId(item.tid);
            }
            using(ObjectPool<List<ItemOutputRandomList.OutputConstraitFixCount>>.GlobalPool.AllocStub(out var container))
            {
                OnInitRandomList(container);
                if(container.Count > 0)
                {
                    mRandomList = new ItemOutputRandomList();
                    mRandomList.AddConstraitFixCount(container);
                }
            }
            OnInitOrderedOutput(mOrderedOutputs);
            mOrderedOutputNextIdx = 0;
            foreach(var rareItem in mOutputSafeRareItem)
            {
                rareItem.nextOutputIdx = -1;
            }
            foreach(var rareItem in mOutputSafeRareItem)
            {
                _RefreshRareItemNextOutputIdx(rareItem);
            }
        }

        private void _RefreshRareItemNextOutputIdx(OutputSafeRareItem targetOutput)
        {
            int startIdx = 0;
            if(targetOutput.nextOutputIdx >= 0)
            {
                startIdx = (targetOutput.nextOutputIdx / targetOutput.outputStride + 1) * targetOutput.outputStride;
            }
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var container))
            {
                for(int i = 0; i < targetOutput.outputStride; i++)
                {
                    container.Add(i + startIdx);
                }
                foreach(var otherItem in mOutputSafeRareItem)
                {
                    container.Remove(otherItem.nextOutputIdx);
                }
                if(container.Count <= 0)
                {
                    DebugEx.FormatWarning("ItemSourceComponentBase::_RefreshRareItemNextOutputIdx ----> rare item is full, sourceId:{0}, output:{1}", item.id, targetOutput.id);
                }
                else
                {
                    targetOutput.nextOutputIdx = container.RandomChooseByWeight((a)=>1);
                    DebugEx.FormatInfo("ItemSourceComponentBase::_RefreshRareItemNextOutputIdx ----> sourceId:{0}, outputId:{1}, outputIdx:{2}", item.id, targetOutput.id, targetOutput.nextOutputIdx);
                }
            }
        }

        //sub class will fill container in this function
        protected abstract void OnInitOutputSet(Dictionary<int, int> container);
        protected virtual void OnInitOrderedOutput(List<int> container)
        {

        }
        protected virtual void OnInitDynamicWeight(Dictionary<int, OutputDynamicWeight> container)
        {

        }
        protected virtual void OnInitSafeRareItem(List<OutputSafeRareItem> container)
        {

        }

        protected virtual void OnInitRandomList(List<ItemOutputRandomList.OutputConstraitFixCount> container)
        {

        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();

            mFixedItemOutput = Env.Instance.GetFixedOutputByItemConfig(item.tid);
            mFixedCategoryOutput = null;
            var cat = Env.Instance.GetCategoryByItem(item.tid);
            if (cat != null)
            {
                mFixedCategoryOutput = Env.Instance.GetFixedOutputConfig(cat.Id);
            }
            
            _InitOutput();
            // _TryRemoveRandomListIfInGroup7();
        }

        // private void _TryRemoveRandomListIfInGroup7()
        // {
        //     if(Env.Instance.GetPlayerTestGroup(1) == 7)
        //     {
        //         if(mRandomList != null)
        //         {
        //             //disable random list
        //             DebugEx.FormatTrace("ItemSourceComponentBase::_TryRemoveRandomListIfInGroup7 ----> pre random list {0}", mOutputs);
        //             mRandomList.MergeToWeightDictionary(mOutputs);
        //             DebugEx.FormatTrace("ItemSourceComponentBase::_TryRemoveRandomListIfInGroup7 ----> post random list {0}", mOutputs);
        //             mRandomList = null;
        //         }
        //         if(mRandomListForId != null)
        //         {
        //             //disable random list for id
        //             DebugEx.FormatTrace("ItemSourceComponentBase::_TryRemoveRandomListIfInGroup7 ----> pre random list for id {0}", mOutputs);
        //             mRandomListForId.MergeToWeightDictionary(mOutputs);
        //             DebugEx.FormatTrace("ItemSourceComponentBase::_TryRemoveRandomListIfInGroup7 ----> post random list for id {0}", mOutputs);
        //             mRandomListForId = null;
        //         }
        //     }
        // }
    }
}