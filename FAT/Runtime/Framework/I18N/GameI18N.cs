/**
 * @Author: handong.liu
 * @Date: 2020-08-31 16:24:20
 */
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System;
using UnityEngine.UI;
using UnityEngine;
using CenturyGame.Framework.ConfigData;
using EL.Resource;
using EL;
using fat.rawdata;
using TMPro;

namespace FAT
{
    public static class GameI18NHelper
    {
        private static GameI18N current;
        public static GameI18N GetOrCreate()
        {
            if (current == null)
                current = new GameI18N();
            return current;
        }
    }

    public class GameI18N : I18N
    {
        public IEnumerator CoLoad(bool init = false)
        {
            var confPath = ConfHelper.GetTableLoadPath("LanguageConf", out _, false, Constant.kConfExt);
            bool loading = true;

            ConfHelper.AsyncLoadConf(confPath, (LanguageConf langs) =>
            {
                if (langs == null)
                {
                    DebugEx.Error($"[I18N] {init} LoadConf failed");
                }
                else
                {
                    foreach (var l in langs.LanguageSlice)
                    {
                        AddLanguage(l.Language_, l.Asset, l.IsTester);
                    }
                    DebugEx.Info($"[I18N] {init} add language conf done");
                }
                loading = false;
            });

            yield return new WaitWhile(() => loading);
#if UNITY_EDITOR
            //编辑器模式下 读取本地存的语言 如果没有 则默认设为英语
            var saved = PlayerPrefs.GetString(ConfHelper.PlayerPrefs_LangKey, string.Empty);
            if (string.IsNullOrEmpty(saved))
            {
                saved = "en";
            }
            var target = DeduceBestLanguage(saved);
            DebugEx.Info($"[I18N] {init} , saved lang {saved} / target lang {target}");
#else
            //非编辑器模式下 每次都直接读取系统语言 并进行校验 如果配置支持改语言 则直接使用 否则默认英语
            var systemLang = _GetSystemLanguage();
            var target = DeduceBestLanguage(systemLang);
            DebugEx.Info($"[I18N] {init} , system lang {systemLang} / target lang {target}");
#endif
            SetLanguage(target);
            FAT.Platform.PlatformSDK.Instance.SetLanguage(target);
            yield return null;
        }

        protected override void DoPrepareLanguage(int requestId, string lang, System.Action<string> onFinish)
        {
            PlayerPrefs.SetString(ConfHelper.PlayerPrefs_LangKey, lang);
            Game.Instance.StartCoroutineGlobal(_CoPrepareLanguage(requestId, lang, onFinish));
        }

        private byte[] LoadFileAsync(string file)
        {
            try
            {
                return ProtoFileUtility.Read(file);
            }
            catch (Exception e)
            {
                DebugEx.FormatError("ConfigMan::_CoLoadRawData ----> data load fail {0} {1}", file, e.Message);
            }
            return Array.Empty<byte>();
        }

        public void ClearLastPath()
        {
            mLastLangConfPath = null;
        }

