/**
 * @Author: handong.liu
 * @Date: 2022-05-20 19:16:41
 */
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using CenturyGame.AssetBundleManager.Runtime;
using CenturyGame.AppUpdaterLib.Runtime;
using System.IO;

namespace EL.Resource
{
    public class WebAssetBundleProvider : IAssetBundleProvider
    {
        private WebFileManifest mManifest;
        private string mIdentifer;
        public WebAssetBundleProvider(WebFileManifest mani)
        {
            mManifest = mani;
            DebugEx.FormatInfo("WebAssetBundleProvider ----> created with root {0}", mManifest.root);
        }
        public string GetIdentifer()
        {
            if (string.IsNullOrEmpty(mIdentifer))
            {
                mIdentifer = string.Format("web@{0}", mManifest.root);
            }
            return mIdentifer;
        }
        public bool HasAssetBundle(string name)
        {
            return !string.IsNullOrEmpty(mManifest.GetFilePath(name));
        }
        public IEnumerator ATLoadManifest()
        {
            var ret = new SimpleResultedAsyncTask<ABManifest>();
            yield return ret;
            string url = mManifest.GetFilePath(AssetsFileSystem.UnityABFileName);
            DebugEx.FormatTrace("WebAssetBundleProvider ----> load manifest from {0}", url);
            if(string.IsNullOrEmpty(url))
            {
                ret.Fail("not exists");
                yield break;
            }
            var request = UnityEngine.Networking.UnityWebRequest.Get(url);
            request.SendWebRequest();
            while(!request.isDone)
            {
                yield return null;
            }
            if(request.isNetworkError || request.isHttpError)
            {
                DebugEx.FormatTrace("WebAssetBundleProvider ----> load manifest from {0} fail:{1}", url, request.error);
                ret.Fail(request.error);
            }
            else
            {
                if(request.responseCode == 200 || request.responseCode == 304)
                {
                    //success
                    var fileData = request.downloadHandler.data;
                    var text = new System.Text.UTF8Encoding(false, true).GetString(fileData);
                    var resManifest = JsonUtility.FromJson<ResManifest>(text);
                    if(resManifest == null)
                    {
                        DebugEx.FormatTrace("WebAssetBundleProvider ----> load manifest from {0} decode error, text:{1}", url, text);
                        ret.Fail("decode error");
                    }
                    else
                    {
                        var mani = new ABManifest();
                        mani.ResetWithManifest(resManifest);
                        ret.Success(mani);
                    }
                }
                else
                {
                    DebugEx.FormatTrace("WebAssetBundleProvider ----> load manifest from {0} error response:{1}", url, request.responseCode);
                    ret.Fail("http response error");
                }
            }
        }
        public IEnumerator CoLoadAssetBundle(string name, ResourceAsyncTask task)
        {
            string url = mManifest.GetFilePath(name);
            if(string.IsNullOrEmpty(url))
            {
                yield break;
            }
            DebugEx.FormatTrace("WebAssetBundleProvider ----> load bundle from {0}", url);
            var www = UnityWebRequestAssetBundle.GetAssetBundle(url);
            yield return www.SendWebRequest();

            if (www == null || www.isNetworkError || www.isHttpError)
            {
                DebugEx.FormatWarning("WebAssetBundleProvider ----> load bundle failed {0}:{1}", url, www.error);
                //task.Fail("load fail");
            }
            else
            {
                try
                {
                    AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                    if(bundle == null)
                    {
                        DebugEx.FormatWarning("WebAssetBundleProvider ----> load bundle failed {0}:GetContent is null", url);
                    }
                    else
                    {
                        DebugEx.FormatTrace("WebAssetBundleProvider ----> load bundle from {0} success", url);
                        task.Success(bundle);
                    }
                }
                catch (System.Exception ex)
                {
                    DebugEx.FormatWarning("WebAssetBundleProvider ----> load bundle exception {0}:{1}:{2}", url, ex.Message, ex.StackTrace);
                    //task.Fail(ex.ToString());
                }
            }
        }
    }
}