using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sunny.UI;
using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;

namespace Jabil.Module.Modbus.Control
{
    /// <summary>
    /// 数字输入输出(DIO)控件
    /// 用于可视化展示和交互控制Modbus设备的数字量信号状态
    /// 支持显示PointName、索引、值和状态，并提供用户点击交互功能
    /// </summary>
    public partial class DIO : UIUserControl, IDisposable
    {
        #region 事件定义
        /// <summary>
        /// 点击事件委托
        /// 定义控件被点击时传递的参数类型
        /// </summary>
        public delegate void ClickEventHandler(object sender, EventArgs e);

        /// <summary>
        /// 双击事件委托
        /// 用于显示点位详细信息
        /// </summary>
        public delegate void DoubleClickEventHandler(object sender, EventArgs e);

        /// <summary>
        /// 状态变化事件委托
        /// 当点位状态发生变化时触发
        /// </summary>
        public delegate void StateChangedEventHandler(object sender, StateChangedEventArgs e);

        /// <summary>
        /// 点击事件
        /// 当用户点击控件的任何可交互区域时触发
        /// </summary>
        public event ClickEventHandler ControlClick;

        /// <summary>
        /// 双击事件
        /// 当用户双击控件时触发，用于显示详细信息
        /// </summary>
        public event DoubleClickEventHandler ControlDoubleClick;

        /// <summary>
        /// 状态变化事件
        /// 当点位状态发生变化时触发
        /// </summary>
        public event StateChangedEventHandler StateChanged;
        #endregion

        #region 私有字段
        private string _pointName = "DI-";
        private string _pointIndex = "999";
        private int _pointState = 999;
        private object _pointValue = 999;
        private string _pointDescription = string.Empty;
        private string _permissionCode = string.Empty;
        private DateTime _lastUpdateTime;
        private readonly ToolTip _toolTip;
        private readonly Timer _statusTimer;
        private bool _isDisposed = false;
        private readonly object _lockObject = new object();
        #endregion

        #region 公共属性
        /// <summary>
        /// PointName，用于标识该数字量信号的名称
        /// </summary>
        [Description("PointName，用于标识数字量信号"), Category("控件定义")]
        public string PointName
        {
            get => _pointName;
            set
            {
                if (_pointName != value)
                {
                    _pointName = value;
                    UpdateControlTextAsync(label_PointName, _pointName);
                    UpdateToolTip();
                }
            }
        }

        /// <summary>
        /// 点位在设备中的地址索引
        /// </summary>
        [Description("点位在设备中的地址索引"), Category("控件定义")]
        public string PointIndex
        {
            get => _pointIndex;
            set
            {
                if (_pointIndex != value)
                {
                    _pointIndex = value;
                    UpdateControlTextAsync(Label_PointIndex, _pointIndex);
                    UpdateToolTip();
                }
            }
        }

        /// <summary>
        /// 点位状态 (0:关闭, 1:开启, 其他:异常闪烁)
        /// </summary>
        [Description("点位状态 (0:关闭, 1:开启, 其他:异常闪烁)"), Category("控件定义")]
        public int 点位状态
        {
            get => _pointState;
            set
            {
                lock (_lockObject)
                {
                    if (_pointState != value)
                    {
                        int oldState = _pointState;
                        _pointState = value;
                        _lastUpdateTime = DateTime.Now;

                        // 触发状态变化事件
                        OnStateChanged(oldState, value);

                        UpdateControlStateAsync();
                    }
                }
            }
        }

