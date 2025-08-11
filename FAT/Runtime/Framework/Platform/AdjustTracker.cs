/*
 * @Author: tang.yan
 * @Description: Adjust打点
 * @Date: 2023-12-20 17:12:23
 */
using EL;
using FAT;
using FAT.Platform;

//https://docs.google.com/spreadsheets/d/1haXzuHWdajB50320kzYiq-rxon3kaEhyUsHJZWp7irY/edit#gid=0
public static class AdjustTracker
{
    private static InstallTrackConfig mConfig;

    public static void SetConfig(InstallTrackConfig config)
    {
        mConfig = config;
    }

    public static void TrackIAP(string token)
    {
        if (string.IsNullOrEmpty(token))
            return;
        DebugEx.Info($"AdjustTracker::TrackEvent ----> track iap {token}");
        PlatformSDK.Instance.TrackAdjust(token);
    }

    public static void TrackEvent(AdjustEventType type)
    {
        var events = mConfig?.other;
        if(events == null)
        {
            DebugEx.FormatInfo("AdjustTracker::TrackEvent ----> no event {0}", type);
            return;
        }
        var e = events.FindEx((a) => a.type == type);
        if(e == null)
        {
            DebugEx.FormatInfo("AdjustTracker::TrackEvent ----> no event {0}", type);
            return;
        }
        DebugEx.FormatInfo("AdjustTracker::TrackEvent ----> track {0}", type);
        _SendTrack(e);
    }
    
    //记录一个账号只打一次的事件
    public static void TrackOnceEvent(AdjustEventType type, string storageKey)
    {
        var events = mConfig?.other;
        if(events == null)
        {
            DebugEx.FormatInfo("AdjustTracker::TrackOnceEvent ----> no event {0}", type);
            return;
        }
        var e = events.FindEx((a) => a.type == type);
        if(e == null)
        {
            DebugEx.FormatInfo("AdjustTracker::TrackOnceEvent ----> no event {0}", type);
            return;
        }
        //根据存档确定是否打点 没找到对应key的数据 说明没打点
        if (!Game.Manager.accountMan.TryGetClientStorage(storageKey, out _))
        {
            Game.Manager.accountMan.SetClientStorage(storageKey, "1");
            DebugEx.FormatInfo("AdjustTracker::TrackOnceEvent ----> track {0}", type);
            _SendTrack(e);
        }
    }

    public static void TrackLevelEvent(int level)
    {
        var events = mConfig?.levels;
        if(events == null)
        {
            DebugEx.FormatInfo("AdjustTracker::TrackLevelEvent ----> no level event {0}", level);
            return;
        }
        var e = events.FindEx((ea) => ea.intValue == level);
        if(e == null)
        {
            DebugEx.FormatInfo("AdjustTracker::TrackLevelEvent ----> no level event {0}", level);
            return;
        }
        DebugEx.FormatInfo("AdjustTracker::TrackLevelEvent ----> track level:{0}", level);

        _SendTrack(e);
    }
    
    public static void TrackIAPPackEvent(int packageId)
    {
        var events = mConfig?.purchase;
        if(events == null)
        {
            DebugEx.FormatInfo("AdjustTracker::TrackIAPPackEvent ----> no purchase event {0}", packageId);
            return;
        }
        var e = events.FindEx((ea) => ea.intValue == packageId);
        if(e == null)
        {
            DebugEx.FormatInfo("AdjustTracker::TrackIAPPackEvent ----> no purchase event {0}", packageId);
            return;
        }
        DebugEx.FormatInfo("AdjustTracker::TrackIAPPackEvent ----> track purchase:{0}", packageId);

        _SendTrack(e);
    }
    
    //记录一个账号只打一次的购买礼包事件
    public static void TrackIAPPackOnceEvent(int packageId)
    {
        var events = mConfig?.purchase;
        if(events == null)
        {
            DebugEx.FormatInfo("AdjustTracker::TrackIAPPackOnceEvent ----> no purchase event {0}", packageId);
            return;
        }
        var e = events.FindEx((ea) => ea.intValue == packageId);
        if(e == null)
        {
            DebugEx.FormatInfo("AdjustTracker::TrackIAPPackOnceEvent ----> no purchase event {0}", packageId);
            return;
        }
        string storageKey = "TrackIAPPack" + packageId;
        //根据存档确定是否打点 没找到对应key的数据 说明没打点
        if (!Game.Manager.accountMan.TryGetClientStorage(storageKey, out _))
        {
            Game.Manager.accountMan.SetClientStorage(storageKey, "1");
            DebugEx.FormatInfo("AdjustTracker::TrackIAPPackOnceEvent ----> track purchase:{0}", packageId);
            _SendTrack(e);
        }
    }

    private static void _SendTrack(InstallTypeEvent evt)
    {
        if (evt.isSendFirebase && !string.IsNullOrEmpty(evt.firebaseToken))
        {
            PlatformSDK.Instance.TrackFirebase(evt.firebaseToken);
        }

        if (evt.isSendAdjust && !string.IsNullOrEmpty(evt.adjustToken))
        {
            PlatformSDK.Instance.TrackAdjust(evt.adjustToken);
        }
    }

    private static void _SendTrack(InstallIntEvent evt)
    {
        if (evt.isSendFirebase && !string.IsNullOrEmpty(evt.firebaseToken))
        {
            PlatformSDK.Instance.TrackFirebase(evt.firebaseToken);
        }

        if (evt.isSendAdjust && !string.IsNullOrEmpty(evt.adjustToken))
        {
            PlatformSDK.Instance.TrackAdjust(evt.adjustToken);
        }
    }
}