using System;
using System.Windows.Forms;
using Jabil.Module.ProductData.Lib;
using Sunny.UI;
using Jabil.Module.Log.Lib;
using System.Threading;
using System.Threading.Tasks;
using Jabil.Module.Log.Frm;
using Jabil.Lib;
using Jabil.Module.Permission;
using System.Drawing;

namespace Jabil.Module.ProductData.Frm
{
    /// <summary>
    /// NG分类统计窗体
    /// 核心功能：
    /// 1. 实时展示当前班次和上一班次的NG数据（总NG数、压力NG、量测NG、多种NG）
    /// 2. 支持通过日期选择器查询历史NG数据（白班/夜班）
    /// 3. 自动刷新数据（1秒/次），确保实时反映生产中的NG状态
    /// 采用单例模式确保全系统唯一的NG统计监控界面，保证数据展示一致性
    /// </summary>
    public partial class Frm_NgStatistics : UIPage
    {
        #region 线程安全单例模式
        /// <summary>
        /// 懒加载单例实例，通过Lazy<T>实现线程安全初始化
        /// LazyThreadSafetyMode.ExecutionAndPublication确保多线程环境下仅创建一个实例
        /// 避免多线程同时创建实例导致的资源竞争和数据不一致问题
        /// </summary>
        private static readonly Lazy<Frm_NgStatistics> _lazyInstance =
            new Lazy<Frm_NgStatistics>(() => new Frm_NgStatistics(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取窗体单例实例
        /// 外部通过此属性访问NG分类统计界面，保证全局只有一个实例
        /// 统一数据展示入口，避免重复创建界面导致的资源浪费
        /// </summary>
        public static Frm_NgStatistics Instance => _lazyInstance.Value;
        #endregion

        // NG统计管理器实例（缓存，减少重复调用单例的性能开销）
        public readonly NgStatisticsManager _ngStatisticsManager;
        // 标识当前显示的是否为今日数据（用于区分实时数据和历史数据）
        private bool isToday = true;
        // 定时器延迟启动时间（单位：毫秒），避免窗体未完全加载时就开始数据刷新
        private const int TimerDelayMs = 3000;  // 3秒延迟

        /// <summary>
        /// 初始化NG分类统计窗体
        /// 完成UI组件初始化、NG统计管理器绑定和定时器配置
        /// </summary>
        public Frm_NgStatistics()
        {
            InitializeComponent();
            // 绑定窗体加载事件，用于实现定时器延迟启动
            this.Load += Frm_NgStatistics_Load;

            // 获取NG统计管理器单例实例，用于数据交互（获取/更新NG数据）
            _ngStatisticsManager = NgStatisticsManager.Instance;

            // 初始化定时器（设置刷新间隔和事件绑定）
            InitializeTimer();
        }

        /// <summary>
        /// 窗体加载事件处理程序
        /// 实现定时器的延迟启用，避免窗体未完全渲染时就开始数据刷新
        /// 首次加载时手动更新一次数据，防止界面空白
        /// </summary>
        private async void Frm_NgStatistics_Load(object sender, EventArgs e)
        {
            try
            {
                // 首次加载时手动更新一次数据，避免等待期间界面空白
                UpdateDataGrids();

                // 延迟指定时间后启用定时器（非阻塞延迟，避免UI冻结）
                await Task.Delay(TimerDelayMs);

                // 启用定时器开始自动刷新数据
                _refreshTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(new Exception("NG统计窗体加载时初始化定时器失败", ex));
            }
        }

        #region 初始化方法
        /// <summary>
        /// 初始化刷新定时器
        /// 配置定时器参数：刷新间隔1秒，绑定刷新事件
        /// 默认禁用定时器，等待窗体加载完成后延迟启用
        /// </summary>
        private void InitializeTimer()
        {
            _refreshTimer.Interval = 1000; // 刷新间隔：1秒（确保实时性）
            _refreshTimer.Tick += RefreshTimer_Tick; // 绑定定时器触发事件
            _refreshTimer.Enabled = false; // 默认不启用
            _datePicker.Value = DateTime.Now.Date; // 日期选择器默认选中当前日期
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 定时器刷新事件：每秒更新数据表格
        /// 实现实时数据展示，反映最新的NG状态
        /// 仅在显示今日数据时刷新（历史数据无需实时更新）
        /// </summary>
        public void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // 测试代码：模拟记录NG事件（实际应从设备获取真实NG数据）
                // 模拟量测NG、压力NG事件（可根据实际需求删除或注释）
                //NgStatisticsManager.Instance.RecordNgEvent(NgCategory.量测NG);
                //NgStatisticsManager.Instance.RecordNgEvent(NgCategory.压力NG);
                //NgStatisticsManager.Instance.RecordNgEvent(NgCategory.压力NG);

                // 仅在显示今日数据时刷新表格（历史数据无需实时更新）
                if (isToday)
                {
                    UpdateDataGrids(); // 更新当前班次和上一班次的NG数据表格
                }
            }
            catch (Exception ex)
            {
                // 记录NG数据刷新失败的异常日志，便于问题排查
                RLog.SaveError(new Exception("NG数据刷新失败", ex));
            }
        }

        /// <summary>
        /// 日期选择器值变更事件：切换历史数据查询
        /// 当用户选择不同日期时，加载对应日期的白班和夜班NG数据，并调整界面状态
        /// </summary>
        public void DatePicker_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // 获取用户选择的日期（忽略时间部分，只保留年月日）
                DateTime selectedDate = _datePicker.Value.Date;
                // 判断是否为当天（用于区分实时数据和历史数据）
                isToday = selectedDate == DateTime.Now.Date;

                // 切换定时器状态：仅当天数据需要实时刷新，历史数据禁用定时器
                _refreshTimer.Enabled = isToday;

                // 非当天时加载历史数据（当天数据使用实时数据源，无需手动加载）
                if (!isToday)
                {
                    _ngStatisticsManager.QueryHistoryNgData(selectedDate); // 加载选中日期的历史NG数据
                }

                // 更新分组框标题（显示当前查询的日期和班次）
                UpdateGroupTitles(selectedDate, isToday);
                // 更新数据表格（切换数据源为历史数据或实时数据）
                UpdateDataGrids(isToday);

                // 如果是当天数据且定时器未启用，则启用定时器
                if (isToday && !_refreshTimer.Enabled)
                {
                    _refreshTimer.Enabled = true;
                }

            }
            catch (Exception ex)
            {
                // 记录日期切换时NG数据加载失败的异常日志
                RLog.SaveError(new Exception("日期切换时NG数据加载失败", ex));
            }
        }

