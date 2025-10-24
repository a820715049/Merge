using UnityEditor;
using UnityEngine;

namespace DevelopTools
{
    public class CustomMenu
    {
        [MenuItem("CONTEXT/Component/Copy GUID")]
        public static void CopyGUID_Inspector(MenuCommand menuCommand)
        {
            var com = menuCommand.context as MonoBehaviour;
            if (com != null)
            {
                // 获取组件的类（脚本）对应的MonoScript
                var monoScript = MonoScript.FromMonoBehaviour(com);
                if (monoScript == null)
                {
                    var type = com.GetType();
                    var scripts = MonoImporter.GetAllRuntimeMonoScripts();
                    foreach (var s in scripts)
                    {
                        if (s != null && s.GetClass() == type)
                        {
                            monoScript = s;
                            break;
                        }
                    }
                }

                if (monoScript != null)
                {
                    var scriptPath = AssetDatabase.GetAssetPath(monoScript);
                    if (!string.IsNullOrEmpty(scriptPath))
                    {
                        var metaPath = scriptPath + ".meta";
                        if (System.IO.File.Exists(metaPath))
                        {
                            var lines = System.IO.File.ReadAllLines(metaPath);
                            foreach (var line in lines)
                            {
                                if (line.StartsWith("guid: "))
                                {
                                    var guid = line.Substring("guid: ".Length).Trim();
                                    GUIUtility.systemCopyBuffer = guid;
                                    // EditorUtility.DisplayDialog("GUID 复制", $"已复制脚本 {monoScript.name} 的 GUID: {guid}", "OK");
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("未找到脚本", "无法找到该组件对应的脚本文件。", "OK");
                }
            }
        }

        [MenuItem("Assets/Copy GUID", false, 12)]
        static private void CopyGUID_Assets()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path))
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                GUIUtility.systemCopyBuffer = guid;
            }
        }
    }
}