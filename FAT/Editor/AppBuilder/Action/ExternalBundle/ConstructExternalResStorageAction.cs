/*
 * @Author: qun.chao
 * @Date: 2024-01-15 18:21:32
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
    class ConstructExternalResStorageAction : BaseBuildFilterAction
    {
        //--------------------------------------------------------------
        #region Fields
        //--------------------------------------------------------------
        private const string ResExt = "ab";
        private ExternalResConfig externalResConfig;
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
            Logger.Info($"Start construct the files that needed to upload !");

            this.ConstructUploadRes(filter,input);

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

        private string GetAssetbundleManifestFilePath()
        {
            var abDir = GetOutputAssetbundleFolder();
            var manifestPath = string.Concat(abDir,
                Path.DirectorySeparatorChar,
                AppBuildContext.AbExportFolder);
            return CommonEditorUtility.OptimazeNativePath(manifestPath);
        }

        private void ConstructUploadRes(IFilter filter, IPipelineInput input)
        {
            var resStorage = CreateStorageDir();

            CopyAssetBundlesToUploadStorage(resStorage);
        }


        private string CreateStorageDir()
        {
            var resStorage = AppBuildContext.GetResStoragePath();
            if (Directory.Exists(resStorage))
            {
                Directory.Delete(resStorage, true);
            }

            AssetDatabase.Refresh();
            Directory.CreateDirectory(resStorage);
            return resStorage;
        }

        private void CopyAssetBundlesToUploadStorage(string resStorage)
        {
            var abDir = GetOutputAssetbundleFolder();
            var manifestPath = GetAssetbundleManifestFilePath();
            Logger.Info($"The assetbundle manifest path : {manifestPath}!");
            byte[] bytes = File.ReadAllBytes(manifestPath);
            var mainAb = AssetBundle.LoadFromMemory(bytes);
            var mainAbManifest = mainAb.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            mainAb.Unload(false);

            // manifest是基于全量资源的打包结果
            var abNames = mainAbManifest.GetAllAssetBundles();

            // ===== analysis =====
            FAT.BundleAnalysis.ClearRecord();
            // ====================

            // ensure config
            externalResConfig ??= ExternalResConfig.GetConfigInst();

            List<ABTableItem> list = new List<ABTableItem>();
            foreach (var abName in abNames)
            {
                // 仅处理external资源
                if (abName.EndsWith(".ab"))
                {
                    if (CheckIsInnerBundle(abName.Replace(".ab", string.Empty)))
                        continue;
                }
                else
                {
                    if (CheckIsInnerBundle(abName))
                        continue;
                }

                var srcAbPath = $"{abDir}/{abName}";
                var desPath = $"{resStorage}/{abName}";/*.{ResExt}"; EAGLEMARK: we use .ab in abName, no more ext*/
                var desDirPath = Path.GetDirectoryName(desPath);
                if (!Directory.Exists(desDirPath))
                {
                    Directory.CreateDirectory(desDirPath);
                }
                File.Copy(srcAbPath, desPath, true);
                ABTableItem item = new ABTableItem();
                item.Name = abName;//.Replace(ResExt, "");           EAGLEMARK: don't know why remote ResExt, stash it
                List<string> dependencies = new List<string>();
                var dep = mainAbManifest.GetAllDependencies(item.Name);
                DebugEx.FormatInfo("ConstructResStorageAction ----> dep {0}: {1}", item.Name, dep);
                foreach (var dependencie in mainAbManifest.GetAllDependencies(item.Name))
                {
                    dependencies.Add(dependencie);//.Replace(ResExt, "");           EAGLEMARK: don't know why remote ResExt, stash it
                }

                if(item.Name == "ui_guide.ab")
                {
                    dependencies.Clear();
                    dependencies.Add("shader_global.ab");
                    dependencies.Add("common_firstload.ab");
                }
                if(item.Name == "shader_global.ab")
                {
                    dependencies.Clear();
                }
                if(item.Name == "common_firstload.ab")
                {
                    dependencies.Clear();
                    dependencies.Add("shader_global.ab");
                }
                if(item.Name == "map_common.ab")
                {
                    dependencies.Clear();
                    dependencies.Add("shader_global.ab");
                    dependencies.Add("common_firstload.ab");
                }
                else
                {
                    // ===== analysis =====
                    FAT.BundleAnalysis.CheckIsCleanDeps(item.Name, dependencies);
                    // ====================
                }
                item.Dependencies = dependencies.ToArray();
                list.Add(item);
            }

            // ===== analysis =====
            FAT.BundleAnalysis.GenerateReport($"{Application.dataPath}/../../bundle_analysis.log");
            // ====================

            Logger.Info("Start craete target custom assetbundle manifest .");

            // 保存external专用的manifest
            string targetPath = $"{resStorage}/{Constant.kExternalResList}";
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            ResManifest resManifest = new ResManifest();
            resManifest.Tables = list.ToArray();
            var json = AppBuildContext.ToJson(resManifest);
            File.WriteAllText(targetPath, json,AppBuildContext.TextEncoding);
            Logger.Info("Create target custom manifest completed!");

            AssetDatabase.Refresh();
            Logger.Debug("Copy assetbundles completed!");
        }

        private bool CheckIsInnerBundle(string bundle)
        {
            var cfg = externalResConfig;
            foreach (var name in cfg.bundle_name)
            {
                if (name.Equals(bundle))
                {
                    Debug.Log($"AssetBundleAction ----> exclude bundle {bundle} by name {name}");
                    return false;
                }
            }
            foreach (var pattern in externalResConfig.bundle_pattern)
            {
                if (Regex.IsMatch(bundle, pattern))
                {
                    Debug.Log($"AssetBundleAction ----> exclude bundle {bundle} by pattern {pattern}");
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}