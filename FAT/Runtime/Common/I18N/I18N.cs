/**
 * @Author: handong.liu
 * @Date: 2020-08-31 15:31:47
 */
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EL
{

    [System.Serializable]
    public class I18NLanguageText
    {
        public string k;
        public string v;
    }

    [System.Serializable]
    public class I18NLanguage
    {
        public string lang;
        public I18NLanguageText[] text;
    }
    public class I18N
    {
        public static event System.Action onLanguageChange;
        public static string DeduceBestLanguage(string nativeLanguage)
        {
            //传入语言可用的条件：1、配置表配了 2、isTester字段配false表示对所有人开放 3、isTester字段配true且当前账号为测试账号 表示仅对测试人员开放
            if(sInstance.mLanguageSet.TryGetValue(nativeLanguage, out var info))
            {
                var isTester = info.Item2;
                if (!isTester || GameSwitchManager.Instance.isDebugMode)
                    return nativeLanguage;
            }
            return "en";
        }
        public static void SetLanguage(string lang, bool force = false)
        {
            sInstance._SetLanguage(lang, force);
        }
        
        public static void GetAllLanguage(List<string> container)
        {
            container.AddRange(sInstance.mLanguageSet.Keys);
        }
        
        //获取语言对应的字体asset资源
        public static string GetLanguageFontAsset(string lang)
        {
            return sInstance.mLanguageSet.TryGetValue(lang, out var info) ? info.Item1 : "";
        }
        
        //获取语言是否仅对tester权限的账号开放
        public static bool GetLanguageIsTester(string lang)
        {
            return sInstance.mLanguageSet.TryGetValue(lang, out var info) && info.Item2;
        }
        
        public static void RefreshI18N()
        {
            sInstance._SetLanguageForcely(sInstance.mCurrentLanguage);
        }
        public static string GetLanguage()
        {
            if(!string.IsNullOrEmpty(sInstance.mTargetLanguage))
            {
                return sInstance.mTargetLanguage;
            }
            else
            {
                return sInstance.mCurrentLanguage;
            }
        }
        public static string GameErrorText(long code)
        {
            var ret = _Text($"#E{code}", false);
            if(ret == null)
            {
                ret = Text("#TipError") + $" - {code}";
            }
            return ret;
        }
        public static string SDKAndroidErrorText(long code)
        {
            return Text($"#SDKAndroidError{code}");
        }
        public static string TextNoPlaceholder(string key)
        {
            string ret = null;
            ret = _Text(key, false) ?? "";
            return ret;
        }
        public static string Text(string key)
        {
            string ret = null;
            ret = _Text(key, true);
            return ret;
        }
        public static string JoinSentence(string a, string b)
        {
            if(string.IsNullOrEmpty(a))
            {
                return b;
            } else if(string.IsNullOrEmpty(b)) {
                return a;
            } else {
                // return string.Format("{0} {1}", a, b);
                return $"{a}{b}";
            }
        }
        private static List<string> sEmptyFormat = new List<string>() {
            ""
        };
        public static string FormatText(string key, params object[] param)
        {
            string ret = _Text(key, false);
            if(string.IsNullOrEmpty(ret))
            {
                while(sEmptyFormat.Count <= param.Length)
                {
                    sEmptyFormat.Add(null);
                }
                if(string.IsNullOrEmpty(sEmptyFormat[param.Length]))
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for(int i = 0; i < param.Length; i++)
                    {
                        sb.AppendFormat("{{{0}}}", i);
                    }
                    sEmptyFormat[param.Length] = sb.ToString();
                }
                return string.Format(key + sEmptyFormat[param.Length], param);
            }
            else
            {
                return string.Format(ret, param);
            }
        }
        
        private static string _Text(string key, bool usePlaceHolder)
        {
            var ret = "";
            if(string.IsNullOrEmpty(key))
            {
                return "";
            }
            if(sInstance == null || sInstance.mPredefines.Count > 0 && sInstance.mPredefines.TryGetValue(key, out ret))
            {
                goto not_found;
            }
            if(((sInstance.mNextDict != null && sInstance.mNextDict.TryGetValue(key, out ret)) ||
                (sInstance.mDict != null && sInstance.mDict.TryGetValue(key, out ret))) && !string.IsNullOrEmpty(ret))
            {
                return ret;
            }
            else if(key.IndexOf("#") >= 0 && key.IndexOf(',') >= 0)
            {
                string[] ks = key.Split(',');
                for(int i = 0; i < ks.Length; i++)
                {
                    ks[i] = _Text(ks[i], true);
                }
                ret = string.Join("", ks);
                sInstance.mDict[key] = ret;         //save it
                return ret;
            }
            not_found:
            if(usePlaceHolder)
            {
                return string.Format("{{{0}}}", key);
            }
            else
            {
                return null;
            }
        }

        private static I18N sInstance;
        private string mCurrentLanguage = null;
        private string _tarLang = null;
        private string mTargetLanguage
        {
            get => _tarLang;
            set => _tarLang = value;
        }
        private Dictionary<string, string> mDict = new Dictionary<string, string>();
        private Dictionary<string, string> mNextDict;
        private Dictionary<string, string> mPredefines = new Dictionary<string, string>();
        private Dictionary<string, (string, bool)> mLanguageSet = new Dictionary<string, (string, bool)>();
        private int mCurrentRequestId = 0;

        private void _SetLanguage(string targetLang, bool force)
        {
            if (force)
            {
                _SetLanguageForcely(targetLang);
            }
            else
            {
                if (mCurrentLanguage == targetLang)
                {
                    mTargetLanguage = null;
                }
                else if (mTargetLanguage != targetLang)
                {
                    _SetLanguageForcely(targetLang);
                }
            }
        }

        private void _SetLanguageForcely(string targetLang)
        {
            mTargetLanguage = targetLang;
            if(mNextDict == null)
            {
                mNextDict = new Dictionary<string, string>();
                mNextDict.Clear();
            }
            mCurrentRequestId++;
            var requestId = mCurrentRequestId;
            DoPrepareLanguage(requestId, targetLang, (err)=>{
                if(requestId == mCurrentRequestId)
                {
                    if(string.IsNullOrEmpty(err))
                    {
                        mDict = mNextDict;
                        mNextDict = null;
                        mCurrentLanguage = targetLang;
                        mTargetLanguage = null;
                        onLanguageChange?.Invoke();
                        DebugEx.FormatInfo("I18N._SetLanguage ----> switch language to {0}", targetLang);
                    }
                    else
                    {
                        DebugEx.FormatWarning("I18N._SetLanguage ----> switch language to {0} error: {1}", targetLang, err);
                    }
                }
            });
        }

        protected I18N()
        {
            DebugEx.FormatInfo("I18N ----> inited as {0}, previous is {1}", GetType().Name, sInstance != null?sInstance.GetType().Name:"null");
            sInstance = this;
        }

        public static void SetPredefineText(string k, string v)
        {
            sInstance.mPredefines[k] = v;
        }

        protected virtual void DoPrepareLanguage (int requestId, string lang, System.Action<string> onFinish/*param error*/)
        {
            onFinish("Not Implemented!");
        }

        protected void AddText(int requestId, string key, string value)
        {
            if(mCurrentRequestId == requestId)
            {
                mNextDict[key] = value;
            }
        }

        protected void AddLanguage(string lang, string fontAsset, bool isTester)
        {
            if(!mLanguageSet.ContainsKey(lang))
            {
                mLanguageSet.Add(lang, (fontAsset, isTester));
                DebugEx.FormatInfo("I18N.AddLanguage ----> added language = {0}, fontAsset = {1}, isTester = {2}", lang, fontAsset, isTester);
            }
        }
    }
}