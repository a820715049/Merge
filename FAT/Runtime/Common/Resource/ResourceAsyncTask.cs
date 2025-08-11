/**
 * @Author: handong.liu
 * @Date: 2020-07-09 15:20:58
 */
using UnityEngine;

namespace EL.Resource
{
    public class ResourceAsyncTask : SimpleResultedAsyncTask<UnityEngine.Object>
    {
        public UnityEngine.Object asset {get {return result;}}
        internal bool isCanceling => state == AsyncTaskState.Canceling;
        internal void ResolveCancel()
        {
            TaskCancel();
        }
        public override string error{ get {return _error;}}
        public override long totalAmount => totalLength;
        public override long currentAmount => currentLength;
        public long totalLength = 0;
        public long currentLength = 0;
        private string _error = null;
        public void Fail(string err)
        {
            this._error = err;
            base.Fail();
        }
    }

    public class ResourceAsyncTaskForAsset : ResourceAsyncTask
    {
        public string groupName {get;private set;}
        public string assetName {get;private set;}
        public System.Type type {get;private set;}
        public ResourceAsyncTaskForAsset(string _groupName, string _assetName, System.Type _type)
        {
            groupName = _groupName;
            assetName = _assetName;
            type = _type;
        }
    }
}