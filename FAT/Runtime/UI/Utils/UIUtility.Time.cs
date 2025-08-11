/*
 * @Author: qun.chao
 * @Date: 2024-09-20 18:20:18
 */

using System;
using Cysharp.Text;
using TMPro;
using EL;
using ZStringBuilder = Cysharp.Text.Utf16ValueStringBuilder;

namespace FAT
{
    public static partial class UIUtility
    {
        static Lazy<string> TimeSepStr;
        static Lazy<string> DayStr;
        static Lazy<string> HourStr;
        static Lazy<string> MinStr;
        static Lazy<string> SecStr;

        // 静态构造函数，确保类被加载时初始化所有静态字段
        static UIUtility()
        {
            InitCacheStrings();
        }

        // 初始化缓存字符串
        private static void InitCacheStrings()
        {
            TimeSepStr = new Lazy<string>(() => I18N.Text("#SysComDesc416"));
            DayStr = new Lazy<string>(() => I18N.Text("#SysComDesc149"));
            HourStr = new Lazy<string>(() => I18N.Text("#SysComDesc1"));
            MinStr = new Lazy<string>(() => I18N.Text("#SysComDesc2"));
            SecStr = new Lazy<string>(() => I18N.Text("#SysComDesc3"));
        }

        static void AppendSep(ref this ZStringBuilder sb)
        {
            sb.Append(TimeSepStr.Value);
        }

        static void AppendDay(ref this ZStringBuilder sb, long val)
        {
            sb.Append(val);
            sb.Append(DayStr.Value);
        }

        static void AppendHour(ref this ZStringBuilder sb, long val)
        {
            sb.Append(val);
            sb.Append(HourStr.Value);
        }

        static void AppendMin(ref this ZStringBuilder sb, long val)
        {
            sb.Append(val);
            sb.Append(MinStr.Value);
        }

        static void AppendSec(ref this ZStringBuilder sb, long val)
        {
            sb.Append(val);
            sb.Append(SecStr.Value);
        }

        public enum CdStyle
        {
            Unified, // 默认通用格式
            OmitZero, // 忽略0
        }

        public static string CountDownFormat(long totalSec, CdStyle style = CdStyle.Unified)
        {
            // 使用using语句确保自动调用Dispose释放资源
            using var sb = style switch
            {
                CdStyle.Unified => CountdownUnified(totalSec),
                CdStyle.OmitZero => CountdownOmitZero(totalSec),
                _ => CountdownUnified(totalSec)
            };

            return sb.ToString();
        }

        // 倒计时显示
        // 如果totalSec 读的配置 不会动态变化 CdStyle需要使用OmitZero
        public static void CountDownFormat(TMP_Text tmp, long totalSec, CdStyle style = CdStyle.Unified)
        {
            if (tmp == null) return;

            // 使用using语句确保自动调用Dispose释放资源
            using var sb = style switch
            {
                CdStyle.Unified => CountdownUnified(totalSec),
                CdStyle.OmitZero => CountdownOmitZero(totalSec),
                _ => CountdownUnified(totalSec)
            };

            tmp.SetText(sb);
        }

        /// <summary>
        /// 通用倒计时格式
        /// </summary>
        private static Utf16ValueStringBuilder CountdownUnified(long totalSec)
        {
            if (totalSec < 0) totalSec = 0;

            var sb = ZString.CreateStringBuilder();

            var hour = totalSec / 3600;
            var day = hour / 24;
            hour = hour % 24;
            var min = (totalSec % 3600) / 60;
            var sec = totalSec % 60;

            // 剩余时长>24h时，显示为：xxd xxh（小时位不足1时，显示为0）
            if (day > 0)
            {
                sb.AppendDay(day);
                sb.AppendSep();
                sb.AppendHour(hour);
            }
            // 24小时≥剩余时长>1小时时，显示为：xxh xxm（分钟位不足1时，显示为0）
            else if (hour > 0)
            {
                sb.AppendHour(hour);
                sb.AppendSep();
                sb.AppendMin(min);
            }
            // 1小时≥剩余时长>1分钟时，显示为：xxm xxs（秒位不足1时，显示为0）
            else if (min > 0)
            {
                sb.AppendMin(min);
                sb.AppendSep();
                sb.AppendSec(sec);
            }
            // 1分钟>剩余时长时，显示为：xxs
            else
            {
                sb.AppendSec(sec);
            }

            return sb;
        }

        /// <summary>
        /// 通用倒计时格式
        /// </summary>
        private static Utf16ValueStringBuilder CountdownOmitZero(long totalSec)
        {
            if (totalSec < 0) totalSec = 0;

            var sb = ZString.CreateStringBuilder();

            var hour = totalSec / 3600;
            var day = hour / 24;
            hour = hour % 24;
            var min = (totalSec % 3600) / 60;
            var sec = totalSec % 60;

            if (day > 0)
            {
                sb.AppendDay(day);
                if (hour != 0)
                {
                    sb.AppendSep();
                    sb.AppendHour(hour);
                }
            }
            else if (hour > 0)
            {
                sb.AppendHour(hour);
                if (min != 0)
                {
                    sb.AppendSep();
                    sb.AppendMin(min);
                }
            }
            else if (min > 0)
            {
                sb.AppendMin(min);
                if (sec != 0)
                {
                    sb.AppendSep();
                    sb.AppendSec(sec);
                }
            }
            // 1分钟>剩余时长时，显示为：xxs
            else
            {
                sb.AppendSec(sec);
            }

            return sb;
        }


        //debug工具中切换多语言后 会主动清理当前缓存的字符，清理方式为重新初始化
        public static void DebugResetCacheStr()
        {
            InitCacheStrings();
        }
    }
}