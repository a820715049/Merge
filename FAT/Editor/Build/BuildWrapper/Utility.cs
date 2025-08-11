/*
 * @Author: qun.chao
 * @Date: 2023-05-22 12:20:36
 */
using System;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using UnityEditor;
#if UNITY_IPHONE
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
#endif
using System.Linq;
using EL;
using static UnityEditor.PlayerSettings.SplashScreen;

namespace BuildWrapper
{
    /*
    拆分 AssetBundleBuilder
    拆分 Variants类携带的各种操作
    抽取通用工具方法
    */
    public static class Utility
    {
        private const string kScriptMacroDefineBegin = "VARIANT_MACRO_BEGIN";
        private const string kScriptMacroDefineEnd = "VARIANT_MACRO_END";
        private static readonly Regex kScriptMacroRegex = new Regex(kScriptMacroDefineBegin + "([\\w\\;\\s-_]*)" + kScriptMacroDefineEnd);

        #region editor menu
        [MenuItem("Tools/SwitchVariant/global-qa")]
        public static void SwitchToAsiaQa()
        {
            SwitchVariant_ForEitorRun("asia_android_qa");
        }
        #endregion

        public static void ForceCompile()
        {
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }

        public static void SwitchVariant_ForEitorRun(string targetVariant)
        {
            var path = AppVariant.kAppSettingResourcePath;

            // save editor
            var data = VariantEditorUtility.LoadEditorData();
            data.currentVariant = targetVariant;
            VariantEditorUtility.SaveEditorData(data);

            var setting = VariantEditorUtility.LoadVariantConfig(targetVariant);
            if (setting != null)
            {
                var ret = UnityEngine.Object.Instantiate(setting);
                CommonEditorUtility.SaveAsset(ret, path);
                Debug.LogFormat("VariantEditorUtility.ApplyCurrentVariant ----> apply appsetting:{0}", JsonUtility.ToJson(setting));
            }
            else
            {
                throw new SystemException(string.Format("VariantEditorUtility.ApplyCurrentVariant ----> appsetting:{0} no exists!", targetVariant));
            }

            Utility.Apply_AppBuilder_Updater(setting);

            Utility.Apply_Macro(setting);

            // 保存修改
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 切换env以后 一般需要 [解析package] / [导入资源] /  [触发脚本编译] / ...
        /// 这些步骤完成后才能进行后续的逻辑
        /// 现在使用的VariantApplyRecord机制偶尔会失效
        /// 简单起见 这里切换完直接结束
        /// </summary>
        public static void SwitchVariant()
        {
            var args = BuildArgs.Create();
            Utility.SwitchVariant(args.variant);
        }

        public static void SwitchVariant(string targetVariant)
        {
            var path = AppVariant.kAppSettingResourcePath;

            // save editor
            var data = VariantEditorUtility.LoadEditorData();
            data.currentVariant = targetVariant;
            VariantEditorUtility.SaveEditorData(data);

            var setting = VariantEditorUtility.LoadVariantConfig(targetVariant);
            if (setting != null)
            {
                var ret = UnityEngine.Object.Instantiate(setting);
                CommonEditorUtility.SaveAsset(ret, path);
                Debug.LogFormat("VariantEditorUtility.ApplyCurrentVariant ----> apply appsetting:{0}", JsonUtility.ToJson(setting));
            }
            else
            {
                throw new SystemException(string.Format("VariantEditorUtility.ApplyCurrentVariant ----> appsetting:{0} no exists!", targetVariant));
            }

            Utility.Apply_Identifier(setting);

            Utility.Apply_ProductName(setting);

            Utility.Apply_ProductVersion(setting);

            Utility.Apply_AppBuilder_Updater(setting);

            Utility.Apply_Macro(setting);

            // UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }

        public static void Apply_Identifier(AppSettings setting = null)
        {
            setting = setting ?? VariantEditorUtility.LoadCurrentAppSetting();

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, setting.builderConfig.bundleId);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, setting.builderConfig.bundleId);
            // if (!string.IsNullOrEmpty(setting.builderConfig.bundleId))
            // {
            //     foreach (int groupInt in Enum.GetValues(typeof(BuildTargetGroup)))
            //     {
            //         var group = (BuildTargetGroup)groupInt;
            //         PlayerSettings.SetApplicationIdentifier(group, setting.builderConfig.bundleId);
            //     }
            // }
        }

