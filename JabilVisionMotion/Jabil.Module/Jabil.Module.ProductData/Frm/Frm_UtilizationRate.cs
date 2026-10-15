using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jabil.Module.ProductData.Lib;
using Sunny.UI;
using Jabil.Module.Log.Lib;
using System.Threading;
using Jabil.Module.Log.Frm;
using Jabil.Lib;
using Jabil.Module.Permission;

namespace Jabil.Module.ProductData.Frm
{
    /// <summary>
    /// 设备利用率统计窗体
    /// 核心功能：实时展示当班和上一班次的设备运行数据（运行时间、异常时间、待料时间），
    /// 并通过图表可视化    可视化展示；同时跟踪最近10次生产周期时间（CT）及平均值，辅助生产效率分析
    /// 采用单例模式确保全系统唯一的设备利用率监控界面
    /// </summary>
    public partial class Frm_UtilizationRate : UIPage
    {
        #region 线程安全单例模式
        /// <summary>
        /// 懒加载单例实例，通过Lazy<T>实现线程安全初始化
        /// LazyThreadSafetyMode.ExecutionAndPublication确保多线程环境下仅创建一个实例
        /// </summary>
        private static readonly Lazy<Frm_UtilizationRate> _lazyInstance =
            new Lazy<Frm_UtilizationRate>(() => new Frm_UtilizationRate(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取窗体单例实例
        /// 外部通过此属性访问设备利用率统计界面，保证界面唯一性
        /// </summary>
        public static Frm_UtilizationRate Instance => _lazyInstance.Value;
        #endregion

        // CT（周期时间）显示标签数组（索引0：平均值；索引1-10：最近10次值）
        private readonly UILabel[] _ctLabels = new UILabel[11];
        // 存储最近10次CT值的队列（超过10条自动移除最早数据）
        public Queue<double> _ctQueue = new Queue<double>();
        // 设备利用率    利用率数据管理器实例（缓存，减少重复调用开销）
        public readonly EquipmentUtilizationManager _utilizationManager;
        // 标识当前显示的是否为今日数据（区分实时数据和历史数据）
        private bool isToday = true;
        // 定时器延迟启动时间（单位：毫秒），可根据需要调整
        private const int TimerDelayMs = 3000;  // 3秒延迟

        /// <summary>
        /// 初始化设备利用率统计窗体
        /// 完成UI组件初始化、图表配置、CT显示区域设置和定时器启动
        /// </summary>
        public Frm_UtilizationRate()
        {
            InitializeComponent();
            // 绑定窗体加载事件，用于实现延迟启动定时器
            this.Load += Frm_UtilizationRate_Load;

            // 获取设备利用率管理器单例实例，用于数据交互
            _utilizationManager = EquipmentUtilizationManager.Instance;

            // 初始化图表配置（设置标题、图例、坐标轴等基础样式）
            InitializeChart();
            // 初始化CT显示区域（创建标签控件用于展示最近10次CT及平均值）
            InitializeCtDisplay();

            // 配置刷新定时器（1秒刷新一次数据，确保实时性）
            _refreshTimer.Interval = 1000;
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Enabled = false;  // 默认不启用，将在窗体加载后延迟启用
        }

        /// <summary>
        /// 窗体加载事件处理程序
        /// 实现定时器的延迟启用，避免窗体未完全加载时就开始数据刷新
        /// </summary>
        private async void Frm_UtilizationRate_Load(object sender, EventArgs e)
        {
            try
            {
                // 首次加载时手动更新一次数据，避免等待期间界面空白
                UpdateDataGrids();
                UpdateChartData();
                UpdateCtDisplay();

                // 延迟指定时间后启用定时器
                // 使用Task.Delay实现非阻塞延迟，避免冻结UI
                await Task.Delay(TimerDelayMs);

                // 启用定时器开始自动刷新
                _refreshTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(new Exception("窗体加载时初始化定时器失败", ex));
            }
        }

        #region 初始化方法

        /// <summary>
        /// 初始化CT（周期时间）显示区域的标签控件
        /// 动态创建11个标签：1个平均值标签 + 10个最近值标签，布局在CT分组框内
        /// </summary>
        private void InitializeCtDisplay()
        {
            // 清空分组框内现有控件
            ctGroupBox.Controls.Clear();

            // 初始化11个标签控件（数组已定义长度为11）
            for (int i = 0; i < _ctLabels.Length; i++)
            {
                _ctLabels[i] = new UILabel();
            }

            // 计算控件布局尺寸（根据分组框高度平均分配）
            int height = (ctGroupBox.Height - 4) / 11; // 每个标签的高度间隔
            int width = (ctGroupBox.Width - 3) / 2;    // 标签文本与数值的宽度分割

            // 创建"10次平均CT"描述标签
            UILabel avgLabel = new UILabel();
            avgLabel.Text = "10次平均CT：";
            avgLabel.Location = new System.Drawing.Point(3, 30); // 定位在分组框上方
            ctGroupBox.Controls.Add(avgLabel);

            // 初始化平均值显示标签（索引0）
            _ctLabels[0].Text = "NA"; // 初始值为"未可用"
            _ctLabels[0].Location = new System.Drawing.Point(width + 60, 30); // 位于描述标签右侧
            ctGroupBox.Controls.Add(_ctLabels[0]);
            _ctLabels[0].BringToFront(); // 确保显示在最上层

            // 初始化最近10次CT值的标签（索引1-10）
            for (int index = 1; index < _ctLabels.Length; index++)
            {
                // 创建"最近第N次CT"描述标签
                UILabel label = new UILabel();
                label.Text = $"最近第{index}次CT:";
                label.Location = new System.Drawing.Point(3, index * height + 20); // 垂直排列
                ctGroupBox.Controls.Add(label);

                // 初始化对应的值标签
                _ctLabels[index].Text = "NA"; // 初始值为"未可用"
                _ctLabels[index].Location = new System.Drawing.Point(width + 60, index * height + 20); // 位于描述标签右侧
                ctGroupBox.Controls.Add(_ctLabels[index]);
                _ctLabels[index].BringToFront();
            }
        }

        /// <summary>
        /// 初始化图表基础配置
        /// 设置设备利用率图表的标题、图例、坐标轴样式等基础属性，创建数据系列
        /// </summary>
        private void InitializeChart()
        {
            // 清除默认数据系列
            utilizationChart.Option.Series.Clear();
            // 设置图表样式为简洁模式
            utilizationChart.ChartStyleType = UIChartStyleType.Plain;

            // 配置图表基础选项
            var chartOption = new UIBarOption
            {
                Title = new UITitle { Text = "设备利用率统计" }, // 图表标题
                Legend = new UILegend
                {
                    Orient = UIOrient.Horizontal, // 图例水平排列
                    Top = UITopAlignment.Top,     // 图例居上
                    Left = UILeftAlignment.Left   // 图例居左
                },
            };

            // 配置提示框（鼠标悬停时显示数据详情）
            chartOption.ToolTip.Visible = true;
            // Y轴启用缩放
            chartOption.YAxis.Scale = true;
            // X轴标签不旋转
            chartOption.XAxis.AxisLabel.Angle = 0;
            // Y轴名称（表示时间数值）
            chartOption.YAxis.Name = "数值";
            // Y轴标签显示整数（时间单位为秒，无小数）
            chartOption.YAxis.AxisLabel.DecimalPlaces = 0;
            // 提示框指示器样式为阴影
            chartOption.ToolTip.AxisPointer.Type = UIAxisPointerType.Shadow;
            // 显示数据值
            chartOption.ShowValue = true;

            // 应用图表配置
            utilizationChart.SetOption(chartOption);

            // 添加图例（运行时间、异常时间、待料时间）
            chartOption.Legend.AddData("运行时间");
            chartOption.Legend.AddData("异常时间");
            chartOption.Legend.AddData("待料时间");

            // 添加数据系列（柱状图）：运行时间（绿色）、异常时间（红色）、待料时间（灰色）
            chartOption.Series.Add(CreateBarSeries("运行时间", Color.Green));
            chartOption.Series.Add(CreateBarSeries("异常时间", Color.Red));
            chartOption.Series.Add(CreateBarSeries("待料时间", Color.Gray));

            // 再次应用配置使更改生效
            utilizationChart.SetOption(chartOption);
        }

        /// <summary>
        /// 创建图表柱状系列
        /// 封装柱状图系列的创建逻辑，统一配置名称和精度
        /// </summary>
        /// <param name="seriesName">系列名称（需与图例名称一致）</param>
        /// <param name="color">柱状图颜色（区分不同类型的时间）</param>
        /// <returns>配置好的柱状图系列</returns>
        private UIBarSeries CreateBarSeries(string seriesName, Color color)
        {
            return new UIBarSeries
            {
                Name = seriesName,       // 系列名称（用于数据更新和关联图例）
                DecimalPlaces = 0,       // 数据显示整数（时间单位为秒）
            };
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 定时器刷新事件：每秒更新数据表格、图表和CT显示
        /// 实现实时数据展示，反映设备最新运行状态
        /// </summary>
        public void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // 测试代码：模拟记录设备运行状态（实际应从设备控制系统获取真实状态）
                //EquipmentUtilizationManager.Instance.RecordStatus(EquipmentStatus.Running);

                // 仅在显示今日数据时刷新表格和图表（历史数据无需实时更新）
                if (isToday)
                {
                    // 更新数据表格（当班和上一班数据）
                    UpdateDataGrids();
                    // 更新图表数据（实时设备时间分布）
                    UpdateChartData();
                }

                // 无论是否为今日数据，均更新CT显示（CT数据需持续跟踪）
                UpdateCtDisplay();
            }
            catch (Exception ex)
            {
                // 严重错误时禁用定时器（避免反复报错），记录异常日志
                _refreshTimer.Enabled = false;
                RLog.SaveError(new Exception("设备利用率数据刷新失败", ex));
            }
        }

        /// <summary>
        /// 日期选择器值变更事件：切换历史数据查询
        /// 当用户选择不同日期时，加载对应日期的白班和夜班数据，并调整界面状态
        /// </summary>
        public void DatePicker_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // 获取用户选择的日期（忽略时间部分）
                DateTime selectedDate = datePicker.Value.Date;
                // 判断是否为当天（用于区分实时数据和历史数据）
                isToday = selectedDate == DateTime.Now.Date;

                // 切换定时器状态：仅当天数据需要实时刷新，历史数据禁用定时器
                _refreshTimer.Enabled = isToday;

                // 非当天时加载历史数据（当天数据使用实时数据源）
                if (!isToday)
                {
                    _utilizationManager.QueryHistoryData(selectedDate);
                }

                // 更新分组框标题（显示当前查询的日期和班次）
                UpdateGroupTitles(selectedDate, isToday);
                // 更新数据表格（切换数据源为历史数据或实时数据）
                UpdateDataGrids(isToday);
            }
            catch (Exception ex)
            {
                // 记录日期切换时的数据加载异常
                RLog.SaveError(new Exception("日期切换时数据加载失败", ex));
            }
        }

