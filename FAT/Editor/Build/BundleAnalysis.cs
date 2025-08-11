/*
 * @Author: qun.chao
 * @Date: 2024-12-09 14:54:35
 */

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FAT
{
    public class BundleAnalysis
    {
        // 目前版本常规bundle应有的依赖 对于重点关注的bundle 其依赖范围不应超出此列表
        private static readonly HashSet<string> default_deps = new()
        {
            "common_firstload.ab",
            "fat_font.ab",
            "fat_font_sprite.ab",
            "fat_font_style.ab",
            "fat_guide.ab",
            "fat_item.ab",
            "fat_item_mini.ab",
            "plugin_spine.ab",
            "plugin_tmp.ab",
            "shader_global.ab"
        };

        // 关键bundle
        private static readonly HashSet<string> key_bundles = new()
        {
            "fat_global.ab"
        };

        private static bool IsEvent(string name)
        {
            return name.Contains("event_");
        }

        private static bool IsCommon(string name)
        {
            return name.Contains("common");
        }

        private static bool IsCard(string name)
        {
            return name.Contains("card");
        }

        private static bool IsDecorate(string name)
        {
            return name.Contains("decorate");
        }

        private static bool IsPachinko(string name)
        {
            return name.Contains("event_pachinko");
        }

        private static bool IsEventCommon(string name)
        {
            return name.Contains("event_common");
        }

        // 暂时容忍fat_global_ext的复杂度
        private static bool IsFatGlobalExt(string name)
        {
            return name.Contains("fat_global_ext");
        }

        private static List<string> _temp = new();

        // 记录存在问题的bundle
        private static List<(string bundle, List<string> deps)> _badDepsRecord = new();

        public static void ClearRecord()
        {
            _badDepsRecord.Clear();
        }

        private static void AddRecord(string bundle, IList<string> deps)
        {
            if (deps.Count == 0) return;
            var report = new List<string>();
            report.AddRange(deps);
            _badDepsRecord.Add((bundle, report));
        }

        public static void GenerateReport(string path)
        {
            var sb = new StringBuilder();
            foreach (var (bundle, deps) in _badDepsRecord)
            {
                sb.Append($"bundle: {bundle}");
                sb.AppendLine();
                foreach (var d in deps)
                {
                    sb.Append($" - {d}");
                    sb.AppendLine();
                }
            }

            System.IO.File.WriteAllText(path, sb.ToString());
        }

        private static bool CheckIsDefaultDeps(string bundle, IList<string> deps)
        {
            var report = _temp;
            report.Clear();
            foreach (var d in deps)
                if (!default_deps.Contains(d))
                    report.Add(d);
            if (report.Count > 0)
            {
                AddRecord(bundle, report);
                return false;
            }

            return true;
        }

        public static bool CheckIsCleanDeps(string bundle, IList<string> deps)
        {
            if (key_bundles.Contains(bundle)) return CheckIsDefaultDeps(bundle, deps);

            var report = _temp;
            report.Clear();
            if (IsCard(bundle))
            {
                foreach (var x in deps)
                    if (IsEvent(x) && !IsEventCommon(x))
                        report.Add(x);
            }
            else if (IsDecorate(bundle))
            {
                var pattern = @"\d+.ab$";
                var match = Regex.Match(bundle, pattern);
                foreach (var x in deps)
                    if (IsCard(x))
                        report.Add(x);
                    else if (IsEvent(x) && !IsCommon(x) && !IsDecorate(x))
                        report.Add(x);
                    else if (IsDecorate(x) && match.Success && IsDecorate(x) && !IsCommon(x) &&
                             match.Value != Regex.Match(x, pattern).Value)
                        report.Add(x);
                    else if (x.Contains("chest"))
                        report.Add(x);
            }
            else if (IsPachinko(bundle))
            {
                var pattern = @"\d+.ab$";
                var match = Regex.Match(bundle, pattern);
                foreach (var x in deps)
                    if (IsCard(x))
                        report.Add(x);
                    else if (IsDecorate(x))
                        report.Add(x);
                    else if (IsPachinko(x) && match.Success && match.Value != Regex.Match(x, pattern).Value && !IsCommon(x))
                        report.Add(x);
                    else if (IsEvent(x) && !IsCommon(x) && !IsPachinko(x))
                        report.Add(x);
                    else if (x.Contains("chest"))
                        report.Add(x);
            }
            else if (IsEvent(bundle))
            {
                foreach (var x in deps)
                    if (IsCard(x))
                        report.Add(x);
                    else if (IsDecorate(x))
                        report.Add(x);
                    else if (IsPachinko(x))
                        report.Add(x);
                    else if (IsEvent(x) && !IsCommon(x))
                        report.Add(x);
                    else if (x.Contains("chest"))
                        report.Add(x);
            }
            else if (IsFatGlobalExt(bundle))
            {
                foreach (var x in deps)
                    if (IsCard(x) && !IsCommon(x) || IsEvent(x) && !IsCommon(x))
                        report.Add(x);
            }
            else
            {
                // 不应包含任何card/event
                foreach (var x in deps)
                    if (IsCard(x) || IsEvent(x))
                        report.Add(x);
            }

            if (report.Count > 0)
            {
                AddRecord(bundle, report);
                return false;
            }

            return true;
        }
    }
}