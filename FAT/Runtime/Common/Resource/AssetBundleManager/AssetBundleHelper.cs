/*
 * @Author: qun.chao
 * @Date: 2025-01-03 11:32:01
 */
using System.Collections.Generic;
using UnityEngine;

namespace EL.Resource
{
    public static class AssetBundleHelper
    {
        // 重复尝试的记录 避免错误资源浪费时间
        private static Dictionary<string, int> dupRetryMap = new();
        // 资源首次加载失败时 优先猜测其失败原因为重复加载 则尝试在已加载列表中寻找
        public static bool IsDuplicate(string name, out AssetBundle bundle)
        {
            dupRetryMap.TryGetValue(name, out var count);
            bundle = count <= 1 ? GetLoadedAssetBundle(name) : null;
            if (bundle != null)
                dupRetryMap.Remove(name);
            else
                dupRetryMap[name] = count + 1;
            return bundle != null;
        }

        public static AssetBundle GetLoadedAssetBundle(string name)
        {
            var bundles = AssetBundle.GetAllLoadedAssetBundles();
            foreach (var b in bundles)
            {
                if (b.name.Equals(name))
                    return b;
            }
            return null;
        }
    }
}