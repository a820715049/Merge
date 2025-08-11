/**
 * @Author: handong.liu
 * @Date: 2020-11-24 12:56:16
 */
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;

//如果SwitchVariant之后需要做一些事，继承这个类，且这个类所有元素必须是public，必须可以被JsonUtility序列化
public class VariantApplyFinishCallbackBase
{
    public virtual void OnSwitchVariantFinish()
    {

    }

    public static VariantApplyFinishCallbackBase Unmarshal(string str)
    {
        var paramIdx = str.IndexOf(':');
        if(paramIdx > 0 && paramIdx < str.Length - 1)
        {
            var typeName = str.Substring(0, paramIdx);
            var serializedData = str.Substring(paramIdx + 1);
            var cls = CommonEditorUtility.GetListOfEntryAssemblyWithReferences().SelectMany((e) => e.GetTypes()).First((e) => e.FullName == typeName);
            if(cls == null)
            {
                Debug.LogWarningFormat("VariantApplyFinishCallbackBase ----> no cls {0}", str);
            }
            else
            {
                Debug.LogFormat("VariantApplyFinishCallbackBase ----> get {0}", str); 
                var inst = System.Activator.CreateInstance(cls) as VariantApplyFinishCallbackBase;
                JsonUtility.FromJsonOverwrite(serializedData, inst);
                return inst;
            }
        }
        return null;
    }

    public static string Marshal(VariantApplyFinishCallbackBase obj)
    {
        if(obj == null)
        {
            return "Null";
        }
        if(obj.GetType().GetCustomAttributes(typeof(System.SerializableAttribute), false).Length == 0)
        {
            Debug.LogErrorFormat("VariantApplyFinishCallbackBase ----> cls cannot be serialized {0}", obj.GetType().Name);
            return "Null";
        }
        else
        {
            return string.Format("{0}:{1}", obj.GetType().FullName, JsonUtility.ToJson(obj));
        }
    }
}

public class VariantApplyRecord
{
    private readonly BuildTargetGroup[] kBuildGroups = new BuildTargetGroup[] {
        BuildTargetGroup.iOS,
        BuildTargetGroup.Android
    };
    private bool mSDKProd = false;
    private bool mIsApplied = false;
    private string mTargetVariant;
    private string mPreviousVariant;
    private Dictionary<BuildTargetGroup, string> mPreviousBundleIds = new Dictionary<BuildTargetGroup, string>();
    private string mPreviousProductName;
    private string mPreviousSDKId;
    private string mPreviousSDKKey;
    private const string kScriptMacroDefineBegin = "VARIANT_MACRO_BEGIN";
    private const string kScriptMacroDefineEnd = "VARIANT_MACRO_END";
    private VariantApplyFinishCallbackBase mFinishAct;
    private static readonly Regex kScriptMacroRegex = new System.Text.RegularExpressions.Regex(kScriptMacroDefineBegin + ";([\\w\\;\\s-_]+);" + kScriptMacroDefineEnd);

    public VariantApplyRecord(string variant)
    {
        mTargetVariant = variant;
    }

