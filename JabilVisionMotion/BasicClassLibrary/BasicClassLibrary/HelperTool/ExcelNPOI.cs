using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 利用NPOI库操作Excel，无需安装Excel组件
    /// </summary>
    public class ExcelNPOI
    {
        private ISheet m_sheet = null;
        /// <summary>
        /// Excel文件名称
        /// </summary>
        private string m_strFilename;
        private IWorkbook m_workbook = null;

        /// <summary>
        /// 添加一个工作表
        /// </summary>
        /// <param name="strSheetName"></param>
        /// <returns></returns>
        public ISheet AddSheet(string strSheetName)
        {
            if (this.m_workbook != null)
            {
                ISheet sheet = this.m_workbook.GetSheet(strSheetName);
                if (sheet == null)
                {
                    sheet = this.m_workbook.CreateSheet(strSheetName);
                }
                return sheet;
            }
            return null;
        }

        /// <summary>
        /// 将内存中数据表格添加到Microsoft.Office.Interop.Excel指定工作表的指定位置二
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="sheet"></param>
        /// <param name="startRow"></param>
        /// <param name="startCol"></param>
        public static void AddTable(DataTable dt, ISheet sheet, int startRow, int startCol)
        {
            if (sheet != null)
            {
                for (int i = 0; i <= (dt.Rows.Count - 1); i++)
                {
                    IRow row = sheet.GetRow(startRow + i);
                    if (row == null)
                    {
                        row = sheet.CreateRow(startRow + i);
                    }
                    for (int j = 0; j <= (dt.Columns.Count - 1); j++)
                    {
                        ICell cell = row.GetCell(startCol + j);
                        if (cell == null)
                        {
                            cell = row.GetCell(startCol + j);
                        }
                        SetCellValue(cell, dt.Rows[i][j]);
                    }
                }
            }
        }

        /// <summary>
        /// 将内存中数据表格添加到Microsoft.Office.Interop.Excel指定工作表的指定位置一
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="strSheetName"></param>
        /// <param name="startRow"></param>
        /// <param name="startCol"></param>
        public void AddTable(DataTable dt, string strSheetName, int startRow, int startCol)
        {
            ISheet sheet = this.GetSheet(strSheetName);
            AddTable(dt, sheet, startRow, startCol);
        }

        /// <summary>
        /// 把DataGridView转换成DataTable
        /// </summary>
        /// <param name="dgv"></param>
        /// <returns></returns>
        public static DataTable DataGridViewToDataTable(DataGridView dgv)
        {
            DataTable table = new DataTable();
            for (int i = 0; i < dgv.Columns.Count; i++)
            {
                DataColumn column = new DataColumn(dgv.Columns[i].Name.ToString());
                table.Columns.Add(column);
            }
            for (int j = 0; j < dgv.Rows.Count; j++)
            {
                DataRow row = table.NewRow();
                for (int k = 0; k < dgv.Columns.Count; k++)
                {
                    row[k] = Convert.ToString(dgv.Rows[j].Cells[k].Value);
                }
                table.Rows.Add(row);
            }
            return table;
        }

        /// <summary>
        /// 将DataTable数据导入到excel中
        /// </summary>
        /// <param name="data">要导入的数据</param>
        /// <param name="fileName">写入的目标Excel的完整名称</param>
        /// <param name="isColumnWritten">DataTable的列名是否要导入</param>
        /// <param name="sheetName">要导入的excel的sheet的名称</param>
        /// <param name="bAppend">是否追加</param>
        /// <returns>导入数据行数(包含列名那一行)</returns>
        public static int DataTableToExcel(DataTable data, string fileName, string sheetName, bool isColumnWritten, bool bAppend)
        {
            int rownum = 0;
            ISheet sheet = null;
            IWorkbook workbook = null;
            try
            {
                FileMode openOrCreate = FileMode.OpenOrCreate;
                FileAccess readWrite = FileAccess.ReadWrite;
                if (!File.Exists(fileName))
                {
                    bAppend = false;
                }
                else if (bAppend)
                {
                    isColumnWritten = false;
                }
                if (fileName.IndexOf(".xlsx") > 0)
                {
                    if (!bAppend)
                    {
                        workbook = new XSSFWorkbook();
                    }
                    else
                    {
                        using (FileStream stream = new FileStream(fileName, openOrCreate, readWrite))
                        {
                            workbook = new XSSFWorkbook(stream);
                        }
                    }
                }
                else if (fileName.IndexOf(".xls") > 0)
                {
                    if (!bAppend)
                    {
                        workbook = new HSSFWorkbook();
                    }
                    else
                    {
                        using (FileStream stream2 = new FileStream(fileName, openOrCreate, readWrite))
                        {
                            workbook = new HSSFWorkbook(stream2);
                        }
                    }
                }
                if (workbook != null)
                {
                    sheet = workbook.GetSheet(sheetName);
                    if (sheet == null)
                    {
                        sheet = workbook.CreateSheet(sheetName);
                    }
                }
                else
                {
                    return -1;
                }
                if (isColumnWritten)
                {
                    IRow row = sheet.CreateRow(0);
                    for (int j = 0; j < data.Columns.Count; j++)
                    {
                        row.CreateCell(j).SetCellValue(data.Columns[j].ColumnName);
                    }
                    rownum = 1;
                }
                else
                {
                    rownum = 0;
                    if (bAppend)
                    {
                        rownum = sheet.LastRowNum;
                    }
                }
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    IRow row2 = sheet.GetRow(rownum);
                    if (row2 == null)
                    {
                        row2 = sheet.CreateRow(rownum);
                    }
                    for (int k = 0; k < data.Columns.Count; k++)
                    {
                        ICell cell = row2.GetCell(k);
                        if (cell == null)
                        {
                            row2.CreateCell(k);
                        }
                        SetCellValue(cell, data.Rows[i][k]);
                    }
                    rownum++;
                }
                using (FileStream stream3 = File.OpenWrite(fileName))
                {
                    workbook.Write(stream3);
                    workbook.Close();
                }
                return rownum;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Exception: " + exception.Message);
                return -1;
            }
        }

        /// <summary>
        /// 从某行某列开始写数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="fileName"></param>
        /// <param name="sheetName"></param>
        /// <param name="startRow"></param>
        /// <param name="startCol"></param>
        /// <returns></returns>
        public static int DataTableToExcel(DataTable data, string fileName, string sheetName, int startRow, int startCol)
        {
            int num = 0;
            ISheet sheet = null;
            IWorkbook workbook = null;
            try
            {
                FileMode openOrCreate = FileMode.OpenOrCreate;
                FileAccess readWrite = FileAccess.ReadWrite;
                bool flag = File.Exists(fileName);
                if (fileName.IndexOf(".xlsx") > 0)
                {
                    if (!flag)
                    {
                        workbook = new XSSFWorkbook();
                    }
                    else
                    {
                        using (FileStream stream = new FileStream(fileName, openOrCreate, readWrite))
                        {
                            workbook = new XSSFWorkbook(stream);
                        }
                    }
                }
                else if (fileName.IndexOf(".xls") > 0)
                {
                    if (!flag)
                    {
                        workbook = new HSSFWorkbook();
                    }
                    else
                    {
                        using (FileStream stream2 = new FileStream(fileName, openOrCreate, readWrite))
                        {
                            workbook = new HSSFWorkbook(stream2);
                        }
                    }
                }
                if (workbook != null)
                {
                    sheet = workbook.GetSheet(sheetName);
                    if (sheet == null)
                    {
                        sheet = workbook.CreateSheet(sheetName);
                    }
                }
                else
                {
                    return -1;
                }
                if (!flag)
                {
                    IRow row = sheet.CreateRow(0);
                    for (int j = 0; j < data.Columns.Count; j++)
                    {
                        row.CreateCell(j + startCol).SetCellValue(data.Columns[j].ColumnName);
                    }
                    num = 1;
                }
                else
                {
                    num = 0;
                }
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    IRow row2 = sheet.GetRow(startRow + i);
                    if (row2 == null)
                    {
                        row2 = sheet.CreateRow(startRow + i);
                    }
                    for (int k = 0; k < data.Columns.Count; k++)
                    {
                        ICell cell = row2.GetCell(k + startCol);
                        if (cell == null)
                        {
                            cell = row2.CreateCell(k + startCol);
                        }
                        object obj2 = data.Rows[i][k];
                        SetCellValue(cell, obj2);
                    }
                    num++;
                }
                using (FileStream stream3 = File.OpenWrite(fileName))
                {
                    workbook.Write(stream3);
                    workbook.Close();
                }
                return num;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Exception: " + exception.Message);
                return -1;
            }
        }

        /// <summary>
        /// 删除一个工作表
        /// </summary>
        /// <param name="strSheetName"></param>
        public void DelSheet(string strSheetName)
        {
            if (this.m_workbook != null)
            {
                int sheetIndex = this.m_workbook.GetSheetIndex(strSheetName);
                if (sheetIndex >= 0)
                {
                    this.m_workbook.RemoveSheetAt(sheetIndex);
                }
            }
        }

        /// <summary>
        /// 将excel中的数据导入到DataTable中
        /// </summary>
        /// <param name="fileName">读取的Excel的完整名称</param>
        /// <param name="sheetName">excel工作薄sheet的名称</param>
        /// <param name="isFirstRowColumn">第一行是否是DataTable的列名</param>
        /// <returns>返回的DataTable</returns>
        public static DataTable ExcelToDataTable(string fileName, string sheetName, bool isFirstRowColumn)
        {
            ISheet sheetAt = null;
            DataTable table2;
            DataTable table = new DataTable();
            int firstRowNum = 0;
            try
            {
                IWorkbook workbook = null;
                using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    if (fileName.IndexOf(".xlsx") > 0)
                    {
                        workbook = new XSSFWorkbook(stream);
                    }
                    else if (fileName.IndexOf(".xls") > 0)
                    {
                        workbook = new HSSFWorkbook(stream);
                    }
                    if (sheetName != null)
                    {
                        sheetAt = workbook.GetSheet(sheetName);
                        if (sheetAt == null)
                        {
                            sheetAt = workbook.GetSheetAt(0);
                        }
                    }
                    else
                    {
                        sheetAt = workbook.GetSheetAt(0);
                    }
                    if (sheetAt != null)
                    {
                        IRow row = sheetAt.GetRow(0);
                        int lastCellNum = row.LastCellNum;
                        if (isFirstRowColumn)
                        {
                            for (int j = row.FirstCellNum; j < lastCellNum; j++)
                            {
                                ICell cell = row.GetCell(j);
                                if (cell != null)
                                {
                                    string stringCellValue = cell.StringCellValue;
                                    if (stringCellValue != null)
                                    {
                                        DataColumn column = new DataColumn(stringCellValue);
                                        table.Columns.Add(column);
                                    }
                                }
                            }
                            firstRowNum = sheetAt.FirstRowNum + 1;
                        }
                        else
                        {
                            firstRowNum = sheetAt.FirstRowNum;
                        }
                        int lastRowNum = sheetAt.LastRowNum;
                        for (int i = firstRowNum; i <= lastRowNum; i++)
                        {
                            IRow row2 = sheetAt.GetRow(i);
                            if (row2 != null)
                            {
                                DataRow row3 = table.NewRow();
                                for (int k = row2.FirstCellNum; k < lastCellNum; k++)
                                {
                                    if (row2.GetCell(k) != null)
                                    {
                                        row3[k] = row2.GetCell(k).ToString();
                                    }
                                }
                                table.Rows.Add(row3);
                            }
                        }
                    }
                    table2 = table;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("Exception: " + exception.Message);
                table2 = null;
            }
            return table2;
        }

        /// <summary>
        /// 把Excel的stream导入到表
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="fileName"></param>
        /// <param name="sheetName"></param>
        /// <param name="isFirstRowColumn"></param>
        /// <returns></returns>
        public static DataTable ExcelToDataTable(Stream fs, string fileName, string sheetName, bool isFirstRowColumn)
        {
            ISheet sheetAt = null;
            DataTable table = new DataTable();
            int firstRowNum = 0;
            try
            {
                IWorkbook workbook = null;
                if (fileName.IndexOf(".xlsx") > 0)
                {
                    workbook = new XSSFWorkbook(fs);
                }
                else if (fileName.IndexOf(".xls") > 0)
                {
                    workbook = new HSSFWorkbook(fs);
                }
                if (sheetName != null)
                {
                    sheetAt = workbook.GetSheet(sheetName);
                    if (sheetAt == null)
                    {
                        sheetAt = workbook.GetSheetAt(0);
                    }
                }
                else
                {
                    sheetAt = workbook.GetSheetAt(0);
                }
                if (sheetAt != null)
                {
                    IRow row = sheetAt.GetRow(0);
                    int lastCellNum = row.LastCellNum;
                    if (isFirstRowColumn)
                    {
                        for (int j = row.FirstCellNum; j < lastCellNum; j++)
                        {
                            ICell cell = row.GetCell(j);
                            if (cell != null)
                            {
                                string stringCellValue = cell.StringCellValue;
                                if (stringCellValue != null)
                                {
                                    DataColumn column = new DataColumn(stringCellValue);
                                    table.Columns.Add(column);
                                }
                            }
                        }
                        firstRowNum = sheetAt.FirstRowNum + 1;
                    }
                    else
                    {
                        firstRowNum = sheetAt.FirstRowNum;
                    }
                    int lastRowNum = sheetAt.LastRowNum;
                    for (int i = firstRowNum; i <= lastRowNum; i++)
                    {
                        IRow row2 = sheetAt.GetRow(i);
                        if (row2 != null)
                        {
                            DataRow row3 = table.NewRow();
                            for (int k = row2.FirstCellNum; k < lastCellNum; k++)
                            {
                                if (row2.GetCell(k) != null)
                                {
                                    row3[k] = row2.GetCell(k).ToString();
                                }
                            }
                            table.Rows.Add(row3);
                        }
                    }
                }
                return table;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Exception: " + exception.Message);
                return null;
            }
        }

        /// <summary>
        /// 获取单元格值
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public static object GetCellValue(ICell cell)
        {
            if (cell == null)
            {
                return "";
            }
            switch (cell.CellType)
            {
                case CellType.Numeric:
                    if (!DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.NumericCellValue;
                    }
                    return cell.DateCellValue;

                case CellType.String:
                    return cell.StringCellValue;

                case CellType.Boolean:
                    return cell.BooleanCellValue;
            }
            return cell.ToString();
        }

        /// <summary>
        /// 获取单元格值
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static object GetCellValue(ISheet sheet, int row, int col)
        {
            IRow row2 = sheet.GetRow(row);
            if (row2 != null)
            {
                ICell cell = row2.GetCell(col);
                if (cell != null)
                {
                    return GetCellValue(cell);
                }
            }
            return null;
        }

        /// <summary>
        /// 获取单元格值
        /// </summary>
        /// <param name="strSheetName"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public object GetCellValue(string strSheetName, int row, int col)
        {
            ISheet sheet = this.GetSheet(strSheetName);
            if (sheet != null)
            {
                return GetCellValue(sheet, row, col);
            }
            return null;
        }

        /// <summary>
        /// 获取工作表
        /// </summary>
        /// <param name="strSheetName"></param>
        /// <returns></returns>
        public ISheet GetSheet(string strSheetName)
        {
            if (this.m_workbook != null)
            {
                return this.m_workbook.GetSheet(strSheetName);
            }
            return null;
        }

        /// <summary>
        /// 将内存中数据表格插入到Microsoft.Office.Interop.Excel指定工作表的指定位置二
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="sheet"></param>
        /// <param name="startRow"></param>
        /// <param name="startCol"></param>
        public static void InsertTable(DataTable dt, ISheet sheet, int startRow, int startCol)
        {
            if (sheet != null)
            {
                sheet.ShiftRows(startRow, startRow + 1, dt.Rows.Count);
                for (int i = 0; i <= (dt.Rows.Count - 1); i++)
                {
                    IRow row = sheet.GetRow(startRow + i);
                    if (row == null)
                    {
                        row = sheet.CreateRow(startRow + i);
                    }
                    for (int j = 0; j <= (dt.Columns.Count - 1); j++)
                    {
                        ICell cell = row.GetCell(startCol + j);
                        if (cell == null)
                        {
                            cell = row.CreateCell(startCol + j);
                        }
                        SetCellValue(cell, dt.Rows[i][j]);
                    }
                }
            }
        }

        /// <summary>
        /// 将内存中数据表格插入到l指定工作表的指定位置 为在使用模板时控制格式时使用一
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="strSheetName"></param>
        /// <param name="startRow"></param>
        /// <param name="startCol"></param>
        public void InsertTable(DataTable dt, string strSheetName, int startRow, int startCol)
        {
            ISheet sheet = this.GetSheet(strSheetName);
            InsertTable(dt, sheet, startRow, startCol);
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="bAppend"></param>
        public bool Open(string strFileName, bool bAppend = false)
        {
            try
            {
                this.m_strFilename = strFileName;
                if (File.Exists(strFileName) & bAppend)
                {
                    using (FileStream stream = new FileStream(strFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        if (strFileName.IndexOf(".xlsx") > 0)
                        {
                            this.m_workbook = new XSSFWorkbook(stream);
                        }
                        else if (strFileName.IndexOf(".xls") > 0)
                        {
                            this.m_workbook = new HSSFWorkbook(stream);
                        }
                    }
                }
                else if (strFileName.IndexOf(".xlsx") > 0)
                {
                    this.m_workbook = new XSSFWorkbook();
                }
                else if (strFileName.IndexOf(".xls") > 0)
                {
                    this.m_workbook = new HSSFWorkbook();
                }
                return (this.m_workbook != null);
            }
            catch (Exception exception)
            {
                this.m_workbook = null;
                MessageBox.Show(exception.Message);
            }
            return false;
        }

        /// <summary>
        /// 重命名一个工作表
        /// </summary>
        /// <param name="strOldSheetName"></param>
        /// <param name="strNewSheetName"></param>
        /// <returns></returns>
        public ISheet ReNameSheet(string strOldSheetName, string strNewSheetName)
        {
            if (this.m_workbook != null)
            {
                int sheetIndex = this.m_workbook.GetSheetIndex(strOldSheetName);
                if (sheetIndex >= 0)
                {
                    this.m_workbook.SetSheetName(sheetIndex, strNewSheetName);
                    return this.m_workbook.GetSheetAt(sheetIndex);
                }
            }
            return null;
        }

        /// <summary>
        /// 保存文档
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            if (this.m_strFilename == "")
            {
                return false;
            }
            try
            {
                using (FileStream stream = File.OpenWrite(this.m_strFilename))
                {
                    this.m_workbook.Write(stream);
                    this.m_workbook.Close();
                }
                return true;
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.Message);
                return false;
            }
        }

        /// <summary>
        /// 设置背景颜色
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="color"></param>
        public static void SetCellBackColor(ISheet sheet, int row, int col, ColorIndex color)
        {
            IRow row2 = sheet.GetRow(row);
            if (row2 == null)
            {
                row2 = sheet.CreateRow(row);
            }
            if ((row2 != null) && (sheet.Workbook != null))
            {
                ICellStyle style = sheet.Workbook.CreateCellStyle();
                style.FillForegroundColor = (short)color;
                style.FillPattern = FillPattern.SolidForeground;
                ICell cell = row2.GetCell(col);
                if (cell == null)
                {
                    cell = row2.CreateCell(col);
                }
                cell.CellStyle = style;
            }
        }

        /// <summary>
        /// 设置背景颜色
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="color"></param>
        public static void SetCellBackColor(ISheet sheet, int row, int col, HSSFColor color)
        {
            IRow row2 = sheet.GetRow(row);
            if (row2 == null)
            {
                row2 = sheet.CreateRow(row);
            }
            if ((row2 != null) && (sheet.Workbook != null))
            {
                ICellStyle style = sheet.Workbook.CreateCellStyle();
                style.FillForegroundColor = color.Indexed;
                style.FillPattern = FillPattern.SolidForeground;
                ICell cell = row2.GetCell(col);
                if (cell == null)
                {
                    cell = row2.CreateCell(col);
                }
                cell.CellStyle = style;
            }
        }

        /// <summary>
        /// 设置背景颜色
        /// </summary>
        /// <param name="strSheetName"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="color"></param>
        public void SetCellBackColor(string strSheetName, int row, int col, ColorIndex color)
        {
            ISheet sheet = this.GetSheet(strSheetName);
            if (sheet != null)
            {
                SetCellBackColor(sheet, row, col, color);
            }
        }

        /// <summary>
        /// 设置背景颜色
        /// </summary>
        /// <param name="strSheetName"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="color"></param>
        public void SetCellBackColor(string strSheetName, int row, int col, HSSFColor color)
        {
            ISheet sheet = this.GetSheet(strSheetName);
            if (sheet != null)
            {
                SetCellBackColor(sheet, row, col, color);
            }
        }

        /// <summary>
        /// 设置单元格值
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="value"></param>
        public static void SetCellValue(ICell cell, object value)
        {
            System.Type type = value.GetType();
            if (((((type == typeof(double)) || (type == typeof(float))) || ((type == typeof(int)) || (type == typeof(long)))) || (((type == typeof(short)) || (type == typeof(uint))) || (type == typeof(ulong)))) || (type == typeof(ushort)))
            {
                cell.SetCellValue(Convert.ToDouble(value));
            }
            else if (type == typeof(DateTime))
            {
                cell.SetCellValue(Convert.ToDateTime(value));
            }
            else if (type == typeof(bool))
            {
                cell.SetCellValue(Convert.ToBoolean(value));
            }
            else
            {
                cell.SetCellValue(value.ToString());
            }
        }

        /// <summary>
        /// 设置单元格的值
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="value"></param>
        public static void SetCellValue(ISheet sheet, int row, int col, object value)
        {
            IRow row2 = sheet.GetRow(row);
            if (row2 == null)
            {
                row2 = sheet.CreateRow(row);
            }
            if (row2 != null)
            {
                ICell cell = row2.GetCell(col);
                if (cell == null)
                {
                    cell = row2.CreateCell(col);
                }
                SetCellValue(cell, value);
            }
        }

        /// <summary>
        /// ws：要设值的工作表的名称 X行Y列 value 值
        /// </summary>
        /// <param name="strSheetName"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="value"></param>
        public void SetCellValue(string strSheetName, int row, int col, object value)
        {
            ISheet sheet = this.GetSheet(strSheetName);
            if (sheet != null)
            {
                SetCellValue(sheet, row, col, value);
            }
        }

        /// <summary>
        /// 测试方法
        /// </summary>
        /// <param name="dt"></param>
        public static void Test(DataTable dt)
        {
            ExcelNPOI lnpoi = new ExcelNPOI();
            lnpoi.Open(@"D:\123.xlsx", true);
            lnpoi.AddSheet("Data");
            lnpoi.InsertTable(dt, "Data", 1, 1);
            lnpoi.SetCellBackColor("Data", 10, 10, ColorIndex.Blue);
            lnpoi.SetCellValue("Data", 10, 10, "111");
            lnpoi.Save();
        }

        /// <summary>
        /// 是否打开
        /// </summary>
        public bool IsOpened
        {
            get
            {
                return (this.m_workbook != null);
            }
        }

        /// <summary>
        /// 颜色索引枚举
        /// </summary>
        public enum ColorIndex
        {
            /// <summary>
            /// 
            /// </summary>
            AQUA = 0x31,
            /// <summary>
            /// 
            /// </summary>
            AUTOMATIC = 0x40,
            /// <summary>
            /// 
            /// </summary>
            Black = 8,
            /// <summary>
            /// 
            /// </summary>
            Blue = 12,
            /// <summary>
            /// 
            /// </summary>
            Blue_Grey = 0x36,
            /// <summary>
            /// 
            /// </summary>
            BRIGHT_GREEN = 11,
            /// <summary>
            /// 
            /// </summary>
            Brown = 60,
            /// <summary>
            /// 
            /// </summary>
            CORAL = 0x1d,
            /// <summary>
            /// 
            /// </summary>
            CORNFLOWER_BLUE = 0x18,
            /// <summary>
            /// 
            /// </summary>
            Dark_Blue = 0x12,
            /// <summary>
            /// 
            /// </summary>
            Dark_Green = 0x3a,
            /// <summary>
            /// 
            /// </summary>
            Dark_Red = 0x10,
            /// <summary>
            /// 
            /// </summary>
            Dark_Teal = 0x38,
            /// <summary>
            /// 
            /// </summary>
            DARK_YELLOW = 0x13,
            /// <summary>
            /// 
            /// </summary>
            Gold = 0x33,
            /// <summary>
            /// 
            /// </summary>
            Green = 0x11,
            /// <summary>
            /// 
            /// </summary>
            GREY_25_PERCENT = 0x16,
            /// <summary>
            /// 
            /// </summary>
            GREY_40_PERCENT = 0x37,
            /// <summary>
            /// 
            /// </summary>
            Grey_50_PERCENT = 0x17,
            /// <summary>
            /// 
            /// </summary>
            Grey_80_PERCENT = 0x3f,
            /// <summary>
            /// 
            /// </summary>
            Indigo = 0x3e,
            /// <summary>
            /// 
            /// </summary>
            LAVENDER = 0x2e,
            /// <summary>
            /// 
            /// </summary>
            LEMON_CHIFFON = 0x1a,
            /// <summary>
            /// 
            /// </summary>
            LIGHT_BLUE = 0x30,
            /// <summary>
            /// 
            /// </summary>
            LIGHT_CORNFLOWER_BLUE = 0x1f,
            /// <summary>
            /// 
            /// </summary>
            LIGHT_GREEN = 0x2a,
            /// <summary>
            /// 
            /// </summary>
            LIGHT_ORANGE = 0x34,
            /// <summary>
            /// 
            /// </summary>
            LIGHT_TURQUOISE = 0x29,
            /// <summary>
            /// 
            /// </summary>
            LIGHT_YELLOW = 0x2b,
            /// <summary>
            /// 
            /// </summary>
            LIME = 50,
            /// <summary>
            /// 
            /// </summary>
            MAROON = 0x19,
            /// <summary>
            /// 
            /// </summary>
            Olive_Green = 0x3b,
            /// <summary>
            /// 
            /// </summary>
            Orange = 0x35,
            /// <summary>
            /// 
            /// </summary>
            ORCHID = 0x1c,
            /// <summary>
            /// 
            /// </summary>
            PALE_BLUE = 0x2c,
            /// <summary>
            /// 
            /// </summary>
            Pink = 14,
            /// <summary>
            /// 
            /// </summary>
            Plum = 0x3d,
            /// <summary>
            /// 
            /// </summary>
            Red = 10,
            /// <summary>
            /// 
            /// </summary>
            Rose = 0x2d,
            /// <summary>
            /// 
            /// </summary>
            ROYAL_BLUE = 30,
            /// <summary>
            /// 
            /// </summary>
            SEA_GREEN = 0x39,
            /// <summary>
            /// 
            /// </summary>
            SKY_BLUE = 40,
            /// <summary>
            /// 
            /// </summary>
            Tan = 0x2f,
            /// <summary>
            /// 
            /// </summary>
            Teal = 0x15,
            /// <summary>
            /// 
            /// </summary>
            TURQUOISE = 15,
            /// <summary>
            /// 
            /// </summary>
            VIOLET = 20,
            /// <summary>
            /// 
            /// </summary>
            White = 9,
            /// <summary>
            /// 
            /// </summary>
            Yellow = 13
        }

        /// <summary>
        /// RGB灰度
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RGB
        {
            /// <summary>
            /// 红色灰度
            /// </summary>
            public byte R;
            /// <summary>
            /// 绿色灰度
            /// </summary>
            public byte G;
            /// <summary>
            /// 蓝色灰度
            /// </summary>
            public byte B;
            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public RGB(byte r, byte g, byte b)
            {
                this.R = r;
                this.G = g;
                this.B = b;
            }
        }
    }
}
