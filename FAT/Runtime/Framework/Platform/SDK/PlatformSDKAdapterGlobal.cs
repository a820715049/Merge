/*
 * @Author: qun.chao
 * @Date: 2022-04-19 10:47:46
 */
using System.Collections.Generic;
using System.Collections;
using System;
using centurygame;
using EL;

namespace FAT.Platform {
    public class PlatformSDKAdapterGlobal : PlatformSDKAdapter {
        public BridgeGlobal SDKBridge { get; }

        public override string DeviceId => CGSdkUtils.Instance.GetIDFA();
        public override int ChannelId => -1;
        public override string SessionId => !string.IsNullOrEmpty(Session?.Fpid) ? Session.Fpid : "Unknown";
        public override string SessionKey => !string.IsNullOrEmpty(Session?.SessionKey) ? Session.SessionKey : "Unknown";
        public override bool SDKLogin => Session != null;
        public CGSession Session { get; private set; }
        public CGAccountType AccountType => Session != null ? Session.AccountType : CGAccountType.FPAccountTypeUnknown;

        public PlatformSDKAdapterGlobal() {
            SDKBridge = new() { Adapter = this };
        }

        public override IEnumerator InitSDK() {
            if (SDKInit) {
                yield break;
            }
            var wait = true;
            OnSDKInit.Setup((r_, e_) => {
                wait = false;
            });
            CGSdkUtils.Instance.SetUtilDelegate(SDKBridge);
            CGSdk.Instance.SetDelegate(SDKBridge);
            CGRemoveAccount.Instance.SetRemoveAccountDelegate(SDKBridge);   //在SDK初始化之前设置删除账号的代理
            CGAdjust.Instance.SetDelegate(SDKBridge);
            CGSdk.Instance.Install();
            while (wait) yield return null;
        }

        public override void InitSDKSuccess() {
            SDKInit = true;
            OnSDKInit?.Invoke(true, null);
            _CheckIDFA();
        }

        public override void InitSDKFail(long code) {
            OnSDKInit?.Invoke(false, null);
        }

        public override void SetLanguage(string lang) {
            switch (lang) {
                case "en": CGSdk.Instance.setGameLanguage(CGLanguage.English); break;
                case "ja": CGSdk.Instance.setGameLanguage(CGLanguage.Japanese); break;
                case "ko": CGSdk.Instance.setGameLanguage(CGLanguage.Korean); break;
                case "fr": CGSdk.Instance.setGameLanguage(CGLanguage.French); break;
                case "de": CGSdk.Instance.setGameLanguage(CGLanguage.German); break;
                case "zh_hans_cn": CGSdk.Instance.setGameLanguage(CGLanguage.SimplifiedChinese); break;
                case "zh_hant_tw": CGSdk.Instance.setGameLanguage(CGLanguage.TraditionalChinese); break;
                case "es": CGSdk.Instance.setGameLanguage(CGLanguage.Spanish); break;
                case "pt": CGSdk.Instance.setGameLanguage(CGLanguage.Portuguese); break;
                case "tr": CGSdk.Instance.setGameLanguage(CGLanguage.Turkish); break;
                default: CGSdk.Instance.setGameLanguage(CGLanguage.English); break;
            }
        }

        public override void LogUserInfoUpdate(string serverId, string userId, string userName, string level, string vipLevel, bool paied)
            => CGSdk.Instance.LogUserInfoUpdate(serverId, userId, userName, level, vipLevel, paied);

        public override void OpenUserCenter()
            => CGAccount.Instance.ShowUserCenter();

        public override void ShowConversation()
            => CGHelpshift.Instance.ShowConversation();

        public override void ShowFAQ()
            => CGHelpshift.Instance.ShowFAQs();

        public override string GetTrackDistinctId()
            => CGThinkingGame.Instance.GetDistinctId();

        public override string GetCountryCode()
            => CGSdkUtils.Instance.GetCountry();

        public override string GetDeviceModel()
            => CGSdkUtils.Instance.GetDeviceType();

