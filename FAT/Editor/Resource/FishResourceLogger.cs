/*
 * @Author: Assistant
 * @Date: 2024-05-17
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FAT
{
    public static class FishResourceLogger
    {
        [MenuItem("Tools/Log Activity Fish Resources")]
        public static void LogActivityFishResources()
        {
            var groups = _CollectFishGroups();
            foreach (var group in groups)
            {
                Debug.Log($"Group: {group}");
                foreach (var asset in _GetAssetsByGroup(group))
                {
                    var bundle = AssetDatabase.GetImplicitAssetBundleName(asset);
                    Debug.Log($"  {bundle}: {asset}");
                }
            }
        }

        private static HashSet<string> _CollectFishGroups()
        {
            var result = new HashSet<string>();
            var fields = typeof(UIConfig).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.Name.StartsWith("UIActivityFish", StringComparison.Ordinal))
                {
                    if (field.GetValue(null) is UIResource res && !string.IsNullOrEmpty(res.prefabGroup))
                    {
                        result.Add(res.prefabGroup);
                    }
                }
            }
            return result;
        }

        private static IEnumerable<string> _GetAssetsByGroup(string group)
        {
            var dir = _FindGroupPath(group);
            if (string.IsNullOrEmpty(dir))
            {
                yield break;
            }
            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                {
                    continue;
                }
                var path = file.Replace('\\', '/');
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                yield return path;
            }
        }

        private static string _FindGroupPath(string group)
        {
            var idx = group.IndexOf('_');
            if (idx < 0)
            {
                var bundlePath = Path.Combine("Assets/Bundle", $"bundle_{group}");
                if (Directory.Exists(bundlePath))
                {
                    return bundlePath;
                }
            }
            else
            {
                while (idx >= 0)
                {
                    var parentPath = Path.Combine("Assets/Bundle", group[..idx]);
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
    }
}
