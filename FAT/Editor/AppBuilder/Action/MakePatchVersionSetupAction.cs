/**
 * @Author: handong.liu
 * @Date: 2021-02-02 14:51:05
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
    class MakePatchVerionSetupAction : BaseBuildFilterAction
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
            var versionStr = $"{appVersion.Major}.{appVersion.Minor}.{appVersion.Patch}";
            var targetVersion = new Version(versionStr);
            if (!AppBuildConfig.GetAppBuildConfigInst().incrementRevisionNumberForPatchBuild && (targetVersion.PatchNum== 0))
            {
                Logger.Error($"In patch build mode and not be auto increment revision number , the patch value can not be zero , target version is \"{targetVersion.GetVersionString()}\".");
                return false;
            }

            var lastVersionInfo = AppBuildContext.GetLastBuildInfo();
            if (lastVersionInfo?.GetCurrentBuildInfo() != null)
            {
                var buildInfo = lastVersionInfo.GetCurrentBuildInfo();
                var lastVersion = new Version(buildInfo.versionInfo.version);
                Logger.Info($"The last app version :  {lastVersion.GetVersionString()} .");
                var result = targetVersion.CompareTo(lastVersion);

                //首先，目标版本必须和上一次build的版本处在同一个大版本上
                if (result > Version.VersionCompareResult.LowerForMinor && result < Version.VersionCompareResult.HigherForMinor)
                {
                    //如果不是自增Patch的build，那么目标版本的Patch必须大于上一次build的Patch
                    if (!AppBuildConfig.GetAppBuildConfigInst().incrementRevisionNumberForPatchBuild &&
                        result < Version.VersionCompareResult.HigherForPatch)
                    {
                        Logger.Error($"Version is invalid , targetVersion : {targetVersion.GetVersionString()} " +
                                     $"lastBuildVersion : {lastVersion.GetVersionString()} .");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Logger.Error($"No last build info , You can't make app patch build !");
                return false;
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
            this.Setup(filter, input);
            this.State = ActionState.Completed;
        }

        private void Setup(IFilter filter, IPipelineInput input)
        {
            input.SetData(EnvironmentVariables.MAKE_BASE_APP_VERSION_KEY, false);
        }


        #endregion

    }
}