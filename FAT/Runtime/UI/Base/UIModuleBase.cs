/*
 * @Author: tang.yan
 * @Description: 用于UI模块化父子结构的组织 
 * @Date: 2023-10-12 10:10:53
 */
using UnityEngine;
using System.Collections.Generic;

namespace FAT
{
    public abstract class UIModuleBase
    {
        public Transform ModuleRoot { get; }
        public bool IsModuleActive => _isActive;
        
        protected UIModuleBase(Transform root)
        {
            ModuleRoot = root;
        }

        private bool _isActive;
        private List<UIModuleBase> _childrenList = new List<UIModuleBase>();

        //首次获得资源时 根据资源创建需要的逻辑对象
        protected abstract void OnCreate();
        //每次获取到外部数据时
        protected abstract void OnParse(params object[] items);
        //每次module显示时
        protected abstract void OnShow();
        //每次module隐藏时
        protected abstract void OnHide();
        //添加常驻事件
        protected abstract void OnAddListener();
        //移除常驻事件
        protected abstract void OnRemoveListener();
        //添加动态事件
        protected abstract void OnAddDynamicListener();
        //移除动态事件
        protected abstract void OnRemoveDynamicListener();
        //当界面关闭时
        protected abstract void OnClose();
        
        //在module中再添加其他module
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
        
        //外部按需主动调用显示
        public void Show(params object[] items)
        {
            _ModuleActiveChange(true);
            _Parse(items);
            OnShow();
        }

        //外部按需主动调用隐藏
        public void Hide()
        {
            _ModuleActiveChange(false);
            OnHide();
        }

        //只在module创建时内部调用一次
        public void Create()
        {
            _isActive = ModuleRoot.gameObject.activeSelf;
            OnCreate();
        }

        //只在界面打开时内部调用一次
        public void Open()
        {
            OnAddListener();
            _DynamicListenerChange();
            //打开进入时先处理父再处理子
            foreach (var module in _childrenList)
            {
                module.Open();
            }
        }

        //只在界面关闭时内部调用一次
        public void Close()
        {
            //关闭退出时先处理子再处理父(避免父类中提前对子类操作 导致子类生命周期走不完全)
            foreach (var module in _childrenList)
            {
                module.Close();
            }
            OnRemoveListener();
            OnClose();
        }

        private void _Parse(params object[] items)
        {
            if (items.Length <= 0)
                return;
            OnParse(items);
        }

        private void _ModuleActiveChange(bool isActive)
        {
            //避免重复设置
            if (_isActive == isActive)
                return;
            _isActive = isActive;
            _DynamicListenerChange();
            ModuleRoot.gameObject.SetActive(_isActive);
        }

        private void _DynamicListenerChange()
        {
            if (_isActive)
                OnAddDynamicListener();
            else
                OnRemoveDynamicListener();
        }
    }
}