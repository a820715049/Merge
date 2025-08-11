/**
 * @Author: handong.liu
 * @Date: 2020-07-21 18:35:37
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EL
{
    public static class EnumerableExt
    {
        public delegate int WeightDelegate<T>(T elem);
        private static int _DefaultWeightFunc<T>(T e) { return 1; }
        private static int _DefaultRandFunc() { return Random.Range(0, int.MaxValue); }
        public static T RandomChooseByWeight<T>(this IEnumerable<T> target, WeightDelegate<T> weightFunc = null, System.Func<int> randFunc = null)
        {
            if(weightFunc == null)
            {
                weightFunc = _DefaultWeightFunc;
            }
            if(randFunc == null)
            {
                randFunc = _DefaultRandFunc;
            }
            int totalWeight = 0;
            var iter = target.GetEnumerator();
            while(iter.MoveNext())
            {
                totalWeight += weightFunc(iter.Current);
            }
            //总权重≤0时返回 避免抛除0异常
            if (totalWeight <= 0) return default(T);
            var rand = randFunc() % totalWeight;// Random.Range(0, totalWeight);
            iter = target.GetEnumerator();
            while(iter.MoveNext())
            {
                rand -= weightFunc(iter.Current);
                if(rand < 0)
                {
                    return iter.Current;
                }
            }
            return default(T);
        }
        public static List<T> ToList<T>(this IEnumerable<T> target)
        {
            List<T> list = new List<T>();
            var iter = target.GetEnumerator();
            while(iter.MoveNext())
            {
                list.Add(iter.Current);
            }
            return list;
        }

        public static Dictionary<K, T> ToDictionaryEx<K, T>(this IEnumerable<T> target, System.Func<T, K> keyExtractor)
        {
            Dictionary<K, T> dict = new Dictionary<K, T>();
            var iter = target.GetEnumerator();
            while(iter.MoveNext())
            {
                var k = keyExtractor(iter.Current);
                dict[k] = iter.Current;
            }
            return dict;
        }

        public static string ToStringEx(this IEnumerable target)
        {
            if(target is string)
            {
                return target as string;
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            foreach(var t in target)
            {
                var enumerable = t as IEnumerable;
                if(enumerable != null)
                {
                    sb.AppendFormat("({0}),", enumerable.ToStringEx());
                }
                else
                {
                    sb.AppendFormat("({0}),", t);
                }
            }
            sb.Append("}");
            return sb.ToString();
        }

        public static string ToStringEx<T>(this IEnumerable<T> target)
        {
            if(target is string)
            {
                return target as string;
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            foreach(T t in target)
            {
                var enumerable = t as IEnumerable;
                if(enumerable != null)
                {
                    sb.AppendFormat("({0}),", enumerable.ToStringEx());
                }
                else
                {
                    sb.AppendFormat("({0}),", t);
                }
            }
            sb.Append("}");
            return sb.ToString();
        }

        public static T FindEx<T>(this IEnumerable<T> target, System.Func<T, bool> predictor)
        {
            if(target != null)
            {
                foreach(var e in target)
                {
                    if(predictor(e))
                    {
                        return e;
                    }
                }
            }
            return default(T);
        }
    }
}