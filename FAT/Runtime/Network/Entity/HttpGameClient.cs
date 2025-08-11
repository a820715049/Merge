/**
 * @Author: handong.liu
 * @Date: 2020-09-25 16:30:35
 */
using UnityEngine.Networking;
using System.Text;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using EL;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using FAT;
using FAT.Platform;

namespace GameNet
{
    public class HttpGameClient : IHttpGameClient
    {
        struct RequestStub
        {
            public HttpWebRequest request;
            public IMessage payload;
            public System.IO.MemoryStream requestStream;
            public string requestBase64;
            public System.Action<NetResponse> onFinish;
            public System.DateTime timeStart;
            public System.DateTime timeEnd;
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


        public void SetErrorRetryCB(System.Action<long> cb)
        {

        }
        public void InitWithConfig(HttpClientConfig config)
        {
            mConfig = config;
        }

        public void SetHobbitsName(int hn)
        {
        }

        public void Dispose()
        {
            mDisposed = true;
        }

        public void SetToken(string token)
        {
            mToken = token;
        }

        public void Update(float dt)
        {

        }

        public void RequestPost(string path, bool needToken, System.Action<NetResponse> onResponse, IMessage data, int retryCount)
        {
            var url = needToken ? string.Format("{0}/api/v1/auto/{1}", mConfig.urlRoot, path) : string.Format("{0}/api/v1/entry/auto/{1}", mConfig.urlRoot, path);
            DebugEx.FormatTrace("HttpGameClient.RequestPost ----> url:{0}, body:{1}", url, data);
            var request = HttpWebRequest.Create(url) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            _SendRequest(request, needToken, data, onResponse);
        }

        public void AddAdditionalHeader(string key, string val)
        {
            mAdditionalHeader[key] = val;
        }

        public void RemoveAdditionalHeader(string key)
        {
            mAdditionalHeader.Remove(key);
        }

        private void _SendRequest(HttpWebRequest request, bool needToken, IMessage message, System.Action<NetResponse> onResponse)
        {
            if (needToken)
            {
                request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Bearer {0}", mToken));
                if (Game.Instance != null)
                {
                    request.Headers.Add("client_send_ts", Game.Instance.GetTimestampSeconds().ToString());
                }
            }
            if (Game.Instance?.appSettings != null)
            {
                request.Headers.Add("TMP_BUILD_VERSION", Game.Instance.appSettings.versionCode);
            }
            string distinctId = PlatformSDK.Instance.GetTrackDistinctId();
            request.Headers.Add("TGA-Distinct-ID", distinctId);
            if (mAdditionalHeader != null)
            {
                foreach (var entry in mAdditionalHeader)
                {
                    request.Headers.Add(entry.Key, entry.Value);
                }
            }
            request.Timeout = mConfig.timeout;
            lock (mRequestQueue)
            {
                DebugEx.FormatInfo("HttpGameClient._SendRequest ----> {0}, distinctId:{1}, request {2}", request.Method, distinctId, request.RequestUri);
                mRequestQueue.Enqueue(new RequestStub() { request = request, payload = message, onFinish = onResponse });
                if (!mThreadingRunning)
                {
                    mThreadingRunning = true;
                    var thread = new Thread(_HttpThread);
                    thread.Start();
                }
            }
        }

        private void _SetMetadata(gen.netutils.RawPacket rp)
        {
            rp.Metadata.Add(gen.netutils.MetadataKey.AppVersion.ToString());
            rp.Metadata.Add(Game.Instance.appSettings.version);

            rp.Metadata.Add(gen.netutils.MetadataKey.Channel.ToString());
            rp.Metadata.Add(Game.Instance.appSettings.updaterConfig.channel);

            rp.Metadata.Add(gen.netutils.MetadataKey.Language.ToString());
            rp.Metadata.Add("en");

            var clientUniqueId = Game.Instance.appSettings.clientId;
            if (!string.IsNullOrEmpty(clientUniqueId))
            {
                rp.Metadata.Add(gen.netutils.MetadataKey.ClientId.ToString());
                rp.Metadata.Add(clientUniqueId);
            }
        }

