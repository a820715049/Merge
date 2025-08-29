// ================================================
// File: MBAutoGuideController.cs
// Author: yueran.li
// Date: 2025/04/25 18:59:20 星期五
// Desc: 棋盘手指指引
// ================================================

using System.Collections.Generic;
using DG.Tweening;
using EL;
using EL.Resource;
using FAT.MSG;
using UnityEngine;

namespace FAT
{
    /// <summary>
    /// 通用自动引导指示器，管理多个引导目标的显示和隐藏
    /// </summary>
    public class MBAutoGuideController : MonoBehaviour
    {
        // 自动引导触发间隔（秒）
        public int Interval = 10;

        // 计时器
        private int _curInterval;

        // 当前是否正在显示引导
        private bool _isPlaying;

        // 引导目标字典
        private static Dictionary<string, MBGuideTarget> _targetDict = new Dictionary<string, MBGuideTarget>();

        private Dictionary<string, GameObject> fingers = new();

        // 自动引导实现接口
        private IAutoGuide _autoGuide;

        /// <summary>
        /// 初始化并注册事件
        /// </summary>
        public void SetUp(IAutoGuide autoGuide)
        {
            _autoGuide = autoGuide;
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(SecondUpdate);
            if (!GameObjectPoolManager.Instance.HasPool(PoolItemType.Auto_Guide_Finger.ToString()))
            {
                var req = ResManager.LoadAsset("fat_global", "AutoGuideFinger.prefab");
                var go = Object.Instantiate(req.asset,
                    UIManager.Instance.GetLayerRootByType(UILayer.Effect)) as GameObject;
                GameObjectPoolManager.Instance.PreparePool(PoolItemType.Auto_Guide_Finger, go);
            }
        }

        /// <summary>
        /// 释放资源并取消注册事件
        /// </summary>
        public void Release()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(SecondUpdate);

            // 隐藏所有手指
            for (int i = fingers.Count - 1; i >= 0; i--)
            {
                var key = fingers.Keys.ToList()[i];
                HideFinger(key);
            }

            _targetDict.Clear();
            fingers.Clear();

            _autoGuide = null;
        }

        /// <summary>
        /// 每秒更新，检查是否需要显示引导
        /// </summary>
        private void SecondUpdate()
        {
            // 如果正在显示引导或有顶层界面，则不处理
            if (_isPlaying || UIManager.Instance.IsShow(UIConfig.UIGuide))
            {
                return;
            }

            // 增加计时
            _curInterval++;

            // 达到间隔时间时刷新引导
            if (_curInterval >= Interval)
            {
                // 重置计时器
                _curInterval = 0;
                _autoGuide?.CheckShowRefresh();
            }
        }

        /// <summary>
        /// 中断当前引导
        /// </summary>
        public void Interrupt()
        {
            _curInterval = 0;

            // 隐藏所有手指
            for (int i = fingers.Count - 1; i >= 0; i--)
            {
                var key = fingers.Keys.ToList()[i];
                HideFinger(key);
            }

            fingers.Clear();

            _isPlaying = false;
        }

        /// <summary>
        /// 显示指定键名的引导手指
        /// </summary>
        public void ShowFinger(string key)
        {
            if (UIManager.Instance.IsOpen(UIConfig.UIGuide))
            {
                return;
            }

            if (_targetDict.TryGetValue(key, out var target))
            {
                _isPlaying = true;

                if (!fingers.TryGetValue(key, out var finger))
                {
                    finger = GameObjectPoolManager.Instance.CreateObject(PoolItemType.Auto_Guide_Finger,
                        target.transform);

                    fingers[key] = finger;
                }

                finger.SetActive(true);
                finger.transform.localPosition = Vector3.zero;
            }
            else
            {
                Debug.LogWarning($"Guide target with key '{key}' not found.");
            }
        }

        private Sequence moveSeq;

        public void ShowFingerMove(string key, Vector3 from, bool loop = false)
        {
            if (UIManager.Instance.IsOpen(UIConfig.UIGuide))
            {
                return;
            }

            moveSeq?.Kill();

            if (_targetDict.TryGetValue(key, out var target))
            {
                _isPlaying = true;

                if (!fingers.TryGetValue(key, out var finger))
                {
                    finger = GameObjectPoolManager.Instance.CreateObject(PoolItemType.Auto_Guide_Finger, transform);
                    fingers[key] = finger;
                }

                finger.SetActive(true);
                finger.transform.position = from;

                moveSeq = DOTween.Sequence();
                if (loop)
                {
                    moveSeq.SetLoops(-1, LoopType.Restart);
                }

                moveSeq.Append(finger.transform.DOMove(target.transform.position, 1f));
                moveSeq.OnKill(() => { HideFinger(key); });
            }
            else
            {
                Debug.LogWarning($"Guide target with key '{key}' not found.");
            }
        }


        /// <summary>
        /// 隐藏指定键名的引导手指
        /// </summary>
        private void HideFinger(string key)
        {
            if (fingers.ContainsKey(key) && fingers[key] != null)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.Auto_Guide_Finger, fingers[key]);
                fingers.Remove(key);
            }

            _isPlaying = false;
        }


        /// <summary>
        /// 静态引导目标注册方法，通过查找自动引导管理器实例
        /// </summary>
        public static void RegisterGuideTarget(string key, MBGuideTarget target)
        {
            var autoGuide = FindObjectOfType<MBAutoGuideController>();
            if (autoGuide != null)
            {
                if (!string.IsNullOrEmpty(key) && target != null)
                {
                    _targetDict[key] = target;
                }
            }
            else
            {
                Debug.LogWarning("No AutoGuideFinger instance found in scene.");
            }
        }

        /// <summary>
        /// 静态引导目标取消注册方法，通过查找自动引导管理器实例
        /// </summary>
        public static void UnRegisterGuideTarget(string key)
        {
            var autoGuide = FindObjectOfType<MBAutoGuideController>();
            if (autoGuide != null)
            {
                if (!string.IsNullOrEmpty(key) && _targetDict.ContainsKey(key))
                {
                    _targetDict.Remove(key);
                }
            }
        }
    }

    /// <summary>
    /// 自动引导接口，所有需要实现自动引导的类都应该实现此接口
    /// </summary>
    public interface IAutoGuide
    {
        /// <summary>
        /// 刷新引导状态，决定是否需要显示引导
        /// </summary>
        void CheckShowRefresh();
    }
}