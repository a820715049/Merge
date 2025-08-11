/*
 * @Author: tang.yan
 * @Description: 难度API
 * @Doc: https://centurygames.feishu.cn/wiki/XaVJwpswoiQXnrk1X7Nc1mdMn8g
 * @Date: 2025-05-22 16:05:28
 */
using System;
using System.Collections.Generic;
using EL;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FAT.RemoteAnalysis
{
    #region 数据结构定义

    //文档地址 https://centurygames.feishu.cn/wiki/OVVJwqFpJi1tvYkKuAqcok7Onkd

    //请求
    public class UserGradeRequest
    {
        public UserInfo user_info;
        public PurchaseInfo purchase_info;
        public ModelInfo model_info;
    }
    
    //返回
    public class UserGradeResponse
    {
        public int code; 
        public string msg;
        public UserGradeResponseData data;
    }

    public class UserGradeResponseData
    {
        public int rec_diff;
    }

    #endregion

    public class UserGradeRemoteWrapper
    {
        private static readonly string apiUrl = "https://dsc-ai.centurygame.com/api/v1/fat/diff-rec";
        
        internal void SendRequest(string modelVersion, Action<int> onReqSucc)
        {
            var req = new UserGradeRequest();
            req.user_info = UserInfo.Current;
            req.purchase_info = ActivityRedirect.CreatePurchase(null);
            req.model_info = new ModelInfo()
            {
                model_version = modelVersion,
            };
            _InnerSend(req, onReqSucc).Forget();
        }
        
        private async UniTaskVoid _InnerSend(UserGradeRequest req, Action<int> onReqSucc)
        {
            var json = JsonConvert.SerializeObject(req);

            if (Application.isEditor || GameSwitchManager.Instance.isDebugMode)
            {
                DebugEx.Info($"[UserGradeDebug] api request => {json}");
            }

            var begin = Time.realtimeSinceStartup;
            using (var webPost = UnityWebRequest.Post(apiUrl, json, "application/json"))
            {
                //Test状态下支持API延迟请求
                if (Application.isEditor || GameSwitchManager.Instance.isDebugMode)
                {
                    var delayTime = (int)(Game.Manager.userGradeMan.DebugDelayTime * 1000);
                    DebugEx.Info($"[UserGradeDebug] api delay START, delayTime = {delayTime}");
                    await UniTask.Delay(delayTime);
                    DebugEx.Info($"[UserGradeDebug] api delay END, delayTime = {delayTime}");
                }
                await webPost.SendWebRequest();
                var end = Time.realtimeSinceStartup;

                if (webPost.result != UnityWebRequest.Result.Success)
                {
                    DebugEx.Warning($"[UserGradeDebug] api failed {req.user_info.account_id}@{req.user_info.event_time} | {webPost.error} | {webPost.result}");
                    return;
                }
                try
                {
                    if (Application.isEditor || GameSwitchManager.Instance.isDebugMode)
                    {
                        DebugEx.Info($"[UserGradeDebug] api response => cost:{(end - begin).ToString("F4")} | {webPost.downloadHandler.text}");
                    }
                    var resp = JsonConvert.DeserializeObject<UserGradeResponse>(webPost.downloadHandler.text);
                    var diff = resp.data.rec_diff;
                    onReqSucc?.Invoke(diff);
                }
                catch
                {
                    DebugEx.Error($"[UserGradeDebug] api bad data {req.user_info.account_id}@{req.user_info.event_time} {webPost.downloadHandler.text}");
                    return;
                }
            }
        }

    }
}