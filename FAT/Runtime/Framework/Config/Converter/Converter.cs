/**
 * @Author: handong.liu
 * @Date: 2021-03-25 20:00:00
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using EL;
using FAT;

namespace Config
{
    namespace Converter
    {
        public class ConverterBase<T> where T : new()
        {
            private Dictionary<string, T> mCache = new Dictionary<string, T>();
            public T Get(string text)
            {
                if(text == null)
                {
                    return default(T);
                }
                if(!mCache.TryGetValue(text, out var ret))
                {
                    ret = new T();
                    var needCache = DoConvert(text, ref ret);
                    if (needCache)
                        mCache[text] = ret;
                }
                return ret;
            }

            //return needCache 表示本次convert结果是否会被缓存 false表示不缓存 每次都会重新convert
            protected virtual bool DoConvert(string text, ref T ret)
            {
                return true;
            }
        }
        public class AssetConverter : ConverterBase<AssetConfig>
        {
            protected override bool DoConvert(string text, ref AssetConfig ret)
            {
                string[] splited = text.Split(':', '#');
                ret.Group = splited.GetElementEx(0, ArrayExt.OverflowBehaviour.Default);
                ret.Asset = splited.GetElementEx(1, ArrayExt.OverflowBehaviour.Default);
                ret.Key = text;
                return true;
            }
        }

        public class StyleAddConfigConverter : ConverterBase<StyleAddConfig>
        {
            protected override bool DoConvert(string text, ref StyleAddConfig ret)
            {
                string[] splited = text.Split(':');
                ret.Id = splited.GetElementEx(0, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                ret.Add = splited.GetElementEx(1, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                return true;
            }
        }

        public class ColorConfigConverter : ConverterBase<ColorConfig>
        {
            protected override bool DoConvert(string text, ref ColorConfig ret)
            {
                string[] splited = text.Split(':');
                ret.H = splited.GetElementEx(0, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                ret.S = splited.GetElementEx(1, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                ret.V = splited.GetElementEx(2, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                return true;
            }
        }

        public class RewardConfigConverter : ConverterBase<RewardConfig>
        {
            protected override bool DoConvert(string text, ref RewardConfig ret)
            {
                string[] splited = text.Split(':');
                ret.Id = splited.GetElementEx(0, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                ret.Count = splited.GetElementEx(1, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                //检查第一项是否是赛季物品 是的话 就根据配置做转化
                var id = ret.Id;
                var objectMan = Game.Manager.objectMan;
                var isSeasonItem = objectMan.IsType(id, ObjConfigType.SeasonItem);
                if (isSeasonItem)
                {
                    ret.Id = objectMan.TransSeasonItemToRealId(id);
                    return false;
                }
                return true;
            }
        }

        public class MergeGridItemConverter : ConverterBase<MergeGridItem>
        {
            protected override bool DoConvert(string text, ref MergeGridItem ret)
            {
                string[] splited = text.Split(':');
                ret.Id = splited.GetElementEx(0, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                ret.State = splited.GetElementEx(1, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                ret.Param = splited.GetElementEx(2, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                return true;
            }
        }

        public class GuideMergeRequireConverter : ConverterBase<GuideMergeRequire>
        {
            protected override bool DoConvert(string text, ref GuideMergeRequire ret)
            {
                string[] splited = text.Split(':');
                ret.Type = splited.GetElementEx(0, ArrayExt.OverflowBehaviour.Default).ConvertToEnumGuideMergeRequireType();
                ret.Value = splited.GetElementEx(1, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                ret.Extra = splited.GetElementEx(2, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                return true;
            }
        }
        
        public class RandomBoxShowRewardConverter : ConverterBase<RandomBoxShowReward>
        {
            protected override bool DoConvert(string text, ref RandomBoxShowReward ret)
            {
                string[] splited = text.Split(':');
                ret.Id = splited.GetElementEx(0, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                ret.MinCount = splited.GetElementEx(1, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                ret.MaxCount = splited.GetElementEx(2, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                return true;
            }
        }

        public class CommonInt3Converter : ConverterBase<(int, int, int)>
        {
            // ref: https://stackoverflow.com/a/62780940
            protected override bool DoConvert(string text, ref (int, int, int) ret)
            {
                var span = text.AsSpan();

                var idx = span.IndexOf(':');
                int.TryParse(span[..idx], out ret.Item1);
                span = span[(idx + 1)..];

                idx = span.IndexOf(':');
                if (idx >= 0)
                {
                    int.TryParse(span[..idx], out ret.Item2);
                    span = span[(idx + 1)..];
                    int.TryParse(span, out ret.Item3);
                }
                else
                {
                    int.TryParse(span, out ret.Item2);
                    ret.Item3 = 0;
                }
                //检查第一项是否是赛季物品 是的话 就根据配置做转化
                var id = ret.Item1;
                var objectMan = Game.Manager.objectMan;
                var isSeasonItem = objectMan.IsType(id, ObjConfigType.SeasonItem);
                if (isSeasonItem)
                {
                    ret.Item1 = objectMan.TransSeasonItemToRealId(id);
                    return false;
                }
                return true;
            }
        }
        
        public class CoordConverter : ConverterBase<CoordConfig>
        {
            protected override bool DoConvert(string text, ref CoordConfig ret)
            {
                string[] splited = text.Split(':');
                ret.Row = splited.GetElementEx(0, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                ret.Col = splited.GetElementEx(1, ArrayExt.OverflowBehaviour.Default).ConvertToInt();
                return true;
            }
        }


        public class RoundsArrayConfigConverter : ConverterBase<RoundsArrayConfig>
        {
            protected override bool DoConvert(string text, ref RoundsArrayConfig ret)
            {
                string[] splited = text.Split(':');
                ret.RoundsArray = new int[splited.Length];
                for (int i = 0; i < splited.Length; i++)
                {
                    ret.RoundsArray[i] = splited[i].ConvertToInt();
                }
                return true;
            }
        }
    }
}