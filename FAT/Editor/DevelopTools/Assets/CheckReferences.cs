using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace DevelopTools
{
    public class CheckReferences
    {
        public static readonly string PROJECT_DIR = Application.dataPath.Substring(0, Application.dataPath.Length - 6);//ALL

        [MenuItem("Assets/Check References", false, 10)]
        static private void Find()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            Object selectObj = Selection.activeObject;
            string path = AssetDatabase.GetAssetPath(selectObj);
            if (!string.IsNullOrEmpty(path))
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                List<string> extensions = new List<string>() { ".prefab", ".unity", ".mat", ".asset", ".fbx.meta", ".controller" };
                string[] allFiles = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories);
                string[] files = FilterFiles(allFiles, extensions);
                int startIndex = 0;
                int matchNum = 0;
                string output = "";
                EditorApplication.update = delegate ()
                {
                    string file = files[startIndex];
                    file = file.Replace("\\", "/");

                    bool isCancel = EditorUtility.DisplayCancelableProgressBar("匹配资源中", file, (float)startIndex / (float)files.Length);

                    //只检测Bundle目录
                    if (file.Contains("/Bundle/") && Regex.IsMatch(File.ReadAllText(file), guid) && file.Replace(CheckReferences.PROJECT_DIR, "").Replace(".meta", "") != path)
                    {
                        matchNum++;
                        output += file + "\n";
                    }

                    startIndex++;
                    if (isCancel || startIndex >= files.Length)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorApplication.update = null;
                        startIndex = 0;
                        Debug.LogWarning(matchNum + " references found for object " + selectObj.name + "\n\n" + output);
                    }
                };
            }
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