        public override string GetDeviceName()
            => CGSdkUtils.Instance.GetDeviceName();

        public override string GetOSVersion()
            => CGSdkUtils.Instance.GetOsVersion();

        private void _CheckIDFA() {
            // 不省略或缺idfa的流程
            // // 已有IDFA 不弹框
            // if (CGInstance.HasIDFA())
            //     return;

            var status = CGSdkUtils.Instance.GetIDFATrackingAuthorization();

            // 没有必要
            if (status == CGIDFAAuthorizationStatus.SystemLoweriOS14 || status == CGIDFAAuthorizationStatus.Authorized) {
                return;
            }
            else if (status == CGIDFAAuthorizationStatus.Denied) {
                // 无法弹
                // TODO: 引导用户
                return;
            }
            else {
                // 可以请求弹框
                CGSdkUtils.Instance.RequestIDFATrackingAuthorization();
            }
        }

        #region SDK广告接口

        //目前直接采用海外版SDK文档的使用方式 暂时无需按平台区别覆写 https://podd-docs.diandian.info/client/integration/unity/10_advertising.html
        //本项目默认 CGAdvertisingType(广告平台类型)使用CGMax  CGAdvertisingDuty(广告业务)使用RewardAd， 不使用其他类型

        public override bool IsAdsInited() {
#if UNITY_EDITOR
            return true;
#else
            return CGAdvertising.Instance.IsModuleInstalled(CGAdvertising.CGAdvertisingType.CGMax);
#endif
        }

        public override bool IsSupportAds() {
#if UNITY_EDITOR
            return true;
#else
            return CGAdvertising.Instance.IsModuleInstalled(CGAdvertising.CGAdvertisingType.CGMax);
#endif
        }

        public override void PlayRewardAds(string unitId) {
#if !UNITY_EDITOR
            CGAdvertising.Instance.ShowRewardAd(CGAdvertising.CGAdvertisingType.CGMax, unitId);
#endif
        }

        public override bool IsRewardAdsReady(string unitId) {
#if UNITY_EDITOR
            return true;
#else
            return CGAdvertising.Instance.HasAdLoaded(CGAdvertising.CGAdvertisingType.CGMax, CGAdvertising.CGAdvertisingDuty.RewardAd, unitId);
#endif
        }

        public override void LoadRewardAds(string unitId) {
#if !UNITY_EDITOR
            CGAdvertising.Instance.LoadRewardAd(CGAdvertising.CGAdvertisingType.CGMax, unitId);
#endif
        }

        public override void SetDisableB2BAdUnitId(string unitId) {
#if !UNITY_EDITOR
            CGSdkUtils.Instance.setConfigParams(CGDataConfigParams.DISABLE_B2B_AD_UNIT_IDS, unitId);
#endif
        }

        #endregion

        #region IAP

        public override IEnumerator InitIAP(string[] productIds, Action<string, bool, SDKError> OnUnhandledIAP_) {
            if (IAPInit) {
                yield break;
            }
            var wait = true;
            OnIAPInit.Setup((r_, e_) => {
                wait = false;
            });
            OnUnhandledIAP = OnUnhandledIAP_;
            CGPayment.Instance.SetDelegate(SDKBridge);
            CGPayment.Instance.StartHelper(productIds);
            while (wait) yield return null;
        }

        public override void InitIAPSuccess(object productList_) {
            var productList = (List<CGProduct>)productList_;
            for (var i = 0; i < productList.Count; i++) {
                var p = productList[i];
                var ppInfo = new IAPProductInfo {
                    Description = p.Description,
                    FormattedPrice = p.FormattedPrice,
                    Id = p.ProductId,
                    Price = p.Price,
                    PriceCurrencyCode = p.PriceCurrencyCode,
                    Title = p.Title
                };
                IAPProduct[p.ProductId] = ppInfo;
            }
            IAPInit = true;
            OnIAPInit.Invoke(true, null);
            CGPayment.Instance.TryCompleteUnPayOrder();
        }