        public static void Apply_ProductVersion(AppSettings setting = null)
        {
            setting = setting ?? VariantEditorUtility.LoadCurrentAppSetting();
            PlayerSettings.bundleVersion = setting.version;
#if UNITY_IPHONE
            PlayerSettings.bundleVersion = setting.version.Substring(0, setting.version.LastIndexOf("."));
#endif
            //for BuildTarget.iOS:
            PlayerSettings.iOS.buildNumber = setting.versionCode;
            //for BuildTarget.Android:
            if (int.TryParse(setting.versionCode, out var version))
            {
                PlayerSettings.Android.bundleVersionCode = version;
            }
        }

        public static void Apply_ProductName(AppSettings setting = null)
        {
            setting = setting ?? VariantEditorUtility.LoadCurrentAppSetting();
            if (!string.IsNullOrEmpty(setting.builderConfig.productName))
            {
                PlayerSettings.productName = setting.builderConfig.productName;
            }
        }

        public static void Apply_Macro(AppSettings setting = null)
        {
            setting = setting ?? VariantEditorUtility.LoadCurrentAppSetting();

#if UNITY_ANDROID
            var groupInt = BuildTargetGroup.Android;
#else
            var groupInt = BuildTargetGroup.iOS;
#endif
            // foreach (int groupInt in Enum.GetValues(typeof(BuildTargetGroup)))
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
        }

        public static void Apply_AppBuilder_Updater(AppSettings setting = null)
        {
            // builder
            setting = setting ?? VariantEditorUtility.LoadCurrentAppSetting();
            var builderConfig = CenturyGame.AppBuilder.Editor.Builds.AppBuildConfig.GetAppBuildConfigInst();
            string[] versions = setting.version.Split('.');
            builderConfig.targetAppVersion.Major = versions[0];
            builderConfig.targetAppVersion.Minor = versions[1];
            builderConfig.targetAppVersion.Patch = versions[2];
            builderConfig.upLoadInfo.protokitgoConfigName = setting.builderConfig.protokitConfName;
            builderConfig.upLoadInfo.remoteDir = setting.updaterConfig.remoteRoot;
            builderConfig.incrementRevisionNumberForPatchBuild = false;
            EditorUtility.SetDirty(builderConfig);

            // updater
            string targetFolderPath = Application.dataPath + "/CenturyGamePackageRes/AppUpdaterLib/Resources";
            if (!Directory.Exists(targetFolderPath))
            {
                Directory.CreateDirectory(targetFolderPath);

                AssetDatabase.Refresh();
            }
            var path = targetFolderPath + "/appupdater.txt";
            string jsonContents = JsonUtility.ToJson(setting.updaterConfig, true);
            File.WriteAllText(path, jsonContents, new System.Text.UTF8Encoding(false, true));
            Debug.Log("Write default appupdater default config completd!");
            AssetDatabase.Refresh();
        }

        public static void Apply_AndroidSigningConfig(AppSettings setting = null)
        {
            setting = setting ?? VariantEditorUtility.LoadCurrentAppSetting();
            string keystorePath = Path.Combine(CommonEditorUtility.projectPath, "cert", setting.builderConfig.apkKeyStore);
            DebugEx.FormatInfo("BuildWrapper.AndroidSigningConfig ----> use keystore {0}", keystorePath);
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = setting.builderConfig.apkPwd;
            PlayerSettings.Android.keyaliasName = setting.builderConfig.apkVariant;
            PlayerSettings.Android.keyaliasPass = setting.builderConfig.apkPwd;
        }

        public static void Apply_SplashScreen_No()
        {
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
        }

        public static void Apply_SplashScreen(string logoPath, float duration, UnityLogoStyle style = UnityLogoStyle.LightOnDark, bool append = false)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);
            var logo = PlayerSettings.SplashScreenLogo.Create(duration, sp);
            var logos = new PlayerSettings.SplashScreenLogo[] { logo };

