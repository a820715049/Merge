/**
 * @Author: handong.liu
 * @Date: 2020-10-30 15:46:40
 */
#if UNITY_EDITOR
#define FAKE_TEST
#endif
using UnityEngine;
using System.Collections;
using EL;
using System;
using centurygame;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using fat.gamekitdata;

namespace FAT.Platform {
    public enum AccountLoginType {
        Cache,
        Wechat,
        Apple,
        Facebook,
        Google,
        Guest,
        EMail,
        Mobile,
        None,
        Unknown,
    }

    public readonly struct AccountBindInfo {
        public readonly string id;
        public readonly string name;

        public AccountBindInfo(string id_, string name_) {
            id = id_;
            name = name_;
        }
    }

    public readonly struct AccountProfile {
        public readonly AccountLoginType type;
        public readonly string id;
        public readonly string name;
        public readonly string pic;

        public AccountProfile(AccountLoginType type_, CGSocialUser data_) {
            type = type_;
            id = data_.Uid;
            name = data_.Name;
            pic = data_.Pic;
        }

        public AccountProfile(AccountLoginType type_, string id_, string name_, string pic_) {
            type = type_;
            id = id_;
            name = name_;
            pic = pic_;
        }

        public static bool Equals(AccountProfile a_, AccountProfile b_)
            => a_.type == b_.type && a_.id == b_.id && a_.name == b_.name && a_.pic == b_.pic;
    }

    public partial class PlatformSDK : MonoBehaviour {
        public static PlatformSDK Instance;
        public bool SDKInit => Adapter.SDKInit;
        private readonly System.Text.StringBuilder builder = new();

        public PlatformSDKAdapter Adapter { get; private set; }

        public IEnumerator Initialize() => Adapter.InitSDK();

        public int GetSDKChannelId() => Adapter.GetSDKChannelId();
        public void SetLanguage(string lang) => Adapter.SetLanguage(lang);
        //客服系统
        public void ShowCustomService(bool preferConversation = false) {
            UpdateGameUserInfo();

            if (Game.Manager.configMan.IsAllConfigReady)
            {
                if (Game.Manager.configMan.globalConfig.IsFaq)
                    Adapter.ShowFAQ();
                else
                    Adapter.ShowConversation();
            }
            else
            {
                if (preferConversation)
                    Adapter.ShowConversation();
                else
                    Adapter.ShowFAQ();
            }
        }

        /*平台指示：1.没有的可以传个默认值，0什么的，2.api的方法里限制了传的类型，3.一开始可以是空的，只要变化了就需要调用，4.这个userlevel我们没什么要求，可能需要问下数据组有没有什么需求，5.这个只要付费了就是true，没发再变回false，不过这个一般不用*/
        public void UpdateGameUserInfo() {
            string serverId = "0";
            // string userId = Game.Instance.socialMan.myId.ToString();
            // string userName = Game.Instance.socialMan.GetName();
            string userId = GetUserId();
            string userName = string.Empty;
            string vipLevel = "0";
            bool paied = false;
            int level = -1;
            if (Game.Manager.archiveMan != null && Game.Manager.archiveMan.isArchiveLoaded) {
                userId = Game.Manager.networkMan.fpId;
                paied = Game.Manager.iap.TotalIAPServer > 0;
                level = Game.Manager.mergeLevelMan.level;
            }
            DebugEx.FormatInfo("PlatformSDK.UpdateGameUserInfo ----> serverId:{0}, userId:{1}, userName:{2}, level:{3}, vipLevel:{4}, isPaid:{5}", serverId, userId, userName, level, vipLevel, paied);
            Adapter.LogUserInfoUpdate(serverId, userId, userName, level.ToString(), vipLevel, paied);
        }

        public void TraceUser(EL.SimpleJSON.JSONObject data) {
            builder.Clear();
            data.WriteToStringBuilder(builder, 4, 4, EL.SimpleJSON.JSONTextMode.Compact);
            string userSetStr = builder.ToString();
            DebugEx.FormatTrace("PlatformSDK.TraceUser:{0}", userSetStr);
            Adapter.TraceUser(userSetStr);
        }
        public void TraceUserAdd(EL.SimpleJSON.JSONObject data) {
            builder.Clear();
            data.WriteToStringBuilder(builder, 4, 4, EL.SimpleJSON.JSONTextMode.Compact);
            string userSetStr = builder.ToString();
            DebugEx.FormatTrace("PlatformSDK.TraceAddUser:{0}", userSetStr);
            Adapter.TraceUserAdd(userSetStr);
        }
        public void TraceEvent(string eventId, string param) {
            DebugEx.FormatTrace("PlatformSDK.TraceEvent ----> trace event {0}:{1}", eventId, param);
            Adapter.TrackEvent(eventId, param);
        }
        public void TrackAdjust(string eventId) {
            DebugEx.FormatTrace("PlatformSDK.TrackAdjust ----> trace event {0}", eventId);
            Adapter.TrackAdjust(eventId);
        }
        public void TrackFirebase(string eventName) {
            DebugEx.FormatTrace("PlatformSDK.TrackFirebase ----> trace event {0}", eventName);
            Adapter.TrackFirebase(eventName);
        }

