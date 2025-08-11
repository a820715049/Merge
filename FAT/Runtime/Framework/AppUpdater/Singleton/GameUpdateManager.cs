/**
 * @Author: handong.liu
 * @Date: 2021-11-05 12:30:07
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using CenturyGame.AppUpdaterLib.Runtime.Managers;
using CenturyGame.AppUpdaterLib.Runtime;
using CenturyGame.AppUpdaterLib.Runtime.Configs;
using CenturyGame.Framework;
using CenturyGame.Framework.Http;
using ProtokitHelper.AppUpdaterRequester;
using CenturyGame.AppUpdaterLib.Runtime.MTDownload;
using FAT;


public class GameUpdateManager : MonoSingleton<GameUpdateManager>
{
    private class Logger : CenturyGame.LoggerModule.Runtime.ILogger
    {
        void CenturyGame.LoggerModule.Runtime.ILogger.Debug(object message) => DebugEx.FormatInfo("{0}", message);
        void CenturyGame.LoggerModule.Runtime.ILogger.DebugFormat(string format, params object[] args) => DebugEx.FormatInfo(format, args);
        void CenturyGame.LoggerModule.Runtime.ILogger.Error(object message) => DebugEx.FormatError("{0}", message);
        void CenturyGame.LoggerModule.Runtime.ILogger.Error(object message, System.Exception exception) => DebugEx.FormatError("{0} :{1}", message, exception.StackTrace);
        void CenturyGame.LoggerModule.Runtime.ILogger.ErrorFormat(string format, params object[] args) => DebugEx.FormatError(format, args);
        void CenturyGame.LoggerModule.Runtime.ILogger.Fatal(object message) => DebugEx.FormatError("{0}", message);
        void CenturyGame.LoggerModule.Runtime.ILogger.Fatal(object message, System.Exception exception) => DebugEx.FormatError("{0} :{1}", message, exception.StackTrace);
        void CenturyGame.LoggerModule.Runtime.ILogger.FatalFormat(string format, params object[] args) => DebugEx.FormatError(format, args);
        void CenturyGame.LoggerModule.Runtime.ILogger.Info(object message) => DebugEx.FormatInfo("{0}", message);
        void CenturyGame.LoggerModule.Runtime.ILogger.InfoFormat(string format, params object[] args) => DebugEx.FormatInfo(format, args);
        void CenturyGame.LoggerModule.Runtime.ILogger.Warn(object message) => DebugEx.FormatWarning("{0}", message);
        void CenturyGame.LoggerModule.Runtime.ILogger.WarnFormat(string format, params object[] args) => DebugEx.FormatWarning(format, args);
    }

    private class LogProvider : CenturyGame.LoggerModule.Runtime.ILoggerProvider
    {
        private Logger mCachedLogger = new Logger();
        string CenturyGame.LoggerModule.Runtime.ILoggerProvider.Name => "GM7";

        CenturyGame.LoggerModule.Runtime.ILogger CenturyGame.LoggerModule.Runtime.ILoggerProvider.GetLogger(string name) => mCachedLogger;
        void CenturyGame.LoggerModule.Runtime.ILoggerProvider.Shutdown() {}
    }


    public enum GameUpdatePhase
    {
        PhaseRes,
        PhaseData
    }

    public enum State
    {
        Idle,
        Checking,
        ForceUpdate,
        Maintenance,
        WaitUpdate,
        NetError,
        Error,
        Finish
    }
    public bool hasPendingUpdate => mHasPendingUpdate;
    public float progress => mProgressData?.Progress ?? 0;
    public EL.Resource.ResourceManifest dataResManifest => mManifest;       //配置很特殊，它应该很精确，少了，错了都不应该进游戏
    public AppUpdaterProgressData progressData => mProgressData;
    public string httpRoot => AppUpdaterManager.AppUpdaterGetServerUrl();
    public long errorCode => mError;
    public System.Action<ulong, System.Action, System.Action> onUpdateConfirm;              //param: size in bytes, confirm download cb, cancel download cb
    public string appUrl => AppUpdaterManager.AppUpdaterGetLHConfig()?.UpdateData?.AppStoreUrl;
    public string jsonUrl => AppUpdaterManager.AppUpdaterGetLHConfig()?.UpdateData?.DescUrl;
    public State state => mState;
    public string channel => AppUpdaterConfigManager.AppUpdaterConfig?.channel;
    public string recommendVersion => mRecommendVersion;
    public int recommendCountPerDay => mRecommendUpdatePerDay;
    public EL.Resource.ResourceManifest importantFileManifest => mManifest;
    public bool AppUpdaterHttpRequestDone =>  mInnerRequester.State == AppUpdaterHttpRequester.InnerState.Idle ||
        mInnerRequester.State == AppUpdaterHttpRequester.InnerState.RequestCompleted ||
        mInnerRequester.State == AppUpdaterHttpRequester.InnerState.RequestFailure;
    internal string version => AppUpdaterManager.AppUpdaterGetLHConfig()?.UpdateData?.Versoin;
    private State mState = State.Idle;
    private long mError = 0;
    private CenturyGame.Framework.Http.HttpManager mHttpManager;
    private AppUpdaterProgressData mProgressData;
    private AppUpdaterHttpRequester mInnerRequester { get; set; }
    private bool mInited = false;
    private float mRetryCounter = -1;
    private int mRetryTimes = 0;
    private bool mHasPendingUpdate = false;
    private string mRecommendVersion;
    private int mRecommendUpdatePerDay = 0;
    private int mClearStorageCounter = 0;
    private EL.Resource.ResourceManifest mManifest;
    private FAT.AppUpdater.UpdateChecker mUpdateChecker = new();

    public void ToForeground()
    {
        mUpdateChecker.ToForeground();
    }

    public void ToBackground()
    {
        mUpdateChecker.ToBackground();
    }

    public void OnRestartGame()
    {
        mUpdateChecker.Cancel();
    }

    public bool IsNeedWaitUpdate()
    {
        return PlayerPrefs.HasKey("____GAME_CLOSE");
    }

    public float GetUpdateProgress01(GameUpdatePhase phase)
    {
        if(mProgressData == null)
        {
            return 0;
        }
        if(mState == State.Finish)
        {
            return 1;
        }
        switch(phase)
        {
            case GameUpdatePhase.PhaseRes:
                if(mProgressData.CurrentUpdateResourceType == UpdateResourceType.NormalResource)
                {
                    return mProgressData.Progress;
                }
                else if(mProgressData.CurrentUpdateResourceType == UpdateResourceType.TableData)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            case GameUpdatePhase.PhaseData:
                if(mProgressData.CurrentUpdateResourceType == UpdateResourceType.TableData)
                {
                    return mProgressData.Progress;
                }
                else
                {
                    return 0;
                }
            default:
                return 0;
        }
    } 
    
    public bool IsNeedWaitUpdate2()
    {
        return PlayerPrefs.HasKey("____GAME_CLOSE2");
    }

    public void SetNeedWaitUpdate(bool needWait)
    {
        if(needWait)
        {
            PlayerPrefs.SetString("____GAME_CLOSE", "22vd");
        }
        else
        {
            PlayerPrefs.DeleteKey("____GAME_CLOSE");
        }
        PlayerPrefs.Save();
    }

    //是否要丢弃本地存档
    public void SetNeedWaitUpdate2(bool needWait)
    {
        if(needWait)
        {
            PlayerPrefs.SetString("____GAME_CLOSE2", "22vd");
        }
        else
        {
            PlayerPrefs.DeleteKey("____GAME_CLOSE2");
        }
        PlayerPrefs.Save();
    }

    public static void SetOverrideChannel(string channel)
    {
        if(string.IsNullOrEmpty(channel))
        {
            DebugEx.FormatInfo("GameUpdateManager::SetOverrideChannel ----> delete override channel");
            PlayerPrefs.DeleteKey("____LH___O__V__R");
            PlayerPrefs.Save();
        }
        else
        {
            DebugEx.FormatInfo("GameUpdateManager::SetOverrideChannel ----> {0}", channel);
            PlayerPrefs.SetString("____LH___O__V__R", channel);
            PlayerPrefs.Save();
        }
    }
    
    public static void SetClientId(string clientId)
    {
        if(string.IsNullOrEmpty(clientId))
        {
            DebugEx.FormatInfo("GameUpdateManager::SetClientId ----> delete client id");
            PlayerPrefs.DeleteKey("____LH___C__I__D");
            FAT.Game.Instance.appSettings.clientId = string.Empty;
            PlayerPrefs.Save();
        }
        else
        {
            DebugEx.FormatInfo("GameUpdateManager::SetClientId ----> {0}", clientId);
            PlayerPrefs.SetString("____LH___C__I__D", clientId);
            FAT.Game.Instance.appSettings.clientId = clientId;
            PlayerPrefs.Save();
        }
    }

    public void StartUpdate()
    {
        bool secondTime = mInited;
        mInited = true;
        CenturyGame.LoggerModule.Runtime.LoggerManager.SetCurrentLoggerProvider(new LogProvider());
        EL.Resource.ResManager.webManifest.Clear();
        mHasPendingUpdate = false;
        string overrideChannel = PlayerPrefs.GetString("____LH___O__V__R", string.Empty);
        DebugEx.FormatInfo("GameUpdateManager::Init ----> override {0}", overrideChannel);
        if(!string.IsNullOrEmpty(overrideChannel))
        {
            DebugEx.FormatInfo("GameUpdateManager::Init ----> use override channel {0}", overrideChannel);
            AppUpdaterConfigManager.AppUpdaterConfig.channel = overrideChannel;
        }
        mState = State.Checking;

        if (secondTime)
        {
            AppUpdaterManager.AppUpdaterStartUpdateAgain();
            return;
        }

        AppUpdaterManager.AppUpdaterHint(AppUpdaterHintName.ENABLE_LEGACY_VERSION_SYSTEM, (int)AppUpdaterBool.TRUE);
        AppUpdaterManager.AppUpdaterHint(AppUpdaterHintName.VMS_MANIFEST_FORMAT, (int)AppUpdaterBool.FALSE);
        AppUpdaterManager.Init();
        mInnerRequester = new AppUpdaterHttpRequester();
        AppUpdaterManager.AppUpdaterSetAppUpdaterRequester(mInnerRequester);
        AppUpdaterManager.AppUpdaterSetStorageInfoProvider(new WrapperStorageInfoProvider());
        AppUpdaterManager.AppUpdaterSetDownloadService<MTRemoteFileDownloadService>();
        // 保持此目录不删除
        AppUpdaterManager.AppUpdaterSetRetainedDataFolderName(EL.Resource.FileAssetBundleProviderExternal.external_res_path_root);
        this.AddListeners();
        this.mProgressData = AppUpdaterManager.AppUpdaterGetAppUpdaterProgressData();
    }

    public void Dispose()
    {

    }

    private void AddListeners()
    {
        AppUpdaterManager.AppUpdaterSetErrorCallback((errorType, desc) => { 
            DebugEx.Error($"GameUpdateManager ----> Error type {errorType} error message : {desc}" );
            switch (errorType)
            {
                //收到此errorCode时认为未连接网络
                case AppUpdaterErrorType.DownloadLighthouseFailure:
                {
                    mState = State.NetError;
                    break;
                }
                default:
                {
                    mState = State.Error;
                    break;
                }
            }
            mError = ErrorCodeUtility.ConvertToCommonCode((int)errorType, ErrorCodeType.UpdateError);
        });

        AppUpdaterManager.AppUpdaterSetForceUpdateCallback(info =>
        {
            DebugEx.Info("GameUpdateManager ----> Client need to force update app!");
            DebugEx.Info($"GameUpdateManager ----> Versoin : {info.Versoin} PackageUrl : {info.PackageUrl} AppStoreUrl : {info.AppStoreUrl} DescUrl : {info.DescUrl}");
            mRecommendVersion = info.Versoin;
            mState = State.ForceUpdate;
        });

        AppUpdaterManager.AppUpdaterSetOnTargetVersionObtainCallback(version =>
        {
            DebugEx.Info($"GameUpdateManager ----> Target version : {version} .");
        });

        AppUpdaterManager.AppUpdaterBindEnableDownloadJudger((bytes, confirm, cancel) =>
        {
            if(onUpdateConfirm == null)
            {
                DebugEx.Info($"GameUpdateManager ----> default go on, bytes : {bytes} .");
                confirm?.Invoke();
            }
            else
            {
                DebugEx.Info($"GameUpdateManager ----> Ask whether go on, bytes : {bytes} .");
                onUpdateConfirm?.Invoke(bytes, confirm, cancel);
            }
        });

        AppUpdaterManager.AppUpdaterSetPerformCompletedCallback((info) =>
        {
            DebugEx.Info("GameUpdateManager ----> AppUpdater perform completed");
            DebugEx.Info($"GameUpdateManager ----> server url : \"{AppUpdaterManager.AppUpdaterGetServerUrl()}\" .");
            DebugEx.Info($"GameUpdateManager ----> Chagnnel : {AppUpdaterManager.AppUpdaterGetChannel()} .");
            try
            {
                var targetVersion = new Version(AppUpdaterManager.AppUpdaterGetLHConfig().UpdateData.Versoin);
                var currentVersion = new Version(AppUpdaterManager.AppUpdaterGetAppInfoManifest().version);
                var compare = currentVersion.CompareTo(targetVersion);
                if(compare < Version.VersionCompareResult.Equal)
                {
                    mRecommendVersion = targetVersion.GetVersionString();
                }
                mRecommendUpdatePerDay = (int)AppUpdaterManager.AppUpdaterGetLHConfig().UpdateData.DisplayPerDay;
            }
            catch(System.Exception ex)
            {
                DebugEx.FormatError("GameUpdateManager::AppUpdaterSetPerformCompletedCallback ----> fail to parse recommend update info {0}, {1}", ex.Message, ex.StackTrace);
                mRecommendVersion = null;
                mRecommendUpdatePerDay = 0;
            }
#if UNITY_WEBGL
#else
            mState = State.Finish;
            mClearStorageCounter = 0;
#endif
        });

        AppUpdaterManager.AppUpdaterSetServerMaintenanceCallback(maintenceInfo =>
        {
            DebugEx.Info($"GameUpdateManager ----> Maintence IsOpen : {maintenceInfo.IsOpen} UrlPattern : {maintenceInfo.UrlPattern}");
            mState = State.Maintenance;
        });
    }

    private void Update()
    {
    }
    
    //检查游戏更新状态
    public bool CheckGameUpdateState()
    {
        if (state == State.ForceUpdate)
        {
            Game.Instance.ShowAppForceUpdate();
            return true;
        }
        else if (state == State.Error)
        {
            var content = ErrorCodeUtility.GetNoticeContentByErrorCodeType(errorCode);
            Game.Instance.Abort(content, errorCode, isShowErrorCode : false);
            return true;
        }
        else if (state == State.Maintenance)
        {
            Game.Instance.Abort("[GameUpdate] : " + I18N.Text("#SysComDesc190"), 0);
            return true;
        }
        else if (state == State.NetError)
        {
            Game.Instance.AbortContinue(I18N.Text("#SysComDesc227"), errorCode, GameProcedure.RestartGame, I18N.Text("#SysComDesc376"), false);
            return true;
        }
        return false;
    }
    
    private void _Restart()
    {
        mState = State.Checking;
        StartUpdate();
    }
}