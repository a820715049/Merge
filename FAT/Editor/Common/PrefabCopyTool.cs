/**
 * @Author: zhangpengjian
 * @Date: 2025/3/5 16:18:36
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/3/5 16:18:36
 * Description: 复制活动帮助预制体
 */

using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabCopyTool
{
    // 指定源预制体路径，根据实际情况修改
    private const string SOURCE_PREFAB_PATH_3 = "Assets/BundleNo/CopyHelpPrefab/UIActivityHelpExample3.prefab";
    private const string SOURCE_PREFAB_PATH_4 = "Assets/BundleNo/CopyHelpPrefab/UIActivityHelpExample4.prefab";
    private const string SOURCE_PREFAB_PATH_5 = "Assets/BundleNo/CopyHelpPrefab/UIActivityHelpExample5.prefab";

    [MenuItem("Tools/复制活动帮助预制体/三段式", false, 1000)]
    static void CopyPrefab()
    {
        // 加载指定路径的预制体
        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_PREFAB_PATH_3);
        CopyPrefab(sourcePrefab, SOURCE_PREFAB_PATH_3);

    }

    [MenuItem("Tools/复制活动帮助预制体/四段式", false, 1001)]
    static void CopyPrefab4()
    {
        // 加载指定路径的预制体
        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_PREFAB_PATH_4);
        CopyPrefab(sourcePrefab, SOURCE_PREFAB_PATH_4);
    }

    [MenuItem("Tools/复制活动帮助预制体/五段式", false, 1002)]
    static void CopyPrefab5()
    {
        // 加载指定路径的预制体
        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_PREFAB_PATH_5);
        CopyPrefab(sourcePrefab, SOURCE_PREFAB_PATH_5);
    }

    private static void CopyPrefab(GameObject sourcePrefab, string targetPath)
    {
        if (sourcePrefab == null)
        {
            EditorUtility.DisplayDialog("错误", $"在路径 {targetPath} 未找到预制体！", "确定");
            return;
        }

        // 打开保存文件对话框
        string fileName = Path.GetFileName(targetPath);
        string savePath = EditorUtility.SaveFilePanel(
            "保存预制体副本",
            "Assets/Bundle/event",
            fileName,
            "prefab"
        );

        // 如果用户取消了保存，直接返回
        if (string.IsNullOrEmpty(savePath))
            return;

        // 转换为项目相对路径
        if (savePath.StartsWith(Application.dataPath))
        {
            savePath = "Assets" + savePath.Substring(Application.dataPath.Length);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", "请将文件保存在项目内！", "确定");
            return;
        }

        // 复制预制体
        if (AssetDatabase.CopyAsset(targetPath, savePath))
        {
            EditorUtility.DisplayDialog("成功", "预制体已成功复制！如有特殊样式请按效果图修改！", "确定");
            AssetDatabase.Refresh();
        }
        else
        {
            EditorUtility.DisplayDialog("错误", "预制体复制失败！", "确定");
        }
    }
}