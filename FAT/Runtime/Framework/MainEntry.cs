/*
 * @Author: qun.chao
 * @Date: 2023-10-11 10:57:01
 */
using UnityEngine;
using CenturyGame.LoggerModule.Runtime;
using CenturyGame.Log4NetForUnity.Runtime;

namespace FAT
{
    public class MainEntry : MonoSingleton<MainEntry>
    {
        void Start()
        {
            // SDK
            LoggerManager.SetCurrentLoggerProvider(new Log4NetLoggerProvider());

            Game.Instance.Init(this);
        }

        void Update()
        {
            Game.Instance.Update(Time.deltaTime);
            EL.ThreadDispatcher.DefaultDispatcher.Execute();
#if UNITY_EDITOR
            DebugUtil.Update();
#endif
        }

        private void OnApplicationFocus(bool focusStatus)
        {

        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                Game.Instance.AppWillEnterBackground();
            }
            else
            {
                Game.Instance.AppDidEnterForeground();
            }
        }

        private void OnApplicationQuit()
        {
        #if UNITY_EDITOR
            //编辑器环境下 退出游戏时清理主字体fallback
            if (FontMaterialRes.Instance != null && FontMaterialRes.Instance.mainFontAsset != null)
                FontMaterialRes.Instance.mainFontAsset.fallbackFontAssetTable.Clear();
        #endif
        }
    }
}