        public string GetTrackDistinctId() => Adapter.GetTrackDistinctId();
        public string GetUserId() => Adapter.SessionId;
        public string GetUserSessionKey() => Adapter.SessionKey;
        public string GetCountryCode() => Adapter.GetCountryCode();
        public string GetDeviceModel() => Adapter.GetDeviceModel();
        public string GetDeviceName() => Adapter.GetDeviceName();
        public string GetOSVersion() => Adapter.GetOSVersion();
        public string GetOSName() => Adapter.GetOSName();

        public ResultedAsyncTask<byte[]> LoadFile(string file) {
            return this.StartAsyncTask<ResultedAsyncTask<byte[]>>(_LoadFile(file));
        }

        public IEnumerator Login() {
            if (Adapter.SDKLogin) yield break;
            yield return Adapter.Login(Adapter.LoginType, null);
            if (!Adapter.SDKLogin) {
                Adapter.RetryLogin(Adapter.LastError.ErrorCode);
                yield break;
            }
            MessageCenter.Get<MSG.GAME_ACCOUNT_CHANGED>().Dispatch();
        }

        public IEnumerator Login(AccountLoginType type_ = AccountLoginType.Cache, Action<bool, SDKError> OnLogin_ = null) {
            DebugEx.Info("PlatformSDK::Login");
            return Adapter.Login(type_, OnLogin_);
        }

        public IEnumerator Logout() {
            DebugEx.Info("PlatformSDK::Logout");
            return Adapter.Logout();
        }

        public void UpdateProfile(FacebookInfo info_) {
            async UniTask N() {
                while (!Adapter.BindInit) await Task.Delay(200);
                if (!Adapter.BindInfo.ContainsKey(AccountLoginType.Facebook)) return;
                var p1 = Adapter.profile;
                var token = new WaitToken();
                Adapter.UserData(Adapter.LoginType, (_, _) => {
                    token.Cancel();
                });
                await token.Wait(500, ui_:false, block_:false);
                var p2 = Adapter.profile;
                if (AccountProfile.Equals(p1, p2)) {
                    DebugEx.Info($"facebook info match");
                    return;
                }
                DebugEx.Info($"facebook info from sdk, id:{p2.id} name:{p2.name}");
                await Game.Manager.networkMan.LoginProfile(p2);
            }
            if (info_ != null) {
                DebugEx.Info($"facebook info from login, id:{info_.Id} name:{info_.Name}");
                Adapter.profile = new(AccountLoginType.Facebook, info_.Id, info_.Name, info_.Avatar);
            }
            _ = N();
        }

        private IEnumerator _LoadFile(string path) {
            var task = new SimpleResultedAsyncTask<byte[]>();
            yield return task;
            UnityEngine.Networking.UnityWebRequest req = null;
            UnityEngine.Networking.UnityWebRequestAsyncOperation toWait = null;
            try {
                req = UnityEngine.Networking.UnityWebRequest.Get(string.Format("file://{0}", path));
                DebugEx.FormatInfo("PlatformSDK._LoadFile ----> upload from:{0}", req.url);
                toWait = req.SendWebRequest();
            }
            catch (Exception ex) {
                DebugEx.FormatWarning("PlatformSDK._LoadFile ----> upload from:{0}", req.url);
                task.Fail(ex.ToString(), (int)GameErrorCode.Unknown, ErrorCodeType.GameError);
            }
            if (task.isFailOrCancel) {
                yield break;
            }
            yield return toWait;
            if (toWait.isDone) {
                task.Success(req.downloadHandler.data);
            }
            else {
                DebugEx.FormatWarning("PlatformSDK._LoadFile ----> read file fail:{0}", req.error);
                task.Fail("read fail", (int)GameErrorCode.Unknown, ErrorCodeType.GameError);
            }
        }

        private void _RefreshLanguage() {
            SetLanguage(I18N.GetLanguage());
        }

        private void OnEnable() {
            I18N.onLanguageChange += _RefreshLanguage;
        }

        private void OnDisable() {
            I18N.onLanguageChange -= _RefreshLanguage;
        }

        private void Awake() {
            if (Adapter == null) {
                Adapter = new PlatformSDKAdapterGlobal();
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}