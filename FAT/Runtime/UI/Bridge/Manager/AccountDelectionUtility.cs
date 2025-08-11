/*
 * @Author: qun.chao
 * @Date: 2022-07-05 12:17:50
 */
using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using centurygame;
using FAT;
using FAT.Platform;

public static class AccountDelectionUtility
{
    //1. 获取 '删除账号' 的隐私协议条款
    public static string GetAccountDeletionPolicy()
    {
        return CGSdkUtils.Instance.GetStringResource(CGDataConfigParams.CG__ACCOUNT_DELETION_POLICY);
    }

    //2.1 请求可用于身份验证的社交列表
    public static void RequestValidSocialList()
    {
        // 传递当前游戏支持的社交登录方式
        List<CGAccountType> supportSocialTypes = new List<CGAccountType>
        {
            CGAccountType.FPAccountTypeGoogle,
            CGAccountType.FPAccountTypeFacebook,
            CGAccountType.FPAccountTypeExpress,
#if UNITY_IOS
            CGAccountType.FPAccountTypeApple,
#endif      
        };
        // 调用SDK接口
        CGRemoveAccount.Instance.RequestValidSocialList(supportSocialTypes);
    }

    //2.2 获取可用于身份验证的社交列表
    public static List<CGAccountType> GetAuthenticationTypeList()
    {
        var list = PlatformSDK.Instance.Adapter.ValidSocialList;
        //SDK返回空时 默认使用游客身份
        if (list.Count < 1)
        {
            list = new List<CGAccountType>()
            {
                CGAccountType.FPAccountTypeExpress,
            };
        }
        return list;
    }
    
    //2.3 UI上展示社交列表字符串
    public static string GetAuthenticationTypeDesc(CGAccountType type)
    {
        string typeStr = "";
        switch (type)
        {
            case CGAccountType.FPAccountTypeGoogle:
                typeStr = I18N.Text("#SysComDesc176");
                break;
            case CGAccountType.FPAccountTypeFacebook:
                typeStr = I18N.Text("#SysComDesc174");
                break;
            case CGAccountType.FPAccountTypeApple:
                typeStr = I18N.Text("#SysComDesc175");
                break;
            case CGAccountType.FPAccountTypeExpress:
                typeStr = I18N.Text("#SysComDesc139");
                break;
        }
        return typeStr;
    }
    
    //3. 用户身份验证任务流程
    private static SimpleResultedAsyncTask<CGBaseRemovalAccountInfo> authenticationTask = null;
    public static SimpleResultedAsyncTask<CGBaseRemovalAccountInfo> TryProcessAuthentication(CGAccountType type)
    {
        var ret = new SimpleResultedAsyncTask<CGBaseRemovalAccountInfo>();
        if (authenticationTask != null)
        {
            ret.Fail();
            return ret;
        }
        
#if !UNITY_EDITOR
        authenticationTask = ret;
        //构造参数
        CGRemovalAccountVerifyData verifyData = new CGRemovalAccountVerifyData();
        verifyData.setRegion(CGAccountRegion.CGAccountRegionGlobal);
        verifyData.setCGAccountType(type);
        //调用SDK接口验证
        DebugEx.FormatInfo("[AccountDelectionUtility]: TryProcessAuthentication type = {0}, verifyData = {1}", type, verifyData.ToJsonString());
        CGRemoveAccount.Instance.VerifyUserIdentity(verifyData);
#else
        authenticationTask = null;
        ret.Success(null);
#endif
        return ret;
    }
    
    //接收到验证结果
    public static void ResolveAuthentication(bool suc, CGBaseRemovalAccountInfo info)
    {
        if (authenticationTask != null)
        {
            var task = authenticationTask as SimpleResultedAsyncTask<CGBaseRemovalAccountInfo>;
            if (suc)
                task.Success(info);
            else
                task.Fail();
            authenticationTask = null;
        }
    }
    
    //4. 向 '身份验证 Data' 中设置邮箱
    public static void SetReceiveEmail(CGBaseRemovalAccountInfo info, string email)
    {
        info?.SetEmail(email);
    }
    
    //5. 提交账号删除申请
    private static SimpleAsyncTask reomveUserTask = null;
    public static SimpleAsyncTask TryProcessRemoveUser(CGBaseRemovalAccountInfo info)
    {
        var ret = new SimpleAsyncTask();
        if (reomveUserTask != null)
        {
            ret.ResolveTaskFail();
            return ret;
        }
        reomveUserTask = ret;
        //调用SDK接口验证
        DebugEx.FormatInfo("[AccountDelectionUtility]: TryProcessRemoveUser info = {0}", info.ToJsonString());
        CGRemoveAccount.Instance.RemoveAccount(info);
        return ret;
    }
    
    public static void ResolveRemoveUser(bool suc)
    {
        if (reomveUserTask != null)
        {
            if (suc)
                reomveUserTask.ResolveTaskSuccess();
            else
                reomveUserTask.ResolveTaskFail();
            reomveUserTask = null;
        }
    }
    
    //6. 检测玩家删除账号状态 在玩家登录时拦截 并提供撤销账号删除申请的入口
    public static void CheckAccountRemoveStatus(Action finishCb)
    {
        var status = PlatformSDK.Instance.Adapter.AccountRemoveStatus;
        DebugEx.FormatInfo("[AccountDelectionUtility]: CheckAccountRemoveStatus status = {0} ",status);
        if (status == CGAccountRemoveGeneralStatus.CGAccountRemoving)   //待删除
        {
            //账号待删除 提供撤销账号删除申请的入口
            Game.Instance.AbortRemoveCount(finishCb);
        }
        else if (status == CGAccountRemoveGeneralStatus.CGAccountRemoved) //已删除
        {
            // 账号已经被删除 弹提示框 点确定后直接退出游戏
            Game.Manager.commonTipsMan.ShowLoadingTips(I18N.Text("#SysComDesc183"), null, Game.Instance.AbortRemovedAccount, true);
        }
        else 
        {
            //正常状态 放行
            finishCb?.Invoke();
        }
    }
    
    public static IEnumerator TryProcessSureRemoveUser()
    {
        //做标记 用于下次登陆成功时 从服务器拿存档
        GameUpdateManager.Instance.SetNeedWaitUpdate(true);
        GameUpdateManager.Instance.SetNeedWaitUpdate2(true);
        //sdk登出
        yield return PlatformSDK.Instance.Logout();
    }
    
    //7. 请求撤销删除账号
    private static SimpleAsyncTask cancelReomveTask = null;
    public static SimpleAsyncTask TryProcessCancelRemoveUser()
    {
        var ret = new SimpleAsyncTask();
        if (cancelReomveTask != null)
        {
            ret.ResolveTaskFail();
            return ret;
        }
        cancelReomveTask = ret;
        //调用SDK接口
        DebugEx.FormatInfo("[AccountDelectionUtility]: TryProcessCancelRemoveUser status = {0}, fpid = {1}", PlatformSDK.Instance.Adapter.AccountRemoveStatus, Game.Manager.networkMan.fpId);
        CGRemoveAccount.Instance.RevokeAccountRemoval();
        return ret;
    }
    
    public static void ResolveCancelRemove(bool suc)
    {
        if (cancelReomveTask != null)
        {
            if (suc)
                cancelReomveTask.ResolveTaskSuccess();
            else
                cancelReomveTask.ResolveTaskFail();
            cancelReomveTask = null;
        }
    }
}