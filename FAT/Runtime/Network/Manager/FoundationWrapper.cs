/*
 * @Author: qun.chao
 * @Date: 2023-11-03 16:13:41
 */
using System.Collections.Generic;
using Google.Protobuf;
using EL;
using CenturyGame.Foundation.RunTime;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.IO;

// foundation里默认已经包含了一些协议，比如storage相关
// storage.proto生成的cs文件应该删除，避免ProtokitUtil类记录错误的Parser
/*
ProtokitUtil.cs
                    if (!(prop.GetValue(null, null) is MessageParser parser)) continue;
                    var p = (IMessage)Activator.CreateInstance(t);
                    ProtoParserMap[p.Descriptor.FullName] = parser;
*/
// payment.proto 和 authorize.proto 可能也有类似问题
namespace FAT
{
    // 不应该直接直接使用foundation提供的序列化反序列化封装
    public static class FoundationWrapper
    {
        public static int RecentHttpErrorStatusCode { get; private set; }
        private static readonly Dictionary<string, string> _metaData = new();
        private static CancellationTokenSource ctsForCommonTask = new();

        private static MemoryStream _UTF8Buffer = new MemoryStream();
        public static ByteString MarshalToByteString(IMessage msg)
        {
            _UTF8Buffer.Position = 0;
            _UTF8Buffer.SetLength(0);
            msg.WriteTo(_UTF8Buffer);
            _UTF8Buffer.Position = 0;
            return ByteString.FromStream(_UTF8Buffer);
        }

        public static void EnsureMetaData(bool forceRefresh = false)
        {
            if (_metaData.Count < 1 || forceRefresh)
            {
                _metaData.Clear();
                _metaData.Add(nameof(fat.netutils.MetadataKey.AppVersion), Game.Instance.appSettings.version ?? string.Empty);
                _metaData.Add(nameof(fat.netutils.MetadataKey.Channel), GameUpdateManager.Instance.channel ?? string.Empty);
            }
        }

        private static string urlRoot => GameUpdateManager.Instance.httpRoot;
        public static SimpleResultedAsyncTask<IMessage> PostMessage(IMessage req, string queryPath, string uriRequest)
        {
            EnsureMetaData();
            var url = $"{urlRoot}{queryPath}";
            DebugEx.Info($"PostMessage Current req url : {url}, {uriRequest}, request:{req}");
            var task = new SimpleResultedAsyncTask<IMessage>();
            FdServerManager.Instance.HttpClient.PostRequestMsg(url,
                req,
                msg =>
                {
                    RecentHttpErrorStatusCode = -1;
                    Game.Manager.networkMan.OnExitNetworkWeak(); //收到消息后认为弱网状态结束

                    // 协议按uri进行解析时 未能稳定的解析error为某一个类型 所以两个error都处理
                    if (msg is gen.netutils.ErrorResponse errResp)
                    {
                        DebugEx.Error($"PostMessage {uriRequest} , gen ErrorResponse , msg = {msg}");
                        var errorCode = errResp.Code;
                        task.Fail(null, errorCode);
                        Game.Manager.networkMan.TransStateByErrorCode(ErrorCodeUtility.ConvertToCommonCode(errorCode, ErrorCodeType.ServerError));//解析错误码 并做出相应处理
                    }
                    else if (msg is fat.netutils.ErrorResponse errRespFat)
                    {
                        DebugEx.Error($"PostMessage {uriRequest} , fat ErrorResponse , msg = {msg}");
                        var errorCode = errRespFat.Code;
                        task.Fail(null, errorCode);
                        Game.Manager.networkMan.TransStateByErrorCode(ErrorCodeUtility.ConvertToCommonCode(errorCode, ErrorCodeType.ServerError));//解析错误码 并做出相应处理
                    }
                    else
                    {
                        DebugEx.Info($"PostMessage receive msg Success , url = {uriRequest} , msg = {msg}");
                        task.Success(msg);
                    }
                },
                statusCode =>
                {
                    RecentHttpErrorStatusCode = statusCode;
                    DebugEx.Error($"PostMessage {uriRequest}, http error : statusCode={statusCode}");
                    task.Fail(null, statusCode);
                    //这里收到的都是http请求相关的错误码,跟游戏业务逻辑无关,这里收到错误码后,统一按照网络状况不好处理
                    Game.Manager.networkMan.OnEnterNetworkWeak();
                    FdServerManager.Instance.HttpClient.Stop();//stop to reset EnableSendRequest

                    Cancel();
                },
                _metaData,
                null,
                uriRequest);
            PreventEndlessTask(uriRequest, task, ctsForCommonTask.Token);
            return task;
        }

        public static void Cancel()
        {
            ctsForCommonTask?.Cancel();
            ctsForCommonTask.Dispose();
            ctsForCommonTask = new();
        }

        private static async void PreventEndlessTask(string uri, SimpleResultedAsyncTask<IMessage> task, CancellationToken token)
        {
            try
            {
                await UniTask.WaitWhile(() => task.keepWaiting).AttachExternalCancellation(token);
            }
            catch (System.OperationCanceledException)
            {
                DebugEx.Info($"PostMessage canceled {uri}");
            }
            await UniTask.Yield();
            if (task.keepWaiting)
            {
                // 网络异常 且 无法正常结束
                task.Fail("force fail", 0xbad);
            }
        }

        //设置http消息预期返回时间
        public static void SetHttpExpectTime(float time)
        {
            if (time <= 0f)
                return;
            FdServerManager.Instance.HttpClient.SetExpectTime(time);
        }
        
        //http请求超时设置回调
        public static void SetHttpReqTimeOutCb()
        {
            if (Game.Manager.networkMan != null)
            {
                FdServerManager.Instance.HttpClient.Evt_ReqTimeOutExpect -= Game.Manager.networkMan.OnEnterNetworkWeak;
                FdServerManager.Instance.HttpClient.Evt_ReqTimeOutExpect += Game.Manager.networkMan.OnEnterNetworkWeak;
                FdServerManager.Instance.HttpClient.Evt_ReqFailed -= Game.Manager.networkMan.OnEnterNetworkWeak;
                FdServerManager.Instance.HttpClient.Evt_ReqFailed += Game.Manager.networkMan.OnEnterNetworkWeak;
            }
        }
    }
}