/**
 * @Author: handong.liu
 * @Date: 2020-09-28 12:54:10
 */
using UnityEngine;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using EL;

namespace EL
{
    public class ThreadDispatcher
    {
        public static ThreadDispatcher DefaultDispatcher { get { return sDefaultDispatcher; } }
        public static void InitForDefaultThread()
        {
            if (sDefaultDispatcher == null)
            {
                sDefaultDispatcher = new ThreadDispatcher();
            }
        }
        private static ThreadDispatcher sDefaultDispatcher = null;
        private ConcurrentQueue<System.Action> mActions = new ConcurrentQueue<System.Action>();
        private List<(System.Action, long)> mDelayedActions = new List<(System.Action, long)>();
        private int mOwnerThreadId;

        public ThreadDispatcher()
        {
            mOwnerThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        //call on any thread
        public void Dispatch(System.Action act)
        {
            mActions.Enqueue(act);
        }

        public void DispatchDelayed(System.Action act, long delayMilli)
        {
            long tick = System.DateTime.Now.Ticks + System.TimeSpan.TicksPerMillisecond * delayMilli;
            lock(mDelayedActions)
            {
                mDelayedActions.Add((act, tick));
            }
        }

        public void CallOrDispatch(System.Action act)
        {
            if(System.Threading.Thread.CurrentThread.ManagedThreadId == mOwnerThreadId)
            {
                // DebugEx.FormatInfo("ThreadDispatcher ---> call");
                act?.Invoke();
            }
            else
            {
                Dispatch(act);
            }
        }

        //only call on owner thread
        public void Execute()
        {
            if(mDelayedActions.Count > 0)
            {
                var time = System.DateTime.Now.Ticks;
                lock(mDelayedActions)
                {
                    for(int i = mDelayedActions.Count - 1; i >= 0; i--)
                    {
                        if(mDelayedActions[i].Item2 <= time)
                        {
                            Dispatch(mDelayedActions[i].Item1);
                            mDelayedActions.RemoveAt(i);
                        }
                    }
                }
            }

            while (true)
            {
                System.Action action;
                if (mActions.TryDequeue(out action))
                {
                    action();
                }
                else
                {
                    break;
                }
            }
        }
    }
}