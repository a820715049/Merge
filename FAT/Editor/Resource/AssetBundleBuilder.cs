

using EL;
using UnityEngine;
using System.IO;
using UnityEditor;
/**
 * @Author: handong.liu
 * @Date: 2020-07-09 14:47:54
 */

public class AssetBundleBuilder
{
    [System.Serializable]
    public class Request
    {
        public enum ProfilerType
        {
            None,
            Normal,
            Deep
        }
        public string variant;
        public bool forceChangeVariant = false;         //即使variant已经到位了，是否还是要做一次切换
        public bool isAAB;
        public bool isIl2cpp;
        public bool isUpload;
        public bool buildNoAppStore;           //额外打一个不带iap的ios包
        public bool noBuildRes;             //不build 资源，拷贝现有资源
        public bool isProtectRes;
        public bool isProtectApp;
        public ProfilerType profilerType;
    }

    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundlesMenu() {
        var target = EditorUserBuildSettings.activeBuildTarget;
        BuildAllAssetBundles(target);
    }
    public static string BuildAllAssetBundles(BuildTarget target, string directory = null, System.Func<string, bool> bundleFilter = null) {
        DebugEx.FormatInfo("CreateAssetBundles.BuildAllAssetBundles ----> build for target {0}", target);
        var assetsToExport = GetAssetBundlesToBuild(bundleFilter);
        for (int i = 0; i < assetsToExport.Length; i++) {
            Debug.LogFormat("build bundle {0}: [{1}]", assetsToExport[i].assetBundleName, assetsToExport[i].assetNames.ToStringEx());
        }
        string assetBundleDirectory = string.IsNullOrEmpty(directory) ?
                                        Path.Combine(Path.Combine(Path.Combine(CommonEditorUtility.projectPath, "../Bundles"), target.ToString()), "Bundle")
                                        : directory;
        if (!Directory.Exists(assetBundleDirectory)) {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        // MobileTextureSubtarget oldTarget = MobileTextureSubtarget.Generic;
        // if (target == BuildTarget.Android) {
        //     oldTarget = EditorUserBuildSettings.androidBuildSubtarget;
        //     EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
        //     DebugEx.FormatInfo("CreateAssetBundles.BuildAllAssetBundles ----> android always use ETC2");
        // }
        var manifest = BuildPipeline.BuildAssetBundles(assetBundleDirectory, assetsToExport,
                                        BuildAssetBundleOptions.None,
                                        target);
        // if (target == BuildTarget.Android) {
        //     EditorUserBuildSettings.androidBuildSubtarget = oldTarget;
        // }
        //_OutputResources(assetBundleDirectory, manifest);
        return assetBundleDirectory;
    }

    //filter param: assetBundleName
    //filter return value: true means included
    static AssetBundleBuild[] GetAssetBundlesToBuild(System.Func<string, bool> assetFilter)
    {
        System.Collections.Generic.List<AssetBundleBuild> allBundles = new System.Collections.Generic.List<AssetBundleBuild>();
        var bundles = AssetDatabase.GetAllAssetBundleNames();
        foreach (var bd in bundles)
        {
            if (assetFilter != null && !assetFilter(bd))
            {
                continue;
            }

            var paths = AssetDatabase.GetAssetPathsFromAssetBundle(bd);
            if (paths.Length > 0)
            {
                var build = new AssetBundleBuild();
                build.assetBundleName = bd;
                build.assetBundleVariant = "";
                build.assetNames = paths;
                allBundles.Add(build);
            }
        }
        return allBundles.ToArray();
    }
}