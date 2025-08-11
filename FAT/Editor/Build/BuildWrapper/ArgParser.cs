/*
 * @Author: qun.chao
 * @Date: 2023-05-22 17:28:14
 */
using UnityEngine;
using UnityEditor;

namespace BuildWrapper
{
    public class BuildArgs
    {
        public string variant = string.Empty;
        public bool isIl2cpp = false;
        public bool isUpload = false;
        public bool isApp = false;
        public bool buildNoAppStore = false;
        public bool encryptRes = false;
        public bool encryptApp = false;
        public AssetBundleBuilder.Request.ProfilerType profilerType = AssetBundleBuilder.Request.ProfilerType.None;

        public static BuildArgs Create()
        {
            var args = new BuildArgs();
            args.Parse();
            return args;
        }

        private void Parse()
        {
            var args = System.Environment.GetCommandLineArgs();

            foreach (var arg in args)
            {
                if (arg.Contains("--variant:"))
                {
                    variant = arg.Split(':').GetElementEx(1, ArrayExt.OverflowBehaviour.Default);
                }
                else if (arg.Contains("--app"))
                {
                    isApp = true;
                }
                else if (arg.Contains("--upload-res"))
                {
                    isUpload = true;
                }
                else if (arg.Contains("--il2cpp"))
                {
                    isIl2cpp = true;
                }
                else if (arg.Contains("--add-no-iap"))
                {
                    buildNoAppStore = true;
                }
                else if (arg.Contains("--encrypt-app"))
                {
                    encryptApp = true;
                }
                else if (arg.Contains("--encrypt-res"))
                {
                    encryptRes = true;
                }
                else if (arg.Contains("--profiler:"))
                {
                    profilerType = (AssetBundleBuilder.Request.ProfilerType)System.Enum.Parse(typeof(AssetBundleBuilder.Request.ProfilerType), arg.Split(':')[1]);
                }
            }
        }
    }
}