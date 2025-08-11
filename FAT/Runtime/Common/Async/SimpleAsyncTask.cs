/**
 * @Author: handong.liu
 * @Date: 2020-07-16 21:19:29
 */

namespace EL
{
    public class SimpleAsyncTask : AsyncTaskBase
    {
        public static SimpleAsyncTask AlwaysSuccess
        {
            get {
                if(sAlwaysSuccess == null)
                {
                    sAlwaysSuccess = new SimpleAsyncTask();
                    sAlwaysSuccess.ResolveTaskSuccess();
                }
                return sAlwaysSuccess;
            }
        }
        public static SimpleAsyncTask AlwaysFail {
            get {
                if(sAlwaysFail == null)
                {
                    sAlwaysFail = new SimpleAsyncTask();
                    sAlwaysFail.ResolveTaskFail();
                }
                return sAlwaysFail;
            }
        }
        private static SimpleAsyncTask sAlwaysSuccess;
        private static SimpleAsyncTask sAlwaysFail;
        public bool isCanceling { get{return state == AsyncTaskState.Canceling;}}
        public override long errorCode => mErrorCode;
        public override string error => mErrorMsg;
        private long mErrorCode;
        private string mErrorMsg;
        
        public void ResolveTaskFail(long errCode = 0, string errMsg = null, ErrorCodeType errorType = ErrorCodeType.GameError)
        {
            this.TaskFail();
            this.mErrorCode = ErrorCodeUtility.ConvertToCommonCode(errCode, errorType);;
            this.mErrorMsg = errMsg;
        }
        public void ResolveTaskFail(AsyncTaskBase otherTask)
        {
            this.TaskFail();
            this.mErrorCode = otherTask.errorCode;
            this.mErrorMsg = otherTask.error;
        }
        public void ResolveTaskSuccess()
        {
            this.TaskSuccess();
        }
        public void ResolveTaskCancel()
        {
            this.TaskCancel();
        }
    }
}