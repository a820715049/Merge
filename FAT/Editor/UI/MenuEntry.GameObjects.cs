// ===================================================
// Author: mengqc
// Date: 2025/09/09
// ===================================================

using UnityEditor;
using UnityEngine;

namespace FAT.Editor
{
    public static partial class MenuEntry
    {
        [MenuItem("GameObject/Copy Hierarchy Path")]
        public static void CopyHierarchyPath(){
            var obj = Selection.activeGameObject;
            if (obj == null) return;
            
            // 从选中节点向上遍历到根节点，收集所有名字
            var pathParts = new System.Collections.Generic.List<string>();
            var current = obj.transform;
            
            while (current != null)
            {
                // 跳过CanvasPreview节点
                if (current.name.Contains("CanvasPreview"))
                {
                    break;
                }
                pathParts.Add(current.name);
                current = current.parent;
            }
            
            // 反转列表，使路径从根到叶子
            pathParts.Reverse();
            
            // 移除根节点（第一个元素）
            if (pathParts.Count > 0)
            {
                pathParts.RemoveAt(0);
            }
            
            // 用"/"连接所有部分
            var hierarchyPath = string.Join("/", pathParts);
            
            // 复制到剪贴板
            EditorGUIUtility.systemCopyBuffer = hierarchyPath;
            
            Debug.Log($"Hierarchy path copied: {hierarchyPath}");
        }
    }
}