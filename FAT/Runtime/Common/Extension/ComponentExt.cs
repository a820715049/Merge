/**
 * @Author: handong.liu
 * @Date: 2020-09-16 21:47:44
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;


public static class ComponentExt
{
    public static AsyncTaskBase StartAsyncTask(this MonoBehaviour This, IEnumerator task)
    {
        var iter = AsyncTaskUtility.ExtractAsyncTaskFromCoroutine<AsyncTaskBase>(out var ret, task);
        if(iter != null)
        {
            This.StartCoroutine(iter);
        }
        return ret;
    } 
    public static T StartAsyncTask<T>(this MonoBehaviour This, IEnumerator task) where T : AsyncTaskBase
    {
        return This.StartAsyncTask(task) as T;
    } 
}