        public override void InitIAPFail(long error, string msg) {
            LastError = new(error, msg);
            IAPInit = false;
            OnIAPInit.Invoke(false, LastError);
        }

        public override void CheckIAPRestore(Action<List<string>, bool, SDKError> OnIAPRestore_) {
            if (IAPInit) {
                OnIAPRestore.Setup(OnIAPRestore_);
                // 检查购买
                CGPayment.Instance.checkOngoingSubscriptOrders();
            }
            else {
                OnIAPRestore_?.Invoke(null, false, new SDKError(-1, "iap uninitialized, can't restore purchase"));
            }
        }

        public override void IAPRestoreSuccess(object productList_) {
            if (!OnIAPRestore.Valid) {
                LogError("IAP restore callback triggered with no receiver");
                return;
            }
            var productList = (List<CGSubscriptProduct>)productList_;
            var itemList = new List<string>();
            for (var i = 0; i < productList.Count; i++) {
                var prodId = productList[i].productId;
                itemList.Add(prodId);
            }
            OnIAPRestore.Invoke(itemList, true, null);
        }

        public override void Buy(Transaction transaction_, Action<string, bool, SDKError> WhenComplete_) {
            var productId = transaction_.productId;
            if (!IAPInit) {
                var error = new SDKError() { Message = "iap uninitialized, can't make purchase", ErrorCode = -1 };
                WhenComplete_?.Invoke(productId, false, error);
                LogError($"IAP purchase for {productId} fail because iap is not initialized");
                return;
            }
            if (PurchaseProcessing != null) {
                LogError($"IAP ongoing purchase for {PurchaseProcessing} exists");
                var error = new SDKError() { Message = "already processing purchase", ErrorCode = (long)GameErrorCode.Duplicated };
                WhenComplete_?.Invoke(productId, false, error);
                return;
            }
            var serverId = transaction_.serverId;
            var cargo = transaction_.cargo;
            OnIAPPurchase = WhenComplete_;
            PurchaseProcessing = productId;
            CGPayment.Instance.Buy(productId, serverId.ToString(), cargo);
            LogInfo($"IAP purchase request {productId} server={serverId} cargo={cargo}");
        }

        public override void IAPBuySuccess(string productId) {
            IAPBuyResult(productId, true, null);
        }

        public override void IAPBuyFail(long error, string msg) {
            LastError = new(error, msg);
            IAPBuyResult(PurchaseProcessing, false, LastError);
        }

        public void IAPBuyResult(string productId, bool result, SDKError error) {
            var p = PurchaseProcessing;
            var callback = OnIAPPurchase ?? OnUnhandledIAP;
            if (p != null && productId != p) {
                LogError($"iap response, but product mismatch expect:{p} receive:{productId} ");
            }
            PurchaseProcessing = null;
            OnIAPPurchase = null;
            if (callback == null) {
                LogError($"iap unhandled. receive:{productId} result:{result} error:{error}");
            }
            callback?.Invoke(productId, result, error);
        }

        #endregion IAP

        #region Tracking

        public override void TrackEvent(string eventName, string properties) {
            CGThinkingGame.Instance.TraceEvent(eventName, properties);
        }

        public override void TrackAdjust(string eventName) {
            CGAdjust.Instance.trackEvent(eventName);
        }

        public override void TrackFirebase(string eventName) {

        }

        public override void TraceUser(string str) {
            CGThinkingGame.Instance.UserSet(CGUserTraceType.USER_SET, str);
        }

        public override void TraceUserAdd(string str) {
            CGThinkingGame.Instance.UserSet(CGUserTraceType.USER_ADD, str);
        }

        #endregion Tracking

        #region Account

