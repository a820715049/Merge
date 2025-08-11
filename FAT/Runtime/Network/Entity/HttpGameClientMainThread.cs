/**
 * @Author: handong.liu
 * @Date: 2020-09-25 16:30:35
 */
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;
using EL;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using FAT;
using FAT.Platform;

namespace GameNet
{
    public class HttpGameClientMainThread : IHttpGameClient
    {
        class RequestStub
        {
            public UnityWebRequestAsyncOperation response;
            public string uri;
            public bool needToken;
            public IMessage payload;
            public gen.netutils.RawPacket rawRequestStruct;
            public byte[] requestBytes;
            public int mimelen;
            public int seqId;
            public System.IO.MemoryStream requestStream;
            public string requestBase64;
            public System.Action<NetResponse> onFinish;
            public System.DateTime timeStart;
            public System.DateTime timeEnd;
            public int retryCountLeft;
        }
        public IProtobufCodec protocolCodec => mCodecMainThread;
        private string mToken;
        private HttpClientConfig mConfig;
        private bool mDisposed;
        private Queue<RequestStub> mRequestQueue = new Queue<RequestStub>();
        private bool mThreadingRunning = false;
        private long mSequenceId = 1;
        private IProtobufCodec mCodec = new ProtobufCodecBin();
        private IProtobufCodec mCodecMainThread = new ProtobufCodecBin();
        private Dictionary<string, string> mAdditionalHeader = new Dictionary<string, string>();
        private ObjectPool<System.IO.MemoryStream> mMemoryPool = new ObjectPool<System.IO.MemoryStream>();          //net thread use only!
        private float mWaitCountDown;
        private byte[] mUrlBytes;           //同constant里的kUrl
        private EL.IFrodo mFrodo = new FrodoCalc2();
        private System.Action<long> mOnErrorRetry;
        public void SetErrorRetryCB(System.Action<long> cb)
        {
            mOnErrorRetry = cb;
        }

        public void InitWithConfig(HttpClientConfig config)
        {
            mConfig = config;
        }

        public void SetHobbitsName(int hn)
        {
            if(hn == 1)
            {
                mFrodo = new FrodoCalc();
            }
            else
            {
                mFrodo = new FrodoCalc2();
            }
        }

        public void Dispose()
        {
            mDisposed = true;
        }

        public void SetToken(string token)
        {
            mToken = token;
        }

        public void RequestPost(string path, bool needToken, System.Action<NetResponse> onResponse, IMessage data, int retryCount)
        {
            // var url = needToken?string.Format("{0}/api/v1/auto/{1}", mConfig.urlRoot, path):string.Format("{0}/api/v1/entry/auto/{1}", mConfig.urlRoot, path);
            var url = needToken ? string.Format("{0}/biz", mConfig.urlRoot) : string.Format("{0}/api/v1/entry/auto", mConfig.urlRoot);
            DebugEx.FormatTrace("HttpGameClient::RequestPost ----> url:{0}, body:{1}", url, data);
            _SendRequest(url, needToken, data, retryCount, onResponse);
        }

        public void AddAdditionalHeader(string key, string val)
        {
            mAdditionalHeader[key] = val;
        }
        
        public void RemoveAdditionalHeader(string key)
        {
            mAdditionalHeader.Remove(key);
        }

        private void _SendRequest(string url, bool needToken, IMessage message, int retryCount, System.Action<NetResponse> onResponse)
        {
            DebugEx.FormatInfo("HttpGameClient::_SendRequest ----> {0}, request {1}", "POST", url);
            mRequestQueue.Enqueue(new RequestStub() { uri = url, needToken = needToken, payload = message, onFinish = onResponse, retryCountLeft = retryCount });
        }