        /// <summary>
        /// 点位的实际数值
        /// </summary>
        [Description("点位的实际数值"), Category("控件定义")]
        public object PointValue
        {
            get => _pointValue;
            set
            {
                if (!Equals(_pointValue, value))
                {
                    _pointValue = value;
                    UpdateControlTextAsync(Label_PointValue, _pointValue?.ToString() ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// 点位的功能描述信息
        /// </summary>
        [Description("点位的功能描述信息"), Category("控件定义")]
        public string PointDescription
        {
            get => _pointDescription;
            set
            {
                _pointDescription = value;
                UpdateToolTip();
            }
        }

        /// <summary>
        /// 操作该点位所需的权限编码
        /// </summary>
        [Description("操作该点位所需的权限编码"), Category("权限控制")]
        public string PermissionCode
        {
            get => _permissionCode;
            set => _permissionCode = value;
        }

        /// <summary>
        /// 上次状态更新时间
        /// </summary>
        [Browsable(false)]
        public DateTime LastUpdateTime => _lastUpdateTime;

        /// <summary>
        /// 状态有效时长(毫秒)，超过此时长未更新则视为通信异常
        /// </summary>
        [Description("状态有效时长(毫秒)，超过此时长未更新则视为通信异常"), Category("状态监控")]
        public int StatusValidDuration { get; set; } = 5000;

        /// <summary>
        /// 检查状态是否有效
        /// </summary>
        [Browsable(false)]
        public bool IsStatusValid => (DateTime.Now - _lastUpdateTime).TotalMilliseconds < StatusValidDuration;

        /// <summary>
        /// 状态指示灯颜色设置 - 开启状态
        /// </summary>
        [Description("状态指示灯开启时的颜色"), Category("外观设置")]
        public Color OnColor { get; set; } = Color.FromArgb(60, 220, 60);

        /// <summary>
        /// 状态指示灯颜色设置 - 关闭状态
        /// </summary>
        [Description("状态指示灯关闭时的颜色"), Category("外观设置")]
        public Color OffColor { get; set; } = Color.FromArgb(220, 60, 60);

        /// <summary>
        /// 状态指示灯颜色设置 - 异常状态
        /// </summary>
        [Description("状态指示灯异常时的颜色"), Category("外观设置")]
        public Color ErrorColor { get; set; } = Color.Orange;
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 初始化DIO控件实例
        /// </summary>
        public DIO()
        {
            InitializeComponent();

            // 启用双缓冲减少闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            UpdateStyles();

            // 初始化工具提示
            _toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 100,
                ReshowDelay = 100,
                ShowAlways = true
            };

            // 初始化状态检查定时器
            _statusTimer = new Timer
            {
                Interval = 1000,
                Enabled = true
            };
            _statusTimer.Tick += StatusTimer_Tick;

            // 为整个控件添加事件
            DoubleClick += DIO_DoubleClick;
            MouseEnter += DIO_MouseEnter;
            MouseLeave += DIO_MouseLeave;

            // 初始化时间戳
            _lastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 控件加载事件处理方法
        /// </summary>
        private void DIO_Load(object sender, EventArgs e)
        {
            if (DesignMode) return;

            // 初始状态设为闪烁，表示控件已加载但尚未接收到有效数据
            uiLight_状态.State = UILightState.Blink;
            label_PointName.Active = false;

            // 设置圆角效果
            Radius = 8;

            // 初始更新工具提示
            UpdateToolTip();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 异步更新UIScrollingText控件文本（线程安全）
        /// </summary>
        private async void UpdateControlTextAsync(UIScrollingText control, string text)
        {
            if (control.IsDisposed) return;

            try
            {
                if (control.InvokeRequired)
                {
                    await Task.Run(() =>
                        control.Invoke(new Action<UIScrollingText, string>(UpdateControlText), control, text));
                }
                else
                {
                    UpdateControlText(control, text);
                }
            }
            catch (ObjectDisposedException)
            {
                // 控件已释放，忽略
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"更新控件文本失败: {control.Name}");
            }
        }

        /// <summary>
        /// 异步更新UILabel控件文本（线程安全）
        /// </summary>
        private async void UpdateControlTextAsync(UILabel control, string text)
        {
            if (control.IsDisposed) return;

            try
            {
                if (control.InvokeRequired)
                {
                    await Task.Run(() =>
                        control.Invoke(new Action<UILabel, string>(UpdateControlText), control, text));
                }
                else
                {
                    UpdateControlText(control, text);
                }
            }
            catch (ObjectDisposedException)
            {
                // 控件已释放，忽略
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"更新控件文本失败: {control.Name}");
            }
        }

        /// <summary>
        /// 实际更新控件文本的方法
        /// </summary>
        private void UpdateControlText(System.Windows.Forms.Control control, string text)
        {
            if (!control.IsDisposed && control.Text != text)
            {
                control.Text = text;
            }
        }

        /// <summary>
        /// 异步更新控件状态指示灯（线程安全）
        /// </summary>
        private async void UpdateControlStateAsync()
        {
            if (uiLight_状态.IsDisposed) return;

            try
            {
                if (uiLight_状态.InvokeRequired)
                {
                    await Task.Run(() =>
                        uiLight_状态.Invoke(new Action(UpdateControlState)));
                }
                else
                {
                    UpdateControlState();
                }
            }
            catch (ObjectDisposedException)
            {
                // 控件已释放，忽略
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, "更新控件状态失败");
            }
        }

        /// <summary>
        /// 实际更新控件状态的方法
        /// </summary>
        private void UpdateControlState()
        {
            if (uiLight_状态.IsDisposed) return;

            lock (_lockObject)
            {
                // 检查状态是否有效
                //if (!IsStatusValid)
                //{
                //    return;
                //}

                // 恢复默认颜色
                uiLight_状态.OnColor = OnColor;
                uiLight_状态.OffColor = OffColor;

                // 根据点位状态设置指示灯状态
                uiLight_状态.State = _pointState switch
                {
                    0 => UILightState.Off,
                    1 => UILightState.On,
                    _ => UILightState.Blink
                };
            }
        }

        /// <summary>
        /// 更新工具提示信息
        /// </summary>
        private void UpdateToolTip()
        {
            if (_toolTip == null) return;

            string tipText = $"名称: {PointName}\r\n索引: {PointIndex}\r\n描述: {PointDescription}";

            try
            {
                _toolTip.SetToolTip(this, tipText);
                _toolTip.SetToolTip(label_PointName, tipText);
                _toolTip.SetToolTip(uiLight_状态, tipText);
                _toolTip.SetToolTip(Label_PointIndex, tipText);
                _toolTip.SetToolTip(Label_PointValue, tipText);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, "更新工具提示失败");
            }
        }

