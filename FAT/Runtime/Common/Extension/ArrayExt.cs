/**
 * @Author: handong.liu
 * @Date: 2020-07-16 20:58:09
 */
using System;

public static class ArrayExt
{
    public enum OverflowBehaviour
    {
        Circle,
        Clamp,
        Default
    }
    public static int IndexOf<T>(this T[] arr, T elem)
    {
        if(arr == null)
        {
            return -1;
        }
        for(int i = 0; i < arr.Length; i++)
        {
            if(System.Collections.Generic.EqualityComparer<T>.Default.Equals(arr[i], elem))
            {
                return i;
            }
        }
        return -1;
    }
    public static int IndexOf<T>(this T[] arr, System.Func<T, bool> predicator)
    {
        if(arr == null)
        {
            return -1;
        }
        for(int i = 0; i < arr.Length; i++)
        {
            if(predicator.Invoke(arr[i]))
            {
                return i;
            }
        }
        return -1;
    }

    public static T GetElementEx<T>(this T[] arr, int idx, OverflowBehaviour beh = OverflowBehaviour.Clamp)
    {
        if(arr == null || arr.Length == 0)
        {
            return default(T);
        }
        if(idx < 0)
        {
            switch(beh)
            {
                case OverflowBehaviour.Clamp:
                idx = 0;
                break;
                case OverflowBehaviour.Circle:
                idx = idx % arr.Length;
                break;
                case OverflowBehaviour.Default:
                return default(T);
            }
        }
        else if(idx >= arr.Length)
        {
            switch(beh)
            {
                case OverflowBehaviour.Clamp:
                idx = arr.Length - 1;
                break;
                case OverflowBehaviour.Circle:
                idx = idx % arr.Length;
                break;
                case OverflowBehaviour.Default:
                return default(T);
            }
        }
        return arr[idx];
    }
}