/*
 * @Author: qun.chao
 * @Date: 2025-06-25 10:52:44
 */
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EL;
using EL.Resource;

namespace FAT
{
    public class MBGlobalLoadingBg : MonoBehaviour
    {
        [Serializable]
        class LoadingImageRequest
        {
            public long endTimestamp;
            public string bundleUrl;
            public string name;
        }

        [Serializable]
        class RequestList
        {
            public List<LoadingImageRequest> requests;
        }

        [SerializeField] private RawImage defaultLoadingImage;
        [SerializeField] private List<GameObject> logoList;


        public bool Show { set => gameObject.SetActive(value); }

        private void OnEnable()
        {
            I18N.onLanguageChange -= _RefreshLanguageLogo;
            I18N.onLanguageChange += _RefreshLanguageLogo;
        }

        private void _RefreshLanguageLogo()
        {
            if (logoList == null || logoList.Count < 1)
                return;
            var curLanguage = I18N.GetLanguage();
            var showIndex = curLanguage switch
            {
                "en" => 0,
                "ja" => 1,
                "ko" => 2,
                _ => 0
            };
            for (var i = 0; i < logoList.Count; i++)
            {
                logoList[i].SetActive(i == showIndex);
            }
        }

        #region 动态loading图

        private static string logTag => nameof(MBGlobalLoadingBg);
        private static readonly string keyForLoadingRequest = "key_loading_request";
        private static readonly string groupForLoadingImage = "event_loading";
        private static AssetBundleCreateRequest abRequest = null;
        private static string currentLoadingImage = string.Empty;
        private static bool HasSavedRequest => PlayerPrefs.HasKey(keyForLoadingRequest);

        private List<(string name, long endTimestamp, UniTask<string> task)> bundleTaskList = new();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void PreSplashScreen()
        {
            Debug.Log($"[loadingbg] PreSplashScreen start");
#if UNITY_EDITOR
            return;
#endif
            if (!HasSavedRequest)
                return;
            try
            {
                var data = PlayerPrefs.GetString(keyForLoadingRequest, string.Empty);
                if (string.IsNullOrEmpty(data))
                    return;
                LoadingImageRequest req = null;
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var requestList = JsonUtility.FromJson<RequestList>(data);
                foreach (var request in requestList.requests)
                {
                    if (request.endTimestamp >= now)
                    {
                        req = request;
                        break;
                    }
                }
                if (req == null)
                    return;
                // 加载资源
                abRequest = AssetBundle.LoadFromFileAsync(req.bundleUrl);
                currentLoadingImage = req.name;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{logTag}] TryLoadDynamicLoadingImage error {e.Message}");
            }
            Debug.Log($"[loadingbg] PreSplashScreen done");
        }


        public void TryPrepareLoadingImage()
        {
            if (!Game.Instance.isRunning) return;
            TryPrepareLoadingImageImp().Forget();
        }

        private async UniTask TryPrepareLoadingImageImp()
        {
            bundleTaskList.Clear();
            var reqs = Game.Manager.configMan.globalConfig.LoadingImage;
            var now = Game.Instance.GetTimestampSeconds();
            foreach (var req in reqs)
            {
                var firstHash = req.IndexOf('#');
                if (firstHash == -1) continue;
                var secondHash = req.IndexOf('#', firstHash + 1);
                if (secondHash == -1) continue;
                var bundleName = req[..firstHash];
                var fileName = req[(firstHash + 1)..secondHash];
                var endTimeStr = req[(secondHash + 1)..];

                if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(endTimeStr))
                    continue;
                if (!long.TryParse(endTimeStr, out var endTime))
                    continue;
                if (endTime <= now)
                    continue;

#if UNITY_EDITOR
                bundleTaskList.Add((fileName, endTime, UniTask.FromResult(bundleName)));
#else
                var provider = AssetBundleManager.Instance.GetProviderByGroup(bundleName);
                if (provider != null && provider is FileAssetBundleProviderExternal ext)
                {
                    var task = ext.TryPrepareRes($"{bundleName}.ab");
                    bundleTaskList.Add((fileName, endTime, task));
                }
#endif
            }

            if (bundleTaskList.Count > 0)
            {
                var tasks = bundleTaskList.Select(t => t.task);
                await UniTask.WhenAll(tasks);
            }

