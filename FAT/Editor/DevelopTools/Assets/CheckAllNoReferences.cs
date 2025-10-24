using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DevelopTools
{

    public class CheckAllNoReferences
    {
        [MenuItem("Assets/CheckAll NoReferences", false, 11)]
        static void Find()
        {
            string path = GetSelectedAssetPath();
            if (path == null)
            {
                Debug.LogWarning("请先选择目标文件夹");
                return;
            }
            GetAllAssets(path, false);
        }

        [MenuItem("Assets/CheckAll NoReferences And Delete", false, 11)]
        static void FindAndDelete()
        {
            string path = GetSelectedAssetPath();
            if (path == null)
            {
                Debug.LogWarning("请先选择目标文件夹");
                return;
            }
            GetAllAssets(path, true);
        }

        public static void GetAllAssets(string rootDir, bool isDelete)
        {
            Dictionary<string, string> fileContents = GetCheckFileContents();
            List<string> result = new List<string>();

            DirectoryInfo dirinfo = new DirectoryInfo(rootDir);
            FileInfo[] fs = dirinfo.GetFiles("*.*", SearchOption.AllDirectories);
            int ind = 0;
            foreach (var f in fs)
            {
                EditorUtility.DisplayProgressBar("正在查询", f.Name, (float)ind / (float)fs.Length);
                ind++;

                int index = f.FullName.IndexOf("Assets", System.StringComparison.CurrentCulture);
                if (index != -1)
                {
                    string assetPath = f.FullName.Substring(index);
                    if (!assetPath.EndsWith(".png", System.StringComparison.CurrentCultureIgnoreCase) &&
                        !assetPath.EndsWith(".jpg", System.StringComparison.CurrentCultureIgnoreCase) &&
                        !assetPath.EndsWith(".tga", System.StringComparison.CurrentCultureIgnoreCase) &&
                        !assetPath.EndsWith(".mat", System.StringComparison.CurrentCultureIgnoreCase) &&
                        !assetPath.EndsWith(".fbx", System.StringComparison.CurrentCultureIgnoreCase) &&
                        !assetPath.EndsWith(".prefab", System.StringComparison.CurrentCultureIgnoreCase) &&
                        !assetPath.EndsWith(".controller", System.StringComparison.CurrentCultureIgnoreCase) &&
                        !assetPath.EndsWith(".anim", System.StringComparison.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }

                    if (!CheckMatch(fileContents, assetPath))
                    {
                        result.Add(assetPath);
                        if (isDelete)
                        {
                            AssetDatabase.DeleteAsset(assetPath);
                        }
                    }
                }
            }
            EditorUtility.ClearProgressBar();

            AssetDatabase.Refresh();

            Debug.LogWarning($"{result.Count}个文件未被引用 {rootDir} \n\n {string.Join("\n", result)} \n\n");
        }

        static string GetSelectedAssetPath()
        {
            var selected = Selection.activeObject;
            if (selected == null)
            {
                return null;
            }

            if (selected is DefaultAsset)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                return path;
            }
            else
            {
                Debug.LogWarning(selected);
                return null;
            }
        }

        static Dictionary<string, string> GetCheckFileContents()
        {
            List<string> extensions = new List<string>() { ".prefab", ".unity", ".mat", ".asset", ".fbx.meta", ".controller" };
            string[] allFiles = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories);
            string[] files = FilterFiles(allFiles, extensions);
            Dictionary<string, string> fileContents = new Dictionary<string, string>();
            int index = 0;
            foreach (string file in files)
            {
                fileContents[file] = File.ReadAllText(file);
                index++;
            }
            return fileContents;
        }

        static bool CheckMatch(Dictionary<string, string> checkFileContents, string path)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            foreach (var item in checkFileContents)
            {
                string file = item.Key;
                string content = item.Value;
                if (Regex.IsMatch(content, guid) && file.Replace(CheckReferences.PROJECT_DIR, "").Replace(".meta", "") != path)
                {
                    return true;
                }
            }
            return false;
        }

        static string[] FilterFiles(string[] allFiles, List<string> extensions)
        {
            List<string> fileList = new List<string>();
            foreach (var file in allFiles)
            {
                foreach (var extension in extensions)
                {
                    if (file.ToLower().EndsWith(extension))
                    {
                        fileList.Add(file);
                        break;
                    }
                }
            }
            return fileList.ToArray();
        }
    }
}
