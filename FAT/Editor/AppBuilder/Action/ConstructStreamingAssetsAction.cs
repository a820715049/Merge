/**
 * @Author: handong.liu
 * @Date: 2021-02-02 14:25:42
 */
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using EL;
using CenturyGame.AppBuilder.Runtime.Exceptions;
using CenturyGame.Core.Pipeline;
using CenturyGame.AppBuilder.Editor;
using Version = CenturyGame.AppUpdaterLib.Runtime.Version;
using CenturyGame.AppBuilder;
using CenturyGame.AppBuilder.Editor.Builds;
using CenturyGame.AppBuilder.Editor.Builds.Actions;
using CenturyGame.AppBuilder.Editor.Builds.Filters.Concrete;
using CenturyGame.AppBuilder.Editor.Builds.Actions.ResProcess;
using CenturyGame.AppBuilder.Editor.Builds.InnerLoggers;
using CenturyGame.AppUpdaterLib.Runtime;
using CenturyGame.LoggerModule.Runtime;
using CenturyGame.AppBuilder.Editor.Builds.PipelineInputs;

namespace GM7.AppBuilder.Editor.Builds.Actions.ResPack
{
    class ConstructStreamingAssetsAction : BaseBuildFilterAction
    {
        //--------------------------------------------------------------
        #region Fields
        //--------------------------------------------------------------

        #endregion


        //--------------------------------------------------------------
        #region Properties & Events
        //--------------------------------------------------------------

        #endregion


        //--------------------------------------------------------------
        #region Creation & Cleanup
        //--------------------------------------------------------------

        #endregion


        //--------------------------------------------------------------
        #region Methods
        //--------------------------------------------------------------


        private string GetUploadDir()
        {
            return AppBuildContext.GetResStoragePath();
        }

        public override bool Test(IFilter filter, IPipelineInput input)
        {


            return true;
        }

        public override void Execute(IFilter filter, IPipelineInput input)
        {
            CopyAssetsToStreamingAssetsPath();

            this.State = ActionState.Completed;
        }


        private void CopyAssetsToStreamingAssetsPath()
        {
            CopyGameResources();

            CopyUnityResFileList();

            CopyDataResFileList();

            CopyDreamDetectiveResources();

            CopyPackageResource();

            Logger.Info("Copy game resources to streaming assets path !");
        }

        private void CopyDreamDetectiveResources()
        {
#if FIND_GAME
            var resStorage = AppBuildContext.GetResStoragePath();
            PackageUtil.ProcessDreamDetectivePackageResource(resStorage);
#endif
        }

        private void CopyPackageResource()
        {
            var streamingPath = System.IO.Path.GetFullPath(AppBuildContext.GetAssetsOutputPath());
            var dirs = System.IO.Directory.GetDirectories(Application.dataPath, "StreamingAssetsFrag", SearchOption.AllDirectories);
            foreach(var dir in dirs)
            {
                DebugEx.FormatInfo("ConstructStreamingAssets ----> copy dir {0}", dir);
                CommonEditorUtility.CopyDirectoryContent(dir, streamingPath);
            }
        }


        private void CopyGameResources()
        {
            var streamingPath = AppBuildContext.GetAssetsOutputPath();
            // if (Directory.Exists(streamingPath))
            // {
            //     Directory.Delete(streamingPath, true);
            // }
            //AssetDatabase.Refresh();
            //Directory.CreateDirectory(streamingPath);

            var resStorage = AppBuildContext.GetResStoragePath();
            DirectoryInfo dirInfo = new DirectoryInfo(resStorage);
            FileInfo[] fileInfos = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
            Logger.Info("Start copy game resources to streaming assets path !");
            foreach (var fileInfo in fileInfos)
            {
                if(string.IsNullOrEmpty(System.IO.Path.GetExtension(fileInfo.FullName)))    //梦境侦探的bundle没有扩展名，在之后的调用处理
                {
                    continue;
                }
                string sourcePath = CommonEditorUtility.OptimazeNativePath(fileInfo.FullName);
                if (sourcePath.Contains(AppBuildContext.GenCodePattern))
                {
                    continue;
                }

                if (sourcePath.Contains("protokitgo.yaml"))
                {
                    continue;
                }

                if (sourcePath.Contains("resource_versions.release"))
                {
                    continue;
                }

                string targePath = $"{streamingPath}{sourcePath.Replace(resStorage, "")}";

                string dirName = Path.GetDirectoryName(targePath);

                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                File.Copy(sourcePath, targePath, true);
            }
            Logger.Info("Copy game resources  completed!");
        }

        private void CopyUnityResFileList()
        {
            var streamingPath = AppBuildContext.GetAssetsOutputPath();
            
            var platformName = string.Empty;
#if UNITY_EDITOR && UNITY_ANDROID
            platformName = "android";
#elif UNITY_EDITOR && UNITY_IPHONE
            platformName = "ios";
#elif UNITY_EDITOR && UNITY_WEBGL
            platformName = "webgl";
#else
            throw new System.InvalidOperationException($"Unsupport build platform : {EditorUserBuildSettings.activeBuildTarget} .");
#endif
         
            var configRepoPath = AppBuilderEditorUtility.GetAppBuildConfigAbsolutePath();

            var resListPath = $"{configRepoPath}/gen/rawdata/version_list/res_{platformName}.json";

            if (!File.Exists(resListPath))
            {
                throw new FileNotFoundException($"The file path is \"{resListPath}\" .");
            }

            Logger.Info("Start copy resource list file to streaming assets path !");
            //var contents = File.ReadAllText(resListPath,AppBuildContext.TextEncoding);
            //var removeStr = $"{AppBuildConfig.GetAppBuildConfigInst().upLoadInfo.remoteDir}/resource/";
            //contents = contents.Replace(removeStr, "");
            //File.WriteAllText($"{streamingPath}/res_{platformName}.json", 
            //    contents,AppBuildContext.TextEncoding);
            File.Copy(resListPath, $"{streamingPath}/res_{platformName}.json");
            Logger.Info("copC resource list file completed!");
            AssetDatabase.Refresh();
        }

        private void CopyDataResFileList()
        {
            var streamingPath = AppBuildContext.GetAssetsOutputPath();
            var configRepoPath = AppBuilderEditorUtility.GetAppBuildConfigAbsolutePath();

            var dataResListPath = $"{configRepoPath}/gen/rawdata/version_list/res_data.json";

            if (!File.Exists(dataResListPath))
            {
                throw new FileNotFoundException($"The file path is \"{dataResListPath}\" .");
            }

            Logger.Info("Start copy data resource list file to streaming assets path !");
            File.Copy(dataResListPath, $"{streamingPath}/res_data.json");
            Logger.Info("copC data resource list file completed!");
            AssetDatabase.Refresh();
           
        }

        #endregion
    }
}