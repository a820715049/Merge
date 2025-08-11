/*
 * @Author: qun.chao
 * @Date: 2023-10-23 18:27:46
 */
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using GameNet;
using EL;
using fat.gamekitdata;
using System.Data;

namespace FAT
{
    public class ArchiveMan : IGameModule, IUpdate
    {
        private class UploadHelper
        {
            // 待上传
            public bool HasPendingUploadRequest => _isRequesting;
            // 已成功上传
            public bool UploadRequestFulfilled => _requestFulfilled;

            private bool _isRequesting;
            private bool _requestFulfilled;

            public void TryStartUpload()
            {
                _isRequesting = true;
                _requestFulfilled = false;
            }

            public void AfterUpload()
            {
                _isRequesting = false;
                _requestFulfilled = false;
            }

            public void TryResolve()
            {
                if (!_isRequesting)
                {
                    _requestFulfilled = true;
                }
            }
        }

        public UserType userType => mUserReady ? mUserType : UserType.Normal;
        public bool accountCreated => mAccountCreated;
        public long lastSyncTime => mLastSyncTime;
        public long lastUpdateTime => mLastUpdateTime.GetValue();
        public bool isArchiveLoaded => mUserReady;
        public AsyncTaskBase syncTask => mArchiveDataSyncTask;
        public bool isNewUser { get; private set; }
        public bool uploadCompleted => uploadHelper.UploadRequestFulfilled;

        private bool mAccountCreated = false;
        private float mSyncCountdown = -1;
        private SimpleResultedAsyncTask<Google.Protobuf.IMessage> mArchiveDataSyncTask = null;
        private bool mUserReady = false;
        private bool mIsLastSyncFail = false;
        private long mLastSyncTime = 0;
        private EL.EncryptLong mLastUpdateTime = new EncryptLong().SetValue(0);
        private UserType mUserType;
        private ICryptoTransform mArchiveEncoder;
        private ICryptoTransform mArchiveDecoder;
        private List<System.Action> mExecuteAfterArchiveInitialize = new List<System.Action>();     //在初始化存档时如果需要做一些用到其他游戏状态的事，这里会发挥作用
        //存档加载完回调
        private Action _loadArchiveFinishCb = null;

        #region save data

        private LocalSaveData syncedData;
        private ClientData currentSyncingData;
        private PlayerBaseData initializedBaseData;
        private UploadHelper uploadHelper;
        private ArchiveUpgrader archiveUpgrader;

        #endregion

        #region login property
        // 登录时等级
        public int LoginLevel { get; private set; }
        // 登录时离线天数
        public int OfflineDays { get; private set; }
        #endregion

        #region imp

        void IGameModule.LoadConfig()
        { }

        void IGameModule.Reset()
        {
            uploadHelper = new UploadHelper();
            archiveUpgrader = new ArchiveUpgrader();
            mUserReady = false;
            mIsLastSyncFail = false;
            mLastSyncTime = 0;
            mLastUpdateTime = new EncryptLong().SetValue(0);
            _loadArchiveFinishCb = null;

            syncedData = null;
            currentSyncingData = null;
            initializedBaseData = null;

            mArchiveDataSyncTask = null;
            mSyncCountdown = -1f;
        }

        void IGameModule.Startup()
        { }

        void IUpdate.Update(float dt)
        {
            _Update(dt);
        }

        #endregion

        //设置存档加载完成后的回调 会在存档数据真正应用之前执行
        public void SetLoadArchiveFinishCb(Action cb)
        {
            _loadArchiveFinishCb = cb;
        }
        
