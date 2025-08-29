// ================================================
// File: MBGuideTarget.cs
// Author: yueran.li
// Date: 2025/04/25 18:59:59 星期五
// Desc: 棋盘手指指引目标
// ================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    /// <summary>
    /// 自动引导目标组件，用于定义可以被引导手指指向的游戏对象
    /// </summary>
    public class MBGuideTarget : MonoBehaviour
    {
        // 唯一标识此引导目标的键名
        public string key;
        
        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(key))
            {
                MBAutoGuideController.RegisterGuideTarget(key, this);
            }
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(key))
            {
                MBAutoGuideController.UnRegisterGuideTarget(key);
            }
        }
    }
}