using Jabil.Lib;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Alarm.Entity;
using Jabil.Module.Alarm.Lib;
using Jabil.Module.Log.Lib;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Alarm.Frm
{
    // 遵循C#命名规范，类名使用PascalCase
    public partial class Frm_Operation : UIPage
    {
        // 常量定义：将硬编码值提取为常量，便于维护
        private const string LogDirectory = @"D:\VPTool\Config\Log\Operation\";
        private const int PageSize = 32;
        private const string ExcelFilter = "Excel文件|*.xls;*.xlsx";
        private const string DefaultExportPath = "C:\\Users\\USER\\Desktop";

        // 字段命名规范：私有字段使用下划线开头
        private List<OperationLog> _operationLogs = new List<OperationLog>();
        private static Frm_Operation _instance;

        /// <summary>
        /// 单例实例属性，确保窗体唯一
        /// </summary>
        public static Frm_Operation Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    _instance = new Frm_Operation();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 构造函数，初始化控件
        /// </summary>
        public Frm_Operation()
        {
            InitializeComponent();
            InitializeControls();
        }

        /// <summary>
        /// 初始化控件状态和事件绑定
        /// </summary>
        private void InitializeControls()
        {
            // 设置默认日期为当天
            uiDatePicker1.Value = DateTime.Now;
            uiDatePicker2.Value = DateTime.Now;

            // 初始化分页控件
            uiPagination1.PageSize = PageSize;
            uiPagination1.PageChanged += UiPagination1_PageChanged;

            // 绑定数据网格事件
            uiDataGridView1.SelectIndexChange += UiDataGridView1_SelectIndexChange;

            // 初始化数据网格列
            InitializeDataGridViewColumns();
        }


        /// <summary>
        /// 初始化数据网格视图的列，只显示时间和信息列
        /// </summary>
        private void InitializeDataGridViewColumns()
        {
            uiDataGridView1.Columns.Clear();
            uiDataGridView1.AutoGenerateColumns = false; // 禁用自动生成列，避免显示不需要的字段
            uiDataGridView1.AllowUserToAddRows = false;  // 禁止用户添加行
            uiDataGridView1.ReadOnly = true;             // 设置为只读，防止编辑
            uiDataGridView1.RowHeadersVisible = false;   // 隐藏行标题列，节省空间
            uiDataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; // 列自动填充

            // 添加时间列
            var timeColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Time",               // 绑定到OperationLog的Time属性
                HeaderText = "操作时间",                  // 列标题
                Width = 180,                             // 固定宽度，确保时间完整显示
                Resizable = DataGridViewTriState.True,   // 允许调整列宽
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft // 左对齐
                }
            };
            uiDataGridView1.Columns.Add(timeColumn);

            // 添加日志内容列
            var contentColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LogContent",          // 绑定到OperationLog的LogContent属性
                HeaderText = "日志信息",                  // 列标题
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, // 自动填充剩余空间
                Resizable = DataGridViewTriState.True,    // 允许调整列宽
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft, // 左对齐
                    WrapMode = DataGridViewTriState.True,                // 自动换行，完整显示长内容
                    NullValue = "无信息"                                 // 空值显示
                }
            };
            uiDataGridView1.Columns.Add(contentColumn);

            // 设置网格视图的整体样式
            uiDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells; // 根据内容自动调整行高
            uiDataGridView1.RowTemplate.Height = 40; // 设置默认行高
            uiDataGridView1.ScrollBars = ScrollBars.Vertical; // 只显示垂直滚动条
        }

        /// <summary>
        /// 查询按钮点击事件
        /// </summary>
        private async void uiButton1_Click(object sender, EventArgs e)
        {
            try
            {
                // 输入验证
                var startTime = uiDatePicker1.Value;
                var endTime = uiDatePicker2.Value;
                if (startTime > endTime)
                {
                    UIMessageBox.Show("开始日期不能晚于结束日期", "提示");
                    return;
                }

                var keyword = uiTextBox1.Text?.Trim() ?? string.Empty;

                // 清空现有数据
                _operationLogs.Clear();
                uiDataGridView1.DataSource = null;

                // 异步加载日志，避免UI卡顿
                _operationLogs = await LoadOperationLogsAsync(startTime, endTime, keyword);

                // 按时间降序排序
                _operationLogs = _operationLogs.OrderByDescending(log => log.Time).ToList();

                // 更新分页控件
                uiPagination1.TotalCount = _operationLogs.Count;
                uiPagination1.ActivePage = 1;

                // 显示第一页数据
                LoadPageData(1);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                UIMessageBox.Show($"查询失败：{ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 异步加载指定日期范围内的操作日志
        /// </summary>
        /// <summary>
        /// 异步加载指定日期范围内的操作日志
        /// </summary>
        private async Task<List<OperationLog>> LoadOperationLogsAsync(DateTime start, DateTime end, string keyword)
        {
            // 日期关键字格式常量
            const string DateKeywordFormat = "yy_MM_dd";
            // 日志中的时间格式
            const string LogTimeFormat = "yyyy/MM/dd HH:mm:ss.fff";

            // 使用Task.Run将IO操作放入后台线程，避免阻塞UI
            return await Task.Run(() =>
            {
                var logs = new List<OperationLog>();
                var totalDays = (end - start).Days + 1;

                // 检查日志目录是否存在
                if (!Directory.Exists(LogDirectory))
                {
                    throw new DirectoryNotFoundException($"日志目录不存在: {LogDirectory}");
                }

                // 获取目录中所有的txt文件
                var allLogFiles = Directory.GetFiles(LogDirectory, "*.txt", SearchOption.TopDirectoryOnly);

                for (var i = 0; i < totalDays; i++)
                {
                    var currentDate = start.AddDays(i);
                    var dateKeyword = currentDate.ToString(DateKeywordFormat);

                    // 查找文件名中包含当前日期关键字的文件（不区分大小写）
                    var matchedFiles = allLogFiles.Where(file =>
                        Path.GetFileName(file).IndexOf(dateKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

                    // 如果没有匹配的文件，记录信息并继续
                    if (!matchedFiles.Any())
                    {
                        RLog.SaveLog(LogType.Operation, Log.Lib.LogLevel.Info, Color.Red,
                            $"未找到包含日期关键字 {dateKeyword} 的日志文件");
                        continue;
                    }

                    // 处理所有匹配的文件
                    foreach (var fileName in matchedFiles)
                    {
                        try
                        {
                            using (var reader = new StreamReader(fileName, Encoding.UTF8))
                            {
                                string line;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    var trimmedLine = line.Trim();
                                    if (string.IsNullOrEmpty(trimmedLine))
                                        continue;

                                    // 检查是否包含关键词（不区分大小写）
                                    if (!string.IsNullOrEmpty(keyword) &&
                                        trimmedLine.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) == -1)
                                        continue;

                                    // 解析日志记录：根据时间格式提取而非固定长度
                                    DateTime logTime;
                                    int timeLength = LogTimeFormat.Length; // 时间格式的长度

                                    // 确保行长度足够且能解析出时间
                                    if (trimmedLine.Length > timeLength &&
                                        DateTime.TryParseExact(
                                            trimmedLine.Substring(0, timeLength),
                                            LogTimeFormat,
                                            System.Globalization.CultureInfo.InvariantCulture,
                                            System.Globalization.DateTimeStyles.None,
                                            out logTime))
                                    {
                                        // 提取时间字符串和日志内容
                                        string timeStr = trimmedLine.Substring(0, timeLength);
                                        string content = trimmedLine.Substring(timeLength).Trim();

                                        logs.Add(new OperationLog
                                        {
                                            Time = timeStr,
                                            LogContent = content
                                        });
                                    }
                                    else
                                    {
                                        // 对于无法解析时间的行，可以选择跳过或全部作为日志内容
                                        // 这里选择全部作为日志内容，时间留空或标记为未知
                                        logs.Add(new OperationLog
                                        {
                                            Time = "时间格式错误",
                                            LogContent = trimmedLine
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            RLog.SaveError(ex, $"读取日志文件 {fileName} 失败");
                            // 继续处理其他文件
                            continue;
                        }
                    }
                }

                return logs;
            });
        }

        /// <summary>
        /// 分页控件页码变更事件
        /// </summary>
        private void UiPagination1_PageChanged(object sender, object pagingSource, int pageIndex, int count)
        {
            LoadPageData(pageIndex);
        }

        /// <summary>
        /// 加载指定页码的数据
        /// </summary>
        private void LoadPageData(int pageIndex)
        {
            if (_operationLogs == null || _operationLogs.Count == 0)
            {
                uiDataGridView1.DataSource = null;
                return;
            }

            // 使用LINQ优化分页逻辑，更简洁高效
            var pageData = _operationLogs
                .Skip((pageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            uiDataGridView1.DataSource = pageData;
        }

        /// <summary>
        /// 数据网格选择项变更事件
        /// </summary>
        private void UiDataGridView1_SelectIndexChange(object sender, int index)
        {
            // 仅在调试时输出，生产环境可移除
            System.Diagnostics.Debug.WriteLine($"SelectedIndex: {index}");
        }

        /// <summary>
        /// 导出按钮点击事件
        /// </summary>
        private void uiButton2_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查是否有数据可导出
                if (_operationLogs == null || !_operationLogs.Any())
                {
                    UIMessageBox.Show("没有可导出的数据", "提示");
                    return;
                }

                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.InitialDirectory = DefaultExportPath;
                    saveDialog.Title = "选择保存位置";
                    saveDialog.Filter = ExcelFilter;

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        ExportToExcel(saveDialog.FileName);
                        UIMessageBox.Show("数据导出成功", "提示");
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                UIMessageBox.Show($"导出失败：{ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 将操作日志导出到Excel文件
        /// </summary>
        private void ExportToExcel(string filePath)
        {
            // 根据文件扩展名创建对应的工作簿
            IWorkbook workbook = CreateWorkbook(filePath);
            if (workbook == null)
                throw new ArgumentException("不支持的文件格式", nameof(filePath));

            // 创建工作表
            ISheet sheet = workbook.CreateSheet("操作日志");

            // 创建表头
            CreateExcelHeader(sheet, workbook);

            // 填充数据
            FillExcelData(sheet);

            // 自动调整列宽
            AdjustColumnWidths(sheet);

            // 保存工作簿
            SaveWorkbook(workbook, filePath);
        }

        /// <summary>
        /// 创建Excel工作簿（支持xls和xlsx格式）
        /// </summary>
        private IWorkbook CreateWorkbook(string filePath)
        {
            var fileExt = Path.GetExtension(filePath).ToLower();
            return fileExt switch
            {
                ".xlsx" => new XSSFWorkbook(),
                ".xls" => new HSSFWorkbook(),
                _ => null
            };
        }

        /// <summary>
        /// 创建Excel表头
        /// </summary>
        private void CreateExcelHeader(ISheet sheet, IWorkbook workbook)
        {
            var headerRow = sheet.CreateRow(0);

            // 设置表头样式
            var headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.SkyBlue.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            // 创建表头单元格
            var timeCell = headerRow.CreateCell(0);
            timeCell.SetCellValue("时间");
            timeCell.CellStyle = headerStyle;


            var contentCell = headerRow.CreateCell(1);
            contentCell.SetCellValue("操作日志");
            contentCell.CellStyle = headerStyle;
        }

        /// <summary>
        /// 填充Excel数据
        /// </summary>
        private void FillExcelData(ISheet sheet)
        {
            for (var i = 0; i < _operationLogs.Count; i++)
            {
                var row = sheet.CreateRow(i + 1);
                var log = _operationLogs[i];

                row.CreateCell(0).SetCellValue(log.Time);
                row.CreateCell(1).SetCellValue(log.LogContent);
            }
        }

        /// <summary>
        /// 调整Excel列宽
        /// </summary>
        private void AdjustColumnWidths(ISheet sheet)
        {
            // 调整前两列宽度
            sheet.AutoSizeColumn(0);
            sheet.AutoSizeColumn(1);

            // 限制最大宽度，避免过宽
            if (sheet.GetColumnWidth(0) > 50 * 256)
                sheet.SetColumnWidth(0, 50 * 256);

            if (sheet.GetColumnWidth(1) > 150 * 256)
                sheet.SetColumnWidth(1, 150 * 256);
        }

        /// <summary>
        /// 保存Excel工作簿到文件
        /// </summary>
        private void SaveWorkbook(IWorkbook workbook, string filePath)
        {
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                var buffer = stream.ToArray();

                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(buffer, 0, buffer.Length);
                    fs.Flush();
                }
            }
        }
    }

    /// <summary>
    /// 操作日志数据模型
    /// </summary>
    public class OperationLog
    {
        public string Time { get; set; }         // 操作时间
        public string LogContent { get; set; }   // 操作日志内容
    }
}
