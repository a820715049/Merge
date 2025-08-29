/**
 * @Author: handong.liu
 * @Date: 2020-11-02 17:02:18
 */
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum AdjustEventType
{
    EnergyPack10,   //商店页面10钻石“购买”体力
    EnergyChest10,  //商店页面24钻石“购买”体力宝箱
    ShowShop3,      //商店页面“拉起”3次
    ShowShop8,      //商店页面“拉起”8次
    LaunchApp,      //文档 => LaunchApp 用户每次打开App
    GameStart,      //文档 => Login 用户登录游戏
    Purchase,       //文档 => Checkout 用户打开购买页面或单击商品按钮时发生事件
}
[Serializable]
public class InstallTrackConfig
{
    public InstallIntEvent[] levels;
    // 主线订单
    public InstallIntEvent[] tasks;
    public InstallIntEvent[] purchase;
    public InstallTypeEvent[] other;
}
[Serializable]
public class InstallIntEvent
{
    public int intValue;
    public string firebaseToken;
    public string adjustToken;
    public string leyunToken;
    public bool isSendFirebase = true;
    public bool isSendAdjust = true;
}
[Serializable]
public class InstallTypeEvent
{
    public AdjustEventType type;
    public string firebaseToken;
    public string adjustToken;
    public string leyunToken;
    public bool isSendFirebase = true;
    public bool isSendAdjust = true;
}

[System.Serializable]
public class HttpClientConfig
{
    public string urlRoot;
    public int timeout;
    public string forceUrlRoot;
}

[System.Serializable]
public class OceanEngineConfig
{
    public string appId;
    public string channel;
}

[System.Serializable]
public class AppShieldConfig
{
    public string appId;
    public string appKey;
}

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/AppSettings", order = 1)]
public class AppSettings : ScriptableObject
{
    public enum SDKEnvironment
    {
        Sandbox,
        Prod
    }
    public enum SDKChannel
    {
        Mainland,
        Global,
        MiniGame,
        Pioneer,
        Asia,
    }
#if UNITY_EDITOR
    [Serializable]
    public class AppBuilderConfig
    {
        public string sdkId;
        public string sdkKey;
        public string productName;
        public string appId;
        public string buildOutputFileNameHint;
        public string bundleId;
        public string protokitConfName;
        public string branch;
        public string scriptMacro;
        #region android参数
        public string GetKeyStorePath()
        {
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "cert", apkKeyStore));
        }
        public string apkKeyStore;
        public string apkVariant;
        public string apkPwd;
        #endregion
        #region ios参数
        public string iosSignTeamId;
        public string iosSignProfileId;
        public string iosSignProfileName;
        public string iosSignCodeSignIdentity;
        #endregion
        #region 自己接sdk使用，仅有微信小游戏
        public string biAppTag;
        public string biAppKey;
        public string biUrl;
        public string midasAppId;
        public string iosPaymentAppId;
        public string androidPaymentAppId;
        #endregion
    }
    public AppBuilderConfig builderConfig; 
    public string GetFileNameHint()
    {
        return string.Format("{0}_{1}_{2}_{3}", builderConfig.buildOutputFileNameHint, variant, version.Replace(".", "_"), versionCode);
    }
#endif
    public string variant;
    public string sdkId;
    public string sdkKey;
    public HttpClientConfig httpServer;
    public string version;
    public string clientId;
    public string leyunAppKey;
    public OceanEngineConfig oceanConfig;
    public AppShieldConfig shieldConfig;
    public string versionCode;
    // public bool enableProfiler;
    public CenturyGame.AppUpdaterLib.Runtime.Configs.AppUpdaterConfig updaterConfig;
    public SDKEnvironment sdkEnv = SDKEnvironment.Sandbox;
    public SDKChannel sdkChannel = SDKChannel.Mainland;
    public string admobRewardAdUnitId;
    public TrackingConfig trackingConfig;
}