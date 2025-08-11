/*
*@Author:chaoran.zhang
*@Desc:类似flytarget的用于定位引导手指的脚本
*@Created Time:2024.05.07 星期二 18:26:03
*/

using System;
using fat.rawdata;
using UnityEngine;

namespace FAT
{
    public class MBAutoGuideFinger : MonoBehaviour
    {
        public AutoFinger Type;
        
        private void OnEnable()
        {
            Game.Manager.autoGuide.RegisterAutoFingerPos(Type,GetTrans);
        }

        private void OnDisable()
        {
            Game.Manager.autoGuide.UnRegisterAutoFingerPos(Type,GetTrans);
        }

        private Transform GetTrans()
        {
            return transform;
        }

        private void OnBecameVisible()
        {
            
        }

        private void OnBecameInvisible()
        {
            
        }
    }
}