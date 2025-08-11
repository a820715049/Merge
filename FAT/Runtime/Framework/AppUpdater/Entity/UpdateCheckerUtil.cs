/*
 * @Author: qun.chao
 * @Date: 2025-06-13 14:37:20
 */
using CenturyGame.AppUpdaterLib.Runtime;
using CenturyGame.AppUpdaterLib.Runtime.Configs;
using CenturyGame.AppUpdaterLib.Runtime.Managers;
using gen.msg;
using gen.netutils;
using static CenturyGame.AppUpdaterLib.Runtime.Configs.LighthouseConfig;

using System.Collections.Generic;
using System.Threading;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using EL;
using ProtokitHelper;
using ProtokitHelper.Http;

namespace FAT.AppUpdater
{
    public class UpdateCheckerUtil
    {
        public static void Info(string message)
        {
            DebugEx.Info($"[UpdateChecker] {message}");
        }

        public static void Error(string message)
        {
            DebugEx.Info($"[UpdateChecker] [Error] {message}");
        }

        public static bool IsForceUpdate(LighthouseConfig lighthouseConfig)
        {
            var server = GetCurrentServerData(lighthouseConfig);
            return server == null || server.MaintenanceInfo.ForceUpdate;
        }

        public static bool HasHotUpdate(string dataVersion, string resVersion)
        {
            if (string.IsNullOrEmpty(dataVersion) || string.IsNullOrEmpty(resVersion))
            {
                return false;
            }
            var appInfoAfterUpdate = AppUpdaterManager.AppUpdaterGetAppInfoManifest();
            if (appInfoAfterUpdate == null)
            {
                return false;
            }
            if (!string.Equals(dataVersion, appInfoAfterUpdate.dataResVersion) ||
                !string.Equals(resVersion, appInfoAfterUpdate.unityDataResVersion))
            {
                return true;
            }
            return false;
        }

        public static async UniTask<LighthouseConfig> GetLighthouseConfig(CancellationToken token)
        {
            var lighthouseUrl = GetLighthouseUrl(null);
            Info("GetLighthouseConfig: " + lighthouseUrl);

            using var request = UnityWebRequest.Get(lighthouseUrl);
            await request.SendWebRequest().WithCancellation(token);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Error("GetLighthouseConfig: " + request.error);
                return null;
            }
            return ReadFromJson(request.downloadHandler.text);
        }

        public static LighthouseConfig.Server GetCurrentServerData(LighthouseConfig lighthouseConfig)
        {
            var serves = lighthouseConfig.ServersData.Servers;
            var curVersion = Game.Instance.appSettings.version;
            for (var i = 0; i < serves.Count; i++)
            {
                var serverData = serves[i];
                if (serverData.CanBeUseForVersion(curVersion))
                {
                    return serverData;
                }
            }
            return null;
        }

        private static string GetRemoteRootUrlFromConfig(FileServerType fileServerType)
        {
            var appUpdaterConfig = AppUpdaterConfigManager.AppUpdaterConfig;
            var serverUrl = (fileServerType == FileServerType.CDN) ? appUpdaterConfig.cdnUrl : appUpdaterConfig.ossUrl;
            return string.IsNullOrWhiteSpace(appUpdaterConfig.remoteRoot) ? serverUrl : $"{serverUrl}/{appUpdaterConfig.remoteRoot}";
        }

        /// <summary>
        /// 获取Lighthouse 配置的url
        /// </summary>
        /// <param name="lightHouseId">Lighthouse配置的id</param>
        /// <param name="fileServerType">文件服务器的类型</param>
        /// <returns></returns>
        private static string GetLighthouseUrl(string lightHouseId, FileServerType fileServerType = FileServerType.CDN)
        {
            var curTime = System.DateTimeOffset.Now.ToUnixTimeSeconds() / 60;
            var appUpdaterConfig = AppUpdaterConfigManager.AppUpdaterConfig;

            string url;

            if (!string.IsNullOrEmpty(lightHouseId))
                url = $"{GetRemoteRootUrlFromConfig(fileServerType)}/{appUpdaterConfig.channel}-lighthouse.json_{lightHouseId}?v={curTime}&t={curTime}";
            else
                url = $"{GetRemoteRootUrlFromConfig(fileServerType)}/{appUpdaterConfig.channel}-lighthouse.json?v={curTime}&t={curTime}";

            return url;
        }

        public class EntryPointRequester
        {
            public string DataVersion { get; private set; }
            public string ResVersion { get; private set; }
            public int RequestId { get; private set; }

            public void Clear()
            {
                DataVersion = null;
                ResVersion = null;
                Inc();
            }

            public void Cancel() => Inc();

            private void Inc()
            {
                ++RequestId;
            }

