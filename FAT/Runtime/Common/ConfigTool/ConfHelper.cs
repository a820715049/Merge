/*
 * @Author: qun.chao
 * @Date: 2023-12-11 15:29:19
 */

using System;
using System.Linq;
using CenturyGame.AppUpdaterLib.Runtime;
using CenturyGame.AppUpdaterLib.Runtime.Managers;
using Google.Protobuf;
using Cysharp.Threading.Tasks;
using EL;

namespace FAT
{
    public static class ConfHelper
    {
        // 节选自sdk内部提供的工具类 增加了重置ResMap的方法
        private static UpdateResMap s_updateResMap;

        public static void RefreshResMap()
        {
            s_updateResMap = UpdateResMap.ReadFromFile();
#if UNITY_EDITOR
            _DebugPrintRes(s_updateResMap);
#endif
        }

        public static bool CheckUpdateAndRefreshResMap()
        {
            var updateMap = UpdateResMap.ReadFromFile().ResMap;
            var map = s_updateResMap.ResMap;
            var diff = updateMap.Where(x => !map.ContainsKey(x.Key) || map[x.Key].FileName != x.Value.FileName).Union(
                map.Where(x => !updateMap.ContainsKey(x.Key) || updateMap[x.Key].FileName != x.Value.FileName));
            RefreshResMap();
            return diff.Any();
            // foreach (var res in map)
            // {
            //     if (res.Key.EndsWith(".bytes"))
            //         continue;
            //
            //     if (!s_updateResMap.ResMap.TryGetValue(res.Key, out var value) ||
            //         res.Value.FileName != value.FileName)
            //     {
            //         update = true;
            //         DebugEx.FormatInfo("Find Diff FileName = {0}", res.Key);
            //     }
            // }
            //RefreshResMap();
        }

        private static void _EnsureResMap()
        {
            if (s_updateResMap == null)
            {
                s_updateResMap = UpdateResMap.ReadFromFile();
#if UNITY_EDITOR
                _DebugPrintRes(s_updateResMap);
#endif
            }
        }

        public static string GetUpdatePath(string tablePath, out bool fileReadable, bool lowerName = false,
            string ext = ".bytes")
        {
            string relativePath = tablePath + ext;
            fileReadable = false;
            _EnsureResMap();
            string updatePath = s_updateResMap.GetResLocalFileName(relativePath);
            if (!string.IsNullOrEmpty(updatePath))
            {
                string externalPath = AssetsFileSystem.GetWritePath(updatePath);
                if (!string.IsNullOrEmpty(externalPath))
                {
                    fileReadable = true;
                    return externalPath;
                }
            }

            return string.Empty;
        }

        public static string GetBuiltinPath(string tablePath, out bool fileReadable, bool isDataConf = false,
            string ext = ".bytes")
        {
            string relativePath = tablePath + ext;
            fileReadable = false;
            var config = AppUpdaterConfigManager.AppUpdaterConfig;
            string localDataRoot = isDataConf ? config.localDataRoot : string.Empty;
            if (!string.IsNullOrEmpty(localDataRoot))
                relativePath = $"{localDataRoot}/{relativePath}";
            string builtinPath = AssetsFileSystem.GetStreamingAssetsPath(relativePath, null, !isDataConf);
            return builtinPath;
        }

        /// <summary>
        /// 获取配置加载路径
        /// </summary>
        /// <param name="tablePath">配置表文件在res_data.json的filelist中的key</param>
        /// <param name="fileReadable">配置加载路径,是否可通过File.Read读取</param>
        /// <param name="lowerName">配置文件是否采用全小写</param>
        /// <param name="ext">配置表文件扩展名</param>
        /// <returns></returns>
        public static string GetTableLoadPath(string tablePath, out bool fileReadable, bool lowerName = false,
            string ext = ".bytes")
        {
            string relativePath = tablePath + ext;
            fileReadable = false;
            _EnsureResMap();
            string updatePath = s_updateResMap.GetResLocalFileName(relativePath);
            if (!string.IsNullOrEmpty(updatePath))
            {
                string externalPath = AssetsFileSystem.GetWritePath(updatePath);
                if (!string.IsNullOrEmpty(externalPath))
                {
                    fileReadable = true;
                    return externalPath;
                }
            }

            var config = AppUpdaterConfigManager.AppUpdaterConfig;
            string localDataRoot = config.localDataRoot;
            if (!string.IsNullOrEmpty(localDataRoot))
                relativePath = $"{localDataRoot}/{relativePath}";
            string builtinPath = AssetsFileSystem.GetStreamingAssetsPath(relativePath, null, false);
            return builtinPath;
        }

        private static System.Text.StringBuilder sb = new();

        private static void _DebugPrintRes(UpdateResMap map)
        {
            sb.Clear();
            sb.AppendLine("=======================");
            sb.AppendLine("debug print resmap");
            foreach (var kv in map.ResMap)
            {
                sb.Append($"{kv.Key}^{kv.Value.ResName}^{kv.Value.FileName}");
            }

            sb.AppendLine("=======================");
            EL.DebugEx.Info(sb.ToString());
        }

        private static string prefix = "file://";

        public static string FixFilePath(string name)
        {
            if (!string.IsNullOrEmpty(prefix) && name.Contains(prefix))
                return name;
            return $"{prefix}{name}";
        }

        public static async void AsyncRegisterConf(string filePath, Action<CodedInputStream> cb)
        {
            try
            {
                var fp = FixFilePath(filePath);
                EL.DebugEx.Info($"ConfHelper::AsyncRegisterConf origin {filePath} after {fp}");
                using (var req = UnityEngine.Networking.UnityWebRequest.Get(FixFilePath(filePath)))
                {
                    await req.SendWebRequest();
                    if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        var bytes = ProtoFileUtility.Decrypt(req.downloadHandler.data);
                        cb?.Invoke(new CodedInputStream(bytes));
                    }
                    else
                    {
                        EL.DebugEx.Error($"ConfHelper::AsyncRegisterConf failed {req.result} {req.error}");
                        cb?.Invoke(default);
                    }
                }
            }
            catch (Exception ex)
            {
                EL.DebugEx.Error($"ConfHelper::AsyncRegisterConf exception {ex.Message} | {filePath}");
                cb?.Invoke(default);
            }
        }

        public static async void AsyncLoadConf<T>(string filePath, Action<T> cb) where T : IMessage, new()
        {
            try
            {
                using (var req = UnityEngine.Networking.UnityWebRequest.Get(FixFilePath(filePath)))
                {
                    await req.SendWebRequest();
                    if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        var data = ProtoFileUtility.Decrypt(req.downloadHandler.data);
                        var conf = new T();
                        conf.MergeFrom(new CodedInputStream(data));
                        cb?.Invoke(conf);
                    }
                    else
                    {
                        cb?.Invoke(default);
                    }
                }
            }
            catch (Exception ex)
            {
                EL.DebugEx.Error($"ConfHelper::AsyncLoadConf failed {ex.Message} | {filePath}");
                cb?.Invoke(default);
            }
        }

        public const string PlayerPrefs_LangKey = "lang";

        // zh_hans_cn -> ZhHansCn
        public static string ConvertLangStrToConfName(string lang)
        {
            if (string.IsNullOrEmpty(lang))
                return default;
            var sb = new System.Text.StringBuilder();
            var strs = lang.Split('_');
            foreach (var s in strs)
            {
                // 首字母大写
                sb.Append(s.Substring(0, 1).ToUpper());
                sb.Append(s.Substring(1));
            }

            return sb.ToString();
        }
    }
}