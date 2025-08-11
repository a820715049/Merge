/**
 * @Author: handong.liu
 * @Date: 2021-02-01 21:31:05
 */

using System.IO;
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

namespace GM7.AppBuilder.Editor.Builds.Actions.AppPrepare
{
    class MakeBaseVerionSetupAction : BaseBuildFilterAction
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


        private bool CheckAppVersionValid()
        {
            return true;
            var appVersion = AppBuildConfig.GetAppBuildConfigInst().targetAppVersion;
            Logger.Info($"The version from build config is \"{appVersion.Major}.{appVersion.Minor}.{appVersion.Patch}\" .");
            
            var versionStr = $"{appVersion.Major}.{appVersion.Minor}.0";

            var targetVersion = new Version(versionStr);

            var lastVersionInfo = AppBuildContext.GetLastBuildInfo();
            if (lastVersionInfo != null && lastVersionInfo.GetCurrentBuildInfo() != null)
            {
                var buildInfo = lastVersionInfo.GetCurrentBuildInfo();
                var lastVersion = new Version(buildInfo.versionInfo.version);
                Logger.Info($"The last app version :  {lastVersion.GetVersionString()} .");

                var result = targetVersion.CompareTo(lastVersion);

                if (result < Version.VersionCompareResult.HigherForMinor)//次版本（Minor）本次必须一样或更高
                {
                    Logger.Error($"The target version that value is \"{appVersion.Major}.{appVersion.Minor}.0\" " +
                                 $"is lower or equal to last build ,last build verison is \"" +
                                 $"{lastVersion.GetVersionString()}\" .");
                    return false;
                }
            }
            else
            {
                Logger.Info($"The last build info is not exist .");
            }
            return true;
        }


        private bool CheckGameConfigs()
        {
            var configsPath = AppBuilderEditorUtility.GetAppBuildConfigAbsolutePath();
            if (!Directory.Exists(configsPath))
            {
                Logger.Error($"The table config git repo that path is \"{configsPath}\" is not exist !" +
                             $" Pleause specify a valid path!");
                return false;
            }

            return true;
        }

        public override bool Test(IFilter filter, IPipelineInput input)
        {
            if (!CheckAppVersionValid())
            {
                var appVersion = AppBuildConfig.GetAppBuildConfigInst().targetAppVersion;
                var versionStr = $"{appVersion.Major}.{appVersion.Minor}.{appVersion.Patch}";
                AppBuildContext.AppendErrorLog($"Invalid target version : {versionStr}.");
                return false;
            }

            if (!CheckGameConfigs())         //not check game config
            {
                return false;
            }

            return true;
        }

        public override void Execute(IFilter filter, IPipelineInput input)
        {
            this.Setup(filter,input);
            this.State = ActionState.Completed;
        }


        private void Setup(IFilter filter, IPipelineInput input)
        {
            input.SetData(EnvironmentVariables.MAKE_BASE_APP_VERSION_KEY, true);
        }

        #endregion

    }
}