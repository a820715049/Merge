/**
 * @Author: handong.liu
 * @Date: 2021-03-30 12:46:59
 */
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using CenturyGame.AppBuilder.Editor.Builds;
using EL;

public static class AppBuilderEditorUtility
{
    public static string GetAppBuildConfigAbsolutePath()
    {
        var path = AppBuildConfig.GetAppBuildConfigInst().repositoryInfo.gameTableDataRepositoryLocalDirName;
        return Path.GetFullPath(path);
    }
    
    public static void SyncConf(bool data = true, bool source = true, bool lua = false) {
        const string ConfRepoPath = "../conf";
        var list = new List<(string, string, string[], string[])>();
        if (data) list.Add((ConfRepoPath + "/gen/rawdata/client", Path.Combine(Application.streamingAssetsPath, Constant.kConfPath),
            new string[] { "*.bytes" }, null));
        if (source) list.Add((ConfRepoPath + "/gen/csharp/Config", "Assets/Scripts/FAT/Game/AutoCode/Server/csharp/Config",
            new string[] { "*.cs" }, new string[] { ".asmdef", "package.json", "link.xml" }));
            /*
        if (lua) list.Add((ConfRepoPath + "/gen/lua", $"Assets/{LuaPathEditor}/gen",
            new string[] { "*.lua" }, null));*/
        foreach (var (src, _, _, _) in list) {
            if (!Directory.Exists(src)) {
                Debug.LogError($"Config repo path unavailable:{src}");
                return;
            }
        }
        foreach (var (src, dst, pattern, filter) in list) {
            if (filter != null) DeleteDirectoryWithFilter(dst, filter);
            else if (Directory.Exists(dst)) Directory.Delete(dst, true);
            Debug.Log($"{src} -> {dst}");
            CopyDirectory(src, dst, pattern);
        }
        AssetDatabase.Refresh();
    }

    public static void ClearVersionCache(bool overwrite, string version) {
        var platform = 
        #if UNITY_IPHONE
            "iOS";
        #else
            "Android";
        #endif
        var appInfoFile = Path.Combine(Application.streamingAssetsPath, "app_info.x");
        var resDataFile = Path.Combine(Application.streamingAssetsPath, "res_data.json");
        var resAssetFile = Path.Combine(Application.streamingAssetsPath, string.Format("res_{0}.json", platform.ToLower()));
        var list = new string[] { appInfoFile, resDataFile, resAssetFile };
        if (!overwrite && list.All(f => File.Exists(f))) return;
        var emptyAppInfo = $"{{\"version\": \"{version}\", \"dataResVersion\": \"0\", \"unityDataResVersion\": \"0\", \"TargetPlatform\": \"{platform}\"}}";
        Directory.CreateDirectory(Application.streamingAssetsPath);
        File.WriteAllText(appInfoFile, emptyAppInfo);
        File.WriteAllText(resDataFile, "{}");
        File.WriteAllText(resAssetFile, "{}");
        var sandboxPath = Application.dataPath.Substring(0, Application.dataPath.Length - 7) + "/sandbox";
        if (Directory.Exists(sandboxPath)) {
            Directory.Delete(sandboxPath, true);
        }
    }

    public static void CopyDirectory(string source, string destination, params string[] patternList) {
        var dir = new DirectoryInfo(source);
        Directory.CreateDirectory(destination);
        foreach (var pattern in patternList) {
            foreach (var file in dir.EnumerateFiles(pattern)) {
                File.Copy(Path.Combine(source, file.Name), Path.Combine(destination, file.Name));
            }
        }
        foreach (var sub in dir.EnumerateDirectories()) {
            CopyDirectory(sub.FullName, Path.Combine(destination, sub.Name), patternList);
        }
    }

    public static void DeleteDirectoryWithFilter(string path, params string[] filter) {
        if (!Directory.Exists(path)) return;
        foreach (var sub in Directory.EnumerateDirectories(path)) {
            Directory.Delete(sub, true);
        }
        foreach (var file in Directory.EnumerateFiles(path)) {
            if (!filter.Any(v => file.EndsWith(v))) {
                File.Delete(file);
            }
        }
    }
}
#endif