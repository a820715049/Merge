/**
 * @Author: handong.liu
 * @Date: 2020-09-28 10:56:31
 */
using EL;
using fat.gamekitdata;

namespace GameNet
{
    public class NetAsyncTask : SimpleResultedAsyncTask<NetResponse>
    {
        public object requestObj;
        public System.DateTime requestStart {get; private set;}
        public System.DateTime reqeustEnd {get; private set;}
        public bool SetResponse(NetResponse resp)
        {
            if(resp != null)
            {
                requestStart = resp.requestStart;
                reqeustEnd = resp.requestEnd;
            }
            if(resp.errCode == (int)ErrorCode.Ok)
            {
                Success(resp);
                return true;
            }
            else
            {
                FailWithResult(resp, resp.err, resp.errCode, (ErrorCodeType)0);
                return false;
            }
        }
    }
    
    public class TypedNetAsyncTask<T> : NetAsyncTask where T:class, Google.Protobuf.IMessage, new()
    {
        public T body { 
            get {
                return result.GetBody<T>();
            }
        }
    }
}