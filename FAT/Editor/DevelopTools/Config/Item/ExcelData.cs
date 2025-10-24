using UnityEngine;
using UnityEditor;
using OfficeOpenXml;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

namespace DevelopTools
{
    /*
        如果数据设置不成功，则需要重置单元格格式。
        操作方式如：赋值单元格文本后选择清除单元格，在把数值粘贴过来
     */
    public class ExcelData
    {
        public readonly static int RowIndexOffset = 4;//前四行是属性说明(固定结构)

        protected string excel_excelName; //配置名
        protected string excel_sheetName; //表名
        protected string sheetName; //表名

        protected int maxRow = 0; //最大行数
        protected int maxCol = 0; //最大列数
        protected int maxId = 0; //最大id
        protected string sheetNote; //表批注

        protected Dictionary<string, int> idToRow = new Dictionary<string, int>();//id对应的行(row)的索引(从1开始)
        protected Dictionary<string, int> attributeToCol = new Dictionary<string, int>(); //属性名对应的列(col)的索引(从1开始)
        protected Dictionary<string, string> attributeToType = new Dictionary<string, string>();//属性名对应的值类型
        protected Dictionary<string, string> attributeToDesc = new Dictionary<string, string>();//属性名对应的描述
        protected Dictionary<string, string> attributeToNote = new Dictionary<string, string>();//属性名（中文）上的批注
        protected Dictionary<int, Dictionary<int, string>> values = new Dictionary<int, Dictionary<int, string>>(); //属性值 {[row]:{[col]:string}}

        protected Dictionary<int, Dictionary<int, string>> valuesNew = new Dictionary<int, Dictionary<int, string>>(); //新属性名{[row]:{[col]:string}}
        protected Dictionary<int, Dictionary<int, System.Drawing.Color>> valuesColor = new Dictionary<int, Dictionary<int, System.Drawing.Color>>(); //属性颜色值{[row]:{[col]:Color}}

        public ExcelData(string excellName, string sheetName, bool initData = true)
        {
            excel_excelName = excellName;
            excel_sheetName = sheetName;
            if (initData)
            {
                ReadExcel();
            }
        }

        public string GetExcelName()
        {
            return excel_excelName;
        }

        public void Log(string message)
        {
            Debug.Log(string.Format("[{0}_{1}]:{2}", excel_excelName, excel_sheetName, message));
        }

        // 是否被系统打开 window打开的时候无法进行写入
        public bool InOpenState()
        {
            //大概率 mac: ".~" window: "~$" 是临时文件
            string excelPath_1 = ConfigUtils.GetExcelPathByName("~$" + excel_excelName);
            string excelPath_2 = ConfigUtils.GetExcelPathByName(".~" + excel_excelName);
            if (File.Exists(excelPath_1) || File.Exists(excelPath_2))
            {
                return true;
            }
            return false;
        }

        //打开文件
        public void OpenFile()
        {
            string excelPath = ConfigUtils.GetExcelPathByName(excel_excelName);
            if (File.Exists(excelPath))
            {

                UnityEngine.Debug.Log("打开：" + excelPath);
                Application.OpenURL("file://" + excelPath);
            }
        }

        public void Clear()
        {
            attributeToCol.Clear();
            attributeToType.Clear();
            attributeToDesc.Clear();
            attributeToNote.Clear();
            idToRow.Clear();
            values.Clear();
            valuesNew.Clear();
            maxId = 0;
            maxRow = 0;
            maxCol = 0;
        }

