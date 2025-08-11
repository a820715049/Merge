/*
 * @Author: tang.yan
 * @Description: 广告防卡死界面 
 * @Date: 2023-12-06 10:12:21
 */
using System;
using EL;

namespace FAT
{
    public class UIAdsWaitResolve : UIBase
    {
        private AsyncTaskBase _waitTask;
        private Action _closeCb;

        protected override void OnCreate()
        {
            transform.AddButton("Mask", _OnBtnResolve);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 1)
            {
                _waitTask = items[0] as AsyncTaskBase;
                _closeCb = items[1] as Action;
            }
        }

        private void Update()
        {
            if (_waitTask == null || !IsOpen()) return;
            if (!_waitTask.keepWaiting)
            {
                _waitTask = null;
                Close();
            }
        }

        private void _OnBtnResolve()
        {
            _closeCb?.Invoke();
            _closeCb = null;
        }
    }
}