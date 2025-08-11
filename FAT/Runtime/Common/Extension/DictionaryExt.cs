/**
 * @Author: handong.liu
 * @Date: 2020-09-07 13:08:36
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

public static class DictionaryExt
{
    public static TVal GetDefault<TKey, TVal>(this IDictionary<TKey, TVal> This, TKey key, TVal defaultVal = default(TVal))
    {
        TVal val;
        if(!This.TryGetValue(key, out val))
        {
            val = defaultVal;   
        }
        return val;
    }
}