    public void SetCompileFinishAction(VariantApplyFinishCallbackBase obj)
    {
        mFinishAct = obj;
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnCompileHandler()
    {
        if (PlayerPrefs.HasKey("___AppSettingRecompile"))
        {
            var cb = VariantApplyFinishCallbackBase.Unmarshal(PlayerPrefs.GetString("___AppSettingRecompile", ""));
            
            PlayerPrefs.DeleteKey("___AppSettingRecompile");
            var cls = CommonEditorUtility.GetSubclassOrSelf(typeof(VariantApplyRecord));
            if (cls != null)
            {
                var c = VariantEditorUtility.GetCurrentVariant();
                Debug.LogFormat("VariantApplyRecord::OnCompileHandler ----> get class {0}, for variant {1}", cls.Name, c);
                var instance = System.Activator.CreateInstance(cls, VariantEditorUtility.GetCurrentVariant()) as VariantApplyRecord;
                if (instance != null)
                {
                    Debug.LogFormat("VariantApplyRecord::OnCompileHandler ----> call");
                    instance.OnCompileOver();
                }
            }
            cb?.OnSwitchVariantFinish();
        }
    }

    public void Apply()
    {
        if (!mIsApplied)
        {
            mIsApplied = true;
            var path = AppVariant.kAppSettingResourcePath;
            var data = VariantEditorUtility.LoadEditorData();
            mPreviousVariant = data.currentVariant;
            SetCurrentVariant(mTargetVariant);
            var setting = VariantEditorUtility.LoadVariantConfig(mTargetVariant);
            if (setting != null)
            {
                var ret = UnityEngine.Object.Instantiate(setting);
                CommonEditorUtility.SaveAsset(ret, path);
                Debug.LogFormat("VariantEditorUtility.ApplyCurrentVariant ----> apply appsetting:{0}", JsonUtility.ToJson(setting));
            }
            else
            {
                throw new BuildFailedException(string.Format("VariantEditorUtility.ApplyCurrentVariant ----> appsetting:{0} no exists!", mTargetVariant));
            }

            PlayerSettings.bundleVersion = setting.version;
            OnApply(setting);
            if (!string.IsNullOrEmpty(setting.builderConfig.bundleId))
            {
                foreach(int groupInt in Enum.GetValues(typeof(BuildTargetGroup)))
                {
                    var group = (BuildTargetGroup)groupInt;
                    mPreviousBundleIds[group] = PlayerSettings.GetApplicationIdentifier(group);
                    PlayerSettings.SetApplicationIdentifier(group, setting.builderConfig.bundleId);
                }
            }
            mPreviousProductName = PlayerSettings.productName;
            if (!string.IsNullOrEmpty(setting.builderConfig.productName))
            {
                PlayerSettings.productName = setting.builderConfig.productName;
            }
            //for BuildTarget.iOS:
            PlayerSettings.iOS.buildNumber = setting.versionCode;
            //for BuildTarget.Android:
            if (int.TryParse(setting.versionCode, out var version))
            {
                PlayerSettings.Android.bundleVersionCode = version;
            }
            //for AppBuilder
            var builderConfig = CenturyGame.AppBuilder.Editor.Builds.AppBuildConfig.GetAppBuildConfigInst();
            string[] versions = setting.version.Split('.');
            builderConfig.targetAppVersion.Major = versions[0];
            builderConfig.targetAppVersion.Minor = versions[1];
            builderConfig.targetAppVersion.Patch = versions[2];
            builderConfig.upLoadInfo.protokitgoConfigName = setting.builderConfig.protokitConfName;
            builderConfig.upLoadInfo.remoteDir = setting.updaterConfig.remoteRoot;
            builderConfig.incrementRevisionNumberForPatchBuild = false;
            EditorUtility.SetDirty(builderConfig);
            //for AppUpdaterLib
            _SaveUpdaterConfig(setting.updaterConfig);

            //for macro
            foreach(int groupInt in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                var targetGroup = (BuildTargetGroup)groupInt;
                var macro = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
                macro = kScriptMacroRegex.Replace(macro, "");
                macro = macro + ";" + kScriptMacroDefineBegin + ";" + setting.builderConfig.scriptMacro + ";" + kScriptMacroDefineEnd;
                macro.Replace(";;", ";");
                if (macro.StartsWith(";"))
                {
                    macro = macro.Substring(1);
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, macro);
            }
            PlayerPrefs.SetString("___AppSettingRecompile", VariantApplyFinishCallbackBase.Marshal(mFinishAct));
            //resolve package manager
            MethodInfo method = typeof(UnityEditor.PackageManager.Client).GetMethod("Resolve", BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
            if (method != null)
            {
                Debug.Log("[AppSetting] call method resolve");
                method.Invoke(null, null);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("[AppSetting] no method resolve");
            }
            if (!EditorApplication.isCompiling)
            {
                OnCompileHandler();
            }
            else
            {
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            }
        }
    }

    public void Revert()
    {
        if (mIsApplied)
        {
            mIsApplied = false;
            OnRevert();
            foreach (var entry in mPreviousBundleIds)
            {
                PlayerSettings.SetApplicationIdentifier(entry.Key, entry.Value);
            }
            PlayerSettings.productName = mPreviousProductName;
            if (!string.IsNullOrEmpty(mPreviousVariant))
            {
                SetCurrentVariant(mPreviousVariant);
            }
        }
    }

    protected virtual void OnApply(AppSettings setting)
    {

    }
    protected virtual void OnRevert()
    {

    }
    protected virtual void OnCompileOver()
    {

    }
    public static void SetCurrentVariant(string variant)
    {
        var data = VariantEditorUtility.LoadEditorData();
        data.currentVariant = variant;
        VariantEditorUtility.SaveEditorData(data);
    }
    private static void _SaveUpdaterConfig(CenturyGame.AppUpdaterLib.Runtime.Configs.AppUpdaterConfig config)
    {
        string targetFolderPath = Application.dataPath + "/CenturyGamePackageRes/AppUpdaterLib/Resources";
        if (!Directory.Exists(targetFolderPath))
        {
            Directory.CreateDirectory(targetFolderPath);

            AssetDatabase.Refresh();
        }

        var path = targetFolderPath + "/appupdater.txt";

        string jsonContents = JsonUtility.ToJson(config, true);
        File.WriteAllText(path, jsonContents, new UTF8Encoding(false, true));

        Debug.Log("Write default appupdater default config completd!");

        AssetDatabase.Refresh();

    }
}

public class VariantEditorUtility : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public const string kVariantAssetsPath = "Assets/Variants/Settings";
    public const string kServerConfigPath = "Assets/Variants/Servers.assets";
    public static readonly string kAppSettingPathFormat = kVariantAssetsPath + "/{0}/config";
    public static string GetVariantConfigAssetPath(string variant)
    {
        return string.Format(kAppSettingPathFormat, variant ?? "");
    }
    public static AppSettings LoadCurrentAppSetting()
    {
        return LoadVariantConfig(GetCurrentVariant());
    }
    public static AppSettings LoadVariantConfig(string variant)
    {
        var path = GetVariantConfigAssetPath(variant);
        var setting = AssetDatabase.LoadAssetAtPath<AppSettings>(path + ".asset");
        return setting;
    }
    //return old app settings
    public static VariantApplyRecord SwitchVariant(string variant, VariantApplyFinishCallbackBase finishCB = null)
    {
        VariantApplyRecord record = null;
        record = CommonEditorUtility.CreateInstanceOfSubclass<VariantApplyRecord>(variant);
        record.SetCompileFinishAction(finishCB);
        record.Apply();
        return record;
    }
    public static readonly string kVariantPathFormat = kVariantAssetsPath + "/{0}";
    [Serializable]
    public class EditorData
    {
        public string currentVariant;
    }
    public static string GetCurrentVariant()
    {
        var data = LoadEditorData();
        return data.currentVariant;
    }
    public static string[] GetVariantList()
    {
        string[] dirs = System.IO.Directory.GetDirectories(kVariantAssetsPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        for (int i = 0; i < dirs.Length; i++)
        {
            dirs[i] = dirs[i].Substring(dirs[i].LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
        }
        return dirs;
    }
    public static string GetVariantDirAssetPath(string variant)
    {
        return string.Format(kVariantPathFormat, variant);
    }

    public static EditorData LoadEditorData()
    {
        var txt = AssetDatabase.LoadAssetAtPath<TextAsset>(kVariantAssetsPath + "/editor.json");
        EditorData ret = null;
        if (txt != null)
        {
            ret = JsonUtility.FromJson<EditorData>(txt.text);
        }
        if (ret == null)
        {
            ret = new EditorData();
        }
        return ret;
    }

    public static void SaveEditorData(EditorData data)
    {
        var path = kVariantAssetsPath + "/editor.json";
        var realPath = CommonEditorUtility.ConvertAssetPathToNativePath(path);
        File.WriteAllText(realPath, JsonUtility.ToJson(data));
        AssetDatabase.Refresh();
    }

    int IOrderedCallback.callbackOrder => -1;
    void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
    {
        var currentVariant = GetCurrentVariant();
        if (string.IsNullOrEmpty(currentVariant))
        {
            throw new BuildFailedException("VariantEditorUtility ----> no variant is selected");
        }
        Debug.LogFormat("VariantEditorUtility.OnPreprocessBuild ----> build with variant {0}", currentVariant);
    }

    void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
    {
    }

    [MenuItem("Tools/Variant/AppSetting")]
    static void Init()
    {
        AppSettingEditor window = (AppSettingEditor)EditorWindow.GetWindow(CommonEditorUtility.GetSubclassOrSelf(typeof(AppSettingEditor)));
        window.Show();
    }
}