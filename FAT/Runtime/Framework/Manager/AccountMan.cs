/**
 * @Author: handong.liu
 * @Date: 2020-12-29 17:22:34
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using FAT.Platform;
using FAT.MSG;
using fat.msg;

namespace FAT
{
    public class AccountMan : IGameModule, IUpdate, IUserDataHolder, IServerDataReceiver
    {
        public readonly struct Referrer {
            public readonly bool Valid => id != null;
            public readonly string id;
            public readonly (int, int, int) info3;
            public readonly int type;
            public readonly int target;
            public readonly long ts;
            public readonly bool fullfill;

            public Referrer(string id_, int target_, long ts_, int ei_, int ef_, int ep_, int st_, bool fullfill_) {
                id = id_;
                target = target_;
                ts = ts_;
                fullfill = fullfill_;
                info3 = (ei_, ef_, ep_);
                type = st_;
            }

            public override string ToString()
                => $"id:{id} target:{target} ts:{ts} fullfull:{fullfill} info3:{info3} type:{type}";
        }

        public ulong uid;
        public long createAt;  // 账号创建时间
        public int playDay;
        public int playDayUTC10;
        private long playDayTs = 0;
        internal long nextRefreshTime = 0;
        private long nextRefreshPlayDayUTC10 = 0;
        private long nextRefreshLocalDay = 0;
        private long continuePlayDay = 0;
        private long lastOperateTS = 0;
        private int activeSeconds = 0;
        private readonly Dictionary<string, string> strDataStorage = new();
        private bool isSynced = false;
        private bool needNotify = false;
        private bool needNotifyLocalDay = false;
        private Referrer referrer;

#if UNITY_EDITOR
        public long NextRefreshPlayDayUtc10 
        {
            get => nextRefreshPlayDayUTC10;
            set => nextRefreshPlayDayUTC10 = value;
        }
#endif

        public void Update()
        {
            if (!isSynced)
            {
                return;
            }
            _CheckAddPlayDay();
            _CheckAddLocalPlayDay();
            if (needNotify)
            {
                _NoticePlayDayChange();
            }
            if (needNotifyLocalDay)
            {
                _NoticeLocalPlayDayChange();
            }
            if (activeSeconds > 0)
            {
                RecordActiveTime(false);
            }
        }

        public bool TryGetClientStorage(string key, out string val)
        {
            return strDataStorage.TryGetValue(key, out val);
        }

        public void SetClientStorage(string key, string val)
        {
            strDataStorage[key] = val;
        }

        private void _UpdateLocalPlayDayRefreshTime()
        {
            var localSeconds = TimeUtility.ConvertUTCSecToLocalSec(Game.TimestampNow());
            nextRefreshLocalDay = TimeUtility.ConvertLocalSecToUTCSec((long)(localSeconds / (long)Constant.kSecondsPerDay + 1) * Constant.kSecondsPerDay);
            DebugEx.FormatInfo("AccountMan._UpdateLocalPlayDayRefreshTime ----> time {0}, next refresh time {1}", localSeconds, nextRefreshLocalDay);
        }

        private void _UpdatePlayDayRefreshTime()
        {
            nextRefreshTime = ((playDayTs - Constant.kDayRefreshSeconds) / Constant.kSecondsPerDay + 1) * Constant.kSecondsPerDay + Constant.kDayRefreshSeconds;
            DebugEx.FormatInfo("AccountMan._UpdatePlayDayRefreshTime ----> last refresh time {0}, next refresh time {1}", playDayTs, nextRefreshTime);
        }

        private void _AddPlayDay(long now)
        {
            playDay++;
            playDayTs = now;
            needNotify = true;
            if (now - nextRefreshTime < Constant.kSecondsPerDay)         //not cross day
            {
                continuePlayDay++;
            }
            else
            {
                continuePlayDay = 1;
            }
            lastOperateTS = 0;
            activeSeconds = 0;
            _UpdatePlayDayRefreshTime();
            DebugEx.FormatInfo("AccountMan._AddPlayDay ----> day add to {0}, continue day add to {1}", playDay, continuePlayDay);
        }

        private void _CheckAddPlayDay()
        {
            var now = Game.TimestampNow();
            if (now >= nextRefreshTime)
            {
                _AddPlayDay(now);
            }
            if (now >= nextRefreshPlayDayUTC10) {
                ++playDayUTC10;
                var rH = Game.Manager.configMan.globalConfig.RequireTypeUtcClock;
                nextRefreshPlayDayUTC10 = Game.Timestamp(Game.NextTimeOfDay(rH));
                MessageCenter.Get<GAME_DAY_CHANGE_TEN>().Dispatch();
            }
            if (playDayUTC10 <= 1 && playDayUTC10 < playDay) {
                playDayUTC10 = playDay;
            }
        }

        private void _CheckAddLocalPlayDay()
        {
            var now = Game.TimestampNow();
            if (now >= nextRefreshLocalDay)
            {
                needNotifyLocalDay = true;
                _UpdateLocalPlayDayRefreshTime();
            }
        }

        private void _NoticePlayDayChange()
        {
            needNotify = false;
            MessageCenter.Get<MSG.GAME_DAY_CHANGE>().Dispatch();
        }

        private void _NoticeLocalPlayDayChange()
        {
            needNotifyLocalDay = false;
            MessageCenter.Get<MSG.GAME_LOCAL_DAY_CHANGE>().Dispatch();
            DebugEx.FormatInfo("AccountMan::_NoticeLocalPlayDayChange");
        }

        public void RecordActiveTime(bool haveOperate)
        {
            var ts = Game.TimestampNow();
            var interval = (int)(ts - lastOperateTS);
            if (interval < 60)         //in 1 minute
            {
                if (haveOperate && interval > 0)
                {
                    activeSeconds += interval;
                    lastOperateTS = ts;
                }
            }
            else
            {
                activeSeconds = 0;
                if (haveOperate)
                {
                    lastOperateTS = ts;
                }
            }
        }

        void IServerDataReceiver.OnReceiveServerData(ServerData data)
        {
            uid = Game.Manager.networkMan.remoteUid;
        }

        void IUserDataHolder.SetData(LocalSaveData data)
        {
            createAt = data.PlayerBaseData.CreatedAt;
            strDataStorage.Clear();
            var generalData = data.ClientData.PlayerGeneralData;
            if (generalData.ClientStorage != null)
            {
                foreach (var entry in generalData.ClientStorage.Data)
                {
                    strDataStorage[entry.Key] = entry.Value;
                }
            }
            var baseData = data.PlayerBaseData;
            playDay = baseData.PlayDay;
            playDayTs = baseData.PlayDayTs;
            isSynced = true;
            uid = baseData.Uid;
            _UpdatePlayDayRefreshTime();
            _CheckAddPlayDay();
            activeSeconds = baseData.ActiveSeconds;
            _UpdateLocalPlayDayRefreshTime();
            needNotifyLocalDay = true;
        }

        void IUserDataHolder.FillData(LocalSaveData data)
        {
            var generalData = data.ClientData.PlayerGeneralData;
            if (strDataStorage.Count > 0)
            {
                generalData.ClientStorage = new ClientStorageState();
                foreach (var entry in strDataStorage)
                {
                    generalData.ClientStorage.Data[entry.Key] = entry.Value;
                }
            }
            // data.Uid = mUid;
            var baseData = data.PlayerBaseData;
            if (baseData != null)
            {
                baseData.PlayDay = playDay;
                baseData.PlayDayTs = playDayTs;
                baseData.ActiveSeconds = activeSeconds;
                baseData.Language = I18N.GetLanguage();
            }
        }

        void IGameModule.Reset()
        {
            strDataStorage.Clear();
        }
        void IGameModule.LoadConfig() { }
        void IGameModule.Startup() {
            IEnumerator R() {
                var n = 0;
                var wait = new WaitForSeconds(180);
                while (n < 5) {
                    PlatformSDK.Instance.shareLink.LinkPayload(r_ => {
                        referrer = r_;
                        if (referrer.Valid && CheckReferer()) MessageCenter.Get<GAME_MERGE_LEVEL_CHANGE>().AddListenerUnique(_ => CheckReferer());
                    });
                    if (referrer.Valid) break;
                    yield return wait;
                }
            }
            Game.Instance.StartCoroutineGlobal(R());
        }

        void IUpdate.Update(float dt)
        {
            Update();
        }

        public bool CheckReferer() {
            DebugEx.Info($"deeplink referer check {referrer}");
            if (!referrer.Valid || referrer.fullfill) return false;
            var level = Game.Manager.mergeLevelMan.level;
            var target = referrer.target;
            if (level < target) return true;
            DebugEx.Info($"deeplink referer target match {referrer.id} {referrer.ts} {level}>={target}");
            IEnumerator R() {
                var task = Game.Manager.networkMan.DeeplinkFullfill(referrer.id, referrer.ts);
                yield return task;
                if (!task.isSuccess || task.result is not DeeplinkInviteResp resp) {
                    DebugEx.Error($"failed to fullfill invite ({task.errorCode}){task.error} {task.result}");
                    yield break;
                }
                SetClientStorage(nameof(Referrer), "t");
                Game.Manager.archiveMan.SendImmediately(true);
                DataTracker.DeeplinkFullfill(referrer);
            }
            Game.StartCoroutine(R());
            return false;
        }

        public void Test() {
            var sdk = PlatformSDK.Instance.Adapter;
            var fpid = sdk.SessionId;
            var ts = Game.TimestampNow();
            IEnumerator R() {
                var task = Game.Manager.networkMan.DeeplinkFullfill(fpid, ts);
                yield return task;
                if (!task.isSuccess || task.result is not DeeplinkInviteResp resp) {
                    DebugEx.Error($"failed to fullfill invite ({task.errorCode}){task.error} {task.result}");
                    yield break;
                }
                yield return new WaitForSeconds(1);
                var task2 = Game.Manager.networkMan.DeeplinkStat(ts);
                yield return task;
                if (!task.isSuccess || task.result is not GetInviteeStatResp resp2) {
                    DebugEx.Error($"failed to check invite stat ({task.errorCode}){task.error} {task.result}");
                    yield break;
                }
                DebugEx.Info($"deeplink service test {resp2.InviteeNum}");
            }
            Game.StartCoroutine(R());
        }
        
        public static void TryRate(int storyId, MapBuilding target) {
            if (storyId > 0 && target.Maxed && Game.Manager.configMan.globalConfig.RateBuildingBase.Contains(target.Id))
            {
                string rated;
                if (Game.Manager.accountMan.TryGetClientStorage(Constant.kHaveRated, out rated))
                {
                    int result;
                    if (int.TryParse(rated, out result))
                    {
                        if (result == 0)
                        {
                            UIManager.Instance.OpenWindow(UIConfig.UIRate);
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("[SETTING] bad storage value. {0} {1}", Constant.kHaveRated, result);
                    }
                }
                else
                {
                    Game.Manager.accountMan.SetClientStorage(Constant.kHaveRated, 0.ToString());
                    UIManager.Instance.OpenWindow(UIConfig.UIRate);
                }
            }
        }
    }
}