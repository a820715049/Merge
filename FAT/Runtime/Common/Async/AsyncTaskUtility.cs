/**
 * @Author: handong.liu
 * @Date: 2021-03-03 14:36:12
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace EL
{
    public static class AsyncTaskUtility
    {
        public static IEnumerator ExtractAsyncTaskFromCoroutine<T>(out T ret, IEnumerator coroutine) where T: AsyncTaskBase
        { 
            if(coroutine != null && coroutine.MoveNext())
            {
                ret = coroutine.Current as T;
                return coroutine;
            }
            else
            {
                ret = null;
                return _CoEmptyCoroutine();
            }
        }

        private static IEnumerator _CoEmptyCoroutine()
        {
            yield break;
        }
    }
}