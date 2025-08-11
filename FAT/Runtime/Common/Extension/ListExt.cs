/**
 * @Author: handong.liu
 * @Date: 2020-08-18 19:22:36
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
namespace EL
{
    public static class ListExt
    {
        public static void Shuffle<T>(this IList<T> This)
        {
            This.Shuffle(0, This.Count);
        }
        public static void Shuffle<T>(this IList<T> This, int startIdx, int endIdx, DeterministicRandom randomGenerator = null)
        {
            for(int i = startIdx; i < endIdx; i++)
            {
                int targetIdx = randomGenerator == null?Random.Range(i, endIdx): ((randomGenerator.Next % (endIdx - i)) + i);
                T temp = This[i];
                This[i] = This[targetIdx];
                This[targetIdx] = temp;
            }
        }

        public static bool TryGetByIndex<T>(this IList<T> arr, int idx, out T ret)
        {
            if(arr != null && idx >= 0 && idx < arr.Count)
            {
                ret = arr[idx];
                return true;
            }
            else
            {
                ret = default(T);
                return false;
            }
        }

        public static T GetElementEx<T>(this IList<T> arr, int idx, ArrayExt.OverflowBehaviour beh = ArrayExt.OverflowBehaviour.Clamp)
        {
            if(arr == null || arr.Count == 0)
            {
                return default(T);
            }
            if(idx < 0)
            {
                switch(beh)
                {
                    case ArrayExt.OverflowBehaviour.Clamp:
                    idx = 0;
                    break;
                    case ArrayExt.OverflowBehaviour.Circle:
                    idx = idx % arr.Count;
                    break;
                    case ArrayExt.OverflowBehaviour.Default:
                    return default(T);
                }
            }
            else if(idx >= arr.Count)
            {
                switch(beh)
                {
                    case ArrayExt.OverflowBehaviour.Clamp:
                    idx = arr.Count - 1;
                    break;
                    case ArrayExt.OverflowBehaviour.Circle:
                    idx = idx % arr.Count;
                    break;
                    case ArrayExt.OverflowBehaviour.Default:
                    return default(T);
                }
            }
            return arr[idx];
        }

        public enum SetCompareStatus
        {
            Invalid         = 1,
            Subset          = 1 << 1,   // a是b的子集
            Superset        = 1 << 2,   // a是b的超集
            Equal           = 1 << 3,
            Overlap         = 1 << 4,
            Disjoint        = 1 << 5,
        }

        public static SetCompareStatus CompareAsSet<T>(this IList<T> left, IList<T> right)
        {
            if (left == null || right == null)
                return SetCompareStatus.Invalid;

            var count_left = left.Count;
            var count_right = right.Count;
            if (count_left == 0)
            {
                return count_right == 0 ? SetCompareStatus.Equal : SetCompareStatus.Subset;
            }
            else if (count_right == 0)
            {
                return SetCompareStatus.Superset;
            }

            // 有相同元素
            var has_same_element = false;
            // 左边少元素
            var missing_left = false;
            // 右边少元素
            var missing_right = false;

            using var _ = PoolMapping.PoolMappingAccess.Borrow<Dictionary<T, int>>(out var dict);
            foreach (var item in left)
            {
                if (dict.ContainsKey(item))
                    ++dict[item];
                else
                    dict[item] = 1;
            }
            foreach (var item in right)
            {
                if (dict.ContainsKey(item))
                {
                    has_same_element = true;
                    --dict[item];
                }
                else
                {
                    missing_left = true;
                }
            }
            foreach (var count in dict.Values)
            {
                if (count < 0)
                    missing_left = true;
                else if (count > 0)
                    missing_right = true;
            }

            if (has_same_element)
            {
                if (missing_left)
                {
                    return missing_right ? SetCompareStatus.Overlap : SetCompareStatus.Subset;
                }
                else
                {
                    return missing_right ? SetCompareStatus.Superset : SetCompareStatus.Equal;
                }
            }
            else
            {
                return SetCompareStatus.Disjoint;
            }
        }

        // public static void Test()
        // {
        //     var listA = new List<int> { 1, 2, 3, 3 };
        //     var listB = new List<int> { 2, 3, 4, 3 };
        //     var listC = new List<int> { 1, 2, 3, 3 };
        //     var listD = new List<int> { 1, 2};
        //     var listE = new List<int> { 4, 5, 6 };
        //     var listF = new List<int> { 1, 2, 3, 4, 5, 6, 3 };

        //     var setA = new List<int> { 1, 2, 3 }.ToHashSet();
        //     var setB = new List<int> { 2, 3, 4 }.ToHashSet();
        //     var setC = new List<int> { 1, 2, 3 }.ToHashSet();
        //     var setD = new List<int>().ToHashSet();
        //     var setE = new List<int> { 4, 5, 6 }.ToHashSet();
        //     var setF = new List<int> { 1, 2, 3, 4, 5, 6 }.ToHashSet();

        //     Debug.Log($"{listA.CompareAsSet(listB)} {setA.Overlaps(setB)}");
        //     Debug.Log($"{listB.CompareAsSet(listA)} {setB.Overlaps(setA)}");
        //     Debug.Log($"{listA.CompareAsSet(listC)} {setA.SetEquals(setC)}");
        //     Debug.Log($"{listA.CompareAsSet(listD)} {setA.IsSupersetOf(setD)}");
        //     Debug.Log($"{listD.CompareAsSet(listA)} {setD.IsSubsetOf(setA)}");
        //     Debug.Log($"{listA.CompareAsSet(listE)} {!setA.Overlaps(setE)}");
        //     Debug.Log($"{listA.CompareAsSet(listF)} {setA.IsSubsetOf(setF)}");
        // }
    }
}