/**
 * @Author: handong.liu
 * @Date: 2020-07-23 18:44:42
 */
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Security.Cryptography;
using System.Collections.Generic;
public static class CommonEditorUtility
{
    private static string sProjectPath;
    private static string sToolPath;
    public static string unityAssetPathSeparator = "/";
    public static string bundleDirPrefix = "bundle_";
    public const string kUIAssetPath = "Assets/Bundle/ui";
    public const string kI18NDataAssetPath = "Assets/Bundle/i18n";
    public const string kExcelDataAssetPath = "Assets/Bundle/bundle_data";
    public static string projectPath
    {
        get
        {
            if (string.IsNullOrEmpty(sProjectPath))
            {
                sProjectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 7);          //delete last /Assets
            }
            return sProjectPath;
        }
    }
    public static string toolPath
    {
        get
        {
            if (string.IsNullOrEmpty(sToolPath))
            {
                sToolPath = System.IO.Path.Combine(System.IO.Path.Combine(projectPath, ".."), "DreamLife_tools");
                sProjectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 7);          //delete last /Assets
            }
            return sToolPath;
        }
    }

    public static string OptimazeNativePath(string path, bool getFullPath = true)
    {
        if (getFullPath)
            path = Path.GetFullPath(path);
        path = path.Replace(@"\", @"/");
        return path;
    }

    public static string ConvertAssetPathToNativePath(string assetPath)
    {
        return Path.Combine(projectPath, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }

    public static string ConvertNativePathToAssetPath(string nativePath)
    {
        var fullPath = Path.GetFullPath(nativePath);
        return nativePath.Replace(Path.DirectorySeparatorChar, '/').Substring(projectPath.Length + 1);
    }

    public static void SaveAsset(UnityEngine.Object asset, string path)
    {
        var target = AssetDatabase.LoadMainAssetAtPath(path);
        if (target == null)
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }
        else
        {
            EditorUtility.CopySerialized(asset, target);
            AssetDatabase.SaveAssets();
        }
    }


    public static string GetMD5(byte[] bytes)
    {
        var md5 = new MD5CryptoServiceProvider();
        var md5Bytes = md5.ComputeHash(bytes);
        return GetMd5String(md5Bytes);
    }

    public static string GetMD5(string path)
    {
        byte[] retval = null;
        FileInfo file = new FileInfo(path);
        if (file.Attributes != FileAttributes.Normal)
        {
            file.Attributes = FileAttributes.Normal;
        }

        using (FileStream fs = file.OpenRead())
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            retval = md5.ComputeHash(fs);
        }
        return GetMd5String(retval);
    }

    private static string GetMd5String(byte[] md5)
    {
        StringBuilder sc = new StringBuilder();
        for (int i = 0; i < md5.Length; i++)
        {
            sc.Append(md5[i].ToString("x2"));
        }
        return sc.ToString();
    }

    public static bool ExecutePythonTools(string scriptName)
    {
        System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo("python");
        Debug.LogFormat("env:{0}", System.Environment.GetEnvironmentVariable("PATH"));
        start.Arguments = System.IO.Path.Combine(CommonEditorUtility.toolPath, scriptName);
        //Debug.LogFormat("_ExecutePythonTools ----> {0}", start.Arguments);
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.StandardOutputEncoding = System.Text.UTF8Encoding.UTF8;
        start.StandardErrorEncoding = System.Text.UTF8Encoding.UTF8;
        System.Diagnostics.Process p = System.Diagnostics.Process.Start(start);
        string error = p.StandardError.ReadToEnd();
        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        p.Close();
        if (string.IsNullOrEmpty(error))
        {
            Debug.LogFormat(output);
            return true;
        }
        else
        {
            Debug.LogError(error);
            return false;
        }
    }

    public static T CreateInstanceOfSubclass<T>(params object[] param)
    {
        var type = GetSubclassOrSelf(typeof(T));
        Debug.LogFormat("create variant record {0}", type.Name);
        var ret = (T)System.Activator.CreateInstance(type, param);
        return ret;
    }

    public static System.Type GetSubclassOrSelf(System.Type type)
    {
        var assemblyList = CommonEditorUtility.GetListOfEntryAssemblyWithReferences();
        var containers = new List<System.Type>();
        for (int i = assemblyList.Count - 1; i >= 0; i--)
        {
            try
            {
                var types = assemblyList[i].GetTypes();
                foreach (var t in types)
                {
                    if (t.IsSubclassOf(type))
                    {
                        containers.Add(t);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("error {0}:{1}", ex.Message, ex.StackTrace);
            }
        }
        if (containers.Count > 0)
        {
            type = containers[0];
        }
        return type;
    }

    public static List<System.Reflection.Assembly> GetListOfEntryAssemblyWithReferences()
    {
        List<System.Reflection.Assembly> listOfAssemblies = new List<System.Reflection.Assembly>();
        var mainAsm = System.AppDomain.CurrentDomain.GetAssemblies();
        listOfAssemblies.AddRange(mainAsm);
        return listOfAssemblies;
    }

    public static IEnumerable<string> WalkAllFileAtPath(string path, string ext)
    {
        if (Directory.Exists(path))
        {
            //is a directory
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.ToLower().EndsWith(ext))
                {
                    yield return file;
                }
            }
        }
        else if (path.EndsWith(ext))
        {
            yield return path;
        }
        else
        {
            yield break;
        }
    }
    public static void ImportFolder(string selectedPath)
    {
        var realPath = Application.dataPath;
        realPath = realPath.Remove(realPath.Length - 6);
        var fileEntries = Directory.GetFiles(System.IO.Path.Combine(projectPath, selectedPath), "*", SearchOption.AllDirectories);
        foreach (var file in fileEntries)
        {
            if (file.EndsWith(".meta"))
            {
                continue;
            }
            var f = file.Replace("\\", "/");
            f = f.Remove(0, realPath.Length);
            AssetDatabase.ImportAsset(f);
        }
    }

    public static void ClearDirectory(string dir)
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }
        Directory.CreateDirectory(dir);
    }

    public static void CopyDirectoryContent(string dirSrcFullPath, string dirDstFullPath)
    {
        var subdirs = System.IO.Directory.GetDirectories(dirSrcFullPath, "*", SearchOption.AllDirectories);
        foreach (var subdir in subdirs)
        {
            Directory.CreateDirectory(dirDstFullPath + subdir.Substring(dirSrcFullPath.Length));
        }
        var files = System.IO.Directory.GetFiles(dirSrcFullPath, "*", SearchOption.AllDirectories);
        foreach (var f in files)
        {
            File.Copy(f, dirDstFullPath + f.Substring(dirSrcFullPath.Length), true);
        }
    }

    [MenuItem("Assets/Cleanup Missing Scripts")]
    static void CleanupMissingScripts()
    {
        HashSet<GameObject> visited = new HashSet<GameObject>();
        Queue<GameObject> toExpand = new Queue<GameObject>();
        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            visited.Add(Selection.gameObjects[i]);
            toExpand.Enqueue(Selection.gameObjects[i]);
        }
        while (toExpand.Count > 0)
        {
            var gameObject = toExpand.Dequeue();
            var count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            if (count > 0)
            {
                Debug.LogFormat(gameObject, "CleanupMissingScript ----> cleaned {0} script", count);
            }
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var childGo = gameObject.transform.GetChild(i).gameObject;
                if (!visited.Contains(childGo))
                {
                    toExpand.Enqueue(childGo);
                }
            }
        }
    }

    [MenuItem("Assets/ConvertAllMesh")]
    static void ConvertAllMeshColorToHalf()
    {
        string[] strs = Selection.assetGUIDs;
        List<Color> colors = new List<Color>();
        foreach (var str in strs)
        {
            var root = Path.Combine(projectPath, AssetDatabase.GUIDToAssetPath(str));
            Debug.LogFormat("ConvertAllMeshColorToHalf ----> process root {0}:{1}", str, root);
            foreach (var path in WalkAllFileAtPath(root, "fbx"))
            {
                var meshs = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Mesh mesh in meshs)
                {
                    colors.Clear();
                    mesh.GetColors(colors);
                    for (int i = 0; i < colors.Count; i++)
                    {
                        colors[i] = new Color(0.5f, 0.5f, 0.5f);
                    }
                    mesh.SetColors(colors);
                    Debug.LogFormat("ConvertAllMeshColorToHalf ----> modify mesh {0}@{1}", mesh.name, path);
                }
            }
        }
    }

    [MenuItem("Assets/GenerateRolePartPrefab")]
    static void GenerateRolePartPrefab()
    {
        string[] strs = Selection.assetGUIDs;
        List<Color> colors = new List<Color>();
        foreach (var str in strs)
        {
            var root = Path.Combine(projectPath, AssetDatabase.GUIDToAssetPath(str));
            Debug.LogFormat("GenerateRolePartPrefab ----> process root {0}:{1}", str, root);
            foreach (var path in WalkAllFileAtPath(root, "fbx"))
            {
                var assetPath = path.Substring(projectPath.Length + 1);
                UnityEditor.ModelImporter setting = AssetImporter.GetAtPath(assetPath) as UnityEditor.ModelImporter;
                var meshs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            }
        }
    }

    public static string FindExePath(string name, out List<string> paths)
    {
        var str = System.Environment.GetEnvironmentVariable("PATH");
        if(string.IsNullOrEmpty(str))
        {
            str = System.Environment.GetEnvironmentVariable("path");
        }
        if(string.IsNullOrEmpty(str))
        {
            paths = new List<string>();
            return "";
        }
        var index = str.IndexOf("!");
        if(index >= 0)
        {
            str = str.Substring(index + 1);
        }
        // Debug.LogFormat("seperator:{0}, {1}", System.IO.Path.PathSeparator, str);
        var pathes = paths = str.Split(System.IO.Path.PathSeparator).ToList();
        #if UNITY_EDITOR_WIN
        #else
        pathes.Add("/usr/local/bin");
        pathes.Add("/opt/homebrew/bin");
        #endif
        var chosen = pathes.Select(x => System.IO.Path.Combine(x, name)).Where(x => System.IO.File.Exists(x)).FirstOrDefault();
        if(string.IsNullOrEmpty(chosen))
        {
        #if UNITY_EDITOR_WIN
            name = name + ".exe";
            chosen = pathes.Select(x => System.IO.Path.Combine(x, name)).Where(x => System.IO.File.Exists(x)).FirstOrDefault();
        #endif
        }
        return chosen;
    }

    public static int StartShellWithOutputs(string cmd, string args, string wd, System.Func<string, bool> onOutput = null)
    {
    #if UNITY_EDITOR_WIN
        args = $"/c '{cmd} {args}'";
        cmd = "cmd";
    #else
        args = $"-c '{cmd} {args}'";
        cmd = "bash";
    #endif
        Debug.LogFormat("CommonEditorUtility::StartProcess ----> call shell {0}", args);
        return StartProcess(cmd, args, wd, false, onOutput);
    }

    //onOutput: 返回false代表此行会在最终结果出现
    public static int StartProcess(string exe, string args, string workDir, bool shell = false, System.Func<string, bool> onOutput = null)
    {
        string exePath = exe;
        List<string> triedPath = null;
        if(!shell)
        {
            exePath = FindExePath(exePath, out triedPath);
        }
        if(string.IsNullOrEmpty(exePath))
        {
            Debug.LogErrorFormat("CommonEditorUtility::StartProcess ----> no exe found {0}, paths: {1}", exe, string.Join(",", triedPath));
            return 1;
        }
        Debug.LogFormat("CommonEditorUtility::StartProcess ----> start {0} in {1} {2} [wd={3}]", exe, exePath, args, workDir);
        var pStartInfo = new System.Diagnostics.ProcessStartInfo();
        pStartInfo.FileName = exePath;
        pStartInfo.UseShellExecute = shell;
        pStartInfo.RedirectStandardInput = !shell;
        pStartInfo.RedirectStandardOutput = !shell;
        pStartInfo.RedirectStandardError = !shell;
        var env = System.Environment.GetEnvironmentVariables();
        foreach(var k in env.Keys)
        {
            if(k is string keyStr && env[k] is string valStr)
            {
                if(keyStr.ToLower() == "path")
                {
                    var index = valStr.IndexOf("!");
                    if(index >= 0)
                    {
                        valStr = valStr.Substring(index + 1);
                    }
                }
                pStartInfo.EnvironmentVariables[keyStr] = valStr;
            }
        }
        
        System.Environment.GetEnvironmentVariables();
        workDir = CommonEditorUtility.OptimazeNativePath(workDir);
        pStartInfo.WorkingDirectory = workDir;

        pStartInfo.CreateNoWindow = true;
        pStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        pStartInfo.Arguments = args;

        pStartInfo.StandardErrorEncoding = System.Text.UTF8Encoding.UTF8;
        pStartInfo.StandardOutputEncoding = System.Text.UTF8Encoding.UTF8;

        var proces = System.Diagnostics.Process.Start(pStartInfo);
        var outputAll = "";
        while (!proces.StandardOutput.EndOfStream)
        {
            string line = proces.StandardOutput.ReadLine();
            if(onOutput == null || !onOutput.Invoke(line))
            {
                outputAll += line + "\n";
            }
        }
        // Debug.LogFormat("{0}:{1}", exe, outputAll);
        var err = proces.StandardError.ReadToEnd();
        // if (!string.IsNullOrEmpty(err))
        // {
        //     Debug.LogErrorFormat("{0}:{1}", exe, err);
        // }
        proces.WaitForExit();
        var ret = proces.ExitCode;

        if (ret == 0)
        {
            Debug.Log($"{exe} exit with code {ret} \n stdout: {outputAll} \n stderr: {err}");
        }
        else
        {
            Debug.LogError($"{exe} exit with code {ret} \n stdout: {outputAll} \n stderr: {err}");
        }

        proces.Close();
        return ret;
    }

    [MenuItem("Tools/ClearData")]
    static void ClearData()
    {
        PlayerPrefs.DeleteAll();
    }
    [MenuItem("Tools/Show Missing Object References in assets", false, 52)]

    [MenuItem("Tools/Find missing components")]
    public static void MissingSpritesInAssets()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        var allAssets = AssetDatabase.GetAllAssetPaths();
        var objs = allAssets.Where(a => a.Contains(path)).Select(a => AssetDatabase.LoadAssetAtPath(a, typeof(GameObject)) as GameObject).Where(a => a != null);

        FindMissingReferences("Project", objs);
    }

    private static void FindMissingReferences(string context, IEnumerable<GameObject> objects)
    {
        foreach (var go in objects)
        {
            var components = go.GetComponents<Component>();

            foreach (var c in components)
            {
                if (!c)
                {
                    UnityEngine.Debug.LogError("Missing Component in GO: " + go.name, go);
                    continue;
                }

                SerializedObject so = new SerializedObject(c);
                var sp = so.GetIterator();

                while (sp.NextVisible(true))
                {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (sp.objectReferenceValue == null
                            && sp.objectReferenceInstanceIDValue != 0)
                        {
                            ShowError(context, go, c.GetType().Name, ObjectNames.NicifyVariableName(sp.name));
                        }
                    }
                }
            }
        }
    }

    private const string err = "Missing Ref in: [{3}]{0}. Component: {1}, Property: {2}";
    private static void ShowError(string context, GameObject go, string c, string property)
    {
        UnityEngine.Debug.LogError(string.Format(err, go.transform.name, c, property, context), go);
    }
}