        /// <summary>
        /// 清空当前班次的所有稼动率数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearData_Click(object sender, EventArgs e)
        {
            var currentUser = PermissionSystem.Instance.CurrentUser;
            // 修正权限判断逻辑：用户未登录 或（用户名不是Admin且不是Jabil）
            if (currentUser == null || (currentUser.UserName != "Admin" && currentUser.UserName != "Jabil" && currentUser.UserName != "JABIL"))
            {
                string errorMessage = currentUser == null
                    ? "当前用户未登录，无法清空当前班次的所有稼动率数据！！！"
                    : "当前用户权限不足，无法清空当前班次的所有稼动率数据！！！";

                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black, errorMessage);
                return;
            }

            Frm_ConfirmBox.Instance.lbl_Message.Text = "确定要清空当前班次的所有稼动率数据吗？此操作不可恢复！";
            Frm_ConfirmBox.Instance.ShowDialog();
            if (Frm_ConfirmBox.Instance.Result != ConfirmBoxResult.Yes) return;

            // 如果用户点击"是"，则执行清空操作
            try
            {
                EquipmentUtilizationManager.Instance.ClearCurrentShiftUtilization();
                Frm_MessageBox.Instance.MessageBoxShow($"操作成功: 数据已清空。", TipLevel.Tip);
            }
            catch (Exception ex)
            {
                Frm_MessageBox.Instance.MessageBoxShow($"操作失败: {ex.Message}", TipLevel.Error);
            }

        }

        #endregion

        #region 数据更新方法

        /// <summary>
        /// 更新数据表格数据源
        /// 根据是否为当天数据，切换不同的数据源（实时数据/历史数据）
        /// </summary>
        /// <param name="isToday">是否为当天数据</param>
        private void UpdateDataGrids(bool isToday = true)
        {
            // 确定数据源：
            // - 当天：使用实时数据（当班和上一班）
            // - 历史：使用查询的白班和夜班数据
            var currentDataSource = isToday
                ? _utilizationManager.CurrentShiftData
                : _utilizationManager.HistoryDayShiftData;

            var previousDataSource = isToday
                ? _utilizationManager.PreviousShiftData
                : _utilizationManager.HistoryNightShiftData;

            // 仅在数据源变化时更新（减少不必要的UI重绘，提升性能）
            if (currentShiftGridView.DataSource != currentDataSource)
            {
                currentShiftGridView.DataSource = currentDataSource;
            }
            if (previousShiftGridView.DataSource != previousDataSource)
            {
                previousShiftGridView.DataSource = previousDataSource;
            }

            // 刷新表格显示，确保数据更新
            currentShiftGridView.Refresh();
            previousShiftGridView.Refresh();
        }

        /// <summary>
        /// 更新图表数据
        /// 同步数据表格中的时间数据到柱状图，支持实时刷新
        /// </summary>
        private void UpdateChartData()
        {
            // 获取当前显示的数据源（当班或历史白班数据）
            var dataSource = currentShiftGridView.DataSource as List<TimeSlotUtilization>;
            // 数据源为空或仅有总计行时不更新（避免索引越界）
            if (dataSource == null || dataSource.Count <= 1) return;

            // 获取图表配置
            var chartOption = utilizationChart.Option;
            // 数据长度为实际时间段数量（排除最后一条总计行）
            int dataCount = dataSource.Count - 1;

            // 数据长度变化时（如班次切换），重新初始化X轴和数据系列
            if (chartOption.XAxis.Data.Count != dataCount)
            {
                // 清空现有数据
                chartOption.XAxis.Data.Clear();
                chartOption.Series[0].Clear(); // 运行时间系列
                chartOption.Series[1].Clear(); // 异常时间系列
                chartOption.Series[2].Clear(); // 待料时间系列

                // 重新添加数据（将时间字符串转换为秒数）
                for (int i = 0; i < dataCount; i++)
                {
                    // 添加X轴标签（时间段）
                    chartOption.XAxis.Data.Add(dataSource[i].时间段);
                    // 添加运行时间（秒）
                    chartOption.Series[0].AddData(_utilizationManager.SecondsByDateTime(dataSource[i].运行时间));
                    // 添加异常时间（秒）
                    chartOption.Series[1].AddData(_utilizationManager.SecondsByDateTime(dataSource[i].异常时间));
                    // 添加待料时间（秒）
                    chartOption.Series[2].AddData(_utilizationManager.SecondsByDateTime(dataSource[i].待料时间));
                }
                // 应用配置更新图表
                utilizationChart.SetOption(chartOption);
            }
            // 数据长度不变时，仅更新数值（提升性能，避免全量重绘）
            else
            {
                // 遍历每个时间段，更新数据（将时间字符串转换为秒数）
                for (int i = 0; i < dataCount; i++)
                {
                    utilizationChart.Update("运行时间", i, _utilizationManager.SecondsByDateTime(dataSource[i].运行时间));
                    utilizationChart.Update("异常时间", i, _utilizationManager.SecondsByDateTime(dataSource[i].异常时间));
                    utilizationChart.Update("待料时间", i, _utilizationManager.SecondsByDateTime(dataSource[i].待料时间));
                }
                // 刷新图表显示
                utilizationChart.Refresh();
            }
        }

        /// <summary>
        /// 更新CT（周期时间）显示
        /// 展示最近10次CT值及平均值，反映生产节奏稳定性
        /// </summary>
        private void UpdateCtDisplay()
        {
            // 线程安全处理（避免多线程操作队列导致异常）
            lock (_ctQueue)
            {
                // 保持队列最多10条数据，超过则移除最早的记录
                while (_ctQueue.Count > 10)
                {
                    _ctQueue.Dequeue();
                }

                // 队列有数据时更新显示
                if (_ctQueue.Count > 0)
                {
                    // 显示平均值（保留2位小数）
                    _ctLabels[0].Text = _ctQueue.Average().ToString("f2");

                    // 显示最近10次值（按时间顺序从早到晚）
                    var ctArray = _ctQueue.ToArray();
                    for (int i = 0; i < ctArray.Length; i++)
                    {
                        // 确保索引不超出标签数组范围
                        if (i + 1 < _ctLabels.Length)
                        {
                            _ctLabels[i + 1].Text = ctArray[ctArray.Length - 1 - i].ToString("f2");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新分组框标题
        /// 根据是否为当天数据，显示不同的标题（当班/上一班或具体日期的白班/夜班）
        /// </summary>
        /// <param name="date">当前查询的日期</param>
        /// <param name="isToday">是否为当天</param>
        private void UpdateGroupTitles(DateTime date, bool isToday)
        {
            // 当班/白班分组框标题
            currentShiftGroupBox.Text = isToday ? "当班" : $"{date:yyyy年MM月dd日 白班}";
            // 上一班/夜班分组框标题
            previousShiftGroupBox.Text = isToday ? "上一班" : $"{date:yyyy年MM月dd日 夜班}";
        }

        #endregion

    }
}
