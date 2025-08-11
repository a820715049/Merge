/*
 * @Author: qun.chao
 * @Date: 2022-04-19 10:47:46
 */
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using centurygame;

namespace FAT.Platform {
    public abstract class PlatformSDKAdapter {
        public virtual string DeviceId => SystemInfo.deviceUniqueIdentifier;
        public abstract int ChannelId { get; }
        public abstract string SessionId { get; }
        public abstract string SessionKey { get; }
        public AccountLoginType LoginCache {
            get => (AccountLoginType)PlayerPrefs.GetInt(nameof(LoginCache), (int)AccountLoginType.Unknown);
            set => PlayerPrefs.SetInt(nameof(LoginCache), (int)value);
        }
        public AccountLoginType LoginType {
            get => (AccountLoginType)PlayerPrefs.GetInt(nameof(LoginType), (int)AccountLoginType.Guest);
            set => PlayerPrefs.SetInt(nameof(LoginType), (int)value);
        }
        public int LoginRetry {
            get => PlayerPrefs.GetInt(nameof(LoginRetry), 0);
            set => PlayerPrefs.SetInt(nameof(LoginRetry), value);
        }
        public string LoginId1 {
            get => PlayerPrefs.GetString(nameof(LoginId1), string.Empty);
            set => PlayerPrefs.SetString(nameof(LoginId1), value);
        }
        public bool LogoutSkip { get; set; }
        public Dictionary<AccountLoginType, AccountBindInfo> BindInfo { get; } = new();
        public bool BindInit { get; internal set; }
        //账号的删除状态 初始为Unkown状态 在SDK登录成功后会收到回调 设置成正确的状态
        public CGAccountRemoveGeneralStatus AccountRemoveStatus { get; set; } = CGAccountRemoveGeneralStatus.CGAccountUnkown;
        //可用于验证用户身份的社交列表
        public List<CGAccountType> ValidSocialList { get; } = new List<CGAccountType>();
        public bool SDKInit { get; internal set; }
        public virtual bool SDKLogin { get; private set; }
        public bool IAPInit { get; internal set; }
        public string PurchaseProcessing { get; internal set; }
        public Dictionary<string, IAPProductInfo> IAPProduct { get; } = new();
        public AccountProfile profile;

        public SDKError LastError { get; internal set; }
        public bool LoginSkip { get; internal set; }

        internal ActionOnce<bool, SDKError> OnSDKInit = new();
        internal ActionOnce<bool, SDKError> OnLogin = new();
        internal ActionOnce<bool, SDKError> OnLogout = new();
        internal ActionOnce<bool, SDKError> OnBindAccount = new();
        internal ActionOnce<bool, SDKError> OnBindInfo = new();
        internal ActionOnce<bool, SDKError> OnIAPInit = new();
        internal ActionOnce<List<string>, bool, SDKError> OnIAPRestore = new();
        internal Action<string, bool, SDKError> OnIAPPurchase;
        internal Action<string, bool, SDKError> OnUnhandledIAP;
        internal ActionOnce<bool, SDKError> OnShare = new();
        internal ActionOnce<string, bool, SDKError> OnShareLink = new();
        internal ActionOnce<bool, SDKError> OnSurvey = new();
        internal ActionOnce<bool, AccountProfile> OnProfile = new();
        internal ActionOnce<List<string>, bool, SDKError> OnGetGameFriends = new();
        internal ActionOnce<string, bool, SDKError> OnAskPermission = new();

        public void LogInfo(string msg) => Debug.Log($"{nameof(PlatformSDKAdapter)} {msg}");
        public void LogError(string msg) => Debug.LogError($"{nameof(PlatformSDKAdapter)} {msg}");

        public void LogToast(SDKError e_) => LogToast(e_.ErrorCode, e_.Message);
        public void LogToast(long code_, string msg_) {
            var msg = $"({code_}){msg_}";
            Game.Manager.commonTipsMan.ShowClientTips(msg);
        }

