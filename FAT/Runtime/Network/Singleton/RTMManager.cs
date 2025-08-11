/**
 * @Author: handong.liu
 * @Date: 2021-06-21 14:24:07
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using com.fpnn.rtm;
using com.fpnn;

public interface IRTMListener
{
    void PushMessage(byte type, byte[] binarymessage, string stringmessage);
    void OnRTMLogout();
}

public partial class RTMManager : EL.Singleton<RTMManager>
{
    public bool needAuth => !mAuthed && (mLoginTask == null || !mLoginTask.keepWaiting);
    private string mToken;
    private string mEndPoint;
    private int mPid;
    private AsyncTaskBase mLoginTask;
    private IRTMListener mRTMListener;
    private bool mInited = false;
    private bool mAuthed = false;
    private long mUid = 0;
    
    public void SetDelegate(IRTMListener del)
    {
        mRTMListener = del;
    }

    protected void _OnLogout()
    {
        mAuthed = false;
        ThreadDispatcher.DefaultDispatcher.CallOrDispatch(()=>mRTMListener.OnRTMLogout());
    }
    #if UNITY_EDITOR || !UNITY_WEBGL

    private class MessageHandler : RTMQuestProcessor
    {
        //----------------[ System Events ]-----------------//
        public override void SessionClosed(int ClosedByErrorCode) { RTMManager.Instance._OnLogout(); }    //-- ErrorCode: com.fpnn.ErrorCode & com.fpnn.rtm.ErrorCode

        //-- Return true for starting relogin, false for stopping relogin.
        public override bool ReloginWillStart(int lastErrorCode, int retriedCount) { return true; }
        public override void ReloginCompleted(bool successful, bool retryAgain, int errorCode, int retriedCount) { if(!successful && !retryAgain) RTMManager.Instance._OnLogout(); }

        public override void Kickout() { RTMManager.Instance._OnLogout(); }
        public override void KickoutRoom(long roomId) { }

        //----------------[ Message Interfaces ]-----------------//
        //-- Messages
        public override void PushMessage(RTMMessage message) {
            ThreadDispatcher.DefaultDispatcher.CallOrDispatch(()=>RTMManager.Instance.mRTMListener?.PushMessage(message.messageType, message.binaryMessage, message.stringMessage)); 
        }
        public override void PushGroupMessage(RTMMessage message) { }
        public override void PushRoomMessage(RTMMessage message) { }
        public override void PushBroadcastMessage(RTMMessage message) { }

        //-- Chat
        public override void PushChat(RTMMessage message) { }
        public override void PushGroupChat(RTMMessage message) { }
        public override void PushRoomChat(RTMMessage message) { }
        public override void PushBroadcastChat(RTMMessage message) { }

        //-- Cmd
        public override void PushCmd(RTMMessage message) { }
        public override void PushGroupCmd(RTMMessage message) { }
        public override void PushRoomCmd(RTMMessage message) { }
        public override void PushBroadcastCmd(RTMMessage message) { }

        //-- Files
        public override void PushFile(RTMMessage message) { }
        public override void PushGroupFile(RTMMessage message) { }
        public override void PushRoomFile(RTMMessage message) { }
        public override void PushBroadcastFile(RTMMessage message) { }
    }
    private RTMClient mClient = null;
    private MessageHandler mMessageHandler = new MessageHandler();

    public void ClearAll()
    {
        if(mClient != null)
        {
            if(mAuthed)
            {
                mClient.Bye(true);
                mClient.Close();
            }
            mClient = null;
        }
        mAuthed = false;
    }

    public void Init()
    {
        if(!mInited)
        {
            mInited = true;
            ClientEngine.Init();
            RTMControlCenter.Init();
        }
    }
    
    public void Login(string token, string endPoint, int pid, ulong uid)
    {
        if(uid > long.MaxValue)
        {
            DebugEx.FormatError("RTMManager::Login ----> uid exceeded LONG_MAX, rtm will not working!");
            return;
        }
        long mRTMUid = (long)uid;
        if(mPid != pid || mEndPoint != endPoint || mUid != mRTMUid || mClient == null)
        {
            mPid = pid;
            mEndPoint = endPoint;
            mUid = mRTMUid;
            if(mClient != null)
            {
                mClient.Close();
            }
            mClient = new RTMClient(mEndPoint, pid, mUid, mMessageHandler, true);
            mAuthed = false;
        }
        if(mAuthed)
        {
            DebugEx.FormatInfo("RTMManager::Login ----> already authed!");
            return;
        }
        mToken = token;
        _DoLogin();
    }

    private void _DoLogin()
    {
        if(mLoginTask != null && mLoginTask.keepWaiting)        //a login task is already in progress
        {
            mLoginTask.Cancel();
        }
        var task = new SimpleAsyncTask();
        mLoginTask = task;
        mClient.Login((long projectId, long uid, bool successful, int errorCode)=>{
            if(task.isCanceling)
            {
                DebugEx.FormatWarning("RTMManager::_ATLogin ---> canceled");
                task.ResolveTaskCancel();
            }
            if(projectId != mPid || mUid != uid)
            {
                DebugEx.FormatInfo("RTMManager::_ATLogin ---> ignore one login result: {0}vs{1}, {2}vs{3}", projectId, mPid, uid, mUid);
                return;
            }
            if(!successful)
            {
                DebugEx.FormatWarning("RTMManager::_ATLogin ---> fail code {0}", errorCode);
                task.ResolveTaskFail((long)errorCode);
            }
            else
            {
                mUid = uid;
                DebugEx.FormatInfo("RTMManager::_ATLogin ---> success, uid {0}", mUid);
                mAuthed = true;
                task.ResolveTaskSuccess();
            }
        }, mToken, 0);
    }
    #endif
}