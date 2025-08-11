/*
 * @Author: qun.chao
 * @Date: 2024-01-15 17:18:14
 */
using UnityEditor;
using System.IO;
using CenturyGame.Core.Pipeline;
using CenturyGame.AppBuilder.Editor.Builds.Actions;

namespace GM7.AppBuilder.Editor.Builds.Actions.ResProcess
{
    public class BuildExternalResAction : BaseBuildFilterAction
    {
        public override bool Test(IFilter filter, IPipelineInput input)
        {
            return true;
        }

        public override void Execute(IFilter filter, IPipelineInput input)
        {
            // external可能依赖包内资源
            // 全量打包 生成包含external的资源依赖
            AssetBundleBuilder.BuildAllAssetBundles(EditorUserBuildSettings.activeBuildTarget, GetOutputAssetbundleFolder());
            this.State = ActionState.Completed;
        }

        private string GetOutputAssetbundleFolder()
        {
            string result = string.Concat(System.Environment.CurrentDirectory,
                Path.DirectorySeparatorChar,
                AppBuildContext.Temporary,
                Path.DirectorySeparatorChar,
                AppBuildContext.AbExportFolder);
            return CommonEditorUtility.OptimazeNativePath(result);
        }
    }
}