
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using EL;

public class AssetBundleArranger : AssetPostprocessor
{
    public static readonly string kAssetBundleRootName = "Bundle";
    public static readonly string kAssetBundleRootPath = "Assets/" + kAssetBundleRootName;
    public static readonly string kAssetNoBundleRootPath = "Assets/BundleNo";

    public static bool ShouldPackInBundle(string path)
    {
        return path.StartsWith(kAssetBundleRootPath) && !path.Contains("__edit_") &&
               !Directory.Exists(Path.Combine(CommonEditorUtility.projectPath, path));
    }

    public static bool ShouldNotPackInBundle(string path)
    {
        return path.StartsWith(kAssetNoBundleRootPath) &&
               !Directory.Exists(Path.Combine(CommonEditorUtility.projectPath, path));
    }

    //for performance, path should always starts with "Assets/"
    public static string BundleName(ReadOnlySpan<char> path)
    {
        var l1 = kAssetBundleRootPath.Length + 1;
        path = path[l1..];
        var s = path.IndexOf('/');
        var b1 = path[..s];
        path = path[s..];
        var t = "/bundle_";
        s = path.IndexOf(t);
        s = s >= 0 ? (s + t.Length) : 1;
        path = path[s..];
        s = path.IndexOf('/');
        var b2 = path[..s];
        return $"{b1.ToString()}_{b2.ToString()}";
    }
    public void OnPreprocessAsset()
    {
        var path = assetImporter.assetPath;
        if (ShouldNotPackInBundle(path))
        {
            if (assetImporter.assetBundleName != null) {
                assetImporter.SetAssetBundleNameAndVariant(null, null);
            }
            return;
        }
        else if (ShouldPackInBundle(path))
        {
            var name = BundleName(path);
            if (name != assetImporter.assetBundleName) {
                assetImporter.SetAssetBundleNameAndVariant(name + ".ab", "");
            }
            DebugEx.FormatInfo("AssetBundleArranger ----> asset info {0}:{1}", path, name);
        }
    }
}