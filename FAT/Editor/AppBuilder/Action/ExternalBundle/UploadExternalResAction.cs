/*
 * @Author: qun.chao
 * @Date: 2024-01-15 17:25:12
 */
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using CenturyGame.Core.Pipeline;
using CenturyGame.AppBuilder.Editor;
using CenturyGame.AppBuilder.Editor.Builds;
using CenturyGame.AppBuilder.Editor.Builds.Actions;

namespace GM7.AppBuilder.Editor.Builds.Actions.ResPack
{
    public class UploadExternalResAction : BaseBuildFilterAction
    {
        //--------------------------------------------------------------
        #region Fields
        //--------------------------------------------------------------

        #endregion

        //--------------------------------------------------------------
        #region Properties & Events
        //--------------------------------------------------------------

        private string script_path = $"{Application.dataPath}/../Tools/ProtokitUpload/ProtokitGoUploaderForExternalBundle.py";

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
            string pythonScripPath = script_path;

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
            string pythonScripPath = script_path;
            
            pythonScripPath = CommonEditorUtility.OptimazeNativePath(pythonScripPath);
            Logger.Info($"script path : {pythonScripPath}.");

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
            throw new System.InvalidOperationException($"Unsupport build platform : {EditorUserBuildSettings.activeBuildTarget}.");
#endif

            var uploadInfo = AppBuildConfig.GetAppBuildConfigInst().upLoadInfo;
            var uploadFilesPattern = uploadInfo.uploadFilesPattern;
            var resListFileName = $"res_{platformName}_external.x";

            string confRepoBranch = VariantEditorUtility.LoadCurrentAppSetting().builderConfig.branch;
            string commandLineArgs =
                $"{pythonScripPath} {uploadFolder} {uploadFilesPattern} {configRepoPath} {confRepoBranch} {resListFileName}";
            
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
            pStartInfo.FileName = @"python3";
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