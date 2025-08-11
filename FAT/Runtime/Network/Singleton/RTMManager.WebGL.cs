/**
 * @Author: handong.liu
 * @Date: 2022-08-16 15:03:20
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using System.Runtime.InteropServices;

#if !UNITY_EDITOR && UNITY_WEBGL
public class RTMManagerAgent : MonoBehaviour
{
    public event System.Action<string> onLogin;
    public event System.Action<string> onPushMessage;
    public event System.Action<string> onLogout;
    public void OnLogin(string detail)
    {
        DebugEx.FormatInfo("RTMManager::OnLogin ----> {0}", detail);
        onLogin?.Invoke(detail);
    }
    public void OnPushMessage(string detail)
    {
        DebugEx.FormatInfo("RTMManager::OnPushMessage ----> {0}", detail);
        onPushMessage?.Invoke(detail);
    }
    public void OnLogout(string detail)
    {
        DebugEx.FormatInfo("RTMManager::OnLogout ----> {0}", detail);
        onLogout?.Invoke(detail);
    }
}

public partial class RTMManager
{
    private delegate void MessageFunc(string json);
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    public static extern void RTMInit(string endPoint, int timeout, int pid);
    [DllImport("__Internal")]
    public static extern void RTMDeinit();
    [DllImport("__Internal")]
    public static extern void RTMLogin(string uid, string token);
    [DllImport("__Internal")]
    public static extern void RTMLogout();
    #else
    public static void RTMInit(string endPoint, int timeout, int pid) {}
    public static void RTMDeinit() {}
    public static void RTMLogin(string uid, string token) {}
    public static void RTMLogout() {}
    #endif
    private RTMManagerAgent mAgent;


    // #if !UNITY_EDITOR && UNITY_WEBGL
    public void ClearAll()
    {
        if(mInited)
        {
            if(mAuthed)
            {
                RTMLogout();
                mAuthed = false;
            }
            RTMDeinit();
            mInited = false;
        }
        mAuthed = false;
    }

    public void Init()
    {
        if(mAgent == null)
        {
            mAgent = new GameObject("RTMAgent").AddComponent<RTMManagerAgent>();
            mAgent.onLogin += _OnLogin;
            mAgent.onPushMessage += _OnPushMessage;
            mAgent.onLogout += _OnLogoutMessage;
        }
    }
    
    public void Login(string token, string endPoint, int pid, ulong uid)
    {
        RTMInit(endPoint, 60, pid);
        mInited = true;
        RTMLogin(uid.ToString(), token);
    }
    

    [System.Serializable]
    public class RTMError
    {
        public long errCode;
        public string errMsg;
    }
    [System.Serializable]
    public class RTMMsg
    {
        public byte mtype;
        public string msg;
    }
    private void _OnLogin(string detail)
    {
        RTMError err = JsonUtility.FromJson<RTMError>(detail);
        if(err.errCode == 0)
        {
            mAuthed = true;
        }
        else
        {
            DebugEx.FormatWarning("RTMManager::_OnRTMMessage: Login Fail {0}", detail);
        }
    }
    private void _OnLogoutMessage(string detail)
    {
        _OnLogout();
    }
    private void _OnPushMessage(string detail) 
    {
        DebugEx.FormatWarning("RTMManager::_OnRTMMessage: message {0}", detail);
        RTMMsg msg = JsonUtility.FromJson<RTMMsg>(detail);
        mRTMListener?.PushMessage(msg.mtype, null, msg.msg);
    }
}
#endif