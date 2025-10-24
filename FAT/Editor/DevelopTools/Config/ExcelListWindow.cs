using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CenturyGame.AppBuilder.Editor.Builds;
using System.Diagnostics;
using System;
using FAT;
using OfficeOpenXml;

namespace DevelopTools
{
    public class ExcelListWindow : EditorWindow
    {
        [MenuItem("Tools/Develop/Excel List Window", false, 10000)]
        public static void ShowWindow()
        {
            try
            {
                var window = EditorWindow.GetWindow(typeof(ExcelListWindow));
                window.titleContent = new GUIContent("Excel List");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.Log(ex.Message);
            }
        }

        private string folderPath = ""; // 设置文件夹路径

        private List<string> fileList = new List<string>(); // 存储文件列表
        private List<string> openFileList = new List<string>(); // 打开的文件列表  根据临时文件判定
        private Dictionary<string, string> sheetName2ExcelName = new Dictionary<string, string>(); // 存储sheet名与excel名的映射
        private Dictionary<string, string> excelName2SheetNames = new Dictionary<string, string>(); // 存储excel名与sheet名的映射

        private Vector2 scrollPosition; // 文件列表的滚动位置
        private Vector2 sheetScrollPosition; // sheet列表的滚动位置

        private double lastClickTime = 0;
        private string lastClickedFilePath = "";
        private bool foldSheets = true; // 是否折叠sheet列表
        private bool foldBtns = true; // 是否折叠按钮

        private readonly string imagePath = "Assets/Scripts/FAT/Editor/DevelopTools/Config/Resource/excel.png";
        private readonly string excelListUrl = "";
        private readonly string pmtUrl = "https://fat.pmt.centurygame.io/pmt#/pmconf/conf_builder";
        private readonly string docUrl = "https://centurygames.feishu.cn/wiki/CU8fwMYVYi5S7HkG7lFcBtlSnCc";
        private readonly string artUrl = "https://art.diandian.info/orange";
        private readonly string jenkinsUrl = "http://gm7-jenkins2.diandian.info:8080/";
        private Texture2D image;
        private string inputStr = "";
        private string inputStr2 = "";

        private string tempPathStr;

        private void OnFocus()
        {
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            image = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
            RefreshFileList();
            RefreshSheetName2ExcelName();
        }

        #region 缓存json

        private Dictionary<string, string> RefreshSheetName2ExcelName()
        {
            sheetName2ExcelName.Clear();
            excelName2SheetNames.Clear();
            var cacheJson = GetCacheJson();
            if (cacheJson.excel_history == null)
            {
                return sheetName2ExcelName;
            }
            var names = new List<string>();
            foreach (var excel in cacheJson.excel_history)
            {
                var excelName = excel.Key.Replace(".xlsx", "");
                names.Clear();
                foreach (var sheet in excel.Value.sheet_history)
                {
                    var sheetName = sheet.Value.name.Replace("@Design", "").Replace("@design", "");
                    sheetName2ExcelName[sheetName] = excelName;
                    names.Add(sheetName);
                }
                excelName2SheetNames[excel.Key] = string.Join(" , ", names);
            }
            return sheetName2ExcelName;
        }

        private CacheJson GetCacheJson()
        {
            var cacheJsonStr = GetCacheJsonStr();
            if (!string.IsNullOrEmpty(cacheJsonStr))
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<CacheJson>(cacheJsonStr);
                // return JsonMapper.ToObject<CacheJson>(cacheJsonStr);
            }
            return new CacheJson();
        }

        private string GetCacheJsonStr()
        {
            var configRoot = ConfigUtils.GetConfigRootPath();
            var cacheJsonPath = configRoot + "cache.json";
            if (File.Exists(cacheJsonPath))
            {
                return File.ReadAllText(cacheJsonPath);
            }
            return "";
        }

        [Serializable]
        public class CacheJson
        {
            public Dictionary<string, ExcelFile> excel_history;
        }