        /// <summary>
        /// 检查操作权限
        /// </summary>
        private bool CheckPermission()
        {
            // 如果没有设置权限编码，则默认允许操作
            if (string.IsNullOrEmpty(PermissionCode))
                return true;

            try
            {
                // 调用权限检查模块
                return PermissionSystem.CheckPermission(PermissionCode, out _);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"检查权限失败: {PermissionCode}");
                return false; // 权限检查失败时默认拒绝操作
            }
        }

        /// <summary>
        /// 触发状态变化事件
        /// </summary>
        private void OnStateChanged(int oldState, int newState)
        {
            StateChanged?.Invoke(this, new StateChangedEventArgs(oldState, newState));
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 状态检查定时器事件
        /// </summary>
        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            // 只有当状态无效时才更新显示，减少不必要的UI操作
            if (!IsStatusValid)
            {
                UpdateControlStateAsync();
            }
        }

        /// <summary>
        /// 控件双击事件处理
        /// </summary>
        private void DIO_DoubleClick(object sender, EventArgs e)
        {
            ControlDoubleClick?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 鼠标进入控件事件
        /// </summary>
        private void DIO_MouseEnter(object sender, EventArgs e)
        {
            if (!IsDisposed)
            {
                FillColor = Color.FromArgb(10, 65, 110);
                FillColor2 = Color.FromArgb(0, 45, 80);
            }
        }

        /// <summary>
        /// 鼠标离开控件事件
        /// </summary>
        private void DIO_MouseLeave(object sender, EventArgs e)
        {
            if (!IsDisposed)
            {
                FillColor = Color.FromArgb(0, 55, 100);
                FillColor2 = Color.FromArgb(0, 45, 80);
            }
        }

        /// <summary>
        /// 触发控件点击事件
        /// </summary>
        private void OnControlClick()
        {
            if (!CheckPermission())
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                    $"用户没有操作 {PointName} 的权限");
                return;
            }

            ControlClick?.Invoke(this, EventArgs.Empty);
        }

        // 各种子控件的点击事件处理
        private void label_PointName_Click(object sender, EventArgs e) => OnControlClick();
        private void uiLight_状态_Click(object sender, EventArgs e) => OnControlClick();
        private void Label_PointIndex_Click(object sender, EventArgs e) => OnControlClick();
        private void Label_PointValue_Click(object sender, EventArgs e) => OnControlClick();
        #endregion

        #region 资源释放
        /// <summary>
        /// 释放资源
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~DIO()
        {
            Dispose(false);
        }
        #endregion
    }

    /// <summary>
    /// 状态变化事件参数
    /// </summary>
    public class StateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 旧状态
        /// </summary>
        public int OldState { get; }

        /// <summary>
        /// 新状态
        /// </summary>
        public int NewState { get; }

        /// <summary>
        /// 初始化状态变化事件参数
        /// </summary>
        public StateChangedEventArgs(int oldState, int newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }
}
