using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using centurygame;
using Newtonsoft.Json.Linq;

namespace FAT.Platform {
    public class BridgeGlobal :
        CGSdkUtils.IUtilDelegate,
        CGSdk.IDelegate,
        CGRemoveAccount.IDelegate,
        CGAccount.IDelegate,
        CGHelpshift.IDelegate,
        CGPayment.IDelegate,
        CGSurvey.IDelegate,
        CGCDKey.IDelegate,
        CGAdjust.IDelegate,
        CGFacebook.IDelegate,
        CGAdvertising.ICGAdvertisingCallback {
        public PlatformSDKAdapterGlobal Adapter { get; set; }

        #region CGSdk.IDelegate

        void CGSdk.IDelegate.OnSdkInstallError(CGError error) {
            Adapter.LogError($"OnSdkInstallError: {error.ToJsonString()}");
            Adapter.InitSDKFail(error.GetErrorCode());
        }

        void CGSdk.IDelegate.OnSdkInstallSuccess(string config) {
            Adapter.LogInfo("OnSdkInstallSuccess");

            CGAccount.Instance.SetDelegate(this);
            CGHelpshift.Instance.SetDelegate(this);
            // sdk v5.8.3 在没有广告配置时不能设置delegate 会导致启动阶段黑屏(ios)
            CGAdvertising.Instance.SetDelegate(this);
            CGSurvey.Instance.SetDelegate(this);
            CGFacebook.Instance.SetDelegate(this);

            Adapter.InitSDKSuccess();
        }

        #endregion

        #region CGAccount.IDelegate
        void CGAccount.IDelegate.OnLoginSuccess(CGSession session) {
            Adapter.LogInfo($"OnLoginSuccess session:{session.ToJsonString()}");
            Adapter.LoginSuccess(session);
        }

        void CGAccount.IDelegate.OnLoginError(CGError error) {
            Adapter.LogError($"OnLoginError: {error.ToJsonString()}");
            Adapter.LoginFail(error.GetErrorCode(), error.GetExtra());
        }

        void CGAccount.IDelegate.OnResetPasswordSuccess(string fpid) {}
        void CGAccount.IDelegate.OnResetPasswordError(CGError error) {}

        void CGAccount.IDelegate.OnLogout() {
            Adapter.LogInfo("OnLogout");
            Adapter.LogoutSuccess();
        }

        void CGAccount.IDelegate.OnBindAccountSuccess(CGSession session) {
            Adapter.LogInfo($"OnBindAccountSuccess {session.ToJsonString()}");
            Adapter.BindAccountSuccess(session);
        }

        void CGAccount.IDelegate.OnBindAccountError(CGError error) {
            Adapter.LogError($"OnBindAccountError: {error.ToJsonString()}");
            Adapter.BindAccountFail(error.GetErrorCode(), error.GetExtra());
        }

        void CGAccount.IDelegate.OnCloseUserCenter() {
            Adapter.LogInfo("OnCloseUserCenter");
        }

        void CGAccount.IDelegate.OnGetBindInfoSuccess(string message, List<CGSocialInfo> socialDetail, string email) {
            Adapter.LogInfo($"OnGetBindInfoSuccess {message} {email} {socialDetail.Count}");
            Adapter.GetBindInfoSuccess(socialDetail);
        }

        void CGAccount.IDelegate.OnGetBindInfoError(CGError error) {
            Adapter.LogError($"OnGetBindInfoError {error.ToJsonString()}");
            Adapter.GetBindInfoFail(error.GetErrorCode());
        }

        void CGAccount.IDelegate.OnBindOrLoginSuccess(bool isBind, CGSession session) {
            Adapter.LogInfo($"OnBindOrLoginSuccess {isBind}, {session.ToJsonString()}");
            Adapter.BindAccountSuccess(session);
        }

        void CGAccount.IDelegate.OnBindOrLoginError(CGError error) {
            Adapter.LogError($"OnBindOrLoginError {error.ToJsonString()}");
            Adapter.BindAccountFail(error.GetErrorCode(), error.GetExtra());
        }

        void CGAccount.IDelegate.OnBindOrLoginWithAgainConfirm(string fpid) {
            Adapter.LogInfo($"OnBindOrLoginWithAgainConfirm {fpid}");
        }

        void CGAccount.IDelegate.OnResetMobilePasswordError(CGError error) {}
        void CGAccount.IDelegate.OnResetMobilePasswordSuccess(string message) {}
        void CGAccount.IDelegate.OnSendSmsCodeError(CGError error) {}
        void CGAccount.IDelegate.OnSendSmsCodeSuccess() {}
        void CGAccount.IDelegate.OnSwitchPGSAccountStatus(CGSwitchAccountStatus status, CGAccountType type, bool needLogOut, string currentFpid, string toFpid) {}
        void CGAccount.IDelegate.OnCountryCallingCodeError(CGError error) {}
        void CGAccount.IDelegate.OnCountryCallingCodeSuccess(string message) {}
        void CGAccount.IDelegate.OnUserCenterOpenFailed() {}
        void CGAccount.IDelegate.OnOpenUserCenterErrorWithDeletedUser(CGError error, string message) {}
        void CGAccount.IDelegate.OnNeedVIPMAllAuthorization() {}
        void CGAccount.IDelegate.OnAuthorizeVipMallUserCallback(CGError error) {}

        #endregion

        #region CGRemoveAccount.IDelegate 删除账号相关
        
        //https://centurygames.yuque.com/cplatform/tfvrxa/hsgtwt9z8w1gtr7f?singleDoc#
        //fpid : 当前玩家的 fpid
        //error : 接口调用的错误对象，error 为空时表示调用成功。 error 不为空，可根据错误码确认错误的原因。
        //accountRemoveGeneralStatus和 accountRemoveStatus 都表示账号状态，项目组按照需求选择其一使用即可 !
        //1. 如果游戏只关注 正常，待删除， 已删除 这三个状态，那么游戏开发者只需要获取 CGAccountRemoveGeneralStatus 类型的状态即可满足需求。
        //2. 如果游戏需要关于账号更详细的"删除状态"，比如 "待审核" 以及未来可能扩充的状态，则需要获取 int类型的 accountRemoveStatus，完成相应业务需求

        //获取账号 "删除状态" 回调
        void CGRemoveAccount.IDelegate.OnRequestAccountRemovalStatusCallback(string fpid, CGAccountRemoveGeneralStatus accountRemoveGeneralStatus, int accountRemoveStatus, CGError error)
        {
            if (error != null)
            {
                Adapter.LogError($"[CGRemoveAccount]: OnRequestAccountRemovalStatusCallback, fpid = {fpid}, error = {error.ToJsonString()}");
            }
            else
            {
                Adapter.LogInfo($"[CGRemoveAccount]: OnRequestAccountRemovalStatusCallback, fpid = {fpid}, status = {accountRemoveGeneralStatus}");
                Adapter.AccountRemoveStatus = accountRemoveGeneralStatus;
            }
        }

        //撤销 ”账号删除“ 回调
        void CGRemoveAccount.IDelegate.OnRevokeAccountRemovalCallback(string fpid, CGAccountRemoveGeneralStatus accountRemoveGeneralStatus, int accountRemoveStatus, CGError error)
        {
            bool isSucc = false;
            if (error != null)
            {
                Adapter.LogError($"[CGRemoveAccount]: OnRevokeAccountRemovalCallback, fpid = {fpid}, error = {error.ToJsonString()}");
            }
            else
            {
                Adapter.LogInfo($"[CGRemoveAccount]: OnRevokeAccountRemovalCallback, fpid = {fpid}, status = {accountRemoveGeneralStatus}");
                Adapter.AccountRemoveStatus = accountRemoveGeneralStatus;
                isSucc = true;
            }
            AccountDelectionUtility.ResolveCancelRemove(isSucc);
        }
        
        //提交 "账号删除申请" 回调
        void CGRemoveAccount.IDelegate.OnRemoveAccountCallback(string fpid, CGAccountRemoveGeneralStatus accountRemoveGeneralStatus, int accountRemoveStatus, CGError error)
        {
            bool isSucc = false;
            if (error != null)
            {
                Adapter.LogError($"[CGRemoveAccount]: OnRemoveAccountCallback, fpid = {fpid}, error = {error.ToJsonString()}");
            }
            else
            {
                Adapter.LogInfo($"[CGRemoveAccount]: OnRemoveAccountCallback, fpid = {fpid}, status = {accountRemoveGeneralStatus}");
                Adapter.AccountRemoveStatus = accountRemoveGeneralStatus;
                isSucc = true;
            }
            AccountDelectionUtility.ResolveRemoveUser(isSucc);
        }

        //获取可用于验证用户身份的社交列表的回调
        void CGRemoveAccount.IDelegate.OnRequestValidSocialListCallback(List<CGAccountType> typeList, CGError error)
        {
            if (error != null)
            {
                Adapter.LogError($"[CGRemoveAccount]: OnRequestValidSocialListCallback: {error.ToJsonString()}");
            }
            else
            {
                Adapter.LogInfo($"[CGRemoveAccount]: OnRequestValidSocialListCallback, typeList Count = {typeList.Count}");
                Adapter.ValidSocialList.Clear();
                Adapter.ValidSocialList.AddRange(typeList);
            }
        }

        //身份校验结果回调
        void CGRemoveAccount.IDelegate.OnVerifyUserIdentityCallback(CGBaseRemovalAccountInfo cgBaseRemovalAccountInfo, CGError error)
        {
            bool isSucc = false;
            if (error != null)
            {
                Adapter.LogError($"[CGRemoveAccount]: OnVerifyUserIdentityCallback: {error.ToJsonString()}");
            }
            else
            {
                Adapter.LogInfo($"[CGRemoveAccount]: OnVerifyUserIdentityCallback, info = {cgBaseRemovalAccountInfo.ToJsonString()}");
                isSucc = true;
            }
            AccountDelectionUtility.ResolveAuthentication(isSucc, cgBaseRemovalAccountInfo);
        }

        public void OnClickUserCenterDeleteAction() {}

        public void OnUserAccountDeleteProcessUICallback(CGError error, string fpid) {}

        public void OnCloseUserCenterDeleteAccountUsingGameUI() {}

        #endregion

        #region  CenturyGameHelpshift.IDelegate hs

        void CGHelpshift.IDelegate.fpDidReceiveUnreadMessagesCount(int count) {
            Adapter.LogInfo($"fpDidReceiveUnreadMessagesCount {count}");
        }

        #endregion

        #region CGPayment.IDelegate 支付

        void CGPayment.IDelegate.OnInitializeSuccess(List<CGProduct> products) {
            Adapter.LogInfo("IAP initialize success");
            Adapter.InitIAPSuccess(products);
        }

        void CGPayment.IDelegate.OnInitializeError(CGError error) {
            Adapter.LogError($"IAP initialize error:{error.ToJsonString()}");
            Adapter.InitIAPFail(error.GetErrorCode(), error.GetExtra());
        }

        private void _OnPurchaseSuccess(string productId) {
            Adapter.IAPBuySuccess(productId);
        }

        void CGPayment.IDelegate.OnPurchaseSuccess(string productId, string throughCargo) {
            Adapter.LogInfo($"IAP purchase success {productId} {throughCargo}");
            _OnPurchaseSuccess(productId);
        }

        void CGPayment.IDelegate.OnPurchaseError(CGError error) {
            Adapter.LogError($"IAP purchase error:{error.ToJsonString()}");
            Adapter.IAPBuyFail(error.GetErrorCode(), error.GetExtra());
        }

        void CGPayment.IDelegate.OnCheckOngoingSubscriptOrders(List<CGSubscriptProduct> products) {
            Adapter.LogInfo($"OnCheckOngoingSubscriptOrders {products.Count}");
            Adapter.IAPRestoreSuccess(products);
        }

        void CGPayment.IDelegate.OnCommonCallBack(string result) {
            Adapter.LogInfo($"OnCommonCallBack {result}");
        }

        void CGPayment.IDelegate.OnCreateOrderIdComplete(string productId, string orderId, string throughCargo) {
            Adapter.LogInfo($"OnCreateOrderIdComplete {productId}-{orderId}-{throughCargo}");
        }

        // asia版支付成功走了这个回调 | 国内版走的是OnPurchaseSuccess
        void CGPayment.IDelegate.OnPurchaseSuccessWithOrderId(string productId, string orderId) {
            Adapter.LogInfo($"IAP purchase successWithOrderId {productId}-{orderId}");
            _OnPurchaseSuccess(productId);
        }

        void CGPayment.IDelegate.OnGetThroughCargoWithProviderIdError(CGError error) {
            Adapter.LogError($"OnGetThroughCargoWithProviderIdError {error.ToJsonString()}");
        }

        void CGPayment.IDelegate.OnGetThroughCargoWithProviderIdSuccess(string throughCargo, string productId) {
            Adapter.LogInfo($"OnGetThroughCargoWithProviderIdSuccess {productId}-{throughCargo}");
        }

        void CGPayment.IDelegate.OnCheckOngoingSubscriptOrdersError(CGError error)
        {
            Adapter.LogError($"OnCheckOngoingSubscriptOrdersError {error.ToJsonString()}");
        }

        #endregion

        #region CGCDKey.IDelegate

        void CGCDKey.IDelegate.OnExchangeSuccess(string cdkey, string throughCargo) {
            Adapter.LogInfo($"OnExchangeSuccess {cdkey}-{throughCargo}");
        }

        void CGCDKey.IDelegate.OnExchangeError(CGError error) {
            Adapter.LogError($"OnExchangeError {error.ToJsonString()}");
        }

        #endregion

        #region CGFacebook.IDelegate facebook

        void CGFacebook.IDelegate.OnShareSuccess(string result) {
            Adapter.ShareSuccess(result);
        }
        void CGFacebook.IDelegate.OnShareError(CGError error) {
            Adapter.LogError($"OnShareError: {error.ToJsonString()}");
            Adapter.ShareFail(error.GetErrorCode(), error.GetExtra());
        }

        void CGFacebook.IDelegate.onGetPayloadFailed(CGError error)
        {
            Adapter.LogError($"onGetPayloadFailed:{error.ToJsonString()}");
        }

        void CGFacebook.IDelegate.onGetPayloadSuccess(string result)
        {
            Adapter.LogInfo($"onGetPayloadSuccess = {result}");
        }

        void CGFacebook.IDelegate.OnGetUserDataSuccess(CGSocialUser user)
        {
            Adapter.LogInfo($"OnGetUserDataSuccess = {user.ToJsonString()}");
            Adapter.UserDataSuccess(user);
        }

        void CGFacebook.IDelegate.OnGetUserDataError(CGError error)
        {
            Adapter.LogError($"OnGetUserDataError:{error.ToJsonString()}");
            Adapter.UserDataFail(error.GetErrorCode(), error.GetExtra());
        }

        void CGFacebook.IDelegate.OnGetGameFriendsSuccess(List<CGFBFriend> friends)
        {
            Adapter.LogInfo($"OnGetGameFriendsSuccess = {friends.Count}");
            Adapter.GetGameFriendsSuccess(friends);
        }

        void CGFacebook.IDelegate.OnGetGameFriendsError(CGError error)
        {
            Adapter.LogError($"OnGetGameFriendsError:{error.ToJsonString()}");
            Adapter.GetGameFriendsError(error.GetErrorCode(), error.GetExtra());
        }

        void CGFacebook.IDelegate.OnGetFacebookGetGameFriendsFpid(string friends)
        {
            Adapter.LogInfo($"OnGetFacebookGetGameFriendsFpid = {friends}");
        }

        void CGFacebook.IDelegate.OnGetFacebookGetGameFriendsFpidError(CGError error)
        {
            Adapter.LogError($"OnGetFacebookGetGameFriendsFpidError:{error.ToJsonString()}");
        }

        void CGFacebook.IDelegate.OnGetGameInvitableFriendsSuccess(List<CGFBFriend> friends)
        {
            Adapter.LogInfo($"OnGetGameInvitableFriendsSuccess = {friends.Count}");
        }

        void CGFacebook.IDelegate.OnGetGameInvitableFriendsError(CGError error)
        {
            Adapter.LogError($"OnGetGameInvitableFriendsError:{error.ToJsonString()}");
        }

        void CGFacebook.IDelegate.OnSendGameRequestSuccess(string result)
        {
            Adapter.LogInfo($"OnSendGameRequestSuccess = {result}");
        }

        void CGFacebook.IDelegate.OnSendGameRequestError(CGError error)
        {
            Adapter.LogError($"OnSendGameRequestError:{error.ToJsonString()}");
        }

        void CGFacebook.IDelegate.OnOpenGraphStoryShareSuccess(string result)
        {
            Adapter.LogInfo($"OnOpenGraphStoryShareSuccess = {result}");
        }

        void CGFacebook.IDelegate.OnOpenGraphStoryShareError(CGError error)
        {
            Adapter.LogError($"OnOpenGraphStoryShareError:{error.ToJsonString()}");
        }

        void CGFacebook.IDelegate.OnFacebookAskPermissionSuccess(string permission)
        {
            Adapter.LogInfo($"OnFacebookAskPermissionSuccess = {permission}");
            Adapter.AskPermissionSuccess(permission);
        }

        void CGFacebook.IDelegate.OnFacebookAskPermissionFail(string permission, CGError error)
        {
            Adapter.LogError($"OnFacebookAskPermissionFail, permission = {permission}, error = {error.ToJsonString()}");
            Adapter.AskPermissionFail(permission, error.GetErrorCode(), error.GetExtra());
        }

        #endregion

        #region CGAdvertising.ICGAdvertisingCallback ads
        
        // https://podd-docs.diandian.info/client/integration/unity/10_advertising.html
        // 广告收益回调，SDK收到三方广告平台收益回调后通知接入方，收到回调后可以通过AdIncommeData 对象通过SetCustomData(string JsonString)方法透传额外信息(例如广告位id)用于tga上报，如果未设置CustomData 不会触发tga上报逻辑。
        AdIncommeData CGAdvertising.ICGAdvertisingCallback.OnAdIncomeNotice(AdIncommeData data, string adunitId, CGAdvertising.CGAdvertisingDuty type) 
        {
            var dict = new Dictionary<string, object> 
            {
                { "ad_local_id", adunitId } 
            };
            string jsonObject = HSMiniJSON.Json.Serialize(dict); // 项目组需要传入的自定义数据
            Adapter.LogInfo($"OnAdIncomeNotice : {jsonObject}");
            data.SetCustomData(jsonObject);
            return data;
        }

        void CGAdvertising.ICGAdvertisingCallback.OnAdvertisingCommon(string result) {
            Adapter.LogInfo($"OnAdvertisingCommon result: {result}");
            try
            {
                var jd = JObject.Parse(result);
                var funcName = jd["callback_function_name"]?.ToString() ?? "";
                var error = jd["error"]?.ToString() ?? "";
                switch (funcName) {
                    case "onAdvertisingInited": {
                            Adapter.LogInfo($"OnAdvertisingCommon onAdvertisingInited {result}");
                        }
                        break;
                    case "onAdvertisingFail": {
                            Adapter.LogError($"OnAdvertisingCommon onAdvertisingFail {result}");
                        }
                        break;
                    case "onAdLoaded": {
                            PlatformSDK.Instance.AdsEvent_RVLoadFinish(true, result);
                        }
                        break;
                    case "onAdFailedToLoad": {
                            PlatformSDK.Instance.AdsEvent_RVLoadFinish(false, result);
                        }
                        break;
                    case "onAdShown": {
                            PlatformSDK.Instance.AdsEvent_RVOpen();
                        }
                        break;
                    case "onAdFailedToShow": {
                            PlatformSDK.Instance.AdsEvent_RVOpenFail((long)GameErrorCode.Ads, error);
                        }
                        break;
                    case "onAdClosed": {
                            PlatformSDK.Instance.AdsEvent_RVClose(false);
                        }
                        break;
                    case "onAdClicked": 
                        break;
                    case "onRewarded": {
                            PlatformSDK.Instance.AdsEvent_RVReward();
                        }
                        break;
                    case "onShouldShowGDPR": 
                        {
                            Adapter.LogInfo($"OnAdvertisingCommon onShouldShowGDPR {result}");
                            var status = jd["status"]?.Value<int>() ?? 0;
                            //status: 是否能展示 GDPR，1 能展示，0 不能展示
                            if (status == 1)
                                CGAdvertising.Instance.ShowConsentRevocation(CGAdvertising.CGAdvertisingType.CGMax); 
                        }
                        break;
                    case "onShowGDPRSuccess":
                        {
                            Adapter.LogInfo($"OnAdvertisingCommon onShowGDPRSuccess {result}");
                        }
                        break;
                    case "onShowGDPRFailed": 
                        {
                            Adapter.LogInfo($"OnAdvertisingCommon onShowGDPRFailed {result}");
                            var errorJson = JObject.Parse(error);
                            int errorCode = errorJson["errorCode"]?.Value<int>() ?? -1;
                            if (errorCode == 3335)
                            {
                                //展示GDPR失败->重新触发
                                CGAdvertising.Instance.ShowConsentRevocation(CGAdvertising.CGAdvertisingType.CGMax);
                            }
                            else if (errorCode == 3337)
                            {
                                //不是欧盟地区ip，不需展示GDPR->无需处理
                            }
                        }
                        break;
                    case "":
                        break;
                }
            }
            catch (Exception e) {
                Adapter.LogError($"OnAdvertisingCommon error {result}-{e.Message}");
            }
        }

        #endregion

        #region CGSdkUtils.IUtilDelegate utils

        void CGSdkUtils.IUtilDelegate.OnRequestIDFATrackingAuthorization(CGIDFAAuthorizationStatus status) {
            Adapter.LogInfo($"OnRequestIDFATrackingAuthorization {status}");
            CGSdkUtils.Instance.Log(CGSdkUtils.CGLogType.CGDebug, "cg", "OnRequestIDFATrackingAuthorization = " + status);
            if (status == CGIDFAAuthorizationStatus.Denied) {
                // TODO: 引导用户
            }
        }

        void CGSdkUtils.IUtilDelegate.OnRequestPermissionResult(Dictionary<string, object> result) {
            Adapter.LogInfo($"OnRequestPermissionResult");
            string jsonString = HSMiniJSON.Json.Serialize(result);
            CGSdkUtils.Instance.Log(CGSdkUtils.CGLogType.CGDebug, "cg", string.Format("message OnRequestPermissionResult: {0}.", jsonString));
        }

        void CGSdkUtils.IUtilDelegate.clipPhotoPathSuccess(string path) {
            Adapter.LogInfo($"clipPhotoPathSuccess {path}");
        }

        void CGSdkUtils.IUtilDelegate.clipPhotoPathFailed(CGError error) {
            Adapter.LogError($"clipPhotoPathFailed {error.ToJsonString()}");
        }

        void CGSdkUtils.IUtilDelegate.compressedPictureResult(CGError error, string path, string size) {
            Adapter.LogInfo($"compressedPictureResult {error.ToJsonString()}, {path}, {size}");
        }

        void CGSdkUtils.IUtilDelegate.showPictureCallback(CGError error) {
            Adapter.LogInfo($"showPictureCallback {error.ToJsonString()}");
        }

        void CGSdkUtils.IUtilDelegate.openGalleryCallback(CGError error, string path) {
            Adapter.LogInfo($"openGalleryCallback {error.ToJsonString()}, {path}");
        }

        void CGSdkUtils.IUtilDelegate.OnQueryDeviceLevelSuccess(string message) {
            Adapter.LogInfo($"OnQueryDeviceLevelSuccess {message}");
        }

        void CGSdkUtils.IUtilDelegate.OnQueryDeviceLevelFail() {
            Adapter.LogError($"OnQueryDeviceLevelFail");
        }

        void CGSdkUtils.IUtilDelegate.OnFetchCountryCodeResult(string countryCode)
        {
            Adapter.LogInfo($"OnFetchCountryCodeResult {countryCode}");
        }

        void CGSdkUtils.IUtilDelegate.OnRequestIosCameraAuthorizationResult(bool authorized)
        {
            Adapter.LogInfo($"OnRequestIosCameraAuthorizationResult {authorized}");
        }

        #endregion

        #region CGSurvey

        public void OnSurveySubmitSuccess(string msg) {
            Adapter.SurveySuccess(msg);
        }

        public void OnSurveySubmitFail(CGError error) {
            Adapter.SurveyFail(error.GetErrorCode(), error.GetExtra());
        }

        #endregion CGSurvey

        #region DeepLink

        public void OnBuildShortLinkCallback(string link_, CGError error_) {
            if (error_ == null) Adapter.ShareLinkSuccess(link_);
            else Adapter.ShareLinkFail(error_.GetErrorCode(), error_.GetExtra());
        }

        public void OnShareToMessengerCallback(CGError error_) {
            if (error_ == null) Adapter.ShareLinkSuccess(null);
            else Adapter.ShareLinkFail(error_.GetErrorCode(), error_.GetExtra());
        }

        public void OnRequestPayloadCallback(string payload_, CGError error_) {
            if (error_ == null) Adapter.LinkPayloadSuccess(payload_);
            else Adapter.LinkPayloadFail(error_.GetErrorCode(), error_.GetExtra());
        }


        #endregion DeepLink
    }
}