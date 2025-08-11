/*
 * @Author: tang.yan
 * @Description: Editor环境下 新建tmp text组件时 自动取消勾选 raycastTarget选项
 * @Date: 2025-07-04 10:07:54
 */

using UnityEditor;
using UnityEngine;
using TMPro;

namespace FAT.Editor
{
    [InitializeOnLoad]
    public static class TMPTextDisableRaycast
    {
        static TMPTextDisableRaycast()
        {
            ObjectFactory.componentWasAdded += OnComponentAdded;
        }

        private static void OnComponentAdded(Component component)
        {
            if (component is TMP_Text tmpText)
            {
                tmpText.raycastTarget = false;
                EditorUtility.SetDirty(tmpText);
            }
        }
    }
}
