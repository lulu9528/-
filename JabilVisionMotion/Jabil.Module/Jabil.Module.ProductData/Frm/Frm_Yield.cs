#define UserSunnyUI
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jabil.Module.ProductData.Lib;   // 产品数据相关类库
using Sunny.UI;                       // SunnyUI界面组件库
using Jabil.Module.Log.Lib;           // 日志记录相关类库
using System.Threading;
using Jabil.Lib;                      // Jabil通用工具类库
using Jabil.Module.Log.Frm;
using Jabil.Module.Permission;

namespace Jabil.Module.ProductData.Frm
{
    /// <summary>
    /// 产能统计窗体，展示当班、上一班及历史产能数据与图表
    /// 功能特点：
    /// 1. 实时显示生产数量、产出/不合格数量及良率（百分比格式）
    /// 2. 支持历史数据查询和图表可视化展示
    /// 3. 采用单例模式确保全系统唯一的产能统计界面实例
    /// 4. 自动刷新数据，保持信息时效性
    /// </summary>
    public partial class Frm_Yield : UIPage  // 继承自SunnyUI的UIPage，支持页面切换功能
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 懒加载单例实例，通过Lazy<T>实现线程安全初始化
        /// LazyThreadSafetyMode.ExecutionAndPublication确保：
        /// - 多线程环境下仅创建一个实例
        /// - 即使多个线程同时访问，也只会有一个线程执行初始化
        /// - 其他线程会等待初始化完成并获取同一个实例
        /// </summary>
        private static readonly Lazy<Frm_Yield> _lazyInstance =
            new Lazy<Frm_Yield>(() => new Frm_Yield(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取窗体单例实例
        /// 外部通过此属性访问产能统计界面，保证界面唯一性，避免重复创建窗口
        /// </summary>
        public static Frm_Yield Instance => _lazyInstance.Value;
        #endregion

        // 缓存产能管理器实例，减少重复调用带来的性能开销
        /// <summary>
        /// 产能管理器实例，负责数据的获取、处理和管理
        /// 采用单例模式设计，确保数据一致性
        /// </summary>
        public readonly YieldManager _yieldManager;

        // 标识当前显示的是否为今日数据（用于区分实时数据和历史数据）
        private bool isToday = true;

        // 定时器延迟启动时间（单位：毫秒），可根据需要调整
        private const int TimerDelayMs = 3000;  // 3秒延迟

        /// <summary>
        /// 初始化产能统计窗体
        /// 完成UI组件初始化、数据绑定和定时器配置
        /// 新增数据表格列配置，确保良率按百分比显示
        /// </summary>
        public Frm_Yield()
        {
            InitializeComponent();  // 初始化Windows窗体组件（设计器自动生成）

            // 绑定窗体加载事件，用于实现延迟启动定时器
            this.Load += Frm_Yield_Load;

            // 获取产能管理器单例实例，用于数据交互
            // 避免在多个地方重复创建，确保数据统一性
            _yieldManager = YieldManager.Instance;

            // 初始化图表配置（设置标题、图例、坐标轴等基础样式）
            InitializeChart();

            // 配置定时器（1秒刷新一次数据，确保实时性）
            refreshTimer.Interval = 1000;  // 刷新间隔：1000毫秒（1秒）
            refreshTimer.Tick += RefreshTimer_Tick;  // 绑定定时器触发事件
            refreshTimer.Enabled = false;  // 默认不启用，将在窗体加载后延迟启用

            // ========== 关键：初始化数据表格列，配置良率百分比格式 ==========
            InitializeDataGridColumns();
        }

        /// <summary>
        /// 初始化数据表格列配置
        /// 手动指定列与数据模型的映射关系，重点设置良率列的百分比显示格式
        /// 避免自动生成列导致的格式混乱
        /// </summary>
        private void InitializeDataGridColumns()
        {
            // 配置当班数据表格（_currentShiftGridView）
            _currentShiftGridView.AutoGenerateColumns = false;  // 禁用自动生成列
            _currentShiftGridView.Columns.Clear();  // 清除默认列

            // 1. 时间段列
            _currentShiftGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "时间段",
                HeaderText = "时间段",
            });

            // 2. 投入数量列
            _currentShiftGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "投入数量",
                HeaderText = "投入数量",
            });

            // 3. 产出数量列（修改：将合格数改为产出数量）
            _currentShiftGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "产出数量",
                HeaderText = "产出数量",
            });

            // 4. NG数列
            _currentShiftGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "NG数",
                HeaderText = "NG数",
            });

            // 5. 良率列（核心：设置为百分比格式）
            _currentShiftGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "良率",
                HeaderText = "良率",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "P2",  // P2：保留2位小数的百分比格式（如98.50%）
                    Alignment = DataGridViewContentAlignment.MiddleRight  // 右对齐，提升可读性
                }
            });

            // 配置上一班数据表格（_previousShiftGridView），与当班表格结构一致
            _previousShiftGridView.AutoGenerateColumns = false;
            _previousShiftGridView.Columns.Clear();
            // 复制当班表格的列配置，确保格式统一
            _previousShiftGridView.Columns.AddRange(
                _currentShiftGridView.Columns.Cast<DataGridViewColumn>()
                .Select(col => (DataGridViewColumn)col.Clone())
                .ToArray()
            );
        }

        /// <summary>
        /// 窗体加载事件处理程序
        /// 实现定时器的延迟启用，避免窗体未完全加载时就开始数据刷新
        /// </summary>
        private async void Frm_Yield_Load(object sender, EventArgs e)
        {
            try
            {
                // 首次加载时手动更新一次数据，避免等待期间界面空白
                UpdateDataGrids();
                UpdateChartData();

                // 延迟指定时间后启用定时器
                // 使用Task.Delay实现非阻塞延迟，避免冻结UI
                await Task.Delay(TimerDelayMs);

                // 启用定时器开始自动刷新
                refreshTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(new Exception("窗体加载时初始化定时器失败", ex));
            }
        }

        #region 事件处理

        /// <summary>
        /// 定时器刷新事件：每秒更新数据表格和图表
        /// 实现数据的实时展示，反映最新生产状态
        /// 采用异常捕获机制，确保刷新失败不会导致整个程序崩溃
        /// </summary>
        public void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // 注意：此处为测试代码，实际使用时需删除，避免自动重复录入数据
                //Frm_Yield.Instance._yieldManager.AddSingleProduct(ProductResult.投入数);
                //Frm_Yield.Instance._yieldManager.AddSingleProduct(ProductResult.投入数);
                //Frm_Yield.Instance._yieldManager.AddSingleProduct(ProductResult.投入数);
                //Frm_Yield.Instance._yieldManager.AddSingleProduct(ProductResult.Ok);
                //Frm_Yield.Instance._yieldManager.AddSingleProduct(ProductResult.Ok);
                //Frm_Yield.Instance._yieldManager.AddSingleProduct(ProductResult.Ng);

                // 仅在显示今日数据时刷新（历史数据无需实时更新）
                if (isToday)
                {
                    // 更新数据表格（切换数据源为实时数据）
                    UpdateDataGrids();
                    // 更新图表数据（实时产能趋势）
                    UpdateChartData();
                }
            }
            catch (Exception ex)
            {
                // 记录刷新失败的异常日志，便于问题排查
                RLog.SaveError(new Exception("产能数据刷新失败", ex));
            }
        }

        /// <summary>
        /// 日期选择器值变更事件：切换历史数据查询
        /// 当用户选择不同日期时，加载对应日期的白班和夜班数据
        /// 实现历史数据与实时数据的切换逻辑
        /// </summary>
        public void DatePicker_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // 获取用户选择的日期（忽略时间部分，只保留年月日）
                DateTime selectedDate = _datePicker.Value.Date;

                // 判断是否为当天（用于区分实时数据和历史数据）
                isToday = selectedDate == DateTime.Now.Date;

                // 非当天时加载历史数据（当天数据使用实时数据源）
                if (!isToday)
                {
                    _yieldManager.QueryHistoryData(selectedDate);
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
        /// 清空当前班次的所有生产数据
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
                    ? "当前用户未登录，无法清空当前班次的所有生产数据！！！"
                    : "当前用户权限不足，无法清空当前班次的所有生产数据！！！";

                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black, errorMessage);
                return;
            }

            Frm_ConfirmBox.Instance.lbl_Message.Text = "确定要清空当前班次的所有生产数据吗？此操作不可恢复！";
            Frm_ConfirmBox.Instance.ShowDialog();
            if (Frm_ConfirmBox.Instance.Result != ConfirmBoxResult.Yes) return;

            // 如果用户点击"是"，则执行清空操作
            try
            {
                YieldManager.Instance.ClearCurrentShiftData();
                Frm_MessageBox.Instance.MessageBoxShow($"操作成功: 数据已清空。", TipLevel.Tip);
            }
            catch (Exception ex)
            {
                Frm_MessageBox.Instance.MessageBoxShow($"操作失败: {ex.Message}", TipLevel.Error);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化图表基础配置
        /// 设置图表标题、图例、坐标轴样式等基础属性，创建数据系列
        /// 统一图表风格，确保数据可视化的一致性
        /// </summary>
        public void InitializeChart()
        {
            // 清除默认系列，避免设计器中预设的测试数据干扰
            _yieldChart.Option.Series.Clear();

            // 设置图表样式为简洁模式，适合工业监控场景
            _yieldChart.ChartStyleType = UIChartStyleType.Plain;

            // 配置图表基础选项（标注串线生产统计，使用低版本兼容属性）
            var chartOption = new UIBarOption
            {
                Title = new UITitle { Text = "串线产能统计" }, // 图表标题，明确串线场景

                Legend = new UILegend
                {
                    Orient = UIOrient.Horizontal, // 图例水平排列
                    Top = UITopAlignment.Top,     // 图例居上
                    Left = UILeftAlignment.Left   // 图例居左
                }
            };

            // Y轴启用缩放，便于查看串线数据差异较大的情况
            chartOption.YAxis.Scale = true;

            // X轴标签不旋转，保持清晰可读
            chartOption.XAxis.AxisLabel.Angle = 0;

            // Y轴名称，明确串线数据含义
            chartOption.YAxis.Name = "数量";

            // Y轴标签显示整数（无小数），串线生产数据为整数
            chartOption.YAxis.AxisLabel.DecimalPlaces = 0;

            // 显示数据值，直观展示串线具体数字
            chartOption.ShowValue = true;

            // 应用图表配置
            _yieldChart.SetOption(chartOption);

            // 添加图例（投入数量和NG数，适配串线生产统计维度）
            chartOption.Legend.AddData("投入数量");
            chartOption.Legend.AddData("NG数");

            // 添加数据系列（柱状图）：投入数量（深蓝色）、NG数（红色）
            chartOption.Series.Add(CreateBarSeries("投入数量", Color.DarkBlue));
            chartOption.Series.Add(CreateBarSeries("NG数", Color.Red));

            // 再次应用配置使更改生效
            _yieldChart.SetOption(chartOption);
        }

        /// <summary>
        /// 创建图表柱状系列
        /// 封装柱状图系列的创建逻辑，统一配置样式和精度
        /// 采用工厂模式思想，简化多个系列的创建过程
        /// </summary>
        /// <param name="seriesName">系列名称（需与图例名称一致）</param>
        /// <param name="color">柱状图颜色</param>
        /// <returns>配置好的柱状图系列</returns>
        private UIBarSeries CreateBarSeries(string seriesName, Color color)
        {
            return new UIBarSeries
            {
                Name = seriesName,       // 系列名称（用于数据更新和关联图例）
                DecimalPlaces = 0,       // 数据显示整数（无小数）
                // 颜色在SunnyUI中可能通过其他属性设置，此处保留参数便于扩展
            };
        }

        // 缓存上一班次串线数据，用于数据表格绑定
        List<ProductionTimeSlot> productionTimes;

        /// <summary>
        /// 更新串线数据表格数据源
        /// 根据是否为当天数据，切换不同的数据源（实时串线数据/历史串线数据）
        /// 采用条件判断减少不必要的UI更新，提升性能
        /// </summary>
        /// <param name="isToday">是否为当天数据</param>
        private void UpdateDataGrids(bool isToday = true)
        {
            // 确定数据源：
            // - 当天：使用实时串线数据（当班和上一班）
            // - 历史：使用查询的白班和夜班串线数据
            var currentDataSource = isToday
                ? _yieldManager.CurrentShiftData       // 实时串线数据：当班
                : _yieldManager.HistoryDayShiftData;   // 历史串线数据：白班

            var previousDataSource = isToday
                ? _yieldManager.PreviousShiftData      // 实时串线数据：上一班
                : _yieldManager.HistoryNightShiftData; // 历史串线数据：夜班

            // 仅在数据源变化时更新（减少不必要的UI重绘，提升性能）
            if (_currentShiftGridView.DataSource != currentDataSource)
            {
                _currentShiftGridView.DataSource = currentDataSource;
            }

            // 加载上一班次串线数据（实时数据从文件加载，历史数据直接使用查询结果）
            productionTimes = !isToday ? previousDataSource : YieldManager.Instance.LoadShiftData(YieldManager.Instance.GetPreviousShiftFilePath());

            if (_previousShiftGridView.DataSource != productionTimes)
            {
                _previousShiftGridView.DataSource = productionTimes;
            }

            // 刷新表格显示，确保串线数据更新（包括良率的百分比格式）
            _currentShiftGridView.Refresh();
            _previousShiftGridView.Refresh();
        }

        /// <summary>
        /// 更新图表数据
        /// 同步数据表格中的数据到柱状图，支持实时刷新
        /// 针对数据长度变化和值变化分别处理，优化性能
        /// </summary>
        private void UpdateChartData()
        {
            // 获取当前显示的串线数据源（当班或历史白班数据）
            // 转换为ProductionTimeSlot列表，便于遍历处理
            var dataSource = _currentShiftGridView.DataSource as List<ProductionTimeSlot>;

            // 数据源为空时不更新，避免空引用异常
            if (dataSource == null || dataSource.Count == 0) return;

            // 获取图表配置
            var chartOption = _yieldChart.Option;

            // 数据长度变化时（如班次切换），重新初始化X轴和串线数据系列
            if (chartOption.XAxis.Data.Count != dataSource.Count)
            {
                // 清空现有数据
                chartOption.XAxis.Data.Clear();
                chartOption.Series[0].Clear(); // 投入数量系列（串线核心字段）
                chartOption.Series[1].Clear(); // NG数系列（串线核心字段）

                // 重新添加串线数据
                foreach (var slot in dataSource)
                {
                    // 添加X轴标签（时间段）
                    chartOption.XAxis.Data.Add(slot.时间段);
                    // 添加投入数量数据（串线生产输入指标）
                    chartOption.Series[0].AddData(slot.投入数量);
                    // 添加NG数数据（串线生产不良品指标）
                    chartOption.Series[1].AddData(slot.NG数);
                }
                // 应用配置更新图表
                _yieldChart.SetOption(chartOption);
            }
            // 数据长度不变时，仅更新数值（提升性能，避免全量重绘）
            else
            {
                // 遍历每个时间段，更新串线数据
                for (int i = 0; i < dataSource.Count; i++)
                {
                    _yieldChart.Update("投入数量", i, dataSource[i].投入数量);
                    _yieldChart.Update("NG数", i, dataSource[i].NG数);
                }
                // 刷新图表显示
                _yieldChart.Refresh();
            }
        }

        /// <summary>
        /// 更新分组框标题
        /// 根据是否为当天数据，显示不同的标题（当班/上一班或具体日期的白班/夜班）
        /// 提升用户体验，明确当前显示的数据范围
        /// </summary>
        /// <param name="date">当前查询的日期</param>
        /// <param name="isToday">是否为当天</param>
        private void UpdateGroupTitles(DateTime date, bool isToday)
        {
            // 当班/白班分组框标题（添加串线标识）
            _currentShiftGroupBox.Text = isToday ? "串线-当班" : $"{date:yyyy年MM月dd日 串线-白班}";

            // 上一班/夜班分组框标题（添加串线标识）
            _previousShiftGroupBox.Text = isToday ? "串线-上一班" : $"{date:yyyy年MM月dd日 串线-夜班}";
        }

        #endregion

    }
}