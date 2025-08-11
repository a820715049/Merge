/*
 * @Author: qun.chao
 * @Date: 2023-10-12 10:48:54
 */
using UnityEngine;
using System.Collections.Generic;
using EL;
using System;
using DG.Tweening;
using UnityEngine.UI;

namespace FAT
{
    public class UIBase : MonoBehaviour
    {
        //UI所属层级
        public UILayer BelongLayer { get; set; }
        //UI对应的配置
        public UIResource ResConfig { get; set; }

        //界面根节点rectTrans
        private RectTransform _rootRect;
        //界面prefab根节点上可以挂载的Animator
        protected Animator RootAnimator;
        //当前界面正处于的状态
        private UIWindowState _uiState = UIWindowState.None;
        //UI下的子模块
        private List<UIModuleBase> _childrenList = new List<UIModuleBase>();
        //通过改变界面位置来显示/隐藏
        private static readonly Vector2 ResumeAnchorPos = new Vector2(0, 0);
        private static readonly Vector2 PauseAnchorPos = new Vector2(9999, 9999);
        //是否自动销毁
        private bool _isAutoDestroy = false;
        //用于Visible时控制界面显隐
        private CanvasGroup _canvasGroup;
        private GameObject _lockEventObject;
        //首次获得资源时
        protected virtual void OnCreate() { }

        //给界面传递数据时 在此方法中进行解析
        protected virtual void OnParse(params object[] items) { }

        //界面准备打开时(播放打开动画开始前)
        protected virtual void OnPreOpen() { }

        //界面开启时添加事件监听
        protected virtual void OnAddListener() { }

        //界面打开完毕时(播放打开动画结束后)
        protected virtual void OnPostOpen() { }

        //界面刷新时(因数据变化导致的刷新)
        protected virtual void OnRefresh() { }

        //界面直接控制GameObject显隐时
        protected virtual void OnVisible(bool isVisible) { }

        //界面准备关闭时
        protected virtual void OnPreClose() { }

        //界面关闭时移除事件监听
        protected virtual void OnRemoveListener() { }

        //界面完全关闭后
        protected virtual void OnPostClose() { }

        //界面被隐藏暂停时
        protected virtual void OnPause() { }

        //界面重新恢复显示时
        protected virtual void OnResume() { }

        public bool IsOpen()
        {
            return _uiState.CompareTo(UIWindowState.Opening) >= 0
                   && _uiState.CompareTo(UIWindowState.Closing) < 0;
        }

        public bool IsOpening()
        {
            return _uiState.CompareTo(UIWindowState.Opening) == 0;
        }

        public bool IsShow()
        {
            return IsOpen() && !IsPause();
        }

        public bool IsPause()
        {
            return _uiState.CompareTo(UIWindowState.Pause) == 0;
        }

        public bool IsClosed()
        {
            return _uiState.CompareTo(UIWindowState.Close) == 0;
        }

        public bool CheckIsState(UIWindowState state)
        {
            return _uiState.CompareTo(state) == 0;
        }

        public void OnLoaded()
        {
            _Prepare();
            _SetUIState(UIWindowState.Ready);
            OnCreate();
        }

        //一般在OnCreate中主动调用
        protected T AddModule<T>(T module) where T : UIModuleBase
        {
            if (_childrenList.Contains(module))
                return null;
            _childrenList.Add(module);
            module.Create();
            return module;
        }

        public void ClearModule()
        {
            foreach (var module in _childrenList)
            {
                module.Close();
            }
            _childrenList.Clear();
        }

        public void PreOpen(params object[] items)
        {
            _SetUIState(UIWindowState.Opening);
            OnParse(items);
            OnPreOpen();
            OnAddListener();
            //打开进入时先处理父再处理子
            foreach (var module in _childrenList)
            {
                module.Open();
            }
        }

        public void PostOpen()
        {
            //界面打开过程中(Opening),如果被其他新界面顶掉并处于Pause状态了 则不设置Open状态
            if (!IsPause())
                _SetUIState(UIWindowState.Open);
            OnPostOpen();
        }

        public void Refresh(params object[] items)
        {
            OnParse(items);
            OnRefresh();
        }

        public void PreClose()
        {
            _SetUIState(UIWindowState.Closing);
            OnPreClose();
        }

        public void PostClose()
        {
            _SetUIState(UIWindowState.Close);
            //关闭退出时先处理子再处理父
            foreach (var module in _childrenList)
            {
                module.Close();
            }
            _ClearAnimCb();
            OnRemoveListener();
            OnPostClose();
        }

        public void Pause()
        {
            _SetUIState(UIWindowState.Pause);
            _rootRect.anchoredPosition = PauseAnchorPos;
            OnPause();
        }

        public void Resume()
        {
            _SetUIState(UIWindowState.Open);
            _rootRect.anchoredPosition = ResumeAnchorPos;
            OnResume();
        }

        public void Close()
        {
            if (_isAutoDestroy)
            {
                PreClose();
                PostClose();
                _isAutoDestroy = false;
                GameObject.Destroy(gameObject);
            }
            else
            {
                UIManager.Instance.CloseWindow(ResConfig);
                UnlockEvent();
            }
        }

        public void MarkAutoDestroy(bool isAuto)
        {
            _isAutoDestroy = isAuto;
        }