        private void _SetMetadata(gen.netutils.RawPacket rp)
        {
            rp.Metadata.Add(gen.netutils.MetadataKey.AppVersion.ToString());
            rp.Metadata.Add(Game.Instance?.appSettings?.version ?? "");

            rp.Metadata.Add(gen.netutils.MetadataKey.Channel.ToString());
            rp.Metadata.Add(Game.Instance?.appSettings?.updaterConfig?.channel ?? "");

            rp.Metadata.Add(gen.netutils.MetadataKey.Language.ToString());
            rp.Metadata.Add("en");

            var clientUniqueId = Game.Instance.appSettings.clientId;
            if (!string.IsNullOrEmpty(clientUniqueId))
            {
                rp.Metadata.Add(gen.netutils.MetadataKey.ClientId.ToString());
                rp.Metadata.Add(clientUniqueId);
            }
        }

        public void Update(float dt)
        {
            if(mWaitCountDown > 0)
            {
                mWaitCountDown -= dt;
                return;
            }
            if(mRequestQueue.Count > 0)
            {
                IProtobufCodec codec = mCodec;
                var next = mRequestQueue.Peek();
                if(next.response == null)
                {
                    if(next.requestBytes == null)
                    {
                        next.timeStart = System.DateTime.Now;
                        //encode message
                        var rawPacket = next.rawRequestStruct = new gen.netutils.RawPacket();
                        rawPacket.SequenceID = (int)mSequenceId++;
                        next.seqId = rawPacket.SequenceID;
                        rawPacket.Version = 1;
                        _SetMetadata(rawPacket);
                        if (next.payload != null)
                        {
                            var rawAny = new gen.netutils.RawAny();
                            rawAny.Uri = next.payload.Descriptor.FullName;
                            //rawAny.NextWhenError = true;
                            rawAny.Raw = codec.MarshalToByteString(next.payload);
                            rawPacket.RawAny.Add(rawAny);
                            rawAny.PassThrough = next.timeStart.Ticks.ToString();
                        }
                        byte[] data = null;
                        int frodolen = mFrodo.GL();
                        using(mMemoryPool.AllocStub(out var stream))
                        {
                            stream.SetLength(0);
                            codec.MarshalToStream(stream, rawPacket);
                            if(stream.Length > 0)
                            {
                                data = new byte[frodolen + stream.Length];
                                stream.Seek(0, System.IO.SeekOrigin.Begin);
                                stream.Read(data, frodolen, (int)stream.Length);
                            }
                        }
                        next.requestBytes = data;
                        if(mUrlBytes == null)           //放到这里，是为了让代码更难读（一个超大的函数，反编译以后不好辨认）
                        {
                            string str = FrodoUtility.ITK(Constant.kUrl);
                            mUrlBytes = System.Text.Encoding.UTF8.GetBytes(str);
                        }
                        mFrodo.RR();
                        if(next.requestBytes != null && next.requestBytes.Length > frodolen)
                        {
                            mFrodo.RL(next.requestBytes, (uint)frodolen, (uint)(next.requestBytes.Length - frodolen));
                        }
                        mFrodo.RL(mUrlBytes, 0, (uint)mUrlBytes.Length);
                        next.mimelen = frodolen;
                        #if UNITY_EDITOR
                        var bytes = new byte[mUrlBytes.Length + (uint)(next.requestBytes.Length - frodolen)];
                        System.Array.Copy(next.requestBytes, frodolen, bytes, 0, next.requestBytes.Length - frodolen);
                        System.Array.Copy(mUrlBytes, 0, bytes, next.requestBytes.Length - frodolen, mUrlBytes.Length);
                        DebugEx.FormatTrace("HttpGameClient::_HttpThread ----> signature body: {0}", System.Convert.ToBase64String(bytes));
                        #endif
                        mFrodo.LL2(next.requestBytes, 0);
                    }
                    DebugEx.FormatTrace("HttpGameClient::_HttpThread ----> begin request {0}, [SeqId]{1}", next.uri, next.seqId);
                    var request = UnityWebRequest.Put(next.uri, next.requestBytes);
                    //request.downloadHandler = new DownloadHandlerBuffer();
                    request.method = "POST";
                    request.SetRequestHeader("using-sandwich-raw-packet", "true");

                    if (next.needToken)
                    {
                        request.SetRequestHeader("Authorization", string.Format("Bearer {0}", mToken));
                        if(Game.Instance != null)
                        {
                            request.SetRequestHeader("client_send_ts", Game.Instance.GetTimestampSeconds().ToString());
                        }
                    }
                    if(Game.Instance?.appSettings != null)
                    {
                        request.SetRequestHeader("tmp-build-version", Game.Instance.appSettings.versionCode);
                    }
                    string distinctId = PlatformSDK.Instance.GetTrackDistinctId();
                    request.SetRequestHeader("TGA-Distinct-ID", distinctId);
                    if(mAdditionalHeader != null)
                    {
                        foreach(var entry in mAdditionalHeader)
                        {
                            request.SetRequestHeader(entry.Key, entry.Value);
                        }
                    }
                    codec.PreProcessRequest(request);
                    if (next.mimelen > 0)
                    {
                        request.SetRequestHeader("Content-Type", request.GetRequestHeader("Content-Type") + "-sign");
                    }
                    request.useHttpContinue = Game.Manager?.configMan?.globalConfig?.ClientHttpContinue ?? false;
                    request.timeout = Game.Manager?.configMan?.globalConfig?.ClientHttpTimeout ?? 30;
                    DebugEx.FormatTrace("HttpGameClient::_HttpThread ----> real send request [SeqId]{0}:{1}", next.seqId, codec.Dump(next.rawRequestStruct));
                    next.response = request.SendWebRequest();
                }
                else if(next.response.isDone)
                {
                    if(next.response.webRequest.isNetworkError)
                    {
                        //network error happened, retry
                        if(next.retryCountLeft > 0)
                        {
                            DebugEx.FormatTrace("HttpGameClient::_HttpThread ----> network error {0}, retry {1} request [SeqId]{2}:{3}", next.response.webRequest.error, next.retryCountLeft, next.seqId, next.uri);
                            next.retryCountLeft --;
                            next.response = null;
                            mWaitCountDown = 5;
                            long errCode = ErrorCodeUtility.ConvertToCommonCode(10016, ErrorCodeType.HttpError);
                            ThreadDispatcher.DefaultDispatcher.Dispatch(() => {
                                mOnErrorRetry?.Invoke(errCode);
                            });
                        }
                    }
                    if(next.response != null)
                    {
                        DebugEx.FormatTrace("HttpGameClient::_HttpThread ----> end request [SeqId]{0}, {1}", next.seqId, next.uri);
                        next.timeEnd = System.DateTime.Now;
                        mRequestQueue.Dequeue();
                        var act = next.onFinish;
                        if (act != null)
                        {
                            var ret = _HandleProtoHttpResponse(next, null, codec, codec);
                            ThreadDispatcher.DefaultDispatcher.Dispatch(() => act(ret));
                        }
                    }
                }
            }
        }

