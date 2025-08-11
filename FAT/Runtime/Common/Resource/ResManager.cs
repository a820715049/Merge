using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using CenturyGame.AppUpdaterLib.Runtime;

namespace EL.Resource
{
    public class ResManagerRunner : MonoBehaviour
    {

    }
    public static partial class ResManager
    {
        public static readonly string kGroupResources = "kGroupResources";
        public static WebFileManifest webManifest => mWebManifest;
        public static event System.Action<string> onLoadGroup;
        public static event System.Action<string, string, System.Type, Object> onLoadAsset;          //param: group, assetName, assetType, assetItSelf
        public static bool isLoading {
            get {
                foreach(var t in _resCache.Values)
                {
                    foreach(var res in t.Values)
                    {
                        if(res.keepWaiting)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        private static Dictionary<string, ResourceAsyncTask> _groupCache = new Dictionary<string, ResourceAsyncTask>();
        private static Dictionary<string, Dictionary<string, ResourceAsyncTask>> _resCache = new Dictionary<string, Dictionary<string, ResourceAsyncTask>>();
        private static IAssetLoader _assetLoader = 
#if UNITY_EDITOR
#   if UNITY_STANDALONE
            new EditorAssetLoader();
#   else
            new EditorAssetLoader();
#   endif
#else
            new AssetBundleAssetLoader();
#endif
        private static WebFileManifest mWebManifest = new WebFileManifest();

        private static bool isInitialized = false;
        public static void Initialize()
        {
            if (isInitialized)
                return;
            isInitialized = true;

            // provider 1 -> 包外路径
            var abRoot = AssetsFileSystem.GetWritePath("fake");
            if (abRoot != "fake")
            {
                abRoot = abRoot.Substring(0, abRoot.Length - 5);      //remote the end /fake to achieve directory name
                AssetBundleManager.Instance.AddProvider(new FileAssetBundleProvider(abRoot, abRoot, false), false);      //the one in app write path
            }

            // provider 2 -> 包内路径
            abRoot = _GetStreamingAssetsPath("fake");
            abRoot = abRoot.Substring(0, abRoot.Length - 5);      //remote the end /fake to achieve directory name
            var fileRoot = _GetStreamingAssetsPath("fake", null, false);
            fileRoot = fileRoot.Substring(0, fileRoot.Length - 5);      //remote the end /fake to achieve directory name
            AssetBundleManager.Instance.AddProvider(new FileAssetBundleProvider(abRoot, fileRoot, true), true);          //the one in package

            // provider 3 -> 包外bundle
            AssetBundleManager.Instance.AddProvider(new FileAssetBundleProviderExternal(), false);          //the one in package
        }

        // public static IEnumerator CoLoadManifests(EL.Resource.ResourceManifest importantFiles)
        // {
        //     if(importantFiles != null)
        //     {
        //         for(int i = 0; i < mFileLoaders.Count; i++)
        //         {
        //             yield return mFileLoaders[i].CoCheckImportantFiles(importantFiles);
        //         }
        //         DebugEx.FormatInfo("ResManager::CoLoadManifests ----> check important file {0}", importantFiles);
        //     }
        // }
        
        private static System.Text.StringBuilder mTempSb = new System.Text.StringBuilder();
        private static string _GetStreamingAssetsPath(string path, string ext = null, bool loadAB = true)
        {
            return AssetsFileSystem.GetStreamingAssetsPath(path, ext, loadAB);
        }
        public static bool HasGroup(string groupName)
        {
            return _assetLoader.HasGroup(groupName);
        }
        public static bool IsGroupLoaded(string groupName)
        {
            return _assetLoader.IsGroupLoaded(groupName);
        }
        public static void TriggerGroupLoad(string groupName)
        {
            onLoadGroup?.Invoke(groupName);
        }
        public static void TriggerAssetLoad(string groupName, string assetName, System.Type type, Object asset)
        {
            onLoadAsset?.Invoke(groupName, assetName, type, asset);
        }
        public static bool IsSupportAppUpdater()
        {
            #if UNITY_WEBGL
            return true;
            #else
            return true;//_assetLoader is AssetBundleAssetLoader;
            #endif
        }
        public static void GetAllGroup(List<string> container)
        {
            _assetLoader.GetAllGroup(container);
        }
        public static bool GetAllFilesInGroup(string group, List<string> container)
        {
            return _assetLoader.GetAllFilesInGroup(group, container);
        }
        public static ResourceAsyncTask LoadGroup(string groupName)
        {
            ResourceAsyncTask task = null;
            if(_groupCache.TryGetValue(groupName, out task) && !task.keepWaiting && !task.isSuccess)
            {
                _groupCache.Remove(groupName);
                task = null;
            }
            if(task == null) 
            {
                if(groupName == kGroupResources)
                {
                    task = new ResourceAsyncTask();
                    task.Success(null);
                }
                else
                {
                    task = _assetLoader.LoadGroup(groupName);
                }
                _groupCache[groupName] = task;
            }
            return task;
        }

        public static void Clear()
        {
            foreach(var groupTask in _groupCache.Values)
            {
                groupTask.Cancel();
            }
            _groupCache.Clear();
            foreach(var resTasks in _resCache)
            {
                bool isResourceDir = resTasks.Key == kGroupResources;
                if(isResourceDir)
                {
                    continue;               //not unload asset by Resources.Load, because if prefab is already instantiated, Resources.Unload will get error. and Resources.Load never load duplicate item, so we are safe
                }
                foreach(var resTask in resTasks.Value.Values)
                {
                    resTask.Cancel();
                    if(resTask.asset != null)
                    {
                        _assetLoader.DestroyAsset(resTask.asset, resTasks.Key);      //clear the asset
                    }
                }
            }
            _assetLoader.Clear();
            _resCache.Clear();
        }

        public static void UnloadAsset(string groupName, string assetName)
        {
            ResourceAsyncTask task = null;
            Dictionary<string, ResourceAsyncTask> cache = null;
            if(assetName == null)
            {
                assetName = "";
            }
            if(groupName == null)
            {
                groupName = "";
            }
            if(_resCache.TryGetValue(groupName, out cache) && cache.TryGetValue(assetName, out task))
            {
                if(task.keepWaiting)
                {
                    task.Cancel();
                }
                else
                {
                    if(task.isSuccess && task.asset != null)
                    {
                        _assetLoader.DestroyAsset(task.asset, groupName);
                    }
                }
                cache.Remove(assetName);
            }
        }

        public static ResourceAsyncTask LoadAsset(string groupName, string assetName)
        {
            return LoadAsset<UnityEngine.Object>(groupName, assetName);
        }

        public static ResourceAsyncTask TryLoadAssetSync<T>(string groupName, string assetName) where T:UnityEngine.Object
        {
            var task = LoadAsset<T>(groupName, assetName);
            if(_assetLoader.TryFinishSync(task) || task.isSuccess)
            {
                return task;
            }
            else
            {
                DebugEx.FormatWarning("ResManager::TryLoadAssetSync cannot finish sync {0}@{1}", assetName, groupName);
                return null;
            }
        }

        public static ResourceAsyncTask LoadAsset<T>(string groupName, string assetName) where T:UnityEngine.Object
        {
            return LoadAssetByType(groupName, assetName, typeof(T));
        }

        public static ResourceAsyncTask LoadAssetByType(string groupName, string assetName, System.Type type)
        {
            ResourceAsyncTask task = null;
            Dictionary<string, ResourceAsyncTask> cache = null;
            if(assetName == null)
            {
                assetName = "";
            }
            if(groupName == null)
            {
                groupName = "";
            }
            if(_resCache.TryGetValue(groupName, out cache) && cache.TryGetValue(assetName, out task))
            {
                if(!task.keepWaiting && !task.isSuccess)
                {
                    cache.Remove(assetName);
                    task = null;
                }
            }
            if(task == null)
            {
                if(cache == null) 
                {
                    _resCache[groupName] = cache = new Dictionary<string, ResourceAsyncTask>();
                }
                if(groupName == kGroupResources)
                {
                    task = new ResourceAsyncTask();
                    var asset = Resources.Load(assetName, type);
                    if(asset == null)
                    {
                        task.Fail("not exists in Resources folder");
                    }
                    else
                    {
                        task.Success(asset);
                        TriggerAssetLoad(groupName, assetName, type, asset);
                    }
                }
                else
                {
                    task = _assetLoader.LoadAsset(groupName, assetName, type);
                }
                cache[assetName] = task;
            }
            return task;
        }

        public static void ReleaseGroup(string groupName)
        {
            if(groupName == kGroupResources)
            {
                return;
            }
            _assetLoader.ReleaseGroup(groupName);
            ResourceAsyncTask task = null;
            if(_groupCache.TryGetValue(groupName, out task))
            {
                task.Cancel();
                _groupCache.Remove(groupName);
            }
            Dictionary<string, ResourceAsyncTask> cache = null;
            if(_resCache.TryGetValue(groupName, out cache))
            {
                var iter = cache.GetEnumerator();
                while(iter.MoveNext())
                {
                    iter.Current.Value.Cancel();
                }
                _resCache.Remove(groupName);
            }
        }
    }
}
