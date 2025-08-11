/*
 * @Author: tang.yan
 * @Description: UI区域外按钮点击(Click 按下+抬起)事件检测
 * @Date: 2023-11-29 16:11:44
 */
using System;
using EL;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FAT
{
    //UI区域外按钮点击(Click 按下+抬起)事件检测
    public class UIOutsideCheckClick : MonoBehaviour
    {
        [SerializeField] private Button btnSelf;
        public Action OnHideHandler;    //外部指定关闭时回调
        private int _showCount;

        private void Awake()
        {
            // 用于排除自身区域 | 只有点击自身区域之外才会触发关闭
            if (btnSelf != null)
            {
                btnSelf.onClick.AddListener(_OnBtnClick);
            }
        }

        private void OnEnable()
        {
            _showCount = 0;
        }

        private void Update()
        {
            if (_showCount < 0)
            {
                // callback 中处理UI隐藏逻辑
                OnHideHandler?.Invoke();
                return;
            }
            if (Input.touchSupported)
            {
                if (Input.touchCount > 0)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        var phase = Input.GetTouch(i).phase;
                        if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
                        {
                            _TryHide();
                            return;
                        }
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonUp(0))
                {
                    _TryHide();
                    return;
                }
            }
        }

        private void _TryHide()
        {
            --_showCount;
        }

        private void _OnBtnClick()
        {
            ++_showCount;
        }
    }
}