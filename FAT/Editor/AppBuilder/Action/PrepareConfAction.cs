using UnityEngine;
using CenturyGame.AppBuilder.Editor.Builds.Actions;
using CenturyGame.Core.Pipeline;
using System.IO;

namespace GM7.AppBuilder.Editor.Builds.Actions.AppPrepare
{
    public class PrepareConfAction : BaseBuildFilterAction {
        public override bool Test(IFilter filter, IPipelineInput input) {
            return true;
        }

        public override void Execute(IFilter filter, IPipelineInput input) {
            // var abDir = Path.Combine(AppBuildContext.Temporary, AppBuildContext.AbExportFolder);
            // if (ModeHelper.RebuildBundle) {
            //     CommonEditorUtility.ClearDirectory(abDir);
            // }
            CommonEditorUtility.ClearDirectory(Application.streamingAssetsPath);

            _EnsureConfBranch();

            AppBuilderEditorUtility.SyncConf(true, false, false);
            //VersionBuildRule.ResetDataVersion();

            State = ActionState.Completed;
        }

        private void _EnsureConfBranch()
        {
            const string ConfRepoPath = "../fatr_conf";
            var branch = VariantEditorUtility.LoadCurrentAppSetting().builderConfig.branch;
            CommonEditorUtility.StartProcess("git", $"reset --hard", ConfRepoPath);
            CommonEditorUtility.StartProcess("git", $"clean -fd", ConfRepoPath);
            CommonEditorUtility.StartProcess("git", $"fetch origin {branch}", ConfRepoPath);
            CommonEditorUtility.StartProcess("git", $"checkout {branch}", ConfRepoPath);
            CommonEditorUtility.StartProcess("git", $"reset --hard origin/{branch}", ConfRepoPath);
        }
    }
}