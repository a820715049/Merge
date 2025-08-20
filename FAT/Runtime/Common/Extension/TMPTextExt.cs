// ==================================================
// // File: TMPTextExt.cs
// // Author: liyueran
// // Date: 2025-05-08 11:05:14
// // Desc: TMPText扩展方法
// // ==================================================

using FAT;
using TMPro;

namespace EL
{
    public static class TMPTextExt
    {
        /// <summary>
        /// 倒计时
        /// </summary>
        public static void SetCountDown(this TMP_Text target, long totalSec, UIUtility.CdStyle style = UIUtility.CdStyle.Unified)
        {
            UIUtility.CountDownFormat(target, totalSec, style);
        }
        
        /// <summary>
        /// 红点数量
        /// </summary>
        public static void SetRedPoint(this TMP_Text target, int count)
        {
            target.SetText(count > 999 ? "999+" : $"{count}");
        }
    }
}