        private void _Update(float dt)
        {
            if (mArchiveDataSyncTask != null && syncedData != null)
            {
                if (!mArchiveDataSyncTask.keepWaiting)
                {
                    if (mArchiveDataSyncTask.isSuccess)
                    {
                        DebugEx.FormatInfo("Update ----> achieve data sync success!");
                        uploadHelper.TryResolve();
                        syncedData.ClientData = currentSyncingData;
                        currentSyncingData = null;
                        mIsLastSyncFail = false;
                        if (mArchiveDataSyncTask.result is fat.msg.StoreResp storeResp)
                        {
                            //在收到服务器存档协议回复时 设置tag目前信息
                            Game.Manager.userGradeMan.OnReceiveUserTagInfo(storeResp.UserTagData);
                        }
                    }
                    else
                    {
                        DebugEx.FormatWarning("Update ----> achieve data sync fail:{0}", mArchiveDataSyncTask.error);
                        mIsLastSyncFail = true;
                    }
                    mArchiveDataSyncTask = null;

                    if (uploadHelper.HasPendingUploadRequest)
                    {
                        // 尽快触发下次上传
                        mSyncCountdown = float.Epsilon;
                    }
                }
            }
            if (mSyncCountdown <= 0)
            {
                mSyncCountdown = Constant.kArchiveAutoSaveInterval;
            }
            if (mSyncCountdown > 0 && mAccountCreated)
            {
                mSyncCountdown -= dt;
                if (mSyncCountdown <= 0)
                {
                    _SaveArchiveData();
                }
            }
        }

        public AsyncTaskBase SendImmediately(bool uploadToRemote)
        {
            uploadHelper.TryStartUpload();

            if (mUserReady && mAccountCreated)
            {
                if (mArchiveDataSyncTask == null)
                {
                    DebugEx.Info("ArchiveMan::SendImmediatly");
                    _SaveArchiveData();
                }
                return mArchiveDataSyncTask;
            }
            else
            {
                return SimpleAsyncTask.AlwaysFail;
            }
        }

        public void ExecuteAfterArchiveReady(System.Action act)
        {
            if (mUserReady)
            {
                act?.Invoke();
            }
            else
            {
                mExecuteAfterArchiveInitialize.Add(act);
            }
        }

        public bool TryLoadLocalArchive()
        {
            if (!mUserReady)
            {
                _LoadDataFromLocal(out var data);
                if (data != null)
                {
                    _InitializeGameArchive(data, false);
                    mIsLastSyncFail = true;
                    return true;
                }
            }
            return false;
        }

        public void OnReceiveServerArchive(PlayerBaseData baseData, ClientData clientData, bool newUser)
        {
            syncedData ??= new LocalSaveData();
            var data = syncedData;
            data.PlayerBaseData = baseData;
            data.ClientData = clientData;

            isNewUser = newUser;
            if (!mUserReady)
            {
                _LoadDataFromLocal(out var _local);
                if (_local != null)
                {
                    // 准备丢弃本地存档 / 把存档备份到tga
                    DataTracker.version_update.Track(0, 0, _local.ClientData, clientData);
                }
                _InitializeGameArchive(data, newUser);
            }

            mUserType = baseData.UserType;
            mIsLastSyncFail = lastSyncTime > baseData.LastSync;
            // baseData.ServerData ??= new ServerData();

            // _Apply_OnReceiveServerData(baseData.ServerData);

            // FAT_TODO
            // if (isNewUser)
            // {
            //     FAT.Platform.PlatformSDK.Instance.InstallTrackRegister(Game.Instance.networkMan.fpId);
            //     DataTracker.TraceUser().user_old(DreamMerge.UIBridgeUtility.IsFindGameUser()).Apply();
            //     AdjustTracker.TrackEvent(AdjustEventType.NewRegister);
            // }
            // else
            // {
            //     FAT.Platform.PlatformSDK.Instance.InstallTrackLogin(Game.Instance.networkMan.fpId);
            // }

            // FAT_TODO
            // Game.Manager.accountMan.OnReceiveLoadData(accountData.ServerData.LoadCommonData);
        }

        public void DiscardLocalArchiveNextLogin(long errorCode)
        {
            GameUpdateManager.Instance.SetNeedWaitUpdate2(true);
            Game.Manager.networkMan.SendLog("discard_archive_cause", string.Format("{{\"state\":\"{0}\", \"err\":{1} }}", Game.Manager.networkMan.state, errorCode));
            string archive = "NotLoaded";
            if (Game.Manager.archiveMan.isArchiveLoaded)
            {
                archive = Game.Manager.archiveMan.SerializeArchive(false).ToString();
            }
            else
            {
                _LoadDataFromLocal(out var usr);
                if (usr != null)
                {
                    archive = usr.ToString();
                }
            }
            Game.Manager.networkMan.SendLog("discard_archive_content", archive);
        }