        /// <summary>
        /// 清空当前班次的所有NG分类数据
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
                    ? "当前用户未登录，无法清空当前班次的所有NG分类数据！！！"
                    : "当前用户权限不足，无法清空当前班次的所有NG分类数据！！！";

                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black, errorMessage);
                return;
            }

            Frm_ConfirmBox.Instance.lbl_Message.Text = "确定要清空当前班次的所有NG分类数据吗？此操作不可恢复！";
            Frm_ConfirmBox.Instance.ShowDialog();
            if (Frm_ConfirmBox.Instance.Result != ConfirmBoxResult.Yes) return;

            // 如果用户点击"是"，则执行清空操作
            try
            {
                NgStatisticsManager.Instance.ClearCurrentShiftNgData();
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
        /// 更新数据表格内容
        /// 根据是否为当天数据，切换不同的数据源（实时数据/历史数据）
        /// 仅在数据源变化时更新表格，减少不必要的UI重绘，提升性能
        /// </summary>
        /// <param name="isToday">是否显示当天数据（默认true）</param>
        private void UpdateDataGrids(bool isToday = true)
        {
            // 使用元组一次性获取当前和上一班次的数据源：
            // - 当天：使用实时数据（当前班次和上一班次）
            // - 历史：使用查询的白班和夜班数据
            var (currentDataSource, previousDataSource) = isToday
                ? (
                    _ngStatisticsManager.CurrentShiftData,   // 当天实时：当前班次NG数据
                    _ngStatisticsManager.PreviousShiftData   // 当天实时：上一班次NG数据
                  )
                : (
                    _ngStatisticsManager.HistoryDayShiftData, // 历史数据：白班NG数据
                    _ngStatisticsManager.HistoryNightShiftData // 历史数据：夜班NG数据
                  );

            // 仅在数据源变化时更新表格（减少UI重绘，提升性能）
            if (_currentShiftGridView.DataSource != currentDataSource)
            {
                _currentShiftGridView.DataSource = currentDataSource; // 绑定当前班次/白班数据源
            }
            if (_previousShiftGridView.DataSource != previousDataSource)
            {
                _previousShiftGridView.DataSource = previousDataSource; // 绑定上一班次/夜班数据源
            }

            // 刷新表格显示，确保数据即时更新
            _currentShiftGridView.Refresh();
            _previousShiftGridView.Refresh();
        }

        /// <summary>
        /// 更新分组框标题
        /// 根据是否为当天数据，显示不同的标题（实时班次/历史日期+班次）
        /// 让用户清晰区分当前查看的是实时数据还是历史数据
        /// </summary>
        /// <param name="selectedDate">选中的日期</param>
        /// <param name="isToday">是否为当天</param>
        private void UpdateGroupTitles(DateTime selectedDate, bool isToday)
        {
            // 当前班次/白班分组框标题
            _currentShiftGroupBox.Text = isToday ? "当前班次" : $"{selectedDate:yyyy年MM月dd日} 白班";
            // 上一班次/夜班分组框标题
            _previousShiftGroupBox.Text = isToday ? "上一班次" : $"{selectedDate:yyyy年MM月dd日} 夜班";
        }
        #endregion

    }
}