        public void ReadExcel()
        {
            string excelPath = ConfigUtils.GetExcelPathByName(excel_excelName);
            using (ExcelPackage package = new ExcelPackage(new System.IO.FileInfo(excelPath)))
            {
                ExcelWorksheet sheet = ConfigUtils.GetWorksheetByExcelPath(package, excel_sheetName);

                //Debug.Log(excel_excelName + " - " + excel_sheetName);

                int offset = RowIndexOffset; //前四行是属性说明(固定结构)
                int row_start = sheet.Dimension.Start.Row;   //行 起始索引
                int row_end = sheet.Dimension.End.Row;       //行 结束索引
                int col_start = sheet.Dimension.Start.Column;//列 起始索引
                int col_end = sheet.Dimension.End.Column;    //列 结束索引

                maxRow = row_end;
                maxCol = col_end;

                int i = 0;

                //表名和批注
                var cell_sheet = sheet.Cells[4, 1];
                sheetName = cell_sheet != null && cell_sheet.Value != null ? cell_sheet.Value.ToString() : "";
                sheetNote = cell_sheet != null && cell_sheet.Comment != null ? cell_sheet.Comment.ToString() : "";

                //收集属性名和属性类型
                for (i = col_start; i <= col_end; i++)
                {
                    var cell_name = sheet.Cells[1, i];
                    var cell_type = sheet.Cells[2, i];
                    var cell_desc = sheet.Cells[3, i];

                    var value_name = cell_name != null && cell_name.Value != null ? cell_name.Value.ToString() : "";
                    var value_type = cell_type != null && cell_type.Value != null ? cell_type.Value.ToString() : "";
                    var value_desc = cell_desc != null && cell_desc.Value != null ? cell_desc.Value.ToString() : "";
                    var value_note = cell_desc != null && cell_desc.Comment != null ? cell_desc.Comment.ToString() : "";


                    if (!attributeToCol.ContainsKey(value_name))
                    {
                        attributeToCol.Add(value_name, i);
                    }

                    if (!attributeToType.ContainsKey(value_name))
                    {
                        attributeToType.Add(value_name, value_type);
                    }

                    if (!attributeToDesc.ContainsKey(value_name))
                    {
                        attributeToDesc.Add(value_name, value_desc);
                    }

                    if (!attributeToNote.ContainsKey(value_name))
                    {
                        attributeToNote.Add(value_name, value_note);
                    }
                }

                //收集id信息
                for (i = offset + 1; i <= row_end; i++)
                {
                    var cell_id = sheet.Cells[i, 1];
                    if (cell_id == null || cell_id.Value == null || string.IsNullOrEmpty(cell_id.Value.ToString()))
                    {
                        continue;
                    }
                    var idstr = cell_id.Value.ToString();
                    if (!idToRow.ContainsKey(idstr))
                    {
                        idToRow.Add(idstr, i);
                        int id;
                        if (int.TryParse(idstr, out id))
                        {
                            if (id > maxId)
                            {
                                maxId = id;
                            }
                        }
                    }

                    //有id的数据进行属性收集
                    Dictionary<int, string> info;
                    int row = i;
                    if (!values.TryGetValue(row, out info))
                    {
                        info = new Dictionary<int, string>();
                        values.Add(row, info);
                    }
                    foreach (var item in attributeToCol)
                    {
                        var col = item.Value;
                        var cell = sheet.Cells[row, col];
                        if (info.ContainsKey(col))
                        {
                            continue;
                        }
                        if (cell != null && cell.Value != null)
                        {
                            info.Add(col, cell.Value.ToString());
                        }
                        else
                        {
                            info.Add(col, "");
                        }
                    }
                }

            }

        }

        /// <summary>
        /// 是否存在指定id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool HasId(string id)
        {
            return idToRow.ContainsKey(id);
        }

        /// <summary>
        /// 指定id所在行
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int GetRow(string id)
        {
            return idToRow[id];
        }

        /// <summary>
        /// 获取新增数据所在行
        /// </summary>
        /// <returns></returns>
        public int GetRowByNew()
        {
            return maxRow + 1;
        }

        /// <summary>
        /// 属性名所在列
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public int GetCol(string attribute)
        {
            return attributeToCol[attribute];
        }

        /// <summary>
        /// 根据id和属性名获取值
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="name">属性名</param>
        /// <returns></returns>
        public string GetValue(int id, string name)
        {
            return GetValue(id.ToString(), name);
        }

        public string GetValue(string id, string name)
        {
            if (!idToRow.TryGetValue(id, out int row) || !attributeToCol.TryGetValue(name, out int col))
            {
                return "";
            }

            return GetValue(row, col);
        }

        public string GetValue(int row, int col)
        {
            try
            {
                var value = values[row][col];
                return value;
            }
            catch (System.Exception ex)
            {
                return "";
            }
        }