        private void _InitializeGameArchive(LocalSaveData data, bool newUser)
        {
            initializedBaseData = data.PlayerBaseData;

            var clientData = data.ClientData;
            // clientData.PlayerBaseData ??= new PlayerBaseData();
            clientData.PlayerGeneralData ??= new PlayerGeneralData();
            clientData.PlayerGameData ??= new PlayerGameData();
            // data.PlayerData ??= new PlayerData();

            mAccountCreated = true;
            DebugEx.FormatInfo("ArchiveMan::_InitializeGameArchive ----> {0}", data);

            try
            {
                _Apply_OnPreSetUserData(data);
                //在真正设置存档数据之前执行回调
                _loadArchiveFinishCb?.Invoke();
                //在活动触发前解析登录属性
                _ParseLoginProperty(data);
                _Apply_UserDataVersionUpgrader(data, newUser);
                _Apply_UserDataHolder_SetData(data);
                if (newUser) _Apply_UserDataInitializer(data);

                mLastSyncTime = data.PlayerBaseData.LastSync;
                mLastUpdateTime.SetValue(data.ClientData.LastUpdate);

                _Apply_PostSetUserDataListener(data);
            }
            catch (System.Exception ex)
            {
                try
                {
                    DebugEx.FormatError("archive exception {0}:{1}", ex.Message, ex.StackTrace);
                    DataTracker.TrackLogError(ex.Message, ex.StackTrace, null, DataTracker.LogErrorType.client);
                    DiscardLocalArchiveNextLogin((long)GameErrorCode.ArchiveError);
                }
                finally
                {
                    var msg =
#if UNITY_EDITOR
                        ex.Message;
#else
                    "";
#endif
                    Game.Instance.Abort(msg, (long)GameErrorCode.ArchiveError);
                }
            }

            mUserReady = true;
            foreach (var cb in mExecuteAfterArchiveInitialize)
            {
                cb?.Invoke();
            }
            mExecuteAfterArchiveInitialize.Clear();
        }

        private void _ParseLoginProperty(LocalSaveData data)
        {
            // 登录时等级
            LoginLevel = data.PlayerBaseData.Level;
            // 登录时离线天数
            var timeAnchor = Game.Manager.configMan.globalConfig.RequireTypeUtcClock;
            var lastDate = Game.TimeOf(data.ClientData.LastUpdate).AddHours(-timeAnchor);
            var nowDate = Game.UtcNow.AddHours(-timeAnchor);
            OfflineDays = (nowDate.Date - lastDate.Date).Days;

#if UNITY_EDITOR
            var _last = Game.TimeOf(data.ClientData.LastUpdate);
            var _now =Game.UtcNow;
            DebugEx.Info($"ArchiveMan::_ParseLoginProperty ----> OfflineDays: {OfflineDays} last: {_last} now: {_now} lastUTC-10: {lastDate} nowUTC-10: {nowDate}");
#endif
        }

        #region data handlers

        private void _Apply_OnReceiveServerData(ServerData data)
        {
            using (ObjectPool<List<IServerDataReceiver>>.GlobalPool.AllocStub(out var list))
            {
                Game.Instance.moduleMan.FillModuleByType(list);
                foreach (var imp in list)
                {
                    imp.OnReceiveServerData(data);
                }
            }
        }

        private void _Apply_OnPreSetUserData(LocalSaveData data)
        {
            using (ObjectPool<List<IPreSetUserDataListener>>.GlobalPool.AllocStub(out var list))
            {
                Game.Instance.moduleMan.FillModuleByType(list);
                foreach (var imp in list)
                {
                    imp.OnPreSetUserData(data);
                }
            }
        }

        private void _Apply_UserDataVersionUpgrader(LocalSaveData nowData, bool newUser)
        {
            var version = nowData.ClientData.Version;
            if (!newUser && version != 0 && version != Constant.kArchiveVersion)
            {
                DataTracker.TrackLogInfo($"ArchiveMan::OnRecieveServerUser ----> migrating archive from version {version} to version {Constant.kArchiveVersion}");
                var oldData = nowData.Clone();

                // 优先使用全局upgrader
                archiveUpgrader.OnDataVersionUpgrade(oldData, nowData, version, Constant.kArchiveVersion);

                using (ObjectPool<List<IUserDataVersionUpgrader>>.GlobalPool.AllocStub(out var list))
                {
                    Game.Instance.moduleMan.FillModuleByType(list);
                    foreach (var imp in list)
                    {
                        imp.OnDataVersionUpgrade(oldData, nowData);
                    }
                }
                var oV = oldData.ClientData;
                DataTracker.version_update.Track(oV.Version, Constant.kArchiveVersion, oV, nowData.ClientData);
            }
        }

