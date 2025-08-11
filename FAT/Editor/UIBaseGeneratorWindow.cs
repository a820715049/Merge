using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FAT.Editor
{
    public class UIBaseGeneratorWindow : EditorWindow
    {
        private string _outputPath = "Scripts/UI";

        [MenuItem("Tools/Generate UI Class")]
        public static void ShowWindow()
        {
            GetWindow<UIBaseGeneratorWindow>("UI Class Generator");
            LoadSavedPath();
        }

        private void OnGUI()
        {
            GUILayout.Label("UI Class Generator", EditorStyles.boldLabel);

            // 路径配置区域
            GUILayout.Space(10);
            _outputPath = EditorGUILayout.TextField("输出路径", _outputPath);

            if (GUILayout.Button("选择路径"))
            {
                string path = EditorUtility.SaveFolderPanel("选择脚本生成路径", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _outputPath = Path.GetRelativePath(Application.dataPath, path);
                    EditorPrefs.SetString("UI_SCRIPT_OUTPUT_PATH", _outputPath);
                }
            }

            GUILayout.Space(20);
            if (GUILayout.Button("生成UI类"))
            {
                GenerateUIScript();
            }
        }

        private static void LoadSavedPath()
        {
            var window = GetWindow<UIBaseGeneratorWindow>();
            window._outputPath = EditorPrefs.GetString("UI_SCRIPT_OUTPUT_PATH", "Scripts/UI");
        }

        private void GenerateUIScript()
        {
            GameObject selectedPrefab = Selection.activeObject as GameObject;

            if (selectedPrefab == null || !PrefabUtility.IsPartOfPrefabAsset(selectedPrefab))
            {
                Debug.LogError("请先选中一个Prefab");
                return;
            }

            string className = selectedPrefab.name;
            string fullPath = Path.Combine(Application.dataPath, _outputPath, $"{className}.cs");

            if (File.Exists(fullPath))
            {
                bool overwrite = EditorUtility.DisplayDialog("文件已存在", $"脚本 {className}.cs 已存在，是否覆盖？", "覆盖", "取消");
                if (!overwrite) return;
            }

            try
            {
                Debug.Log($"开始生成脚本路径：{fullPath}");
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                var imageNodes = FindImageNodes(selectedPrefab);
                var textNodes = FindTextNodes(selectedPrefab);
                var objectNodes = FindObjectNodes(selectedPrefab);

                string scriptContent = $@"
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{{
    public class {className} : UIBase
    {{
        // Image字段
        {GenerateImageFields(imageNodes)}
        
        // Text字段
        {GenerateTextFields(textNodes)}
        
        // Object字段
        {GenerateObjectFields(objectNodes)}

        protected override void OnCreate()
        {{
            base.OnCreate();
            // Image绑定
            {GenerateAccessCalls(imageNodes)}
            // Text绑定
            {GenerateTextAccessCalls(textNodes)}
            // Object绑定
            {GenerateObjectAccessCalls(objectNodes)}
        }}

        protected override void OnParse(params object[] items)
        {{

        }}

        protected override void OnPreOpen()
        {{

        }}

        protected override void OnPostOpen()
        {{

        }}

        protected override void OnAddListener()
        {{

        }}

        protected override void OnRefresh()
        {{

        }}

        protected override void OnRemoveListener()
        {{

        }}

        protected override void OnPreClose()
        {{

        }}

        protected override void OnPostClose()
        {{
        }}
    }}
}}
                ";
                File.WriteAllText(fullPath, scriptContent);
                AssetDatabase.Refresh();
                Debug.Log($"成功生成UI类：{AssetDatabase.GetAssetPath(Selection.activeObject)}");

                // 延迟挂载组件
                //EditorApplication.delayCall += () => AddComponentToPrefab(selectedPrefab, className);
            }
            catch (Exception e)
            {
                Debug.LogError($"生成失败：{e.Message}");
            }
        }

        private void AddComponentToPrefab(GameObject prefab, string className)
        {
            Type componentType = Type.GetType($"FAT.{className}, Assembly-CSharp");
            if (componentType == null)
            {
                Debug.LogError($"组件类型FAT.{className}加载失败，请等待编译完成");
                return;
            }

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                if (instance.GetComponent(componentType) == null)
                {
                    instance.AddComponent(componentType);
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    Debug.Log($"成功挂载组件：{className}");
                }
                else
                {
                    Debug.Log($"组件{className}已存在，跳过挂载");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(instance);
            }
        }

        private Dictionary<string, string> FindImageNodes(GameObject prefab)
        {
            var imageNodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.EndsWith("_img", StringComparison.OrdinalIgnoreCase))
                {
                    string fieldName = child.name.Replace("_img", "", StringComparison.OrdinalIgnoreCase);
                    string path = GetRelativePath(prefab.transform, child);
                    imageNodes[fieldName] = path;
                }
            }
            return imageNodes;
        }

        private Dictionary<string, string> FindTextNodes(GameObject prefab)
        {
            return FindNodesBySuffix(prefab, "_txt");
        }

        private Dictionary<string, string> FindObjectNodes(GameObject prefab)
        {
            return FindNodesBySuffix(prefab, "_obj");
        }

        private Dictionary<string, string> FindNodesBySuffix(GameObject prefab, string suffix)
        {
            var nodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    string fieldName = child.name.Replace(suffix, "", StringComparison.OrdinalIgnoreCase);
                    string path = GetRelativePath(prefab.transform, child);
                    nodes[fieldName] = path;
                }
            }
            return nodes;
        }

        private static string GetRelativePath(Transform root, Transform child)
        {
            var pathSegments = new List<string>();
            Transform current = child;
            while (current != null && current != root)
            {
                pathSegments.Add(current.name.Replace("_img", "").Replace("_txt", "").Replace("_obj", ""));
                current = current.parent;
            }
            pathSegments.Reverse();
            return string.Join("/", pathSegments);
        }

        private static string GenerateImageFields(Dictionary<string, string> nodes)
        {
            return nodes.Count == 0
                ? ""
                : string.Join("\n        ", nodes.Keys.Select(k => $"private UIImageRes _{k};"));
        }

        private static string GenerateTextFields(Dictionary<string, string> nodes)
        {
            return nodes.Count == 0
                ? ""
                : string.Join("\n        ", nodes.Keys.Select(k => $"private TextMeshProUGUI _{k};"));
        }

        private static string GenerateObjectFields(Dictionary<string, string> nodes)
        {
            return nodes.Count == 0
                ? ""
                : string.Join("\n        ", nodes.Keys.Select(k => $"private Transform _{k};"));
        }

        private static string GenerateAccessCalls(Dictionary<string, string> nodes)
        {
            return nodes.Count == 0
                ? ""
                : string.Join("\n            ", nodes.Select(kv => $"transform.Access(\"{kv.Value}\", out _{kv.Key});"));
        }

        private static string GenerateTextAccessCalls(Dictionary<string, string> nodes)
        {
            return nodes.Count == 0
                ? ""
                : string.Join("\n            ", nodes.Select(kv => $"transform.Access(\"{kv.Value}\", out _{kv.Key});"));
        }

        private static string GenerateObjectAccessCalls(Dictionary<string, string> nodes)
        {
            return nodes.Count == 0
                ? ""
                : string.Join("\n            ", nodes.Select(kv => $"_{kv.Key} = transform.Find(\"{kv.Value}\");"));
        }
    }
}
