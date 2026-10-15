using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sunny.UI;
using Jabil.Module.Log.Lib;
using Jabil.Lib;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Permission;

namespace Jabil.Module.Motion.Control
{
    /// <summary>
    /// 运动轴状态显示控件
    /// 用于实时展示指定运动轴的各项运行参数（位置、速度等）和状态信息（报警、使能等）
    /// 继承自Sunny.UI的UIUserControl，提供美观的界面组件和交互效果
    /// </summary>
    public partial class MotionStatu : UIUserControl
    {
        #region 字段与属性
        /// <summary>
        /// 运动轴参数对象，存储当前监控轴的基本配置信息
        /// 只读字段，确保初始化后不会被修改
        /// </summary>
        private readonly MotionAxisDto _motionAxis;

        /// <summary>
        /// 实例级别的同步锁对象
        /// 用于线程安全控制，避免多线程同时访问共享资源
        /// 采用实例级锁而非静态锁，防止多控件实例间的锁竞争
        /// </summary>
        private readonly object _syncLock = new object();

        /// <summary>
        /// 取消令牌源，用于安全终止异步监控任务
        /// 当控件释放或需要停止监控时，通过此对象发送取消信号
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// 运动状态数组，用于存储轴的各种状态标志
        /// 预先分配32位空间以满足扩展需求，复用该数组减少内存分配
        /// 数组索引对应不同状态（见UI更新部分的注释）
        /// </summary>
        private bool[] _motionStatusArray = new bool[32];
        #endregion