        // 完整路径里带有hash 如LangZhHansCnConf_e2673802616cd16f7c10c2cad9aa274e.bytes
        // 可以用于识别相同文件 省略无效加载
        private string mLastLangConfPath = null;
        // 产品要求不使用pmt翻译系统的导表结果 直接用常规表格加载多语言配置
        // zh_hans_cn -> LangZhHansCnConf.bytes ; zh_hant_tw -> LangZhHantTwConf.bytes
        // zh_hans_cn -> LangZhHansCnHotfixConf.bytes ; zh_hant_tw -> LangZhHantTwHotfixConf.bytes
        IEnumerator _CoPrepareLanguage(int requestId, string lang, System.Action<string> onFinish)
        {
            // 加载主体文件
            var file = ConfHelper.GetTableLoadPath($"Lang{ConfHelper.ConvertLangStrToConfName(lang)}Conf", out bool fileReadable, false, Constant.kConfExt);
            if (string.Equals(file, mLastLangConfPath))
            {
                // 文件一致 忽略
                onFinish?.Invoke("conf no change, skip loading");
                yield break;
            }
            mLastLangConfPath = file;

            bool loading = true;
            // 每种语言的表结构一致 直接用简中conf结构作为解析器
            ConfHelper.AsyncLoadConf(file, (LangZhHansCnConf conf) =>
            {
                if (conf == null)
                {
                    DebugEx.Error($"[I18N] _CoPrepareLanguage failed");
                }
                else
                {
                    foreach (var entry in conf.LangZhHansCnSlice)
                    {
                        AddText(requestId, entry.LK, entry.Name);
                    }
                }

                // 加载语言hotfix文件
                var file_hotfix = ConfHelper.GetTableLoadPath($"Lang{ConfHelper.ConvertLangStrToConfName(lang)}HotfixConf", out bool fileReadable, false, Constant.kConfExt);
                ConfHelper.AsyncLoadConf(file_hotfix, (LangZhHansCnConf conf_hotfix) =>
                {
                    if (conf_hotfix != null)
                    {
                        foreach (var entry in conf_hotfix.LangZhHansCnSlice)
                        {
                            AddText(requestId, entry.LK, entry.Name);
                        }
                    }
                    loading = false;
                });
            });

            yield return new WaitWhile(() => loading);

            onFinish(null);
        }
        
        //运行时通过editor面板切换语言
        public void SwitchTargetLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang))
            {
                SetLanguage("en");
                return;
            }
            SetLanguage(lang);
            Game.Instance.StartCoroutineGlobal(CoLoadFontAsset());
        }
        
        public IEnumerator CoLoadFontAsset()
        {
            if (FontMaterialRes.Instance == null || FontMaterialRes.Instance.mainFontAsset == null) 
                yield break;
            var curLanguage = GetLanguage();
            var curFontAsset = GetLanguageFontAsset(curLanguage);
            var asset = curFontAsset?.ConvertToAssetConfig();
            DebugEx.FormatInfo("GameI18N::CoLoadFontAsset ----> try load font assets , language = {0}, asset = {1}", curLanguage, curFontAsset);
            if (string.IsNullOrEmpty(curFontAsset) || asset == null)
            {
                var fallback = FontMaterialRes.Instance.mainFontAsset.fallbackFontAssetTable;
                fallback.Clear();
                yield break;
            }
            var task = EL.Resource.ResManager.LoadAsset<TMP_FontAsset>(asset.Group, asset.Asset);
            yield return task;
            if (!task.isSuccess)
            {
                DebugEx.FormatError("GameI18N::CoLoadFontAsset ----> load font assets failed, {0}", task.error);
            }
            else if (task.asset is TMP_FontAsset fontAsset)
            {
                var fallback = FontMaterialRes.Instance.mainFontAsset.fallbackFontAssetTable;
                fallback.Clear();
                fallback.Add(fontAsset);
                DebugEx.FormatInfo("GameI18N::CoLoadFontAsset ----> load font assets success, language = {0}, asset = {1}:{2}", curLanguage, asset.Group, asset.Asset);
            }
        }

        private static string _GetSystemLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.English: return ("en");
                case SystemLanguage.Japanese: return ("ja");
                case SystemLanguage.Korean: return ("ko");
                case SystemLanguage.French: return ("fr");
                case SystemLanguage.German: return ("de");
                case SystemLanguage.Chinese: return ("zh_hans_cn");
                case SystemLanguage.ChineseSimplified: return ("zh_hans_cn");
                case SystemLanguage.ChineseTraditional: return ("zh_hant_tw");
                case SystemLanguage.Spanish: return ("es");
                case SystemLanguage.Portuguese: return ("pt");
                case SystemLanguage.Turkish: return ("tr");
                case SystemLanguage.Unknown: return ("en");
                default: return "en";
            }
        }
    }
}