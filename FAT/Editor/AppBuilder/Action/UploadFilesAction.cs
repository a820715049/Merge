/**
 * @Author: handong.liu
 * @Date: 2021-02-02 12:07:20
 */
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Diagnostics;
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
    class UploadFilesAction : BaseBuildFilterAction
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

        public override bool Test(IFilter filter, IPipelineInput input)
        {
            string pythonScripPath = $"{Application.dataPath}/../Tools/ProtokitUpload/ProtokitGoUploader.py";

            pythonScripPath = CommonEditorUtility.OptimazeNativePath(pythonScripPath);
            Logger.Info($"Lua projecet root path : {pythonScripPath} .");

            if (!File.Exists(pythonScripPath))
            {
                AppBuildContext.ErrorSb.AppendLine($"The target upload script that path is \"{pythonScripPath}\" is not exist!");
                return false;
            }
            
            return true;
        }

        public override void Execute(IFilter filter, IPipelineInput input)
        {
            var resStorage = AppBuildContext.GetResStoragePath();

            Logger.Info($"Start upload files , path is \" {resStorage}\" .");

            var result = this.UploadFiles(resStorage,input);
            if (result)
            {
                this.State = ActionState.Completed;
            }
            else
            {
                this.State = ActionState.Error;
            }
        }

        private bool UploadFiles(string sourceFolder, IPipelineInput input)
        {
            string pythonScripPath = $"{Application.dataPath}/../Tools/ProtokitUpload/ProtokitGoUploader.py";
            
            pythonScripPath = CommonEditorUtility.OptimazeNativePath(pythonScripPath);
            Logger.Info($"Lua projecet root path : {pythonScripPath} .");

            var appBuildConfig = AppBuildConfig.GetAppBuildConfigInst();
            var configRepoPath = System.IO.Path.GetFullPath(AppBuilderEditorUtility.GetAppBuildConfigAbsolutePath());
            if (!Directory.Exists(configRepoPath))
            {
                throw new DirectoryNotFoundException(configRepoPath);
            }

            var protokitgoConfigName = appBuildConfig.upLoadInfo.protokitgoConfigName;

            var uploadFolder = sourceFolder;

            string platformName = string.Empty;
#if UNITY_EDITOR && UNITY_ANDROID
            platformName = "android";
#elif UNITY_EDITOR && UNITY_IPHONE
            platformName = "ios";
#elif UNITY_EDITOR && UNITY_WEBGL
            platformName = "webgl";
#else
            throw new System.InvalidOperationException($"Unsupport build platform : {EditorUserBuildSettings.activeBuildTarget} .");
#endif

            var uploadInfo = AppBuildConfig.GetAppBuildConfigInst().upLoadInfo;
            var uploadFilesPattern = uploadInfo.uploadFilesPattern;

            var makeBaseVersion = input.GetData(EnvironmentVariables.MAKE_BASE_APP_VERSION_KEY, false);
            var appVersion = AppBuildContext.GetTargetAppVersion(makeBaseVersion);
            
            var resVersion = appVersion.Patch;

            var noUpload = "false";
            if (uploadInfo.isUploadToRemote)
                noUpload = "false";
            else
                noUpload = "true";

            string remoteDir = uploadInfo.remoteDir; 
            if (string.IsNullOrEmpty(remoteDir) || string.IsNullOrWhiteSpace(remoteDir))
            {
                remoteDir = "**NOROOT**";
            }
            string confRepoBranch = VariantEditorUtility.LoadCurrentAppSetting().builderConfig.branch;

            string commandLineArgs =
                $"{pythonScripPath} {configRepoPath} {protokitgoConfigName} {platformName} {uploadFilesPattern} {uploadFolder} {remoteDir} {appVersion.Major}.{appVersion.Minor} {resVersion} {noUpload} {confRepoBranch}";

            
            UnityEngine.Debug.Log($"commandline args : {commandLineArgs}");

            var pStartInfo = new ProcessStartInfo();

#if UNITY_EDITOR_WIN
            if (uploadInfo.pythonType == FilesUpLoadInfo.PythonType.Python)
            {
                pStartInfo.FileName = @"python.exe";
            }
            else
            {
                pStartInfo.FileName = @"python3.exe";
            }
#elif UNITY_EDITOR_OSX
            // if (uploadInfo.pythonType == FilesUpLoadInfo.PythonType.Python)
            // {
            //     pStartInfo.FileName = @"python";
            // }
            // else
            {
                pStartInfo.FileName = @"python3";
            }
#else
        throw new System.InvalidOperationException($"Unsupport build platform : {EditorUserBuildSettings.activeBuildTarget} .");
#endif


            pStartInfo.UseShellExecute = false;

            pStartInfo.RedirectStandardInput = true;
            pStartInfo.RedirectStandardOutput = true;
            pStartInfo.RedirectStandardError = true;
            var workDir = Path.GetDirectoryName(pythonScripPath);
            workDir = CommonEditorUtility.OptimazeNativePath(workDir);
            pStartInfo.WorkingDirectory = workDir;

            pStartInfo.CreateNoWindow = true;
            pStartInfo.WindowStyle = ProcessWindowStyle.Normal;
            pStartInfo.Arguments = commandLineArgs;

            pStartInfo.StandardErrorEncoding = System.Text.UTF8Encoding.UTF8;
            pStartInfo.StandardOutputEncoding = System.Text.UTF8Encoding.UTF8;

            var proces = Process.Start(pStartInfo);
            proces.ErrorDataReceived += (s, e) =>
            {
                Logger.Info(e.Data);
            };
            proces.OutputDataReceived += (s, e) =>
            {
                Logger.Debug(e.Data);
            };
            proces.BeginOutputReadLine();
            proces.BeginErrorReadLine();
            proces.WaitForExit();
            var exitCode = proces.ExitCode;
            if (exitCode != 0)
            {
                AppBuildContext.AppendErrorLog($"Exit code : {proces.ExitCode}!");
                Logger.Error($"Exit code : {proces.ExitCode}!");
            }
            else
            {
                Logger.Debug("Upload files successful!");
            }
            proces.Close();

            AssetDatabase.Refresh();

            return exitCode == 0;
        }


        #endregion

    }
}
