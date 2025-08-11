/*
 * @Author: tang.yan
 * @Description: UI区域外按钮按下(Down)事件检测
 * @Date: 2023-11-29 16:11:25
 */
using System;
using EL;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FAT
{
    //UI区域外按钮按下(Down)事件检测
    public class UIOutsideCheckDown : MonoBehaviour
    {
        [SerializeField] private UIEventTrigger touchSelf;
        [SerializeField] private bool hasChildButton;       //面板上是否有子节点也含有按钮
        public Action OnHideHandler;
        private int _showCount;
        private bool _isTouchUp = true;
        private bool _isTouchDown = false;

        private void Awake()
        {
            // 用于排除自身区域 | 只有点击自身区域之外才会触发关闭
            if (touchSelf != null)
            {
                touchSelf.onDown = OnTouchDown;
            }
        }

        private void OnEnable()
        {
            _showCount = 0;
            _isTouchUp = true;
            _isTouchDown = false;
        }

        private void Update()
        {
            if (_showCount < 0)
            {
                // callback 中处理UI隐藏逻辑
                OnHideHandler?.Invoke();
                _showCount = 0;
                return;
            }
            if (Input.touchSupported)
            {
                if (Input.touchCount > 0)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        var phase = Input.GetTouch(i).phase;
                        if (phase == TouchPhase.Began)
                        {
                            if (_CheckTouchChildButton())
                            {
                                return;
                            }
                            _TryHide();
                            return;
                        }
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0) && _isTouchUp)
                {
                    _isTouchUp = false;
                    _isTouchDown = true;
                    if (_CheckTouchChildButton())
                    {
                        return;
                    }
                    _TryHide();
                    return;
                }
                if (Input.GetMouseButtonUp(0) && _isTouchDown)
                {
                    _isTouchUp = true;
                    _isTouchDown = false;
                }
            }
        }

        //检测是否触摸到了子节点中的按钮
        private bool _CheckTouchChildButton()
        {
            if (hasChildButton)
            {
                //用于解决tips面板上还有其他按钮的情况
                var curObj = EventSystem.current.currentSelectedGameObject;
                if (curObj != null && touchSelf != null && curObj.transform.IsChildOf(touchSelf.transform))
                {
                    return true;
                }
            }
            return false;
        }

        private void _TryHide()
        {
            --_showCount;
        }

        private void OnTouchDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.pointerEnter != null && touchSelf != null)
            {
                var obj = eventData.pointerEnter;
                if (obj.name == touchSelf.gameObject.name || obj.transform.IsChildOf(touchSelf.transform))
                {
                    ++_showCount;
                }
            }
        }
    }
}