        public CGAccountType Map(AccountLoginType t_, CGAccountType d_ = CGAccountType.FPAccountTypeUnknown)
            => t_ switch {
                AccountLoginType.Facebook => CGAccountType.FPAccountTypeFacebook,
                AccountLoginType.Google => CGAccountType.FPAccountTypeGoogle,
                AccountLoginType.Apple => CGAccountType.FPAccountTypeApple,
                AccountLoginType.Wechat => CGAccountType.FPAccountTypeWechat,
                AccountLoginType.Guest => CGAccountType.FPAccountTypeExpress,
                _ => d_,
            };
        public AccountLoginType Map(CGAccountType t_, AccountLoginType d_ = AccountLoginType.Unknown)
            => t_ switch {
                CGAccountType.FPAccountTypeFacebook => AccountLoginType.Facebook,
                CGAccountType.FPAccountTypeGoogle => AccountLoginType.Google,
                CGAccountType.FPAccountTypeApple => AccountLoginType.Apple,
                CGAccountType.FPAccountTypeWechat => AccountLoginType.Wechat,
                CGAccountType.FPAccountTypeExpress => AccountLoginType.Guest,
                _ => d_,
            };
        
        public override IEnumerator Login(AccountLoginType type_, Action<bool, SDKError> OnLogin_) {
            var wait = true;
            OnLogin.Setup((r_, e_) => {
                wait = false;
                OnLogin_?.Invoke(r_, e_);
            });
            //请求登录时重置一下账号删除状态 编辑器下默认始终为normal
#if UNITY_EDITOR
            AccountRemoveStatus = CGAccountRemoveGeneralStatus.CGAccountNormal;
#else
            AccountRemoveStatus = CGAccountRemoveGeneralStatus.CGAccountUnkown;
#endif
            var sdkLogin = CGAccount.Instance.IsUserLoggedIn();
            var type = type_ switch {
                AccountLoginType.Cache or AccountLoginType.Unknown when sdkLogin
                    => CGAccountType.FPAccountTypeCache,
                AccountLoginType.Cache when !sdkLogin
                    => Map(LoginCache, CGAccountType.FPAccountTypeExpress),
                _ when sdkLogin && LoginType == LoginCache => CGAccountType.FPAccountTypeCache,
                _ => Map(type_, CGAccountType.FPAccountTypeExpress),
            };
            LogInfo($"login request {type_}->{type} {sdkLogin} {LoginType} {LoginCache}");
            CGAccount.Instance.Login(type);
            while (wait) yield return null;
        }

        public override void RetryLogin(long code_) {
            var v = LoginRetry;
            if (v > 2) {
                GameProcedure.CancelWhenError(I18N.Text("#SysComDesc765"), GameProcedure.RestartGame);
                return;
            }
            LogInfo($"login retry");
            LoginRetry = v + 1;
            CGAccount.Instance.Logout();
            if (code_ != 1207) LoginType = AccountLoginType.Unknown;
            GameProcedure.RestartGame();
        }

        private void LoginSession(CGSession session_) {
            Session = session_;
            LoginType = Map(AccountType);
            LoginCache = LoginType;
            LoginRetry = 0;
            if (Session.AccountType == CGAccountType.FPAccountTypeExpress) LoginId1 = SessionId;
            LogInfo($"login {SessionId} {AccountType} {LoginType} {LoginId1}");
        }

        public override void LoginSuccess(object session) {
            ResetAskLogout();
            LoginSession((CGSession)session);
            CGSdk.Instance.startGameServer();
            RefreshBindInfo();
            OnLogin?.Invoke(true, null);
        }

        public override void LoginFail(long error, string msg) {
            LastError = new(error, msg);
            OnLogin?.Invoke(false, LastError);
        }

        public override IEnumerator Logout() {
            // 如果当前未login，登出流程可能是由sdk发起
            // 则此处不应再尝试logout
            // 否则无法收到回调 卡死流程
            if (!CGAccount.Instance.IsUserLoggedIn() || LogoutSkip) {
                LogInfo($"logout skip {CGAccount.Instance.IsUserLoggedIn()} {LogoutSkip}");
                LogoutSkip = false;
                yield break;
            }
            LogInfo($"logout {SessionId} {AccountType}");
            var wait = true;
            // 一般由游戏内发起logout 此处调用登出接口 并等待回调结果
            OnLogout.Setup((r_, error_) => {
                wait = false;
                // sdk退出登录失败，游戏还是要退出
                if (r_) {
                    LogInfo("logout success.");
                }
                else {
                    LogInfo($"logout error:({error_.ErrorCode}){error_.Message}.");
                }
            });
            Session = null;
            CGSdk.instance.endGameServer();
            CGAccount.Instance.Logout();
            while (wait) yield return null;
        }