        private void _HttpThread()
        {
            var client = this;
            while (true)
            {
                var requestQueue = client.mRequestQueue;
                HttpWebResponse resp = null;
                RequestStub next;
                IProtobufCodec codec = mCodec;
                IProtobufCodec mainThreadCodec = mCodecMainThread;
                lock (requestQueue)
                {
                    if (requestQueue.Count == 0)
                    {
                        client.mThreadingRunning = false;
                        break;
                    }
                    next = requestQueue.Dequeue();
                }
                next.timeStart = System.DateTime.Now;
                next.requestStream = mMemoryPool.Alloc();
                DebugEx.FormatTrace("HttpGameClient._HttpThread ----> begin request {0}", next.request.RequestUri);

                var rawPacket = new gen.netutils.RawPacket();
                rawPacket.SequenceID = (int)mSequenceId++;
                rawPacket.Version = 1;
                _SetMetadata(rawPacket);
                codec.PreProcessRequest(next.request);
                if (next.payload != null)
                {
                    var rawAny = new gen.netutils.RawAny();
                    rawAny.Uri = next.payload.Descriptor.FullName;
                    //rawAny.NextWhenError = true;
                    rawAny.Raw = codec.MarshalToByteString(next.payload);
                    rawPacket.RawAny.Add(rawAny);
                }
                System.Exception networkError = null;
                try
                {
                    next.request.Headers.Add("using-sandwich-raw-packet", "true");
                    DebugEx.FormatTrace("HttpGameClient._HttpThread ----> real send request:{0}", codec.Dump(rawPacket));
                    next.requestStream.SetLength(0);
                    codec.MarshalToStream(next.requestStream, rawPacket);
                    using (var stream = next.request.GetRequestStream())
                    {
                        if (next.requestStream.Length > 0)
                        {
                            stream.Write(next.requestStream.GetBuffer(), 0, (int)next.requestStream.Length);
                        }
                        stream.Close();
                    }
                    resp = next.request.GetResponse() as HttpWebResponse;
                }
                catch (System.Exception ex)
                {
                    networkError = ex;
                    DebugEx.FormatWarning("HttpGameClient._HttpThread ----> request exception {0}", ex.ToString());
                }
                next.timeEnd = System.DateTime.Now;

                DebugEx.FormatTrace("HttpGameClient._HttpThread ----> end request {0}", next.request.RequestUri);
                var act = next.onFinish;
                if (act != null)
                {
                    if (networkError is System.AggregateException agg)
                    {
                        networkError = agg.InnerException;
                    }
                    var ret = _HandleProtoHttpResponse(next, resp, networkError, codec, mainThreadCodec);
                    ThreadDispatcher.DefaultDispatcher.Dispatch(() => act(ret));
                }
                mMemoryPool.Free(next.requestStream);
                next.requestStream = null;
            }
        }

        private void _CalculateRequestBase64(ref RequestStub stub)
        {
            if (stub.requestStream != null && stub.requestStream.Length > 0 && string.IsNullOrEmpty(stub.requestBase64))
            {
                stub.requestBase64 = System.Convert.ToBase64String(stub.requestStream.GetBuffer(), 0, (int)stub.requestStream.Length);
                DebugEx.FormatInfo("HttpGameClient::_CalculateRequestBase64 ----> {0}:{1}", stub.request.RequestUri, stub.requestBase64);
            }
        }

