using NPOI.HSSF.UserModel; // NPOI库：处理XLS格式Excel
using NPOI.SS.UserModel;  // NPOI库：Excel通用接口
using NPOI.XSSF.UserModel; // NPOI库：处理XLSX格式Excel
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicClassLibrary
{
    /// <summary>
    /// 保存数据的格式枚举（支持CSV、XLS、XLSX）
    /// </summary>
    public enum SaveDataFormat
    {
        XLS = 0,   // Excel 97-2003格式（.xls）
        CSV = 1,   // 逗号分隔值格式（.csv）
        XLSX = 2   // Excel 2007+格式（.xlsx）
    }

    /// <summary>
    /// 文件工具类，提供全面的文件和目录操作功能，包括：
    /// 1. CSV/Excel数据导入导出（DataTable、DataGridView）
    /// 2. 日志文件写入与读取
    /// 3. 目录/文件的创建、删除、复制、移动等管理
    /// 4. 文件内容读写、清空等基础操作
    /// 依赖NPOI库处理Excel文件（XLS/XLSX）
    /// </summary>
    public class FileUtils
    {
        #region CSV文件操作（导入到DataTable）

        /// <summary>
        /// 从CSV文件读取数据并转换为DataTable
        /// </summary>
        /// <param name="filePath">CSV文件的绝对路径（如"D:\\data.csv"）</param>
        /// <returns>包含CSV数据的DataTable；读取失败返回null</returns>
        public static DataTable OpenCSV(string filePath)
        {
            try
            {
                // 自动检测编码（0表示系统默认编码）
                System.Text.Encoding encoding = Encoding.GetEncoding(0);
                DataTable dt = new DataTable(); // 存储CSV数据的表

                // 打开文件流（读模式）
                using (System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                using (System.IO.StreamReader sr = new System.IO.StreamReader(fs, encoding))
                {
                    string strLine = ""; // 存储每行读取的内容
                    string[] aryLine = null; // 存储每行拆分后的字段
                    string[] tableHead = null; // 存储表头（第一行）
                    int columnCount = 0; // 列数（由表头决定）
                    bool IsFirst = true; // 是否为第一行（表头行）

                    // 逐行读取CSV
                    while ((strLine = sr.ReadLine()) != null && strLine != "")
                    {
                        if (IsFirst)
                        {
                            // 处理第一行：作为表头
                            tableHead = strLine.Split(',');
                            IsFirst = false;
                            columnCount = tableHead.Length;

                            // 为DataTable添加列（列名对应表头）
                            for (int i = 0; i < columnCount; i++)
                            {
                                DataColumn dc = new DataColumn(tableHead[i]);
                                dt.Columns.Add(dc);
                            }
                        }
                        else
                        {
                            // 处理数据行：拆分字段并添加到DataTable
                            aryLine = strLine.Split(',');
                            DataRow dr = dt.NewRow();
                            for (int j = 0; j < columnCount; j++)
                            {
                                // 若字段数量不足，用空字符串填充
                                dr[j] = j >= aryLine.Length ? "" : aryLine[j];
                            }
                            dt.Rows.Add(dr);
                        }
                    }

                    // 按第一列升序排序（若有数据）
                    if (aryLine != null && aryLine.Length > 0)
                    {
                        dt.DefaultView.Sort = tableHead[0] + " asc";
                    }
                }

                return dt;
            }
            catch (Exception ex)
            {
                // 异常时返回null（可根据需要添加日志）
                return null;
                throw;
            }
        }

        #endregion

        #region CSV文件操作（从DataTable导出）

        /// <summary>
        /// 将DataTable中的数据保存为CSV文件
        /// </summary>
        /// <param name="dt">包含数据的DataTable</param>
        /// <param name="fileName">CSV文件的保存路径（含文件名，如"D:\\output.csv"）</param>
        public static void SaveCSV(DataTable dt, string fileName)
        {
            // 确保文件存在（不存在则创建）
            FileUtils.ExistsFile(fileName);

            // 打开文件流（创建模式，写权限）
            using (FileStream fs = new FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default))
            {
                string data = "";

                // 写入表头（列名）
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    data += dt.Columns[i].ColumnName.ToString();
                    // 列间用逗号分隔（最后一列不加逗号）
                    if (i < dt.Columns.Count - 1)
                    {
                        data += ",";
                    }
                }
                sw.WriteLine(data); // 写入表头行

                // 写入数据行
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    data = "";
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        data += dt.Rows[i][j].ToString();
                        if (j < dt.Columns.Count - 1)
                        {
                            data += ",";
                        }
                    }
                    sw.WriteLine(data); // 写入一行数据
                }
            }
        }

        /// <summary>
        /// 将DataTable中的ROI（感兴趣区域）数据保存为CSV文件（特殊格式处理）
        /// </summary>
        /// <param name="dt">包含ROI数据的DataTable</param>
        /// <param name="fileName">CSV文件的保存路径</param>
        public static void SaveCSVROI(DataTable dt, string fileName)
        {
            // 确保文件存在
            FileUtils.ExistsFile(fileName);

            using (FileStream fs = new FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default))
            {
                string data = "";

                // 写入表头
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    data += dt.Columns[i].ColumnName.ToString();
                    if (i < dt.Columns.Count - 1)
                    {
                        data += ",";
                    }
                }
                sw.WriteLine(data);

                // 写入数据行（注释部分为示例特殊处理，实际按DataTable数据写入）
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    data = "";
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        data += dt.Rows[i][j].ToString();
                        if (j < dt.Columns.Count - 1)
                        {
                            data += ",";
                        }
                    }
                    sw.WriteLine(data);
                }
            }
        }

        #endregion

        #region 应用目录与日志操作

        /// <summary>
        /// 获取当前应用程序所在的目录路径
        /// </summary>
        /// <returns>应用程序根目录的绝对路径（如"D:\\MyApp"）</returns>
        public static string GetAppDirectory()
        {
            // 获取当前进程主模块的路径（可执行文件路径）
            string str = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            // 截取路径部分（去掉文件名）
            string logFilePath = str.Substring(0, str.LastIndexOf("\\"));
            return logFilePath;
        }

        /// <summary>
        /// 将日志信息写入txt文件（默认保存到应用目录下的"Log--"文件夹）
        /// </summary>
        /// <param name="strLog">日志内容（如操作记录、错误信息）</param>
        public static void WriteLogToTxt(string strLog)
        {
            String System_Now_Time = null; // 日志时间戳（精确到秒）
            String File_Create_Time = null; // 日志文件名（按日期）

            System_Now_Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File_Create_Time = DateTime.Now.ToString("yyyyMMdd"); // 文件名格式：yyyyMMdd.txt
            string logDocument = File_Create_Time + ".txt";

            // 日志目录：应用目录下的"Log--"文件夹
            string str = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string logFilePath = str.Substring(0, str.LastIndexOf("\\")) + "\\Log--";
            // 目录不存在则创建
            if (!Directory.Exists(logFilePath))
            {
                Directory.CreateDirectory(logFilePath);
            }
            // 日志文件完整路径
            logFilePath = logFilePath + "//" + logDocument;

            // 追加写入日志（带时间戳）
            using (FileStream fs = new FileStream(logFilePath, FileMode.Append))
            using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
            {
                sw.WriteLine(System_Now_Time + ":   " + strLog);
                sw.Flush(); // 立即写入磁盘
            }
        }

        /// <summary>
        /// 将日志信息写入指定文件夹的txt文件（按日期命名）
        /// </summary>
        /// <param name="strLog">日志内容</param>
        /// <param name="fileName">日志文件夹名称（在应用目录下）</param>
        public static void WriteLogToTxt(string strLog, string fileName)
        {
            String System_Now_Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            String File_Create_Time = DateTime.Now.ToString("yyyyMMdd");
            string logDocument = File_Create_Time + ".txt";

            // 日志目录：应用目录下的指定文件夹
            string str = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string logFilePath = str.Substring(0, str.LastIndexOf("\\")) + "\\" + fileName;
            if (!Directory.Exists(logFilePath))
            {
                Directory.CreateDirectory(logFilePath);
            }
            logFilePath = logFilePath + "//" + logDocument;

            // 追加写入日志
            using (FileStream fs = new FileStream(logFilePath, FileMode.Append))
            using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
            {
                sw.WriteLine(System_Now_Time + ":   " + strLog);
                sw.Flush();
            }
        }

        #endregion

        #region DataGridView数据导出到Excel/CSV

        /// <summary>
        /// 将DataGridView中的数据导出为指定格式的文件（CSV、XLS、XLSX）
        /// </summary>
        /// <param name="dgv">包含数据的DataGridView控件</param>
        /// <param name="excelFullPath">保存文件的完整路径（如"D:\\data.xlsx"）</param>
        /// <param name="format">导出格式（CSV/XLS/XLSX）</param>
        public static void SaveDataGridViewToExcel(DataGridView dgv, string excelFullPath, SaveDataFormat format)
        {
            int columnCount = dgv.ColumnCount; // 列数
            int rowCount = dgv.RowCount; // 行数
            string strLine = ""; // 临时存储行数据

            switch (format)
            {
                case SaveDataFormat.CSV:
                    #region 导出为CSV格式
                    try
                    {
                        // 打开流写入器（追加模式，系统默认编码）
                        using (StreamWriter sw = new StreamWriter(excelFullPath, true, System.Text.Encoding.GetEncoding(-0)))
                        {
                            // 写入表头（列标题）
                            for (int i = 0; i < columnCount; i++)
                            {
                                if (i > 0)
                                    strLine += ",";
                                strLine += dgv.Columns[i].HeaderText;
                            }
                            strLine = strLine.TrimEnd(','); // 移除末尾多余逗号
                            sw.WriteLine(strLine);

                            // 写入数据行
                            for (int j = 0; j < rowCount; j++)
                            {
                                strLine = "";
                                for (int k = 0; k < columnCount; k++)
                                {
                                    if (k > 0)
                                        strLine += ",";
                                    // 处理空值和逗号（用中文逗号替换避免字段拆分错误）
                                    if (dgv.Rows[j].Cells[k].Value == null)
                                        strLine += "";
                                    else
                                    {
                                        string m = dgv.Rows[j].Cells[k].Value.ToString().Trim();
                                        strLine += m.Replace(",", "，"); // 替换英文逗号为中文
                                    }
                                }
                                strLine = strLine.TrimEnd(',');
                                sw.WriteLine(strLine);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        throw; // 抛出异常由调用方处理
                    }
                    break;
                #endregion

                case SaveDataFormat.XLS:
                    #region 导出为XLS格式（Excel 97-2003）
                    // 使用线程池异步执行（避免UI阻塞）
                    ThreadPool.QueueUserWorkItem((pp) =>
                    {
                        // 创建XLS工作簿和工作表
                        HSSFWorkbook npoiwb = new HSSFWorkbook();
                        HSSFSheet sheet = (HSSFSheet)npoiwb.CreateSheet("11"); // 工作表名称为"11"
                        HSSFRow headRow = (HSSFRow)sheet.CreateRow(0); // 表头行（第0行）

                        // 写入表头
                        for (int i = 0; i < dgv.Columns.Count; i++)
                        {
                            HSSFCell headCell = (HSSFCell)headRow.CreateCell(i, CellType.String);
                            headCell.SetCellValue(dgv.Columns[i].HeaderText);
                        }

                        // 写入数据行
                        for (int i = 0; i < dgv.Rows.Count; i++)
                        {
                            HSSFRow row = (HSSFRow)sheet.CreateRow(i + 1); // 数据行从第1行开始
                            for (int j = 0; j < dgv.Columns.Count; j++)
                            {
                                HSSFCell cell = (HSSFCell)row.CreateCell(j);
                                if (dgv.Rows[i].Cells[j].Value == null)
                                {
                                    cell.SetCellType(CellType.Blank); // 空值处理
                                }
                                else
                                {
                                    // 根据单元格数据类型设置对应的值（确保Excel中格式正确）
                                    if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.Int32"))
                                    {
                                        cell.SetCellValue(Convert.ToInt32(dgv.Rows[i].Cells[j].Value));
                                    }
                                    else if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.String"))
                                    {
                                        cell.SetCellValue(dgv.Rows[i].Cells[j].Value.ToString());
                                    }
                                    else if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.Single"))
                                    {
                                        cell.SetCellValue(Convert.ToSingle(dgv.Rows[i].Cells[j].Value));
                                    }
                                    else if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.Double"))
                                    {
                                        cell.SetCellValue(Convert.ToDouble(dgv.Rows[i].Cells[j].Value));
                                    }
                                    else if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.Decimal"))
                                    {
                                        cell.SetCellValue(Convert.ToDouble(dgv.Rows[i].Cells[j].Value));
                                    }
                                    else if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.DateTime"))
                                    {
                                        cell.SetCellValue(Convert.ToDateTime(dgv.Rows[i].Cells[j].Value).ToString("yyyy-MM-dd"));
                                    }
                                }
                            }
                        }

                        // 自动调整列宽
                        for (int i = 0; i < dgv.Columns.Count; i++)
                        {
                            sheet.AutoSizeColumn(i);
                        }

                        // 保存工作簿到文件
                        using (FileStream fs = new FileStream(excelFullPath, FileMode.Create))
                        {
                            npoiwb.Write(fs);
                        }
                    });
                    break;
                #endregion

                case SaveDataFormat.XLSX:
                    #region 导出为XLSX格式（Excel 2007+）
                    // 异步执行（同XLS，区别在于使用XSSF类）
                    ThreadPool.QueueUserWorkItem((pp) =>
                    {
                        XSSFWorkbook npoiwb = new XSSFWorkbook();
                        XSSFSheet sheet = (XSSFSheet)npoiwb.CreateSheet("11");
                        XSSFRow headRow = (XSSFRow)sheet.CreateRow(0);

                        // 写入表头
                        for (int i = 0; i < dgv.Columns.Count; i++)
                        {
                            XSSFCell headCell = (XSSFCell)headRow.CreateCell(i, CellType.String);
                            headCell.SetCellValue(dgv.Columns[i].HeaderText);
                        }

                        // 写入数据行（逻辑同XLS）
                        for (int i = 0; i < dgv.Rows.Count; i++)
                        {
                            XSSFRow row = (XSSFRow)sheet.CreateRow(i + 1);
                            for (int j = 0; j < dgv.Columns.Count; j++)
                            {
                                XSSFCell cell = (XSSFCell)row.CreateCell(j);
                                if (dgv.Rows[i].Cells[j].Value == null)
                                {
                                    cell.SetCellType(CellType.Blank);
                                }
                                else
                                {
                                    // 按数据类型设置值
                                    if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.Int32"))
                                    {
                                        cell.SetCellValue(Convert.ToInt32(dgv.Rows[i].Cells[j].Value));
                                    }
                                    else if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.String"))
                                    {
                                        cell.SetCellValue(dgv.Rows[i].Cells[j].Value.ToString());
                                    }
                                    else if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.Single"))
                                    {
                                        cell.SetCellValue(Convert.ToSingle(dgv.Rows[i].Cells[j].Value));
                                    }
                                    else if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.Double"))
                                    {
                                        cell.SetCellValue(Convert.ToDouble(dgv.Rows[i].Cells[j].Value));
                                    }
                                    else if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.Decimal"))
                                    {
                                        cell.SetCellValue(Convert.ToDouble(dgv.Rows[i].Cells[j].Value));
                                    }
                                    else if (dgv.Rows[i].Cells[j].ValueType.FullName.Contains("System.DateTime"))
                                    {
                                        cell.SetCellValue(Convert.ToDateTime(dgv.Rows[i].Cells[j].Value).ToString("yyyy-MM-dd"));
                                    }
                                }
                            }
                        }

                        // 自动调整列宽
                        for (int i = 0; i < dgv.Columns.Count; i++)
                        {
                            sheet.AutoSizeColumn(i);
                        }

                        // 保存文件
                        using (FileStream fs = new FileStream(excelFullPath, FileMode.Create))
                        {
                            npoiwb.Write(fs);
                        }
                    });
                    break;
                #endregion

                default:
                    break;
            }
        }

        #endregion

        #region 目录与文件检查

        /// <summary>
        /// 检查指定目录是否存在
        /// </summary>
        /// <param name="directoryPath">目录的绝对路径（如"D:\\MyDir"）</param>
        /// <returns>存在返回true，否则false</returns>
        public static bool IsExistDirectory(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        /// <summary>
        /// 检查指定文件是否存在
        /// </summary>
        /// <param name="filePath">文件的绝对路径（如"D:\\file.txt"）</param>
        /// <returns>存在返回true，否则false</returns>
        public static bool IsExistFile(string filePath)
        {
            return File.Exists(filePath);
        }

        #endregion

        #region 目录与文件列表获取

        /// <summary>
        /// 获取指定目录下的所有文件路径（仅当前目录，不包含子目录）
        /// </summary>
        /// <param name="directoryPath">目录的绝对路径</param>
        /// <returns>文件路径数组；目录不存在则抛出异常</returns>
        public static string[] GetFileNames(string directoryPath)
        {
            if (!IsExistDirectory(directoryPath))
            {
                throw new FileNotFoundException("目录不存在", directoryPath);
            }
            return Directory.GetFiles(directoryPath);
        }

        /// <summary>
        /// 获取指定目录下的所有子目录路径（仅当前目录，不包含嵌套子目录）
        /// </summary>
        /// <param name="directoryPath">目录的绝对路径</param>
        /// <returns>子目录路径数组；异常时抛出IOException</returns>
        public static string[] GetDirectories(string directoryPath)
        {
            try
            {
                return Directory.GetDirectories(directoryPath);
            }
            catch (IOException ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 获取指定目录及子目录中的文件路径（支持过滤和递归）
        /// </summary>
        /// <param name="directoryPath">目录的绝对路径</param>
        /// <param name="searchPattern">搜索模式（如"*.txt"匹配所有文本文件）</param>
        /// <param name="isSearchChild">是否搜索子目录（true=递归搜索）</param>
        /// <returns>文件路径数组；目录不存在则抛出异常</returns>
        public static string[] GetFileNames(string directoryPath, string searchPattern, bool isSearchChild)
        {
            if (!IsExistDirectory(directoryPath))
            {
                throw new FileNotFoundException("目录不存在", directoryPath);
            }

            try
            {
                // 选择搜索选项（当前目录/所有子目录）
                SearchOption option = isSearchChild ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetFiles(directoryPath, searchPattern, option);
            }
            catch (IOException ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 获取指定目录及子目录中的子目录路径（支持过滤和递归）
        /// </summary>
        /// <param name="directoryPath">目录的绝对路径</param>
        /// <param name="searchPattern">搜索模式（如"test*"匹配以test开头的目录）</param>
        /// <param name="isSearchChild">是否搜索子目录</param>
        /// <returns>子目录路径数组；异常时抛出IOException</returns>
        public static string[] GetDirectories(string directoryPath, string searchPattern, bool isSearchChild)
        {
            try
            {
                SearchOption option = isSearchChild ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetDirectories(directoryPath, searchPattern, option);
            }
            catch (IOException ex)
            {
                throw ex;
            }
        }

        #endregion

        #region 目录与文件状态检查

        /// <summary>
        /// 检查指定目录是否为空（无文件且无子目录）
        /// </summary>
        /// <param name="directoryPath">目录的绝对路径</param>
        /// <returns>空目录返回true，否则false；异常时返回true（默认处理）</returns>
        public static bool IsEmptyDirectory(string directoryPath)
        {
            try
            {
                // 检查是否有文件
                string[] fileNames = GetFileNames(directoryPath);
                if (fileNames.Length > 0)
                {
                    return false;
                }

                // 检查是否有子目录
                string[] directoryNames = GetDirectories(directoryPath);
                if (directoryNames.Length > 0)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                // 异常时默认视为空（可添加日志）
                return true;
            }
        }

        /// <summary>
        /// 检查指定目录中是否存在符合搜索模式的文件（不搜索子目录）
        /// </summary>
        /// <param name="directoryPath">目录的绝对路径</param>
        /// <param name="searchPattern">搜索模式（如"*.csv"）</param>
        /// <returns>存在返回true，否则false；异常时抛出</returns>
        public static bool Contains(string directoryPath, string searchPattern)
        {
            try
            {
                string[] fileNames = GetFileNames(directoryPath, searchPattern, false);
                return fileNames.Length > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.StackTrace);
            }
        }

        /// <summary>
        /// 检查指定目录及子目录中是否存在符合搜索模式的文件
        /// </summary>
        /// <param name="directoryPath">目录的绝对路径</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <param name="isSearchChild">是否搜索子目录</param>
        /// <returns>存在返回true，否则false；异常时抛出</returns>
        public static bool Contains(string directoryPath, string searchPattern, bool isSearchChild)
        {
            try
            {
                string[] fileNames = GetFileNames(directoryPath, searchPattern, isSearchChild);
                return fileNames.Length > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.StackTrace);
            }
        }

        #endregion

        #region 日期相关的目录/文件名生成

        /// <summary>
        /// 根据当前日期生成目录名（格式：yyyyMMdd）
        /// </summary>
        /// <returns>目录名字符串（如"20240520"）</returns>
        public static string GetDateDir()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }

        /// <summary>
        /// 根据当前时间生成文件名（格式：HHmmssff，精确到毫秒）
        /// </summary>
        /// <returns>文件名字符串（如"15304512"表示15时30分45秒12毫秒）</returns>
        public static string GetDateFile()
        {
            return DateTime.Now.ToString("HHmmssff");
        }

        #endregion

        #region 目录复制与文件删除

        /// <summary>
        /// 递归复制文件夹（包含所有子目录和文件）
        /// </summary>
        /// <param name="varFromDirectory">源目录路径</param>
        /// <param name="varToDirectory">目标目录路径（不存在则创建）</param>
        public static void CopyFolder(string varFromDirectory, string varToDirectory)
        {
            // 确保目标目录存在
            Directory.CreateDirectory(varToDirectory);

            if (!Directory.Exists(varFromDirectory))
                return;

            // 复制子目录（递归）
            string[] directories = Directory.GetDirectories(varFromDirectory);
            if (directories.Length > 0)
            {
                foreach (string d in directories)
                {
                    // 目标子目录路径（保留源目录结构）
                    string targetSubDir = varToDirectory + d.Substring(d.LastIndexOf("\\"));
                    CopyFolder(d, targetSubDir);
                }
            }

            // 复制文件（覆盖已存在文件）
            string[] files = Directory.GetFiles(varFromDirectory);
            if (files.Length > 0)
            {
                foreach (string s in files)
                {
                    string targetFile = varToDirectory + s.Substring(s.LastIndexOf("\\"));
                    File.Copy(s, targetFile, true);
                }
            }
        }

        /// <summary>
        /// 删除指定目录对应的目标目录中的文件（保持目录结构，仅删文件）
        /// </summary>
        /// <param name="varFromDirectory">源目录路径（作为结构参考）</param>
        /// <param name="varToDirectory">目标目录路径（删除其中的文件）</param>
        public static void DeleteFolderFiles(string varFromDirectory, string varToDirectory)
        {
            Directory.CreateDirectory(varToDirectory);

            if (!Directory.Exists(varFromDirectory))
                return;

            // 递归处理子目录
            string[] directories = Directory.GetDirectories(varFromDirectory);
            if (directories.Length > 0)
            {
                foreach (string d in directories)
                {
                    string targetSubDir = varToDirectory + d.Substring(d.LastIndexOf("\\"));
                    DeleteFolderFiles(d, targetSubDir);
                }
            }

            // 删除目标目录中与源目录对应的文件
            string[] files = Directory.GetFiles(varFromDirectory);
            if (files.Length > 0)
            {
                foreach (string s in files)
                {
                    string targetFile = varToDirectory + s.Substring(s.LastIndexOf("\\"));
                    File.Delete(targetFile);
                }
            }
        }

        #endregion

        #region 文件创建与检查

        /// <summary>
        /// 检查文件是否存在，不存在则创建（解决File.Create后文件占用问题）
        /// </summary>
        /// <param name="FilePath">文件的绝对路径（含文件名）</param>
        public static void ExistsFile(string FilePath)
        {
            if (!File.Exists(FilePath))
            {
                // 直接使用File.Create会导致文件句柄未释放，需手动关闭
                using (FileStream fs = File.Create(FilePath))
                {
                    // 立即关闭流，释放文件
                }
            }
        }

        /// <summary>
        /// 创建空文件（若文件已存在则不操作）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        public static void CreateFile(string filePath)
        {
            try
            {
                if (!IsExistFile(filePath))
                {
                    FileInfo file = new FileInfo(filePath);
                    using (FileStream fs = file.Create())
                    {
                        // 关闭流释放文件
                    }
                }
            }
            catch (Exception ex)
            {
                // 可添加日志记录
            }
        }

        /// <summary>
        /// 创建文件并写入二进制数据（若文件已存在则覆盖）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <param name="buffer">要写入的二进制数据</param>
        public static void CreateFile(string filePath, byte[] buffer)
        {
            try
            {
                if (!IsExistFile(filePath))
                {
                    using (FileStream fs = File.Create(filePath))
                    {
                        fs.Write(buffer, 0, buffer.Length); // 写入二进制流
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 创建目录（若目录已存在则不操作）
        /// </summary>
        /// <param name="directoryPath">目录的绝对路径</param>
        public static void CreateDirectory(string directoryPath)
        {
            if (!IsExistDirectory(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        #endregion

        #region 文件删除

        /// <summary>
        /// 删除指定文件（若文件不存在则不操作）
        /// </summary>
        /// <param name="file">文件的绝对路径</param>
        public static void DeleteFile(string file)
        {
            if (File.Exists(file))
                File.Delete(file);
        }

        /// <summary>
        /// 删除指定目录及所有子目录和文件
        /// </summary>
        /// <param name="directoryPath">目录的绝对路径</param>
        public static void DeleteDirectory(string directoryPath)
        {
            if (IsExistDirectory(directoryPath))
            {
                Directory.Delete(directoryPath, true); // true=递归删除所有内容
            }
        }

        #endregion

        #region 文件内容读取

        /// <summary>
        /// 获取文本文件中指定行的内容（索引从0开始）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <param name="index">行索引（0为第一行）</param>
        /// <returns>指定行的内容；索引超出范围返回空字符串</returns>
        public static string ReadText(string filePath, int index)
        {
            string strTemp = string.Empty;
            try
            {
                string[] strAll = File.ReadAllLines(filePath); // 读取所有行
                if (index < strAll.Length)
                {
                    strTemp = strAll[index];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("读文件失败！\r\n" + ex.StackTrace);
            }
            return strTemp;
        }

        /// <summary>
        /// 读取文本文件的所有内容（每行用逗号分隔）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <returns>所有内容拼接的字符串；读取失败返回空</returns>
        public static string ReadAllText(string filePath)
        {
            string strTemp = string.Empty;
            try
            {
                string[] strAll = File.ReadAllLines(filePath);
                strTemp = string.Join(",", strAll);
            }
            catch (Exception ex)
            {
                MessageBox.Show("读文件失败！\r\n" + ex.StackTrace);
            }
            return strTemp;
        }

        #endregion

        #region 文件内容写入

        /// <summary>
        /// 向文本文件写入内容（覆盖原有内容）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <param name="content">要写入的内容</param>
        public static void WriteText(string filePath, string content)
        {
            try
            {
                File.WriteAllText(filePath, content);
            }
            catch (Exception ex)
            {
                MessageBox.Show("写文件失败！\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 向文本文件写入内容（指定编码，覆盖原有内容）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <param name="text">要写入的内容</param>
        /// <param name="encoding">编码（如Encoding.UTF8）</param>
        public static void WriteText(string filePath, string text, Encoding encoding)
        {
            if (!File.Exists(filePath))
            {
                // 确保文件存在
                using (FileStream fs = File.Create(filePath)) { }
            }
            File.WriteAllText(filePath, text, encoding);
        }

        /// <summary>
        /// 向文本文件尾部追加内容（不覆盖原有内容）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <param name="content">要追加的内容</param>
        public static void AppendText(string filePath, string content)
        {
            try
            {
                File.AppendAllText(filePath, content);
            }
            catch (Exception ex)
            {
                MessageBox.Show("写文件失败！\r\n" + ex.StackTrace);
            }
        }

        #endregion

        #region 文件移动与复制

        /// <summary>
        /// 将文件复制到目标路径（覆盖已存在文件）
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="destFilePath">目标文件路径</param>
        public static void Copy(string sourceFilePath, string destFilePath)
        {
            File.Copy(sourceFilePath, destFilePath, true); // true=覆盖
        }

        /// <summary>
        /// 将文件移动到指定目录（若目标存在则先删除）
        /// </summary>
        /// <param name="sourceFilePath">源文件的绝对路径</param>
        /// <param name="descDirectoryPath">目标目录的绝对路径</param>
        public static void Move(string sourceFilePath, string descDirectoryPath)
        {
            // 获取源文件名
            string sourceFileName = GetFileName(sourceFilePath);

            if (IsExistDirectory(descDirectoryPath))
            {
                string destFilePath = descDirectoryPath + "\\" + sourceFileName;
                // 目标文件存在则删除
                if (IsExistFile(destFilePath))
                {
                    DeleteFile(destFilePath);
                }
                // 移动文件
                File.Move(sourceFilePath, destFilePath);
            }
        }

        #endregion

        #region 文件名与扩展名处理

        /// <summary>
        /// 从文件路径中获取文件名（含扩展名）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <returns>文件名（如"data.csv"）</returns>
        public static string GetFileName(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            return fi.Name;
        }

        /// <summary>
        /// 从文件路径中获取文件名（不含扩展名）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <returns>文件名（如"date"）</returns>
        public static string GetFileNameNoExtension(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            return fi.Name.Split('.')[0]; // 按"."拆分取第一部分（简单处理，可能不适合多扩展名文件）
        }

        /// <summary>
        /// 从文件路径中获取扩展名（含"."）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <returns>扩展名（如".csv"）</returns>
        public static string GetExtension(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            return fi.Extension;
        }

        #endregion

        #region 清空操作

        /// <summary>
        /// 清空目录（删除所有文件和子目录，但保留当前目录）
        /// </summary>
        /// <param name="directoryPath">目录的绝对路径</param>
        public static void ClearDirectory(string directoryPath)
        {
            if (IsExistDirectory(directoryPath))
            {
                // 删除所有文件
                string[] fileNames = GetFileNames(directoryPath);
                foreach (string fileName in fileNames)
                {
                    DeleteFile(fileName);
                }

                // 删除所有子目录
                string[] directoryNames = GetDirectories(directoryPath);
                foreach (string dirName in directoryNames)
                {
                    DeleteDirectory(dirName);
                }
            }
        }

        /// <summary>
        /// 清空文件内容（删除后重建空文件）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        public static void ClearFile(string filePath)
        {
            File.Delete(filePath);
            CreateFile(filePath);
        }

        #endregion

        #region 其他辅助方法

        /// <summary>
        /// 获取文本文件的行数
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <returns>行数；文件不存在或异常时返回0</returns>
        public static int GetLineCount(string filePath)
        {
            if (!IsExistFile(filePath))
                return 0;
            return File.ReadAllLines(filePath).Length;
        }

        /// <summary>
        /// 获取文件大小（字节数）
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <returns>文件大小（Byte）；文件不存在返回0</returns>
        public static int GetFileSize(string filePath)
        {
            if (!IsExistFile(filePath))
                return 0;
            FileInfo fi = new FileInfo(filePath);
            return (int)fi.Length;
        }

        #endregion
    }
}