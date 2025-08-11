/*
 * @Author: qun.chao
 * @Date: 2023-10-12 14:48:41
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Config;
using EL;
using fat.rawdata;
using conf = fat.conf;

namespace FAT
{
    public partial class ConfigMan : IGameModule, IConfigProvider
    {
        public int configVersion => 2; //Game.Instance.globalConfig.Version;
        public Global globalConfig { get; private set; }
        // public System.Text.StringBuilder loadErrors => mLoadErrors;
        private System.Text.StringBuilder mLoadErrors = new System.Text.StringBuilder();
        //当前用户在所有ab测试组中的分组配置标签
        //目前只有debug面板中在使用 实际底层的读配置逻辑已经自动应用了tag标签 获取配置数据时直接拿就可以 无需额外传递tag
        public IEnumerable<string> abTags => mAbTags;
        private List<string> mAbTags = new List<string>();

        public bool IsAllConfigReady => allConfProcessed && total_error_count == 0;

        private bool allConfProcessed = false;
        private int total_processed_conf_count = 0;
        private int total_error_count = 0;
        private Dictionary<string, Action<Google.Protobuf.CodedInputStream>> loaders => conf.Data.Loaders;

        public IEnumerator CoLoadAll()
        {
            allConfProcessed = false;
            conf.conf_loader.ConfManager.SetABFallback(conf.conf_loader.ConfManager.TagCommon, new List<string> { conf.conf_loader.ConfManager.TagFoundation });
            total_processed_conf_count = 0;
            total_error_count = 0;
            _LoadRawData();
            yield return new WaitUntil(() => loaders.Count == total_processed_conf_count);
            allConfProcessed = true;
        }

        public float LoadingProgress()
        {
            return total_processed_conf_count * 1f / loaders.Count;
        }

        //越靠前优先级越高
        public void SetAbTags(List<string> tags)
        {
            string defaultTag = conf.conf_loader.ConfManager.TagFoundation;
            if (tags.Count > 0)
            {
                defaultTag = tags[0];
                tags.RemoveAt(0);
            }
            mAbTags.Clear();
            mAbTags.Add(defaultTag);
            mAbTags.AddRange(tags);
            tags.Add(conf.conf_loader.ConfManager.TagFoundation);           //一定要有foundation
            DebugEx.FormatInfo("ConfigMan::SetAbTags {0}:{1}", defaultTag, tags);
            conf.conf_loader.ConfManager.SetABDefault(defaultTag);
            conf.conf_loader.ConfManager.SetABFallback(defaultTag, tags);
        }

        private void _LoadRawData()
        {
            var loader = loaders;
            DebugEx.Info($"load file total {loader.Count}");

            foreach (var kvPair in loader)
            {
                // 多语言配置在I18N类里自行读取
                if (kvPair.Key.StartsWith("Lang"))
                {
                    ++total_processed_conf_count;
                    continue;
                }
                var file = ConfHelper.GetTableLoadPath(kvPair.Key, out bool fileReadable, false, Constant.kConfExt);
                ConfHelper.AsyncRegisterConf(file, (data) =>
                {
                    if (data != null)
                    {
                        kvPair.Value?.Invoke(data);
                    }
                    else
                    {
                        ++total_error_count;
                        Debug.Log($"ConfigMan::_LoadRawData error {file}");
                    }
                    ++total_processed_conf_count;
                });
            }
        }

        void IGameModule.Reset()
        {
            allConfProcessed = false;
            total_processed_conf_count = 0;
            total_error_count = 0;
            mLoadErrors.Clear();
            conf.Data.Clear();
        }

        void IGameModule.LoadConfig()
        {
            globalConfig = GetGlobalConfig();
            if (globalConfig == null)
            {
                Debug.LogError("globalconfig null");
            }
        }
        void IGameModule.Startup() { }
    }
}