        private NetResponse _HandleProtoHttpResponse(RequestStub stub, HttpWebResponse resp, System.Exception error, IProtobufCodec codec, IProtobufCodec mainThreadCodec)
        {
#if UNITY_EDITOR
            _CalculateRequestBase64(ref stub);
#endif
            NetResponse ret = new NetResponse();
            ret.requestStart = stub.timeStart;
            ret.requestEnd = stub.timeEnd;
            if (resp == null)
            {
                long errorCode = 0;
                if (error is WebException webError)
                {
                    errorCode = ErrorCodeUtility.ConvertToCommonCode((int)webError.Status + 10000, ErrorCodeType.HttpError);
                }
                else
                {
                    errorCode = ErrorCodeUtility.ConvertToCommonCode(GameErrorCode.Unknown);
                }
                _CalculateRequestBase64(ref stub);
                ThreadDispatcher.DefaultDispatcher.Dispatch(() =>
                {
                    DataTracker.TrackNetworkError(string.Format("{0}, send error: {1}", stub.request.RequestUri, error?.Message), error?.StackTrace, stub.payload, DataTracker.LogErrorType.network_send, stub.timeStart, stub.timeEnd, stub.requestBase64);
                });
                ret.SetError(errorCode, (ErrorCodeType)0, "no connection");
            }
            else if (resp.StatusCode != HttpStatusCode.OK)
            {
                _CalculateRequestBase64(ref stub);
                ThreadDispatcher.DefaultDispatcher.Dispatch(() =>
                {
                    DataTracker.TrackNetworkError(string.Format("{0}, http error: {1}", stub.request.RequestUri, error?.Message), error?.StackTrace, stub.payload, DataTracker.LogErrorType.network_send, stub.timeStart, stub.timeEnd, stub.requestBase64);
                });
                ret.SetError((int)resp.StatusCode, ErrorCodeType.HttpError, "http error");
            }
            else
            {
                gen.netutils.RawPacket packet = null;
                try
                {
                    using (var respStream = resp.GetResponseStream())
                    {
                        packet = codec.UnmarshalFromStream<gen.netutils.RawPacket>(respStream);
                        if (packet == null)
                        {
                            DebugEx.FormatWarning("HttpGameClient::_HandleProtoHttpResponse ----> decode error");
                        }
                    }
                    DebugEx.FormatTrace("HttpGameClient::_HandleProtoHttpResponse ----> msg:{0}", packet != null ? codec.Dump(packet) : "null");
                    if (packet == null)
                    {
                        _CalculateRequestBase64(ref stub);
                        ThreadDispatcher.DefaultDispatcher.Dispatch(() =>
                        {
                            DataTracker.TrackNetworkError(string.Format("{0}, response error: EmptyResponse", stub.request.RequestUri), "", stub.payload, DataTracker.LogErrorType.network_response, stub.timeStart, stub.timeEnd, stub.requestBase64);
                        });
                        ret.SetError((int)GameErrorCode.EmptyResponse, ErrorCodeType.GameError, "no response");
                    }
                    else
                    {
                        if (packet.RawAny.Count > 0)
                        {
                            if (packet.RawAny[0].Uri == "netutils.ErrorResponse")
                            {
                                //error
                                _CalculateRequestBase64(ref stub);
                                var errorResponse = codec.UnmarshalFromByteString<gen.netutils.ErrorResponse>(packet.RawAny[0].Raw);
                                ret.SetError(errorResponse);
                                DebugEx.FormatWarning("HttpGameClient::_HandleProtoHttpResponse ----> error:{0}", errorResponse.Message);
                                ThreadDispatcher.DefaultDispatcher.Dispatch(() =>
                                {
                                    DataTracker.TrackNetworkError(string.Format("{0}, logic error: EmptyResponse", stub.request.RequestUri), "", stub.payload, DataTracker.LogErrorType.network_logic, stub.timeStart, stub.timeEnd, stub.requestBase64);
                                });
                                ret.SetError(errorResponse);
                            }
                            else
                            {
                                ret.SetMessage(packet.RawAny[0], mainThreadCodec);
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    _CalculateRequestBase64(ref stub);
                    ThreadDispatcher.DefaultDispatcher.Dispatch(() =>
                    {
                        DataTracker.TrackNetworkError(string.Format("{0}, decode error: {1}", stub.request.RequestUri, ex?.Message), ex?.StackTrace, stub.payload, DataTracker.LogErrorType.network_response, stub.timeStart, stub.timeEnd, stub.requestBase64);
                    });
                    ret.SetError((int)GameErrorCode.DecodeError, ErrorCodeType.GameError, ex.ToString());
                }
            }
            return ret;
        }
    }
}