            if (bundleTaskList.Count > 0)
            {
                var data = new RequestList
                {
                    requests = new List<LoadingImageRequest>()
                };
                foreach (var (name, endTimestamp, task) in bundleTaskList)
                {
                    data.requests.Add(new LoadingImageRequest
                    {
                        endTimestamp = endTimestamp,
                        name = name,
                        bundleUrl = task.GetAwaiter().GetResult()
                    });
                }
                PlayerPrefs.SetString(keyForLoadingRequest, JsonUtility.ToJson(data));
                DebugEx.Info($"[{logTag}] setup loading image request");
            }
            else
            {
                if (HasSavedRequest)
                {
                    PlayerPrefs.DeleteKey(keyForLoadingRequest);
                    DebugEx.Info($"[{logTag}] cleanup loading image request");
                }
            }
        }

        /// <summary>
        /// 尝试加载动态loading图
        /// 1. 从本地存档获取列表需求
        /// 2. 列表在 则认为资源已就位
        /// 3. 加载资源需要有超时逻辑, 避免用户手动删除过资源的情况下, 触发长时间下载
        /// </summary>
        public async UniTask TryLoadDynamicLoadingImage()
        {
            if (!HasSavedRequest)
                return;
            try
            {
                var cts = new CancellationTokenSource();

#if UNITY_EDITOR
                var data = PlayerPrefs.GetString(keyForLoadingRequest, string.Empty);
                if (string.IsNullOrEmpty(data))
                    return;
                LoadingImageRequest req = null;
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var requestList = JsonUtility.FromJson<RequestList>(data);
                foreach (var request in requestList.requests)
                {
                    if (request.endTimestamp >= now)
                    {
                        req = request;
                        break;
                    }
                }
                if (req == null)
                    return;
                await LoadTexture_Editor(req.bundleUrl, req.name, cts)
                    .Timeout(TimeSpan.FromSeconds(1), taskCancellationTokenSource: cts);
                return;
#endif

                if (abRequest != null && !string.IsNullOrEmpty(currentLoadingImage))
                {
                    if (!abRequest.isDone)
                    {
                        await UniTask.WaitUntil(() => abRequest.isDone, cancellationToken: cts.Token).
                            Timeout(TimeSpan.FromSeconds(1), taskCancellationTokenSource: cts);
                    }
                    var ab = abRequest.assetBundle;
                    if (ab != null)
                    {
                        var texture = ab.LoadAsset<Texture2D>(currentLoadingImage);
                        if (texture != null)
                        {
                            defaultLoadingImage.texture = texture;
                            DebugEx.Info($"[{logTag}] LoadTexture_External success {currentLoadingImage}");
                        }
                        ab.Unload(false);
                    }
                }
            }
            catch (Exception e)
            {
                DebugEx.Error($"[{logTag}] TryLoadDynamicLoadingImage error {e.Message}");
            }
        }

        private async UniTask LoadTexture_Editor(string group, string name, CancellationTokenSource cts)
        {
            var task = ResManager.LoadAsset<Texture2D>(group, name);
            if (task.keepWaiting)
                await UniTask.WaitWhile(() => task.keepWaiting && !task.isCanceling, cancellationToken: cts.Token);
            if (task.isSuccess)
            {
                var texture = task.asset as Texture2D;
                if (texture != null)
                {
                    defaultLoadingImage.texture = texture;
                    DebugEx.Info($"[{logTag}] LoadTexture_Editor success {name}");
                }
            }
        }

        // private async UniTask LoadTexture_External(string url, string name, CancellationTokenSource cts)
        // {
        //     var ab = await AssetBundle.LoadFromFileAsync(url).WithCancellation(cts.Token);
        //     if (ab != null)
        //     {
        //         var texture = ab.LoadAsset<Texture2D>(name);
        //         if (texture != null)
        //         {
        //             defaultLoadingImage.texture = texture;
        //             DebugEx.Info($"[{logTag}] LoadTexture_External success {name}");
        //         }
        //         // 使用false参数，只卸载AssetBundle但保留已加载的资源
        //         ab.Unload(false);
        //     }
        // }

        #endregion
    }
}