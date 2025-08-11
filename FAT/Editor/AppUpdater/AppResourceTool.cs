/**
 * @Author: handong.liu
 * @Date: 2021-02-01 20:44:13
 */
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using EL;
using CenturyGame.AppBuilder;
using CenturyGame.AppBuilder.Editor.Builds;
using CenturyGame.AppBuilder.Editor.Builds.Filters.Concrete;
using CenturyGame.AppBuilder.Editor.Builds.Actions.ResProcess;
using CenturyGame.AppBuilder.Editor.Builds.InnerLoggers;
using CenturyGame.AppUpdaterLib.Runtime;
using CenturyGame.LoggerModule.Runtime;
using CenturyGame.Core.Pipeline;
using CenturyGame.AppBuilder.Editor.Builds.PipelineInputs;


public static class AppResourceTool
{
    public static bool needProtect = false;
    [MenuItem("GM7/AppBuilder/Commands/制作并上传Patch")]
    public static bool MakePatchResouces()
    {
        string configPath = AppBuildConfig.GetAppBuildConfigInst().AppBuildConfigFolder + "/MakePatchVersion.yaml";
        var result = RunPipeline(configPath);

        if (result.State == ProcessState.Error)
        {
            Debug.LogError($"Build app failure , error message : {result.Message}");
            return false;
        }
        else
        {
            Debug.Log("Build app completed!");
            return true;
        }
    }

    [MenuItem("GM7/AppBuilder/Commands/制作一个基础版本")]
    public static bool MakeBaseVersionResources()
    {
        string configPath = AppBuildConfig.GetAppBuildConfigInst().AppBuildConfigFolder + "/MakeBaseVersion.yaml";

        var result = RunPipeline(configPath);

        if (result.State == ProcessState.Error)
        {
            Debug.LogError($"Build app failure , error message : {result.Message}");
            return false;
        }
        else
        {
            Debug.Log("Build app completed!");
            return true;
        }
    }

    [MenuItem("GM7/AppBuilder/Commands/拷贝现有streamingassets")]
    public static bool MakeVersionStreamingAssets()
    {
        string configPath = AppBuildConfig.GetAppBuildConfigInst().AppBuildConfigFolder + "/MakeStreamingAssets.yaml";

        var result = RunPipeline(configPath);

        if (result.State == ProcessState.Error)
        {
            Debug.LogError($"Build app failure , error message : {result.Message}");
            return false;
        }
        else
        {
            Debug.Log("Build app completed!");
            return true;
        }
    }

    [MenuItem("GM7/AppBuilder/Commands/构建非包内bundle")]
    public static bool MakeExternalBundle()
    {
        string configPath = AppBuildConfig.GetAppBuildConfigInst().AppBuildConfigFolder + "/MakeExternalBundle.yaml";
        var result = RunPipeline(configPath);

        if (result.State == ProcessState.Error)
        {
            Debug.LogError($"Build failed , error message : {result.Message}");
            return false;
        }
        else
        {
            Debug.Log("Build completed!");
            return true;
        }
    }

    public static void ExportResourceForCurrentSetting(bool upload)
    {
        var setting = VariantEditorUtility.LoadCurrentAppSetting();
        var versions = setting.version.Split('.');
        CenturyGame.AppBuilder.Editor.Builds.AppBuildConfig.GetAppBuildConfigInst().upLoadInfo.isUploadToRemote = upload;
        EditorUtility.SetDirty(CenturyGame.AppBuilder.Editor.Builds.AppBuildConfig.GetAppBuildConfigInst());
        AssetDatabase.Refresh();
        if(versions[2] == "0")
        {
            DebugEx.FormatInfo("_ExportResource ----> make base resources", setting.variant);
            AppResourceTool.MakeBaseVersionResources();
        }
        else
        {
            DebugEx.FormatInfo("_ExportResource ----> make patch resources", setting.variant);
            AppResourceTool.MakePatchResouces();
        }
    }


    static ProcessResult RunPipeline(string pipelineConfigPath)
    {
        if (string.IsNullOrEmpty(pipelineConfigPath))
        {
            return ProcessResult.Create(ProcessState.Error,$"The pipeline config that path is \"{pipelineConfigPath}\" is not found!");
        }

        LoggerManager.SetCurrentLoggerProvider(new AppBuilderLoggerProvider());

        var processor = AppBuilderPipelineProcessor.ReadFromBuildProcessConfig(pipelineConfigPath);

        AppBuildPipelineInput input = new AppBuildPipelineInput();

        return processor.Process(input);
    }

    [MenuItem("GM7/AppBuilder/Commands/清理编译缓存")]
    static void ClearBuildCacheFolder()
    {
        string dir = AppBuildConfig.GetAppBuildConfigInst().GetBuildCacheFolderPath();
        dir = CommonEditorUtility.OptimazeNativePath(dir);

        if (Directory.Exists(dir))
            Directory.Delete(dir,true);

        Debug.Log("Clear build cache success !");
    }

}