/*
 * @Author: qun.chao
 * @Date: 2024-01-16 16:58:04
 */
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using EL;
using CenturyGame.Core.Pipeline;
using CenturyGame.AppBuilder.Editor.Builds.Actions;
using CenturyGame.AssetBundleManager.Runtime;
using System.Text.RegularExpressions;

namespace GM7.AppBuilder.Editor.Builds.Actions.ResPack
{
    class CopyExternalBundleListAction : BaseBuildFilterAction
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
            Logger.Info($"尝试同步外部资源列表到待上传路径");

            CopyResList();

            this.State = ActionState.Completed;
        }

        private void CopyResList()
        {
            var resStorage = AppBuildContext.GetResStoragePath();
            if (!Directory.Exists(resStorage))
            {
                DebugEx.Error($"CopyResList res folder not exists {resStorage}");
                return;
            }

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

            var fileName = $"res_{platformName}_external.x";
            // 分支已通过 PrepareConfAction 设置了正确状态
            var configRepoPath = System.IO.Path.GetFullPath(AppBuilderEditorUtility.GetAppBuildConfigAbsolutePath());
            var srcPath = $"{configRepoPath}/gen/rawdata/version_list/{fileName}";
            var dstPath = $"{resStorage}/{fileName}";

            if (!File.Exists(srcPath))
            {
                DebugEx.Info($"CopyResList external res not found {srcPath}");
                return;
            }

            File.Copy(srcPath, dstPath, true);
        }

        #endregion
    }
}