/**
 * @Author: handong.liu
 * @Date: 2021-02-02 14:52:22
 */
using UnityEngine;
using UnityEditor;
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
using CenturyGame.AppBuilder.Editor.Builds.Actions.ResPack;
using CenturyGame.AppBuilder.Editor.Builds.BuildInfos;
#if DEBUG_FILE_CRYPTION
using File = CenturyGame.Core.IO.File;
#else
using File = System.IO.File;
#endif

namespace GM7.AppBuilder.Editor.Builds.Actions.ResPack
{
    public class MakeAppPatchVersionAction : BaseMakeVersionAction
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
            return true;
            var lastBuildInfo = AppBuildContext.GetLastBuildInfo();
            if (lastBuildInfo == null || lastBuildInfo.GetCurrentBuildInfo() == null) 
            {
                AppBuildContext.AppendErrorLog("You can't make patch version , because you has no last build info .");
                return false;
            }
            return true;
        }

        public override void Execute(IFilter filter, IPipelineInput input)
        {
            this.Save(filter, input);
            this.State = ActionState.Completed;
        }

        private bool Save(IFilter filter, IPipelineInput input)
        {
            var appBuildContext = AppBuildContext;
            var streamingPath = AppBuildContext.GetAssetsOutputPath();

            appBuildContext.AppInfoManifest.version = AppBuildContext.GetTargetAppVersion().GetVersionString();
            
            var resFileListPath = $"{streamingPath}/res_{AppBuilderUtility.GetPlatformStrForUpload(appBuildContext)}.json";
            appBuildContext.AppInfoManifest.unityDataResVersion = CommonEditorUtility.GetMD5(resFileListPath);

            var resDataFileListPath = $"{streamingPath}/res_data.json";
            appBuildContext.AppInfoManifest.dataResVersion = CommonEditorUtility.GetMD5(resDataFileListPath);

            var builtinAppInfoFilePath = appBuildContext.GetBuiltinAppInfoFilePath();
            var appInfoJson = appBuildContext.ToJson(appBuildContext.AppInfoManifest);

            //保存AppInfo文件
            File.WriteAllText(builtinAppInfoFilePath, appInfoJson, appBuildContext.TextEncoding);
            Logger.Info($"Save file \"{builtinAppInfoFilePath}\" completed");

            //保存编译信息
            var lastBuildInfo = appBuildContext.GetLastBuildInfo();
            if (lastBuildInfo == null) // 
            {
                lastBuildInfo = new LastBuildInfo();
            }
            lastBuildInfo.AddAppInfo(null, appBuildContext.AppInfoManifest);
            appBuildContext.SaveLastBuildInfo(lastBuildInfo);

            AssetDatabase.Refresh();
            return true;
        }

        #endregion
    }
}
