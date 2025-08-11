/**
 * @Author: handong.liu
 * @Date: 2021-02-02 14:35:11
 */
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using EL;
using CenturyGame.AppBuilder.Runtime.Exceptions;
using CenturyGame.AppBuilder.Runtime;
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

public static class AppBuilderUtility
{
    public static string GetPlatformStrForUpload(CenturyGame.AppBuilder.Editor.Builds.Contexts.AppBuildContext AppBuildContext)
    {
        #if UNITY_WEBGL
        return "webgl";
        #else
        return AppBuildContext.GetPlatformStrForUpload();
        #endif
    }
}

namespace GM7.AppBuilder.Editor.Builds.Actions.ResPack
{
    class MakeAppBaseVersionAction : BaseMakeVersionAction
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
        }

        public override void Execute(IFilter filter, IPipelineInput input)
        {
            Save(filter, input); 
            this.State = ActionState.Completed;
        }

        private bool Save(IFilter filter, IPipelineInput input)
        {
            var appBuildContext = AppBuildContext;
            appBuildContext.AppInfoManifest.version = AppBuildContext.GetTargetAppVersion(true).GetVersionString();
            var streamingPath = AppBuildContext.GetAssetsOutputPath();
            
            var resFileListPath = $"{streamingPath}/res_{AppBuilderUtility.GetPlatformStrForUpload(appBuildContext)}.json";
            appBuildContext.AppInfoManifest.unityDataResVersion = CommonEditorUtility.GetMD5(resFileListPath);

            var resDataFileListPath = $"{streamingPath}/res_data.json";
            appBuildContext.AppInfoManifest.dataResVersion = CommonEditorUtility.GetMD5(resDataFileListPath);

            var builtinAppInfoFilePath = appBuildContext.GetBuiltinAppInfoFilePath();
            var appInfoJson = appBuildContext.ToJson(appBuildContext.AppInfoManifest);
            File.WriteAllText(builtinAppInfoFilePath, appInfoJson,appBuildContext.TextEncoding);
            Logger.Info($"Save file \"{builtinAppInfoFilePath}\" completed");

            //保存编译信息
            var lastBuildInfo = appBuildContext.GetLastBuildInfo();
            if (lastBuildInfo == null) // 
            {
                lastBuildInfo = new LastBuildInfo();
            }
            lastBuildInfo.AddAppInfo(appBuildContext.AppInfoManifest,
                appBuildContext.AppInfoManifest);
            appBuildContext.SaveLastBuildInfo(lastBuildInfo);

            AssetDatabase.Refresh();
            return true;
        }

        #endregion
    }
}
