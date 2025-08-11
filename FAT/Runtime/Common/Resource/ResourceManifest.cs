/**
 * @Author: handong.liu
 * @Date: 2022-11-17 14:45:42
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;


namespace EL.Resource
{
    public struct FileInfo
    {
        public string key;
        public string md5;
        public int size;
        public override string ToString()
        {
            return string.Format("({0}:{1}:{2}", key, size, md5);
        }
    }
    public class ResourceManifest
    {
        public int fileCount => mFiles.Count;
        private Dictionary<string, FileInfo> mFiles = new Dictionary<string, FileInfo>();

        public override string ToString()
        {
            return mFiles.ToStringEx();
        }

        public void FillAllFile(List<FileInfo> container)
        {
            container.AddRange(mFiles.Values);
        }

        public bool TryGetInfo(string key, out FileInfo f)
        {
            return mFiles.TryGetValue(key, out f);
        }

        //basePath不要有/结尾
        public void ParseFromCenturyGameManifest(string content, string gen, string basePath)
        {
            var mani = CenturyGame.AppUpdaterLib.Runtime.ResManifestParser.VersionManifestParser.Parse(content);
            ParseFromCenturyGameManifest(mani, gen, basePath);
        }

        public void ParseFromCenturyGameManifest(CenturyGame.AppUpdaterLib.Runtime.VersionManifest mani, string gen, string basePath)
        {
            if(!string.IsNullOrEmpty(basePath))
            {
                basePath = basePath + "/";
            }
            foreach(var f in mani.Datas)
            {
                string key = $"{basePath}{f.N}";
                mFiles[key] = new FileInfo() {
                    key = key,
                    md5 = f.H,
                    size = f.S
                };
            }
        }
    }
}