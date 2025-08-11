/**
 * @Author: handong.liu
 * @Date: 2021-02-02 10:40:13
 */
using UnityEngine;
using UnityEditor;
using System.IO;
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
using System.Text.RegularExpressions;

namespace GM7.AppBuilder.Editor.Builds.Actions.ResProcess
{
    public class AssetBundleAction : BaseBuildFilterAction
    {
        private ExternalResConfig externalResConfig;

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

        #endregion

        public override bool Test(IFilter filter, IPipelineInput input)
        {
            /*
            string configPath = GetCurrentAssetGraphProjectConfigPath();

            Logger.Info($"AssetsGraph config path : \"{configPath}\" .");

            if (!File.Exists(configPath))
            {
                var appBuildContext = AppBuildContext;
                appBuildContext.ErrorSb.AppendLine($"The assetgraph config that path is \"{configPath}\" is not exist!");
                return false;
            }*/

            //var appSetting = Resources.Load<AppSetting>("AppSetting");
            //if (appSetting == null)
            //{
            //    var appBuildContext = AppBuildContext;
            //    appBuildContext.AppendErrorLog($"Pleause create a appSetting asset !");
            //    return false;
            //}
            return true;
        }

        public override void Execute(IFilter filter, IPipelineInput input)
        {
            /*
            string configPath = GetCurrentAssetGraphProjectConfigPath(true);
            Logger.Info($"AssetGraph configPath : \"{configPath}\" .");
            UnityEngine.AssetGraph.AssetGraphUtility.ExecuteGraph(configPath);*/

            externalResConfig ??= ExternalResConfig.GetConfigInst();
            // 打包时需要排除external资源
            AssetBundleBuilder.BuildAllAssetBundles(EditorUserBuildSettings.activeBuildTarget, GetOutputAssetbundleFolder(), CheckIsInnerBundle);
            this.State = ActionState.Completed;
        }


        private string GetCurrentAssetGraphProjectConfigPath(bool relative = false)
        {
            string targetConfigPath = AppBuildConfig.GetAppBuildConfigInst().TargetAssetGraphConfigAssetsPath;
            targetConfigPath = CommonEditorUtility.OptimazeNativePath(targetConfigPath, false);

            string filePath;
            if (relative)
            {
                filePath = targetConfigPath;
            }
            else
            {
                filePath = $"{Application.dataPath}/../{targetConfigPath}";
                filePath = CommonEditorUtility.OptimazeNativePath(filePath);
            }

            return filePath;
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

        private bool CheckIsInnerBundle(string bundle)
        {
            if (bundle.EndsWith(".ab"))
            {
                return !FilterForExternalRes(bundle.Replace(".ab", string.Empty));
            }
            else
            {
                return !FilterForExternalRes(bundle);
            }
        }

        // 筛选external标记的bundle
        private bool FilterForExternalRes(string bundle)
        {
            var cfg = externalResConfig;
            foreach (var name in cfg.bundle_name)
            {
                if (name.Equals(bundle))
                {
                    Debug.Log($"AssetBundleAction ----> exclude bundle {bundle} by name {name}");
                    return true;
                }
            }
            foreach (var pattern in externalResConfig.bundle_pattern)
            {
                if (Regex.IsMatch(bundle, pattern))
                {
                    Debug.Log($"AssetBundleAction ----> exclude bundle {bundle} by pattern {pattern}");
                    return true;
                }
            }
            return false;
        }
    }
}
