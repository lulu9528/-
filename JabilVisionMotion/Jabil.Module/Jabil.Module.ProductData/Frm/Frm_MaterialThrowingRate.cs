using System;
using System.Windows.Forms;
using Jabil.Module.ProductData.Lib;
using Sunny.UI;
using Jabil.Module.Log.Lib;
using System.Threading;
using System.Threading.Tasks;
using Jabil.Module.Log.Frm;
using Jabil.Lib;
using System.Drawing;
using Jabil.Module.Permission;

namespace Jabil.Module.ProductData.Frm
{
    /// <summary>
    /// 物料抛料率统计窗体
    /// 核心功能：实时展示当班和上一班次的物料抛料数据（包括抛料数量、抛料率等指标）
    /// 支持通过日期选择器查询历史抛料数据
    /// 采用单例模式确保全系统唯一的抛料率监控界面，保证数据展示一致性
    /// </summary>
    public partial class Frm_MaterialThrowingRate : UIPage
    {
        #region 线程安全单例模式
        /// <summary>
        /// 懒加载单例实例，通过Lazy<T>实现线程安全初始化
        /// LazyThreadSafetyMode.ExecutionAndPublication确保多线程环境下仅创建一个实例
        /// </summary>
        private static readonly Lazy<Frm_MaterialThrowingRate> _lazyInstance =
            new Lazy<Frm_MaterialThrowingRate>(() => new Frm_MaterialThrowingRate(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取窗体单例实例
        /// 外部通过此属性访问物料抛料率统计界面，保证界面唯一性
        /// </summary>
        public static Frm_MaterialThrowingRate Instance => _lazyInstance.Value;
        #endregion

        // 物料抛料管理器实例（缓存，减少重复调用带来的性能开销）
        public readonly MaterialThrowingManager _throwingManager;
        // 标识当前显示的是否为今日数据（用于区分实时数据和历史数据）
        private bool isToday = true;
        // 定时器延迟启动时间（单位：毫秒），可根据需要调整
        private const int TimerDelayMs = 3000;  // 3秒延迟

        /// <summary>
        /// 初始化物料抛料率统计窗体
        /// 完成UI组件初始化、数据管理器绑定和定时器配置
        /// </summary>
        public Frm_MaterialThrowingRate()
        {
            InitializeComponent();
            // 绑定窗体加载事件，用于实现延迟启动定时器
            this.Load += Frm_MaterialThrowingRate_Load;

            // 获取物料抛料管理器单例实例，用于数据交互
            _throwingManager = MaterialThrowingManager.Instance;

            // 初始化定时器（设置刷新间隔和事件绑定）
            InitializeTimer();
        }

        /// <summary>
        /// 窗体加载事件处理程序
        /// 实现定时器的延迟启用，避免窗体未完全加载时就开始数据刷新
        /// </summary>
        private async void Frm_MaterialThrowingRate_Load(object sender, EventArgs e)
        {
            try
            {
                // 首次加载时手动更新一次数据，避免等待期间界面空白
                UpdateDataGrids();

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
        /// 初始化刷新定时器
        /// 配置定时器参数，设置数据刷新频率为1秒/次，确保实时性
        /// </summary>
        private void InitializeTimer()
        {
            _refreshTimer.Interval = 1000; // 刷新间隔：1秒
            _refreshTimer.Tick += RefreshTimer_Tick; // 绑定刷新事件
            _refreshTimer.Enabled = false; // 默认不启用，将在窗体加载后延迟启用
            _datePicker.Value = DateTime.Now.Date;
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 定时器刷新事件：每秒更新数据表格
        /// 实现实时数据展示，反映最新的物料抛料状态
        /// </summary>
        public void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // 测试代码：模拟记录Nozzle1的抛料事件（实际应从设备获取真实抛料数据）
                //MaterialThrowingManager.Instance.RecordThrowing(MaterialThrowingResult.吸头一);

                // 仅在显示今日数据时刷新表格（历史数据无需实时更新）
                if (isToday)
                {
                    // 更新数据表格（当班和上一班数据）
                    UpdateDataGrids();
                }
            }
            catch (Exception ex)
            {
                // 记录刷新失败的异常日志，便于问题排查
                RLog.SaveError(new Exception("抛料数据刷新失败", ex));
            }
        }

        /// <summary>
        /// 日期选择器值变更事件：切换历史数据查询
        /// 当用户选择不同日期时，加载对应日期的白班和夜班抛料数据，并调整界面状态
        /// </summary>
        public void DatePicker_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // 获取用户选择的日期（忽略时间部分）
                DateTime selectedDate = _datePicker.Value.Date;
                // 判断是否为当天（用于区分实时数据和历史数据）
                isToday = selectedDate == DateTime.Now.Date;

                // 切换定时器状态：仅当天数据需要实时刷新，历史数据禁用定时器
                _refreshTimer.Enabled = isToday;

                // 非当天时加载历史数据（当天数据使用实时数据源）
                if (!isToday)
                {
                    _throwingManager.QueryHistoryData(selectedDate);
                }

                // 更新分组框标题（显示当前查询的日期和班次）
                UpdateGroupTitles(selectedDate, isToday);
                // 更新数据表格（切换数据源为历史数据或实时数据）
                UpdateDataGrids(isToday);

                // 如果是当天数据则启用定时器
                if (isToday && !_refreshTimer.Enabled)
                {
                    _refreshTimer.Enabled = true;
                }

            }
            catch (Exception ex)
            {
                // 记录日期切换时的数据加载异常
                RLog.SaveError(new Exception("日期切换时抛料数据加载失败", ex));
            }
        }

        /// <summary>
        /// 清空当前班次的所有抛料率数据
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
                    ? "当前用户未登录，无法清空当前班次的所有抛料率数据！！！"
                    : "当前用户权限不足，无法清空当前班次的所有抛料率数据！！！";

                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black, errorMessage);
                return;
            }

