using System.Collections.Generic;
using UnityEngine;
using OfficeOpenXml;
using System.IO;

namespace DevelopTools
{
    public class ConfigUtils
    {
        /// <summary>
        /// 获取资源目录
        /// </summary>
        /// <param name="path">根路径</param>
        /// <param name="suffix">扩展名</param>
        /// <returns>结果</returns>
        public static List<string> GetFiles(string path, string suffix, List<string> content = null)
        {
            if (content == null)
            {
                content = new List<string>();
            }
            _GetFiles(path, suffix, content);
            return content;
        }

        private static void _GetFiles(string path, string suffix, List<string> content)
        {
            string[] files = Directory.GetFiles(path, suffix);
            foreach (var file in files)
            {
                var temp = file.Replace("\\", "/");
                content.Add(temp);
            }

            string[] directorys = Directory.GetDirectories(path);
            foreach (var directory in directorys)
            {
                _GetFiles(directory, suffix, content);
            }
        }

        public static  string GetConfigRootPath()
        {
            var workingDir = $"{Application.dataPath}/../../conf";
            return workingDir + "/";
        }

        public static string GetExcelRootPath()
        {
            var configRoot = GetConfigRootPath();
            return configRoot + "excel/";
        }

        //获取配置路径
        public static string GetExcelPathByName(string excelName)
        {
            string excelRoot = GetExcelRootPath();
            string excelPath = excelRoot + string.Format("{0}.xlsx", excelName);
            return excelPath;
        }

        /// <summary>
        /// 获取配置 sheet
        /// </summary>
        /// <param name="excel">配置表</param>
        /// <param name="sheetName">需要获取的sheet名称</param>
        /// <returns>ExcelWorksheet</returns>
        public static ExcelWorksheet GetWorksheetByExcelPath(ExcelPackage excel, string sheetName)
        {
            ExcelWorksheets sheets = excel.Workbook.Worksheets;
            int count = sheets.Count;
            for (int i = 1; i <= count; i++)
            {
                ExcelWorksheet sheet = sheets[i];
                if (sheet.Name == sheetName)
                {
                    return sheet;
                }
            }
            return null;
        }
    }
}