        //Visible暂不记入UIWindowState中，这里先借助_canvasGroup判断一下
        public bool IsVisible()
        {
            //认为只有界面在显示时visible才会有意义
            if (!IsShow())
                return false;
            //如果界面正在显示 但_canvasGroup为空 此时也返回true
            if (_canvasGroup == null)
                return true;
            //_canvasGroup不为空时 其alpha只要大于0就认为是true
            return _canvasGroup.alpha > 0;
        }

        public void Visible(bool v_)
        {
            //认为只有界面在显示时visible才会有意义
            if (!IsShow()) return;
            //Visible时播放对应动画
            if (v_)
                PlayOpenAnim();
            else
                PlayCloseAnim();
            //没有_canvasGroup时尝试获取并添加
            if (_canvasGroup == null)
            {
                if (!gameObject.TryGetComponent(out _canvasGroup))
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            _canvasGroup.alpha = v_ ? 1 : 0;
            _canvasGroup.blocksRaycasts = v_;
            //执行完表现后走通用接口
            OnVisible(v_);
        }

        private void _Prepare()
        {
            _rootRect = transform as RectTransform;
            RootAnimator = transform.GetComponent<Animator>();
        }

        private void _SetUIState(UIWindowState state)
        {
            DebugEx.FormatInfo("UI State Change [{0}] : before = {1}, after = {2}", this.name, _uiState, state);
            _uiState = state;
        }

        #region UI级别的事件阻挡方法
        /// <summary>
        /// 开启事件阻挡
        /// </summary>
        protected void LockEvent()
        {
            if (_lockEventObject == null)
            {
                // 直接创建事件阻挡器
                _lockEventObject = new GameObject("EventBlocker");
                _lockEventObject.transform.SetParent(transform, false);

                // 添加必要的组件
                RectTransform blockerRect = _lockEventObject.AddComponent<RectTransform>();
                // 设置阻挡器覆盖整个UI区域
                blockerRect.anchorMin = Vector2.zero;
                blockerRect.anchorMax = Vector2.one;
                blockerRect.offsetMin = Vector2.zero;
                blockerRect.offsetMax = Vector2.zero;

                _lockEventObject.AddComponent<UnityEngine.UI.Extensions.NonDrawingGraphic>();

                // 确保阻挡器在最上层
                blockerRect.SetAsLastSibling();
            }

            _lockEventObject.SetActive(true);
        }

        /// <summary>
        /// 关闭事件阻挡，在UI关闭的时候会自动调用这个方法
        /// </summary>
        protected void UnlockEvent()
        {
            if (_lockEventObject != null)
            {
                _lockEventObject.SetActive(false);
            }
        }
        #endregion

        #region Animator动画相关

        //缓存动画播完回调方法
        private Action _cacheOpenAnimCb = null;
        private Action _cacheCloseAnimCb = null;
        private Action _cachePauseAnimCb = null;
        private Action _cacheResumeAnimCb = null;

        public bool CheckAnimValid()
        {
            //使用parameterCount保证Animator合法
            //如果parameterCount至少为4个 则底层代码可以保证界面可以自动播放开启关闭动画
            //如果少于4个 说明界面有特殊的定制动画 则需要具体界面相关代码手动控制播放动画
            return RootAnimator != null && RootAnimator.runtimeAnimatorController != null && RootAnimator.parameterCount >= 4;
        }

        public void PlayOpenAnim(Action finishCb = null)
        {
            _cacheOpenAnimCb = finishCb;
            if (RootAnimator != null)
            {
                RootAnimator.SetTrigger(UIManager.OpenAnimTrigger);
            }
            else
            {
                OnPlayOpenAnimFinish();
            }
        }

        public void PlayCloseAnim(Action finishCb = null)
        {
            _cacheCloseAnimCb = finishCb;
            if (RootAnimator != null)
            {
                RootAnimator.SetTrigger(UIManager.CloseAnimTrigger);
            }
            else
            {
                OnPlayCloseAnimFinish();
            }
        }

        public void PlayPauseAnim(Action finishCb = null)
        {
            _cachePauseAnimCb = finishCb;
            if (RootAnimator != null && RootAnimator.parameterCount >= 4)   //使用parameterCount保证Animator合法
            {
                RootAnimator.SetTrigger(UIManager.PauseAnimTrigger);
            }
            else
            {
                OnPlayPauseAnimFinish();
            }
        }

        public void PlayResumeAnim(Action finishCb = null)
        {
            _cacheResumeAnimCb = finishCb;
            if (RootAnimator != null && RootAnimator.parameterCount >= 4)   //使用parameterCount保证Animator合法
            {
                RootAnimator.SetTrigger(UIManager.ResumeAnimTrigger);
            }
            else
            {
                OnPlayResumeAnimFinish();
            }
        }

        public void OnPlayOpenAnimFinish()
        {
            _cacheOpenAnimCb?.Invoke();
            _cacheOpenAnimCb = null;
        }

        public void OnPlayCloseAnimFinish()
        {
            _cacheCloseAnimCb?.Invoke();
            _cacheCloseAnimCb = null;
        }

        public void OnPlayPauseAnimFinish()
        {
            _cachePauseAnimCb?.Invoke();
            _cachePauseAnimCb = null;
        }

        public void OnPlayResumeAnimFinish()
        {
            _cacheResumeAnimCb?.Invoke();
            _cacheResumeAnimCb = null;
        }

        private void _ClearAnimCb()
        {
            _cacheOpenAnimCb = null;
            _cacheCloseAnimCb = null;
            _cachePauseAnimCb = null;
            _cacheResumeAnimCb = null;
        }

        #endregion
    }
}
