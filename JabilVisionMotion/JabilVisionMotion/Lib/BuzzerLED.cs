using csLTDMC;
using Jabil.Lib;
using Jabil.Module.IO;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using JabilVisionMotion.Edit;
using JabilVisionMotion.Edit.Process.IOControlProcess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JabilVisionMotion.Lib
{
    /// <summary>
    /// 蜂鸣器和LED控制类
    /// 核心职责：统一管理设备上所有蜂鸣器和LED灯的状态
    /// 仅在ControlMode.PurePC模式下启用所有功能
    /// </summary>
    public class BuzzerLED
    {
        #region 设备实例字段
        private readonly IOProcess _buzzerRed;         // 红色三色灯（设备异常/急停指示）
        private readonly IOProcess _buzzerGreen;       // 绿色三色灯（正常运行/初始化完成指示）
        private readonly IOProcess _buzzerYellow;      // 黄色三色灯（警告/暂停指示）
        private readonly IOProcess _buzzer;            // 蜂鸣器（声音报警）
        private readonly IOProcess _ledReset;          // 复位信号灯
        private readonly IOProcess _ledStart;          // 启动信号灯
        private readonly IOProcess _ledPause;          // 暂停信号灯
        #endregion

        #region 枚举定义
        /// <summary>
        /// LED灯颜色/状态枚举
        /// 定义设备不同运行阶段对应的指示灯组合
        /// </summary>
        public enum LedColor
        {
            Initializing,               // 初始化中
            InitializationComplete,     // 初始化完成
            DeviceRunningNormally,      // 设备正常运行
            SoftwareFirstOpen,          // 软件第一次打开
            SoftwareAlarm,              // 软件报警
            PausedAfterAlarmReset,      // 报警复位后暂停
            StartedAfterReset,          // 复位后启动
            EmergencyStop,              // 机台急停
            ResetAfterEmergencyStop,    // 急停后复位
            Undefined,                  // 未定义状态

            DrawerStartingBlink,        // 抽屉启动闪烁
            FeederDrawerWaitingReset,   // 供料抽屉待复位
            FeederDrawerResetComplete,  // 供料抽屉复位完成
            FeederDrawerRunningNormally // 供料抽屉正常运行
        }

        /// <summary>
        /// LED灯单个状态枚举
        /// 表示单个指示灯的具体状态
        /// </summary>
        public enum LedStatus
        {
            On,         // 常亮
            Off,        // 关闭
            Blinking    // 闪烁
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数：初始化所有IO设备实例
        /// </summary>
        public BuzzerLED()
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            _buzzerRed = new IOProcess("IO|输出|三色灯-红灯|0|5", "隐藏");
            _buzzerGreen = new IOProcess("IO|输出|三色灯-绿灯|0|5", "隐藏");
            _buzzerYellow = new IOProcess("IO|输出|三色灯-黄灯|0|5", "隐藏");
            _buzzer = new IOProcess("IO|输出|三色灯-蜂鸣器|0|5", "隐藏");
            _ledReset = new IOProcess("IO|输出|复位提示灯|0|5", "隐藏");
            _ledStart = new IOProcess("IO|输出|启动提示灯|0|5", "隐藏");
            _ledPause = new IOProcess("IO|输出|暂停提示灯|0|5", "隐藏");

        }
        #endregion

        #region 模式检查辅助方法
        /// <summary>
        /// 检查当前是否为PurePC控制模式
        /// 所有硬件操作前都需要调用此方法验证
        /// </summary>
        /// <returns>true如果是PurePC模式，否则false</returns>
        private bool IsPurePCMode()
        {
            // 检查控制器和工位信息是否存在，避免空引用异常
            if (Controller.Instance?.StationInfo == null)
                return false;

            return Controller.Instance.StationInfo.currentMode == ControlMode.PurePC;
        }
        #endregion

        #region 通用控制方法
        /// <summary>
        /// 通用设备控制方法
        /// 仅在PurePC模式下执行实际操作
        /// </summary>
        private void ControlDevice(IOProcess device, LedStatus status, bool isBuzzer = false)
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            if (device == null) return;

            switch (status)
            {
                case LedStatus.On:
                    if (!isBuzzer || !Controller.Instance.StationInfo._屏蔽蜂鸣器)
                    {
                        UpdateDeviceState(device, true);
                    }
                    break;
                case LedStatus.Off:
                    UpdateDeviceState(device, false);
                    break;
                case LedStatus.Blinking:
                    // 闪烁状态由刷新线程处理
                    break;
            }
        }

        /// <summary>
        /// 更新设备开关状态
        /// 仅在PurePC模式下生效
        /// </summary>
        private void UpdateDeviceState(IOProcess device, bool isOn)
        {
            // 再次检查模式，确保安全
            if (!IsPurePCMode())
                return;

            var newValue = isOn ? "1" : "0";
            var oldValue = isOn ? "0" : "1";
            device.SetProcess(device.Parameter.Replace($"|{oldValue}|", $"|{newValue}|"));
            device.Run();
        }

        /// <summary>
        /// 处理设备闪烁状态
        /// 仅在PurePC模式下执行
        /// </summary>
        private void HandleBlinking(IOProcess device, int intervalMs = 1000)
        {
            if (!IsPurePCMode() || device == null)
                return;

            try
            {
                lock (_lockObj)
                {
                    try
                    {
                        UpdateDeviceState(device, true);
                        Thread.Sleep(intervalMs);
                    }
                    catch (TaskCanceledException)
                    {
                        UpdateDeviceState(device, false);
                    }
                    finally
                    {
                        UpdateDeviceState(device, false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 预期的取消操作，无需处理
            }
        }
        private void HandleBlinking(IOProcess device1, IOProcess device2, int intervalMs = 1000)
        {
            if (!IsPurePCMode() || device1 == null || device2 == null)
                return;

            try
            {
                lock (_lockObj)
                {
                    try
                    {
                        UpdateDeviceState(device1, true);
                        UpdateDeviceState(device2, true);
                        Thread.Sleep(intervalMs);
                    }
                    catch (TaskCanceledException)
                    {
                        UpdateDeviceState(device1, false);
                        UpdateDeviceState(device2, false);
                    }
                    finally
                    {
                        UpdateDeviceState(device1, false);
                        UpdateDeviceState(device2, false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 预期的取消操作，无需处理
            }
        }
        private void HandleBlinking(IOProcess device1, IOProcess device2, IOProcess device3, int intervalMs = 1000)
        {
            if (!IsPurePCMode() || device1 == null || device2 == null || device3 == null)
                return;

            try
            {
                lock (_lockObj)
                {
                    try
                    {
                        UpdateDeviceState(device1, true);
                        UpdateDeviceState(device2, true);
                        UpdateDeviceState(device3, true);
                        Thread.Sleep(intervalMs);
                    }
                    catch (TaskCanceledException)
                    {
                        UpdateDeviceState(device1, false);
                        UpdateDeviceState(device2, false);
                        UpdateDeviceState(device3, false);
                    }
                    finally
                    {
                        UpdateDeviceState(device1, false);
                        UpdateDeviceState(device2, false);
                        UpdateDeviceState(device3, false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 预期的取消操作，无需处理
            }
        }
        #endregion

        #region 设备控制方法（单个设备操作）
        /// <summary>设置复位灯状态 - 仅在PurePC模式下生效</summary>
        public void SetResetLed(LedStatus status) => ControlDevice(_ledReset, status);

        /// <summary>设置启动灯状态 - 仅在PurePC模式下生效</summary>
        public void SetStartLed(LedStatus status) => ControlDevice(_ledStart, status);

        /// <summary>设置暂停灯状态 - 仅在PurePC模式下生效</summary>
        public void SetPauseLed(LedStatus status) => ControlDevice(_ledPause, status);

        /// <summary>设置红色三色灯状态 - 仅在PurePC模式下生效</summary>
        public void SetBuzzerRed(LedStatus status) => ControlDevice(_buzzerRed, status);

        /// <summary>设置绿色三色灯状态 - 仅在PurePC模式下生效</summary>
        public void SetBuzzerGreen(LedStatus status) => ControlDevice(_buzzerGreen, status);

        /// <summary>设置黄色三色灯状态 - 仅在PurePC模式下生效</summary>
        public void SetBuzzerYellow(LedStatus status) => ControlDevice(_buzzerYellow, status);

        /// <summary>设置蜂鸣器状态 - 仅在PurePC模式下生效</summary>
        public void SetBuzzer(LedStatus status) => ControlDevice(_buzzer, status, true);
        #endregion

        #region 整体状态控制（多设备组合操作）
        /// <summary>
        /// 设置蜂鸣器和LED的整体状态
        /// 仅在PurePC模式下执行状态更新
        /// </summary>
        public void SetBuzzerLedStatus(LedColor color)
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            // 先关闭所有设备，避免状态残留
            ResetAllDevices();

            // 根据不同状态设置对应的指示灯组合
            switch (color)
            {
                case LedColor.Initializing: // 初始化中
                    HandleBlinking(_buzzerYellow, intervalMs: 1000);
                    break;
                case LedColor.InitializationComplete: // 初始化完成
                    HandleBlinking(_buzzerGreen, _ledStart, intervalMs: 1000);
                    break;
                case LedColor.DeviceRunningNormally: // 设备正常运行
                    if (MenuProxy.Instance.OperateType == OperateMode.Online
                        && MenuProxy.Instance.MachineRunModel == MachineMode.Manual
                        && MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Running)
                    {
                        SetBuzzerYellow(LedStatus.On);
                    }
                    else
                    {
                        SetStartLed(LedStatus.On);
                        SetBuzzerGreen(LedStatus.On);
                    }
                    break;
                case LedColor.SoftwareFirstOpen: // 软件第一次打开
                    SetBuzzerYellow(LedStatus.On);
                    break;
                case LedColor.SoftwareAlarm: // 软件报警
                    SetPauseLed(LedStatus.On);
                    if (!Controller.Instance.StationInfo._屏蔽蜂鸣器)
                    {
                        HandleBlinking(_buzzer, intervalMs: 1000);
                    }
                    HandleBlinking(_buzzerRed, _ledReset, intervalMs: 1000);
                    break;
                case LedColor.PausedAfterAlarmReset:// 报警复位后暂停
                    switch (MenuProxy.Instance.MachineRunModel)
                    {
                        case MachineMode.Manual:
                            HandleBlinking(_buzzerYellow, intervalMs: 1000);
                            break;
                        case MachineMode.Auto:
                            HandleBlinking(_ledStart, _buzzerGreen, intervalMs: 1000);
                            break;
                    }
                    break;
                case LedColor.StartedAfterReset: // 复位后启动
                    switch (MenuProxy.Instance.MachineRunModel)
                    {
                        case MachineMode.Manual:
                            SetBuzzerYellow(LedStatus.On);
                            break;
                        case MachineMode.Auto:
                            SetBuzzerGreen(LedStatus.On);
                            SetStartLed(LedStatus.On);
                            break;
                    }
                    break;
                case LedColor.EmergencyStop: // 机台急停
                    if (!Controller.Instance.StationInfo._屏蔽蜂鸣器)
                    {
                        HandleBlinking(_buzzer, intervalMs: 500);
                    }
                    HandleBlinking(_buzzerRed, intervalMs: 500);
                    break;
                case LedColor.ResetAfterEmergencyStop: // 急停后复位
                    SetBuzzerYellow(LedStatus.On);
                    break;
                case LedColor.Undefined: // 未定义状态
                    ResetAllDevices();
                    break;
            }
        }

        /// <summary>
        /// 重置所有设备为关闭状态
        /// 仅在PurePC模式下执行
        /// </summary>
        private void ResetAllDevices()
        {
            if (!IsPurePCMode())
                return;

            SetResetLed(LedStatus.Off);
            SetStartLed(LedStatus.Off);
            SetPauseLed(LedStatus.Off);
            SetBuzzer(LedStatus.Off);
            SetBuzzerRed(LedStatus.Off);
            SetBuzzerGreen(LedStatus.Off);
            SetBuzzerYellow(LedStatus.Off);
        }
        #endregion

        #region 抽屉灯状态控制（供料抽屉专用）
        /// <summary>上料安全门按钮灯状态（0:待复位, 1:复位完成, 2:正常运行）</summary>
        public int 上料安全门按钮灯状态 { get; set; } = 0;

        /// <summary>下料安全门按钮灯状态（0:待复位, 1:复位完成, 2:正常运行）</summary>
        public int 下料安全门按钮灯状态 { get; set; } = 0;

        /// <summary>
        /// 设置左供料抽屉灯状态 - 仅在PurePC模式下更新状态
        /// </summary>
        public void SetLeftFeederDrawerLed(LedColor color)
        {
            if (!IsPurePCMode())
                return;

            上料安全门按钮灯状态 = color switch
            {
                LedColor.FeederDrawerWaitingReset => 0,
                LedColor.FeederDrawerResetComplete => 1,
                LedColor.FeederDrawerRunningNormally => 2,
                _ => 上料安全门按钮灯状态
            };
        }

        /// <summary>
        /// 设置右供料抽屉灯状态 - 仅在PurePC模式下更新状态
        /// </summary>
        public void SetRightFeederDrawerLed(LedColor color)
        {
            if (!IsPurePCMode())
                return;

            下料安全门按钮灯状态 = color switch
            {
                LedColor.FeederDrawerWaitingReset => 0,
                LedColor.FeederDrawerResetComplete => 1,
                LedColor.FeederDrawerRunningNormally => 2,
                _ => 下料安全门按钮灯状态
            };
        }
        #endregion

        #region 实时刷新逻辑（处理闪烁状态）
        /// <summary>
        /// 异步运行蜂鸣器和LED刷新逻辑
        /// 仅在PurePC模式下执行刷新
        /// </summary>
        public void RunBuzzerLed(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Task.Delay(500).Wait();

                    // 非PurePC模式或程序退出时，跳过刷新
                    if (!IsPurePCMode() ||
                        Project.Instance.GlobalVariables.IsTestMode ||
                        Project.Instance.GlobalVariables.IsProgramExiting)
                    {
                        break;
                    }

                    // 根据设备当前运行状态设置对应的指示灯组合
                    SetBuzzerLedStatus(MenuProxy.Instance.MachineRunStatus switch
                    {
                        MachineRunStatus.None => LedColor.SoftwareFirstOpen,
                        MachineRunStatus.Paused => LedColor.PausedAfterAlarmReset,
                        MachineRunStatus.Homing => LedColor.Initializing,
                        MachineRunStatus.HomeSuccess => LedColor.InitializationComplete,
                        MachineRunStatus.Running => LedColor.DeviceRunningNormally,
                        MachineRunStatus.Alarming => LedColor.SoftwareAlarm,
                        MachineRunStatus.Reseting => LedColor.ResetAfterEmergencyStop,
                        MachineRunStatus.EmergencyStopped => LedColor.EmergencyStop,
                        _ => LedColor.Undefined
                    });
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 异步运行安全门刷新逻辑
        /// 仅在PurePC模式下执行刷新
        /// </summary>
        public void RunSafeLed(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Task.Delay(1000).Wait();

                    // 非PurePC模式或程序退出时，跳过刷新
                    if (!IsPurePCMode() ||
                        Project.Instance.GlobalVariables.IsTestMode ||
                        Project.Instance.GlobalVariables.IsProgramExiting)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 通用抽屉灯状态更新方法
        /// 仅在PurePC模式下执行
        /// </summary>
        private void UpdateDrawerLeds(int status, IOProcess resetLed, IOProcess startLed)
        {
            if (!IsPurePCMode())
                return;

            switch (status)
            {
                case 0: // 待复位
                    HandleBlinking(resetLed, intervalMs: 1000);
                    SetStartLed(LedStatus.Off);
                    break;
                case 1: // 复位完成
                    SetResetLed(LedStatus.Off);
                    HandleBlinking(startLed, intervalMs: 1000);
                    break;
                case 2: // 正常运行
                    SetResetLed(LedStatus.Off);
                    SetStartLed(LedStatus.On);
                    break;
            }
        }
        #endregion

        #region 蜂鸣器控制（声音报警专用）
        #region 私有字段
        private readonly object _lockObj = new object();
        private CancellationTokenSource _currentCts;
        #endregion

        #region 公共方法
        /// <summary>
        /// 蜂鸣器报警 - 仅在PurePC模式下生效
        /// </summary>
        public void Beep(int count, int beepDuration = 300, int interval = 200)
        {
            // 非PurePC模式下不执行
            if (!IsPurePCMode())
                return;

            if (count <= 0)
                throw new ArgumentException("报警次数必须大于0", nameof(count));
            if (beepDuration <= 0)
                throw new ArgumentException("鸣响时间必须大于0", nameof(beepDuration));

            StopCurrentBeep();

            lock (_lockObj)
            {
                _currentCts = new CancellationTokenSource();
                var token = _currentCts.Token;

                Task.Run(async () =>
                {
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (token.IsCancellationRequested)
                                break;

                            SetBuzzer(LedStatus.On);
                            await Task.Delay(beepDuration, token);

                            if (token.IsCancellationRequested)
                                break;

                            SetBuzzer(LedStatus.Off);

                            if (i < count - 1)
                            {
                                await Task.Delay(interval, token);
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // 预期的取消异常
                    }
                    finally
                    {
                        SetBuzzer(LedStatus.Off);
                    }
                }, token);
            }
        }

        /// <summary>
        /// 停止当前正在进行的蜂鸣 - 仅在PurePC模式下生效
        /// </summary>
        public void StopBeep()
        {
            if (!IsPurePCMode())
                return;

            StopCurrentBeep();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 停止当前正在进行的报警（内部实现）
        /// </summary>
        private void StopCurrentBeep()
        {
            lock (_lockObj)
            {
                if (_currentCts != null && !_currentCts.IsCancellationRequested)
                {
                    _currentCts.Cancel();
                    _currentCts.Dispose();
                    _currentCts = null;
                }
            }
        }
        #endregion
        #endregion
    }
}
