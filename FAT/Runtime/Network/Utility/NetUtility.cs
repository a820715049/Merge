/**
 * @Author: handong.liu
 * @Date: 2020-09-29 12:14:05
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using GameNet;
using FAT.Platform;

public static class NetUtility
{
    /*
    public static IEnumerator ATLogin()
    {
        var ret = new SingleTaskWrapper();
        yield return ret;

        var loginTask = Game.Instance.StartAsyncTaskGlobal(_ATRealLogin());
        ret.SetTask(loginTask);
        yield return loginTask;
        if(!loginTask.isSuccess && loginTask.errorCode == (long)GameNet.ErrorCode.CheckFpidFailed)
        {
            DebugEx.FormatWarning("NetHandlerer.CoLogin ----> encount error 1203, do logout and login");
            var logout = PlatformSDK.Instance.Logout();
            yield return logout;
            loginTask = Game.Instance.StartAsyncTaskGlobal(_ATRealLogin());
            ret.SetTask(loginTask);
            yield return loginTask;
        }
        
        if(loginTask.isSuccess)
        {
            ret.ResolveSuccess();
        }
        else
        {
            ret.ResolveError();
        }
    }

    private static IEnumerator _ATRealLogin()
    {
        UILoading.SetLoadingState("#LoadStateInitSDK");
        var task = new SimpleAsyncTask();
        yield return task;

        var sdkInitTask = PlatformSDK.Instance.Initialize();            //must init sdk     //必须在所有networkMan的协议之前做，否则无法发送distinctId头
        yield return sdkInitTask;
        if(sdkInitTask.isFailOrCancel)
        {
            task.ResolveTaskFail((long)GameErrorCode.PlatformInitError);
            yield break;
        }
        if(!PlayerPrefs.HasKey("___OPENED"))
        {
            PlayerPrefs.SetString("___OPENED", "YES");
            AdjustTracker.TrackEvent(AdjustEventType.FirstOpen);
        }

        UILoading.SetLoadingState("#LoadStateSDKLogin");
        var loginTask = AccountCenter.Instance.Login(centurygame.CGAccountType.FPAccountTypeUnknown);                   //must login platform
        yield return loginTask;
        if(loginTask.isFailOrCancel)
        {
            task.ResolveTaskFail((long)GameErrorCode.PlatformLoginError);
            yield break;
        }
        
        var key = PlayerPrefs.GetString(Constant.kPrefKeyDebugFPID, null);
        var sessionSecret = "";
        if(string.IsNullOrEmpty(key))           //not a debug play
        {
            key = PlatformSDK.Instance.GetUserId();
            sessionSecret = PlatformSDK.Instance.GetUserSessionKey();
        }
        //key = "AndroidAudit";       //For android

        UILoading.SetLoadingState("#LoadStateGameLogin");
        var resp = Game.Instance.networkMan.Login(key, sessionSecret);
        yield return resp;
        if(!resp.isSuccess)
        {
            if(resp.errorCode == (long)ErrorCode.CheckFpidFailed)
            {
                DebugEx.FormatInfo("NetHandlerer.CoLogin ----> check fpid fail, we logout", resp.error);
                var logoutTask = PlatformSDK.Instance.Logout();
                yield return logoutTask;
            }
            DebugEx.FormatInfo("NetHandlerer.CoLogin ----> login fail:{0}", resp.error);
            task.ResolveTaskFail(resp.errorCode);
            yield break;
        }
        Game.Instance.SetServerTimeValid();

        var remoteUser = resp.result.GetBody<LoginResp>();
        DebugLogUploaderManager.Instance.TryInit(key, remoteUser.User.Uid);
        Game.Instance.socialMan.bindSocialGem = remoteUser.BindSocialGem;
        Game.Instance.archiveMan.ResolveAchive(remoteUser, remoteUser.NewUser);

        PlatformSDK.Instance.StartGameServer();
        DebugEx.FormatInfo("NetHandlerer.CoLogin ----> login finish, displayId:{0}", remoteUser.User.DisplayId);
        task.ResolveTaskSuccess();
    }
    */

    // public static IEnumerator ATUploadBinFile(byte[] data)
    // {
    //     var task = new SimpleResultedAsyncTask<string>();
    //     yield return task;

    //     string uploadUrl = null;
    //     string downloadUrl = null;
    //     var urlResp = Game.Instance.networkMan.GenUploadUrl("bin", data.Length);
    //     yield return urlResp;
    //     if(urlResp.isSuccess)
    //     {
    //         uploadUrl = urlResp.body.Url;
    //         downloadUrl = urlResp.body.StoragePath;
    //         DebugEx.FormatInfo("NetUtility.ATUploadBinFile ----> presigned url:{0}, storagePath:{1}", uploadUrl, downloadUrl);
    //     }
    //     else
    //     {
    //         DebugEx.FormatWarning("NetUtility.ATUploadBinFile ----> get presigned url fail:{0}", urlResp.error);
    //         task.Fail(urlResp.error);
    //         yield break;
    //     }
    //     yield return _CoUploadData(data, uploadUrl, downloadUrl, task);
    //     if(task.isSuccess)
    //     {
    //         DebugEx.FormatInfo("NetUtility.ATUploadBinFile ----> success!");
    //     }
    // }
}