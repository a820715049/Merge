/*
 * @Author: qun.chao
 * @Date: 2023-05-22 12:16:43
 */
using UnityEngine;
using UnityEditor;
using BuildWrapper;
using centurygame.Internal;

public static class VersionBuilder
{
    [MenuItem("Tools/Build/Build Version")]
    public static void Build()
    {
        CGSettingsEditor.InnerSync();
#if UNITY_IOS
        MakeIOS();
#elif UNITY_ANDROID
        MakeAndroid();
#endif
    }

    [MenuItem("Tools/Build/Build External Bundle")]
    public static void BuildExternalBundle()
    {
        Utility.BuildExternalResource();
    }

    [MenuItem("Tools/Build/Rebuild Bundle")]
    public static void BuildBundle()
    {
        Utility.BuildResource(false, false, false);
    }

    [MenuItem("Tools/Build/Rebuild Bundle Local")]
    public static void BuildBundleLocal()
    {
        AssetBundleBuilder.BuildAllAssetBundles(EditorUserBuildSettings.activeBuildTarget, "Assets/StreamingAssets");
    }

    public static void MakeIOS()
    {
        var args = BuildArgs.Create();
        var setting = VariantEditorUtility.LoadCurrentAppSetting();
        string xcodeProjectDir = string.Format("../{0}.ios", setting.GetFileNameHint());

        // sandbox / prod
        CGSettings.Environment = setting.sdkEnv == AppSettings.SDKEnvironment.Prod;

        // assetbundle
        Utility.BuildResource(args.isUpload, args.encryptRes, !args.isUpload);

        // LaunchScreen
        Utility.Apply_LaunchScreen_IOS();

        string[] levels = new string[] { "Assets/Scenes/Main.unity" };
        Utility.BuildXcode(xcodeProjectDir, args.profilerType, setting, levels);

        // LaunchScreen
        Utility.Apply_LaunchScreen_IOS_Logo(xcodeProjectDir);

        Utility.Apply_XcodeProjectSigning(xcodeProjectDir, setting);

        // Utility.Apply_XcodeProjectSigning_PushNotification(xcodeProjectDir, setting);

        // Utility.Apply_XcodeProductName(xcodeProjectDir, setting);

        // app名称多语言插件会添加localization设置 此处无需自行添加了
        // Utility.Apply_Localization_Asia(xcodeProjectDir);
        
        Utility.Apply_Embed_Swift_No(xcodeProjectDir);

        Utility.Apply_TestFlightComplianceInformation(xcodeProjectDir);

        Utility.Apply_TryFixCrashFor_15_5(xcodeProjectDir);

        Utility.Apply_HandleHomeBar_Ios(xcodeProjectDir);

        // TODO: fakebitcode 和 payment魔改 移到外部脚本处理
        if (args.encryptApp)
            Utility.Apply_FakeBitCode(xcodeProjectDir);
        if (args.buildNoAppStore)
            Utility.Apply_NoAppStore(xcodeProjectDir);
    }

    public static void MakeAndroid()
    {
        var args = BuildArgs.Create();
        var setting = VariantEditorUtility.LoadCurrentAppSetting();

        // sandbox / prod
        CGSettings.Environment = setting.sdkEnv == AppSettings.SDKEnvironment.Prod;

        // assetbundle
        Utility.BuildResource(args.isUpload, args.encryptRes, !args.isUpload);

        // 需要包含签名
        // keystore
        Utility.Apply_AndroidSigningConfig(setting);

        // LaunchScreen
        Utility.Apply_LaunchScreen_Android();

        // 非通用的必要设置
        Utility.Apply_ExportAsGoogleAndroidProject(true, out var prevExportGradle);
        Utility.Apply_ExportForAppBundle(true, out var prevAAB);
        Utility.Apply_SplitApplicationBinary(true, out var prevSplit);

        // export
        string exportPath = string.Format("../{0}.gradle", setting.GetFileNameHint());
        string[] levels = new string[] { "Assets/Scenes/Main.unity" };
        Utility.BuildAndroid(exportPath, args.profilerType, setting, levels);

        // 还原设置
        Utility.Apply_ExportAsGoogleAndroidProject(prevExportGradle, out var _);
        Utility.Apply_ExportForAppBundle(prevAAB, out var _);
        Utility.Apply_SplitApplicationBinary(prevSplit, out var _);
    }
}