        public virtual Rect GetSafeArea() => Screen.safeArea;

        public virtual int GetSDKChannelId() {
            return Constant.kChannelFunplus;
        }

        public virtual IEnumerator InitSDK() {
            InitSDKSuccess();
            yield break;
        }
        public virtual void InitSDKSuccess() {
            SDKInit = true;
        }
        public virtual void InitSDKFail(long code) { }

        public virtual void Share(string content, ShareType type, Action<bool, SDKError> OnShare_) {
            OnShare_?.Invoke(false, null);
        }
        public abstract void ShareSuccess(string result);
        public abstract void ShareFail(long code_, string msg_);

        public virtual void UserData(AccountLoginType type_, Action<bool, AccountProfile> OnProfile_) {
            OnProfile_?.Invoke(false, default);
        }
        public abstract void UserDataSuccess(object data_);
        public abstract void UserDataFail(long code_, string msg_);

        public virtual void OpenSurvey(string id_, string param_, Action<bool, SDKError> OnSurvey_) {
            OnSurvey_?.Invoke(false, null);
        }
        public abstract void SurveySuccess(string msg_);
        public abstract void SurveyFail(long code_, string msg_);

        public abstract void SetLanguage(string lang);
        public abstract void ShowConversation();
        public abstract void ShowFAQ();
        public abstract void OpenUserCenter();
        public abstract void LogUserInfoUpdate(string serverId, string userId, string userName, string level, string vipLevel, bool paied);
        public abstract void TraceUser(string str);
        public abstract void TraceUserAdd(string str);
        public abstract void TrackEvent(string eventId, string param);
        public abstract void TrackAdjust(string eventId);
        public abstract void TrackFirebase(string eventName);
        public abstract string GetTrackDistinctId();
        public abstract string GetCountryCode();
        public abstract string GetDeviceModel();
        public abstract string GetDeviceName();
        public abstract string GetOSVersion();
        public virtual string GetOSName() {
#if UNITY_IPHONE
            return "iOS";
#else
            return "Android";
#endif
        }

        #region SDK广告接口

        public abstract bool IsAdsInited();
        public abstract bool IsSupportAds();
        public abstract void PlayRewardAds(string unitId);
        public abstract bool IsRewardAdsReady(string unitId);
        public abstract void LoadRewardAds(string unitId);
        public abstract void SetDisableB2BAdUnitId(string unitId);

        #endregion

        #region IAP

        public virtual IEnumerator InitIAP(string[] productIds, Action<string, bool, SDKError> OnUnhandledIAP_) {
            yield break;
        }

        public abstract void InitIAPSuccess(object productList);
        public abstract void InitIAPFail(long error, string msg);
        public abstract void CheckIAPRestore(Action<List<string>, bool, SDKError> OnIAPRestore_);
        public abstract void IAPRestoreSuccess(object productList);
        public virtual void Buy(Transaction transaction_, Action<string, bool, SDKError> WhenComplete_) {
            WhenComplete_?.Invoke(transaction_.productId, false, null);
        }

        public abstract void IAPBuySuccess(string productId);
        public abstract void IAPBuyFail(long error, string msg);

        public virtual string GetIAPChannel() {
            return
#if UNITY_IPHONE
            "appleiap";
#elif UNITY_ANDROID
            "googleiap";
#else
            "unknown";
#endif
        }

        #endregion IAP

        #region CDKey

