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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Alarm.Frm
{
    /// <summary>
    /// 报警Top榜统计窗体
    /// 功能描述：展示报警数据统计结果、支持排名展示、图表可视化和Excel导出
    /// </summary>
    public partial class Frm_AlarmTop : UIPage
    {
        #region 常量与私有成员

        private const int DefaultTopCount = 10;
        private const int FilteredTopCount = 5;
        private const string ExcelFilter = "Excel文件|*.xls;*.xlsx";
        private List<AlarmStatisticData> _statisticDataList = new List<AlarmStatisticData>();
        private static Frm_AlarmTop _instance;
        private int _currentTopNumber = DefaultTopCount;

        #endregion

        #region 单例属性

        public static Frm_AlarmTop Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new Frm_AlarmTop();
                return _instance;
            }
        }

        #endregion

        #region 构造函数与初始化

        public Frm_AlarmTop()
        {
            InitializeComponent();
            InitializeControls();
            SetDefaultTimeRange();
        }

        private void InitializeControls()
        {
            BarChart.Option.Series.Clear();
            uiButton1.Click += QueryButton_Click;
            uiButton2.Click += ExportButton_Click;
            rbt_Count.CheckedChanged += SortRadio_CheckedChanged;
            rbt_time.CheckedChanged += SortRadio_CheckedChanged;
            InitializeDataGridView();
        }

        /// <summary>
        /// 初始化数据表格，配置列属性确保正确绑定
        /// </summary>
        private void InitializeDataGridView()
        {
            uiDataGridView1.Columns.Clear();

            // 添加数据表格列并明确配置DataPropertyName
            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AlarmRank",
                HeaderText = "报警Top榜",
                DataPropertyName = "AlarmRank",
                Width = 100
            });

            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AlarmCode",
                HeaderText = "报警代码",
                DataPropertyName = "AlarmCode",
                Width = 100
            });

            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Count",
                HeaderText = "次数",
                DataPropertyName = "Count",
                Width = 100
            });

            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TotalDuration",
                HeaderText = "总耗时",
                DataPropertyName = "TotalDuration",
                Width = 100
            });

            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AlarmContent",
                HeaderText = "报警内容",
                DataPropertyName = "AlarmContent",
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Station",
                HeaderText = "站别",
                DataPropertyName = "Station",
                Width = 100
            });

            // 关键设置：禁止用户排序，确保显示顺序不被改变
            foreach (DataGridViewColumn column in uiDataGridView1.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // 其他表格属性设置
            uiDataGridView1.AllowUserToAddRows = false;
            uiDataGridView1.AllowUserToDeleteRows = false;
            uiDataGridView1.ReadOnly = true;
            uiDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }

        private void SetDefaultTimeRange()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var startTime = DateTime.Now.Hour >= 7 && DateTime.Now.Hour < 19
                ? $"{today} 07:00:00"
                : $"{today} 19:00:00";

            uiDatetimePicker1.Value = Convert.ToDateTime(startTime);
            uiDatetimePicker2.Value = uiDatetimePicker1.Value.AddHours(12);
        }

        #endregion

        #region 事件处理方法

        private void Frm_AlarmTop_Load(object sender, EventArgs e)
        {
            LoadAlarmTypeComboBox();
        }

        private async void QueryButton_Click(object sender, EventArgs e)
        {
            try
            {
                _statisticDataList.Clear();
                uiDataGridView1.DataSource = null; // 清除数据源而非行，避免顺序问题
                BarChart.Option.Series.Clear();

                DateTime startTime = uiDatetimePicker1.Value;
                DateTime endTime = uiDatetimePicker2.Value;
                string selectedType = GetSelectedAlarmType();
                _currentTopNumber = string.IsNullOrEmpty(selectedType) ? DefaultTopCount : FilteredTopCount;

                var alarmRecords = await QueryAlarmRecords(startTime, endTime, selectedType);

                if (alarmRecords == null || !alarmRecords.Any())
                {
                    UIMessageBox.Show("未查询到符合条件的报警记录", "提示");
                    return;
                }

                // 处理统计数据并保持排序稳定性
                _statisticDataList = ProcessAlarmStatistics(alarmRecords);

                if (!_statisticDataList.Any())
                {
                    UIMessageBox.Show("没有可统计的报警数据", "提示");
                    return;
                }

                UpdateChart();
                UpdateDataGridView();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_statisticDataList == null || !_statisticDataList.Any())
                {
                    UIMessageBox.Show("没有可导出的数据", "提示");
                    return;
                }

                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
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
            }
        }

        private void SortRadio_CheckedChanged(object sender, EventArgs e)
        {
            if ((sender as RadioButton)?.Checked == true)
            {
                QueryButton_Click(sender, e);
            }
        }

        private void BarChart_Click(object sender, EventArgs e)
        {
            // 图表交互逻辑
        }

        #endregion

        #region 私有业务方法

        private void LoadAlarmTypeComboBox()
        {
            var typeTable = new DataTable();
            typeTable.Columns.Add("ID");
            typeTable.Columns.Add("Name");

            var allRow = typeTable.NewRow();
            allRow["ID"] = "";
            allRow["Name"] = "全部报警";
            typeTable.Rows.Add(allRow);

            var alarmTypes = IniConfig.GetAllKeyValues("AlarmName");
            foreach (var key in alarmTypes.Keys)
            {
                if (alarmTypes.TryGetValue(key, out string value))
                {
                    var row = typeTable.NewRow();
                    row["ID"] = key;
                    row["Name"] = value;
                    typeTable.Rows.Add(row);
                }
            }

            uiComboBox1.DataSource = typeTable;
            uiComboBox1.DisplayMember = "Name";
            uiComboBox1.ValueMember = "ID";
        }

        private string GetSelectedAlarmType()
        {
            if (uiComboBox1.SelectedValue == null)
                return string.Empty;

            return uiComboBox1.SelectedValue.ToString() ?? string.Empty;
        }

        private async Task<List<ProcessErrorEntity>> QueryAlarmRecords(DateTime start, DateTime end, string alarmType)
        {
            var allRecords = await SugarSQLProcessHelper.Instance.DB
                .Queryable<ProcessErrorEntity>()
                .ToListAsync();

            var filteredRecords = allRecords.Where(item =>
                !string.Equals(item.报警ID, "test", StringComparison.OrdinalIgnoreCase) &&
                ParseDateTime(item.异常开始时间, item.日期) > start &&
                ParseDateTime(item.结束时间, item.日期) < end);

            if (!string.IsNullOrEmpty(alarmType) && filteredRecords.Any())
            {
                filteredRecords = filteredRecords.Where(item =>
                    !string.IsNullOrEmpty(item.报警ID) && item.报警ID.Contains(alarmType));
            }

            return filteredRecords.ToList();
        }

        /// <summary>
        /// 处理报警统计数据并保持排序稳定性
        /// </summary>
        private List<AlarmStatisticData> ProcessAlarmStatistics(List<ProcessErrorEntity> records)
        {
            // 使用稳定排序确保相同值的记录保持原始顺序
            var groupedData = records
                .GroupBy(
                    item => $"{item.站别}+{item.报警ID}+{item.报警内容}",
                    item => item.耗时
                )
                .Select(g => new
                {
                    Key = g.Key,
                    Count = g.Count(),
                    TotalDuration = g.Sum(),
                    // 保留原始顺序的关键：使用第一个元素的索引作为次要排序键
                    OriginalIndex = records.FindIndex(r =>
                        $"{r.站别}+{r.报警ID}+{r.报警内容}" == g.Key)
                })
                .ToList();

            // 根据选择的排序方式进行排序，使用OriginalIndex作为次要键确保稳定性
            var sortedGroups = rbt_Count.Checked
                ? groupedData.OrderByDescending(g => g.Count).ThenBy(g => g.OriginalIndex).ToList()
                : groupedData.OrderByDescending(g => g.TotalDuration).ThenBy(g => g.OriginalIndex).ToList();

            var result = new List<AlarmStatisticData>();
            foreach (var group in sortedGroups)
            {
                var keyParts = group.Key.Split('+');
                if (keyParts.Length == 3)
                {
                    result.Add(new AlarmStatisticData
                    {
                        Station = keyParts[0],
                        AlarmCode = keyParts[1],
                        AlarmContent = keyParts[2],
                        Count = group.Count,
                        TotalDuration = group.TotalDuration
                    });
                }
            }

            return result;
        }

        private void UpdateChart()
        {
            var option = new UIBarOption
            {
                ToolTip = { Visible = true, AxisPointer = { Type = UIAxisPointerType.Shadow } },
                Title = new UITitle { Text = $"报警Top{_currentTopNumber}", SubText = "" },
                XAxis = { Name = "报警代码" },
                YAxis = { Name = rbt_Count.Checked ? "报警次数" : "报警总耗时(S)" }
            };

            var series = new UIBarSeries();
            var displayData = _statisticDataList.Take(_currentTopNumber).ToList();

            for (int i = 0; i < displayData.Count; i++)
            {
                option.XAxis.Data.Add(displayData[i].AlarmCode);
                series.AddData(rbt_Count.Checked ? displayData[i].Count : (double)displayData[i].TotalDuration);
                displayData[i].AlarmRank = $"Top{i + 1}";
            }

            option.Series.Add(series);
            BarChart.SetOption(option);
        }

        /// <summary>
        /// 更新数据表格，确保按统计结果顺序显示
        /// </summary>
        private void UpdateDataGridView()
        {
            // 只取需要显示的Top数据
            var displayData = _statisticDataList.Take(_currentTopNumber).ToList();

            // 使用BindingList确保数据绑定且保持顺序
            uiDataGridView1.DataSource = new BindingList<AlarmStatisticData>(displayData);

            // 调整列宽
            uiDataGridView1.Columns["AlarmContent"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            uiDataGridView1.Refresh();
        }

        private void ExportToExcel(string filePath)
        {
            IWorkbook workbook = CreateWorkbook(filePath);
            if (workbook == null)
                throw new ArgumentException("不支持的文件格式", nameof(filePath));

            ISheet sheet = workbook.CreateSheet("报警Top统计");
            CreateExcelHeader(sheet, workbook);
            FillExcelData(sheet);
            AddChartImageToExcel(sheet, workbook);

            for (int i = 0; i < 6; i++)
                sheet.AutoSizeColumn(i);

            SaveWorkbook(workbook, filePath);
        }

        private IWorkbook CreateWorkbook(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            return ext switch
            {
                ".xlsx" => new XSSFWorkbook(),
                ".xls" => new HSSFWorkbook(),
                _ => null
            };
        }

        private void CreateExcelHeader(ISheet sheet, IWorkbook workbook)
        {
            var headerRow = sheet.CreateRow(0);
            var headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.SkyBlue.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var headers = new[] { "报警Top榜", "报警代码", "次数", "总耗时", "报警内容", "站别" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }
        }

        private void FillExcelData(ISheet sheet)
        {
            for (int i = 0; i < _statisticDataList.Count; i++)
            {
                var row = sheet.CreateRow(i + 1);
                var data = _statisticDataList[i];

                row.CreateCell(0).SetCellValue(data.AlarmRank);
                row.CreateCell(1).SetCellValue(data.AlarmCode);
                row.CreateCell(2).SetCellValue(data.Count);
                row.CreateCell(3).SetCellValue(data.TotalDuration);
                row.CreateCell(4).SetCellValue(data.AlarmContent);
                row.CreateCell(5).SetCellValue(data.Station);
            }
        }

        private void AddChartImageToExcel(ISheet sheet, IWorkbook workbook)
        {
            using (var ms = new MemoryStream())
            {
                using (var bmp = BarChart.SaveToImage())
                {
                    bmp.Save(ms, ImageFormat.Png);
                }

                byte[] imageBytes = ms.ToArray();
                int pictureIndex = workbook.AddPicture(imageBytes, PictureType.PNG);
                var anchor = new HSSFClientAnchor(0, 0, 0, 0, 0,
                    _statisticDataList.Count + 2, 5, _statisticDataList.Count + 22);

                sheet.CreateDrawingPatriarch().CreatePicture(anchor, pictureIndex);
            }
        }

        private void SaveWorkbook(IWorkbook workbook, string filePath)
        {
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(stream.ToArray(), 0, stream.ToArray().Length);
                    fs.Flush();
                }
            }
        }

        private DateTime ParseDateTime(string timeStr, DateTime date)
        {
            try
            {
                var parts = timeStr.Split(':');
                if (parts.Length != 3)
                    return DateTime.MinValue;

                return new DateTime(
                    date.Year, date.Month, date.Day,
                    int.Parse(parts[0]),
                    int.Parse(parts[1]),
                    int.Parse(parts[2])
                );
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return DateTime.MinValue;
            }
        }

        #endregion
    }

    public class AlarmStatisticData
    {
        public string AlarmRank { get; set; }
        public string AlarmCode { get; set; }
        public int Count { get; set; }
        public double TotalDuration { get; set; }
        public string AlarmContent { get; set; }
        public string Station { get; set; }
    }
}
