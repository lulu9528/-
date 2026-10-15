using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Jabil.Module.Log.Lib;       // 日志相关工具类
using Jabil.Lib;                 // 基础工具类
using Jabil.Module.Modbus.Lib;   // Modbus相关核心库
using Jabil.Module.Modbus.Common.Dtos;  // Modbus数据传输对象定义
using Sunny.UI;                  // UI控件库
using HslCommunication;          // 通信库（用于Modbus通信）
using Jabil.Module.Permission;   // 权限管理系统

namespace Jabil.Module.Modbus.Control
{
    /// <summary>
    /// Modbus设备控制界面用户控件
    /// 功能：
    /// 1. 集成Modbus点位实时数据管理器，通过同步方式处理设备数据
    /// 2. 展示数字输入（DI）和数字输出（DO）点位状态
    /// 3. 支持对数字输出点位进行手动操作（需权限验证）
    /// 4. 自动监控设备连接状态和数据变化，定时更新UI
    /// 实现IDisposable接口，确保资源正确释放
    /// </summary>
    public partial class ModbusControlUI : UIUserControl, IDisposable
    {
        #region 字段定义
        /// <summary>
        /// 数字输入（DI）控件集合
        /// 存储界面上所有数字输入点位对应的UI控件，用于统一管理和更新
        /// </summary>
        private readonly List<DIO> _digitalInputs = new List<DIO>();

        /// <summary>
        /// 数字输出（DO）控件集合
        /// 存储界面上所有数字输出点位对应的UI控件，支持点击操作
        /// </summary>
        private readonly List<DIO> _digitalOutputs = new List<DIO>();

        /// <summary>
        /// Modbus设备信息对象
        /// 存储当前控件关联的设备基本信息（如设备名称、类型等）
        /// 构造函数初始化，不可变更
        /// </summary>
        private readonly ModbusCommunicationDto _modbusDevice;

        /// <summary>
        /// 监控任务取消令牌
        /// 用于通知监控线程停止运行（跨线程通信）
        /// ManualResetEvent：线程同步事件，初始为未触发状态
        /// </summary>
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);

        /// <summary>
        /// 监控线程
        /// 独立线程，负责定时读取设备数据并更新到管理器，控制UI刷新节奏
        /// </summary>
        private Thread _monitoringThread;

        /// <summary>
        /// 状态刷新间隔（毫秒）
        /// 监控线程的休眠时间，控制数据读取和UI更新的频率（300ms即约3次/秒）
        /// </summary>
        private const int RefreshInterval = 300;

        /// <summary>
        /// 控件可见性状态
        /// 记录当前控件是否可见，用于优化性能（不可见时暂存数据，可见时一次性更新）
        /// </summary>
        private bool _isVisible = false;

        /// <summary>
        /// UI更新需求标记
        /// 标识是否有缓存的更新数据需要同步到UI（控件不可见时缓存数据，可见时触发更新）
        /// </summary>
        private bool _needsUpdate = false;

        /// <summary>
        /// 待处理的UI更新数据
        /// 控件不可见时，临时存储设备实时数据，避免频繁更新不可见的UI
        /// </summary>
        private IEnumerable<ModbusDioRealTimeData> _pendingUpdateData = null;

        /// <summary>
        /// 待处理数据的线程安全锁
        /// 确保多线程（监控线程和UI线程）对_pendingUpdateData的访问安全
        /// </summary>
        private readonly object _pendingDataLock = new object();
        #endregion

        #region 属性定义
        /// <summary>
        /// 设备名称
        /// 只读属性，返回当前关联设备的名称（从_modbusDevice获取，为空时返回空字符串）
        /// </summary>
        public string DeviceName => _modbusDevice?.设备名称 ?? string.Empty;

        /// <summary>
        /// 每行显示的DIO控件数量
        /// 可读写属性，控制界面上每个标签页中表格布局的行数
        /// 默认值为6，可根据界面需求调整
        /// </summary>
        public int RowCount { get; set; } = 6;

        /// <summary>
        /// 每列显示的DIO控件数量
        /// 可读写属性，控制界面上每个标签页中表格布局的列数
        /// 默认值为2，可根据界面需求调整
        /// </summary>
        public int ColumnCount { get; set; } = 2;

        /// <summary>
        /// 监控启动延迟时间(毫秒)
        /// 可读写属性，控制监控线程启动后延迟多久开始执行数据读取
        /// 确保设备初始化完成，默认1000ms（1秒），最小值为0
        /// </summary>
        public int MonitoringStartDelay
        {
            get => _monitoringStartDelay;
            set => _monitoringStartDelay = Math.Max(0, value);  // 确保值不为负
        }
        private int _monitoringStartDelay = 1000;

        /// <summary>
        /// 设备连接状态
        /// 只读属性，判断当前设备是否处于连接状态
        /// 根据设备类型（PLC/Robot）从Modbus系统中查询对应设备的连接状态
        /// </summary>
        public bool IsDeviceConnected
        {
            get
            {
                // 区分设备类型，查询对应的设备列表中是否存在已连接的设备
                return _modbusDevice.设备类型 == "PLC"
                    ? ModbusSystem.Instance.PlcModbusList.Any(p => p.DeviceName == _modbusDevice.设备名称 && p.IsConnected)
                    : ModbusSystem.Instance.RobotModbusList.Any(r => r.DeviceName == _modbusDevice.设备名称 && r.IsConnected);
            }
        }
        #endregion

