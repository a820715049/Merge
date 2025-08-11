/**
 * @Author: yingbo.li
 * @Doc: https://centurygames.feishu.cn/wiki/JLX5wnl61ijHlakyeQrctrO0nkc
 * @Date: 2023-12-26
 */
using UnityEngine;
using System.Collections;
using System;
using fat.rawdata;
using EL;
using static EL.MessageCenter;
using FAT.MSG;
using static fat.conf.Data;
using Unity.Notifications;
using NoticeInstance = Unity.Notifications.Notification;
using System.Collections.Generic;
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace FAT
{
    public class DeviceNotification : IGameModule, IUserDataHolder
    {
        [Serializable]
        public class NotificationCustomData
        {
            public int eventId;
            public int eventFrom;
            public int eventParam;
            public string eventType;
            public int noticeDetailId;    // 通知模板id
        }

        [Serializable]
        class EventNotice
        {
            // 活动id
            public int id;
            // 活动from
            public int from;
            // 活动参数
            // timestamp seconds
            public int time;
            // 通知模板id
            public int templateId;
        }

        [Serializable]
        class NotificationData
        {
            // 账号
            public string fpid;
            // utc day
            public int popDay;
            // 弹窗次数
            public int popCount;
            // 体力购买弹窗时间戳
            public int remindTimeEnergy;
            // 商城关闭弹窗时间戳
            public int remindTimeShop;
            // 切换到meta弹窗时间戳
            public int remindTimeMeta;
            // 活动通知
            public List<EventNotice> eventNotices = new();
        }

        public bool Init { get; set; }
        public bool PermissionGranted { get; set; }
        public bool Enabled
        {
            get => CheckEnabled();
            set => TrySetEnabled(value);
        }
        private bool requested;
        private bool requestSwitch;
        private bool redirect;
        private bool notify;
        private string Key => nameof(DeviceNotification);
        private readonly ActivityVisual visual = new();
        // 提醒弹窗不走pop系统了
        // private readonly PopupNotification popup = new();

        // 缓存按开始时间排序的活动
        private readonly List<EventTime> _sortedEvents = new();
        private readonly Dictionary<OpenNotifiPopType, OpenNoticePop> _openNoticePopMap = new();
        // 活动主动触发的通知
        // 活动可根据需求调度通知 / 更新调度 / 取消调度
        [SerializeField]
        private NotificationData _notificationData = new();

        // key: 本地存储的通知数据
        private string prefskey_data => $"{Key}_data";
        // key: 是否已经请求过权限
        private string prefskey_requested => $"{Key}_requested";
        // 达到配置的等级后才主动申请权限
        private int permissionUnlockLevel;
        // 是否已经请求过权限
        private bool permissionRequested
        {
            get => PlayerPrefs.GetInt(prefskey_requested, -1) > 0;
            set => PlayerPrefs.SetInt(prefskey_requested, 1);
        }
        // 是否允许申请权限
        private bool allowRequestPermission => permissionRequested || Game.Manager.mergeLevelMan.level >= permissionUnlockLevel;

        public void Reset()
        {
            // Get<SCREEN_POPUP_QUERY>().AddListenerUnique(PopupQuery);
        }

        public void Startup()
        {
            permissionUnlockLevel = Game.Manager.configMan.globalConfig.NotificationApplicationLevel;

            // SetupPopup();
            LoadPrefsData();
            CheckInit();
            CheckPermission();
            // 上线时应该清除之前的推送
            Clear();
        }

        public void LoadConfig()
        {
            _openNoticePopMap.Clear();
            foreach (var n in GetOpenNoticePopSlice())
            {
                _openNoticePopMap.Add(n.NoticePopId, n);
            }

            _sortedEvents.Clear();
            var events = GetEventTimeMap();
            foreach (var (_, evt) in events)
            {
                _sortedEvents.Add(evt);
            }
            _sortedEvents.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        }

        // private void SetupPopup()
        // {
        //     var ui = new UIResAlt(UIConfig.UINotificationRemind);
        //     var theme = Game.Manager.configMan.globalConfig.NotifiPopupEventTheme;
        //     if (visual.Setup(theme, ui)) popup.Setup(visual, ui.ActiveR);
        // }

        // 体力恢复提示弹窗逻辑变更
        // internal void PopupQuery(ScreenPopup popup_, PopupType state_)
        // {
        //     if (Game.Manager.mergeLevelMan.displayLevel >= Game.Manager.configMan.globalConfig.NotifiPopupShutdownLv
        //         || !Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureNotificationPopup)
        //         || Enabled) return;
        //     popup_.TryQueue(popup, state_);
        // }

        private bool CanRemind()
        {
            if (Game.Manager.mergeLevelMan.level >= Game.Manager.configMan.globalConfig.NotifiPopupShutdownLv ||
                !Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureNotificationPopup) ||
                Enabled)
                return false;
            return true;
        }

        private void TryRemind(OpenNotifiPopType type_)
        {
            var ui = UIManager.Instance;
            if (ui.LoadingCount > 0) return;
            if (ui.IsBlocking() || ui.IsBlocked) return;
            if (!CanRemind()) return;

            // 今日弹窗次数是否达到限制
            if (IsRemindPopCountReachLimit()) return;
            // 当前类别弹窗是否在CD中
            var pop = _openNoticePopMap[type_];
            var now = Game.TimestampNow();
            if (now < GetRemindTimeByType(type_) + pop.PopCd) return;

            // 更新今日计数
            UpdateRemindPopCountInfo();
            // 更新当前类别CD时间
            SetRemindTimeByType(type_, (int)now);

            var uiRes = GetRemindUIByType(type_);
            if (uiRes == null) return;
            ui.OpenWindow(uiRes, type_);
        }

        public void TryRemindEnergy()
        {
            TryRemind(OpenNotifiPopType.Energy);
        }

        public void TryRemindShopClose()
        {
            TryRemind(OpenNotifiPopType.CloseShop);
        }

        public void TryRemindSwitchScene()
        {
            TryRemind(OpenNotifiPopType.SwitchScene);
        }

        private bool IsRemindPopCountReachLimit()
        {
            var count = _notificationData.popCount;
            var day = Game.UtcNow.DayOfYear;
            if (_notificationData.popDay != day)
            {
                count = 0;
            }
            return count >= Game.Manager.configMan.globalConfig.NotificationPopLimit;
        }

        private void UpdateRemindPopCountInfo()
        {
            var day = Game.UtcNow.DayOfYear;
            var data = _notificationData;
            if (data.popDay != day)
            {
                data.popDay = day;
                data.popCount = 0;
            }
            data.popCount++;
        }

        private UIResource GetRemindUIByType(OpenNotifiPopType type_)
        {
            return type_ switch
            {
                OpenNotifiPopType.Energy => UIConfig.UINotificationRemind,
                OpenNotifiPopType.CloseShop => UIConfig.UINotificationRemindActivity,
                OpenNotifiPopType.SwitchScene => UIConfig.UINotificationRemindActivity,
                _ => null,
            };
        }

        private int GetRemindTimeByType(OpenNotifiPopType type_)
        {
            var data = _notificationData;
            return type_ switch
            {
                OpenNotifiPopType.Energy => data.remindTimeEnergy,
                OpenNotifiPopType.CloseShop => data.remindTimeShop,
                OpenNotifiPopType.SwitchScene => data.remindTimeMeta,
                _ => 0,
            };
        }

        private void SetRemindTimeByType(OpenNotifiPopType type_, int time_)
        {
            var data = _notificationData;
            switch (type_)
            {
                case OpenNotifiPopType.Energy: data.remindTimeEnergy = time_; break;
                case OpenNotifiPopType.CloseShop: data.remindTimeShop = time_; break;
                case OpenNotifiPopType.SwitchScene: data.remindTimeMeta = time_; break;
                default: break;
            }
        }

        public void DebugTest()
        {
            CheckPermission();
            DeviceNotificationHelper.debug = !DeviceNotificationHelper.debug;
            ScheduleInterval(new() { Title = DeviceNotificationHelper.debug ? "Debug On" : "Debug Off", Text = "now+5s", Identifier = 1005, Data = "data 5s" }, new(0, 0, 5));
            ScheduleInterval(new() { Title = "test 10s", Text = "now+10s", Identifier = 1010, Data = "data 10s" }, new(0, 0, 10));
        }

        public void DebugInfo()
        {
            var v = PlayerPrefs.GetInt(Key, -1);
            var str = $"{Key} user:{v} permission:{PermissionGranted} enable:{Enabled}";
            Game.Manager.commonTipsMan.ShowMessageTips(str, isSingle: true);
        }

        public void ToForeground()
        {
            if (!Init) return;
            DebugEx.Info($"{Key} to fore {requested} {requestSwitch}");
            if (!requestSwitch) CheckPermission();
            requestSwitch = false;
            Clear();
            CheckReceive();
        }

        public void ToBackground()
        {
            if (!Init) return;
            requestSwitch = requested;
            DebugEx.Info($"{Key} to back {requested} {requestSwitch}");
            Reschedule();
        }

        public bool CheckEnabled()
        {
            var k = Key;
            var v = PlayerPrefs.GetInt(k, -1);
            return v != 0 && PermissionGranted;
        }

        public bool TrySetEnabledWithNotify(bool v_)
        {
            notify = true;
            return TrySetEnabled(v_);
        }

        public bool TrySetEnabled(bool v_)
        {
            if (!PermissionGranted && v_)
            {
                // 如果从未申请过 则应该直接申请
                if (!permissionRequested)
                {
                    CheckPermission(true);
                    return false;
                }

                OpenRedirect();
                v_ = false;
                goto end;
            }
            var k = Key;
            PlayerPrefs.SetInt(k, v_ ? 1 : 0);
        end:
            DebugEx.Info($"{Key} state {v_}");
            Get<NOTIFICATION_STATE>().Dispatch(v_);
            DataTracker.notification.Track(v_);
            if (v_ && notify)
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.NotificationOn);
            }
            notify = false;
            return v_;
        }

        public void ToggleEnabled()
        {
            Enabled = !Enabled;
        }

        public void OpenRedirect()
        {
            var target = UIConfig.UINotificationRedirect;
            var ui = UIManager.Instance;
            if (ui.IsOpen(target)) return;
            ui.OpenWindow(target);
        }

        public void RedirectToSetting()
        {
            redirect = true;
            NotificationCenter.OpenNotificationSettings();
            DebugEx.Info($"{Key} redirect to settings");
        }

        public void CheckRedirectResult()
        {
            if (!redirect) return;
            redirect = false;
            var v = TrySetEnabled(true);
            if (v)
            {
                UIManager.Instance.CloseWindow(UIConfig.UINotificationRedirect);
                Game.Manager.commonTipsMan.ShowPopTips(Toast.NotificationOn);
            }
        }

        public void Clear()
        {
            NotificationCenter.CancelAllDeliveredNotifications();
            NotificationCenter.CancelAllScheduledNotifications();
            DebugEx.Info($"{Key} clear");
        }

        private void CheckInit()
        {
            if (Init) return;
            var args = NotificationCenterArgs.Default;
            args.AndroidChannelId = "default";
            args.AndroidChannelName = "Notifications";
            args.AndroidChannelDescription = "game notifications";
            NotificationCenter.Initialize(args);
            Init = true;
        }

        public void CheckPermission(bool force_ = false)
        {
            IEnumerator Routine()
            {
                requested = true;
                DebugEx.Info($"{Key} permission requested");
                var request = NotificationCenter.RequestPermission();
                if (request.Status == NotificationsPermissionStatus.RequestPending)
                    yield return request;
                requested = false;
                // 记录是否请求过权限
                permissionRequested = true;
                PermissionGranted = request.Status == NotificationsPermissionStatus.Granted;
                DebugEx.Info($"{Key} permission status:{request.Status}");
                CheckRedirectResult();
                DataTracker.TraceUser().Notification(Enabled).Apply();
            }
            if (force_ || allowRequestPermission)
            {
                Game.Instance.StartCoroutineGlobal(Routine());
            }
        }

        private void CheckReceive()
        {
            var resp = NotificationCenter.LastRespondedNotification;
            if (!resp.HasValue) return;
            var notice = resp.Value;
            DebugEx.Info($"{Key} responded:{(NoticeType)notice.Identifier}");
        }

        public void Reschedule()
        {
            if (!Enabled) return;
            DebugEx.Info($"{Key} reschedule");
            ScheduleEnergy();
            ScheduleComeback();
            ScheduleEvent();
            ScheduleHeldNotification();
        }

        public void ScheduleEnergy()
        {
            var t = Game.Manager.mergeEnergyMan.FullRecoverCD();
            if (t <= 0) return;
            var (valid, notice, detail) = Create(NoticeType.EnergyFull);
            var data = new NotificationCustomData { eventType = nameof(NoticeType.EnergyFull), noticeDetailId = detail.Id };
            notice.Data = JsonUtility.ToJson(data);
            if (valid) DeviceNotificationHelper.ScheduleNotification(ref notice, new(0, 0, t), detail.Image);
        }

        public void ScheduleComeback()
        {
            var t = Game.Manager.configMan.globalConfig.NoticeCombackTime;
            var (valid, notice, detail) = Create(NoticeType.Comeback);
            var data = new NotificationCustomData { eventType = nameof(NoticeType.Comeback), noticeDetailId = detail.Id };
            notice.Data = JsonUtility.ToJson(data);
            if (valid) DeviceNotificationHelper.ScheduleNotification(ref notice, new(0, 0, t), detail.Image);
        }

        // 处理活动开始结束通知
        public void ScheduleEvent()
        {
            // 仅处理n日内的活动
            var checkRange = Game.Manager.configMan.globalConfig.NoticeEventValidTime;
            var begin = Game.TimestampNow();
            var end = begin + 86400 * checkRange;
            var acts = _sortedEvents;

            // 提前检查30天 避免错过一些长周期活动的结束时间点
            var nextEventIdx = FindNextEventIdxByStartTime(begin - 86400 * 30);
            if (nextEventIdx < 0) nextEventIdx = 0;

#if UNITY_EDITOR
            var evt = acts[nextEventIdx];
            var startDate = DateTimeOffset.FromUnixTimeSeconds(evt.StartTime).ToString("yyyy-MM-dd HH:mm:ss");
            var endDate = DateTimeOffset.FromUnixTimeSeconds(evt.EndTime).ToString("yyyy-MM-dd HH:mm:ss");
            DebugEx.Info($"{Key} nextEventIdx:{nextEventIdx} StartTime:{startDate} EndTime:{endDate}");
#endif

            using var _ = PoolMapping.PoolMappingAccess.Borrow(out Dictionary<fat.rawdata.EventType, bool> readyCache);
            for (var i = nextEventIdx; i < acts.Count; i++)
            {
                var act = acts[i];
                if (act.StartTime > end) break;
                if (act.EndTime < begin) continue;

                // 检查活动feature
                var group = Game.Manager.activity.GroupOf(act.EventType);
                if (group == null) continue;
                var (valid, _) = group.CreateCheck(act.EventType, new(act.Id, 0, act.EventParam, act));
                if (!valid) continue;

                // 检查活动时间trigger
                if (!readyCache.TryGetValue(act.EventType, out var ready))
                {
                    var (trigger_ready, _) = Game.Manager.activity.CheckTypeReady(act.EventType);
                    readyCache[act.EventType] = trigger_ready;
                    ready = trigger_ready;
                }
                if (!ready) continue;

                if (act.NoticeGroup.Count > 0)
                {
                    foreach (var id in act.NoticeGroup)
                    {
                        var notiConf = GetNoticeEvent(id);
                        if (!notiConf.IsOn) continue;
                        if (notiConf.NoticeTimingType == NoticeTimingType.TimingTypeStart)
                        {
                            var fireTime = act.StartTime - notiConf.TimeDiff;
                            if (fireTime > begin)
                            {
                                var asset = notiConf.Asset.RandomChooseByWeight();
                                var data = GetNotificationCustomData(act.Id, 0, act.EventParam, act.EventType, asset);
                                ScheduleNoticeWithTemplate(asset, new(0, 0, (int)(fireTime - begin)), data);
                            }
                        }
                        else if (notiConf.NoticeTimingType == NoticeTimingType.TimingTypeEnd)
                        {
                            var fireTime = act.EndTime - notiConf.TimeDiff;
                            if (fireTime > begin)
                            {
                                var asset = notiConf.Asset.RandomChooseByWeight();
                                var data = GetNotificationCustomData(act.Id, 0, act.EventParam, act.EventType, asset);
                                ScheduleNoticeWithTemplate(asset, new(0, 0, (int)(fireTime - begin)), data);
                            }
                        }
                    }
                }
            }
        }

        private NotificationCustomData GetNotificationCustomData(int id, int from, int param, fat.rawdata.EventType eventType, int noticeDetailId)
        {
            var data = new NotificationCustomData
            {
                eventId = id,
                eventFrom = from,
                eventParam = param,
                eventType = eventType.ToString(),
                noticeDetailId = noticeDetailId,
            };
            return data;
        }

        private int FindNextEventIdxByStartTime(long time_)
        {
            var left = 0;
            var right = _sortedEvents.Count - 1;
            var result = -1;
            while (left <= right)
            {
                var mid = left + (right - left) / 2;
                if (_sortedEvents[mid].StartTime > time_)
                {
                    result = mid;
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }
            return result;
        }

        #region 活动特殊通知

        public void SetEventNotice(int eventId, int eventFrom, int eventParam, fat.rawdata.EventType eventType, int time_, int templateId_)
        {
            foreach (var n in _notificationData.eventNotices)
            {
                if (n.id == eventId && n.from == eventFrom)
                {
                    n.templateId = templateId_;
                    n.time = time_;
                    return;
                }
            }
            var notice = new EventNotice
            {
                id = eventId,
                from = eventFrom,
                time = time_,
                templateId = templateId_,
            };
            _notificationData.eventNotices.Add(notice);
        }

        public void CancelEventNotice(int eventId, int eventFrom)
        {
            var notices = _notificationData.eventNotices;
            for (var i = 0; i < notices.Count; i++)
            {
                if (notices[i].id == eventId && notices[i].from == eventFrom)
                {
                    notices.RemoveAt(i);
                    return;
                }
            }
        }

        private void ScheduleHeldNotification()
        {
            var now = Game.TimestampNow();
            var acts = Game.Manager.activity.map;
            var notices = _notificationData.eventNotices;
            for (var i = notices.Count - 1; i >= 0; i--)
            {
                var notice = notices[i];
                if (notice.time < now)
                {
                    notices.RemoveAt(i);
                }
                else
                {
                    if (acts.TryGetValue((notice.id, notice.from), out var act) && act.Valid)
                    {
                        // 活动有效则调度通知
                        var data = GetNotificationCustomData(act.Id, act.From, act.Param, act.Type, notice.templateId);
                        ScheduleNoticeWithTemplate(notice.templateId, new(0, 0, (int)(notice.time - now)), data);
                    }
                    else
                    {
                        notices.RemoveAt(i);
                    }
                }
            }
        }

        // 直接保存到prefs里
        private void SavePrefsData()
        {
            _notificationData.fpid = Game.Manager.networkMan.fpId;
            var json = JsonUtility.ToJson(_notificationData);
            DebugEx.Info($"{Key} save notification data: {json}");
            PlayerPrefs.SetString(prefskey_data, json);
        }

        private void LoadPrefsData()
        {
            var json = PlayerPrefs.GetString(prefskey_data);
            try
            {
                _notificationData.eventNotices.Clear();
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonUtility.FromJson<NotificationData>(json);
                    if (data != null && data.fpid == Game.Manager.networkMan.fpId)
                    {
                        _notificationData = data;
                    }
                }
            }
            catch (Exception e)
            {
                DebugEx.Error($"{Key} load notification data failed: {e.Message}");
            }
        }

        void IUserDataHolder.SetData(fat.gamekitdata.LocalSaveData archive) { }
        // 上报存档数据时 顺便save
        void IUserDataHolder.FillData(fat.gamekitdata.LocalSaveData archive) { SavePrefsData(); }

        #endregion

        public void ScheduleNoticeWithTemplate(int templateId_, TimeSpan interval_, NotificationCustomData customData_)
        {
            var (valid, notice, detail) = Create(templateId_);
            if (!valid) return;
            notice.Data = JsonUtility.ToJson(customData_);
            DeviceNotificationHelper.ScheduleNotification(ref notice, interval_, detail.Image);
        }

        public (bool, NoticeInstance, NoticeDetail) Create(NoticeType id_)
        {
            static (NoticeInstance, NoticeDetail) CreateFrom(NoticeInfo conf_, int id_)
            {
                var detail = GetNoticeDetail(conf_.Asset.RandomChooseByWeight());
                return (new()
                    {
                        Title = I18N.Text(detail.Title),
                        Text = I18N.Text(detail.Text),
                        Identifier = id_,
                    },
                    detail);
            }
            foreach (var n in GetNoticeInfoSlice())
            {
                if (n.NoticeId == id_)
                {
                    var (notice, detail) = CreateFrom(n, (int)id_);
                    return (true, notice, detail);
                }
            }
            DebugEx.Warning($"{Key} config for {id_} not found");
            return (false, default, default);
        }

        public (bool, NoticeInstance, NoticeDetail) Create(int templateId_, int? identifier_ = null)
        {
            static NoticeInstance CreateFrom(NoticeDetail detail_, int? id_)
            {
                return new()
                {
                    Title = I18N.Text(detail_.Title),
                    Text = I18N.Text(detail_.Text),
                    Identifier = id_,
                };
            }
            var detail = GetNoticeDetail(templateId_);
            if (detail == null)
            {
                DebugEx.Warning($"{Key} config for {templateId_} not found");
                return (false, default, default);
            }
            var notice = CreateFrom(detail, identifier_);
            return (true, notice, detail);
        }

        public void ScheduleTime(NoticeInstance notice_, DateTime time_)
        {
            var schedule = new NotificationDateTimeSchedule(time_);
            NotificationCenter.ScheduleNotification(notice_, schedule);
            DebugEx.Info($"{Key} schedule {notice_.Title} @{time_}");
        }

        public void ScheduleInterval(NoticeInstance notice_, TimeSpan interval_)
        {
            var schedule = new NotificationIntervalSchedule(interval_);
            NotificationCenter.ScheduleNotification(notice_, schedule);
            DebugEx.Info($"{Key} schedule {notice_.Title} @{interval_} later");
        }
    }
}