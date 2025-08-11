/**
 * @Author: handong.liu
 * @Date: 2020-07-09 15:15:44
 */
using UnityEngine;

namespace EL
{
    public enum AsyncTaskState
    {
        Started,
        Canceling,
        Canceled,
        Success,
        Error
    }
    public class AsyncTaskBase : CustomYieldInstruction
    {
        public override bool keepWaiting { get { return _state == AsyncTaskState.Canceling || _state == AsyncTaskState.Started; } }
        public bool isSuccess { get { return _state == AsyncTaskState.Success; } }
        public bool isCanceled { get { return _state == AsyncTaskState.Canceled; } }
        public bool isFailOrCancel { get { return _state == AsyncTaskState.Canceled || _state == AsyncTaskState.Error; } }
        protected AsyncTaskState state { get { return _state; } }
        public virtual long errorCode { get { return 0; } }
        public virtual string error { get { return null; } }
        public virtual long totalAmount { get { return 1; } }
        public virtual long currentAmount { get { return isSuccess ? 1 : 0; } }
        private AsyncTaskState _state = AsyncTaskState.Started;
        private System.Action<AsyncTaskBase> mFinishCb;
        public virtual void Cancel()
        {
            if (keepWaiting)
            {
                this._state = AsyncTaskState.Canceling;
            }
        }
        public void OnFinish(System.Action<AsyncTaskBase> finishCb)
        {
            if(!this.keepWaiting)
            {
                finishCb?.Invoke(this);
            }
            else
            {
                mFinishCb = finishCb;
            }
        }
        protected void TaskFail()
        {
            this._state = AsyncTaskState.Error;
            _TriggerFinish();
        }
        protected void TaskSuccess()
        {
            this._state = AsyncTaskState.Success;
            _TriggerFinish();
        }
        protected void TaskCancel()
        {
            this._state = AsyncTaskState.Canceled;
            _TriggerFinish();
        }

        private void _TriggerFinish()
        {
            mFinishCb?.Invoke(this);
            mFinishCb = null;
        }
    }
}