        private void _Apply_UserDataHolder_SetData(LocalSaveData data)
        {
            using (ObjectPool<List<IUserDataHolder>>.GlobalPool.AllocStub(out var list))
            {
                Game.Instance.moduleMan.FillModuleByType(list);
                foreach (var imp in list)
                {
                    imp.SetData(data);
                }
            }
        }

        private void _Apply_UserDataHolder_FillData(LocalSaveData data)
        {
            using (ObjectPool<List<IUserDataHolder>>.GlobalPool.AllocStub(out var list))
            {
                Game.Instance.moduleMan.FillModuleByType(list);
                foreach (var imp in list)
                {
                    imp.FillData(data);
                }
            }
        }

        private void _Apply_UserDataInitializer(LocalSaveData data)
        {
            using (ObjectPool<List<IUserDataInitializer>>.GlobalPool.AllocStub(out var list))
            {
                Game.Instance.moduleMan.FillModuleByType(list);
                foreach (var imp in list)
                {
                    DebugEx.FormatInfo("ArchiveMan::OnRecieveServerUser ----> init user data for {0}", imp.GetType().FullName);
                    imp.InitUserData();
                }
            }
        }

        private void _Apply_PostSetUserDataListener(LocalSaveData data)
        {
            using (ObjectPool<List<IPostSetUserDataListener>>.GlobalPool.AllocStub(out var list))
            {
                Game.Instance.moduleMan.FillModuleByType(list);
                foreach (var imp in list)
                {
                    DebugEx.FormatInfo("ArchiveMan::OnRecieveServerUser ----> post set user data for {0}", imp.GetType().FullName);
                    imp.OnPostSetUserData();
                }
            }
        }

        #endregion

        #region save & load

        public LocalSaveData SerializeArchive(bool updateSyncTime)
        {
            long serverTime = 0;
            if (updateSyncTime)
            {
                serverTime = Game.Instance.GetTimestampSeconds();
            }
            var data = new LocalSaveData
            {
                PlayerBaseData = syncedData?.PlayerBaseData ?? initializedBaseData
            };
            var clientData = new ClientData();
            data.ClientData = clientData;
            // clientData.PlayerBaseData = new PlayerBaseData();
            clientData.PlayerGameData = new PlayerGameData();
            clientData.PlayerGeneralData = new PlayerGeneralData();

            clientData.Version = Constant.kArchiveVersion;
            if (mLastUpdateTime.GetValue() < serverTime)
            {
                mLastUpdateTime.SetValue(serverTime);
            }
            clientData.LastUpdate = mLastUpdateTime.GetValue();

            _Apply_UserDataHolder_FillData(data);

            if (updateSyncTime && !mIsLastSyncFail)
            {
                mLastSyncTime = serverTime;
            }
            clientData.LastSyncByClient = mLastSyncTime;
            return data;
        }

        private bool _SaveArchiveData()
        {
            if (!Game.Instance.isRunning)
            {
                DebugEx.FormatWarning("ArchiveMan::_SaveArchiveData ----> game stopped, should not save data");
                mSyncCountdown = Constant.kArchiveAutoSaveInterval;
                return false;
            }
            if (!Game.Manager.networkMan.IsGameLogined)
            {
                DebugEx.FormatWarning("ArchiveMan::_SaveArchiveData ----> no login, should not save data");
                mSyncCountdown = Constant.kArchiveAutoSaveInterval;
                return false;
            }
            if (!mUserReady)
            {
                DebugEx.FormatWarning("ArchiveMan::_SaveArchiveData ----> user not ready!");
                return false;
            }
            bool willUploadToRemote = mArchiveDataSyncTask == null && Game.Manager.networkMan.isInSync;
            var data = SerializeArchive(willUploadToRemote);
            _SaveDataToLocal(data);
            if (willUploadToRemote)
            {
                _UploadArchiveToRemote(data);
            }
            else
            {
                DebugEx.FormatInfo("ArchiveMan::_SaveArchiveData ----> save to local only");
            }
            return true;
        }

