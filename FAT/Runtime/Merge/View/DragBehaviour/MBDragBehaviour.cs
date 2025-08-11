// ================================================
// File: MBDragBehaviour.cs
// Author: yueran.li
// Date: 2025/04/29 10:20:56 星期二
// Desc: 自定义类型的 拖拽behaviour 注册transform
// ================================================


using UnityEngine;

namespace FAT
{
    public class MBDragBehaviour : MonoBehaviour
    {
        private RectTransform rect;

        private void Awake()
        {
            rect = transform.GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (rect == null)
            {
                return;
            }

            CustomDragBehaviourCtrl.RegisterOnDrag(rect);
        }

        private void OnDisable()
        {
            if (rect == null)
            {
                return;
            }

            CustomDragBehaviourCtrl.UnRegisterOnDrag(rect);
        }
    }
}