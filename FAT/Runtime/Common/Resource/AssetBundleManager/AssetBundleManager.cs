/**
 * @Author: handong.liu
 * @Date: 2020-07-09 19:16:23
 */
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EL.Resource
{
    class AssetBundleProviderEntry
    {
        public bool isAppPackage;
        public IAssetBundleProvider provider;
        public AsyncTaskBase manifestTask;
        public Dictionary<string, Hash128> assetBundleHash = new Dictionary<string, Hash128>();
        public ABManifest cachedManifest;
    }
    public class AssetBundleManager : MonoSingleton<AssetBundleManager>
    {
        public Dictionary<string, ResourceAsyncTask> mBundleLoadTask = new Dictionary<string, ResourceAsyncTask>();
        private List<AssetBundleProviderEntry> mProviders = new List<AssetBundleProviderEntry>();
        public void AddProvider(IAssetBundleProvider provider, bool isAppPackage)
        {
            AssetBundleProviderEntry entry = new AssetBundleProviderEntry();
            mProviders.Add(entry);
            entry.isAppPackage = isAppPackage;
            entry.provider = provider;
        }

        public void Clear()
        {
            foreach(var entry in mProviders)
            {
                entry.assetBundleHash.Clear();
                entry.cachedManifest = null;
                if(entry.manifestTask != null)
                {
                    entry.manifestTask.Cancel();
                }
                entry.manifestTask = null;
            }
            foreach(var task in mBundleLoadTask.Values)
            {
                task.Cancel();
                task.ResolveCancel();
                AssetBundle ab = task.asset as AssetBundle;
                if(ab != null)
                {
                    ab.Unload(true);
                    UnityEngine.Object.Destroy(ab);
                }
            }
            mBundleLoadTask.Clear();
        }

        public IEnumerator CoPrepare()
        {
            for(int i = 0; i < mProviders.Count; i++)
            {
                yield return StartCoroutine(_CoLoadAssetbundleManifest(mProviders[i]));
            }
        }
        
        private HashSet<string> mCachedVisitedBundle = new HashSet<string>();
        public ResourceAsyncTask LoadAssetBundle(string groupName, List<ResourceAsyncTask> tasksToWait)
        {
            mCachedVisitedBundle.Clear();
            return _LoadAssetIncludeDependency(_ToBundleName(groupName), tasksToWait);
        }

        public bool HasGroup(string group)
        {
            string bundleName = _ToBundleName(group);
            foreach(var providerEntry in mProviders)
            {
                if(providerEntry.assetBundleHash.ContainsKey(bundleName))
                {
                    return true;
                }
            }
            return false;
        }

        public void GetAllGroup(List<string> container)
        {
            foreach(var providerEntry in mProviders)
            {
                foreach(var hash in providerEntry.assetBundleHash)
                {
                    var groupName = _ToGroupName(hash.Key);
                    if(!container.Contains(groupName))
                    {
                        container.Add(groupName);
                    }
                }
            }
        }

        public IAssetBundleProvider GetProviderByGroup(string groupName)
        {
            var bundleName = _ToBundleName(groupName);
            foreach (var providerEntry in mProviders)
            {
                if (providerEntry.assetBundleHash.ContainsKey(bundleName))
                {
                    return providerEntry.provider;
                }
            }
            return null;
        }

        private ResourceAsyncTask _LoadAssetIncludeDependency(string bundleName, List<ResourceAsyncTask> tasksToWait)
        {
            var task = _LoadOneAssetBundle(bundleName);
            if(!task.isSuccess)
            {
                tasksToWait.Add(task);
            }
            string[] deps = _GetBundleDependencies(bundleName);
            if(deps != null)
            {
                foreach(var dep in deps)
                {
                    if(!mCachedVisitedBundle.Contains(dep))
                    {
                        mCachedVisitedBundle.Add(dep);
                        _LoadAssetIncludeDependency(dep, tasksToWait);
                    }
                }
            }
            return task;
        }
        
        private ResourceAsyncTask _LoadOneAssetBundle(string bundleName)
        {
            ResourceAsyncTask ret = null;
            if(mBundleLoadTask.TryGetValue(bundleName, out ret))
            {
                return ret;
            }
            else
            {
                ret = new ResourceAsyncTask();
                mBundleLoadTask.Add(bundleName, ret);
                StartCoroutine(_CoLoadOneAssetBundle(bundleName, ret));
                return ret;
            }
        }

        private IEnumerator _CoLoadOneAssetBundle(string bundleName, ResourceAsyncTask task)
        {
            DebugEx.FormatTrace("AssetBundleManager::_CoLoadOneAssetBundle {0}", bundleName);
            for(var i = 0; i < mProviders.Count; i++)
            {
                var entry = mProviders[i];
                if(_HasABInProvider(entry, bundleName))
                {
                    var resIter = _CoLoadOneAssetBundleInProvider(bundleName, entry.provider, task);
                    if(resIter != null)
                    {
                        yield return resIter;
                        if(task.asset != null)
                        {
                            break;
                        }
                    }
                }
            }
            if(task.keepWaiting || !task.isSuccess)
            {
                DebugEx.FormatError("AssetBundleManager._CoLoadOneAssetBundle ----> fail to load:{0}:{1}", bundleName, task.error);
                task.Fail("load fail");
                mBundleLoadTask.Remove(bundleName);
            }
        }

        private bool _HasABInProvider(AssetBundleProviderEntry provider, string abName)
        {
            return provider.isAppPackage && provider.assetBundleHash.ContainsKey(abName) ||
                    (!provider.isAppPackage && provider.provider.HasAssetBundle(abName));
        }

        private IEnumerator _CoLoadOneAssetBundleInProvider(string bundleName, IAssetBundleProvider provider, ResourceAsyncTask task)
        {
            IEnumerator resIter = provider.CoLoadAssetBundle(bundleName, task);
            if(resIter != null)
            {
                yield return resIter;
                if(task.asset == null)
                {
                    DebugEx.FormatError("AssetBundleManager._CoLoadOneAssetBundle ----> task is not resolved in provider {0}:{1}", provider.GetIdentifer(), bundleName);
                }
            }
            else
            {
                DebugEx.FormatWarning("AssetBundleManager._CoLoadOneAssetBundle ----> res is not exists in provider {0}:{1}", provider.GetIdentifer(), bundleName);
            }
        }

        private string[] _GetBundleDependencies(string bundleName)
        {
            for(var i = 0; i < mProviders.Count; i++)
            {
                var entry = mProviders[i];
                if(entry.assetBundleHash.ContainsKey(bundleName))
                {
                    return entry.cachedManifest.GetDependency(bundleName);
                }
            }
            return null;
        }

        private IEnumerator _CoLoadAssetbundleManifest(AssetBundleProviderEntry entry)
        {
            if(entry.cachedManifest != null)
            {
                yield break;
            }
            if(entry.manifestTask != null)
            {
                yield return entry.manifestTask;
            }
            else
            {
                var task = new SimpleAsyncTask();
                entry.manifestTask = task;
                var iter = entry.provider.ATLoadManifest();
                iter.MoveNext();
                var manifestLoadTask = iter.Current as ResultedAsyncTask<ABManifest>;
                yield return iter;
                if(manifestLoadTask.isSuccess)
                {
                    //read all md5
                    var manifest = entry.cachedManifest = manifestLoadTask.result;// old version: (manifestLoadTask.asset as AssetBundle).LoadAsset("AssetBundleManifest") as AssetBundleManifest;
                    var assetBundles = manifest.GetAllAssetBundles();
                    foreach(var bundleName in assetBundles)
                    {
                        entry.assetBundleHash[bundleName] = default(Hash128);//EAGLEMARK: since we don't use this hash, a wrong value is used. old version: manifest.GetAssetBundleHash(bundleName);
                    }
                    /*
                    //delete assetBundle
                    (manifestLoadTask.asset as AssetBundle).Unload(false);
                    */
                    task.ResolveTaskSuccess();
                }
                else
                {
                    //error happen
                    DebugEx.FormatWarning("AssetBundleManager._CoLoadAssetbundleManifest ----> load manifest for entry {0} failed:{1}", entry.provider.GetIdentifer(), manifestLoadTask.error);
                    task.ResolveTaskFail();
                }
                entry.manifestTask = null;
            }
        }

        private string _ToGroupName(string bundleFileName)
        {
            return bundleFileName.Substring(0, bundleFileName.LastIndexOf('.'));
        }
        private string _ToBundleName(string groupName)
        {
            return groupName + ".ab";
        }
    }
}