        //cdkey
        public virtual bool CDKeyCanMakePurchases() => !string.IsNullOrEmpty(Game.Instance.appSettings?.sdkId);
        public virtual void CDKeyExchange(string code, string channel, string section, string throughCargo, string serverId, bool s) {
            // FAT_TODO
            // var jsonParam = new EL.SimpleJSON.JSONObject();
            // jsonParam["game_id"] = Game.Instance.appSettings.sdkId;
            // jsonParam["l"] = "zh";
            // jsonParam["platform"] = GetOSName() ?? "android";
            // jsonParam["channel"] = channel;
            // jsonParam["fpid"] = Game.Manager.networkMan.fpId;
            // jsonParam["section"] = section;
            // jsonParam["cdkey"] = code;
            // jsonParam["uid"] = Game.Manager.accountMan.uid.ToString();
            // jsonParam["through_cargo"] = throughCargo ?? "";
            // jsonParam["appservid"] = serverId;
            // // var root = Game.Instance.appSettings.sdkEnv == AppSettings.SDKEnvironment.Sandbox?
            // //              "http://cdkey-cn-endpoint-sandbox.campfiregames.cn":
            // //              "http://cdkey-cn-endpoint.campfiregames.cn";
            // EL.DianDianNetUtility.RequestMiddleProxyWithParamDefaultUrl("cdkey", "", jsonParam, Game.Instance.appSettings.sdkKey, (ret) =>
            // {
            //     var json = EL.SimpleJSON.JSON.Parse(ret);
            //     if (json == null || json["status"].IsNull)
            //     {
            //         EL.DebugEx.FormatWarning("PlatformSDKAdapter::CDKeyExchange ----> error 1 {0}", ret);
            //         Platform.PlatformSDK.Instance.OnExchangeError("服务器有问题，稍后再试", (long)GameErrorCode.HumanReadableError);
            //     }
            //     else
            //     {
            //         var retCode = json["status"].AsInt;
            //         if (retCode != 1)
            //         {
            //             EL.DebugEx.FormatWarning("PlatformSDKAdapter::CDKeyExchange ----> error 2 {0}", ret);
            //             Platform.PlatformSDK.Instance.OnExchangeError(json["reason"], ErrorCodeUtility.ConvertToCommonCode(retCode, ErrorCodeType.SDKError));
            //         }
            //         else
            //         {
            //             EL.DebugEx.FormatInfo("PlatformSDKAdapter::CDKeyExchange ----> success");
            //             Platform.PlatformSDK.Instance.OnExchangeSuccess(code, throughCargo);
            //         }
            //     }
            // }, (err, errCode) =>
            // {
            //     EL.DebugEx.FormatWarning("PlatformSDKAdapter::CDKeyExchange ----> error 3 {0},{1}", err, errCode);
            //     Platform.PlatformSDK.Instance.OnExchangeError(err, errCode);
            // });
        }

        #endregion CDKey

        #region Account

        public virtual IEnumerator Login(AccountLoginType type_, Action<bool, SDKError> OnLogin_) {
            SDKLogin = true;
            LoginType = type_;
            OnLogin_?.Invoke(true, null);
            return null;
        }

        public virtual void RetryLogin(long code_) { }

        public virtual void LoginSuccess(object session) {
            ResetAskLogout();
        }

        public virtual void LoginFail(long error, string msg) { }

        public virtual IEnumerator Logout() {
            SDKLogin = false;
            return null;
        }

        public virtual void LogoutSuccess() {
            ResetAskLogout();
        }

        public virtual void BindAccount(AccountLoginType type, Action<bool, SDKError> OnBindAccount_, bool switch_ = false) {
            OnBindAccount_?.Invoke(false, null);
        }

        public abstract void RequestAccountRemovalStatus();
        public abstract void BindAccountSuccess(object session);
        public abstract void BindAccountFail(long error, string msg);
        public virtual bool LoginAvailable(AccountLoginType loginType) => false;

        public virtual void GetBindInfo(Action<bool, SDKError> OnBindInfo_) {
            OnBindInfo_?.Invoke(false, null);
        }

        public virtual void GetBindInfoSuccess(object detail_) { }
        public virtual void GetBindInfoFail(long error) {
            SDKLogin = true;
        }

        #endregion Account

        #region AntiAddiction

        public virtual int GetAge() {
            return -1;
        }

        public virtual IEnumerator GuestTimeLimit() {
            yield return null;
        }

        public virtual void RealNameVerification() { }