            Frm_ConfirmBox.Instance.lbl_Message.Text = "确定要清空当前班次的所有抛料率数据吗？此操作不可恢复！";
            Frm_ConfirmBox.Instance.ShowDialog();
            if (Frm_ConfirmBox.Instance.Result != ConfirmBoxResult.Yes) return;

            // 如果用户点击"是"，则执行清空操作
            try
            {
                MaterialThrowingManager.Instance.ClearCurrentShiftThrowingData();
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
        /// </summary>
        /// <param name="isToday">是否显示当天数据</param>
        private void UpdateDataGrids(bool isToday = true)
        {
            // 使用元组一次性获取当前和上一班次的数据源：
            // - 当天：使用实时数据（当班和上一班）
            // - 历史：使用查询的白班和夜班数据
            var (currentDataSource, previousDataSource) = isToday
                ? (
                    _throwingManager.CurrentShiftData,   // 当班实时数据
                    _throwingManager.PreviousShiftData   // 上一班实时数据
                  )
                : (
                    _throwingManager.HistoryDayShiftData, // 历史白班数据
                    _throwingManager.HistoryNightShiftData // 历史夜班数据
                  );

            // 仅在数据源变化时更新（减少不必要的UI重绘，提升性能）
            if (_currentShiftGridView.DataSource != currentDataSource)
            {
                _currentShiftGridView.DataSource = currentDataSource;
            }
            if (_previousShiftGridView.DataSource != previousDataSource)
            {
                _previousShiftGridView.DataSource = previousDataSource;
            }

            // 刷新表格显示，确保数据更新
            _currentShiftGridView.Refresh();
            _previousShiftGridView.Refresh();
        }

        /// <summary>
        /// 更新分组框标题
        /// 根据是否为当天数据，显示不同的标题（当班/上一班或具体日期的白班/夜班）
        /// </summary>
        /// <param name="selectedDate">选中的日期</param>
        /// <param name="isToday">是否为当天</param>
        private void UpdateGroupTitles(DateTime selectedDate, bool isToday)
        {
            // 当班/白班分组框标题
            _currentShiftGroupBox.Text = isToday ? "当班" : $"{selectedDate:yyyy年MM月dd日 白班}";
            // 上一班/夜班分组框标题
            _previousShiftGroupBox.Text = isToday ? "上一班" : $"{selectedDate:yyyy年MM月dd日 夜班}";
        }
        #endregion

    }
}
