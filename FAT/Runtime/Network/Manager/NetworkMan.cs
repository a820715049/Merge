/**
 * @Author: handong.liu
 * @Date: 2020-09-25 17:41:18
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using Google.Protobuf;
using CenturyGame.Foundation.RunTime;
using gen.netutils;
using FAT;
using FAT.Platform;
using fat.msg;
using fat.gamekitdata;
using fat.service;

namespace GameNet
{
    //state
    //
    // NotLogin ---call login---> Logining
    // Logining ----login fail---> NotLogin
    // Logining ----find out that server LastSync is newer---> ArchiveChanged
    // Logining ----find out that archive's LastUpdate is newer than now server time----> TimeNotSync
    // Logining ----success----> Logined
    // any state ----get Maintenance error---> Maintenance
    // any state ----get ForceUpdate error---> ForceUpdate
    // any state ----get KickedOff error----> KickedOff
    // Logined ---->Sync time find that Server time bias is very difference with now----> TimeNotSync
    // 
    // 游戏中和服务器对时以及login统称 Sync
    public partial class NetworkMan: IRTMListener, IGameModule, IUpdate
    {
        public enum State
        {
            NotLogin,
            Logining,
            Logined,
            Maintenance,
            ForceUpdate,
            TokenError,
            KickedOff,
            CommonQuit,
            CommonRestart,
            CommonRelogin,
            CommonReloginSDK,
            ArchiveChanged,
            TimeNotSync,
            NetWeakRestart,
            UserSmallVersionBan,
            UnknownError,
        }
        public bool IsGameLogined { get; private set; }
        public bool isLogin => state == State.Logined;
        public State state => mState;
        public bool isInSync => timeSyncHelper.isSynced;
        public bool isKickedOff => mState == State.KickedOff;
        public ulong remoteUid {get;private set;}
        public long networkBias => timeSyncHelper.serverTimeBias;
        public long debugBias => timeSyncHelper.localTimeBias;
        public bool isWeakNetwork => mNetworkErrorStartTime > 0;
        public long NetWeakStartTime => mNetworkErrorStartTime;
        private List<System.Action> mExecuteAfterLogin = new List<System.Action>();     //如果要等游戏登陆后再执行，使用这个队列
        public string fpId => mFPID;
        private string mFPID;
        private float mHeartbeatInterval = 295;
        private float mHeartbeatCounter = 0;
        private long mNetworkErrorStartTime = 0;
        private int mNetworkErrorStartStatusCode = 0;
        private struct LoginResult
        {
            public bool isAuthResult;           //is this result belong to Auth request
            public NetResponse result;          //result
            public bool isArchiveModified;      //is archive modified
        }
        private LoginResult mLoginResult;
        private State mState = State.NotLogin;
        private AsyncTaskBase mSyncTask = null;
        private float mNextSyncRetryCountDown = 10;
        private NetTimeSync timeSyncHelper = new();


        #region IGameModule
        void IGameModule.Reset()
        {
            IsGameLogined = false;

            mState = State.NotLogin;
            mHeartbeatCounter = 0;
            mNetworkErrorStartTime = 0;
            mNextSyncRetryCountDown = 0;

            mFPID = string.Empty;

            mSyncTask = null;
            timeSyncHelper.Reset();
        }

        void IGameModule.LoadConfig()
        { }

        void IGameModule.Startup()
        {
            timeSyncHelper.StartUp();
        }
        #endregion

        public void DebugSetNetworkBias(long bias)
        {
            timeSyncHelper.DebugSetTimeBias(bias);
        }

        private static readonly HashSet<long> kQuitErrorCode = new HashSet<long>() {
            (long)fat.gamekitdata.ErrorCode.BanUser,
        };

        private static readonly HashSet<long> kRestartErrorCode = new HashSet<long>() {
            (long)fat.gamekitdata.ErrorCode.UserDataInvalid
        };
        
        private static readonly HashSet<long> kReloginSDKErrorCode = new HashSet<long>() {
            (long)fat.gamekitdata.ErrorCode.CheckFpidFailed,
            (long)fat.gamekitdata.ErrorCode.RemovedUser
        };
        
        private static bool _IsQuitErrorCode(long err)
        {
            return kQuitErrorCode.Contains(err);
        }


        private static bool _IsRestartErrorCode(long err)
        {
            return kRestartErrorCode.Contains(err);
        }

        private static bool _IsReloginSDKErrorCode(long err)
        {
            return kReloginSDKErrorCode.Contains(err);
        }

        public void TransStateByErrorCode(long commonErrorCode)
        {
            _TransitStateByErrorCode(commonErrorCode);
        }
        
        private void _TransitStateByErrorCode(long commonErrorCode)
        {
            if(commonErrorCode > 0)
            {
                ErrorCodeUtility.ExtractComonCode(commonErrorCode, out var type, out var errorCode);
                switch(type)
                {
                    case ErrorCodeType.ServerError:
                        if(errorCode == (int)fat.gamekitdata.ErrorCode.TokenExpired)
                        {
                            //token error
                            DebugEx.FormatWarning("NetworkMan._PostHttpMessage ----> token error! {0}", errorCode);
                            _SetState(State.TokenError, commonErrorCode);
                        }
                        else if(errorCode == (int)fat.gamekitdata.ErrorCode.KickError)  //顶号踢人
                        {
                            //kicked
                            DebugEx.FormatWarning("NetworkMan._PostHttpMessage ----> kicked! {0}", errorCode);
                            _SetState(State.KickedOff, commonErrorCode);
                        }
                        else if(errorCode == (int)fat.gamekitdata.ErrorCode.Maintenance)
                        {
                            //maintenance
                            DebugEx.FormatWarning("NetworkMan._PostHttpMessage ----> maintenance!");
                            _SetState(State.Maintenance, commonErrorCode);
                        }
                        else if(errorCode == (int)fat.gamekitdata.ErrorCode.ForceUpdate)
                        {
                            //maintenance
                            DebugEx.FormatWarning("NetworkMan._PostHttpMessage ----> force update!");
                            _SetState(State.ForceUpdate, commonErrorCode);
                        }
                        else if(errorCode == (int)fat.gamekitdata.ErrorCode.UserSmallVersionBan)
                        {
                            DebugEx.FormatWarning("NetworkMan._PostHttpMessage ----> small version ban!");
                            _SetState(State.UserSmallVersionBan, commonErrorCode);
                        }
                        else if(_IsQuitErrorCode(errorCode))
                        {
                            DebugEx.FormatWarning("NetworkMan._PostHttpMessage ----> quit error code! {0}", errorCode);
                            _SetState(State.CommonQuit, commonErrorCode);
                        }
                        else if(_IsRestartErrorCode(errorCode))
                        {
                            DebugEx.FormatWarning("NetworkMan._PostHttpMessage ----> restart error code! {0}", errorCode);
                            _SetState(State.CommonRelogin, commonErrorCode);
                        }
                        else if(_IsReloginSDKErrorCode(errorCode))
                        {
                            DebugEx.FormatInfo("NetworkMan._PostHttpMessage ----> relogin sdk error code! {0}", errorCode);
                            _SetState(State.CommonReloginSDK, commonErrorCode);
                        }
                        else
                        {
                            DebugEx.FormatInfo("NetworkMan._PostHttpMessage ----> unknown error code! {0}", errorCode);
                            _SetState(State.UnknownError, commonErrorCode);
                        }
                        break;
                    case ErrorCodeType.HttpError:
                        _OnNetworkWeak(true);
                        break;
                    case ErrorCodeType.GameError:
                        if(errorCode == (long)GameErrorCode.ServerSignError)
                        {
                            _SetState(State.CommonQuit, commonErrorCode);
                        }
                        break;
                }
            }
        }

        public NetworkMan()
        { }

        public void ExecuteAfterLogin(System.Action act)
        {
            if(mState == State.Logined)
            {
                act?.Invoke();
            }
            else if(act != null)
            {
                mExecuteAfterLogin.Add(act);
            }
        }

        public void SetServerUnsync()
        {
            if(state == State.Logined)
            {
                timeSyncHelper.SetUnsync();
                mNextSyncRetryCountDown = 2;
                DebugEx.FormatInfo("NetworkMan::SetServerUnsync ----> set server unsync");
                Game.Instance.OnNetworkStateChanged(false);
            }
        }

        private float mRTMLoginCounter = 0;
        void IUpdate.Update(float dt)
        {
            if (isInSync)
            {
                mHeartbeatCounter += dt;
                if (mHeartbeatCounter > mHeartbeatInterval)
                {
                    mHeartbeatCounter = 0;
                    Game.Instance.StartCoroutineGlobal(_ATHeartbeat());
                }
                mRTMLoginCounter += dt;
                if (mRTMLoginCounter > 20)
                {
                    mRTMLoginCounter = 0;
                    if (RTMManager.Instance.needAuth)
                    {
                        Game.Instance.StartCoroutineGlobal(_CoInitRTM());
                    }
                }
            }
            if (!isInSync && (mSyncTask == null || !mSyncTask.keepWaiting))
            {
                mNextSyncRetryCountDown -= dt;
                if (mNextSyncRetryCountDown <= 0)
                {
                    mNextSyncRetryCountDown = 5;
                    if (state == State.Logined)
                    {
                        Game.Instance.StartAsyncTaskGlobal(_ATRequestSyncTime());
                    }
                    else if (state != State.Logining) {
                        Game.Instance.StartAsyncTaskGlobal(_ATLogin());
                    }
                }
            }

            if (!isInSync)
            {
                if (state == State.Logined)
                { }
            }
        }

        // 在login以后发起
        private IEnumerator _ATRequestSyncTime()
        {
            DebugEx.FormatInfo("NetworkMan::_ATRequestSyncTime");
            if(mSyncTask != null && mSyncTask.keepWaiting)
            {
                yield return mSyncTask;
                yield break;
            }
            var ret = new SimpleAsyncTask();
            mSyncTask = ret;
            yield return ret;

            DebugEx.FormatInfo("NetworkMan::_ATRequestSyncTime ---> real");

            var req = PostMessage_SyncTime();
            yield return req;
            if(ret.isCanceling)
            {
                DebugEx.FormatInfo("NetworkMan::ATRequestSyncTime ----> sync time canceled");
                ret.ResolveTaskCancel();
            }
            else
            {
                if(req.isSuccess)
                {
                    var resp = req.result as SyncTimeResp;
                    ret.ResolveTaskSuccess();
                    var state = timeSyncHelper.CheckSync(resp.ServerSec, null);
                    if(state != SyncStatus.InSync)
                    {
                        _SetState(State.TimeNotSync, 0);
                    }
                    else
                    {
                        Game.Instance.OnNetworkStateChanged(true, false);
                    }
                }
                else
                {
                    DebugEx.FormatWarning("NetworkMan::ATRequestSyncTime ----> sync time failed {0}", req.error);
                    ret.ResolveTaskFail(req.errorCode);
                }
            }
        }

        public IEnumerator ATLogin()
        {
            return _ATLogin();
        }

        public void CancelOngoingSyncTime()
        {
            if(mSyncTask != null && mSyncTask.keepWaiting && mState == State.Logined)
            {
                //is syncing time
                mSyncTask.Cancel();
                mSyncTask = null;
            }
        }

        private IEnumerator _ATLogin()
        {
            DebugEx.FormatInfo("NetworkMan::_ATLogin");
            if(mSyncTask != null && mSyncTask.keepWaiting)
            {
                yield return mSyncTask;
                yield break;
            }
            mState = State.Logining;
            var ret = new SingleTaskWrapper();
            mSyncTask = ret;
            yield return ret;

            DebugEx.FormatInfo("NetworkMan::_ATLogin ----> Real");
            var loginTask = Game.Instance.StartAsyncTaskGlobal(_ATRealLogin());
            ret.SetTask(loginTask);
            yield return loginTask;
            if(!loginTask.isSuccess && loginTask.errorCode == (long)fat.gamekitdata.ErrorCode.CheckFpidFailed)
            {
                DebugEx.FormatWarning("NetworkMan::Login ----> encount error 1203, do logout and login");
                var logout = PlatformSDK.Instance.Adapter.Logout();
                PlatformSDK.Instance.Adapter.LoginType = AccountLoginType.Guest;
                yield return logout;
                loginTask = Game.Instance.StartAsyncTaskGlobal(_ATRealLogin());
                ret.SetTask(loginTask);
                yield return loginTask;
            }

            if(loginTask.isSuccess)
            {
                _ConfirmLogin();
                DebugEx.FormatInfo("NetworkMan::Login ----> state change to {0}", mState);
                ret.ResolveSuccess();
            }
            else
            {
                ret.ResolveError();
            }
        }

        private IEnumerator _ATRealLogin()
        {
            var task = new SimpleAsyncTask();
            yield return task;

            var sdk = PlatformSDK.Instance;
            yield return sdk.Login();//must login platform

            if(!sdk.Adapter.SDKLogin)
            {
                task.ResolveTaskFail((long)GameErrorCode.PlatformLoginError);
                yield break;
            }

            var key = PlayerPrefs.GetString(Constant.kPrefKeyDebugFPID, string.Empty);
            var sessionSecret = "";
            if(string.IsNullOrEmpty(key))           //not a debug play
            {
                key = PlatformSDK.Instance.GetUserId();
                sessionSecret = PlatformSDK.Instance.GetUserSessionKey();
            }
            //key = "AndroidAudit";       //For android

            var resp = default(LoginResult);
            _Login(key, sessionSecret, (result)=>{
                resp = result;
            });
            while(resp.result == null)
            {
                yield return null;
            }

            if(resp.isArchiveModified)
            {
                DebugEx.FormatInfo("NetworkMan::Login ----> login result: archive modified {0}", resp.result);
                mLoginResult = resp;

                task.ResolveTaskSuccess();
            }
            else if(resp.isAuthResult || resp.result.errCode != (int)fat.gamekitdata.ErrorCode.Ok)
            {
                DebugEx.FormatInfo("NetworkMan::Login ----> login fail:{0}", resp);
                task.ResolveTaskFail(resp.result.errCode, null, (ErrorCodeType)0);
            }
            else
            {
                mLoginResult = resp;
                task.ResolveTaskSuccess();
            }
        }

        private void _ConfirmLogin()
        {
            var resp = mLoginResult.result;
            if(mLoginResult.isArchiveModified)          //early exit when archive modified
            {
                GameUpdateManager.Instance.SetNeedWaitUpdate(true);
                GameUpdateManager.Instance.SetNeedWaitUpdate2(true);
                _SetState(State.ArchiveChanged, 0);
                return;
            }
            if(mLoginResult.isAuthResult)
            {
                if(resp.errCode != (int)fat.gamekitdata.ErrorCode.Ok)
                {
                    //auth error
                    DebugEx.FormatWarning("NetworkMan::Login ----> auth error = {0}, code = {1}", resp.err, resp.errCode);
                    _TransitStateByErrorCode(resp.errCode);
                }
                else
                {
                    //token is null
                    DebugEx.FormatWarning("NetworkMan::Login ----> token is null {0}", resp);
                }
            }
            else
            {
                if(resp.errCode != (int)fat.gamekitdata.ErrorCode.Ok)
                {
                    DebugEx.FormatWarning("NetworkMan::Login ----> login error = {0}, code = {1}", resp.err, resp.errCode);
                    _TransitStateByErrorCode(resp.errCode);
                }
                else
                {
                    var body = resp.GetBody<AuthorizeRspOverride>();
                    var state = timeSyncHelper.CheckSync(body.ServerSec, body.BaseData);
                    DebugEx.FormatInfo("NetworkMan::Login ----> success!, state = {0}", state);
                    if(state == SyncStatus.InSync)
                    {
                        var clientData = new ClientData();
                        clientData.MergeFrom(new Google.Protobuf.CodedInputStream(body.ClientData.ToByteArray()));
                        Game.Manager.archiveMan.OnReceiveServerArchive(body.BaseData, clientData, body.NewUser);

                        _SetState(State.Logined, 0);
                        _GameLogin(body.BaseData, true);
                        Game.Instance.OnNetworkStateChanged(true, true);
                        foreach(var cb in mExecuteAfterLogin)
                        {
                            cb?.Invoke();
                        }
                        mExecuteAfterLogin.Clear();
                    }
                    else
                    {
                        GameUpdateManager.Instance.SetNeedWaitUpdate(true);
                        GameUpdateManager.Instance.SetNeedWaitUpdate2(true);
                        if(state == SyncStatus.ArchiveModified)
                        {
                            _SetState(State.ArchiveChanged, 0);
                        }
                        else if(state == SyncStatus.TimeIsDifferent)
                        {
                            _SetState(State.TimeNotSync, 0);
                        }
                        else
                        {
                            _SetState(State.KickedOff, 0);
                        }
                    }
                }
            }
        }

        private SimpleResultedAsyncTask<IMessage> _curGameLoginTask;
        private void _GameLogin(PlayerBaseData base_, bool forceFail)
        {
            if (IsGameLogined)
                return;
            if (!forceFail && _curGameLoginTask != null)
                return;
            _curGameLoginTask?.Cancel();
            Game.Instance.StartCoroutineGlobal(_ATGameLogin(base_));
        }

        private IEnumerator _ATGameLogin(PlayerBaseData base_)
        {
            FoundationWrapper.EnsureMetaData(true);
            var req = new LoginReq() {
                BaseData = base_,
                SystemInfo = new fat.gamekitdata.SystemInfo() {
                    Device = PlatformSDK.Instance.GetDeviceName() ?? string.Empty,
                    OsVersion = PlatformSDK.Instance.GetOSVersion() ?? string.Empty,
                    Os = PlatformSDK.Instance.GetOSName() ?? string.Empty,
                    Country = PlatformSDK.Instance.GetCountryCode() ?? string.Empty,
                    Language = I18N.GetLanguage() ?? string.Empty,
                    TimeZone = TimeUtility.GetTimeZone() ?? string.Empty,
                    Channel = GameUpdateManager.Instance.channel ?? string.Empty,
                    AppVersion = Game.Instance.appSettings.version ?? string.Empty,
                    DeviceId = PlatformSDK.Instance.GetTrackDistinctId()
                }
            };
            var task = FoundationWrapper.PostMessage(req, LoginService_Login.QueryPath, LoginService_Login.URIRequest);
            _curGameLoginTask = task;
            yield return task;
            _curGameLoginTask = null;
            if (!task.isSuccess)
            {
                DebugEx.Warning($"NetworkMan._ATLoginSync fail {task.errorCode} : {task.error}");
                // 失败后 等2s再重试
                yield return new WaitForSeconds(1f);
                _GameLogin(base_, false);
            }
            else if (task.result is LoginResp resp)
            {
                DebugEx.Info("NetworkMan._ATLoginSync resp");
                IsGameLogined = true;
                Game.Manager.userGradeMan.OnReceiveUserTagInfo(resp.BaseData.UserTagData);
                PlatformSDK.Instance.UpdateProfile(resp.BaseData.FacebookInfo);
            }
            else
            {
                DebugEx.Error("NetworkMan._ATLoginSync unknown");
            }
        }

        private NetResponse buildResult(IMessage msg)
        {
            var result = new NetResponse();
            if (msg is gen.netutils.ErrorResponse resp)
            {
                result.SetError(resp.Code, ErrorCodeType.ServerError, resp.Message);
            }
            else if (msg is fat.netutils.ErrorResponse respFat)
            {
                result.SetError(respFat.Code, ErrorCodeType.ServerError, respFat.Message);
            }
            else
            {
                result.SetBody(msg);
            }

            return result;
        }
        
        //cb(是否auth期间报错, 网络返回)
        private void _Login(string fpId, string sessionKey, System.Action<LoginResult> cb)
        {
            DebugEx.FormatTrace("NetworkMan::Login ----> with fpId:{0}, sessionKey:{1}", fpId, sessionKey);
            mFPID = fpId;
            
            // FAT_TODO: snsID ?
            FdServerManager.Instance.DoAuthorize(fpId,
                sessionKey,
                "",
                (msg) =>
                {
                    var respResult = buildResult(msg);
                    if (msg is AuthorizeRspOverride)
                    {
                        var resp = msg as AuthorizeRspOverride;
                        if (resp.BaseData == null)
                        {
                            DebugEx.FormatWarning("NetworkMan::Login ----> player data is null");
                            var err = ErrorCodeUtility.ConvertToCommonCode((long)fat.gamekitdata.ErrorCode.UserDataInvalid, ErrorCodeType.ServerError);
                            _TransitStateByErrorCode(err);
                            return;
                        }

                        if (timeSyncHelper.CheckSync(resp.ServerSec, resp.BaseData) != SyncStatus.InSync)
                        {
                            DebugEx.FormatWarning("NetworkMan::Login ----> after auth we find the archive changed");
                            cb?.Invoke(new LoginResult() { result = respResult, isAuthResult = true, isArchiveModified = true });
                            var err = ErrorCodeUtility.ConvertToCommonCode((long)fat.gamekitdata.ErrorCode.DataExpired, ErrorCodeType.ServerError);
                            _TransitStateByErrorCode(err);
                        }
                        else
                        {
                            remoteUid = resp.BaseData.Uid;
                            Debug.Log($"NetworkMan::Login ----> login success");
                            cb?.Invoke(new LoginResult() {result = respResult, isAuthResult = false});
                        }
                    }
                    else
                    {
                        // error
                        DebugEx.FormatWarning("NetworkMan::Login ----> auth error {0}", respResult.errCode);
                        cb?.Invoke(new LoginResult() { result = respResult, isAuthResult = true });
                        _TransitStateByErrorCode(respResult.errCode);
                    }
                },
                (status) =>
                {
                    DebugEx.Error($"NetworkMan::Login ----> auth error {status}");
                    var err = ErrorCodeUtility.ConvertToCommonCode(status, ErrorCodeType.HttpError);
                    _TransitStateByErrorCode(err);
                });
        }

        #region Game protocols

        public SimpleResultedAsyncTask<IMessage> PostMessage_SaveArchive(LocalSaveData archive)
        {
            var req = new StoreReq()
            {
#if UNITY_EDITOR
                // editor环境下 始终传服务器时间 | 避免用户被拦截
                Ts = Game.Instance.GetTimestampLocalSeconds() + networkBias - debugBias,
#else
                // 非editor环境下 传活动标准时间 | test用户可跳过检查
                Ts = Game.Instance.GetTimestampSeconds(),
#endif
                BaseData = archive.PlayerBaseData,
                ClientData = FoundationWrapper.MarshalToByteString(archive.ClientData),
                ExtraData = new() {
                    EnergyCost = DataTracker.energy_change_total.CommitTotalConsume(),
                    FinishMilestoneTaskTS = Game.Manager.dailyEvent.MilestoneCompleteTS,
                    GemCost = Game.Manager.coinMan.FetchGemCost(),
                    CoinGet = Game.Manager.coinMan.FetchCoinGet(),
                    RankingActivity = Game.Manager.activity.RankingData(),
                },
            };
            var iap = Game.Manager.iap;
            if (iap.IAPDelivery.Count > 0) {
                req.ExtraData.LatestDonePayRecords.AddRange(Game.Manager.iap.IAPDelivery);
                DebugEx.Info($"iap delivery record {Game.Manager.iap.IAPDelivery.Count}");
                iap.ConfirmDeliver();
            }
            return FoundationWrapper.PostMessage(req, fat.service.StorageService_Store.QueryPath, fat.service.StorageService_Store.URIRequest);
        }

        public SimpleResultedAsyncTask<IMessage> PostMessage_HeartBeat()
        {
            return FoundationWrapper.PostMessage(new HeartbeatReq(), fat.service.HeartbeatService_Hi.QueryPath, fat.service.HeartbeatService_Hi.URIRequest);
        }

        public SimpleResultedAsyncTask<IMessage> PostMessage_SyncTime()
        {
            return FoundationWrapper.PostMessage(new SyncTimeReq(), fat.service.SyncTimeService_Sync.QueryPath, fat.service.SyncTimeService_Sync.URIRequest);
        }

        public SimpleResultedAsyncTask<IMessage> PostMessage_RTMToken()
        {
            return FoundationWrapper.PostMessage(new RTMTokenReq(), fat.service.RTMService_GetToken.QueryPath, fat.service.RTMService_GetToken.URIRequest);
        }

        // FAT_TODO SendLog是否在工作 / 到哪里查看上传结果
        public void SendLog(string tag, string content = "")
        {
            var fpid = PlatformSDK.Instance.GetUserId();
            if (!string.IsNullOrEmpty(fpid))
            {
            }
            // _PostHttpMessageInner(new LogReq() { Tag = tag, Content = content}, false, (res)=> {
            //     
            // });
        }

        #endregion

        #region  RTM    

        private IEnumerator _CoInitRTM()
        {
            mRTMLoginCounter = 0;
            RTMManager.Instance.Init();
            RTMManager.Instance.SetDelegate(this);

            var task = PostMessage_RTMToken();
            yield return task;
            if (task == null)
            {
                DebugEx.FormatWarning("NetworkMan::InitRTM ----> RTMTokenReq failed!");
            }
            else if (task.errorCode != 0)
            {
                DebugEx.FormatWarning("NetworkMan::InitRTM ----> RTMTokenReq failed:{0}", task.errorCode);
            }
            else
            {
                var body = task.result as RTMTokenResp;
                DebugEx.FormatInfo("NetworkMan::InitRTM ----> RTMTokenReq success:{0}", body);
                RTMManager.Instance.Login(body.Token, body.Endpoint, body.Pid, Game.Manager.accountMan.uid);
            }
        }

        void IRTMListener.PushMessage(byte messageType, byte[] binaryMessage, string stringMessage)
        {
            DebugEx.FormatInfo("NetworkMan::IRTMListener::PushMessage ----> {0}", messageType);
            DebugEx.FormatTrace("NetworkMan::IRTMListener::PushMessage ----> {0}, {1}, {2}", messageType, stringMessage, binaryMessage);
            byte[] data = binaryMessage;
            if(!string.IsNullOrEmpty(stringMessage))
            {
                data = System.Convert.FromBase64String(stringMessage);
            }
            else if(messageType == (byte)Notify.UserModify) //用户在线时收到存档数据改变消息
            {
                GameUpdateManager.Instance.SetNeedWaitUpdate(true);
                GameUpdateManager.Instance.SetNeedWaitUpdate2(true);
                Game.Instance.AbortRestart(I18N.Text("#SysComDesc192"), ErrorCodeUtility.ConvertToCommonCode(GameErrorCode.AccountChanged));
            }
            else if(messageType == (byte)Notify.NewEmail) //用户在线时收到新邮件
            {
                Game.Manager.mailMan.RequestMail();
            }
            else if(messageType == (byte)Notify.ExchangeSendCard) //用户在线时收到好友赠送的卡片
            {
                Game.Manager.cardMan.TryPullPendingCardInfo();
            }
        }

        void IRTMListener.OnRTMLogout()
        {
            DebugEx.FormatInfo("NetworkMan::IRTMListener::OnRTMLogout ----> fpId = {0}, uId = {1}", fpId, Game.Manager.accountMan.uid);
        }
        #endregion

        private IEnumerator _ATHeartbeat()
        {
            var task = PostMessage_HeartBeat();
            yield return task;
            if (!task.isSuccess)
            {
                DebugEx.Warning($"NetworkMan._DoHeartbeat fail {task.errorCode} : {task.error}");
            }
            else
            {
                DebugEx.FormatInfo("NetworkMan._DoHeartbeat");
            }
        }

        //弱网状态开始
        public void OnEnterNetworkWeak()
        {
            if (Game.Manager.networkMan == this)
            {
                _OnNetworkWeak(true);
            }
        }
        
        //弱网状态结束
        public void OnExitNetworkWeak()
        {
            if (Game.Manager.networkMan == this)
            {
                _OnNetworkWeak(false);
            }
        }

        public void CheckNetworkWeakInLoading(bool isBad, int targetTime = 0)
        {
            if (Game.Manager.networkMan == this)
            {
                _OnNetworkWeak(isBad, targetTime);
            }
        }

        //收到网络消息时(包括游戏服务器发来的业务逻辑消息以及http底层触发的消息)，若是弱网则计时，超过一段时间后若还是弱网，则弹窗告知玩家
        private void _OnNetworkWeak(bool isBad, int targetTime = 0)
        {
            if (!isBad && mNetworkErrorStartTime > 0)
            {
                if (mNetworkErrorStartTime > 0)
                {
                    mNetworkErrorStartTime = 0;
                    mNetworkErrorStartStatusCode = -1;
                    DebugEx.FormatInfo("NetworkMan::_OnNetworkState ----> network weak: false");
                    //离开弱网状态时立即告知玩家
                    Game.Instance.OnNetworkWeakChanged();
                }
            }
            else if (isBad)
            {
                var now = Game.Instance.GetTimestampSeconds();
                if (mNetworkErrorStartTime <= 0)
                {
                    mNetworkErrorStartTime = now;
                    mNetworkErrorStartStatusCode = FoundationWrapper.RecentHttpErrorStatusCode;
                    DebugEx.FormatInfo("NetworkMan::_OnNetworkState ----> network weak: true");
                    //进入弱网状态时立即告知玩家
                    Game.Instance.OnNetworkWeakChanged();
                }
                else
                {
                    int quitTime = Game.Instance.isRunning
                        ? Game.Manager.configMan.globalConfig.NoNetQuitTime
                        : targetTime;
                    if (mNetworkErrorStartTime + quitTime <= now)
                    {
                        DataTracker.TrackNetWeakRestart(mNetworkErrorStartTime,
                                                        now,
                                                        mNetworkErrorStartStatusCode,
                                                        FoundationWrapper.RecentHttpErrorStatusCode);
                        DebugEx.FormatInfo("NetworkMan::_OnNetworkState ----> network weak: true and tell user");
                        _SetState(State.NetWeakRestart, ErrorCodeUtility.ConvertToCommonCode(GameErrorCode.NoNetwork));
                    }
                }
            }
        }

        public void SetState(State state, long errorCode)
        {
            _SetState(state, errorCode);
        }

        private void _SetState(State state, long errorCode)
        {
            if (mState != state)
            {
                var info = $"NetworkMan::_SetState ----> state change from {mState} to {state}";
                DataTracker.TrackLogInfo(info);
                DebugEx.Info(info);
                mState = state;
                if (state == GameNet.NetworkMan.State.ForceUpdate)
                {
                    Game.Instance.ShowAppForceUpdate();
                }
                else if (state == GameNet.NetworkMan.State.Maintenance)
                {
                    Game.Instance.Abort(I18N.Text("#SysComDesc190"), errorCode);
                }
                else if (state == GameNet.NetworkMan.State.ArchiveChanged)
                {
                    GameUpdateManager.Instance.SetNeedWaitUpdate(true);
                    Game.Manager.archiveMan.DiscardLocalArchiveNextLogin(errorCode);
                    Game.Instance.AbortRestart(I18N.Text("#SysComDesc187"), errorCode);
                }
                else if (state == GameNet.NetworkMan.State.TimeNotSync)
                {
                    GameUpdateManager.Instance.SetNeedWaitUpdate(true);
                    Game.Manager.archiveMan.DiscardLocalArchiveNextLogin(errorCode);
                    Game.Instance.AbortRestart(I18N.Text("#SysComDesc188"), errorCode);
                }
                else if (state == GameNet.NetworkMan.State.TokenError)
                {
                    Game.Instance.RestartGame();
                }
                else if (state == GameNet.NetworkMan.State.KickedOff)
                {
                    GameUpdateManager.Instance.SetNeedWaitUpdate(true);
                    Game.Manager.archiveMan.DiscardLocalArchiveNextLogin(errorCode);
                    Game.Instance.AbortRestart(I18N.Text("#SysComDesc189"), errorCode, "", true, I18N.Text("#SysComDesc517"));
                }
                else if (state == GameNet.NetworkMan.State.CommonQuit)
                {
                    GameUpdateManager.Instance.SetNeedWaitUpdate(true);
                    Game.Manager.archiveMan.DiscardLocalArchiveNextLogin(errorCode);
                    var content = ErrorCodeUtility.GetNoticeContentByErrorCodeType(errorCode);
                    Game.Instance.Abort(content, errorCode, isShowErrorCode : false);
                }
                else if (state == GameNet.NetworkMan.State.CommonRestart)
                {
                    GameUpdateManager.Instance.SetNeedWaitUpdate(true);
                    var content = ErrorCodeUtility.GetNoticeContentByErrorCodeType(errorCode);
                    Game.Instance.AbortRestart(content, errorCode, isShowErrorCode : false);
                }
                else if (state == GameNet.NetworkMan.State.NetWeakRestart)
                {
                    GameUpdateManager.Instance.SetNeedWaitUpdate(true);
                    Game.Instance.AbortRestart(I18N.Text("#SysComDesc227"), errorCode, I18N.Text("#SysComDesc376"), false);
                }
                else if (state == GameNet.NetworkMan.State.CommonRelogin)
                {
                    GameUpdateManager.Instance.SetNeedWaitUpdate(true);
                    Game.Manager.archiveMan.DiscardLocalArchiveNextLogin(errorCode);
                    var content = ErrorCodeUtility.GetNoticeContentByErrorCodeType(errorCode);
                    Game.Instance.AbortRestart(content, errorCode, isShowErrorCode : false);
                }
                else if (state == GameNet.NetworkMan.State.CommonReloginSDK)
                {
                    GameUpdateManager.Instance.SetNeedWaitUpdate(true);
                    Game.Manager.archiveMan.DiscardLocalArchiveNextLogin(errorCode);
                    var sdk = PlatformSDK.Instance.Adapter;
                    if (sdk.LoginType == AccountLoginType.Guest) {
                        var content = ErrorCodeUtility.GetNoticeContentByErrorCodeType(errorCode);
                        Game.Instance.AbortRestart(content, errorCode, isShowErrorCode : false);
                    }
                    else {
                        PlatformSDK.Instance.binding.ToGuest();
                    }
                }
                else if (state == GameNet.NetworkMan.State.UserSmallVersionBan)
                {
                    Game.Instance.Abort(I18N.Text("#SysComDesc709"), errorCode);
                }
                else if (state == GameNet.NetworkMan.State.UnknownError)
                {
                    var content = ErrorCodeUtility.GetNoticeContentByErrorCodeType(errorCode);
                    Game.Instance.Abort(content, errorCode, isShowErrorCode : false);
                }
            }
        }
    }
}