        public string GetValueNew(string id, string name, bool isNew = false)
        {
            if (!idToRow.TryGetValue(id, out int row))
            {
                if (!isNew)
                {
                    return "";
                }
                row = GetRowByNew();
            }

            if (!attributeToCol.TryGetValue(name, out int col))
            {
                return "";
            }

            try
            {
                var value = valuesNew[row][col];
                return value;
            }
            catch (System.Exception ex)
            {
                return "";
            }
        }


        /// <summary>
        /// 根据id删除行数据
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="resetData">删除后是否更新缓存数据</param>
        public void DeleteIds(List<string> ids, bool resetData = true)
        {
            if (ids == null || ids.Count == 0)
            {
                return;
            }
            string excelPath = ConfigUtils.GetExcelPathByName(excel_excelName);
            using (ExcelPackage package = new ExcelPackage(new System.IO.FileInfo(excelPath)))
            {
                ExcelWorksheet sheet = ConfigUtils.GetWorksheetByExcelPath(package, excel_sheetName);
                List<int> rows = new List<int>();
                foreach (var id in ids)
                {
                    if (idToRow.TryGetValue(id, out int row))
                    {
                        rows.Add(row);
                        Log("删除Id:" + id);
                    }
                }
                rows.Sort();
                rows.Reverse();
                foreach (var row in rows)
                {
                    try
                    {
                        sheet.DeleteRow(row);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.Log(ex);
                    }
                }
                package.Save();
            }
            if (resetData)
            {
                Clear();
                ReadExcel();
            }
        }

        /// <summary>
        /// 获取id列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetIds()
        {
            return new List<string>(idToRow.Keys);
        }

        public string[] GetIdArr()
        {
            return idToRow.Keys.ToArray();
        }

        /// <summary>
        /// 获取最大Id
        /// </summary>
        /// <returns></returns>
        public int GetMaxId()
        {
            return maxId;
        }

        //获取属性列表
        public string[] GetAttributes()
        {
            return attributeToCol.Keys.ToArray();
        }