        private bool _UploadArchiveToRemote(LocalSaveData archive)
        {
            if (mArchiveDataSyncTask != null && mArchiveDataSyncTask.keepWaiting)            //a remote upload in progress, earth exit
            {
                DebugEx.FormatWarning("ArchiveMan::_SaveArchiveData ----> data is sending!");
                return false;
            }
            if (syncedData == null)
            {
                return false;
            }
            DebugEx.FormatInfo("ArchiveMan::_SaveArchiveData ----> upload to remote");
            currentSyncingData = archive.ClientData;
            mSyncCountdown = -1;

            mArchiveDataSyncTask = Game.Manager.networkMan.PostMessage_SaveArchive(archive);
            uploadHelper.AfterUpload();
            return true;
        }

        private static Google.Protobuf.JsonFormatter mFormatter = new Google.Protobuf.JsonFormatter(Google.Protobuf.JsonFormatter.Settings.Default.WithFormatEnumsAsIntegers(true));
        private static Google.Protobuf.JsonParser mParser = Google.Protobuf.JsonParser.Default;
        private static GameNet.ProtobufCodecBin mLocalSaveCodec = new ProtobufCodecBin();
        private System.IO.MemoryStream mArchiveLocalStream = new System.IO.MemoryStream();
        private void _SaveDataToLocal(LocalSaveData data)
        {
            CommonUtility.CheckReuseOrCreateCipher(ref mArchiveEncoder, ref mArchiveDecoder);
            mArchiveLocalStream.SetLength(0);
            if (data != null)
            {
                mLocalSaveCodec.MarshalToStream(mArchiveLocalStream, data);
                mArchiveLocalStream.Flush();
                var ciphered = mArchiveEncoder.TransformFinalBlock(mArchiveLocalStream.GetBuffer(), 0, (int)mArchiveLocalStream.Length);
                PlayerPrefs.SetString(Constant.kPrefKeyUserData, System.Convert.ToBase64String(ciphered));
            }
            else
            {
                PlayerPrefs.SetString(Constant.kPrefKeyUserData, "");
            }

            PlayerPrefs.Save();
        }
        private void _LoadDataFromLocal(out LocalSaveData data)
        {
            CommonUtility.CheckReuseOrCreateCipher(ref mArchiveEncoder, ref mArchiveDecoder);
            mArchiveLocalStream.SetLength(0);
            string userString = PlayerPrefs.GetString(Constant.kPrefKeyUserData);
            data = null;
            try
            {
                if (!string.IsNullOrEmpty(userString))
                {
                    var ciphered = System.Convert.FromBase64String(userString);
                    var deciphered = mArchiveDecoder.TransformFinalBlock(ciphered, 0, ciphered.Length);
                    mArchiveLocalStream.Write(deciphered, 0, deciphered.Length);
                    mArchiveLocalStream.Flush();
                    mArchiveLocalStream.Seek(0, System.IO.SeekOrigin.Begin);
                    data = mLocalSaveCodec.UnmarshalFromStream<LocalSaveData>(mArchiveLocalStream);
                }
            }
            catch (System.Exception ex)
            {
                data = null;
                DebugEx.FormatWarning("ArchiveMan::_LoadDataFromLocal ----> parse data error {0}", ex.ToString());
            }
            DebugEx.FormatInfo("ArchiveMan::_LoadDataFromLocal ----> user:{0}", data?.ToString());
        }
        #endregion

        // #region storage
        // // 和server约定好的key 用此key上传的数据会在auth协议里返回
        // public const string kKeyPreDefined_ClientData = "key_clientdata";

        // public NetAsyncTask SaveArchive()
        // {
        //     var task = new NetAsyncTask();
        //     FoundationWrapper.SaveData(syncedData.PlayerBaseData.Uid, kKeyPreDefined_ClientData, syncedData.ClientData, (suc, code) =>
        //     {
        //         DebugEx.Info($"save data result {suc} : {code}");
        //         if (suc)
        //         {
        //             task.Success(null);
        //         }
        //         else
        //         {
        //             task.Fail(null, code);
        //         }
        //     });
        //     return task;
        // }
        // #endregion
    }
}