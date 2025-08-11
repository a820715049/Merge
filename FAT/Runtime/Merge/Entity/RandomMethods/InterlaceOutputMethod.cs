/**
 * @Author: handong.liu
 * @Date: 2023-02-23 16:57:00
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    public class InterlaceOutputMethod
    {
        private DeterministicRandom mRandomGenerator = new DeterministicRandom(123);
        private List<int> mCurrentSet = new List<int>();
        private int mNextItem = 0;
        private Dictionary<int, int> mPreviousItemCount = new Dictionary<int, int>();
        private Dictionary<int, int> mWeightMap = new Dictionary<int, int>();
        private List<int> mWeightOrder = new List<int>();           //应该要喝mWeightMap的key一一对应
        private int mSetCount = 10;

        public override string ToString()
        {
            return $"weight:{mWeightMap.ToStringEx()}, order:{mWeightOrder.ToStringEx()}, mPreviousItemCount:{mPreviousItemCount.ToStringEx()}";
        }

        public void Serialize(RandomParam param)
        {
            param.Type = (int)RandomMethodType.Interlace;
            param.IntParam.Add(mRandomGenerator.seed);
            param.IntParam.Add(mNextItem);
            foreach(var entry in mPreviousItemCount)
            {
                param.MapParam1[entry.Key] = entry.Value;
            }
        }

        public void Deserialize(RandomParam param)
        {
            if(param.Type != (int)RandomMethodType.Interlace)
            {
                return;
            }
            mRandomGenerator = new DeterministicRandom();
            mRandomGenerator.ResetWithSeed(param.IntParam.GetElementEx(0, ArrayExt.OverflowBehaviour.Default));
            mNextItem = param.IntParam.GetElementEx(1, ArrayExt.OverflowBehaviour.Default);
            mPreviousItemCount.Clear();
            foreach(var entry in param.MapParam1)
            {
                mPreviousItemCount[entry.Key] = entry.Value;
            }
            mCurrentSet.Clear();
        }

        public void InitConfig(IEnumerable<int> items, IEnumerable<int> weight)
        {
            mWeightOrder.Clear();
            mPreviousItemCount.Clear();
            mWeightMap.Clear();
            mCurrentSet.Clear();
            mNextItem = 0;
            
            mWeightOrder.AddRange(items);
            var iterItem = items.GetEnumerator();
            var iterWeight = weight.GetEnumerator();
            while(iterItem.MoveNext() && iterWeight.MoveNext())
            {
                mWeightMap[iterItem.Current] = iterWeight.Current;
            }
        }
        
        public int PeekNextItem()
        {
            _EnsureNext();
            return mCurrentSet[mNextItem];
        }

        public int UseNextItem()
        {
            int item = PeekNextItem();
            mNextItem++;
            return item;
        }

        private void _EnsureNext()
        {
            if(mNextItem >= mCurrentSet.Count)
            {
                if(mRandomGenerator == null || mNextItem >= mSetCount)
                {
                    //这属于走完了整个序列，重新生成随机数
                    mRandomGenerator = new DeterministicRandom();
                    mRandomGenerator.ResetWithSeed(Random.Range(1, 10000));
                    mNextItem = 0;
                }
                mRandomGenerator.ResetWithSeed(mRandomGenerator.seed, 100);
                foreach(var id in mCurrentSet)
                {
                    var thisCount = mPreviousItemCount.GetDefault(id, 0) + 1;
                    if(thisCount > 0)           //防止溢出
                    {
                        mPreviousItemCount[id] = thisCount;
                    }
                }
                mCurrentSet.Clear();

                //check remove
                int count = 9999999;
                foreach(var entry in mPreviousItemCount)
                {
                    var weight = mWeightMap.GetDefault(entry.Key);
                    if(weight > 0)
                    {
                        var thisCount = entry.Value / weight;
                        if(thisCount < count)
                        {
                            count = thisCount;
                        }
                    }
                }
                if(count > 0)
                {
                    DebugEx.FormatInfo("InterlaceOutputMethod::_EnsureNext ---> shrink item array {0} for count {1}, weight: {2}", mPreviousItemCount, count, mWeightMap);
                    using(ObjectPool<List<int>>.GlobalPool.AllocStub(out var keyContainer))
                    {
                        keyContainer.AddRange(mPreviousItemCount.Keys);
                        foreach(var key in keyContainer)
                        {
                            mPreviousItemCount[key] = mPreviousItemCount[key] - count * mWeightMap.GetDefault(key);
                        }
                    }
                }

                using(ObjectPool<Dictionary<int,int>>.GlobalPool.AllocStub(out var countNeeded))
                {
                    long totalCount = mSetCount;
                    foreach(var countEntry in mPreviousItemCount)
                    {
                        totalCount += countEntry.Value;
                    }
                    long totalWeight = 0;
                    foreach(var weight in mWeightMap)
                    {
                        totalWeight += weight.Value;
                    }
                    foreach(var weight in mWeightMap)
                    {
                        if(weight.Value > 0)
                        {
                            long a = totalCount * weight.Value / totalWeight; 
                            if(a > int.MaxValue)        //防溢出
                            {
                                a = int.MaxValue;
                            }
                            countNeeded[weight.Key] = (int)a;
                        }
                    }
                    long restCount = totalCount;
                    foreach(var countEntry in countNeeded)
                    {
                        restCount -= countEntry.Value;
                    }
                    if(restCount > 0)
                    {
                        //把剩余量给需求最大的一个
                        int maxNeededKey = 0;
                        int maxNeededCount = 0;
                        foreach(var countEntry in countNeeded)
                        {
                            if(countEntry.Value > maxNeededCount)
                            {
                                maxNeededCount = countEntry.Value;
                                maxNeededKey = countEntry.Key;
                            }
                        }
                        if(countNeeded.ContainsKey(maxNeededKey))
                        {
                            countNeeded[maxNeededKey] = countNeeded[maxNeededKey] + (int)restCount;
                        }
                        else
                        {
                            DebugEx.FormatError("InterlaceOutputMethod::_EnsureNext ----> no max needed key");
                            // FAT_TODO
                            // Game.Instance.Abort("Fail", 0);
                            throw new System.Exception();
                        }
                    }
                    using(ObjectPool<List<int>>.GlobalPool.AllocStub(out var keyContainer))
                    {
                        keyContainer.AddRange(countNeeded.Keys);
                        int negtiveCount = 0;           //如果策划改配置了，可能出现负数
                        foreach(var key in keyContainer)
                        {
                            if(mPreviousItemCount.TryGetValue(key, out var previousCount))
                            {
                                countNeeded[key] = countNeeded[key] - previousCount;
                                if(countNeeded[key] < 0)
                                {
                                    negtiveCount += -countNeeded[key];
                                    countNeeded[key] = 0;
                                }
                            }
                        }
                        if(negtiveCount > 0)          //如果策划改配置了，可能出现负数，接着处理
                        {
                            foreach(var key in keyContainer)
                            {
                                if(countNeeded[key] > 0)
                                {
                                    var del = Mathf.Min(negtiveCount, countNeeded[key]);
                                    negtiveCount -= del;
                                    countNeeded[key] = countNeeded[key] - del;
                                }
                            }
                        }
                    }
                    _ArrangeItemByNeed(countNeeded, mWeightOrder, mCurrentSet);
                }
            }
        }

        private void _ArrangeItemByNeed(Dictionary<int, int> needCount, List<int> itemByOrder, List<int> container)
        {
            DebugEx.FormatInfo("InterlaceOutputMethod::_ArrangeItemByNeed ----> for item {0}, needCount {1}", itemByOrder, needCount);
            using(ObjectPool<List<List<int>>>.GlobalPool.AllocStub(out var subArrays))
            {
                while(true)
                {
                    List<int> newSubList = ObjectPool<List<int>>.GlobalPool.Alloc();
                    foreach(var item in itemByOrder)
                    {
                        if(needCount.TryGetValue(item, out var count) && count > 0)
                        {
                            newSubList.Add(item);
                            needCount[item] = count - 1;
                        }
                    }
                    subArrays.Add(newSubList);
                    if(newSubList.Count <= 1)
                    {
                        break;
                    }
                }
                while(subArrays.Count > 0 && subArrays[subArrays.Count - 1].Count == 0)     //remove empty list
                {
                    subArrays.RemoveAt(subArrays.Count - 1);
                }
                if(subArrays.Count > 1 && subArrays[subArrays.Count - 1].Count == 1)            //means should add all that left, and put it in other arrays
                {
                    var tailItem = subArrays[subArrays.Count - 1][0];
                    subArrays.RemoveAt(subArrays.Count - 1);
                    var tailCount = needCount.GetDefault(tailItem) + 1;
                    using(ObjectPool<List<int>>.GlobalPool.AllocStub(out var randomIdxes))
                    {
                        for(int i = 0; i < subArrays.Count; i++)
                        {
                            randomIdxes.Add(i);
                        }
                        int randomIdx = subArrays.Count;
                        for(int i = 0; i < tailCount; i++, randomIdx++)
                        {
                            if(randomIdx >= subArrays.Count)
                            {
                                randomIdxes.Shuffle(0, randomIdxes.Count, mRandomGenerator);
                                randomIdx = 0;
                            }
                            subArrays[randomIdxes[randomIdx]].Add(tailItem);
                        }
                    }
                    DebugEx.FormatInfo("InterlaceOutputMethod::_ArrangeItemByNeed ----> add tail, tailitem{0}, tailCount{1}", tailItem, tailCount);
                }
                //revert the arrays, as DongYi said
                for(int i = subArrays.Count - 1; i >= 0; i--)
                {
                    var targetArray = subArrays[i];
                    for(int j = targetArray.Count - 1; j >= 0; j--)
                    {
                        container.Add(targetArray[j]);
                    }
                }
                foreach(var array in subArrays)
                {
                    array.Clear();
                    ObjectPool<List<int>>.GlobalPool.Free(array);
                }
                subArrays.Clear();
                DebugEx.FormatTrace("InterlaceOutputMethod::_ArrangeItemByNeed ----> result:{0}", container);
            }
        }
    }
}