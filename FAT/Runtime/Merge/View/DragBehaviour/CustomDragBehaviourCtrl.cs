// ================================================
// File: CustomDragBehaviourCtrl.cs
// Author: yueran.li
// Date: 2025/04/29 10:20:03 星期二
// Desc: 自定义类型 的 拖拽逻辑判断脚本
// ================================================


using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public static class CustomDragBehaviourCtrl
    {
        private static readonly List<RectTransform> TransformContainer = new();

        public static void RegisterOnDrag(RectTransform rect)
        {
            if (TransformContainer.Contains(rect))
            {
                return;
            }

            TransformContainer.Add(rect);
        }

        public static void UnRegisterOnDrag(RectTransform rect)
        {
            if (TransformContainer.Contains(rect))
            {
                TransformContainer.Remove(rect);
            }
        }


        public static bool CheckDragBehaviour(Vector2 screenPos)
        {
            foreach (var t in TransformContainer)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(t, screenPos))
                {
                    return true;
                }
            }

            return false;
        }
    }
}