/*
 * @Author: qun.chao
 * @Date: 2023-12-06 19:19:34
 */
using System.Collections;
using UnityEngine;
using FAT.Platform;
using EL;
using Cysharp.Threading.Tasks;

namespace FAT
{
    public static partial class GameProcedure
    {
        public static void LaunchGame()
        {
            Game.Instance.StartCoroutineGlobal(_CoLaunchGame());
        }

        private static IEnumerator _CoLaunchGame()
        {
            Application.targetFrameRate = 60;
            ThreadDispatcher.InitForDefaultThread();

            // 动态加载loading背景图
            yield return MBGlobalLoading.Instance.loadingBg.TryLoadDynamicLoadingImage().ToCoroutine();
            MBGlobalLoading.Instance.WhenBgReady();

            DataTracker.InitUnityErrorLog();
            var sdkInit = PlatformSDK.Instance.Initialize();
            EL.Resource.ResManager.Initialize();
            GameObjectPoolManager.Instance.Initialize();
            Game.Instance.moduleMan.RigisteAllModules();
            Game.Instance.moduleMan.ResetAll(GameModuleManager.ModuleScope.AppLaunch);
            Game.Instance.moduleMan.StartupAll(GameModuleManager.ModuleScope.AppLaunch);
            Merge.Env.SetEnv(new Merge.GameMergeEnv());
            //加载游戏时刷新账号身份
            GameSwitchManager.Instance.Refresh();

            yield return EL.Resource.AssetBundleManager.Instance.CoPrepare();

            // res 多语言
            yield return GameI18NHelper.GetOrCreate().CoLoad(true);
            // res 全局
            yield return LoadingJob_CoLoadCommonRes();
            // res Sound&Music
            LoadSoundAndMusic();

            DebugEx.FormatTrace("[GameProcedure] app setting:{0}", JsonUtility.ToJson(Game.Instance.appSettings));
            //初始化adjustConfig
            AdjustTracker.SetConfig(Game.Instance.appSettings.trackingConfig?.config);

            DebugEx.Info($"[GameProcedure] sdk pre init");
            yield return sdkInit;
            DebugEx.Info($"[GameProcedure] sdk post init");
            DeviceNotificationHelper.CheckRespondedNotification();
            AdjustTracker.TrackEvent(AdjustEventType.LaunchApp);

            // 改为不在启动时加载热更
            // yield return AsyncTaskUtility.ExtractAsyncTaskFromCoroutine<AsyncTaskBase>(out var taks, Hotfix.HotfixManager.Instance.ATInitPatch());
            // DebugEx.Info($"[GameProcedure] hotfix on init");

            GameProcedure.AsyncEnterGame().Forget();
        }

        private static void LoadSoundAndMusic()
        {
            // res 音效
            Game.Manager.audioMan.InitWithConfig(CommonRes.Instance.soundConfig);
            
            // loading时取消播放bgm
            // Game.Manager.audioMan.PlayDefaultBgm();

            // // res 背景音乐
            // var isMute = PlayerPrefs.GetInt(SettingManager.GameMuteMusicKeyName, 0) == 1;
            // MusicManager.Instance.SetMainVolume(isMute ? 0f : 1f);
            // MusicManager.Instance.SetMusic(0, "common_audio", "fat_bgm.ogg");
        }

        private static IEnumerator LoadingJob_CoLoadCommonRes()
        {
            var firstLoadAssets = EL.Resource.ResManager.LoadAsset<CommonRes>(Constant.kCommonRes.Group, Constant.kCommonRes.Asset);
            yield return firstLoadAssets;
            if (!firstLoadAssets.isSuccess)
            {
                DebugEx.FormatError("Game::CoPrepareGame ----> load first assets failed, {0}", firstLoadAssets.error);
            }
            CommonRes.Instance = firstLoadAssets.asset as CommonRes;
        }
    }
}