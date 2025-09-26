/*
 * @Author: qun.chao
 * @Date: 2023-12-06 19:19:51
 */
using System;
using System.Collections;
using UnityEngine;
using EL;
using EL.Resource;
using CenturyGame.AppUpdaterLib.Runtime.Managers;
using FAT.Platform;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FAT
{
    public static partial class GameProcedure
    {
        public static async UniTaskVoid AsyncEnterGame(Action afterFadeIn = null, Action afterFadeOut = null)
        {
            try
            {
                var token = tokenForLoading;
                await AsyncStartGame(afterFadeIn, afterFadeOut, token).AttachExternalCancellation(token);
            }
            catch (OperationCanceledException)
            {
                DebugEx.Info("[GameProcedure] canceled");
            }
        }

        private static async UniTask AsyncStartGame(Action afterFadeIn, Action afterFadeOut, CancellationToken token)
        {
            IsInGame = false;
            Game.Instance.SetNotRunning();

            MBGlobalLoading.Instance.InstallLoadingImp();
            MBGlobalLoading.Instance.ShowDefault();
            DataTracker.TrackLoading(LoadingPhase.BeginLoading);

            DebugEx.Info("[GameProcedure] AsyncStartGame ---> begin");

            var curLoading = MBGlobalLoading.Instance as IGameLoading;
            var waitLoadingFinished = false;
            var waitFadeInDone = false;
            void afterFadeInWrapper() { waitFadeInDone = true; afterFadeIn?.Invoke(); }

            // 通知splash屏可以撤下
            MessageCenter.Get<MSG.UI_SPLASH_SCREEN_STATE>().Dispatch(false);
            curLoading?.SetProgress(0.05f);

            var task_loading = ALoadingImp(MBGlobalLoading.Instance, () => waitLoadingFinished, token, null, afterFadeInWrapper, null, afterFadeOut);
            await UniTask.WaitUntil(() => waitFadeInDone).AttachExternalCancellation(token);
            curLoading?.SetProgress(0.2f);

            DebugEx.Info("[GameProcedure] AsyncStartGame ---> pre reset");

            // 兼容restart流程
            Game.Instance.moduleMan.ResetAll(GameModuleManager.ModuleScope.AppLaunch, true);
            Game.Instance.moduleMan.StartupAll(GameModuleManager.ModuleScope.GameStart);

            // Game.Manager.networkMan.CheckNetworkWeakInLoading(true);   //等待SDK时 执行弱网检查
            var sdk = PlatformSDK.Instance;
            DataTracker.TrackLoading(LoadingPhase.PreUpdate);
            DataTracker.TrackLoading(LoadingPhase.PreSDK);
            var task_update = _CoAppUpdate(curLoading, 0.2f, 0.5f).ToUniTask();
            var task_sdk = sdk.Login().ToUniTask();
            await UniTask.WhenAll(task_update, task_sdk).AttachExternalCancellation(token);
            curLoading?.SetProgress(0.6f);

            // 此处有可能跳过了真实login 这里主动触发一次删号状态刷新
            PlatformSDK.Instance.Adapter.RequestAccountRemovalStatus();

            // ======= loading perf =======
            // 临时做法 loading先尽量加载bundle
            ResManager.LoadGroup("fat_global");
            ResManager.LoadGroup("fat_global_ext");
            // ======= loading perf =======

            // Game.Manager.networkMan.CheckNetworkWeakInLoading(false);    //sdk逻辑结束时 退出弱网检查

            DataTracker.TrackLoading(LoadingPhase.PostSDK);
            DataTracker.TrackLoading(LoadingPhase.PostUpdate);

            // 收集效果管理器
            UIFlyManager.Instance.Init();

            DebugEx.Info("[GameProcedure] AsyncStartGame ---> pre conf");
            curLoading?.SetProgress(0.7f, 0.9f, Game.Manager.configMan.LoadingProgress);
            await Game.Manager.configMan.CoLoadAll().ToUniTask().AttachExternalCancellation(token);
            if (!Game.Manager.configMan.IsAllConfigReady)
            {
                var errType = CenturyGame.AppUpdaterLib.Runtime.AppUpdaterErrorType.DownloadFileFailure;
                var code = ErrorCodeUtility.ConvertToCommonCode((int)errType, ErrorCodeType.UpdateError);
                var content = ErrorCodeUtility.GetNoticeContentByErrorCodeType(code);
                Game.Instance.Abort(content, code, isShowErrorCode: false);
                throw new OperationCanceledException("conf not ready");
            }
            curLoading?.SetProgress(0.9f);
            DebugEx.Info("[GameProcedure] AsyncStartGame ---> post conf");

            DataTracker.TrackLoading(LoadingPhase.PreLoadArchive);
            Action OnLoadArchiveFinish = () =>
            {
                DebugEx.Info("[GameProcedure] AsyncStartGame ---> OnLoadArchiveFinish");
                Game.Instance.moduleMan.LoadConfigAll(GameModuleManager.ModuleScope.AppLaunch, true);
                Game.Instance.moduleMan.StartupAll(GameModuleManager.ModuleScope.ConfReady);
            };
            //这里将各个Manager可以真正读取配置的时机延迟到获得存档数据之后，目的是为了让AB测试功能可以应用到配置数据上，从而确保各功能读取到的配置是准确的。
            //但这个功能打破了之前默认的先初始化配置数据，而后才加载并应用存档数据的执行顺序，将初始化配置的时机挪到了加载存档数据成功之后，应用存档数据之前。
            //这里设置一下存档加载成功回调，会在真正获取到存档时执行
            Game.Manager.archiveMan.SetLoadArchiveFinishCb(OnLoadArchiveFinish);
            // 加载本地存档 or Login后获得服务器存档
            await _CoLoadArchive().ToUniTask().AttachExternalCancellation(token);
            DataTracker.TrackLoading(LoadingPhase.PostLoadArchive);

            curLoading?.SetProgress(0.99f);
            // 存档载入后 刷新账号身份
            GameSwitchManager.Instance.Refresh();
            // 存档载入后 初始化设置信息
            SettingManager.Instance.Initialize();
            // 根据当前能量状况 调整用户bet
            SettingManager.Instance.OnLoginRefreshEnergyBoostState();
            // 初始化震动模块
            VibrationManager.Init();

            Game.Instance.moduleMan.StartupAll(GameModuleManager.ModuleScope.ArchiveLoaded);

            UIManager.Instance.OpenWindow(UIConfig.UIMergeBoardMain);
            UIManager.Instance.OpenWindow(UIConfig.UIDEReward);
            UIManager.Instance.OpenWindow(UIConfig.UIStatus);
            // 检查生成器丢失 打点
            Game.Manager.mainMergeMan.CheckMissingItem();
            await UniTask.WaitUntil(() => UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain) && UIManager.Instance.IsOpen(UIConfig.UIStatus)).
                AttachExternalCancellation(token);

            // ======= loading perf =======
            // 临时做法 loading先尽量加载bundle
            await UniTask.WaitUntil(() => ResManager.IsGroupLoaded("fat_global_ext") && ResManager.IsGroupLoaded("fat_global"));
            // ======= loading perf =======

            // 登录后会自动接收到账号的删除状态回调 | 在登录被跳过时也会由之前流程里的主动查询触发回调
            await WaitRemoveAccount().AttachExternalCancellation(token);

            if (!Game.Instance.isRunning)
            {
                return;
            }

            //进入棋盘时尝试升级 避免各种原因中断升级流程时导致的升级失败
            Game.Manager.mergeLevelMan.CheckLevelup();
            curLoading?.SetProgress(1f);

            DataTracker.TrackLoading(LoadingPhase.EndLoading);

            IsInGame = true;
            //当进入游戏时尝试触发弹脸逻辑
            Game.Manager.loginSignMan.TrySignIn();
            Game.Manager.screenPopup.WhenEnterGame();
            //尝试触发引导
            GuideUtility.TriggerGuide();
            //进游戏时尝试拉取一下卡片收取箱的内容
            Game.Manager.cardMan.TryPullPendingCardInfo();
            waitLoadingFinished = true;
            // await task_loading.AttachExternalCancellation(token);

            // 进游戏时尝试准备loading图
            MBGlobalLoading.Instance.loadingBg.TryPrepareLoadingImage();
            //进游戏时尝试弹出在之前session中没有来的及解析的特殊奖励（如玩家杀端）
            Game.Manager.specialRewardMan.TryPumpWhenLogin();

            DataTracker.game_start.Track();
            DataTracker.board_info.Track(Game.Manager.mainMergeMan.world.activeBoard);
            AdjustTracker.TrackEvent(AdjustEventType.GameStart);
            DebugEx.Info("[GameProcedure] AsyncStartGame ---> finish");
        }

        private static async UniTask WaitRemoveAccount()
        {
            try
            {
                await UniTask.WaitWhile(() => PlatformSDK.Instance.Adapter.AccountRemoveStatus == centurygame.CGAccountRemoveGeneralStatus.CGAccountUnkown).
                    AttachExternalCancellation(timeoutController.Timeout(TimeSpan.FromSeconds(5f)));
                timeoutController.Reset();
            }
            catch (OperationCanceledException)
            {
                if (timeoutController.IsTimeout())
                {
                    // 删号状态检查超时
                    // 不再阻拦用户进游戏 留下log即可
                    DataTracker.TrackLogInfo("[GameProcedure] WaitRemoveAccount ---> wait callback timeout");
                    return;
                }
            }

            var isWaitRemoveAccount = true;
            Action PassWaitRemoveAccount = () =>
            {
                DebugEx.FormatInfo("[GameProcedure] AsyncStartGame ---> CheckAccountRemoveStatus finish, status = {0}", PlatformSDK.Instance.Adapter.AccountRemoveStatus);
                isWaitRemoveAccount = false;
            };
            //检查账号状态
            AccountDelectionUtility.CheckAccountRemoveStatus(PassWaitRemoveAccount);
            //截断登录流程 直到isWaitRemoveAccount为false
            await UniTask.WaitWhile(() => isWaitRemoveAccount);
        }

        private static IEnumerator _CoLoadArchive()
        {
            // 简化流程 尽量使用服务器存档
            DataTracker.TrackLoading(LoadingPhase.PreLogin);
            var loginTask = Game.Instance.StartAsyncTaskGlobal(Game.Manager.networkMan.ATLogin());
            yield return loginTask;
            DataTracker.TrackLoading(LoadingPhase.PostLogin);
            if (!loginTask.isSuccess)
            {
                DebugEx.FormatWarning("GameProcedure::_CoLoadArchive ----> login fail: {0}", loginTask.error);
                yield break;
            }
            if (!Game.Manager.networkMan.isInSync)
            {
                DebugEx.FormatWarning("GameProcedure::_CoLoadArchive ----> login fail: not sync");
                yield break;
            }
            Game.Instance.SetRunning();
        }

        private static IEnumerator _CoAppUpdate(IGameLoading loading, float from, float to)
        {
            AppUpdaterManager.AppUpdaterHint(CenturyGame.AppUpdaterLib.Runtime.AppUpdaterHintName.VMS_MANIFEST_FORMAT, 0);
#if UNITY_EDITOR
            AppUpdaterManager.AppUpdaterHint(CenturyGame.AppUpdaterLib.Runtime.AppUpdaterHintName.ENABLE_UNITY_RES_UPDATE, 0);
#endif

            var hasResUpdate = false;
            var hasDataUpdate = false;
            GameUpdateManager.Instance.onUpdateConfirm = (bytes, confirm, cancel) =>
            {
                var phase = GameUpdateManager.Instance.progressData?.CurrentUpdateResourceType ?? CenturyGame.AppUpdaterLib.Runtime.UpdateResourceType.UnKnow;
                if (phase == CenturyGame.AppUpdaterLib.Runtime.UpdateResourceType.NormalResource)
                {
                    hasResUpdate = true;
                }
                if (phase == CenturyGame.AppUpdaterLib.Runtime.UpdateResourceType.TableData)
                {
                    hasDataUpdate = true;
                }
                confirm?.Invoke();
            };

            GameUpdateManager.Instance.StartUpdate();

            Func<float> reporter = () =>
            {
                var p1 = GameUpdateManager.Instance.GetUpdateProgress01(GameUpdateManager.GameUpdatePhase.PhaseRes);
                var p2 = GameUpdateManager.Instance.GetUpdateProgress01(GameUpdateManager.GameUpdatePhase.PhaseData);
                return (p1 + p2) * 0.5f;
            };

            loading?.SetProgress(from, to, reporter);

            // 等待更新结束 or 出错后卡在空转流程
            yield return _CoWaitGameUpdateFinish();

            // 更新毕重置confHelper类的清单缓存
            var resChange = false;
            if (!hasResUpdate)
                resChange = ConfHelper.CheckUpdateAndRefreshResMap();
            else
                ConfHelper.RefreshResMap();

            string httpRoot = GameUpdateManager.Instance.httpRoot;
            if (!string.IsNullOrEmpty(httpRoot))
            {
                // httpRoot = "http://10.0.89.171:50001/";          //自定义服务器
                DebugEx.FormatInfo("GameProcedure::_CoAppUpdate ----> reset http url to :{0}", httpRoot);
                Game.Instance.appSettings.httpServer.urlRoot = httpRoot;
            }
            CenturyGame.Foundation.RunTime.FdServerManager.Instance.Init(httpRoot, true);
            //设置http消息预期返回时间 默认5秒
            FoundationWrapper.SetHttpExpectTime(5f);
            //设置http网络状态检测回调 在收到ReqTimeOutCb时认为处于弱网
            FoundationWrapper.SetHttpReqTimeOutCb();

            var appInfoAfterUpdate = AppUpdaterManager.AppUpdaterGetAppInfoManifest();
            if (appInfoAfterUpdate != null && !string.IsNullOrEmpty(appInfoAfterUpdate.version))
            {
                DebugEx.FormatInfo("GameProcedure::_CoAppUpdate ----> reset version to :{0}", appInfoAfterUpdate.version);
                Game.Instance.appSettings.version = appInfoAfterUpdate.version;
            }

            // track
            if (hasResUpdate || hasDataUpdate)
            {
                DataTracker.hotfix.Track(hasDataUpdate ? appInfoAfterUpdate.dataResVersion : "not change",
                                        hasResUpdate ? appInfoAfterUpdate.unityDataResVersion : "not change");
            }

            if (isRestarting || hasResUpdate || resChange)
            {
                // 1. clear
                // Game.Manager.audioMan.ClearAll();
                // MusicManager.Instance.ClearAll();
                // 更新后清除所有资源
                GameObjectPoolManager.Instance.ClearAllPool();
                NetTexture2DPool.Instance.ClearAll();
                ResManager.Clear();
                // 2. load 如果用startcoroutine 似乎不能被正确等待 和切换为UniTask有关?
                yield return AssetBundleManager.Instance.CoPrepare();
                // res 全局
                yield return LoadingJob_CoLoadCommonRes();
                // res Sound&Music
                LoadSoundAndMusic();
                // 有更新 清理上次记录的语言路径 避免语言不重新加载
                GameI18NHelper.GetOrCreate().ClearLastPath();
            }
            //加载代码热更
            DebugEx.Info($"[GameProcedure] hotfix after update");
            yield return AsyncTaskUtility.ExtractAsyncTaskFromCoroutine<AsyncTaskBase>(out var taks, Hotfix.HotfixManager.Instance.ATInitPatch());
            // 加载字体材质Asset配置信息
            yield return LoadingJob_CoLoadFontMatRes();
            //根据当前语言加载对应字体Asset资源
            yield return GameI18NHelper.GetOrCreate().CoLoadFontAsset();
            //刷新当前语言对应的配置文本
            I18N.RefreshI18N();
            //加载飞图标使用的配置
            yield return LoadingJob_UIFlyConfig();
            Game.Instance.appSettings.updaterConfig.channel = GameUpdateManager.Instance.channel;
            MessageCenter.Get<MSG.GAME_RES_UPDATE_FINISH>().Dispatch();
        }

        private static IEnumerator _CoWaitGameUpdateFinish()
        {
            var stop = false;
            var mgr = GameUpdateManager.Instance;
            while (mgr.state != GameUpdateManager.State.Finish)
            {
                if (stop)
                {
                    yield return null;
                    continue;
                }
                if (mgr.CheckGameUpdateState())
                    stop = true;
                yield return null;
            }
        }

        public static IEnumerator LoadingJob_CoLoadFontMatRes()
        {
            var firstLoadAssets = EL.Resource.ResManager.LoadAsset<FontMaterialRes>(Constant.kFontMatRes.Group, Constant.kFontMatRes.Asset);
            yield return firstLoadAssets;
            if (!firstLoadAssets.isSuccess)
            {
                DebugEx.FormatError("Game::LoadingJob_CoLoadFontMatRes ----> load font material assets failed, {0}", firstLoadAssets.error);
            }
            FontMaterialRes.Instance = firstLoadAssets.asset as FontMaterialRes;
        }

        private static IEnumerator LoadingJob_UIFlyConfig()
        {
            var flyConfigAssets = EL.Resource.ResManager.LoadAsset<UIFlyConfig>(Constant.KFlyConfig.Group, Constant.KFlyConfig.Asset);
            yield return flyConfigAssets;
            if (!flyConfigAssets.isSuccess)
            {
                DebugEx.FormatError("Game::CoPrepareGame ----> load first assets failed, {0}", flyConfigAssets.error);
            }

            UIFlyConfig.Instance = flyConfigAssets.asset as UIFlyConfig;
        }
    }
}