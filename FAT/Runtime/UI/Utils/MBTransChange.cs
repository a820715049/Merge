/*
 * @Author: qun.chao
 * @Date: 2024-11-20 10:36:10
 */
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FAT
{
    public class MBTransChange : UIBehaviour
    {
        public Action OnDimensionsChange;
        public Action<bool> OnVisiableChange;

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            OnDimensionsChange?.Invoke();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnVisiableChange?.Invoke(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnVisiableChange?.Invoke(false);
        }
    }
}