            if (append)
            {
                PlayerSettings.SplashScreen.logos = PlayerSettings.SplashScreen.logos.Concat(logos).ToArray();
            }
            else
            {
                PlayerSettings.SplashScreen.show = true;
                PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
                PlayerSettings.SplashScreen.unityLogoStyle = style;
                PlayerSettings.SplashScreen.showUnityLogo = false;
                PlayerSettings.SplashScreen.logos = logos;
            }
        }

        public static void Apply_ExportAsGoogleAndroidProject(bool flag, out bool prevFlag)
        {
            prevFlag = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = flag;
        }

        public static void Apply_ExportForAppBundle(bool flag, out bool prevFlag)
        {
            prevFlag = EditorUserBuildSettings.buildAppBundle;
            EditorUserBuildSettings.buildAppBundle = flag;
        }

        public static void Apply_SplitApplicationBinary(bool flag, out bool prevFlag)
        {
            prevFlag = PlayerSettings.Android.useAPKExpansionFiles;
            PlayerSettings.Android.useAPKExpansionFiles = flag;
        }

        public static void BuildAndroid(string targetPath, AssetBundleBuilder.Request.ProfilerType pt, AppSettings setting = null, string[] levels = null)
        {
            setting = setting ?? VariantEditorUtility.LoadCurrentAppSetting();
            var opt = BuildOptions.None;
            if (pt != AssetBundleBuilder.Request.ProfilerType.None)
            {
                opt |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;
                if (pt == AssetBundleBuilder.Request.ProfilerType.Deep)
                {
                    opt |= BuildOptions.EnableDeepProfilingSupport;
                }
            }

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            // PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.stripEngineCode = false;
            EditorUserBuildSettings.androidCreateSymbolsZip = false;

            if (levels == null)
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, targetPath, BuildTarget.Android, opt);
            else
                BuildPipeline.BuildPlayer(levels, targetPath, BuildTarget.Android, opt);
        }

        public static void BuildXcode(string xcodeProjectDir, AssetBundleBuilder.Request.ProfilerType pt, AppSettings setting = null, string[] levels = null)
        {
            setting = setting ?? VariantEditorUtility.LoadCurrentAppSetting();
            var opt = BuildOptions.None;
            if (pt != AssetBundleBuilder.Request.ProfilerType.None)
            {
                opt |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;
                if (pt == AssetBundleBuilder.Request.ProfilerType.Deep)
                {
                    opt |= BuildOptions.EnableDeepProfilingSupport;
                }
            }

            string teamId = setting?.builderConfig?.iosSignTeamId;
            string profileId = setting?.builderConfig?.iosSignProfileId;
            PlayerSettings.iOS.appleDeveloperTeamID = teamId;
            PlayerSettings.iOS.iOSManualProvisioningProfileID = profileId;
            PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Automatic;
            PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;

            PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);
            AssetDatabase.Refresh();
            var previousStrip = PlayerSettings.stripEngineCode;
            PlayerSettings.stripEngineCode = false;
            if (levels == null)
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, xcodeProjectDir, BuildTarget.iOS, opt);
            else
                BuildPipeline.BuildPlayer(levels, xcodeProjectDir, BuildTarget.iOS, opt);
            PlayerSettings.stripEngineCode = previousStrip;
            AssetDatabase.SaveAssets();
        }

        public static void BuildExternalResource()
        {
            bool success = false;
            try
            {
                DebugEx.Info("BuildWrapper.BuildExternalResource begin");
                success = AppResourceTool.MakeExternalBundle();
            }
            catch (System.Exception ex)
            {
                DebugEx.FormatError("BuildWrapper.BuildExternalResource ----> error:{0}:{1}", ex.ToString(), ex.StackTrace);
                success = false;
            }
            finally
            {
                AssetDatabase.SaveAssets();
            }

            if (success)
            {
                DebugEx.Info("BuildWrapper.BuildExternalResource succeed");
            }
            else
            {
                DebugEx.Error("BuildWrapper.BuildExternalResource failed");
                throw new Exception("build failed");
            }
        }

        // 制作bundle / 上传资源到conf / 制作StreamingAssets
        public static void BuildResource(bool upload, bool encrypt, bool useLastBuildResultInTemporary)
        {
            // 不上传资源 直接使用上次在打包路径里缓存的资源 位于Temporary/AB目录
            bool noBuild = useLastBuildResultInTemporary;
            var setting = VariantEditorUtility.LoadCurrentAppSetting();
            var versions = setting.version.Split('.');
            CenturyGame.AppBuilder.Editor.Builds.AppBuildConfig.GetAppBuildConfigInst().upLoadInfo.isUploadToRemote = upload;
            var oldEncrypt = AppResourceTool.needProtect;
            AppResourceTool.needProtect = encrypt; // 易盾加密

            EditorUtility.SetDirty(CenturyGame.AppBuilder.Editor.Builds.AppBuildConfig.GetAppBuildConfigInst());
            EditorSettings.spritePackerMode = SpritePackerMode.BuildTimeOnly;
            AssetDatabase.Refresh();
            try
            {
                bool success = false;
                if (noBuild)
                {
                    DebugEx.FormatInfo("BuildWrapper.BuildResource MakeVersionStreamingAssets ----> copy existing resources", setting.variant);
                    success = AppResourceTool.MakeVersionStreamingAssets();
                }
                else if (versions[2] == "0")
                {
                    DebugEx.FormatInfo("BuildWrapper.BuildResource MakeBaseVersionResources ----> make base resources", setting.variant);
                    success = AppResourceTool.MakeBaseVersionResources();
                }
                else
                {
                    DebugEx.FormatInfo("BuildWrapper.BuildResource MakePatchResouces ----> make patch resources", setting.variant);
                    success = AppResourceTool.MakePatchResouces();
                }
            }
            catch (System.Exception ex)
            {
                DebugEx.FormatError("BuildWrapper.BuildResource ----> error:{0}:{1}", ex.ToString(), ex.StackTrace);
            }
            finally
            {
                EditorSettings.spritePackerMode = SpritePackerMode.Disabled;
                DebugEx.FormatInfo("[SpritePacker] ----> restore to {0}", EditorSettings.spritePackerMode);
                AssetDatabase.SaveAssets();
                AppResourceTool.needProtect = oldEncrypt;
            }
        }

        public static void Apply_FakeBitCode(string xcodeProjectDir)
        {
            // TODO 暂无易盾
            // AssetBundleBuilder.CallFakeBitcode(Path.GetFullPath(xcodeProjectDir));
        }

        public static void Apply_NoAppStore(string xcodeProjectDir)
        {
            var fullPath = System.IO.Path.Combine(System.IO.Path.GetFullPath(xcodeProjectDir), "Classes", "Native");
            DebugEx.FormatInfo("BuildWrapper.NoAppStore ----> build no appstore ios {0}", fullPath);
            // TODO 暂时不需要屏蔽iap
            // AssetBundleBuilder.ModifyIL2CppNoPurchase(fullPath);
        }

        public static void Apply_XcodeProjectSigning(string xcodeProjectDir, AppSettings setting = null)
        {
            setting = setting ?? VariantEditorUtility.LoadCurrentAppSetting();
            var pbxproj = Path.Combine(xcodeProjectDir, "Unity-iPhone.xcodeproj", "project.pbxproj");
            Apply_XcodeSigningByGuid(pbxproj, null, setting.builderConfig.iosSignProfileName, setting.builderConfig.iosSignTeamId);
        }

        public static void Apply_XcodeProjectSigning_PushNotification(string xcodeProjectDir, AppSettings setting = null)
        {
            // 目前仅服务于asia版sdk / target写死 / provision名称和主provision有如下推导关系
            // DDAsia_Applestore_20230511 -> DDAsiaPush_Applestore_20230511
            var target = "CGPushServerExtension";
            setting = setting ?? VariantEditorUtility.LoadCurrentAppSetting();
            var strs = setting.builderConfig.iosSignProfileName.Split('_');
            var provision = $"{strs[0]}Push";
            for (int i = 1; i < strs.Length; ++i)
            {
                provision = $"{provision}_{strs[i]}";
            }
            var pbxproj = Path.Combine(xcodeProjectDir, "Unity-iPhone.xcodeproj", "project.pbxproj");
            Apply_XcodeSigningByGuid(pbxproj, target, provision, setting.builderConfig.iosSignTeamId);
        }

        // 参考sdk里的写法
        public static void Apply_XcodeSigningByGuid(string pbxproj, string target, string provision, string teamId)
        {
#if UNITY_IOS
            var codesign = "Apple Distribution";
            if (provision.ToLower().Contains("_dev_"))
            {
                codesign = "Apple Development";
            }
            var proj = new PBXProject();
            proj.ReadFromFile(pbxproj);
            var guid = string.Empty;
            if (string.IsNullOrEmpty(target))
                guid = proj.GetUnityMainTargetGuid();
            else
                guid = proj.TargetGuidByName(target);
            proj.SetBuildProperty(guid, "CODE_SIGN_IDENTITY", codesign);
            proj.SetBuildProperty(guid, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", codesign);
            proj.SetBuildProperty(guid, "CODE_SIGN_STYLE", "Manual");
            proj.SetBuildProperty(guid, "PROVISIONING_PROFILE", provision);
            proj.SetBuildProperty(guid, "PROVISIONING_PROFILE_SPECIFIER", provision);
            if (!string.IsNullOrEmpty(teamId))
                proj.SetBuildProperty(guid, "DEVELOPMENT_TEAM", teamId);
            proj.WriteToFile(pbxproj);
#endif
        }

        public static void Apply_TestFlightComplianceInformation(string xcodeProjectDir)
        {
#if UNITY_IPHONE
			string plistPath = Path.Combine(xcodeProjectDir, "Info.plist");
			var plist = new PlistDocument();
			plist.ReadFromFile(plistPath);
			plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);
#endif
        }

        // ref: https://stackoverflow.com/a/76065241
        public static void Apply_Embed_Swift_No(string xcodeProjectDir)
        {
#if UNITY_IPHONE
            var proj = new PBXProject();
            var pbxproj = Path.Combine(xcodeProjectDir, "Unity-iPhone.xcodeproj", "project.pbxproj");
            proj.ReadFromFile(pbxproj);
            var guid = proj.GetUnityFrameworkTargetGuid();
            proj.SetBuildProperty(guid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
            proj.WriteToFile(pbxproj);
#endif
        }

        public static void Apply_Localization_Asia(string xcodeProjectDir)
        {
            Apply_Localization_Add_Lang(xcodeProjectDir, "en");
            Apply_Localization_Add_Lang(xcodeProjectDir, "zh_CN");
            Apply_Localization_Add_Lang(xcodeProjectDir, "zh_TW");
        }

        public static void Apply_Localization_Mainland(string xcodeProjectDir)
        {
            Apply_Localization_Add_Lang(xcodeProjectDir, "zh_CN");
        }

        private static void Apply_Localization_Add_Lang(string xcodeProjectDir, string lang)
        {
#if UNITY_IPHONE
			string plistPath = Path.Combine(xcodeProjectDir, "Info.plist");
			var plist = new PlistDocument();
			plist.ReadFromFile(plistPath);

            var key = "CFBundleLocalizations";
            PlistElementArray langs = null;
			if (plist.root.values.ContainsKey(key))
			{
				langs = plist.root[key].AsArray();
			}
			else
			{
				langs = plist.root.CreateArray(key);
			}
			langs.AddString(lang);

            plist.WriteToFile(plistPath);
#endif
        }

        public static void Apply_Scheme_For_Ads(string xcodeProjectDir)
        {
            var scheme_path = Path.Combine(Application.dataPath, "SDK", "Resources", "ios_scheme_for_ads");
            
#if UNITY_IPHONE
			string plistPath = Path.Combine(xcodeProjectDir, "Info.plist");
			var plist = new PlistDocument();
			plist.ReadFromFile(plistPath);

            var key = "LSApplicationQueriesSchemes";
            PlistElementArray schemes = null;
			if (plist.root.values.ContainsKey(key))
			{
				schemes = plist.root[key].AsArray();
			}
			else
			{
				schemes = plist.root.CreateArray(key);
			}
            var scheme_all = System.IO.File.ReadAllLines(scheme_path);
            foreach (var s in scheme_all)
            {
                schemes.AddString(s);
            }

            plist.WriteToFile(plistPath);
#endif
        }

        // ref https://github.com/firebase/firebase-cpp-sdk/issues/700
        public static void Apply_TryFixCrashFor_15_5(string xcodeProjectDir)
        {
#if UNITY_IPHONE
			string plistPath = Path.Combine(xcodeProjectDir, "Info.plist");
			var plist = new PlistDocument();
			plist.ReadFromFile(plistPath);
			plist.root.SetBoolean("FirebaseAppStoreReceiptURLCheckEnabled", false);
            plist.WriteToFile(plistPath);
#endif
        }

        // unity bug 中文字符产品名属性设置问题
        // https://www.cnblogs.com/fzuljz/p/16924842.html
        public static void Apply_XcodeProductName(string xcodeProjectDir, AppSettings setting = null)
        {
#if UNITY_IPHONE
            setting = setting ?? VariantEditorUtility.LoadCurrentAppSetting();
            var projectFile = Path.Combine(xcodeProjectDir, "Unity-iPhone.xcodeproj", "project.pbxproj");
            var proj = new PBXProject();
            proj.ReadFromFile(projectFile);
            string target = proj.GetUnityMainTargetGuid();
            proj.SetBuildProperty(target, "PRODUCT_NAME_APP", setting.builderConfig.productName);
            proj.WriteToFile(projectFile);
#endif
        }

        public static void Apply_DefaultSplashScreen()
        {
            Utility.Apply_SplashScreen("Assets/ResourceForApp/logo/logo-black.png", 2f, UnityLogoStyle.DarkOnLight);
        }

        public static void Apply_SplashScreen_White()
        {
            PlayerSettings.SplashScreen.backgroundColor = Color.black;
            Utility.Apply_SplashScreen("Assets/ResourceForApp/logo/logo-white.png", 2f, UnityLogoStyle.LightOnDark);
        }

        public static void Apply_LaunchScreen_Android()
        {
            Utility.Apply_SplashScreen_No();
            PlayerSettings.SplashScreen.backgroundColor = Color.black;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ResourceForApp/Logo/portrait_black.png");
            const string projectSettings = "ProjectSettings/ProjectSettings.asset";
            var obj = LoadSerializedObject(projectSettings);
            Apply_Property_Int(obj, "androidSplashScreen.m_FileID", tex.GetInstanceID());
        }

        public static void Apply_LaunchScreen_IOS()
        {
            Utility.Apply_SplashScreen_No();

            PlayerSettings.iOS.SetiPhoneLaunchScreenType(iOSLaunchScreenType.CustomStoryboard);
            PlayerSettings.iOS.SetiPadLaunchScreenType(iOSLaunchScreenType.CustomStoryboard);
            var story_board_path = "Assets/ResourceForApp/logo/StoryBoard/launch-whitelogo.storyboard";
            const string projectSettings = "ProjectSettings/ProjectSettings.asset";
            var obj = LoadSerializedObject(projectSettings);
            Apply_Property_String(obj, "iOSLaunchScreenCustomStoryboardPath", story_board_path);
            Apply_Property_String(obj, "iOSLaunchScreeniPadCustomStoryboardPath", story_board_path);
        }

        public static void Apply_LaunchScreen_IOS_Logo(string xcodeProjectDir)
        {
#if UNITY_IPHONE
            var logo_name = "logo-white.png";
            File.Copy("Assets/ResourceForApp/logo/logo-white.png", $"{xcodeProjectDir}/{logo_name}");
            var projectFile = Path.Combine(xcodeProjectDir, "Unity-iPhone.xcodeproj", "project.pbxproj");
            var proj = new PBXProject();
            proj.ReadFromFile(projectFile);
            string target = proj.GetUnityMainTargetGuid();
			string guid = proj.AddFile(logo_name, logo_name, PBXSourceTree.Source);
			proj.AddFileToBuild(target, guid);
            proj.WriteToFile(projectFile);
#endif
        }

        private static SerializedObject LoadSerializedObject(string path)
        {
            var obj = AssetDatabase.LoadAllAssetsAtPath(path)[0];
            var serialized_obj = new SerializedObject(obj);
            return serialized_obj;
        }

        private static void Apply_Property_Int(SerializedObject obj, string propertyName, int intVal)
        {
            var prop = obj.FindProperty(propertyName);
            if (prop != null)
            {
                prop.intValue = intVal;
                obj.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogError($"property not found: {propertyName}");
            }
        }

        private static void Apply_Property_Float(SerializedObject obj, string propertyName, float floatVal)
        {
            var prop = obj.FindProperty(propertyName);
            if (prop != null)
            {
                prop.floatValue = floatVal;
                obj.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogError($"property not found: {propertyName}");
            }
        }

        private static void Apply_Property_String(SerializedObject obj, string propertyName, string strVal)
        {
            var prop = obj.FindProperty(propertyName);
            if (prop != null)
            {
                prop.stringValue = strVal;
                obj.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogError($"property not found: {propertyName}");
            }
        }

        public static void Apply_HandleHomeBar_Ios(string xcodePath)
        {
            PostProcessBuild_CustomHomeBar_AutoGen.OnPostprocessBuild(BuildTarget.iOS, xcodePath);
        }
    }
}