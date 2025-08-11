/**
 * @Author: handong.liu
 * @Date: 2021-05-28 15:47:05
 */
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EL
{
    public class HandlerChain<T1,T2,T3,T4>
    {
        private List<Func<T1,T2,T3,T4, bool>> mChain = new List<Func<T1,T2,T3,T4, bool>>();
        //handler返回是否处理完成（无需继续传播）
        public void AddFront(Func<T1,T2,T3,T4, bool> handler)
        {
            mChain.Remove(handler);
            mChain.Add(handler);
        }

        public void Remove(Func<T1,T2,T3,T4, bool> handler)
        {
            mChain.Remove(handler);
        }

        public bool Handler(T1 t1, T2 t2, T3 t3, T4 t4)
        {
            for(int i = mChain.Count - 1; i >= 0; i--)
            {
                if(mChain[i](t1, t2, t3, t4))
                {
                    return true;
                }
            }
            return false;
        }
    }
}