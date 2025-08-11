/**
 * @Author: handong.liu
 * @Date: 2022-05-20 19:24:52
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace EL.Resource
{
    public class WebFileLoader : IFileLoader
    {
        private WebFileManifest mManifest;
        public WebFileLoader(WebFileManifest root)
        {
            mManifest = root;
        }
        IEnumerator IFileLoader.CoCheckImportantFiles(ResourceManifest container)
        {
            yield break;
        }
        void IFileLoader.LoadFile(string path, System.Action<byte[], string> cb/*filedata, error*/)
        {
            string url = mManifest.GetFilePath(path);
            DebugEx.FormatTrace("WebFileLoader ----> load file from {0}", url);
            if(string.IsNullOrEmpty(url))
            {
                cb?.Invoke(null, "no file");
                return;
            }
            var request = UnityEngine.Networking.UnityWebRequest.Get(url);
            request.SendWebRequest().completed += (op) => {
                if(request.isNetworkError || request.isHttpError)
                {
                    DebugEx.FormatTrace("WebFileLoader ----> load file from {0} fail:{1}", url, request.error);
                    cb?.Invoke(null, request.error);
                }
                else
                {
                    if(request.responseCode == 200 || request.responseCode == 304)
                    {
                        //success
                        var fileData = request.downloadHandler.data;
                        cb?.Invoke(fileData, null);
                    }
                    else
                    {
                        DebugEx.FormatTrace("WebFileLoader ----> load manifest from {0} error response:{1}", url, request.responseCode);
                        cb?.Invoke(null, "http response error");
                    }
                }
            };
        }
    }
}