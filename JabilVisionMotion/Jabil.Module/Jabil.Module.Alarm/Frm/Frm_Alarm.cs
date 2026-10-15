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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Alarm.Frm
{
    /// <summary>
    /// 报警信息管理窗体
    /// 功能描述：负责报警记录的查询、筛选、排序、分页显示及Excel导出等操作
    /// </summary>
    public partial class Frm_Alarm : UIPage
    {
        #region 私有成员与单例模式

        private static Frm_Alarm _instance;
        private List<ProcessErrorEntity> _allAlarmRecords;
        private List<ProcessErrorEntity> _filteredAlarmRecords;

        public static Frm_Alarm Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new Frm_Alarm();
                return _instance;
            }
        }

        #endregion

        #region 构造函数与初始化方法

        public Frm_Alarm()
        {
            InitializeComponent();
            InitializeControlSettings();
            SetDefaultTimeRange();
            // 初始化数据表格列
            InitializeDataGridViewColumns();
        }

        private void InitializeControlSettings()
        {
            uiPagination1.PageSize = 21;
            uiPagination1.PageChanged += Pagination_PageChanged;
            uiDataGridView1.SelectIndexChange += DataGridView_SelectIndexChange;
            btnQuery.Click += QueryButton_Click;
            btnExport.Click += ExportButton_Click;
            cboAlarmType.SelectedIndexChanged += AlarmType_SelectedIndexChanged;
            rbtAscending.CheckedChanged += SortRadio_CheckedChanged;
            rbtDescending.CheckedChanged += SortRadio_CheckedChanged;
            当班ToolStripMenuItem.Click += CurrentShiftMenuItem_Click;
            上一班ToolStripMenuItem.Click += PreviousShiftMenuItem_Click;

            // 设置数据表格属性
            uiDataGridView1.AllowUserToAddRows = false;
            uiDataGridView1.AllowUserToDeleteRows = false;
            uiDataGridView1.ReadOnly = true;
            uiDataGridView1.AutoGenerateColumns = false;
            uiDataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            uiDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }

        /// <summary>
        /// 初始化数据表格列，确保与实体类属性正确映射
        /// </summary>
        private void InitializeDataGridViewColumns()
        {
            // 清除现有列
            uiDataGridView1.Columns.Clear();

            // 添加站别列
            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "站别",
                HeaderText = "站别",
                Width = 50
            });

            // 添加日期列
            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "日期",
                HeaderText = "日期",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            });

            // 添加报警ID列
            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "报警ID",
                HeaderText = "报警ID",
                Width = 80
            });

            // 添加异常开始时间列
            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "异常开始时间",
                HeaderText = "异常开始时间",
                Width = 120
            });

            // 添加结束时间列
            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "结束时间",
                HeaderText = "结束时间",
                Width = 80
            });

            // 添加报警内容列
            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "报警内容",
                HeaderText = "报警内容",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            // 添加耗时列
            uiDataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "耗时",
                HeaderText = "耗时",
                Width = 100
            });
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

        private async void QueryButton_Click(object sender, EventArgs e)
        {
            try
            {
                btnQuery.Enabled = false;

                cboAlarmType.SelectedIndexChanged -= AlarmType_SelectedIndexChanged;

                DateTime startTime = uiDatetimePicker1.Value;
                DateTime endTime = uiDatetimePicker2.Value;

                // 验证时间范围
                if (startTime >= endTime)
                {
                    UIMessageBox.Show("开始时间不能晚于结束时间", "提示");
                    return;
                }

                _allAlarmRecords = await SugarSQLProcessHelper.Instance.DB
                    .Queryable<ProcessErrorEntity>()
                    .Where(item =>
                        item.日期 >= startTime.Date &&
                        item.日期 <= endTime.Date)
                    .ToListAsync();

                _allAlarmRecords = FilterAndSortAlarmRecords(_allAlarmRecords, startTime, endTime);
                LoadAlarmTypeComboBox();
                cboAlarmType.SelectedIndexChanged += AlarmType_SelectedIndexChanged;

                _filteredAlarmRecords = _allAlarmRecords;
                UpdatePaginationControl();

                // 显示记录数量
                labRecordCount.Text = $"共 {_filteredAlarmRecords.Count} 条记录";
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                UIMessageBox.Show($"查询失败: {ex.Message}", "错误");
            }
            finally
            {
                btnQuery.Enabled = true;
            }
        }

        private void AlarmType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cboAlarmType.SelectedIndex <= -1 || _allAlarmRecords == null || !_allAlarmRecords.Any())
                    return;

                var selectedValue = cboAlarmType.SelectedValue;
                if (selectedValue == null)
                    return;

                string selectedType = selectedValue.ToString();
                if (string.IsNullOrEmpty(selectedType))
                    return;

                var query = _allAlarmRecords
                    .Where(item => !string.IsNullOrEmpty(item.报警ID) && item.报警ID.Contains(selectedType));

                _filteredAlarmRecords = rbtAscending.Checked
                    ? query.OrderBy(item => ParseDateTime(item.异常开始时间, item.日期)).ToList()
                    : query.OrderByDescending(item => ParseDateTime(item.异常开始时间, item.日期)).ToList();

                UpdatePaginationControl();
                labRecordCount.Text = $"共 {_filteredAlarmRecords.Count} 条记录";
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        private void DataGridView_SelectIndexChange(object sender, int index)
        {
            index.WriteConsole("SelectedIndex");
        }

        private void Pagination_PageChanged(object sender, object pagingSource, int pageIndex, int pageSize)
        {
            try
            {
                if (_filteredAlarmRecords == null)
                {
                    uiDataGridView1.DataSource = null;
                    return;
                }

                var currentPageData = _filteredAlarmRecords
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 使用BindingList确保数据变更通知
                uiDataGridView1.DataSource = new BindingList<ProcessErrorEntity>(currentPageData);
                AdjustDataGridViewColumnWidths();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                uiDataGridView1.DataSource = null;
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_filteredAlarmRecords == null || !_filteredAlarmRecords.Any())
                {
                    UIMessageBox.Show("没有可导出的数据", "提示");
                    return;
                }

                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    saveDialog.Title = "选择保存位置";
                    saveDialog.Filter = "Excel文件|*.xlsx|Excel 97-2003文件|*.xls";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        ExportToExcel(_filteredAlarmRecords, saveDialog.FileName);
                        UIMessageBox.Show("数据导出成功", "提示");
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                UIMessageBox.Show($"导出失败: {ex.Message}", "错误");
            }
        }

        private void SortRadio_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (_filteredAlarmRecords == null || !_filteredAlarmRecords.Any() ||
                    _allAlarmRecords == null || !_allAlarmRecords.Any())
                    return;

                _filteredAlarmRecords = rbtAscending.Checked
                    ? _filteredAlarmRecords.OrderBy(item => ParseDateTime(item.异常开始时间, item.日期)).ToList()
                    : _filteredAlarmRecords.OrderByDescending(item => ParseDateTime(item.异常开始时间, item.日期)).ToList();

                UpdatePaginationControl();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        private void CurrentShiftMenuItem_Click(object sender, EventArgs e)
        {
            SetTimeRangeForCurrentShift();
            QueryButton_Click(sender, e);
        }

        private void PreviousShiftMenuItem_Click(object sender, EventArgs e)
        {
            SetTimeRangeForPreviousShift();
            QueryButton_Click(sender, e);
        }

        private void rbtAscending_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtAscending.Checked && _allAlarmRecords != null)
            {
                UpdateFilteredRecordsOrder();
            }
        }

        #endregion

        #region 私有业务方法

        private List<ProcessErrorEntity> FilterAndSortAlarmRecords(List<ProcessErrorEntity> records, DateTime start, DateTime end)
        {
            if (records == null)
                return new List<ProcessErrorEntity>();

            // 过滤有效的记录
            var filtered = records.Where(item =>
            {
                // 验证关键字段不为空
                if (string.IsNullOrEmpty(item.报警ID) ||
                    string.IsNullOrEmpty(item.异常开始时间) ||
                    item.日期 == DateTime.MinValue)
                    return false;

                // 排除测试记录
                if (string.Equals(item.报警ID, "test", StringComparison.OrdinalIgnoreCase))
                    return false;

                // 验证时间范围
                var startTime = ParseDateTime(item.异常开始时间, item.日期);
                var endTime = ParseDateTime(item.结束时间, item.日期);

                return startTime > start && endTime < end;
            });

            // 排序
            return rbtAscending.Checked
                ? filtered.OrderBy(item => ParseDateTime(item.异常开始时间, item.日期)).ToList()
                : filtered.OrderByDescending(item => ParseDateTime(item.异常开始时间, item.日期)).ToList();
        }

        private void LoadAlarmTypeComboBox()
        {
            var alarmTypes = _allAlarmRecords
                .Where(item => !string.IsNullOrEmpty(item.报警ID))
                .GroupBy(item => item.报警ID.Substring(0, 1))
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();

            var typeTable = new DataTable();
            typeTable.Columns.Add("ID");
            typeTable.Columns.Add("Name");

            var alarmTypeNames = IniConfig.GetAllKeyValues("AlarmName");

            foreach (var typeId in alarmTypes)
            {
                if (alarmTypeNames.TryGetValue(typeId, out string typeName))
                {
                    var row = typeTable.NewRow();
                    row["ID"] = typeId;
                    row["Name"] = typeName;
                    typeTable.Rows.Add(row);
                }
            }

            cboAlarmType.DataSource = typeTable;
            cboAlarmType.DisplayMember = "Name";
            cboAlarmType.ValueMember = "ID";
            cboAlarmType.SelectedIndex = -1;
        }

        private void UpdatePaginationControl()
        {
            uiPagination1.TotalCount = _filteredAlarmRecords?.Count ?? 0;
            uiPagination1.ActivePage = 1;
        }

        private void AdjustDataGridViewColumnWidths()
        {
            if (uiDataGridView1.Columns.Count != 7) return;

            uiDataGridView1.Columns[0].Width = 50;
            uiDataGridView1.Columns[1].Width = 100;
            uiDataGridView1.Columns[2].Width = 80;
            uiDataGridView1.Columns[3].Width = 120;
            uiDataGridView1.Columns[4].Width = 80;
            uiDataGridView1.Columns[5].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            uiDataGridView1.Columns[6].Width = 100;
        }

        private void ExportToExcel(List<ProcessErrorEntity> data, string filePath)
        {
            IWorkbook workbook = CreateWorkbook(filePath);
            if (workbook == null)
                throw new ArgumentException("不支持的文件格式", nameof(filePath));

            ISheet sheet = workbook.CreateSheet("报警记录");
            CreateExcelHeader(sheet, workbook);
            FillExcelData(sheet, data);

            for (int i = 0; i < 7; i++)
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

            var headers = new[] { "站别", "日期", "报警ID", "异常开始时间", "结束时间", "报警内容", "耗时" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }
        }

        private void FillExcelData(ISheet sheet, List<ProcessErrorEntity> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                var row = sheet.CreateRow(i + 1);
                var item = data[i];

                row.CreateCell(0).SetCellValue(item.站别 ?? "");
                row.CreateCell(1).SetCellValue(item.日期.ToString("yyyyMMdd"));
                row.CreateCell(2).SetCellValue(item.报警ID ?? "");
                row.CreateCell(3).SetCellValue(item.异常开始时间 ?? "");
                row.CreateCell(4).SetCellValue(item.结束时间 ?? "");
                row.CreateCell(5).SetCellValue(item.报警内容 ?? "");
                row.CreateCell(6).SetCellValue(item.耗时.ToString() ?? "");
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
                if (string.IsNullOrEmpty(timeStr))
                    return DateTime.MinValue;

                var parts = timeStr.Split(':');
                if (parts.Length != 3)
                    return DateTime.MinValue;

                if (int.TryParse(parts[0], out int hour) &&
                    int.TryParse(parts[1], out int minute) &&
                    int.TryParse(parts[2], out int second))
                {
                    // 处理跨天情况
                    if (hour >= 24)
                    {
                        hour -= 24;
                        date = date.AddDays(1);
                    }

                    return new DateTime(date.Year, date.Month, date.Day, hour, minute, second);
                }
                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return DateTime.MinValue;
            }
        }

        private void SetTimeRangeForCurrentShift()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var startTime = DateTime.Now.Hour >= 7 && DateTime.Now.Hour < 19
                ? $"{today} 07:00:00"
                : $"{today} 19:00:00";

            uiDatetimePicker1.Value = Convert.ToDateTime(startTime);
            uiDatetimePicker2.Value = uiDatetimePicker1.Value.AddHours(12);
        }

        private void SetTimeRangeForPreviousShift()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var currentShiftStart = DateTime.Now.Hour >= 7 && DateTime.Now.Hour < 19
                ? Convert.ToDateTime($"{today} 07:00:00")
                : Convert.ToDateTime($"{today} 19:00:00");

            uiDatetimePicker1.Value = currentShiftStart.AddHours(-12);
            uiDatetimePicker2.Value = currentShiftStart;
        }

        private void UpdateFilteredRecordsOrder()
        {
            if (_filteredAlarmRecords == null) return;

            _filteredAlarmRecords = rbtAscending.Checked
                ? _filteredAlarmRecords.OrderBy(item => ParseDateTime(item.异常开始时间, item.日期)).ToList()
                : _filteredAlarmRecords.OrderByDescending(item => ParseDateTime(item.异常开始时间, item.日期)).ToList();

            UpdatePaginationControl();
        }

        #endregion
    }
}
