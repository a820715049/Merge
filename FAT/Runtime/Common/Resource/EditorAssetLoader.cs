/**
 * @Author: handong.liu
 * @Date: 2020-07-09 15:59:30
 */
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
namespace EL.Resource
{
#if UNITY_EDITOR
    public class EditorAssetLoader : IAssetLoader
    {
        public enum GroupState {
            None,
            Loading,
            Ready,
        }

        public readonly Dictionary<string, ResourceAsyncTask> groupState = new();
        public static int GroupDelayMS = 1000;

        private class TaskRunner : MonoBehaviour
        {

        }
        private MonoBehaviour mGlobalTaskRunner;

        public ResourceAsyncTask LoadGroup(string group)
        {
            static async UniTask D(ResourceAsyncTask task_) {
                await UniTask.Delay(GroupDelayMS);
                task_.Success(null);
            }
            var task = new ResourceAsyncTask();
            groupState[group] = task;
            _ = D(task);
            return task;
        }
        public bool IsGroupLoaded(string group)
        {
            return groupState.TryGetValue(group, out var s) && !s.keepWaiting;
        }
        public void Clear()
        {
            
        }
        public void DestroyAsset(UnityEngine.Object asset, string group)
        {
            
        }
        public bool GetAllFilesInGroup(string group, List<string> container)
        {
            var groupDict = _GetGroupFiles(group);
            container.AddRange(groupDict.Keys);
            return true;
        }
        public ResourceAsyncTask LoadAsset(string group, string asset, System.Type assetType)
        {
            return _StartGlobalAsyncTask<ResourceAsyncTask>(_ATLoadAsset(group ,asset, assetType));
        }
        public bool HasGroup(string group)
        {
            return !string.IsNullOrEmpty(_FindGroupPath(group));
        }
        public void GetAllGroup(List<string> container)
        {
            var assets = AssetDatabase.GetAllAssetBundleNames();
            foreach(var a in assets)
            {
                var paths = AssetDatabase.GetAssetPathsFromAssetBundle(a);
                if(paths != null && paths.Length > 0)
                {
                    container.Add(_ToGroupName(a));
                }
            }
        }
        public void ReleaseGroup(string group)
        {
        }

        private string _ToGroupName(string bundleFileName)
        {
            int index = bundleFileName.LastIndexOf('.');
            if(index > 0)
            {
                return bundleFileName.Substring(0, index);
            }
            else
            {
                return bundleFileName;
            }
        }

        private Dictionary<string, string> _GetGroupFiles(string group)
        {
            Dictionary<string, string> ret = null;
            if(!mGroups.TryGetValue(group, out ret))
            {
                ret = new Dictionary<string, string>();
                mGroups.Add(group, ret);
                //search path
                var dirPath = _FindGroupPath(group);
                if(!string.IsNullOrEmpty(dirPath))
                {
                    var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
                    foreach(var fname in files)
                    {
                        if(!Directory.Exists(fname) && !fname.EndsWith(".meta"))            //not dir and not meta file
                        {
                            int startIdx = fname.LastIndexOf(Path.DirectorySeparatorChar) + 1;
                            int lastIdx = fname.LastIndexOf(".");
                            if(lastIdx < 0)
                            {
                                lastIdx = fname.Length;
                            }
                            ret[fname.Substring(startIdx)] = fname;// fname.Substring(0, lastIdx);
                        }
                    }
                }
            }
            return ret;
        }

        private string _FindGroupPath(string group)
        {
            var idx = group.IndexOf('_');
            if (idx < 0)
            {
                var bundlePath = $"Assets/Bundle/bundle_{group}";
                if (Directory.Exists(bundlePath))
                {
                    return bundlePath;
                }
            }
            else
            {
                while (idx >= 0)
                {
                    var parentPath = $"Assets/Bundle/{group[..idx]}";
                    if (Directory.Exists(parentPath))
                    {
                        var bundleFolder = $"bundle_{group[(idx + 1)..]}";
                        var dirs = Directory.GetDirectories(parentPath, bundleFolder, SearchOption.AllDirectories);
                        if (dirs.Length > 0)
                        {
                            return dirs[0];
                        }
                    }
                    idx = group.IndexOf('_', idx + 1);
                }
            }
            return null;
        }

        public bool TryFinishSync(ResourceAsyncTask task)
        {
            if(task is ResourceAsyncTaskForAsset assetTask && task.keepWaiting)
            {
                var groupDict = _GetGroupFiles(assetTask.groupName);
                if(groupDict.TryGetValue(assetTask.assetName, out var path))
                {
                    var res = AssetDatabase.LoadAssetAtPath(path, assetTask.type);
                    if(res != null)
                    {
                        task.Success(res);
                        return true;
                    }
                    else
                    {
                        DebugEx.FormatWarning("EditorAssetLoader::TryFinishSync ----> no asset at {0}", path);
                    }
                }
                else
                {
                    DebugEx.FormatWarning("EditorAssetLoader::TryFinishSync ----> no file at {0}, {1}", path, groupDict);
                }
            }
            return false;
        }
        private IEnumerator _ATLoadAsset(string group, string asset, System.Type type)
        {
            ResourceAsyncTask ret = null;
            var dict = mLoadingTasks.GetDefault(group, null);
            if(dict != null && dict.TryGetValue(asset, out ret))
            {
                yield return ret;
                yield break;
            }
            var groupDict = _GetGroupFiles(group);
            string path = null;
            var task = new ResourceAsyncTaskForAsset(group, asset, type);
            if(dict == null)
            {
                dict = new Dictionary<string, ResourceAsyncTask>();
                mLoadingTasks.Add(group, dict);
            }
            dict.Add(asset, task);
            yield return task;
            
            yield return null;      //skip 1 frame to simulate bundle async load

            if(groupDict.TryGetValue(asset, out path))
            {
                if (groupState.TryGetValue(group, out var gTask) && gTask.keepWaiting) {
                    yield return gTask;
                }
                var res = AssetDatabase.LoadAssetAtPath(path, type);
                if(res == null)
                {
                    var error = string.Format("file not exists {0}@{1}", asset, group);
                    DebugEx.FormatWarning("EditorAssetLoader ----> {0}, file are {1}, path is:{2}", error, groupDict, path);
                    task.Fail(string.Format("file not exists {0}@{1}", asset, group));
                }
                else
                {
                    task.Success(res);
                    ResManager.TriggerAssetLoad(group, asset, type, res);
                }
            }
            else
            {
                var error = string.Format("file not exists {0}@{1}", asset, group);
                DebugEx.FormatWarning("EditorAssetLoader ----> {0}, file are {1}", error, groupDict);
                task.Fail(string.Format("file not exists {0}@{1}", asset, group));
            }
        }

        private T _StartGlobalAsyncTask<T>(IEnumerator iter) where T: AsyncTaskBase
        {
            if(mGlobalTaskRunner == null)
            {
                mGlobalTaskRunner = new GameObject("EditorAssetLoader").AddComponent<TaskRunner>();
            }
            return mGlobalTaskRunner.StartAsyncTask<T>(iter);
        }

        private Dictionary<string, Dictionary<string, string>> mGroups = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, Dictionary<string, ResourceAsyncTask>> mLoadingTasks = new Dictionary<string, Dictionary<string, ResourceAsyncTask>>();
    }
#endif
}