        #region 构造函数
        /// <summary>
        /// 无参构造函数
        /// 主要为设计器提供支持，运行时通常使用带参数的构造函数
        /// 设置双缓冲和禁止擦除背景以减少绘制闪烁
        /// </summary>
        public MotionStatu()
        {
            InitializeComponent();
            // 启用双缓冲和所有绘制在WM_PAINT消息中完成，提升UI绘制性能
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        /// <summary>
        /// 带参数构造函数
        /// 初始化指定运动轴的状态监控控件
        /// </summary>
        /// <param name="axis">运动轴参数对象，不能为空</param>
        /// <exception cref="ArgumentNullException">当axis为null时抛出</exception>
        public MotionStatu(MotionAxisDto axis) : this()
        {
            // 验证参数有效性，确保运动轴信息不为空
            _motionAxis = axis ?? throw new ArgumentNullException(nameof(axis), "运动轴参数不能为空");
            // 初始化UI并启动监控
            InitializeUI();
        }
        #endregion


        #region 初始化
        /// <summary>
        /// 初始化UI显示
        /// 设置轴的基本信息并启动状态监控任务
        /// </summary>
        private void InitializeUI()
        {
            // 防御性检查，避免空引用异常
            if (_motionAxis == null) return;

            // 初始设置轴基本信息到UI控件
            uiLabel_轴名称.Text = _motionAxis.轴名称;
            uiLabel_轴类型.Text = _motionAxis.轴类型;

            // 启动状态监控任务，开始实时更新数据
            StartStatusMonitoring();
        }

        /// <summary>
        /// 启动状态监控任务
        /// 初始化取消令牌源并启动异步监控循环
        /// </summary>
        private void StartStatusMonitoring()
        {
            // 创建新的取消令牌源，用于后续控制任务取消
            _cts = new CancellationTokenSource();
            // 启动异步监控任务，不等待其完成（火并忘记模式）
            // 使用下划线忽略未使用的Task返回值，避免编译器警告
            _ = RunMotionStatuAsync(_cts.Token);
        }

        #endregion

        #region 核心监控逻辑
        /// <summary>
        /// 异步运行运动轴状态监控
        /// 周期性读取轴状态并更新UI，支持任务取消
        /// </summary>
        /// <param name="cancellationToken">取消令牌，用于接收取消请求</param>
        private async Task RunMotionStatuAsync(CancellationToken cancellationToken)
        {
            try
            {
                // 循环监控，直到收到取消请求
                while (!cancellationToken.IsCancellationRequested)
                {
                    // 等待300ms后再进行下一次检查
                    // 使用带取消令牌的Delay，可在等待期间响应取消请求
                    await Task.Delay(300, cancellationToken).ConfigureAwait(false);

                    // 检查是否需要停止监控（如程序退出、控件已释放等）
                    if (ShouldStopMonitoring())
                        break;

                    // 刷新运动轴状态数据并更新UI
                    // ConfigureAwait(false)用于避免不必要的上下文切换，提升性能
                    await RefreshMotionStatusAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被正常取消，不记录错误，直接退出
            }
            catch (Exception ex)
            {
                // 记录监控过程中发生的异常
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 检查是否应该停止监控
        /// 综合多种退出条件，确保监控任务能正确终止
        /// </summary>
        /// <returns>true表示应停止监控，false表示应继续</returns>
        private bool ShouldStopMonitoring()
        {
            // 多种退出条件的逻辑或：
            // 1. 控件已被释放
            // 2. 程序正在退出
            // 3. 处于测试模式
            return IsDisposed
                   || Project.Instance?.GlobalVariables?.IsProgramExiting == true
                   || Project.Instance?.GlobalVariables?.IsTestMode == true;
        }

        /// <summary>
        /// 刷新运动轴状态
        /// 读取轴的实时状态和参数，处理紧急情况，更新UI
        /// </summary>
        private async Task RefreshMotionStatusAsync()
        {
            // 防御性检查：确保轴信息和运动控制器实例有效
            if (_motionAxis == null || MotionSystem.Instance?.MotionController == null)
                return;

            try
            {
                // 紧急停止状态处理：当机器处于紧急停止状态时，执行平滑停止
                if (MenuProxy.Instance?.MachineRunStatus == MachineRunStatus.EmergencyStopped)
                {
                    // 在后台线程执行停止命令，避免阻塞UI线程
                    await Task.Run(() =>
                        MotionSystem.Instance.MotionController.SmtStop(_motionAxis, 0),
                        CancellationToken.None).ConfigureAwait(false);
                }

                // 读取轴的实时状态数组（报警、使能等状态）
                _motionStatusArray = MotionSystem.Instance.MotionController.GetMotionSts(_motionAxis);

                // 读取轴的各项参数值，封装到数据对象中
                var axisData = new AxisStatusData
                {
                    CmdPosition = MotionSystem.Instance.MotionController.GetCmd(_motionAxis),          // 规划位置
                    CmdVelocity = MotionSystem.Instance.MotionController.GetCmdVel(_motionAxis),      // 规划速度
                    EncoderPosition = MotionSystem.Instance.MotionController.GetEncPos(_motionAxis),  // 编码器位置
                    EncoderVelocity = MotionSystem.Instance.MotionController.GetEncVel(_motionAxis),  // 编码器速度
                    ActualPosition = MotionSystem.Instance.MotionController.ReadActualPosition(_motionAxis), // 实际位置
                    TargetPosition = MotionSystem.Instance.MotionController.GetTargetPos(_motionAxis) // 目标位置
                };

                // 更新UI显示，确保在UI线程执行
                UpdateUI(axisData);
            }
            catch (Exception ex)
            {
                // 记录当前轴的状态刷新错误，但不终止监控循环
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region UI更新
        /// <summary>
        /// 更新UI显示
        /// 将最新的轴状态数据更新到界面控件，确保线程安全
        /// </summary>
        /// <param name="data">包含轴状态数据的对象</param>
        private void UpdateUI(AxisStatusData data)
        {
            // 检查控件状态：已释放或句柄未创建则不更新
            if (IsDisposed || !IsHandleCreated)
                return;

            // 线程安全检查：如果当前不是UI线程，通过BeginInvoke切换到UI线程
            if (InvokeRequired)
            {
                // 使用BeginInvoke异步调用，避免阻塞当前线程
                BeginInvoke(new Action<AxisStatusData>(UpdateUI), data);
                return;
            }

            try
            {
                // 更新数值显示控件
                uiDigitalLabel_编码器位置.Value = data.CmdPosition;
                uiDigitalLabel_轴规划位置.Value = data.EncoderPosition;

                // 更新状态指示灯（根据状态数组索引对应不同状态）
                uiLight1_报警.State = _motionStatusArray[0] ? UILightState.On : UILightState.Off;       // [0] 报警状态
                uiLight1_使能.State = _motionStatusArray[1] ? UILightState.On : UILightState.Off;       // [1] 使能状态
                uiLight_正极限.State = _motionStatusArray[2] ? UILightState.On : UILightState.Off;     // [2] 正极限触发
                uiLight_原点.State = _motionStatusArray[3] ? UILightState.On : UILightState.Off;       // [3] 原点信号
                uiLight_负极限.State = _motionStatusArray[4] ? UILightState.On : UILightState.Off;     // [4] 负极限触发
                uiLight_运动.State = _motionStatusArray[5] ? UILightState.On : UILightState.Off;       // [5] 运动中状态
                uiLight_到位.State = _motionStatusArray[6] ? UILightState.On : UILightState.Off;       // [6] 到位状态
                uiLight1_掉线.State = _motionStatusArray[7] ? UILightState.On : UILightState.Off;      // [7] 掉线状态
                uiLight_缓停触发.State = _motionStatusArray[8] ? UILightState.On : UILightState.Off;  // [8] 缓停触发状态
            }
            catch (Exception ex)
            {
                // 记录UI更新过程中的异常
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 辅助类
        /// <summary>
        /// 轴状态数据封装类
        /// 用于集中存储轴的各项参数值，便于传递和管理
        /// </summary>
        private class AxisStatusData
        {
            /// <summary>规划位置（指令位置）</summary>
            public double CmdPosition { get; set; }

            /// <summary>规划速度（指令速度）</summary>
            public double CmdVelocity { get; set; }

            /// <summary>编码器反馈位置</summary>
            public double EncoderPosition { get; set; }

            /// <summary>编码器反馈速度</summary>
            public double EncoderVelocity { get; set; }

            /// <summary>驱动器内部实际位置</summary>
            public double ActualPosition { get; set; }

            /// <summary>运动目标位置</summary>
            public double TargetPosition { get; set; }
        }
        #endregion
    }
}