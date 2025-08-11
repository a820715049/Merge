/*
 * @Author: qun.chao
 * @Date: 2024-01-17 18:44:39
 */
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using CenturyGame.AssetBundleManager.Runtime;
using CenturyGame.AppUpdaterLib.Runtime;
using CenturyGame.AppUpdaterLib.Runtime.Managers;
using System.IO;
using Cysharp.Threading.Tasks;

namespace EL.Resource
{
    public class FileAssetBundleProviderExternal : IAssetBundleProvider
    {
        public const string external_res_path_root = "ext";
        private string mIdentifer;
        private UpdateResMap mResMap;
        private string _resRootPath;
        private string mResRootPath 
        {
            get
            {
                if (string.IsNullOrEmpty(_resRootPath))
                    _resRootPath = AssetsFileSystem.GetWritePath(external_res_path_root);
                return _resRootPath;
            }
        }

        private void _EnsureResFolder()
        {
            if (!Directory.Exists(mResRootPath))
            {
                Directory.CreateDirectory(mResRootPath);
            }
        }

        private string platform_str =
#if UNITY_ANDROID
            "android"
#elif UNITY_IOS
            "ios"
#else
            "dummy"
#endif
        ;

        public string GetIdentifer()
        {
            if (string.IsNullOrEmpty(mIdentifer))
            {
                mIdentifer = $"file@{external_res_path_root}";
            }
            return mIdentifer;
        }

        public bool HasAssetBundle(string name)
        {
            if (mResMap == null)
                return false;
            return mResMap.ResMap.ContainsKey(name);
        }

        private bool _TryLoadResMap(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                DebugEx.Warning($"FileAssetBundleProviderExternal::_TryEnsureResMap ----> no external res");
                return false;
            }
            var ret = false;
            try
            {
                mResMap = UpdateResMapParser.Parse(text);
                ret = true;
            }
            catch
            {
                DebugEx.Error($"FileAssetBundleProviderExternal::_TryEnsureResMap ----> invalid data {text}");
            }
            return ret;
        }

        private string _GetResItemWritePath(UpdateResItem item, out string hash)
        {
            hash = item.FileName.Split("#")[2];
            return $"{mResRootPath}/{_GetFileNameWithHash(item.ResName, hash)}";
        }

        private string _GetFileNameWithHash(string origName, string hash)
        {
            var names = origName.Split('.');
            return $"{names[0]}_{hash}.{names[1]}";
        }

