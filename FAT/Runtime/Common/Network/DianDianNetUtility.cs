/**
 * @Author: handong.liu
 * @Date: 2022-07-11 17:41:18
 */
using System.Collections.Generic;
using System;

namespace EL
{
    public static class DianDianNetUtility
    {
        private static bool _GenPostFormData(EL.SimpleJSON.JSONObject param, string sdkSec, bool signBeforeUrlEncode, out string formData, out string sec)
        {
            using (ObjectPool<System.Text.StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                using (ObjectPool<List<string>>.GlobalPool.AllocStub(out var keys))
                {
                    using (ObjectPool<System.Text.StringBuilder>.GlobalPool.AllocStub(out var sbFormData))
                    {
                        foreach (var k in param.Keys)
                        {
                            keys.Add((string)k);
                        }
                        keys.Sort();
                        foreach (var k in keys)
                        {
                            if (sb.Length > 0)
                            {
                                sb.Append("&");
                                sbFormData.Append("&");
                            }
                            sb.AppendFormat("{0}={1}", k, param[k].Value);
                            sbFormData.AppendFormat("{0}={1}", k, UnityEngine.Networking.UnityWebRequest.EscapeURL(param[k].Value));
                        }
                        string ret = sb.ToString();
                        formData = sbFormData.ToString();
                        if(!signBeforeUrlEncode)
                        {
                            ret = formData;
                        }
                        DebugEx.FormatTrace("DianDianNetUtility::_GenSignature ----> {0}, {1}", ret, sdkSec);
                        var data = System.Text.Encoding.UTF8.GetBytes(ret);
                        var digest = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(sdkSec));
                        var secBytes = digest.ComputeHash(data);
                        var secHex = System.BitConverter.ToString(secBytes).Replace("-", "").ToLower();
                        sec = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(secHex));
                        DebugEx.FormatTrace("DianDianNetUtility::_GenSignature result ----> {0}, {1}", sec, secHex);
                        return true;
                    }
                }
            }
        }

        public static void RequestMiddleProxyWithParamDefaultUrl(string service, string url_path, EL.SimpleJSON.JSONObject json, string sdkSec, Action<string> onFinish, Action<string, long> onError)
        {
            var url = string.Format("{0}/api/v1/proxy", FAT.Game.Instance.appSettings.httpServer.urlRoot);
            RequestMiddleProxyWithParam(url, url_path, service, json, sdkSec, onFinish, onError);
        }

        public static void RequestMiddleProxyWithParam(string middleUrl, string url_path, string service, EL.SimpleJSON.JSONObject json, string sdkSec, Action<string> onFinish, Action<string, long> onError)
        {
            var sb = EL.ObjectPool<System.Text.StringBuilder>.GlobalPool.Alloc();
            string formData = "";
            string sec = "";
            try
            {
                json.WriteToStringBuilder(sb, 0, 0, SimpleJSON.JSONTextMode.Compact);
                using(ObjectPool<EL.SimpleJSON.JSONObject>.GlobalPool.AllocStub(out var jsonParam))
                {
                    jsonParam["service"] = service;
                    jsonParam["payload"] = sb.ToString();
                    if(!string.IsNullOrEmpty(url_path))
                    {
                        jsonParam["url_path"] = url_path;
                    }
                    if(!_GenPostFormData(jsonParam, sdkSec, false, out formData, out sec))
                    {
                        onError?.Invoke("错误的参数1", (long)GameErrorCode.HumanReadableError);
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                DebugEx.FormatError("error marshal {0}", sb.ToString());
                onError?.Invoke("错误的参数2", (long)GameErrorCode.HumanReadableError);
                return;
            }
            finally
            {
                EL.ObjectPool<System.Text.StringBuilder>.GlobalPool.Free(sb);
            }
            var req = UnityEngine.Networking.UnityWebRequest.Put(middleUrl, formData);
            req.method = "POST";
            req.SetRequestHeader("Authorization", sec);
            req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            DebugEx.FormatTrace("DianDianNetUtility::RequestMiddleProxyWithParam ----> start {0}, {1}", middleUrl, formData);
            req.SendWebRequest().completed += (oper) =>
            {
                if (req == null)
                {
                    onError("error param", (long)GameErrorCode.Unknown);
                    return;
                }
                else if (req.isNetworkError)
                {
                    onError(req.error, (long)GameErrorCode.Unknown);
                    return;
                }
                else if (req.isHttpError)
                {
                    onError(req.error, ErrorCodeUtility.ConvertToCommonCode(req.responseCode, ErrorCodeType.HttpError));
                    return;
                }
                var body = req.downloadHandler.text;
                DebugEx.FormatTrace("DianDianNetUtility::RequestMiddleProxyWithParam ----> finish {0}", body);
                try
                {
                    onFinish(body);
                }
                catch (System.Exception ex)
                {
                    onError(ex.Message + " " + ex.StackTrace, ErrorCodeUtility.ConvertToCommonCode(GameErrorCode.Unknown));
                }
            };
        }

        public static void RequestWithParam(EL.SimpleJSON.JSONObject json, string url, string sdkGameId, string sdkSec, Action<string> onFinish, Action<string, long> onError)
        {
            var sb = EL.ObjectPool<System.Text.StringBuilder>.GlobalPool.Alloc();
            try
            {
                json.WriteToStringBuilder(sb, 0, 0, SimpleJSON.JSONTextMode.Compact);
            }
            catch (System.Exception ex)
            {
                DebugEx.FormatError("error marshal {0}", sb.ToString());
                onError?.Invoke("错误的参数", (long)GameErrorCode.HumanReadableError);
                return;
            }
            finally
            {
                EL.ObjectPool<System.Text.StringBuilder>.GlobalPool.Free(sb);
            }
            var str = sb.ToString();
            if (_GenPostFormData(json, sdkSec, true, out var formData, out var sec))
            {
                var req = UnityEngine.Networking.UnityWebRequest.Put(url, formData);
                req.method = "POST";
                req.SetRequestHeader("AUTHORIZATION", string.Format("FP {0}:{1}", sdkGameId, sec));
                req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                req.SendWebRequest().completed += (oper) =>
                {
                    if (req == null)
                    {
                        onError("error param", (long)GameErrorCode.Unknown);
                        return;
                    }
                    else if (req.isNetworkError)
                    {
                        onError(req.error, (long)GameErrorCode.Unknown);
                        return;
                    }
                    else if (req.isHttpError)
                    {
                        onError(req.error, ErrorCodeUtility.ConvertToCommonCode(req.responseCode, ErrorCodeType.HttpError));
                        return;
                    }
                    var body = req.downloadHandler.text;
                    DebugEx.FormatTrace("DianDianNetUtility::RequestWithParam ----> {0}", body);
                    try
                    {
                        onFinish(body);
                    }
                    catch (System.Exception ex)
                    {
                        onError(ex.Message + " " + ex.StackTrace, ErrorCodeUtility.ConvertToCommonCode(GameErrorCode.Unknown));
                    }
                };
            }
            else
            {
                onFinish?.Invoke(null);
            }
        }
    }
}