        /// <summary>
        /// 根据属性值获取id列表
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public List<string> GetIdsByValue(string attribute, string value)
        {
            var result = new List<string>();
            var ids = GetIds();
            foreach (var id in ids)
            {
                var temp = GetValue(id, attribute);
                if (temp == value)
                {
                    result.Add(id);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取某个属性的值列表
        /// </summary>
        /// <param name="name">属性名</param>
        /// <returns></returns>
        public List<string> GetValueList(string name)
        {
            var list = new List<string>();
            var col = attributeToCol[name];

            foreach (var item in values)
            {
                list.Add(item.Value[col]);
            }

            return list;
        }

        /// <summary>
        /// 某个值是否在指定属性列表里
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="name">属性名</param>
        /// <returns></returns>
        public bool InValueList(string value, string name)
        {
            var col = attributeToCol[name];

            foreach (var item in values)
            {
                if (item.Value[col] == value)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 根据属性名删除列数据
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="resetData">删除后是否更新缓存数据</param>
        public void DeleteAttribute(List<string> attributes, bool resetData = true)
        {
            if (attributes == null || attributes.Count == 0)
            {
                return;
            }
            string excelPath = ConfigUtils.GetExcelPathByName(excel_excelName);
            using (ExcelPackage package = new ExcelPackage(new System.IO.FileInfo(excelPath)))
            {
                ExcelWorksheet sheet = ConfigUtils.GetWorksheetByExcelPath(package, excel_sheetName);
                List<int> cols = new List<int>();
                foreach (var attribute in attributes)
                {
                    if (attributeToCol.TryGetValue(attribute, out int col))
                    {
                        cols.Add(col);
                        Log("删除属性:" + attribute);
                    }
                }
                cols.Sort();
                cols.Reverse();
                foreach (var col in cols)
                {
                    sheet.DeleteRow(col);
                }
                package.Save();
            }
            if (resetData)
            {
                Clear();
                ReadExcel();
            }
        }

        /// <summary>
        /// 设置某个id的所有属性值为指定颜色
        /// </summary>
        /// <param name="id"></param>
        /// <param name="color"></param>
        public void SetColor(string id, System.Drawing.Color color)
        {
            var attributes = attributeToCol.Keys;
            foreach (var attribute in attributes)
            {
                SetColor(id, attribute, color);
            }
        }

        /// <summary>
        /// 设置某个id的某个属性值为指定颜色
        /// </summary>
        /// <param name="id"></param>
        /// <param name="attributeName"></param>
        /// <param name="color"></param>
        public void SetColor(string id, string attributeName, System.Drawing.Color color)
        {
            var value = GetValueNew(id, attributeName);
            if (string.IsNullOrEmpty(value))
            {
                value = GetValue(id, attributeName);
            }
            //设置一下值是因为只有 value有过设置行为才会触发储存逻辑
            SetValue(id, attributeName, value);

            //缓存颜色值
            var row = GetRow(id);
            var col = GetCol(attributeName);
            if (!valuesColor.TryGetValue(row, out var colValues))
            {
                colValues = new Dictionary<int, System.Drawing.Color>();
                valuesColor.Add(row, colValues);
            }
            colValues[col] = color;
        }

        public void SetColor(ExcelRange cell, System.Drawing.Color color)
        {
            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(color);
        }

        /// <summary>
        /// 设置单元格数据
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="attributeName">属性名</param>
        /// <param name="value">新值</param>
        public void SetValue(string id, string attributeName, string value, bool isNew = false)
        {
            var row = isNew ? GetRowByNew() : GetRow(id);
            var col = GetCol(attributeName);
            SetValue(row, col, value);
        }

        /// <summary>
        /// 设置单元格新数据
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="col">列</param>
        /// <param name="value"></param>
        public void SetValue(int row, int col, string value)
        {
            if (!valuesNew.TryGetValue(row, out var colValues))
            {
                colValues = new Dictionary<int, string>();
                valuesNew.Add(row, colValues);
            }

            colValues[col] = value;
        }

        /// <summary>
        /// 清除变化
        /// </summary>
        public virtual void ClearChange()
        {
            valuesNew.Clear();
            valuesColor.Clear();
        }

        /// <summary>
        /// 是否有数据变化
        /// </summary>
        /// <returns></returns>
        public bool HasChange()
        {
            foreach (var colValues in valuesNew.Values)
            {
                if (colValues.Values.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 储存缓存数据
        /// </summary>
        public void SaveChange(bool resetData = true)
        {
            if (!HasChange())
            {
                return;
            }

            //修改数据
            string excelPath = ConfigUtils.GetExcelPathByName(excel_excelName);
            using (ExcelPackage package = new ExcelPackage(new System.IO.FileInfo(excelPath)))
            {
                ExcelWorksheet sheet = ConfigUtils.GetWorksheetByExcelPath(package, excel_sheetName);
                foreach (var colValues in valuesNew)
                {
                    var row = colValues.Key;
                    foreach (var colInfo in colValues.Value)
                    {
                        var col = colInfo.Key;
                        var cell = sheet.Cells[row, col];
                        var oldValue = GetValue(row, col);
                        var newValue = colInfo.Value;
                        //外界颜色值优先
                        var signColor = false;
                        {
                            System.Drawing.Color color = default;
                            if (valuesColor.TryGetValue(row, out Dictionary<int, System.Drawing.Color> innerDict))
                            {
                                innerDict.TryGetValue(col, out color);
                            }
                            if (!color.Equals(default))
                            {
                                SetColor(cell, color);
                                signColor = true;
                            }
                        }
                        //设置新的数值和颜色
                        if (oldValue != newValue)
                        {
                            cell.Value = newValue;
                            if (!signColor)
                            {
                                //新增 绿色
                                if (row > maxRow)
                                {
                                    SetColor(cell, System.Drawing.Color.Lime);
                                }
                                //变化 黄色
                                else
                                {
                                    SetColor(cell, System.Drawing.Color.Moccasin);
                                }
                            }
                        }
                    }
                }
                ClearChange();
                package.Save();
            }

            if (resetData)
            {
                Clear();
                ReadExcel();
            }
        }
    }
}