        [Serializable]
        public class ExcelFile
        {
            public string name;
            public string md5;
            public Dictionary<string, SheetHistory> sheet_history;
            public bool can_skip;
        }

        [Serializable]
        public class SheetHistory
        {
            public string name;
        }

        #endregion

        private void OnGUI()
        {
            // GUILayout.Label("Excel File List", EditorStyles.boldLabel);
            GUILayout.Space(10);

            foldBtns = EditorGUILayout.Foldout(foldBtns, "快捷入口");
            if (foldBtns)
            {
                var btnWidth = position.width / 4;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("美术资源", GUILayout.Width(btnWidth)))//美术资源
                    {
                        Application.OpenURL(artUrl);
                    }
                    if (GUILayout.Button("文档空间", GUILayout.Width(btnWidth)))
                    {
                        Application.OpenURL(docUrl);
                    }
                    if (GUILayout.Button("PMT后台", GUILayout.Width(btnWidth)))
                    {
                        Application.OpenURL(pmtUrl);
                    }
                    if (GUILayout.Button("Jenkins", GUILayout.Width(btnWidth)))
                    {
                        Application.OpenURL(jenkinsUrl);
                    }
                }
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("", GUILayout.Width(btnWidth)))
                    {
                    }
                    if (GUILayout.Button("本地配置", GUILayout.Width(btnWidth)))
                    {
                        // EditorUtility.RevealInFinder(GetExcelRootPath());
                        Application.OpenURL("file://" + ConfigUtils.GetExcelRootPath());
                    }
                    if (GUILayout.Button("", GUILayout.Width(btnWidth))) //目录(cloud)
                    {
                        // Application.OpenURL(excelListUrl);
                    }
                    /* if (GUILayout.Button("使用本地配置", GUILayout.Width(btnWidth)))
                    {
                        Generate_Mac();
                    } */
                    if (GUILayout.Button("", GUILayout.Width(btnWidth))) //同步配置
                    {
                        // ConfTool.SyncProto();
                    }
                }
                GUILayout.EndHorizontal();
            }


            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("搜索(文件名/sheet名):", EditorStyles.miniBoldLabel, GUILayout.Width(105));
                inputStr = GUILayout.TextField(inputStr);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("搜索内容所属文件:", EditorStyles.miniBoldLabel, GUILayout.Width(105));
                inputStr2 = GUILayout.TextField(inputStr2);
                if (GUILayout.Button("搜索", GUILayout.Width(50)) && !string.IsNullOrEmpty(inputStr2))
                {
                    var result = EditorUtility.DisplayDialog("提示", "将搜索所有excel文件，结果将展示在Console窗口中。搜索内容较多，过程较慢，请耐心等待。", "确定", "取消");
                    if (result)
                    {
                        SearchContent();
                    }
                }
            }
            GUILayout.EndHorizontal();

            //分割线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.Height(6));

            #region 显示文件列表
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            var i = 0;
            var selectFile = "";
            foreach (string filePath in fileList)
            {
                var path = filePath.Replace(folderPath, "");
                var strs = path.Split('/');
                var name_suffix = "" + strs[strs.Length - 1];

                var tempPath = path.Replace(name_suffix, "");


                var name = name_suffix.Replace(".xlsx", "");
                excelName2SheetNames.TryGetValue(name_suffix, out var sheets);
                if (inputStr != "" && (name.IndexOf(inputStr, System.StringComparison.OrdinalIgnoreCase) == -1 && sheets.IndexOf(inputStr, System.StringComparison.OrdinalIgnoreCase) == -1))
                {
                    continue;
                }

                if (tempPathStr == null || i == 0 || tempPathStr != tempPath)
                {
                    tempPathStr = tempPath;
                    GUILayout.Label(tempPathStr, EditorStyles.miniLabel);
                }


                GUILayout.BeginHorizontal();

                if (image != null)
                {
                    GUILayout.Label(image, GUILayout.Width(20), GUILayout.Height(20));
                }

                bool isLastClicked = lastClickedFilePath == filePath;

                if (isLastClicked)
                {
                    selectFile = name_suffix;
                }

                if (openFileList.Contains(name_suffix))
                {
                    name_suffix += " (已打开)";
                }

                if (GUILayout.Button(name_suffix, isLastClicked ? EditorStyles.boldLabel : EditorStyles.label))
                {
                    OnFileClicked(filePath);
                }

                GUILayout.EndHorizontal();

                i++;
            }
            EditorGUILayout.EndScrollView();
            #endregion

            //分割线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.Height(6));

            #region sheet list
            if (!string.IsNullOrEmpty(inputStr))
            {
                foldSheets = true;
            }
            foldSheets = EditorGUILayout.Foldout(foldSheets, "Sheets");
            if (foldSheets)
            {
                sheetScrollPosition = EditorGUILayout.BeginScrollView(sheetScrollPosition, GUILayout.MaxHeight(position.height / 2));
                if (excelName2SheetNames.TryGetValue(selectFile, out var sheetNames))
                {
                    sheetNames = sheetNames.Replace(" , ", "\n");
                    GUILayout.Label(sheetNames, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndScrollView();
            }

            #endregion
        }

        private void SearchContent()
        {
            var searchStr = inputStr2.ToLower();
            var excelRoot = ConfigUtils.GetExcelRootPath();
            // 遍历excel目录下所有.xlsx文件，查找包含str的单元格，输出excel名、sheet名、行、列、内容
            var resultList = new List<string>();
            var files = Directory.GetFiles(excelRoot, "*.xlsx", SearchOption.AllDirectories);
            // 添加过程弹窗
            int totalFiles = files.Length;
            int currentFileIndex = 0;
            int currentSheetIndex = 0;
            bool cancelled = false;
            resultList.Add("<color=yellow> 搜索的内容是: <color=green>" + inputStr2 + "</color></color>");
            resultList.Add("--------------------------------");
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(
                    System.Text.RegularExpressions.Regex.Escape(searchStr),
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled
                );

                EditorUtility.DisplayProgressBar("查找Excel内容", "正在查找包含内容的单元格...", 0f);
                foreach (var file in files)
                {
                    currentFileIndex++;
                    float progress = (float)currentFileIndex / totalFiles;
                    string excelName = Path.GetFileName(file);
                    // 检查用户是否点击了取消
                    if (EditorUtility.DisplayCancelableProgressBar($"查找Excel内容({currentFileIndex}/{totalFiles})", $"正在查找: {excelName}", progress))
                    {
                        cancelled = true;
                        break;
                    }

                    try
                    {
                        using (ExcelPackage package = new ExcelPackage(new System.IO.FileInfo(file)))
                        {
                            currentSheetIndex = 0;
                            foreach (var worksheet in package.Workbook.Worksheets)
                            {
                                currentSheetIndex++;
                                if (worksheet == null) continue;

                                string sheetName = worksheet.Name;
                                if (EditorUtility.DisplayCancelableProgressBar($"查找Excel内容({currentFileIndex}/{totalFiles})", $"正在查找: {excelName}({currentSheetIndex}/{package.Workbook.Worksheets.Count}) - {sheetName}", progress))
                                {
                                    cancelled = true;
                                    break;
                                }

                                int rowStart = worksheet.Dimension?.Start.Row ?? 1;
                                int rowEnd = worksheet.Dimension?.End.Row ?? 0;
                                int colStart = worksheet.Dimension?.Start.Column ?? 1;
                                int colEnd = worksheet.Dimension?.End.Column ?? 0;

                                for (int r = rowStart; r <= rowEnd; r++)
                                {
                                    for (int c = colStart; c <= colEnd; c++)
                                    {
                                        var cell = worksheet.Cells[r, c];
                                        if (cell == null || cell.Value == null) continue;
                                        string cellValue = cell.Value.ToString();
                                        if (string.IsNullOrEmpty(cellValue)) continue;
                                        if (cellValue.ToLower().Contains(searchStr))
                                        {
                                            int rowNum = r; // already 1-based
                                            int colNum = c; // already 1-based
                                            string colLetter = GetExcelColumnName(colNum);
                                            //给匹配的字符串添加颜色 不使用Replace是因为包含 符号:#内容 匹配不到
                                            string cellValueStr = regex.Replace(cellValue, "<color=green>" + searchStr + "</color>");
                                            string info = $"{excelName}\t{sheetName}\t行:{rowNum}\t列:{colLetter}\t{cellValueStr}";
                                            resultList.Add(info);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"读取Excel文件失败: {file} {ex.Message}");
                    }

                    if (cancelled)
                    {
                        break;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (cancelled)
            {
                UnityEngine.Debug.Log("查找已取消。");
            }
            resultList.Add("--------------------------------");
            if (resultList.Count == 0)
            {
                UnityEngine.Debug.Log("未找到包含内容的单元格");
            }
            else
            {
                string output = string.Join("\n", resultList);
                UnityEngine.Debug.Log(output);
            }
        }

        // 使用A~Z的字母表示列号
        private string GetExcelColumnName(int colNum)
        {
            string colLetter = "";
            while (colNum > 0)
            {
                int remainder = (colNum - 1) % 26;
                colLetter = (char)('A' + remainder) + colLetter;
                colNum = (colNum - 1) / 26;
            }
            return colLetter;
        }

        // 获取文件夹下的.xlsx文件列表
        private void RefreshFileList()
        {
            openFileList.Clear();
            fileList.Clear();
            folderPath = ConfigUtils.GetExcelRootPath();
            fileList = ConfigUtils.GetFiles(folderPath, "*.xlsx");

            //过滤掉.开头的临时文件
            fileList = fileList.Where((string item) =>
            {
                var arr = item.Split('/');
                var str = arr[arr.Length - 1];
                var hasTemporary = str.StartsWith(".~") || str.StartsWith("~$");
                if (hasTemporary)
                {
                    var name = Path.GetFileName(item);
                    name = name.Replace(".~", "");//mac
                    name = name.Replace("~$", "");//window
                    openFileList.Add(name);
                }
                return !hasTemporary;
            }).ToList();
        }

        // 处理单击文件事件
        private void OnFileClicked(string filePath)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (/* currentTime - lastClickTime <= 0.5 && */ lastClickedFilePath == filePath)
            {
                OpenFile(filePath);
            }
            lastClickTime = currentTime;
            lastClickedFilePath = filePath;
        }

        // 打开文件
        private void OpenFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                // Application.OpenURL("file://" + filePath);
                UnityEngine.Debug.Log("打开：" + filePath);
                System.Diagnostics.Process.Start(filePath);
            }
        }

        private static void Generate_Mac()
        {
            string root = Application.dataPath + "/../";
            string path = root + "Library/GameTableDataLocalPath/conf/generate_config.sh";
            //导出
            ExecCommand("/bin/bash", path);
            //同步导出文件
            // MenuEntry.SyncConfigData();
        }

        public static void ExecCommand(string fileName, string path)
        {
            Process process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = path;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true; // 打开终端窗口
            process.Start();

            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            int exit_code = process.ExitCode;

            process.Close();

            string color = exit_code == 0 ? "#00E31F" : "#E20003";

            UnityEngine.Debug.Log(output);
            UnityEngine.Debug.Log(string.Format("<color={0}>执行完成，状态码: {1} </color>", color, exit_code));

            if (exit_code != 0)
            {
                EditorUtility.DisplayDialog(string.Format("错误 code:{0}", exit_code), "详情见Log !!!。\n可再次尝试，多次错误请检查excel文件。", "确定");
            }
        }
    }
}
