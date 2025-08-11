/**
 * @Author: handong.liu
 * @Date: 2022-11-10 17:57:39
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace EL
{
    public class TickManager : MonoSingleton<TickManager>
    {
        public delegate void TickFunc(float dt);
        private List<TickFunc> mTickerPool = new List<TickFunc>();
        private bool mIsTicking = false;

        public void Schedule(TickFunc func)
        {
            if(!mTickerPool.Contains(func))
            {
                mTickerPool.Add(func);
            }
        }

        public void Unschedule(TickFunc func)
        {
            int idx = mTickerPool.IndexOf(func);
            if(idx >= 0)
            {
                mTickerPool[idx] = null;
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            int count = mTickerPool.Count;
            for(int i = 0; i < count; i++)
            {
                var func = mTickerPool[i];
                if(object.ReferenceEquals(func, null))
                {
                    mTickerPool.RemoveAt(i);
                    i--;
                    count--;
                }
                else
                {
                    func.Invoke(dt);
                }
            }
        }
    }
}