        protected enum AntiAddictionResult {
            NoError,
            NeedBind,
            NeedLogout
        }

        private bool mCallbackAskToLogout = false;
        internal void ResetAskLogout() => mCallbackAskToLogout = false;

        protected AntiAddictionResult OnAntiAddictioncCallable(bool isPlayable, bool isHoliday, long duration, string humanReadableError, long errorCode) {
            Debug.LogFormat("onAntiAddictioncCallable: {0}, {1}, {2}, {3}, {4}", isPlayable, isHoliday, duration, errorCode, humanReadableError);
            if (isPlayable) {
                // 这两个参数存在 当成未成年处理
                if (GetAge() < 18) {
                    // FindGameProxy.TryResolve(FindGameProxy.RequestType.RT_BRIDGE_SDK_SHOW_REMAIN_PLAYTIME, (duration, true, isHoliday));
                }
                Debug.Log("onAntiAddictioncCallable: age is " + GetAge());
            }
            else {
                Debug.Log("onAntiAddictioncCallable errorCode:" + errorCode);
                switch (errorCode) {
                    case 2502://年龄不超过18周岁，本时间段不能上线
                        break;
                    case 2503://未成年防沉迷提示: 玩太久了，该休息了
                        break;
                    case 2504://未成年防沉迷节假日提示: 也别玩太久了
                        break;
                    case 2305://需要用户绑定
                        // 这种情况好像不必退出 实名认证如果通过应该可以继续玩
                        RealNameVerification();
                        return AntiAddictionResult.NeedBind;
                    case 2306://游客用户，超时
                        // dreammerge版不支持纯游客登入 尝试不弹出游客限制
                        // DDOneSDK.DDOneSDK.Instance.ddOpenRunSupportedMethod("showGuestLimitedPlayWindow", "com.funplus.sdk.account.FunplusAccount");
                        break;
                }

                // => 强制登出
                // 此处是登出 让玩家确认后再弹窗

                if (!mCallbackAskToLogout) {
                    // 此标记的作用是避免同时出现多个登出弹窗
                    mCallbackAskToLogout = true;
                    // error log for tga
                    Debug.LogErrorFormat("[dd] onAntiAddictioncCallable: {0}, {1}, {2}, {3}, {4}", isPlayable, isHoliday, duration, errorCode, humanReadableError);
                    // FindGameProxy.TryResolve(FindGameProxy.RequestType.RT_BRIDGE_SDK_FORCE_LOGOUT, (humanReadableError, errorCode));
                }
                return AntiAddictionResult.NeedLogout;

            }
            return AntiAddictionResult.NoError;
        }

        #endregion AntiAddiction

        #region DeepLink

        public virtual void ShareLink(string payload_, Action<string, bool, SDKError> WhenComplete_, bool share_ = true) {
            WhenComplete_?.Invoke(null, false, new(-1, "unsupported"));
        }
        public virtual void ShareLinkSuccess(string link_) {}
        public virtual void ShareLinkFail(long code_, string msg_) {}

        public virtual void LinkPayload(Action<string, bool, SDKError> WhenComplete_) {
            WhenComplete_?.Invoke(null, true, null);
        }

        public virtual void LinkPayloadSuccess(string payload_) {}
        public virtual void LinkPayloadFail(long code_, string msg_) {}

        #endregion DeepLink

        #region Facebook

        public virtual bool IsNeedLimitedLogin() { return false; }
        public virtual bool HasPermission(string permission) { return false; }
        public virtual void AskPermission(string permission, Action<string, bool, SDKError> onAskPermission) { }
        public virtual void AskPermissionSuccess(string permission) { }
        public virtual void AskPermissionFail(string permission, long code, string msg) { }
        
        public virtual void GetGameFriends(Action<List<string>, bool, SDKError> onGetGameFriends) { }
        public virtual void GetGameFriendsSuccess(object friends) { }
        public virtual void GetGameFriendsError(long code, string msg) { }

        #endregion
    }
}