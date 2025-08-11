/**
 * @Author: handong.liu
 * @Date: 2020-09-01 12:39:28
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;


public static class CollectionExt
{
    public static bool IsNullOrEmpty<T>(this ICollection<T> target)
    {
        return target == null || target.Count <= 0;
    }

    public static bool AddIfAbsent<T>(this ICollection<T> target, T elem)
    {
        if(target != null && !target.Contains(elem))
        {
            target.Add(elem);
            return true;
        }
        else
        {
            return false;
        }
    }
}