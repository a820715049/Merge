/**
 * @Author: handong.liu
 * @Date: 2022-05-20 18:57:39
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace EL.Resource
{
    public class WebFileManifest
    {
        public string root => mHttpRoot;
        private string mHttpRoot = "";          //后面不带/的网址
        private Dictionary<string, string> mFileMap = new Dictionary<string, string>();
        public void SetHttpRoot(string root)
        {
            mHttpRoot = root;
            if(mHttpRoot.EndsWith("/"))
            {
                mHttpRoot = mHttpRoot.Substring(0, mHttpRoot.Length - 1);
            }
        }
        public void AddManifest(CenturyGame.AppUpdaterLib.Runtime.ResManifestParser.BaseResManifestParser parser, CenturyGame.AppUpdaterLib.Runtime.VersionManifest mani)
        {
            foreach(var d in mani.Datas)
            {
                //liuxiaochun
                var url = mHttpRoot + d.RN;
                mFileMap.Add(d.N, url);
            }
        }

        public string GetFilePath(string name)
        {
            return mFileMap.GetDefault(name, null);
        }

        public string GetDebugInfo()
        {
            using(ObjectPool<System.Text.StringBuilder>.GlobalPool.AllocStub(out var sb))
            {
                sb.AppendFormat("Root: {0}\nFiles:\n", mHttpRoot);
                foreach(var entry in mFileMap)
                {
                    sb.AppendFormat("{0} -> {1}\n", entry.Key, entry.Value);
                }
                return sb.ToString();
            }
        }

        public void Clear()
        {
            mHttpRoot = "";
            mFileMap.Clear();
        }
    }
}