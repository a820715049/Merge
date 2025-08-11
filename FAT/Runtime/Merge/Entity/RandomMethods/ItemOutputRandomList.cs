using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EL;

namespace FAT.Merge
{
    public class ItemOutputRandomList
    {
        public class OutputConstraitFixCount
        {
            public int id;
            public int totalCount;
            public int targetCount;
        }
        public int randomOutputNextIdx => mRandomOutputNextIdx;
        public int randomOutputSeed => mRandomOutputSeed;
        private string mDebugTag = "";
        private List<int> mRandomOutputList = new List<int>();
        private int mRandomOutputNextIdx = -1;
        private int mRandomOutputSeed = 0;
        private List<OutputConstraitFixCount> mRandomOutputCandidates = new List<OutputConstraitFixCount>();

        public int FillPossibleOutput(List<int> container = null)
        {
            int ret = 0;
            foreach(var c in mRandomOutputCandidates)
            {
                container?.Add(c.id);
                ret ++;
            }
            return ret;
        }

        private Dictionary<int, int> mTempCalculateDict = new Dictionary<int, int>();
        public void MergeToWeightDictionary(Dictionary<int, int> originDict)
        {
            
            mTempCalculateDict.Clear();
            int originOutputTotalWeight = 0;
            foreach(var v in originDict)
            {
                mTempCalculateDict[v.Key] = v.Value;
                originOutputTotalWeight += v.Value;
            }
            int randomListTotalWeight = 0;
            foreach(var e in mRandomOutputCandidates)
            {
                if(randomListTotalWeight != 0)
                {
                    var gcd = EL.MathUtility.CalculateGCD(e.totalCount, randomListTotalWeight);
                    randomListTotalWeight = e.totalCount * (randomListTotalWeight / gcd);
                }
                else
                {
                    randomListTotalWeight = e.totalCount;
                }
            }
            int outputTotalWeight = randomListTotalWeight;
            foreach(var e in mRandomOutputCandidates)
            {
                if(e.targetCount > 0)
                {
                    outputTotalWeight -= e.targetCount * (randomListTotalWeight / e.totalCount);
                }
            }
            originDict.Clear();
            if(outputTotalWeight > 0 && originOutputTotalWeight > 0)
            {
                int gcd = EL.MathUtility.CalculateGCD(outputTotalWeight, originOutputTotalWeight);
                int factor = outputTotalWeight / gcd;
                randomListTotalWeight = randomListTotalWeight * originOutputTotalWeight / gcd;
                originDict.Clear();
                foreach(var v in mTempCalculateDict)
                {
                    originDict[v.Key] = v.Value * factor;
                }
            }
            foreach(var e in mRandomOutputCandidates)
            {
                originDict[e.id] = (int)(e.targetCount * randomListTotalWeight / e.totalCount);
            }
        }

        public ItemOutputRandomList(string debugTag = "")
        {
            mDebugTag = debugTag;
        }

        public void AddConstraitFixCount(IEnumerable<OutputConstraitFixCount> constrain)
        {
            mRandomOutputCandidates.Clear();
            mRandomOutputCandidates.AddRange(constrain);
        }

        public void SetParam(int seed, int outputedCount)
        {
            if(outputedCount >= 0)
            {
                mRandomOutputSeed = seed;
                _GenerateRandomOutputList();
            }
            mRandomOutputNextIdx = outputedCount;
        }

        public int PeekNext()
        {
            if(mRandomOutputCandidates.Count <= 0)
            {
                return 0;
            }
            if(mRandomOutputNextIdx < 0 || mRandomOutputNextIdx >= mRandomOutputList.Count)
            {
                mRandomOutputNextIdx = 0;
                mRandomOutputSeed = Random.Range(1, 256);
                _GenerateRandomOutputList();
            }
            var targetItem = mRandomOutputList[mRandomOutputNextIdx];
            DebugEx.FormatInfo("ItemOutputRandomList[{0}] ----> item random list output idx {1}, item {2}", mDebugTag, mRandomOutputNextIdx, targetItem);
            return targetItem;
        }

        public int TakeNext()
        {
            var target = PeekNext();
            mRandomOutputNextIdx ++;
            return target;
        }


        private void _GenerateRandomOutputList()
        {
            mRandomOutputList.Clear();
            if(mRandomOutputCandidates.Count > 0)
            {
                using(ObjectPool<DeterministicRandom>.GlobalPool.AllocStub(out var randomGen))
                {
                    using(ObjectPool<List<int>>.GlobalPool.AllocStub(out var validIndexes))
                    {
                        randomGen.ResetWithSeed(mRandomOutputSeed);
                        int totalCount = mRandomOutputCandidates[0].totalCount;
                        for(var i = 1; i < mRandomOutputCandidates.Count; i++)
                        {
                            long mul = totalCount * mRandomOutputCandidates[i].totalCount;
                            totalCount = (int)(mul / EL.MathUtility.CalculateGCD(totalCount, mRandomOutputCandidates[i].totalCount)); 
                        }
                        for(int i = 0; i < totalCount; i++)
                        {
                            mRandomOutputList.Add(0);
                        }
                        for(int i = 0; i < mRandomOutputCandidates.Count; i++)
                        {
                            int count = mRandomOutputCandidates[i].totalCount;
                            int targetCount = mRandomOutputCandidates[i].targetCount;
                            int id = mRandomOutputCandidates[i].id;
                            int startIdx = 0;
                            while(startIdx < totalCount)
                            {
                                validIndexes.Clear();
                                for(int j = startIdx; j < count + startIdx; j++)
                                {
                                    if(mRandomOutputList[j] == 0)
                                    {
                                        validIndexes.Add(j);
                                    }
                                }
                                validIndexes.Shuffle(0, validIndexes.Count, randomGen);
                                if(targetCount > validIndexes.Count)
                                {
                                    DebugEx.FormatWarning("ItemOutputRandomList[{0}] ----> illigal random output", mDebugTag);
                                    targetCount = validIndexes.Count;
                                }
                                
                                for(int j = 0; j < targetCount; j++)
                                {
                                    mRandomOutputList[validIndexes[j]] = id;
                                }
                                startIdx += count;
                            }
                        }
                        DebugEx.FormatTrace("ItemOutputRandomList[{0}] ----> seed:{1}, {2}, {3}", mDebugTag, mRandomOutputSeed, mRandomOutputList.Count, mRandomOutputList);
                    }
                }
            }
        }
    }
}