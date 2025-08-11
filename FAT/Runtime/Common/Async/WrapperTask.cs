/**
 * @Author: handong.liu
 * @Date: 2020-10-16 18:20:51
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

public class SingleTaskWrapper : AsyncTaskBase
{
    public bool isCanceling { get{return state == AsyncTaskState.Canceling;}}
    public override string error { get { return mWrappedTask != null?mWrappedTask.error:""; } }
    public override long errorCode { get { return mWrappedTask != null?mWrappedTask.errorCode:0; } }
    private AsyncTaskBase mWrappedTask;
    public void SetTask(AsyncTaskBase task)
    {
        mWrappedTask = task;
    }

    public override void Cancel()
    {
        base.Cancel();
        if(mWrappedTask != null)
        {
            mWrappedTask.Cancel();
        }
    }

    public void ResolveCancel()
    {
        TaskCancel();
    }

    public void ResolveError()
    {
        TaskFail();
    }

    public void ResolveSuccess()
    {
        TaskSuccess();
    }
}