        // 读取本地文件内容
        private async UniTask<string> _GetTextByURL(string url)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                try
                {
                    await req.SendWebRequest();
                    return req.downloadHandler?.text;
                }
                catch (UnityWebRequestException ex)
                {
                    DebugEx.Warning($"FileAssetBundleProviderExternal::_GetTextByUrl ----> {url} | {ex.Message}");
                }
            }
            return null;
        }

        // 下载资源
        private async UniTask<string> _TryEnsureRes(UpdateResItem resInfo)
        {
            var filePath = _GetResItemWritePath(resInfo, out var hash);
            var md5 = CommonUtility.GetMD5HashFromFile(filePath);
            if (md5 != hash)
            {
                if (File.Exists(filePath))
                {
                    // 重新下载
                    DebugEx.Error($"FileAssetBundleProviderExternal::_TryEnsureRes ----> res invalid, delete {filePath}");
                    File.Delete(filePath);
                }

                _EnsureResFolder();

                var config = AppUpdaterConfigManager.AppUpdaterConfig;
                var url = $"{config.cdnUrl}/{resInfo.FileName}".Replace("#", "%23");
                DebugEx.Info($"FileAssetBundleProviderExternal::_TryEnsureRes ----> prepare to download {url}");

                using (var req = UnityWebRequest.Get(url))
                {
                    try
                    {
                        await req.SendWebRequest();
                        if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                        {
                            DebugEx.Error($"FileAssetBundleProviderExternal::_TryEnsureRes ----> download failed {url} {req.result}");
                            return string.Empty;
                        }
                        else
                        {
                            if (req.responseCode != 200 && req.responseCode != 304)
                            {
                                DebugEx.Error($"FileAssetBundleProviderExternal::_TryEnsureRes ----> unexpected response {url} {req.result} {req.responseCode}");
                                return string.Empty;
                            }
                            File.WriteAllBytes(filePath, req.downloadHandler.data);
                        }
                    }
                    catch (UnityWebRequestException ex)
                    {
                        DebugEx.Warning($"FileAssetBundleProviderExternal::_TryEnsureRes ----> {url} | {ex.Message}");
                        return string.Empty;
                    }
                }
            }
            return filePath;
        }

        // 1. res_{platform}_external.x 中获取所有外部资源精确名称 作为内部manifest信息
        // 2. 确保ab依赖文件存在(external_file_list.x), 将文件内容作为外部ABManifest返回(作为AB的依赖信息依据)
        // 3. 根据内部manifest里是否有ab作为HasAssetBundle的依据
        // 4. LoadAssetBundle时, 先check精确文件是否存在，否则尝试下载，最后正常加载资源
        private async UniTask<ABManifest> _LoadManifest()
        {
            // 构建资源清单路径
            var res_plat_json_name = $"res_{platform_str}_external.x";
            string url;
            url = FAT.ConfHelper.GetUpdatePath(res_plat_json_name, out _, false, string.Empty);
            if (string.IsNullOrEmpty(url))
            {
                url = FAT.ConfHelper.GetBuiltinPath(res_plat_json_name, out _, false, string.Empty);
            }
            url = FAT.ConfHelper.FixFilePath(url);

            // 加载资源清单
            DebugEx.Info($"FileAssetBundleProviderExternal ----> load {res_plat_json_name} from {url}");
            var json = await _GetTextByURL(url);
            if (!_TryLoadResMap(json))
            {
                return null;
            }

            // 获取AB依赖文件
            if (!mResMap.ResMap.TryGetValue(Constant.kExternalResList, out var resItem))
            {
                DebugEx.Error($"FileAssetBundleProviderExternal ----> load {res_plat_json_name} from {url} invalid");
                return null;
            }

            var tryCount = 0;
            string res_path = null;
            while (string.IsNullOrEmpty(res_path))
            {
                if (tryCount >= 3)
                {
                    DebugEx.Error($"FileAssetBundleProviderExternal ----> load {res_plat_json_name} from {url} try {tryCount} failed");
                    return null;
                }
                await UniTask.Delay(System.TimeSpan.FromSeconds(1 * tryCount * 2), ignoreTimeScale: true);
                ++tryCount;
                res_path = await _TryEnsureRes(resItem);
            }

            // 通过WebRequest加载 增加前缀file://
            var textManifest = await _GetTextByURL(FAT.ConfHelper.FixFilePath(res_path));
            var resManifest = JsonUtility.FromJson<ResManifest>(textManifest);
            if (resManifest == null)
            {
                return null;
            }
            var mani = new ABManifest();
            mani.ResetWithManifest(resManifest);
            return mani;
        }

        public IEnumerator ATLoadManifest()
        {
            var ret = new SimpleResultedAsyncTask<ABManifest>();
            yield return ret;

            var task_load_manifest = _LoadManifest();
            yield return new WaitUntil(() => task_load_manifest.Status.IsCompleted());
            var mani = task_load_manifest.GetAwaiter().GetResult();
            if (mani == null)
            {
                DebugEx.Warning($"FileAssetBundleProviderExternal::ATLoadManifest ----> load manifest failed");
                ret.Fail("failed");
            }
            else
            {
                ret.Success(mani);
            }
        }

        public async UniTask<string> TryPrepareRes(string name)
        {
            if (!mResMap.ResMap.TryGetValue(name, out var resItem))
            {
                return null;
            }
            var res = await _TryEnsureRes(resItem);
            return res;
        }

        public IEnumerator CoLoadAssetBundle(string name, ResourceAsyncTask task)
        {
            if (!mResMap.ResMap.TryGetValue(name, out var resItem))
            {
                task.Fail($"res missing {name}");
                yield break;
            }

            var task_ensure_res = _TryEnsureRes(resItem);
            yield return new WaitUntil(() => task_ensure_res.Status.IsCompleted());
            var url = task_ensure_res.GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(url))
            {
                task.Fail($"res not ready {name}");
                yield break;
            }

            DebugEx.Trace($"FileAssetBundleProvider ----> load bundle from {url}");

            var www = AssetBundle.LoadFromFileAsync(url);
            yield return www;

            if (www == null || www.assetBundle == null)
            {
                if (AssetBundleHelper.IsDuplicate(name, out var ab))
                {
                    try
                    {
                        task.Success(ab);
                    }
                    catch (System.Exception ex)
                    {
                        DebugEx.FormatWarning("FileAssetBundleProviderEx ----> load bundle dup exception {0}:{1}:{2}", url, ex.Message, ex.StackTrace);
                        task.Fail(ex.ToString());
                    }
                }
                else
                {
                    DebugEx.FormatWarning("FileAssetBundleProviderEx ----> load bundle failed {0}", url);
                    task.Fail("load fail");
                }
            }
            else
            {
                try
                {
                    AssetBundle bundle = www.assetBundle;
                    task.Success(bundle);
                }
                catch (System.Exception ex)
                {
                    DebugEx.FormatWarning("FileAssetBundleProvider ----> load bundle exception {0}:{1}:{2}", url, ex.Message, ex.StackTrace);
                    task.Fail(ex.ToString());
                }
            }
        }
    }
}