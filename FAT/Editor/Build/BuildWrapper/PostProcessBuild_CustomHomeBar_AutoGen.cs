/*
 * @Author: yanfuxing
 * @Date: 2025-04-29 10:31:00
 */
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace BuildWrapper
{
    public class PostProcessBuild_CustomHomeBar_AutoGen
    {
        public static void OnPostprocessBuild(BuildTarget buildTarget, string xcodeProjectDir)
        {
            if (buildTarget != BuildTarget.iOS) return;

            string filePath = Path.Combine(xcodeProjectDir, "Classes/UI/UnityViewControllerBase+iOS.mm");

            if (!File.Exists(filePath))
            {
                return;
            }

            string content = File.ReadAllText(filePath);

            content = Regex.Replace(
                content,
                @"- \(UIRectEdge\)\s*preferredScreenEdgesDeferringSystemGestures\s*\{[^}]*\}",
                @"- (UIRectEdge)preferredScreenEdgesDeferringSystemGestures
{
    return UIRectEdgeAll;
}", RegexOptions.Multiline);

            if (content.Contains("prefersHomeIndicatorAutoHidden"))
            {
                content = Regex.Replace(
                    content,
                    @"- \(BOOL\)\s*prefersHomeIndicatorAutoHidden\s*\{[^}]*\}",
                    "",
                    RegexOptions.Singleline);
            }

            File.WriteAllText(filePath, content);
        }
    }
}