        private void _CalculateRequestBase64(ref RequestStub stub)
        {
            if(stub.requestStream != null && stub.requestStream.Length > 0 && string.IsNullOrEmpty(stub.requestBase64))
            {
                stub.requestBase64 = _CalculateBase64(stub.requestStream.GetBuffer(), (int)stub.requestStream.Length);
                DebugEx.FormatInfo("HttpGameClient::_CalculateRequestBase64 ----> {0}:{1}", stub.uri, stub.requestBase64);
            }
        }

        private string _CalculateBase64(byte[] data, int length)
        {
            return System.Convert.ToBase64String(data, 0, length);
        }

        private bool _IsOpenSign(RequestStub stub)
        {
            if(stub.needToken)
            {
                if(Game.Manager.configMan.globalConfig == null || Game.Manager.configMan.globalConfig.HobbitsName2 != 7)
                {
                    return true;
                }
            }
            return false;
        }

        private byte[] mCachedSignBuffer;
        private NetResponse _HandleProtoHttpResponse(RequestStub stub, System.Exception error, IProtobufCodec codec, IProtobufCodec mainThreadCodec)
        {
#if UNITY_EDITOR
            _CalculateRequestBase64(ref stub);
#endif
            NetResponse ret = new NetResponse();
            if (stub.response.webRequest.isHttpError || stub.response.webRequest.isNetworkError)
            {
                long errorCode = 0;
                if(stub.response.webRequest.isHttpError)
                {
                    errorCode = ErrorCodeUtility.ConvertToCommonCode(stub.response.webRequest.responseCode,  ErrorCodeType.HttpError);
                }
                else
                {
                    errorCode = ErrorCodeUtility.ConvertToCommonCode(10016/*这是HttpWebRequest里的未知网络错误，用来做错误码兼容*/,  ErrorCodeType.HttpError);
                }
                _CalculateRequestBase64(ref stub);
                ThreadDispatcher.DefaultDispatcher.Dispatch(() => {
                    DataTracker.TrackNetworkError(string.Format("send error: [{0}],[{1}]", stub.response.webRequest.error, error?.Message), error?.StackTrace, stub.payload, DataTracker.LogErrorType.network_send, stub.timeStart, stub.timeEnd, stub.requestBase64, errorCode, "", stub.uri, stub.requestBytes?.LongLength ?? 0);
                });
                ret.SetError(errorCode, (ErrorCodeType)0, "net error");
            }
            else
            {
                gen.netutils.RawPacket packet = null;
                try
                {
                    var bytes = stub.response.webRequest.downloadHandler.data;
                    //验证签名
                    int frodolen = _IsOpenSign(stub)?mFrodo.GL():0;
                    string serverSign = stub.response.webRequest.GetResponseHeader(Constant.kSignHeader);
                    if(frodolen > 0 && !string.IsNullOrEmpty(serverSign))
                    {
                        if(mCachedSignBuffer == null || mCachedSignBuffer.Length != frodolen)
                        {
                            mCachedSignBuffer = new byte[frodolen];
                        }
                        if(mUrlBytes == null)           //放到这里，是为了让代码更难读（一个超大的函数，反编译以后不好辨认）
                        {
                            string str = FrodoUtility.ITK(Constant.kUrl);
                            mUrlBytes = System.Text.Encoding.UTF8.GetBytes(str);
                        }
                        mFrodo.RR();
                        mFrodo.RL(bytes, 0, (uint)bytes.Length);
                        mFrodo.RL(mUrlBytes, 0, (uint)mUrlBytes.Length);
                        mFrodo.LL2(mCachedSignBuffer, 0);
                        if(serverSign != null && serverSign.Length == frodolen)
                        {
                            while(frodolen > 0)
                            {
                                if(serverSign[frodolen - 1] != mCachedSignBuffer[frodolen - 1])
                                {
                                    //签名验证失败
                                    break;
                                }
                                frodolen--;
                            }
                        }

                        #if UNITY_EDITOR
                        if(frodolen > 0)
                        {
                            string myMd5 = "";
                            for(int i = 0; i < mCachedSignBuffer.Length; i++)
                            {
                                myMd5 += (char)mCachedSignBuffer[i];
                            }
                            DebugEx.FormatError("[SIGN]check error {0} vs {1}", myMd5, serverSign);
                        }
                        #endif
                    }
                    //处理协议
                    using (mMemoryPool.AllocStub(out var respStream))
                    {
                        respStream.Write(bytes, 0, bytes.Length);
                        respStream.Seek(0, System.IO.SeekOrigin.Begin);
                        packet = codec.UnmarshalFromStream<gen.netutils.RawPacket>(respStream);
                        if(packet == null)
                        {
                            DebugEx.FormatWarning("HttpGameClient::_HandleProtoHttpResponse ----> decode error");
                        }
                    }
                    DebugEx.FormatTrace("HttpGameClient::_HandleProtoHttpResponse ----> msg:{0}", packet!=null?codec.Dump(packet):"null");
                    if (packet == null)
                    {
                        _CalculateRequestBase64(ref stub);
                        ret.SetError((int)GameErrorCode.EmptyResponse, ErrorCodeType.GameError,  "no response");
                        ThreadDispatcher.DefaultDispatcher.Dispatch(() => {
                            DataTracker.TrackNetworkError("response error: EmptyResponse", "", stub.payload, DataTracker.LogErrorType.network_response, stub.timeStart, stub.timeEnd, stub.requestBase64, ret.errCode, _CalculateBase64(bytes, bytes.Length), stub.uri, stub.requestBytes?.LongLength ?? 0);
                        });
                    }
                    else
                    {
                        if(packet.RawAny.Count > 0)
                        {
                            var rawAny = packet.RawAny[0];
                            if(stub.rawRequestStruct.RawAny.Count > 0 && rawAny.PassThrough != stub.rawRequestStruct.RawAny[0].PassThrough)
                            {
                                //passthrough对不上
                                long errorCode = (long)GameErrorCode.ServerSignError;
                                _CalculateRequestBase64(ref stub);
                                ThreadDispatcher.DefaultDispatcher.Dispatch(() => {
                                    DataTracker.TrackNetworkError(string.Format("send error: [{0}],[{1}],[{2}]", "x", rawAny.PassThrough, stub.rawRequestStruct.RawAny[0].PassThrough), error?.StackTrace, stub.payload, DataTracker.LogErrorType.network_send, stub.timeStart, stub.timeEnd, stub.requestBase64, errorCode, _CalculateBase64(bytes, bytes.Length), stub.uri, stub.requestBytes?.LongLength ?? 0);
                                });
                                ret.SetError(errorCode, (ErrorCodeType)0, "net error");
                            }
                            else if(rawAny.Uri == "netutils.ErrorResponse")
                            {
                                //error
                                _CalculateRequestBase64(ref stub);
                                var errorResponse = codec.UnmarshalFromByteString<gen.netutils.ErrorResponse>(rawAny.Raw);
                                ret.SetError(errorResponse);
                                DebugEx.FormatWarning("HttpGameClient::_HandleProtoHttpResponse ----> error:{0}", errorResponse.Message);
                                if(errorResponse.Code == 1)
                                {
                                    ThreadDispatcher.DefaultDispatcher.Dispatch(() => {
                                        DataTracker.TrackNetworkError("logic error: ErrorResponse", "", stub.payload, DataTracker.LogErrorType.network_logic, stub.timeStart, stub.timeEnd, stub.requestBase64, ret.errCode, errorResponse.Message, stub.uri, stub.requestBytes?.LongLength ?? 0);
                                    });
                                }
                                // ret.SetError(errorResponse);
                            }
                            else
                            {
                                if(frodolen > 0)
                                {
                                    //签名验证失败的处理
                                    long errorCode = (long)GameErrorCode.ServerSignError;
                                    _CalculateRequestBase64(ref stub);
                                    ThreadDispatcher.DefaultDispatcher.Dispatch(() => {
                                        DataTracker.TrackNetworkError(string.Format("send error: [{0}],[{1}],[{2}],[{3}]", "xx", error?.Message, serverSign, frodolen), error?.StackTrace, stub.payload, DataTracker.LogErrorType.network_send, stub.timeStart, stub.timeEnd, stub.requestBase64, errorCode, _CalculateBase64(bytes, bytes.Length), stub.uri, stub.requestBytes?.LongLength ?? 0);
                                    });
                                    ret.SetError(errorCode, (ErrorCodeType)0, "net error");
                                }
                                else
                                {
                                    ret.SetMessage(rawAny, mainThreadCodec);
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    _CalculateRequestBase64(ref stub);
                    ret.SetError((int)GameErrorCode.DecodeError, ErrorCodeType.GameError, ex.ToString());
                    ThreadDispatcher.DefaultDispatcher.Dispatch(() => {
                        DataTracker.TrackNetworkError(string.Format("decode error: {0}", ex?.Message), ex?.StackTrace, stub.payload, DataTracker.LogErrorType.network_response, stub.timeStart, stub.timeEnd, stub.requestBase64, ret.errCode, "", stub.uri, stub.requestBytes?.LongLength ?? 0);
                    });
                }
            }
            return ret;
        }
    }
}