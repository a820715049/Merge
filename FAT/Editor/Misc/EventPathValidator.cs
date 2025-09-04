/*
 * @Author: qun.chao
 * @Date: 2025-08-29 11:07:44
 */
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FAT
{
    /// <summary>
    /// 数据来源
    /// https://docs.google.com/spreadsheets/d/1raGnNBa4p-swzU-7WaRu-RdhGQnX2EHB7fyG47uqPoA/edit?gid=1805893810
    /// 
    /// 下载成csv文件后, 通过菜单Tools/Validate Event Path 打开
    /// 
    /// CSV-driven validator and fixer for bundle folder placement.
    ///
    /// CSV 结构:
    /// - 第 1 列: bundle 名 (例如 event_endless_2025america)
    /// - 第 2..N 列: 版本号列; 单元格表示该版本下是否“存在/不应存在”
    ///
    /// 规则 (严格大小写路径):
    /// - 存在:   Assets/Bundle/{category 小写}/bundle_...
    /// - 不存在: Assets/BundleNo/Exclude/{Category 首字母大写}/bundle_...
    ///
    /// 功能:
    /// - 解析 CSV、选择版本列、校验路径
    /// - 支持确认后自动移动错误目录并仅重导入被移动目录
    /// </summary>
    public class EventPathValidator
    {
        [MenuItem("Tools/Validate Event Path")]
        private static void Validate()
        {
            try
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select CSV file", Application.dataPath, "csv");
                if (string.IsNullOrEmpty(selectedPath))
                {
                    Debug.Log("CSV selection cancelled.");
                    return;
                }

                string csv = File.ReadAllText(selectedPath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(csv))
                {
                    Debug.LogError($"CSV file is empty: {selectedPath}");
                    return;
                }

                List<string[]> rows = ParseCsv(csv);
                if (rows.Count == 0)
                {
                    Debug.LogWarning($"No rows parsed from CSV: {selectedPath}");
                    return;
                }
                ValidateFolderLayoutAgainstCsv(rows);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to read or parse CSV: {ex.Message}\n{ex}");
                EditorUtility.DisplayDialog("CSV Error", "Failed to read or parse CSV. Check Console for details.", "OK");
            }
        }

        /// <summary>
        /// 版本选择入口：校验前先弹出版本选择 Wizard。
        /// </summary>
        private static void ValidateFolderLayoutAgainstCsv(List<string[]> rows)
        {
            if (rows == null || rows.Count < 2)
            {
                Debug.LogWarning("CSV must contain a header row and at least one data row.");
                return;
            }

            string[] header = rows[0];
            if (header.Length < 2)
            {
                Debug.LogWarning("CSV header must contain at least two columns: bundle name + one version.");
                return;
            }
            VersionInputWizard.Show(rows);
        }

        /// <summary>
        /// 按指定版本列执行校验，收集可自动修复项，并在确认后执行移动与重导入。
        /// </summary>
        private static void RunValidationCore(List<string[]> rows, int versionColumnIndex, string versionName)
        {
            int okCount = 0;
            int failCount = 0;
            int skipCount = 0;
            var sb = new StringBuilder();
            sb.AppendLine($"Validating folders for version: {versionName}");

            var pendingMoves = new List<FixItem>();
            var nonFixableIssues = new List<string>();

            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                if (row == null || row.Length == 0) { skipCount++; continue; }

                string rawBundleName = SafeGet(row, 0)?.Trim();
                if (string.IsNullOrEmpty(rawBundleName)) { skipCount++; continue; }

                string existsCell = versionColumnIndex < row.Length ? SafeGet(row, versionColumnIndex) : null;
                bool? expectedExistsOpt = InterpretExistsCell(existsCell);
                if (!expectedExistsOpt.HasValue)
                {
                    skipCount++;
                    sb.AppendLine($"SKIP: '{rawBundleName}' has unrecognized value '{existsCell}' for version '{versionName}'.");
                    continue;
                }
                bool expectedExists = expectedExistsOpt.Value;

                if (!TryDerivePaths(rawBundleName, out string expectedBundlePath, out string expectedExcludePath))
                {
                    failCount++;
                    sb.AppendLine($"FAIL: '{rawBundleName}' cannot derive paths (expected format 'category_subname').");
                    continue;
                }

                bool inBundle = AssetDatabase.IsValidFolder(expectedBundlePath);
                bool inExclude = AssetDatabase.IsValidFolder(expectedExcludePath);

                if (expectedExists)
                {
                    bool ok = inBundle && !inExclude;
                    if (ok)
                    {
                        okCount++;
                    }
                    else
                    {
                        failCount++;
                        if (inExclude && !inBundle)
                        {
                            pendingMoves.Add(new FixItem { bundleName = rawBundleName, fromPath = expectedExcludePath, toPath = expectedBundlePath });
                            sb.AppendLine($"MOVE: '{rawBundleName}' will be moved from '{expectedExcludePath}' -> '{expectedBundlePath}'.");
                        }
                        else
                        {
                            nonFixableIssues.Add($"'{rawBundleName}' expected in Bundle but folder missing or duplicated. bundle={inBundle}, exclude={inExclude}");
                            sb.AppendLine($"FAIL: '{rawBundleName}' expected in Bundle at '{expectedBundlePath}', actual: inBundle={inBundle}, inExclude={inExclude}.");
                        }
                    }
                }
                else
                {
                    bool ok = inExclude && !inBundle;
                    if (ok)
                    {
                        okCount++;
                    }
                    else
                    {
                        failCount++;
                        if (inBundle && !inExclude)
                        {
                            pendingMoves.Add(new FixItem { bundleName = rawBundleName, fromPath = expectedBundlePath, toPath = expectedExcludePath });
                            sb.AppendLine($"MOVE: '{rawBundleName}' will be moved from '{expectedBundlePath}' -> '{expectedExcludePath}'.");
                        }
                        else
                        {
                            nonFixableIssues.Add($"'{rawBundleName}' expected in Exclude but folder missing or duplicated. bundle={inBundle}, exclude={inExclude}");
                            sb.AppendLine($"FAIL: '{rawBundleName}' expected in Exclude at '{expectedExcludePath}', actual: inBundle={inBundle}, inExclude={inExclude}.");
                        }
                    }
                }
            }

            sb.AppendLine($"Summary: OK={okCount}, FAIL={failCount}, SKIP={skipCount}.");
            sb.AppendLine($"Auto-fix candidates: {pendingMoves.Count}, Non-fixable: {nonFixableIssues.Count}.");

            if (pendingMoves.Count > 0)
            {
                var preview = new StringBuilder();
                int previewCount = Math.Min(20, pendingMoves.Count);
                for (int i = 0; i < previewCount; i++)
                {
                    var m = pendingMoves[i];
                    preview.AppendLine($"{i + 1}. {m.bundleName}: {m.fromPath} -> {m.toPath}");
                }
                if (pendingMoves.Count > previewCount)
                {
                    preview.AppendLine($"... and {pendingMoves.Count - previewCount} more");
                }

                int res = EditorUtility.DisplayDialogComplex(
                    "确认修正",
                    $"发现 {pendingMoves.Count} 个可自动修正的目录，是否移动到期望位置？\n\n预览:\n{preview}",
                    "修正并重导入",
                    "仅报告",
                    "取消");

                if (res == 0)
                {
                    ApplyMoves(pendingMoves);
                }
                else if (res == 2)
                {
                    sb.AppendLine("User cancelled auto-fix.");
                }
                else
                {
                    sb.AppendLine("User chose report-only; no changes applied.");
                }
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// 单个修复项：将目录 fromPath 移动到 toPath。
        /// </summary>
        private class FixItem
        {
            public string bundleName;
            public string fromPath;
            public string toPath;
        }

        /// <summary>
        /// 执行目录移动，并对被移动目录进行递归与逐资源重导入，保证 Inspector 与 meta 同步。
        /// </summary>
        private static void ApplyMoves(List<FixItem> moves)
        {
            var log = new StringBuilder();
            log.AppendLine($"Applying {moves.Count} folder move(s)...");
            int success = 0;
            foreach (var m in moves)
            {
                try
                {
                    EnsureParentFolderExists(m.toPath);
                    string error = AssetDatabase.MoveAsset(m.fromPath, m.toPath);
                    if (!string.IsNullOrEmpty(error))
                    {
                        log.AppendLine($"FAIL: Move '{m.bundleName}' -> {m.toPath} failed: {error}");
                        continue;
                    }
                    AssetDatabase.ImportAsset(m.toPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
                    ReimportAllAssetsUnder(m.toPath);
                    success++;
                    log.AppendLine($"OK: Moved '{m.bundleName}' to {m.toPath} and reimported.");
                }
                catch (Exception ex)
                {
                    log.AppendLine($"ERROR: Exception moving '{m.bundleName}': {ex.Message}");
                }
            }
            AssetDatabase.Refresh();
            log.AppendLine($"Move summary: {success}/{moves.Count} succeeded.");
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// 对目标目录下的所有文件资源执行 ImportAsset(ForceUpdate)。
        /// 仅限被移动目录，不扩大范围。
        /// </summary>
        private static void ReimportAllAssetsUnder(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;
                try
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                catch { }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 如果不存在则逐级创建目标文件夹（从 Assets/ 开始）。
        /// </summary>
        private static void EnsureParentFolderExists(string assetFolderPath)
        {
            string parent = Path.GetDirectoryName(assetFolderPath).Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || parent == "Assets") return;

            string[] parts = parent.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets") return;
            string curr = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = curr + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(curr, parts[i]);
                }
                curr = next;
            }
        }

        /// <summary>
        /// 版本选择界面：从表头（第 2 列起）收集版本，默认选中最后一个。
        /// </summary>
        private class VersionInputWizard : ScriptableWizard
        {
            private static List<string[]> s_rows;
            private static string[] s_versionNames;
            private static int[] s_versionCols;
            private static int s_defaultIndex;

            public int selectedIndex = 0;

            /// <summary>
            /// 初始化并展示版本选择界面。
            /// </summary>
            public static void Show(List<string[]> rows)
            {
                s_rows = rows;

                // Build version list from header (skip column 0)
                var header = rows[0];
                var names = new List<string>();
                var cols = new List<int>();
                for (int i = 1; i < header.Length; i++)
                {
                    string name = header[i];
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    names.Add(name.Trim());
                    cols.Add(i);
                }

                if (names.Count == 0)
                {
                    EditorUtility.DisplayDialog("错误", "未在表头中找到任何版本列。", "OK");
                    return;
                }

                s_versionNames = names.ToArray();
                s_versionCols = cols.ToArray();
                s_defaultIndex = s_versionNames.Length - 1; // default to last (most recent)

                DisplayWizard<VersionInputWizard>("选择版本号", "开始校验", "取消");
            }

            void OnEnable()
            {
                // Initialize default selection
                selectedIndex = s_defaultIndex;
            }

            protected override bool DrawWizardGUI()
            {
                EditorGUILayout.LabelField("请选择用于校验的版本:");
                if (s_versionNames != null && s_versionNames.Length > 0)
                {
                    selectedIndex = EditorGUILayout.Popup("版本", Mathf.Clamp(selectedIndex, 0, s_versionNames.Length - 1), s_versionNames);
                }
                return true;
            }

            void OnWizardCreate()
            {
                if (s_rows == null || s_rows.Count == 0 || s_versionNames == null || s_versionCols == null || s_versionNames.Length == 0)
                {
                    EditorUtility.DisplayDialog("错误", "CSV 数据无效或版本列表为空。", "OK");
                    return;
                }

                int safeIndex = Mathf.Clamp(selectedIndex, 0, s_versionNames.Length - 1);
                string versionName = s_versionNames[safeIndex];
                int col = s_versionCols[safeIndex];

                RunValidationCore(s_rows, col, versionName);
            }
        }

        /// <summary>
        /// 获取单元格内容，越界返回 null。
        /// </summary>
        private static string SafeGet(string[] arr, int index)
        {
            if (arr == null) return null;
            if (index < 0 || index >= arr.Length) return null;
            return arr[index];
        }

        /// <summary>
        /// 将单元格解析为三态布尔：true/false/null(未知)。
        /// 支持常见真/假关键词（1/0, yes/no, 是/否, include/exclude 等）。
        /// </summary>
        private static bool? InterpretExistsCell(string raw)
        {
            if (raw == null) return null;
            string v = raw.Trim();
            if (v.Length == 0) return null;

            string lower = v.ToLowerInvariant();
            // truthy values: exist/included
            if (lower == "1" || lower == "y" || lower == "yes" || lower == "true" || lower == "是" || lower == "存在" || lower == "in" || lower == "include" || lower == "包含")
                return true;
            // falsy values: not exist/excluded
            if (lower == "0" || lower == "n" || lower == "no" || lower == "false" || lower == "否" || lower == "不存在" || lower == "out" || lower == "exclude" || lower == "不包含")
                return false;

            // Allow explicit empty marker like '-' or '' to mean unknown
            if (lower == "-" || lower == "null") return null;

            // If it's a number like '0.0' or '1.0', try parse int
            if (int.TryParse(v, out int intVal))
            {
                if (intVal == 0) return false;
                if (intVal == 1) return true;
            }
            return null;
        }

        /// <summary>
        /// 根据 bundle 名推导目标路径。
        /// 示例: event_endless_2025america →
        /// - 存在:   Assets/Bundle/event/bundle_endless_2025america
        /// - 不存在: Assets/BundleNo/Exclude/Event/bundle_endless_2025america
        /// </summary>
        private static bool TryDerivePaths(string bundleName, out string expectedBundlePath, out string expectedExcludePath)
        {
            expectedBundlePath = null;
            expectedExcludePath = null;

            int underscore = bundleName.IndexOf('_');
            if (underscore <= 0 || underscore >= bundleName.Length - 1) return false;

            string category = bundleName.Substring(0, underscore);
            string sub = bundleName.Substring(underscore + 1); // may contain further underscores
            string bundleDirName = "bundle_" + sub;

            string categoryLower = category.ToLowerInvariant();
            string categoryUpper = UpperFirst(category);

            expectedBundlePath = $"Assets/Bundle/{categoryLower}/{bundleDirName}";
            expectedExcludePath = $"Assets/BundleNo/Exclude/{categoryUpper}/{bundleDirName}";
            return true;
        }

        /// <summary>
        /// 字符串首字母大写。
        /// </summary>
        private static string UpperFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length == 1) return s.ToUpperInvariant();
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>
        /// 简易 CSV 解析器：支持引号字段与 CRLF/LF 换行。
        /// </summary>
        private static List<string[]> ParseCsv(string csvContent)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(csvContent)) return rows;

            int index = 0;
            int length = csvContent.Length;
            var currentField = new StringBuilder();
            var currentRow = new List<string>();
            bool inQuotes = false;

            while (index < length)
            {
                char ch = csvContent[index];
                if (inQuotes)
                {
                    if (ch == '\"')
                    {
                        bool isEscaped = (index + 1 < length && csvContent[index + 1] == '\"');
                        if (isEscaped)
                        {
                            currentField.Append('\"');
                            index += 2;
                            continue;
                        }
                        else
                        {
                            inQuotes = false;
                            index++;
                            continue;
                        }
                    }
                    else
                    {
                        currentField.Append(ch);
                        index++;
                        continue;
                    }
                }
                else
                {
                    if (ch == '\"')
                    {
                        inQuotes = true;
                        index++;
                        continue;
                    }
                    if (ch == ',')
                    {
                        currentRow.Add(currentField.ToString());
                        currentField.Length = 0;
                        index++;
                        continue;
                    }
                    if (ch == '\n' || ch == '\r')
                    {
                        currentRow.Add(currentField.ToString());
                        currentField.Length = 0;
                        rows.Add(currentRow.ToArray());
                        currentRow.Clear();
                        if (ch == '\r' && index + 1 < length && csvContent[index + 1] == '\n')
                        {
                            index += 2;
                        }
                        else
                        {
                            index++;
                        }
                        continue;
                    }
                    currentField.Append(ch);
                    index++;
                }
            }

            if (inQuotes)
            {
                inQuotes = false;
            }
            if (currentField.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentField.ToString());
                rows.Add(currentRow.ToArray());
            }

            return rows;
        }

    }
}