        public override void LogoutSuccess() {
            ResetAskLogout();
            OnLogout.Invoke(true, null);
        }

        public override void RequestAccountRemovalStatus() {
            CGRemoveAccount.Instance.RequestAccountRemovalStatus();
        }

        public override void BindAccount(AccountLoginType type, Action<bool, SDKError> OnBindAccount_, bool change_ = false) {
            var tt = Map(type);
            LogInfo($"BindAccount {type} {tt}");
            if (tt == CGAccountType.FPAccountTypeUnknown) {
                OnBindAccount_?.Invoke(false, new(-1, "unsupported"));
                return;
            }
            OnBindAccount.Setup(OnBindAccount_);
            if (change_) CGAccount.Instance.BindOrLogin(tt);
            else CGAccount.Instance.BindAccount(tt);
            #if UNITY_EDITOR
            if (change_) {
                BindInfo[type] = new("test", "test");
                OnBindAccount_?.Invoke(true, null);
            }
            else OnBindAccount_?.Invoke(false, new(1109, "already bound"));
            // OnBindAccount_?.Invoke(false, new(-1, "unsupported"));
            #endif
        }

        public override void BindAccountSuccess(object session_) {
            LoginSession((CGSession)session_);
            OnBindAccount.Invoke(true, null);
            RefreshBindInfo();
        }

        public override void BindAccountFail(long error, string msg) {
            LastError = new(error, msg);
            OnBindAccount.Invoke(false, LastError);
        }

        public override bool LoginAvailable(AccountLoginType loginType) {
            return loginType switch {
                AccountLoginType.Apple => CGAccount.Instance.IsSigninAppleAvailable(),
                _ => true,
            };
        }

        public void RefreshBindInfo() {
            BindInit = false;
            CGAccount.Instance.GetBindInfo();
            #if UNITY_EDITOR
            BindInit = true;
            #endif
        }

        public override void GetBindInfo(Action<bool, SDKError> OnBindInfo_) {
            if (BindInit) {
                OnBindInfo_?.Invoke(true, null);
                return;
            }
            OnBindInfo.Setup(OnBindInfo_);
            CGAccount.Instance.GetBindInfo();
        }

        public override void GetBindInfoSuccess(object detail_) {
            var list = (List<CGSocialInfo>)detail_;
            BindInfo.Clear();
            foreach(var n in list) {
                DebugEx.Info($"{n.AccountType}:{n.SnsId} {n.SnsName}");
                BindInfo[Map(n.AccountType)] = new(n.SnsId, n.SnsName);
            }
            BindInit = true;
            OnBindInfo.Invoke(true, null);
            AccountDelectionUtility.RequestValidSocialList();//获取可用于身份验证的社交列表
        }

        public override void GetBindInfoFail(long error) {
            OnBindInfo.Invoke(false, new(error, "bind fail"));
        }

        public override void UserData(AccountLoginType type_, Action<bool, AccountProfile> OnProfile_) {
            OnProfile.Setup(OnProfile_);
            switch (type_) {
                case AccountLoginType.Facebook: CGFacebook.Instance.GetUserData(); break;
                default: {
                    LogError($"user data for {type_} not implemented");
                    OnProfile.Cancel();
                } break;
            }
        }

        public override void UserDataSuccess(object data_) {
            var d = (CGSocialUser)data_;
            profile = new(AccountLoginType.Facebook, d);
            OnProfile.Invoke(true, profile);
        }

        public override void UserDataFail(long code_, string msg_) {
            OnProfile.Invoke(false, default);
        }

        #endregion Account

        #region share

