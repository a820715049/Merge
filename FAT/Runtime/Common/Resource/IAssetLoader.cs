/**
 * @Author: handong.liu
 * @Date: 2020-07-09 12:35:01
 */
using UnityEngine;
using System.Collections.Generic;

namespace EL.Resource 
{
    public interface IAssetLoader {
        void DestroyAsset(UnityEngine.Object asset, string group);
        ResourceAsyncTask LoadGroup(string group);
        bool TryFinishSync(ResourceAsyncTask task);
        bool IsGroupLoaded(string group);
        ResourceAsyncTask LoadAsset(string group, string asset, System.Type assetType);
        void ReleaseGroup(string group);
        bool HasGroup(string group);
        bool GetAllFilesInGroup(string group, List<string> container);
        void GetAllGroup(List<string> container);
        void Clear();
    }
}