/*
 * @Author: qun.chao
 * @Date: 2023-10-20 12:13:37
 */
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FAT
{
    /// <summary>
    /// runtime类 不涉及UnityEditor有关引用
    /// </summary>
    public static class ConfTool
    {
        private static readonly string confRepoName = "fatr_conf";

        // 框架自动更新配置
        // public static void SyncConf()
        // {
        //     var projectName = FindProjectRoot();
        //     var workingDir = $"{Application.dataPath}/../../{confRepoName}";
        //     ProcessUtility.StartProcess($"{workingDir}/fatSyncConf.sh", projectName, workingDir);
        // }

        public static void SyncProto()
        {
            var projectName = FindProjectRoot();
            var workingDir = $"{Application.dataPath}/../../{confRepoName}";
            ProcessUtility.StartProcess($"{workingDir}/fatSyncProto.sh", projectName, workingDir);
        }

        static string FindProjectRoot()
        {
            string pattern = @"\/([\w]*)\/Assets$";
            var matches = Regex.Matches(Application.dataPath, pattern, RegexOptions.Singleline);
            if (matches.Count > 0)
            {
                return matches[0].Groups[1].Value;
            }
            return "";
        }
    }
}