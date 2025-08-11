/**
 * @Author: handong.liu
 * @Date: 2021-11-19 17:59:11
 */
using System;
using FAT;
using UnityEngine;

public static partial class DataTracker
{
    public enum LogErrorType
    {
        client,
        network_send,
        network_response,
        network_logic
    }
    [Serializable]
    private class log_error : DataTrackBase
    {
        public string msg;
        public string stack;
        public string type;
        public string request;
        public string uri;
        public string time_start;
        public int seconds_cost;
        public long error_code;
        public string http_resp;
        public string http_req_uri;
        public long http_req_size;
        public log_error Fill(string _msg, string _stackTrace, object _request, LogErrorType _type)
        {
            msg = _msg;
            stack = _stackTrace;
            request = _request == null ? "" : _request.ToString();
            uri = _request == null ? "" : _request.GetType().FullName;
            type = _type.ToString();
            time_start = "";
            seconds_cost = 0;
            return this;
        }
        public log_error Fill(string _msg, string _stackTrace, object _request, LogErrorType _type, DateTime start, DateTime end, string _base64, long errCode, string httpResp, string uri, long reqSize)
        {
            msg = _msg;
            stack = _stackTrace;
            request = _request == null ? "" : _request.ToString();
            uri = _request == null ? "" : _request.GetType().FullName;
            type = _type.ToString();
            time_start = start.ToString("o");
            seconds_cost = (int)(end - start).TotalSeconds;
            error_code = errCode;
            http_resp = httpResp;
            http_req_uri = uri;
            http_req_size = reqSize;
            return this;
        }
    }
    
    [Serializable]
    private class log_info : DataTrackBase
    {
        public string msg;
        public log_info Fill(string _msg)
        {
            msg = _msg;
            return this;
        }
    }

    public static void TrackLogError(string msg, string stackTrace, object request, LogErrorType type)
    {
        _TrackData(_GetTrackData<log_error>().Fill(msg, stackTrace, request, type));
    }

    public static void TrackNetworkError(string msg, string stackTrace, object request, LogErrorType type, DateTime start, DateTime end, string base64)
    {
        _TrackData(_GetTrackData<log_error>().Fill(msg, stackTrace, request, type, start, end, base64, 0, "", "", 0));
    }

    public static void TrackNetworkError(string msg, string stackTrace, object request, LogErrorType type, DateTime start, DateTime end, string base64, long errCode, string httpResp, string uri, long reqSize)
    {
        _TrackData(_GetTrackData<log_error>().Fill(msg, stackTrace, request, type, start, end, base64, errCode, httpResp, uri, reqSize));
    }
    
    public static void TrackLogInfo(string msg)
    {
        _TrackData(_GetTrackData<log_info>().Fill(msg));
    }

    public static void TrackNetWeakRestart(long beginTime, long endTime, int startError, int recentError)
    {
        var name = $"netweak_restart";
        var data = BorrowTrackObject();
        data["begin_time"] = TimeUtility.GetDateTimeFromEpoch(beginTime).ToShortTimeString();
        data["end_time"] = TimeUtility.GetDateTimeFromEpoch(endTime).ToShortTimeString();
        data["start_error"] = startError;
        data["recent_error"] = recentError;
        TrackObject(data, name);
    }

    public static void InitUnityErrorLog()
    {
        Application.logMessageReceivedThreaded -= _OnUnityLogMessage;
        Application.logMessageReceivedThreaded += _OnUnityLogMessage;
    }

    private static void _OnUnityLogMessage(string msg, string stack, LogType type)
    {
#if !UNITY_EDITOR
        if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
        {
            EL.ThreadDispatcher.DefaultDispatcher.CallOrDispatch(() =>
            {
                DataTracker.TrackLogError(msg, stack, null, LogErrorType.client);
            });
        }
#endif
    }
}