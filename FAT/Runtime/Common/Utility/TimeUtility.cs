/**
 * @Author: handong.liu
 * @Date: 2020-09-23 12:06:46
 */
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EL;

public static class TimeUtility
{
    private static readonly long kEpochTick = (new DateTime(1970, 1, 1, 0, 0, 0)).Ticks;
    private static System.Text.StringBuilder sCachedString = new System.Text.StringBuilder();
    private static string sCurrentTimeZoneId;
    private static long sTimeZoneBiasMilli;
    private static void _EnsureTimeZone()
    {
        if(string.IsNullOrEmpty(sCurrentTimeZoneId))
        {
            var timeZone = System.TimeZoneInfo.Local;
            sCurrentTimeZoneId = timeZone.Id;
            sTimeZoneBiasMilli = timeZone.GetUtcOffset(DateTime.Now).Ticks / System.TimeSpan.TicksPerMillisecond;
            DebugEx.FormatInfo("TimeUtility::_EnsureTimeZone ----> timezone set to {0},{1}", timeZone.ToString(), sTimeZoneBiasMilli / 1000);
        }
    }
    public static string GetTimeZone()
    {
        _EnsureTimeZone();
        return sCurrentTimeZoneId;
    }
    public static void ResetTimeZoneData()
    {
        sCurrentTimeZoneId = null;
    }
    //单位秒
    public static long GetTimeZoneBias()
    {
        _EnsureTimeZone();
        return sTimeZoneBiasMilli / 1000;
    }
    public static long ConvertLocalMilliToUTCMilli(long local)
    {
        _EnsureTimeZone();
        return ConvertLocalMilliToUTCMilli(local, sTimeZoneBiasMilli);
    }
    public static long ConvertUTCMilliToLocalMilli(long utc)
    {
        _EnsureTimeZone();
        return ConvertUTCMilliToLocalMilli(utc, sTimeZoneBiasMilli);
    }
    public static long ConvertLocalMilliToUTCMilli(long local, long biasMilli)
    {
        return local - biasMilli;
    }
    public static long ConvertUTCMilliToLocalMilli(long utc, long biasMilli)
    {
        return utc + biasMilli;
    }
    public static long ConvertUTCSecToLocalSec(long utcSec)
    {
        return ConvertUTCMilliToLocalMilli(utcSec * 1000) / 1000;
    }
    public static long ConvertLocalSecToUTCSec(long localSec)
    {
        return ConvertLocalMilliToUTCMilli(localSec * 1000) / 1000;
    }
    public static long ConvertUTCSecToLocalSec(long utcSec, long biasSeconds)
    {
        return ConvertUTCMilliToLocalMilli(utcSec * 1000, biasSeconds * 1000) / 1000;
    }
    public static long ConvertLocalSecToUTCSec(long localSec, long biasSeconds)
    {
        return ConvertLocalMilliToUTCMilli(localSec * 1000, biasSeconds * 1000) / 1000;
    }
    public static long GetTickSinceEpoch(long tick)
    {
        return tick - kEpochTick;
    }
    public static long GetSecondsSinceEpoch(long tick)
    {
        return (tick - kEpochTick) / TimeSpan.TicksPerSecond;
    }

    //if startHour larger than endHour, means endHour is next day, but hour should always [0, 23]
    public static bool IsHourInRange(int hour, int startHour, int endHour)
    {
        if(endHour < startHour)
        {
            return hour >= startHour || hour <= endHour;
        }
        else
        {
            return hour >= startHour && hour <= endHour;
        }
    }
    public static DateTime GetDateTimeFromEpoch(long seconds, DateTimeKind kidn = DateTimeKind.Utc)
    {
        return new DateTime(kEpochTick + seconds * TimeSpan.TicksPerSecond, kidn);
    }
    public static string FormatDateTime(DateTime dt)
    {
        return dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
    }
    public static string FormatCountDown(int seconds)
    {
        TimeSpan ts = new TimeSpan(0, 0, (int)seconds);
        return string.Format("{0}:{1:mm\\:ss}", ts.TotalHours, ts);
    }
    public static long GetUTCDay(long utcSec1)
    {
        return utcSec1 / Constant.kSecondsPerDay;
    }
    public static string FormatCountDown2(int seconds)
    {
        TimeSpan ts = new TimeSpan(0, 0, (int)seconds);
        sCachedString.Clear();
        if(ts.TotalDays >= 1)
        {
            sCachedString.AppendFormat("{0}d ", ts.Days);
        }
        if(ts.TotalHours >= 1)
        {
            sCachedString.AppendFormat("{0}h ", ts.Hours);
        }
        if(ts.TotalMinutes >= 1)
        {
            sCachedString.AppendFormat("{0}m ", ts.Minutes);
        }
        sCachedString.AppendFormat("{0}s", ts.Seconds);
        return sCachedString.ToString();
    }
    public static string FormatCountDownWithLimit(int seconds, int limit = 2)
    {
        int componentNum = 1;
        TimeSpan ts = new TimeSpan(0, 0, (int)seconds);
        sCachedString.Clear();
        if(ts.TotalDays >= 1)
        {
            sCachedString.AppendFormat("{0}d", ts.Days);
            ++componentNum;
        }
        if(ts.TotalHours >= 1 && componentNum <= limit)
        {
            sCachedString.AppendFormat("{0}h", ts.Hours);
            ++componentNum;
        }
        if(ts.TotalMinutes >= 1 && componentNum <= limit)
        {
            sCachedString.AppendFormat("{0}m", ts.Minutes);
            ++componentNum;
        }
        if (componentNum <= limit)
        { 
            sCachedString.AppendFormat("{0}s", ts.Seconds);
        }
        return sCachedString.ToString();
    }
    // 最多显示两档计时单位 二级单位时间为0时省略
    public static string FormatCountDownOmitZeroTail(int seconds)
    {
        var ts = new TimeSpan(0, 0, seconds);
        sCachedString.Clear();
        // days取值范围可以跨年
        if (ts.Days > 0)
        {
            sCachedString.Append(ts.Hours > 0 ? $"{ts.Days}d{ts.Hours}h" : $"{ts.Days}d");
        }
        else if (ts.Hours > 0)
        {
            sCachedString.Append(ts.Minutes > 0 ? $"{ts.Hours}h{ts.Minutes}m" : $"{ts.Hours}h");
        }
        else if (ts.Minutes > 0)
        {
            sCachedString.Append(ts.Seconds > 0 ? $"{ts.Minutes}m{ts.Seconds}s" : $"{ts.Minutes}m");
        }
        else
        {
            sCachedString.AppendFormat("{0}s", ts.Seconds);
        }
        return sCachedString.ToString();
    }
}