        public override void Share(string content, ShareType type, Action<bool, SDKError> OnShare_) {
            OnShare.Setup(OnShare_);
            if ((type & ShareType.Image) > 0) {
                CGFacebook.Instance.ShareImage(content);
                return;
            }
            if ((type & ShareType.Link) > 0) {
                CGFacebook.Instance.ShareLink(content);
                return;
            }
            OnShare.Invoke(false, new(-1, null));
            DebugEx.Warning($"invalid share type:{type}");
        }

        public override void ShareSuccess(string result) {
            OnShare.Invoke(true, null);
        }

        public override void ShareFail(long code_, string msg_) {
            LastError = new(code_, msg_);
            OnShare.Invoke(false, LastError);
        }

        #endregion share

        #region Survey

        public override void OpenSurvey(string id_, string param_, Action<bool, SDKError> OnSurvey_) {
            OnSurvey.Setup(OnSurvey_);
            CGSurvey.Instance.showSurvey(id_, param_);
            #if UNITY_EDITOR
            OnSurvey.Invoke(true, null);
            #endif
        }

        public override void SurveySuccess(string msg_) {
            OnSurvey.Invoke(true, null);
        }

        public override void SurveyFail(long code_, string msg_) {
            LastError = new(code_, msg_);
            OnSurvey.Invoke(false, LastError);
        }

        #endregion Survey

        #region DeepLink

        public override void ShareLink(string payload_, Action<string, bool, SDKError> WhenComplete_, bool share_ = true) {
            OnShareLink.Setup(WhenComplete_);
            var bean = new CGShortDynamicLinkBean();
            bean.setPayload(payload_);
            if (share_) {
                CGAdjust.Instance.shareToMessenger(bean);
            }
            else {
                CGAdjust.Instance.buildShortDynamicLink(bean);
            }
        }

        public override void ShareLinkSuccess(string link_) {
            OnShareLink.Invoke(link_, true, null);
        }
        public override void ShareLinkFail(long code_, string msg_) {
            LastError = new(code_, msg_);
            OnShareLink.Invoke(null, false, LastError);
        }

        public override void LinkPayload(Action<string, bool, SDKError> WhenComplete_) {
            OnShareLink.Setup(WhenComplete_);
            CGAdjust.Instance.requestPayload();
        }

        public override void LinkPayloadSuccess(string payload_) {
            OnShareLink.Invoke(payload_, true, null);
        }
        public override void LinkPayloadFail(long code_, string msg_) {
            LastError = new(code_, msg_);
            OnShareLink.Invoke(null, false, LastError);
        }

        #endregion DeepLink
        
        #region Facebook

        public override bool IsNeedLimitedLogin()
        {
            return CGFacebook.Instance.IsNeedLimitedLogin();
        }
        
        public override bool HasPermission(string permission)
        {
            return CGFacebook.Instance.HasPermission(permission);
        }
        
        public override void AskPermission(string permission, Action<string, bool, SDKError> onAskPermission)
        {
            OnAskPermission.Setup(onAskPermission);
            CGFacebook.Instance.AskPermission(permission);
        }

        public override void AskPermissionSuccess(string permission)
        {
            OnAskPermission.Invoke(permission, true, null);
        }

        public override void AskPermissionFail(string permission, long code, string msg)
        {
            LastError = new(code, msg);
            OnAskPermission.Invoke(permission, false, LastError);
        }
        
        public override void GetGameFriends(Action<List<string>, bool, SDKError> onGetGameFriends)
        {
            OnGetGameFriends.Setup(onGetGameFriends);
            CGFacebook.Instance.GetGameFriends();
        }

        public override void GetGameFriendsSuccess(object friends)
        {
            var idList = new List<string>();
            if (friends is List<CGFBFriend> fbFriendList)
            {
                foreach (var friend in fbFriendList)
                {
                    idList.Add(friend.Id);
                }
            }
            OnGetGameFriends.Invoke(idList, true, null);
        }

        public override void GetGameFriendsError(long code, string msg)
        {
            LastError = new(code, msg);
            OnGetGameFriends.Invoke(null, false, LastError);
        }

        #endregion
    }
}