            public int SendEntryPointRequest(string serverUrl)
            {
                var curUrl = MakeLighthouseUrl(serverUrl);
                curUrl = OptimizeUrl(curUrl);
                var request = new EntryPointRequest
                {
                    AppVersion = Game.Instance.appSettings.version,
                    LighthouseId = string.Empty,
                    From = EntryPointFromType.Cdn
                };
                var header = new Dictionary<string, string>
                {
                    {"Content-Type", "application/x-protobuf"}, {"charset", "utf-8"}
                };
                var metadata = new Dictionary<string, string>
                {
                    { MetadataKey.AppVersion.ToString(), Game.Instance.appSettings.version },
                    { MetadataKey.Channel.ToString(), AppUpdaterConfigManager.AppUpdaterConfig.channel },
                    { MetadataKey.Language.ToString(), "en" }
                };
                var clientUniqueId = AppUpdaterManager.ClientUniqueId;
                if (!string.IsNullOrEmpty(clientUniqueId))
                {
                    metadata.Add(MetadataKey.ClientId.ToString(), clientUniqueId);
                }
                var data = ProtokitUtil.Instance.GetSendBytesFromProto(request, metadata, ProtoPacketType.RawPacket);
                ++RequestId;
                var reqId = RequestId;
#if UNITY_WEBGL && !UNITY_EDITOR
                HttpManager.Instance.PostUnityWebRequest(curUrl, header, data, (requestInfo, recvData) => OnEntryPointResponse(reqId, requestInfo, recvData));
#else
                HttpManager.Instance.PostHttpRequest(curUrl, header, data, (requestInfo, recvData) => OnEntryPointResponse(reqId, requestInfo, recvData));
#endif
                return reqId;
            }

            private string MakeLighthouseUrl(string serverUrl)
            {
                return serverUrl + "lighthouse";
            }

            private string OptimizeUrl(string url)
            {
                if (url[^1].Equals('/'))
                {
                    url = url[..^1];
                }
                return url;
            }

            public void OnEntryPointResponse(int reqId, HttpRequestInfo requestInfo, byte[] recvData)
            {
                if (reqId != RequestId)
                {
                    return;
                }
                Inc();
                if (!requestInfo.IsSuccess || recvData == null || recvData.Length == 0)
                {
                    Error($"OnEntryPointResponse failed, requestInfo: {requestInfo}, recvData: {recvData}");
                    return;
                }

                ErrorResponse errorMsg = null;
                EntryPointResponse msg = null;
                bool isEncrypt = false;
                bool isCompress = false;

                var rp = RawPacket.Parser.ParseFrom(recvData);
                if (rp.Metadata.Count > 0)
                {
                    if (rp.Metadata.Count % 2 == 0)
                    {
                        var metadata = new Dictionary<string, string>();
                        for (int i = 0; i < rp.Metadata.Count; i = i + 2)
                        {
                            string key = rp.Metadata[i];
                            string value = rp.Metadata[i + 1];
                            metadata[key] = value;
                        }
                        isEncrypt = ProtokitUtil.Instance.CheckMetadataEncrypt(metadata);
                        isCompress = ProtokitUtil.Instance.CheckMetadataCompress(metadata);
                    }
                    else
                        Error($"response message's metadata key-value is not in pair. the count is {rp.Metadata.Count}");
                }

                var uri = rp.RawAny[0].Uri;
                var rawdata = rp.RawAny[0].Raw.ToByteArray();
                var msgData = ProtokitUtil.Instance.GetProtoData(rawdata, isEncrypt, isCompress);
                if (ErrorResponse.Descriptor.FullName.Equals(uri))
                {
                    errorMsg = ErrorResponse.Parser.ParseFrom(msgData);
                }
                else if (EntryPointResponse.Descriptor.FullName.Equals(uri))
                {
                    msg = EntryPointResponse.Parser.ParseFrom(msgData);
                }
                else
                {
                    Error($"GetResourceVersionRequest response unexpect message {uri}");
                }

                if (errorMsg != null)
                {
                    Error($"ErrorResponse code : {errorMsg.Code}");
                    return;
                }
                else if (msg != null)
                {
                    Info($"Recv EntryPointResponse:{msg}");
                    var redirectUrl = msg.RedirectURL;
                    if (!string.IsNullOrEmpty(msg.RedirectURL))
                    {
                        redirectUrl = OptimizeUrl(redirectUrl);
                        if (!string.Equals(redirectUrl, requestInfo.Url))
                        {
                            Info($"redirect request , redirect url : {msg.RedirectURL} .");
                            return;
                        }
                    }

                    if (msg.ResourceDetail != null)
                    {
#if UNITY_ANDROID
                        DataVersion = msg.ResourceDetail.DataVersion;
                        ResVersion = msg.ResourceDetail.AndroidVersion.Md5;
#elif UNITY_IOS
                        DataVersion = msg.ResourceDetail.DataVersion;
                        ResVersion = msg.ResourceDetail.IOSVersion.Md5;
#else
                        throw new InvalidOperationException($"Invalid platform : {Application.platform} .");
#endif
                    }
                }
            }
        }
    }
}