        #region 构造函数与析构
        /// <summary>
        /// 初始化ModbusControlUI实例
        /// </summary>
        /// <param name="device">Modbus设备信息对象（不可为null）</param>
        /// <exception cref="ArgumentNullException">当device为null时抛出</exception>
        public ModbusControlUI(ModbusCommunicationDto device)
        {
            // 验证设备信息不为null
            _modbusDevice = device ?? throw new ArgumentNullException(nameof(device), "设备信息不能为空");
            // 初始化Windows窗体控件（设计器自动生成的代码）
            InitializeComponent();
            // 设置控件样式以优化绘制性能（双缓冲、避免闪烁）
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            UpdateStyles();
            // 绑定控件销毁事件，确保资源释放
            Disposed += ModbusControlUI_Disposed;
            // 绑定可见性变化事件，用于控制数据更新策略
            this.VisibleChanged += ModbusControlUI_VisibleChanged;
            // 初始化可见性状态
            _isVisible = this.Visible;
        }

        /// <summary>
        /// 析构函数
        /// 用于释放非托管资源，由垃圾回收器调用
        /// 调用Dispose(false)，表示非用户主动释放
        /// </summary>
        ~ModbusControlUI()
        {
            Dispose(false);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 刷新DIO点位配置信息
        /// 功能：
        /// 1. 确保实时数据管理器已初始化，未初始化则尝试初始化
        /// 2. 已初始化则刷新配置缓存
        /// 3. 在UI线程中重新初始化数字输入和输出标签页（刷新界面控件）
        /// 异常处理：捕获并记录刷新过程中的错误
        /// </summary>
        public void RefreshDioPoints()
        {
            try
            {
                // 确保实时数据管理器已初始化
                if (!ModbusDioRealTimeManager.IsInitialized)
                {
                    if (!ModbusDioRealTimeManager.Initialize())
                    {
                        ShowErrorTip("Modbus实时数据管理器初始化失败");
                        return;
                    }
                }
                else
                {
                    // 刷新配置缓存（例如配置文件更新后）
                    ModbusDioRealTimeManager.RefreshConfigCache();
                }

                // 在UI线程更新界面（避免跨线程操作控件）
                WFUtil.UpdateMainThreadUIAsync(this, () =>
                {
                    InitializeDigitalInputTabs();
                    InitializeDigitalOutputTabs();
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, "刷新DIO点位信息时发生错误");
            }
        }
        #endregion

        #region 事件处理方法
        /// <summary>
        /// 控件可见性变化事件处理
        /// 当控件从不可见变为可见时，立即处理所有缓存的更新数据
        /// 避免控件不可见时积累的大量数据未更新到UI
        /// </summary>
        /// <param name="sender">事件源（当前控件）</param>
        /// <param name="e">事件参数</param>
        private void ModbusControlUI_VisibleChanged(object sender, EventArgs e)
        {
            bool wasVisible = _isVisible;
            _isVisible = this.Visible;

            // 状态从不可见变为可见时，处理缓存数据
            if (!wasVisible && _isVisible)
            {
                ProcessPendingUpdates();
            }
        }

        /// <summary>
        /// 控件加载事件处理
        /// 控件首次加载时执行初始化操作：
        /// 1. 初始化实时数据管理器
        /// 2. 初始化数字输入和输出标签页（创建UI控件）
        /// 3. 启动监控线程
        /// 设计模式下不执行（避免设计器报错）
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        private void ModbusControlUI_Load(object sender, EventArgs e)
        {
            // 设计模式下退出（防止设计器中执行代码导致异常）
            if (DesignMode) return;

            try
            {
                // 初始化实时数据管理器
                if (!ModbusDioRealTimeManager.Initialize())
                {
                    ShowErrorTip("Modbus实时数据管理器初始化失败");
                    return;
                }

                // 初始化界面控件
                InitializeDigitalInputTabs();
                InitializeDigitalOutputTabs();

                // 启动监控线程
                StartModbusMonitoring();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"[{_modbusDevice.设备名称}] 控件加载失败");
                ShowErrorTip("控件初始化失败，请查看日志");
            }
        }

        /// <summary>
        /// 控件销毁事件处理
        /// 释放资源：
        /// 1. 停止监控线程
        /// 2. 释放所有DIO控件
        /// 3. 释放事件对象
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        private void ModbusControlUI_Disposed(object sender, EventArgs e)
        {
            // 通知监控线程停止（触发_stopEvent）
            _stopEvent.Set();

            // 等待线程结束（最多等待1秒，避免无限阻塞）
            if (_monitoringThread != null && _monitoringThread.IsAlive)
            {
                _monitoringThread.Join(1000);
            }

            // 释放所有DIO控件资源
            foreach (var dio in _digitalInputs.Concat(_digitalOutputs))
            {
                dio.Dispose();
            }
            // 清空集合
            _digitalInputs.Clear();
            _digitalOutputs.Clear();
            // 释放事件对象
            _stopEvent.Dispose();
        }

        /// <summary>
        /// 数字输出控件点击事件处理
        /// 响应DO控件的点击操作，触发输出点位的状态切换
        /// 确保在UI线程中执行，避免跨线程操作控件
        /// </summary>
        /// <param name="sender">事件源（点击的DIO控件）</param>
        /// <param name="e">事件参数</param>
        private void DigitalOutput_Click(object sender, EventArgs e)
        {
            // 验证 sender 是DIO控件
            if (!(sender is DIO outputControl)) return;

            // 在UI线程中执行操作（避免跨线程异常）
            WFUtil.UpdateMainThreadUIAsync(this, () =>
            {
                // 控件禁用时不处理
                if (!outputControl.Enabled) return;

                try
                {
                    // 临时禁用控件，防止重复点击
                    outputControl.Enabled = false;
                    // 处理输出操作（切换状态）
                    HandleDigitalOutputOperation(outputControl);
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex, $"[{_modbusDevice.设备名称}] 输出控件操作失败");
                    ShowErrorTip("操作失败，请查看日志");
                }
                finally
                {
                    // 无论成功失败，恢复控件可用状态
                    outputControl.Enabled = true;
                }
            });
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化数字输入标签页
        /// 功能：
        /// 1. 从实时数据管理器获取当前设备的所有输入点位配置
        /// 2. 调用CreateTabPages创建标签页及对应的DIO控件
        /// 输入点位控件不可点击（只读）
        /// </summary>
        private void InitializeDigitalInputTabs()
        {
            // 从管理器获取当前设备的输入点位（筛选类型为"输入"的点位，按索引排序）
            var inputPoints = ModbusDioRealTimeManager
                .GetDeviceConfigByDevice(_modbusDevice.设备名称)
                .Where(p => p.点位类型 == "输入")
                .OrderBy(p => p.点位索引)
                .ToList();

            // 创建输入标签页（控件不可点击，无点击事件）
            CreateTabPages(uiTabControl_输入, inputPoints, false, _digitalInputs, null);
        }

        /// <summary>
        /// 初始化数字输出标签页
        /// 功能：
        /// 1. 从实时数据管理器获取当前设备的所有输出点位配置
        /// 2. 调用CreateTabPages创建标签页及对应的DIO控件
        /// 输出点位控件可点击，绑定DigitalOutput_Click事件
        /// </summary>
        private void InitializeDigitalOutputTabs()
        {
            // 从管理器获取当前设备的输出点位（筛选类型为"输出"的点位，按索引排序）
            var outputPoints = ModbusDioRealTimeManager
                .GetDeviceConfigByDevice(_modbusDevice.设备名称)
                .Where(p => p.点位类型 == "输出")
                .OrderBy(p => p.点位索引)
                .ToList();

            // 创建输出标签页（控件可点击，绑定点击事件）
            CreateTabPages(uiTabControl_输出, outputPoints, true, _digitalOutputs, DigitalOutput_Click);
        }

        /// <summary>
        /// 创建标签页页面
        /// 通用方法：根据点位列表创建多个标签页，每个标签页包含表格布局的DIO控件
        /// </summary>
        /// <param name="tabControl">目标标签控件（如uiTabControl_输入）</param>
        /// <param name="points">点位配置列表</param>
        /// <param name="isEnabled">DIO控件是否启用（输出控件启用，输入控件禁用）</param>
        /// <param name="dioList">存储创建的DIO控件的集合</param>
        /// <param name="clickHandler">点击事件处理器（输出控件需要，输入控件为null）</param>
        /// <exception cref="ArgumentNullException">当tabControl或dioList为null时抛出</exception>
        private void CreateTabPages(UITabControl tabControl, List<ModbusCommunicationDioDto> points,
                                   bool isEnabled, List<DIO> dioList, DIO.ClickEventHandler clickHandler = null)
        {
            // 参数验证
            if (tabControl == null) throw new ArgumentNullException(nameof(tabControl));
            if (dioList == null) throw new ArgumentNullException(nameof(dioList));

            // 清空现有控件和集合
            dioList.Clear();
            tabControl.Controls.Clear();

            // 无点位时隐藏标签控件
            if (!points.Any())
            {
                tabControl.Visible = false;
                return;
            }

            // 显示标签控件
            tabControl.Visible = true;

            // 计算分页：根据每页显示数量（行×列）计算总页数
            int totalControls = points.Count;
            int controlsPerPage = RowCount * ColumnCount;
            int pageCount = (totalControls + controlsPerPage - 1) / controlsPerPage;  // 向上取整

            // 为每一页创建标签页
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                // 创建表格布局面板（用于排列DIO控件）
                var tablePanel = CreateTableLayoutPanel();
                // 获取当前页的点位（分页处理）
                var pagePoints = points.Skip(pageIndex * controlsPerPage).Take(controlsPerPage).ToList();

                // 为当前页的每个点位创建DIO控件
                foreach (var point in pagePoints)
                {
                    var dioControl = CreateDioControl(point, isEnabled);
                    // 绑定点击事件（输出控件）
                    if (clickHandler != null)
                    {
                        dioControl.ControlClick += clickHandler;
                    }
                    // 添加到表格和集合
                    tablePanel.Controls.Add(dioControl);
                    dioList.Add(dioControl);
                }

                // 创建标签页并添加到标签控件
                tabControl.Controls.Add(CreateTabPage(pageIndex, tablePanel));
            }
        }

