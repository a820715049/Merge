using System;
using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public class AnimationEvent : MonoBehaviour
    {
        #region 动画事件

        public static string AnimationStart = "Start";
        public static string AnimationEnd = "End";
        public static string AnimationTrigger = "Trigger";

        #endregion

        private readonly Dictionary<string, Action> _callbackDic = new();

        public void SetCallBack(string name, Action cb)
        {
            if (_callbackDic.ContainsKey(name))
            {
                return; 
            }
            _callbackDic.Add(name, cb);
        }

        public void Event(string name)
        {
            if (_callbackDic.TryGetValue(name, out var cb))
                cb.Invoke();
        }
    }
}