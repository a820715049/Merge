/**
 * @Author: handong.liu
 * @Date: 2020-07-09 16:09:52
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EL.Resource
{
    public class AssetBundleAssetLoader : IAssetLoader
    {
        private Dictionary<string, ResourceAsyncTask> mAllBundles = new Dictionary<string, ResourceAsyncTask>();
        public bool HasGroup(string group)
        {
            return AssetBundleManager.Instance.HasGroup(group);
        }
        public void Clear()
        {
            foreach(var task in mAllBundles.Values)
            {
                task.Cancel();          //this task is not the direct load task, so don't need to release asset bundle
            }
            mAllBundles.Clear();
            AssetBundleManager.Instance.Clear();
        }
        public void GetAllGroup(List<string> container)
        {
            AssetBundleManager.Instance.GetAllGroup(container);
        }
        public void DestroyAsset(UnityEngine.Object asset, string group)
        {
            if(asset is GameObject)
            {
                // GameObject.Destroy(asset);
            }
            else
            {
                Resources.UnloadAsset(asset);
            }
            // Object.DestroyImmediate(asset, true);
        }
        public bool GetAllFilesInGroup(string group, List<string> container)
        {
            ResourceAsyncTask task = null;
            if(mAllBundles.TryGetValue(group, out task) && task.isSuccess)
            {
                AssetBundle ab = task.asset as AssetBundle;
                var names = ab.GetAllAssetNames();
                foreach(var name in names)
                {
                    int idx = name.LastIndexOf('/');
                    if(idx >= 0)
                    {
                        container.Add(name.Substring(idx + 1));
                    }
                    else
                    {
                        container.Add(name);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IsGroupLoaded(string group)
        {
            return mAllBundles.TryGetValue(group, out var task) && task.isSuccess;
        }
        public ResourceAsyncTask LoadGroup(string group)
        {
            ResourceAsyncTask ret = null;
            // 当task不存在 或者 task已经失败了 则创建新的task
            if (!mAllBundles.TryGetValue(group, out ret) || (!ret.keepWaiting && !ret.isSuccess))
            {
                DebugEx.FormatInfo("AssetBundleAssetLoader::LoadGroup ----> load group {0}", group);
                ret = AssetBundleManager.Instance.StartAsyncTask(_AtLoadBundle(group)) as ResourceAsyncTask;
                mAllBundles[group] = ret;
            }
            return ret;
        }
        public ResourceAsyncTask LoadAsset(string group, string asset, System.Type type)
        {
            var ret = AssetBundleManager.Instance.StartAsyncTask(_AtWaitAndLoadAsset(group, asset, type)) as ResourceAsyncTask;
            return ret;
        }
        public bool TryFinishSync(ResourceAsyncTask task)
        {
            if(task is ResourceAsyncTaskForAsset forAsset && forAsset.keepWaiting)
            {
                if(mAllBundles.TryGetValue(forAsset.groupName, out task) && task.isSuccess)
                {
                    var asset = (task.asset as AssetBundle).LoadAsset(forAsset.assetName, forAsset.type);
                    if(asset != null)
                    {
                        forAsset.Success(asset);
                        return true;
                    }
                    else
                    {
                        DebugEx.FormatWarning("AssetBundleAssetLoader::TryFinishSync ----> load from bundle fail {0}", forAsset.assetName);
                    }
                }
                else
                {
                    DebugEx.FormatWarning("AssetBundleAssetLoader::TryFinishSync ----> bundle no load {0}@{1}", forAsset.assetName, forAsset.groupName);
                }
            }
            return false;
        }
        public void ReleaseGroup(string group)
        {
            ResourceAsyncTask task = null;
            if(mAllBundles.TryGetValue(group, out task))
            {
                if(task.keepWaiting)
                {
                    DebugEx.FormatWarning("AssetBundleAssetLoader ----> don't support unload loading assetbundle, please implement later!");
                }
                else
                {
                    (task.asset as AssetBundle).Unload(true);
                }
            }
        }

        private IEnumerator _AtWaitAndLoadAsset(string group, string asset, System.Type type)
        {
            var outTask = new ResourceAsyncTaskForAsset(group, asset, type);
            yield return outTask;

            if(string.IsNullOrEmpty(asset) || string.IsNullOrEmpty(group))
            {
                outTask.FailWithResult(null, "not exists", (long)GameErrorCode.NoItem);
                yield break;
            }

            var bundleTask = LoadGroup(group);
            yield return bundleTask;
            if(outTask.isCanceling)
            {
                outTask.ResolveCancel();
                yield break;
            }
            if(bundleTask.isSuccess)
            {
                if(!outTask.keepWaiting)
                {
                    yield break;
                }
                // DebugEx.FormatTrace("AssetBundleAssetLoader._CoWaitAndLoadAsset ----> bundle load ok {0}, load {1}", group, asset);
                var bundle = bundleTask.asset as AssetBundle;
                var retAsync = bundle.LoadAssetAsync(asset, type);
                yield return retAsync;
                if(retAsync != null && retAsync.isDone && retAsync.asset != null)
                {
                    // DebugEx.FormatTrace("AssetBundleAssetLoader._CoWaitAndLoadAsset ----> bundle load ok {0}, load {1} finish", group, asset);
                    outTask.Success(retAsync.asset);
                    ResManager.TriggerAssetLoad(group, asset, type, retAsync.asset);
                }
                else
                {
                    DebugEx.FormatWarning("AssetBundleAssetLoader._CoWaitAndLoadAsset ----> bundle load ok, asset fail, {0}@{1}", asset, group);
                    outTask.Fail("bundle load success, but load asset fail");
                }
            }
            else
            {
                outTask.Fail(bundleTask.error);
            }
        }

        private IEnumerator _AtLoadBundle(string bundleName)
        {
            ResourceAsyncTask outtask = new ResourceAsyncTask();
            yield return outtask;

            List<ResourceAsyncTask> tasks = new List<ResourceAsyncTask>();
            var mainTask = AssetBundleManager.Instance.LoadAssetBundle(bundleName, tasks);
            int loadingCount = 1;
            while(outtask.keepWaiting && loadingCount > 0)
            {
                loadingCount = 0;
                for(int i = 0; i < tasks.Count; i++)
                {
                    if(tasks[i].keepWaiting)
                    {
                        loadingCount++;
                        yield return tasks[i];
                    }
                    else if(!tasks[i].isSuccess)
                    {
                        outtask.Fail("dependency download error");
                        break;
                    }
                }
            }
            if(outtask.keepWaiting)
            {
                if(mainTask == null)
                {
                    //no main task, we return
                    outtask.Success(null);
                }
                else
                {
                    yield return mainTask;
                    if(mainTask.isSuccess)
                    {
                        (mainTask.asset as AssetBundle).LoadAllAssets<Shader>();            //load all shader, so we needn't load shader in bundle for outside
                        outtask.Success(mainTask.asset);
                        ResManager.TriggerGroupLoad(bundleName);
                    }
                    else
                    {
                        outtask.Fail(mainTask.error);
                    }
                }
            }
        }
    }
}