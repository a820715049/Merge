/**
 * @Author: handong.liu
 * @Date: 2020-08-31 19:10:26
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using UnityEditor;
using fat.rawdata;
using FAT;

namespace EL
{
    public class EditorI18N : I18N
    {
        private static EditorI18N sI18N;
        // public static string currentLanguage { get { return sCurrentLanguage; } }
        // private static string sCurrentLanguage = "";
        private static List<string> sAllLanguage;
        public static List<string> languages
        {
            get
            {
                if (sAllLanguage == null)
                {
                    sAllLanguage = new List<string>();
                    var confPath = ConfHelper.GetTableLoadPath("LanguageConf", out _, false, Constant.kConfExt);
                    ConfHelper.AsyncLoadConf(confPath, (LanguageConf langs) =>
                    {
                        if (langs == null) return;
                        foreach (var l in langs.LanguageSlice)
                        {
                            sAllLanguage.Add(l.Language_);
                        }
                    });
                }
                return sAllLanguage;
            }
        }

        public EditorI18N()
        {
            foreach(var lang in languages)
            {
                AddLanguage(lang, "", false);
            }
            var langs = PlayerPrefs.GetString(ConfHelper.PlayerPrefs_LangKey, "en");
            SetLanguage(langs);
        }

        public new static void RefreshI18N()
        {
            sAllLanguage = null;
            sI18N = new EditorI18N();
        }
        
        // [MenuItem("I18N/切换")]
        static void SwitchI18N()
        {
            if(sI18N == null)
            {
                sI18N = new EditorI18N();
            }
            sI18N.SwitchNextLanguage();
        }
        
        // [MenuItem("I18N/重置")]
        static void MenuRefreshI18N()
        {
            RefreshI18N();
            sI18N.SwitchNextLanguage();
        }

        public void SwitchNextLanguage()
        {
            if (languages.Count <= 0)
            {
                SetLanguage("en");
                return;
            }
            int idx = -1;
            for(int i = 0; i < languages.Count; i++)
            {
                if(languages[i] == GetLanguage())
                {
                    idx = i;
                    break;
                }
            }
            SetLanguage(languages[(idx + 1) % languages.Count]);
        }

        protected override void DoPrepareLanguage(int requestId, string lang, System.Action<string> onFinish)
        {
            PlayerPrefs.SetString(ConfHelper.PlayerPrefs_LangKey, lang);
            var confPath = ConfHelper.GetTableLoadPath($"Lang{ConfHelper.ConvertLangStrToConfName(lang)}Conf", out _, false, Constant.kConfExt);
            if (System.IO.File.Exists(confPath))
            {
                // 每种语言的表结构一致 直接用简中conf结构作为解析器
                ConfHelper.AsyncLoadConf(confPath, (LangZhHansCnConf langConf) =>
                {
                    foreach (var entry in langConf.LangZhHansCnSlice)
                    {
                        AddText(requestId, entry.LK, entry.Name);
                    }
                    onFinish(null);
                });
            }
            else
            {
                onFinish("no language");
            }
        }
        
        //editor工具调用选择当前语言
        public static void SwitchTargetLanguage(string lang)
        {
            sI18N ??= new EditorI18N();
            if (languages.Count <= 0 || string.IsNullOrEmpty(lang))
            {
                SetLanguage("en");
                return;
            }
            SetLanguage(lang);
        }
        
        public static string GetLanguageShowName(string lang)
        {
            switch (lang)
            {
                case "en": return ("英语");
                case "ja": return ("日语");
                case "ko": return ("韩语");
                case "fr": return ("法语");
                case "de": return ("德语");
                case "zh_hans_cn": return ("简体中文");
                case "zh_hant_tw": return ("繁体中文");
                case "es": return ("西班牙语");
                case "pt": return ("葡萄牙语");
                case "tr": return ("土耳其语");
                default: return "英语";
            }
        }
    }
}