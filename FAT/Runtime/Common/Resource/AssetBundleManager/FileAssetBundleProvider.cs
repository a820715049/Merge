/**
 * @Author: handong.liu
 * @Date: 2020-07-09 19:17:58
 */
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using CenturyGame.AssetBundleManager.Runtime;
using CenturyGame.AppUpdaterLib.Runtime;
using System.IO;

namespace EL.Resource
{
    public class FileAssetBundleProvider : IAssetBundleProvider
    {
        private string mABRoot;
        private string mPlainFileRoot;
        private string mIdentifer;
        // 区分是否在包内
        private bool mInAppPath;

        public FileAssetBundleProvider(string root, string plainFileRoot, bool inAppPath)
        {
            mABRoot = root;
            mPlainFileRoot = plainFileRoot;
            mInAppPath = inAppPath;
            if(mABRoot.EndsWith("/") || mABRoot.EndsWith("\\"))
            {
                mABRoot = mABRoot.Substring(0, mABRoot.Length - 1);
            }
            if(mPlainFileRoot.EndsWith("/") || mPlainFileRoot.EndsWith("\\"))
            {
                mPlainFileRoot = mPlainFileRoot.Substring(0, mPlainFileRoot.Length - 1);
            }
            DebugEx.FormatInfo("FileAssetBundleProvider ----> created with root {0}, {1}", mABRoot,mPlainFileRoot);
        }
        public string GetIdentifer()
        {
            if (string.IsNullOrEmpty(mIdentifer))
            {
                mIdentifer = string.Format("file@{0}", mABRoot);
            }
            return mIdentifer;
        }
        public bool HasAssetBundle(string name)
        {
            // 仅能用于包外资源判断
            if (mInAppPath)
                return false;

            // 以resMap为依据判断包外资源是否存在
            string url = FAT.ConfHelper.GetUpdatePath(name, out _, false, string.Empty);
            return !string.IsNullOrEmpty(url);
        }
        public IEnumerator ATLoadManifest()
        {
            var ret = new SimpleResultedAsyncTask<ABManifest>();
            yield return ret;

            string url;
            if (mInAppPath) url = FAT.ConfHelper.GetBuiltinPath(AssetsFileSystem.UnityABFileName, out _, false, string.Empty);
            else url = FAT.ConfHelper.GetUpdatePath(AssetsFileSystem.UnityABFileName, out _, false, string.Empty);
            url = FAT.ConfHelper.FixFilePath(url);

            if (string.IsNullOrEmpty(url))
            {
                DebugEx.FormatTrace("FileAssetBundleProvider ----> invalid path {0}", url);
                ret.Fail("invalid file path");
                yield break;
            }
            DebugEx.FormatTrace("FileAssetBundleProvider ----> load manifest from {0}", url);

            var request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                DebugEx.FormatTrace("FileAssetBundleProvider ----> load manifest from {0} fail:{1}", url, request.error);
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
                        DebugEx.FormatTrace("FileAssetBundleProvider ----> load manifest from {0} decode error, text:{1}", url, text);
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
                    DebugEx.FormatTrace("FileAssetBundleProvider ----> load manifest from {0} error response:{1}", url, request.responseCode);
                    ret.Fail("http response error");
                }
            }
        }
        public IEnumerator CoLoadAssetBundle(string name, ResourceAsyncTask task)
        {
            string url;
            if (mInAppPath) url = FAT.ConfHelper.GetBuiltinPath(name, out _, false, string.Empty);
            else url = FAT.ConfHelper.GetUpdatePath(name, out _, false, string.Empty);

            if (string.IsNullOrEmpty(url))
            {
                DebugEx.FormatTrace("FileAssetBundleProvider ----> invalid path {0}", url);
                // 保持和之前一致的逻辑 不resolve fail
                yield break;
            }
            DebugEx.FormatTrace("FileAssetBundleProvider ----> load bundle from {0}", url);

            #if UNITY_WEBGL
            var www = UnityWebRequestAssetBundle.GetAssetBundle(url);
            yield return www.SendWebRequest();

            if (www == null || www.isNetworkError || www.isHttpError)
            {
                DebugEx.FormatWarning("FileAssetBundleProvider ----> load bundle failed {0}:{1}", url, www.error);
                //task.Fail("load fail");
            }
            else
            {
                try
                {
                    AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                    if(bundle == null)
                    {
                        DebugEx.FormatWarning("FileAssetBundleProvider ----> load bundle failed {0}:GetContent is null", url);
                    }
                    else
                    {
                        task.Success(bundle);
                    }
                }
                catch (System.Exception ex)
                {
                    DebugEx.FormatWarning("FileAssetBundleProvider ----> load bundle exception {0}:{1}:{2}", url, ex.Message, ex.StackTrace);
                    //task.Fail(ex.ToString());
                }
            }
            #else
            var www = AssetBundle.LoadFromFileAsync(url);
            yield return www;

            if (www == null || www.assetBundle == null)
            {
                if (AssetBundleHelper.IsDuplicate(name, out var ab))
                {
                    try
                    {
                        task.Success(ab);
                    }
                    catch (System.Exception ex)
                    {
                        DebugEx.FormatWarning("FileAssetBundleProvider ----> load bundle dup exception {0}:{1}:{2}", url, ex.Message, ex.StackTrace);
                        //task.Fail(ex.ToString());
                    }
                }
                else
                {
                    DebugEx.FormatWarning("FileAssetBundleProvider ----> load bundle failed {0}", url);
                    //task.Fail("load fail");
                }
            }
            else
            {
                try
                {
                    AssetBundle bundle = www.assetBundle;
                    task.Success(bundle);
                }
                catch (System.Exception ex)
                {
                    DebugEx.FormatWarning("FileAssetBundleProvider ----> load bundle exception {0}:{1}:{2}", url, ex.Message, ex.StackTrace);
                    //task.Fail(ex.ToString());
                }
            }
            #endif
        }
    }
}