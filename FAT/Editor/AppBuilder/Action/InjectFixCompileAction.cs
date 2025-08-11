/**
 * @Author: handong.liu
 * @Date: 2021-06-11 18:54:36
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
    class InjectFixCompileAction : BaseBuildFilterAction
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
            IFix.Editor.IFixEditor.Platform platform = IFix.Editor.IFixEditor.Platform.android;
            var platformStr = AppBuilderUtility.GetPlatformStrForUpload(AppBuildContext);
            if(platformStr == "ios")
            {
                platform = IFix.Editor.IFixEditor.Platform.ios;
            }
            else if(platformStr == "webgl")
            {
                platform = IFix.Editor.IFixEditor.Platform.webgl;
            }
            else
            {
                platform = IFix.Editor.IFixEditor.Platform.android;
            }
            var targetDir = System.IO.Path.Combine(AppBuildContext.GetResStoragePath(), "hotfix");
            DebugEx.FormatInfo("InjectFixCompileAction::Execute ----> for platform {0} to dir {1}", platform, targetDir);
            System.IO.Directory.CreateDirectory(targetDir);
            try
            {
                IFix.Editor.IFixEditor.GenPlatformPatch(platform, targetDir + System.IO.Path.DirectorySeparatorChar);
                this.State = ActionState.Completed;
            }
            catch(System.Exception e)
            {
                UnityEngine.Debug.LogError(e);
                this.State = ActionState.Error;
            }
        }

        #endregion
    }
}