        /// <summary>
        /// 创建表格布局面板
        /// 配置表格的行列数、样式，用于整齐排列DIO控件
        /// </summary>
        /// <returns>配置好的UITableLayoutPanel</returns>
        private UITableLayoutPanel CreateTableLayoutPanel()
        {
            var table = new UITableLayoutPanel
            {
                Dock = DockStyle.Fill,           // 填充父容器
                RowCount = RowCount,             // 行数（由RowCount属性指定）
                ColumnCount = ColumnCount,       // 列数（由ColumnCount属性指定）
                Padding = new Padding(5),        // 内边距
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single  // 单元格边框样式
            };

            // 配置行样式（平均分配高度）
            for (int i = 0; i < RowCount; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / RowCount));
            }

            // 配置列样式（平均分配宽度）
            for (int i = 0; i < ColumnCount; i++)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / ColumnCount));
            }

            return table;
        }

        /// <summary>
        /// 创建DIO控件
        /// 根据点位配置初始化DIO控件的属性（名称、初始值、状态等）
        /// </summary>
        /// <param name="point">点位配置信息</param>
        /// <param name="isEnabled">控件是否启用</param>
        /// <returns>初始化后的DIO控件</returns>
        /// <exception cref="ArgumentNullException">当point为null时抛出</exception>
        private DIO CreateDioControl(ModbusCommunicationDioDto point, bool isEnabled)
        {
            if (point == null) throw new ArgumentNullException(nameof(point));

            // 从管理器获取点位的实时数据（初始值）
            var realTimeData = ModbusDioRealTimeManager.GetRealTimeData(point.设备名称, point.点位索引);

            // 初始化DIO控件属性
            return new DIO
            {
                Dock = DockStyle.Fill,                   // 填充单元格
                Margin = new Padding(3),                 // 边距
                Enabled = isEnabled,                     // 是否启用（输出控件启用）
                PointName = point.点位名称 ?? $"未命名_{point.点位索引}",  // 点位名称（默认值：未命名_索引）
                PointValue = realTimeData?.RealTimeValue ?? point.点位值,   // 显示值（优先实时数据，无则用配置值）
                PointIndex = point.点位索引 ?? string.Empty,  // 点位索引
                点位状态 = realTimeData != null ? (Convert.ToBoolean(realTimeData.RealTimeValue) ? 1 : 0) : 999  // 状态（1：真，0：假，999：无效）
            };
        }

        /// <summary>
        /// 创建标签页
        /// 为每个分页创建标签页，并添加表格布局面板作为内容
        /// </summary>
        /// <param name="pageIndex">页索引（从0开始）</param>
        /// <param name="contentControl">标签页内容（表格布局面板）</param>
        /// <returns>创建的TabPage</returns>
        /// <exception cref="ArgumentNullException">当contentControl为null时抛出</exception>
        private TabPage CreateTabPage(int pageIndex, UITableLayoutPanel contentControl)
        {
            if (contentControl == null) throw new ArgumentNullException(nameof(contentControl));

            var tabPage = new TabPage
            {
                BackColor = Color.Transparent,           // 透明背景
                Font = new Font("微软雅黑", 12F, FontStyle.Bold, GraphicsUnit.Point, 134),  // 字体样式
                Name = $"tabPage_{pageIndex}",           // 名称（用于标识）
                Size = new Size(450, 628),               // 尺寸
                TabIndex = pageIndex,                    // 索引
                Text = $"第{pageIndex + 1}页"            // 显示文本（从1开始计数）
            };
            contentControl.Dock = DockStyle.Fill;       // 内容填充标签页
            tabPage.Controls.Add(contentControl);       // 添加内容控件

            return tabPage;
        }
        #endregion

        #region Modbus操作方法
        /// <summary>
        /// 启动Modbus监控线程
        /// 创建并启动后台线程，执行MonitoringLoop方法（数据监控循环）
        /// 线程名称包含设备名，便于日志跟踪
        /// </summary>
        private void StartModbusMonitoring()
        {
            _monitoringThread = new Thread(MonitoringLoop)
            {
                IsBackground = true,                     // 设为后台线程（程序退出时自动终止）
                Name = $"ModbusMonitor_{_modbusDevice.设备名称}"  // 线程名称（含设备名）
            };
            _monitoringThread.Start();                  // 启动线程
        }

        /// <summary>
        /// 监控循环（线程执行的方法）
        /// 核心逻辑：
        /// 1. 启动延迟（等待设备初始化）
        /// 2. 循环读取设备数据并更新到管理器
        /// 3. 根据控件可见性决定实时更新UI或缓存数据
        /// 4. 响应停止信号，退出循环
        /// </summary>
        private void MonitoringLoop()
        {
            try
            {
                // 记录线程启动日志
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black,
                    $"[{_modbusDevice.设备名称}] Modbus监控线程将在{MonitoringStartDelay}毫秒后启动...");

                // 启动延迟（等待设备就绪，期间可响应停止信号）
                if (MonitoringStartDelay > 0)
                {
                    if (_stopEvent.WaitOne(MonitoringStartDelay))
                    {
                        return; // 收到停止信号，退出线程
                    }
                }

                // 记录线程启动成功日志
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black,
                    $"[{_modbusDevice.设备名称}] Modbus监控线程已启动");

                // 监控主循环（未收到停止信号则持续运行）
                while (!_stopEvent.WaitOne(0))
                {
                    // 检查是否需要停止监控（如程序退出、测试模式等）
                    if (ShouldStopMonitoring()) break;

                    // 读取设备数据并更新到管理器（无论控件是否可见，确保数据最新）
                    UpdateDeviceDataToManager();

                    // 根据控件可见性处理UI更新
                    if (_isVisible)
                    {
                        // 可见时：先处理缓存数据，再实时更新UI
                        if (_needsUpdate)
                        {
                            ProcessPendingUpdates();
                        }
                        UpdateUiFromManager();
                    }
                    else
                    {
                        // 不可见时：缓存数据，标记需要更新（避免无效UI操作）
                        CacheUpdateData();
                        _needsUpdate = true;
                    }

                    // 等待刷新间隔（期间可响应停止信号）
                    if (_stopEvent.WaitOne(RefreshInterval))
                    {
                        break; // 收到停止信号，退出循环
                    }
                }

                // 记录线程停止日志
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black,
                    $"[{_modbusDevice.设备名称}] Modbus监控线程已停止");
            }
            catch (Exception ex)
            {
                // 记录线程异常日志
                RLog.SaveError(ex, $"[{_modbusDevice.设备名称}] Modbus监控线程异常");
            }
        }

        /// <summary>
        /// 缓存更新数据
        /// 控件不可见时，将设备实时数据缓存到_pendingUpdateData
        /// 使用锁确保线程安全
        /// </summary>
        private void CacheUpdateData()
        {
            // 从管理器获取设备所有实时数据
            var deviceData = ModbusDioRealTimeManager.GetDeviceRealTimeData(_modbusDevice.设备名称);
            if (deviceData != null && deviceData.Any())
            {
                // 锁定缓存数据，确保线程安全
                lock (_pendingDataLock)
                {
                    _pendingUpdateData = deviceData;
                }
            }
        }

        /// <summary>
        /// 处理缓存的更新数据
        /// 控件从不可见变为可见时，将缓存的数据更新到UI控件
        /// 使用锁确保线程安全，避免数据读取过程中被修改
        /// </summary>
        private void ProcessPendingUpdates()
        {
            IEnumerable<ModbusDioRealTimeData> pendingData = null;

            // 锁定并获取缓存数据（原子操作）
            lock (_pendingDataLock)
            {
                pendingData = _pendingUpdateData;
                _pendingUpdateData = null; // 清空缓存
            }

            // 用缓存数据更新输入和输出控件
            if (pendingData != null)
            {
                UpdateControlsFromData(_digitalInputs, pendingData);
                UpdateControlsFromData(_digitalOutputs, pendingData);
            }

            // 清除更新需求标记
            _needsUpdate = false;
        }

        /// <summary>
        /// 检查是否应该停止监控
        /// 满足以下条件之一时停止监控：
        /// 1. 全局变量实例不存在
        /// 2. 程序处于测试模式
        /// 3. 程序正在退出
        /// </summary>
        /// <returns>是否需要停止监控</returns>
        private bool ShouldStopMonitoring()
        {
            return Project.Instance?.GlobalVariables == null ||
                   Project.Instance.GlobalVariables.IsTestMode ||
                   Project.Instance.GlobalVariables.IsProgramExiting;
        }

        /// <summary>
        /// 读取设备数据并更新到管理器
        /// 根据设备类型（PLC/Robot）调用对应的更新方法
        /// 异常时记录错误日志
        /// </summary>
        private void UpdateDeviceDataToManager()
        {
            if (_modbusDevice == null) return;

            try
            {
                // 根据设备类型分发处理
                switch (_modbusDevice.设备类型)
                {
                    case "PLC":
                        UpdatePlcDataToManager();
                        break;
                    case "Robot":
                        UpdateRobotDataToManager();
                        break;
                    default:
                        // 记录不支持的设备类型日志
                        RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                            $"[{_modbusDevice.设备名称}] 不支持的设备类型: {_modbusDevice.设备类型}");
                        break;
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"[{_modbusDevice.设备名称}] 更新设备数据到管理器失败");
            }
        }

        /// <summary>
        /// 读取PLC数据并更新到管理器
        /// 功能：
        /// 1. 查找已连接的PLC设备
        /// 2. 未找到则标记所有点位为无效
        /// 3. 找到则逐个读取点位值并更新到实时数据管理器
        /// </summary>
        private void UpdatePlcDataToManager()
        {
            // 查找已连接的PLC设备
            var plc = ModbusSystem.Instance.PlcModbusList
                .FirstOrDefault(p => p.DeviceName == _modbusDevice.设备名称 && p.IsConnected);

            if (plc == null)
            {
                // 设备未连接，标记所有点位为无效
                MarkAllPointsAsInvalid();
                return;
            }

            // 获取设备所有点位配置
            var devicePoints = ModbusDioRealTimeManager.GetDeviceConfigByDevice(_modbusDevice.设备名称);

            // 逐个读取点位值并更新到管理器
            foreach (var point in devicePoints)
            {
                try
                {
                    var (value, success) = ReadPlcValue(plc, point);
                    ModbusDioRealTimeManager.UpdateRealTimeValue(point.设备名称, point.点位索引, value, success);
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex, $"[{_modbusDevice.设备名称}] 读取PLC点位 {point.点位名称} 失败");
                    ModbusDioRealTimeManager.UpdateRealTimeValue(point.设备名称, point.点位索引, null, false);
                }
            }
        }

        /// <summary>
        /// 读取机器人数据并更新到管理器
        /// 功能类似UpdatePlcDataToManager，针对机器人设备处理
        /// </summary>
        private void UpdateRobotDataToManager()
        {
            // 查找已连接的机器人设备
            var robot = ModbusSystem.Instance.RobotModbusList
                .FirstOrDefault(r => r.DeviceName == _modbusDevice.设备名称 && r.IsConnected);

            if (robot == null)
            {
                // 设备未连接，标记所有点位为无效
                MarkAllPointsAsInvalid();
                return;
            }

            // 获取设备所有点位配置
            var devicePoints = ModbusDioRealTimeManager.GetDeviceConfigByDevice(_modbusDevice.设备名称);

            // 逐个读取点位值并更新到管理器
            foreach (var point in devicePoints)
            {
                try
                {
                    var (value, success) = ReadRobotValue(robot, point);
                    ModbusDioRealTimeManager.UpdateRealTimeValue(point.设备名称, point.点位索引, value, success);
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex, $"[{_modbusDevice.设备名称}] 读取机器人点位 {point.点位名称} 失败");
                    ModbusDioRealTimeManager.UpdateRealTimeValue(point.设备名称, point.点位索引, null, false);
                }
            }
        }

        /// <summary>
        /// 标记所有点位为无效
        /// 当设备未连接时，将设备所有点位的状态更新为无效（IsValid=false）
        /// </summary>
        private void MarkAllPointsAsInvalid()
        {
            var devicePoints = ModbusDioRealTimeManager.GetDeviceConfigByDevice(_modbusDevice.设备名称);
            foreach (var point in devicePoints)
            {
                ModbusDioRealTimeManager.UpdateRealTimeValue(
                    point.设备名称, point.点位索引, null, false);
            }
        }

        /// <summary>
        /// 从管理器更新UI控件
        /// 从实时数据管理器获取最新数据，更新到输入和输出控件
        /// </summary>
        private void UpdateUiFromManager()
        {
            // 获取设备所有实时数据
            var deviceData = ModbusDioRealTimeManager.GetDeviceRealTimeData(_modbusDevice.设备名称);
            if (deviceData == null || !deviceData.Any()) return;

            // 更新输入和输出控件
            UpdateControlsFromData(_digitalInputs, deviceData);
            UpdateControlsFromData(_digitalOutputs, deviceData);
        }

        /// <summary>
        /// 根据实时数据更新控件
        /// 将实时数据与控件绑定，更新控件的显示值和状态
        /// 确保在UI线程中更新控件属性
        /// </summary>
        /// <param name="controls">需要更新的DIO控件集合</param>
        /// <param name="dataList">实时数据列表</param>
        private void UpdateControlsFromData(List<DIO> controls, IEnumerable<ModbusDioRealTimeData> dataList)
        {
            if (controls == null || !controls.Any()) return;

            // 遍历控件，匹配对应的数据并更新
            foreach (var control in controls)
            {
                // 找到与控件匹配的实时数据（设备名称和点位索引一致）
                var data = dataList.FirstOrDefault(d =>
                    d.DeviceName == _modbusDevice.设备名称 &&
                    d.PointIndex == control.PointIndex);

                if (data != null)
                {
                    // 在UI线程中更新控件（避免跨线程异常）
                    WFUtil.UpdateMainThreadUIAsync(this, () =>
                    {
                        // 控件已释放则不更新
                        if (!control.IsDisposed)
                        {
                            control.PointValue = data.RealTimeValue ?? "无值";  // 显示值（无值时显示"无值"）
                            // 状态：有效时根据值判断（1/0），无效时为999
                            control.点位状态 = data.IsValid
                                ? (Convert.ToBoolean(data.RealTimeValue) ? 1 : 0)
                                : 999;
                        }
                    });
                }
            }
        }

        /// <summary>
        /// 读取PLC点位值
        /// 根据点位的数据类型（bool/int16/int32）调用对应的读取方法
        /// 返回值包含读取结果和成功状态
        /// </summary>
        /// <param name="plc">PLC设备对象</param>
        /// <param name="point">点位配置</param>
        /// <returns>（值，是否成功）</returns>
        /// <exception cref="ArgumentNullException">当plc或point为null时抛出</exception>
        private (object value, bool success) ReadPlcValue(PLC_Modbus plc, ModbusCommunicationDioDto point)
        {
            // 参数验证
            if (plc == null) throw new ArgumentNullException(nameof(plc));
            if (point == null) throw new ArgumentNullException(nameof(point));
            if (string.IsNullOrEmpty(point.点位索引))
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                    $"[{_modbusDevice.设备名称}] 点位 {point.点位名称} 索引为空");
                return (null, false);
            }

            try
            {
                // 根据数据类型调用对应的读取方法
                return point.数据类型 switch
                {
                    "bool" => (plc.ReadBool(point.点位索引).Content, true),
                    "int16" => (plc.ReadInt16(point.点位索引).Content, true),
                    "int32" => (plc.ReadInt32(point.点位索引).Content, true),
                    _ => (null, false)  // 不支持的数据类型
                };
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"[{_modbusDevice.设备名称}] 读取PLC点位 {point.点位名称} 失败");
                return (null, false);
            }
        }

        /// <summary>
        /// 读取机器人点位值
        /// 类似ReadPlcValue，针对机器人设备处理（支持bool/int16类型）
        /// </summary>
        /// <param name="robot">机器人设备对象</param>
        /// <param name="point">点位配置</param>
        /// <returns>（值，是否成功）</returns>
        /// <exception cref="ArgumentNullException">当robot或point为null时抛出</exception>
        private (object value, bool success) ReadRobotValue(Robot_Modbus robot, ModbusCommunicationDioDto point)
        {
            // 参数验证
            if (robot == null) throw new ArgumentNullException(nameof(robot));
            if (point == null) throw new ArgumentNullException(nameof(point));
            if (string.IsNullOrEmpty(point.点位索引))
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                    $"[{_modbusDevice.设备名称}] 点位 {point.点位名称} 索引为空");
                return (null, false);
            }

            try
            {
                // 根据数据类型调用对应的读取方法
                return point.数据类型 switch
                {
                    "bool" => (robot.ReadBool(point.点位索引).Content, true),
                    "int16" => (robot.ReadInt16(point.点位索引).Content, true),
                    _ => (null, false)  // 不支持的数据类型
                };
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"[{_modbusDevice.设备名称}] 读取机器人点位 {point.点位名称} 失败");
                return (null, false);
            }
        }

        /// <summary>
        /// 处理数字输出操作
        /// 响应输出控件点击事件，执行状态切换逻辑：
        /// 1. 检查设备连接状态
        /// 2. 检查设备运行模式（自动模式禁止操作）
        /// 3. 查找点位配置信息
        /// 4. 检查操作权限（注释掉的代码，可按需启用）
        /// 5. 根据设备类型调用对应的处理方法
        /// </summary>
        /// <param name="outputControl">点击的输出控件</param>
        /// <exception cref="ArgumentNullException">当outputControl为null时抛出</exception>
        private void HandleDigitalOutputOperation(DIO outputControl)
        {
            if (outputControl == null) throw new ArgumentNullException(nameof(outputControl));

            // 检查设备连接状态
            if (!IsDeviceConnected)
            {
                ShowErrorTip("设备未连接，无法操作");
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                    $"[{_modbusDevice.设备名称}] 设备未连接，操作 {outputControl.PointName} 失败");
                return;
            }

            // 检查运行模式（自动模式禁止手动操作）
            if (MenuProxy.Instance?.MachineRunModel == MachineMode.Auto)
            {
                ShowErrorTip("自动模式下不允许手动操作，请切换到手动模式");
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                    $"[{_modbusDevice.设备名称}] 自动模式下禁止操作 {outputControl.PointName}");
                return;
            }

            // 从管理器获取点位配置信息
            var point = ModbusDioRealTimeManager
                .GetDeviceConfigByDevice(_modbusDevice.设备名称)
                .FirstOrDefault(p => p.点位名称 == outputControl.PointName);

            if (point == null)
            {
                ShowErrorTip("未找到对应的点位配置信息");
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                    $"[{_modbusDevice.设备名称}] 未找到点位 {outputControl.PointName} 的配置信息");
                return;
            }

            // 检查操作权限（当前注释掉，如需启用可取消注释）
            //if (!CheckOperationPermission(point))
            //{
            //    ShowErrorTip("没有操作权限");
            //    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
            //        $"[{_modbusDevice.设备名称}] 用户无权限操作 {outputControl.PointName}");
            //    return;
            //}

            // 根据设备类型处理输出操作
            switch (_modbusDevice.设备类型)
            {
                case "PLC":
                    HandlePlcOutputOperation(point);
                    break;
                case "Robot":
                    HandleRobotOutputOperation(point);
                    break;
                default:
                    ShowErrorTip($"不支持的设备类型: {_modbusDevice.设备类型}");
                    break;
            }
        }

        /// <summary>
        /// 检查操作权限
        /// 验证当前用户是否有操作该点位的权限：
        /// 1. 拥有该点位的特定权限（Modbus/设备名/点位名）
        /// 2. 拥有所有Modbus操作权限（Modbus/All）
        /// 异常时记录日志并返回false
        /// </summary>
        /// <param name="point">点位配置</param>
        /// <returns>是否有权限</returns>
        private bool CheckOperationPermission(ModbusCommunicationDioDto point)
        {
            try
            {
                return PermissionSystem.CheckPermission($"Modbus/{_modbusDevice.设备名称}/{point.点位名称}", out _) ||
                       PermissionSystem.CheckPermission("Modbus/All", out _);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, "检查操作权限失败");
                return false;
            }
        }

        /// <summary>
        /// 处理PLC输出操作
        /// 针对PLC设备的输出点位，执行状态切换：
        /// 1. 查找PLC设备
        /// 2. 获取当前点位值，计算新值（取反或0/设定值切换）
        /// 3. 写入新值到PLC
        /// 4. 成功则更新实时数据管理器
        /// </summary>
        /// <param name="point">点位配置</param>
        private void HandlePlcOutputOperation(ModbusCommunicationDioDto point)
        {
            // 查找PLC设备
            var plc = ModbusSystem.Instance.PlcModbusList
                .FirstOrDefault(p => p.DeviceName == _modbusDevice.设备名称);

            if (plc == null)
            {
                ShowErrorTip("未找到PLC设备");
                return;
            }

            try
            {
                object newValue = null;
                // 获取当前点位的实时数据
                var currentData = ModbusDioRealTimeManager.GetRealTimeData(point.设备名称, point.点位索引);

                if (currentData == null)
                {
                    ShowErrorTip("未找到当前点位数据");
                    return;
                }

                // 根据数据类型计算新值（切换状态）
                switch (point.数据类型)
                {
                    case "bool":
                        // bool类型：取反（true→false，false→true）
                        newValue = !Convert.ToBoolean(currentData.RealTimeValue);
                        break;
                    case "int16":
                        // int16类型：0→配置值，配置值→0
                        newValue = Convert.ToInt16(currentData.RealTimeValue) == 0
                            ? Convert.ToInt16(point.点位值)
                            : (short)0;
                        break;
                    case "int32":
                        // int32类型：0→配置值，配置值→0
                        newValue = Convert.ToInt32(currentData.RealTimeValue) == 0
                            ? Convert.ToInt32(point.点位值)
                            : 0;
                        break;
                    default:
                        ShowErrorTip($"不支持的PLC数据类型: {point.数据类型}");
                        return;
                }

                // 写入新值到PLC
                var writeSuccess = WriteToPlc(plc, point, newValue);
                if (writeSuccess)
                {
                    // 成功则更新管理器中的值
                    ModbusDioRealTimeManager.UpdateRealTimeValue(
                        point.设备名称, point.点位索引, newValue, true);

                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green,
                        $"操作成功：{_modbusDevice.设备名称}, {point.点位名称}--》新值：{newValue}");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"[{_modbusDevice.设备名称}] 处理PLC输出操作失败: {point.点位名称}");
                ShowErrorTip("操作失败，请查看日志");
            }
        }

        /// <summary>
        /// 写入PLC数据
        /// 根据数据类型调用对应的写入方法，返回写入结果
        /// 失败时记录日志并显示错误提示
        /// </summary>
        /// <param name="plc">PLC设备对象</param>
        /// <param name="point">点位配置</param>
        /// <param name="value">要写入的值</param>
        /// <returns>是否写入成功</returns>
        private bool WriteToPlc(PLC_Modbus plc, ModbusCommunicationDioDto point, object value)
        {
            try
            {
                // 根据数据类型调用对应的写入方法
                OperateResult result = point.数据类型 switch
                {
                    "bool" => plc.WriteBool(Convert.ToBoolean(value), point.点位索引),
                    "int16" => plc.WriteInt16(Convert.ToInt16(value), point.点位索引),
                    "int32" => plc.WriteInt32(Convert.ToInt32(value), point.点位索引),
                    _ => new OperateResult("不支持的数据类型")
                };

                // 处理写入失败
                if (!result.IsSuccess)
                {
                    ShowErrorTip($"写入失败: {result.Message}");
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                        $"[{_modbusDevice.设备名称}] 写入 {point.点位名称} 失败: {result.Message}");
                }

                return result.IsSuccess;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"写入PLC点位 {point.点位名称} 失败");
                return false;
            }
        }

        /// <summary>
        /// 处理机器人输出操作
        /// 类似HandlePlcOutputOperation，针对机器人设备处理（支持bool/int16类型）
        /// </summary>
        /// <param name="point">点位配置</param>
        private void HandleRobotOutputOperation(ModbusCommunicationDioDto point)
        {
            // 查找机器人设备
            var robot = ModbusSystem.Instance.RobotModbusList
                .FirstOrDefault(r => r.DeviceName == _modbusDevice.设备名称);

            if (robot == null)
            {
                ShowErrorTip("未找到机器人设备");
                return;
            }

            try
            {
                object newValue = null;
                // 获取当前点位的实时数据
                var currentData = ModbusDioRealTimeManager.GetRealTimeData(point.设备名称, point.点位索引);

                if (currentData == null)
                {
                    ShowErrorTip("未找到当前点位数据");
                    return;
                }

                // 根据数据类型计算新值（切换状态）
                switch (point.数据类型)
                {
                    case "bool":
                        // bool类型：取反
                        newValue = !Convert.ToBoolean(currentData.RealTimeValue);
                        break;
                    case "int16":
                        // int16类型：0→配置值，配置值→0
                        newValue = Convert.ToInt16(currentData.RealTimeValue) == 0
                            ? Convert.ToInt16(point.点位值)
                            : (short)0;
                        break;
                    default:
                        ShowErrorTip($"不支持的机器人数据类型: {point.数据类型}");
                        return;
                }

                // 写入新值到机器人
                var writeSuccess = WriteToRobot(robot, point, newValue);
                if (writeSuccess)
                {
                    // 成功则更新管理器中的值
                    ModbusDioRealTimeManager.UpdateRealTimeValue(
                        point.设备名称, point.点位索引, newValue, true);

                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green,
                        $"操作成功：{_modbusDevice.设备名称}, {point.点位名称}--》新值：{newValue}");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"[{_modbusDevice.设备名称}] 处理机器人输出操作失败: {point.点位名称}");
                ShowErrorTip("操作失败，请查看日志");
            }
        }

        /// <summary>
        /// 写入机器人数据
        /// 类似WriteToPlc，针对机器人设备处理（支持bool/int16类型）
        /// </summary>
        /// <param name="robot">机器人设备对象</param>
        /// <param name="point">点位配置</param>
        /// <param name="value">要写入的值</param>
        /// <returns>是否写入成功</returns>
        private bool WriteToRobot(Robot_Modbus robot, ModbusCommunicationDioDto point, object value)
        {
            try
            {
                // 根据数据类型调用对应的写入方法
                OperateResult result = point.数据类型 switch
                {
                    "bool" => robot.WriteBool(Convert.ToBoolean(value), point.点位索引),
                    "int16" => robot.WriteInt16(Convert.ToInt16(value), point.点位索引),
                    _ => new OperateResult("不支持的数据类型")
                };

                // 处理写入失败
                if (!result.IsSuccess)
                {
                    ShowErrorTip($"写入失败: {result.Message}");
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                        $"[{_modbusDevice.设备名称}] 写入 {point.点位名称} 失败: {result.Message}");
                }

                return result.IsSuccess;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"写入机器人点位 {point.点位名称} 失败");
                return false;
            }
        }
        #endregion

        #region UI辅助方法
        /// <summary>
        /// 显示错误提示
        /// 在UI线程中显示错误消息对话框（避免跨线程操作）
        /// </summary>
        /// <param name="message">错误消息内容</param>
        private void ShowErrorTip(string message)
        {
            WFUtil.UpdateMainThreadUIAsync(this, () =>
            {
                UIMessageBox.ShowError(message);
            });
        }
        #endregion
    }
}
