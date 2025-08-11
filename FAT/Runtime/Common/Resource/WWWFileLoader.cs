// /**
//  * @Author: handong.liu
//  * @Date: 2021-03-30 14:59:49
//  */
// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// using CenturyGame.AppUpdaterLib.Runtime;
// using CenturyGame.AppUpdaterLib.Runtime.ResManifestParser;
// using EL;

// namespace EL.Resource
// {
//     public class WWWFileLoader : IFileLoader
//     {
//         private string mRoot;
//         private HashSet<string> mDiscardFiles = new HashSet<string>();
//         public WWWFileLoader(string root)
//         {
//             mRoot = root;
//         }
//         public const string kConfPath = "lua/gen";
//         IEnumerator IFileLoader.CoCheckImportantFiles(ResourceManifest container)
//         {
//             mDiscardFiles.Clear();
//             string resManifest = $"res_{Utility.GetPlatformName().ToLower()}.json";
//             string dataManifest = "res_data.json";
//             ResourceManifest res = new ResourceManifest();
            
//             yield return _CoLoadManifest(resManifest, "", "", res);
//             yield return _CoLoadManifest(dataManifest, "gen", $"{kConfPath}", res);

            
//             List<FileInfo> allManifestFiles = new List<FileInfo>();
//             container.FillAllFile(allManifestFiles);
//             for(int i = allManifestFiles.Count - 1; i >= 0; i--)
//             {
//                 if(res.TryGetInfo(allManifestFiles[i].key, out var desc) && desc.md5 == allManifestFiles[i].md5)
//                 {
//                     allManifestFiles.RemoveAt(i);
//                 }
//             }

//             foreach(var f in allManifestFiles)
//             {
//                 mDiscardFiles.Add(f.key);
//             }

//             DebugEx.FormatInfo("WWWFileLoader::CoCheckImportantFiles ----> discard files {0}:\n{1}", mRoot, mDiscardFiles);
//         }
//         private IEnumerator _CoLoadManifest(string maniFile, string gen, string root, ResourceManifest container)
//         {
//             bool finish = false;
//             (this as IFileLoader).LoadFile(maniFile, (data, err) => {
//                 if(data != null)
//                 {
//                     string c = System.Text.Encoding.UTF8.GetString(data) ?? "";
//                     DebugEx.FormatInfo("WWWFileLoader::_CoLoadManifest ----> manifest {0}:{1}:{2}", mRoot, maniFile, c);
//                     container.ParseFromCenturyGameManifest(c, gen, root);
//                 }
//                 finish = true;
//             });
//             yield return new WaitUntil(() => finish);
//         }
//         void IFileLoader.LoadFile(string path, System.Action<byte[], string> cb/*filedata, error*/)
//         {
//             if(mDiscardFiles.Contains(path))
//             {
//                 cb?.Invoke(null, "not found");
//                 return;
//             }
//             string url = string.Format("{0}/{1}", mRoot, path);
//             if(!url.Contains("://"))
//             {
//                 url = string.Format("file://{0}", url);
//             }
//             // DebugEx.FormatTrace("WWWFileLoader ----> load file from {0}", url);
//             var request = UnityEngine.Networking.UnityWebRequest.Get(url);
//             var waitObj = request.SendWebRequest();
//             // if(GameSwitchManager.Instance.isDebugMode)
//             // {
//             //     AssetBundleManager.Instance.StartCoroutine(_CoDebugGuard(url, waitObj));
//             // }
//             waitObj.completed += (op) => {
//                 if(request.isNetworkError || request.isHttpError)
//                 {
//                     DebugEx.FormatTrace("WWWFileLoader ----> load file from {0} fail:{1}", url, request.error);
//                     cb?.Invoke(null, request.error);
//                 }
//                 else
//                 {
//                     if(request.responseCode == 200 || request.responseCode == 304)
//                     {
//                         //success
//                         var fileData = request.downloadHandler.data;
//                         cb?.Invoke(fileData, null);
//                     }
//                     else
//                     {
//                         DebugEx.FormatTrace("WWWFileLoader ----> load manifest from {0} error response:{1}", url, request.responseCode);
//                         cb?.Invoke(null, "http response error");
//                     }
//                 }
//             };
//         }

//         IEnumerator _CoDebugGuard(string url, UnityEngine.Networking.UnityWebRequestAsyncOperation www)
//         {
//             yield return www;
//             // DebugEx.FormatTrace("WWWFileLoader ----> [_CoDebugGuard]load file from {0} finish, error: {1}", url, www.webRequest.error);
//         }
//     }
// }