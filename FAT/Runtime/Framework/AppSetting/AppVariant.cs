/**
 * @Author: handong.liu
 * @Date: 2020-11-24 13:24:33
 */
#if UNITY_EDITOR
using System.Reflection;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class AppVariant
{
    public readonly static string kAppSettingFileName = "config";
    public static readonly string kAppSettingResourcePath = string.Format("Assets/Resources/{0}.asset", kAppSettingFileName);

    public static AppSettings Load()
    {
#if UNITY_EDITOR
        var editorAssembly = System.AppDomain.CurrentDomain.GetAssemblies();
        System.Type targetType = null;
        foreach(var assembly in editorAssembly)
        {
            targetType = assembly.GetType("VariantEditorUtility");
            if(targetType != null)
            {
                break;
            }
        }
        var method = targetType.GetMethod("LoadCurrentAppSetting", BindingFlags.Static | BindingFlags.Public);
        AppSettings ret = method.Invoke(null, null) as AppSettings;
        return ret;
#else
        return Resources.Load(kAppSettingFileName) as AppSettings;
#endif
    }
}