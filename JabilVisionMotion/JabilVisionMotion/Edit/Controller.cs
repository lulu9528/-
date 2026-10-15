using Cognex.VisionPro;
using Cognex.VisionPro.FGGigE;
using Cognex.VisionPro.FGGigE.Implementation.Internal;
using Cognex.VisionPro.ToolBlock;
using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Lib.Parameter;
using Jabil.Module.Alarm.Common.Dtos;
using Jabil.Module.Alarm.Service;
using Jabil.Module.IO.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus;
using Jabil.Module.Motion;
using Jabil.Module.Motion.Frm;
using Jabil.Module.Permission;
using Jabil.Module.Serialport;
using Jabil.Module.Serialport.Lib;
using Jabil.Module.TCPIP;
using Jabil.Module.Vision;
using Jabil.Module.Vision.Lib;
using JabilVisionMotion.Frm;
using JabilVisionMotion.Frm.Dialog;
using JabilVisionMotion.Frm.UserPage;
using JabilVisionMotion.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Concurrent;
using Cognex.VisionPro.ImageFile;
using JabilVisionMotion.Edit.Process.IOControlProcess;
using System.Windows.Documents;
using Newtonsoft.Json;
using JabilVisionMotion.Edit.Process.ModbusControlProcess;
using Jabil.Module.IO;
using Jabil.Module.Motion.Lib;

namespace JabilVisionMotion.Edit
{
    /// <summary>
    /// 设备控制器核心类，实现IStationController接口<br/>
    /// 核心职责：
    /// 1. 设备运行状态全生命周期管理（初始化/运行/暂停/急停等状态转换）
    /// 2. 核心流程控制（初始化、启动、停止、复位等设备操作）
    /// 3. 外部通信管理（TCP服务器/客户端、串口通信）
    /// 4. 设备安全监控（急停、安全门、机器人碰撞风险等）
    /// 采用单例模式确保全局状态一致性
    /// </summary>
    public class Controller : IStationController
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 线程安全的单例实例<br/>
        /// 使用Lazy<T>实现延迟初始化，确保多线程环境下仅创建一个实例<br/>
        /// 延迟初始化：在首次访问Instance时才创建对象，减少启动时资源消耗
        /// </summary>
        private static readonly Lazy<Controller> _instance = new Lazy<Controller>(() => new Controller());

        /// <summary>
        /// 全局访问点，提供控制器单例的唯一入口
        /// </summary>
        public static Controller Instance => _instance.Value;
        #endregion

        #region 字段与属性
        /// <summary>
        /// 工位信息对象，存储设备运行过程中的动态状态数据<br/>
        /// 包含：传感器状态、机器人模式、缓存的运行状态等核心数据
        /// </summary>
        private StationInfo _stationInfo;

        /// <summary>
        /// 工位信息访问属性，封装工位状态数据的读写操作<br/>
        /// 外部通过此属性访问或修改工位信息，确保数据访问的一致性
        /// </summary>
        public StationInfo StationInfo
        {
            get => _stationInfo;
            set => _stationInfo = value;
        }

        /// <summary>
        /// 运行状态与菜单配置的映射字典<br/>
        /// 设计目的：将设备状态与UI菜单按钮状态解耦，便于维护不同状态下的按钮权限<br/>
        /// 键：设备运行状态（MachineRunStatus枚举）<br/>
        /// 值：对应状态下的菜单按钮配置（启用/禁用状态）
        /// </summary>
        private readonly Dictionary<MachineRunStatus, MenuConfig> _statusMenuMappings = new Dictionary<MachineRunStatus, MenuConfig>();
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 私有构造函数，防止外部实例化（单例模式核心）<br/>
        /// 初始化逻辑：
        /// 1. 创建工位信息对象（StationInfo）
        /// 2. 加载状态-菜单配置映射（InitializeStatusMappings）
        /// </summary>
        private Controller()
        {
            StationInfo = new StationInfo();
            InitializeStatusMappings();
        }

        /// <summary>
        /// 初始化运行状态与菜单配置的映射关系<br/>
        /// 为每种设备运行状态配置对应的菜单按钮启用/禁用规则<br/>
        /// 业务意义：确保设备在特定状态下只能执行合法操作（如运行中不能再次启动）
        /// </summary>
        private void InitializeStatusMappings()
        {
            // 无状态：设备未初始化或已停止，仅允许停止和初始化操作
            _statusMenuMappings[MachineRunStatus.None] = new MenuConfig
            {
                PauseStatus = 0,          // 暂停状态标识（0：未暂停）
                EnableStart = false,      // 启动按钮禁用
                EnablePause = false,      // 暂停按钮禁用
                EnableStop = true,        // 停止按钮启用
                EnableInit = true,        // 初始化按钮启用
                EnableReset = false,      // 复位按钮禁用
                EnableMachineRunType = false // 运行模式切换禁用
            };

            // 暂停状态：设备临时停止，允许继续或停止操作
            _statusMenuMappings[MachineRunStatus.Paused] = new MenuConfig
            {
                PauseStatus = 1,          // 暂停状态标识（1：已暂停）
                EnableStart = false,
                EnablePause = true,
                EnableStop = true,
                EnableInit = false,
                EnableReset = false
            };

            // 回零中：设备正在执行初始化回零，仅允许暂停或停止
            _statusMenuMappings[MachineRunStatus.Homing] = new MenuConfig
            {
                PauseStatus = 0,
                EnableStart = false,
                EnablePause = true,
                EnableStop = true,
                EnableInit = false,
                EnableReset = false
            };

            // 回零完成：初始化成功，允许启动或停止，可切换运行模式
            _statusMenuMappings[MachineRunStatus.HomeSuccess] = new MenuConfig
            {
                PauseStatus = 0,
                EnableStart = true,
                EnablePause = false,
                EnableStop = true,
                EnableInit = false,
                EnableReset = false,
                EnableMachineRunType = true
            };

            // 复位中：设备正在从异常状态恢复，禁止所有操作
            _statusMenuMappings[MachineRunStatus.Reseting] = new MenuConfig
            {
                PauseStatus = 0,
                EnableStart = false,
                EnablePause = false,
                EnableStop = false,
                EnableInit = true,
                EnableReset = false,
                EnableMachineRunType = false
            };

            // 运行中：设备正常工作，仅允许停止操作
            _statusMenuMappings[MachineRunStatus.Running] = new MenuConfig
            {
                PauseStatus = 0,
                EnableStart = false,
                EnablePause = false,
                EnableStop = true,
                EnableInit = false,
                EnableReset = false,
                EnableMachineRunType = false
            };

            // 报警中：设备出现异常，仅允许停止操作
            _statusMenuMappings[MachineRunStatus.Alarming] = new MenuConfig
            {
                PauseStatus = 0,
                EnableStart = false,
                EnablePause = false,
                EnableStop = true,
                EnableInit = false,
                EnableReset = false,
                EnableMachineRunType = false
            };

            // 急停：设备被紧急停止，仅允许复位操作
            _statusMenuMappings[MachineRunStatus.EmergencyStopped] = new MenuConfig
            {
                PauseStatus = 0,
                EnableStart = false,
                EnablePause = false,
                EnableStop = false,
                EnableInit = false,
                EnableReset = true,
                EnableMachineRunType = false
            };
        }
        #endregion

        #region 设备运行状态管理
        /// <summary>
        /// 更新设备运行状态并同步到UI的核心方法<br/>
        /// 执行流程：
        /// 1. 更新全局运行状态（通过MenuProxy同步到UI）
        /// 2. 应用对应状态的菜单配置（控制按钮启用/禁用）
        /// 3. 执行状态特定的业务逻辑（如急停时停止机器人）
        /// </summary>
        /// <param name="GeneralStatus">目标运行状态</param>
        /// <returns>是否更新成功（true：成功；false：失败）</returns>
        public bool UpdateMachineRunStatus(MachineRunStatus GeneralStatus)
        {
            try
            {
                // 更新全局运行状态，MenuProxy作为状态代理，统一管理UI与业务层状态
                MenuProxy.Instance.MachineRunStatus = GeneralStatus;

                // 应用菜单配置：从映射字典中获取当前状态对应的按钮配置并生效
                if (_statusMenuMappings.TryGetValue(GeneralStatus, out var config))
                {
                    ApplyMenuConfig(config);
                }

                // 处理当前状态的专属逻辑（不同状态下的差异化操作）
                HandleStatusSpecificLogic(GeneralStatus);

                return true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录异常日志，便于问题排查
                return false;
            }
        }

        /// <summary>
        /// 将菜单配置应用到UI<br/>
        /// 作用：通过MenuProxy将按钮状态（启用/禁用）同步到前端界面，确保用户操作合法性
        /// </summary>
        /// <param name="config">菜单配置对象，包含各按钮的启用状态</param>
        private void ApplyMenuConfig(MenuConfig config)
        {
            MenuProxy.Instance.PauseStatus = config.PauseStatus;
            MenuProxy.Instance.EnableStart = config.EnableStart;
            MenuProxy.Instance.EnablePause = config.EnablePause;
            MenuProxy.Instance.EnableStop = config.EnableStop;
            MenuProxy.Instance.EnableInit = config.EnableInit;
            MenuProxy.Instance.EnableReset = config.EnableReset;
            MenuProxy.Instance.EnableMachineRunType = config.EnableMachineRunType;

            if (Controller.Instance.IsSafetyDoorStatusMonitored == false)
            {
                MenuProxy.Instance.PauseStatus = 0;
            }
        }

        /// <summary>
        /// 处理特定运行状态的业务逻辑<br/>
        /// 根据设备状态执行差异化操作，确保设备行为符合预期
        /// </summary>
        /// <param name="status">当前设备运行状态</param>
        private void HandleStatusSpecificLogic(MachineRunStatus status)
        {
            if (StationInfo.CachedMachineRunStatus == status)
            {
                return;
            }

            switch (status)
            {
                case MachineRunStatus.None:
                    // 无状态：重置暂停监控，避免残留状态影响后续操作
                    IsMachinePauseMonitored = false;
                    break;
                case MachineRunStatus.Paused:
                    // 判断暂停的原因是否为STO（安全转矩关闭）。
                    // 依据是 Controller.Instance.IsMachineInternalStoMonitored 的状态。
                    // 这个标志通常在检测到STO触发条件（如急停、安全门打开）时被设置为true。

                    // 如果 IsMachineInternalStoMonitored 为 true，说明当前暂停是由于STO事件触发的。
                    if (Controller.Instance.IsMachineInternalStoMonitored)
                    {
                        _isAlarmCausedBySTO = true;

                    }
                    // 备注：此处可以添加暂停状态下的相关处理逻辑，例如更新UI显示、记录暂停日志等。
                    // 目前仅处理了暂停原因的判断和标记。
                    break;
                case MachineRunStatus.Homing:      // 回零中
                case MachineRunStatus.HomeSuccess:  // 回零完成
                case MachineRunStatus.Running:     // 运行中
                    // 缓存当前状态，用于暂停后恢复到正确状态
                    StationInfo.CachedMachineRunStatus = status;
                    // 运行状态：发送继续指令给机器人，恢复正常工作
                    break;
                case MachineRunStatus.Alarming:
                    // 判断报警的原因是否为STO（安全转矩关闭）。
                    // 依据是 Controller.Instance.IsMachineInternalStoMonitored 的状态。
                    // 这个标志通常在检测到STO触发条件（如急停、安全门打开）时被设置为true。

                    // 如果 IsMachineInternalStoMonitored 为 true，说明当前暂停是由于STO事件触发的。
                    if (Controller.Instance.IsMachineInternalStoMonitored)
                    {
                        _isAlarmCausedBySTO = true;
                    }

                    // 备注：此处可以添加通用的报警处理逻辑，例如触发声光报警器、记录报警日志等。
                    // 目前仅处理了报警原因的判断和标记。
                    break;
                case MachineRunStatus.EmergencyStopped:
                    // 急停状态：
                    // 1. 停止机器人运动
                    // 2. 记录急停日志
                    // 3. 关闭报警窗口（减少干扰）
                    StationInfo.CachedMachineRunStatus = status;
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "设备被拍下急停，如需继续运行，请重新初始化！！！");
                    WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () => AlarmWindowManager.CloseAlarmWindow());
                    break;
            }
        }
        #endregion

        #region 设备操作（初始化、启动、停止等）
        /// <summary>
        /// 初始化工位设备的核心方法<br/>
        /// 初始化流程（安全校验→用户确认→资源检查→参数配置→状态更新）：
        /// 1. 检查初始化前置条件（如急停/暂停状态不允许初始化）
        /// 2. 显示确认对话框，需人工确认设备状态
        /// 3. 检查资源是否未释放（如CPU占用过高）
        /// 4. 选择运动速度（如需手动调整）
        /// 5. 更新状态为回零中并启动复位流程
        /// </summary>
        public void InitStation()
        {
            try
            {
                // 检查初始化前置条件，不符合则直接返回
                if (!CheckInitConditions())
                    return;

                // 显示确认对话框，确保操作人员知晓并确认初始化操作
                if (!ShowConfirmDialog("确定要进行初始化操作吗？\r\n请人工确认机台是否符合初始化条件！！"))
                    return;

                // 检查资源是否未释放（如CPU占用过高可能导致初始化失败）
                if (IsResourceNotReleased())
                {
                    HandleResourceReleaseIssue(); // 处理资源释放问题（如提示重启软件）
                    return;
                }

                // 关键：同步等待异步方法 RefreshParamAsync 完成
                // 注意：会阻塞当前线程，适合初始化等必须按顺序执行的场景
                bool isRefreshSuccess = RefreshParamAsync().GetAwaiter().GetResult();

                // 选择运动轴速度（根据实际需求手动调整）
                SelectMotionSpeedIfNeeded();
                IniSign();
                // 更新状态为回零中，并启动复位流程
                UpdateMachineRunStatus(MachineRunStatus.Homing);
                StartResetProcess();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录初始化过程中的异常
            }
        }

        /// <summary>
        /// 启动设备自动运行的核心方法<br/>
        /// 启动流程：
        /// 1. 检查当前状态是否为回零完成（仅回零后可启动）
        /// 2. 检查启动条件（如无急停、流程已初始化）
        /// 3. 显示确认对话框，需人工确认
        /// 4. 选择运动速度（如需调整）
        /// 5. 加载对应模式的运行流程
        /// 6. 更新状态为运行中
        /// </summary>
        public void StartStation()
        {
            try
            {
                // 仅回零完成状态可启动，避免设备在未初始化状态下运行
                if (MenuProxy.Instance.MachineRunStatus != MachineRunStatus.HomeSuccess)
                    return;

                // 检查启动条件（如无急停、流程已初始化）
                if (!CheckStartConditions())
                    return;

                // 人工确认设备状态，确保满足自动运行条件
                if (!ShowConfirmDialog("请人工确认机台是否符合自动运行条件！！"))
                    return;

                // 选择运动速度（根据需求调整）
                SelectMotionSpeedIfNeeded();

                // 根据操作类型（联机/单机）加载对应流程
                LoadProcessByOperationType();

                // 记录启动时间，更新日志，切换状态为运行中
                Frm_Main.Instance.RunStartTime = DateTime.Now;
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black, "工位已开启自动运行状态！");
                UpdateMachineRunStatus(MachineRunStatus.Running);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录启动过程中的异常
            }
        }

        /// <summary>
        /// 停止设备运行（急停）<br/>
        /// 急停逻辑：
        /// 1. 检查当前状态（急停状态不重复处理）
        /// 2. 非报警状态下显示确认对话框（避免误操作）
        /// 3. 更新状态为急停，触发设备停止
        /// </summary>
        public void Stop()
        {
            try
            {
                var currentStatus = MenuProxy.Instance.MachineRunStatus;
                if (currentStatus == MachineRunStatus.EmergencyStopped)
                    return; // 已急停，无需重复处理

                // 非报警状态需确认（报警状态直接停止，减少操作步骤）
                if (currentStatus != MachineRunStatus.Alarming &&
                    !ShowConfirmDialog("确定要切换到停止模式吗？"))
                    return;

                // 更新状态为急停，触发设备停止逻辑
                UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录停止过程中的异常
            }
        }

        /// <summary>
        /// 暂停设备运行<br/>
        /// 暂停逻辑：
        /// 1. 仅允许在回零中/运行中/回零完成状态暂停（其他状态无需暂停）
        /// 2. 暂停前缓存当前状态（用于后续恢复）
        /// 3. 切换到暂停状态并启用暂停监控
        /// </summary>
        public void Pause()
        {
            try
            {
                var currentStatus = MenuProxy.Instance.MachineRunStatus;
                // 仅允许在特定状态暂停，避免无效操作
                if (currentStatus == MachineRunStatus.Homing || currentStatus == MachineRunStatus.Running || currentStatus == MachineRunStatus.HomeSuccess)
                {
                    StationInfo.CachedMachineRunStatus = currentStatus; // 缓存当前状态，用于恢复
                    UpdateMachineRunStatus(MachineRunStatus.Paused);    // 切换到暂停状态
                    IsMachinePauseMonitored = true;                     // 启用暂停监控
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录暂停过程中的异常
            }
        }

        /// <summary>
        /// 恢复设备运行（从暂停状态继续）<br/>
        /// 恢复流程：
        /// 1. 检查当前是否为暂停状态（非暂停状态不处理）
        /// 2. 显示确认对话框，需人工确认设备状态
        /// 3. 恢复到暂停前的状态（如运行中/回零中）
        /// </summary>
        public void Resume()
        {
            try
            {

                if (MenuProxy.Instance.MachineRunStatus != MachineRunStatus.Paused)
                    return; // 非暂停状态不处理

                // 人工确认后恢复运行，确保设备状态安全
                if (!ShowConfirmDialog("确定继续执行？\r\n请人工确认机台是否符合继续运行条件！！"))
                {
                    UpdateMachineRunStatus(MachineRunStatus.Paused); // 取消恢复，保持暂停
                    return;
                }

                // 恢复到暂停前的状态（从缓存中获取）
                UpdateMachineRunStatus(StationInfo.CachedMachineRunStatus);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录恢复过程中的异常
            }
        }

        /// <summary>
        /// 复位设备（从急停/报警状态恢复）<br/>
        /// 复位逻辑：
        /// 1. 检查是否已在复位中（避免重复操作）
        /// 2. 更新状态为复位中，触发复位流程
        /// </summary>
        public void Reset()
        {
            try
            {
                if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Reseting)
                    return; // 已在复位中，无需处理

                UpdateMachineRunStatus(MachineRunStatus.Reseting);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录复位过程中的异常
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 检查初始化条件是否满足<br/>
        /// 禁止在以下状态执行初始化：
        /// - 急停状态（EmergencyStopped）
        /// - 暂停状态（Paused）
        /// - 回零中状态（Homing）
        /// 同时测试流程报警记录功能是否正常
        /// </summary>
        /// <returns>是否满足条件（true：满足；false：不满足）</returns>
        private bool CheckInitConditions()
        {
            var currentStatus = MenuProxy.Instance.MachineRunStatus;
            if (currentStatus == MachineRunStatus.EmergencyStopped)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "接收到急停信号，不允许手动执行工位初始化操作！！！");
                return false;
            }

            if (currentStatus == MachineRunStatus.Paused)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "接收到暂停信号，不允许手动执行工位初始化操作！！！");
                return false;
            }

            //if (!IsSafetyDoorStatusMonitored)
            //{
            //    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "安全门未关，不允许手动执行工位初始化操作！！！");
            //    return false;
            //}

            if (currentStatus == MachineRunStatus.Homing)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, "工位正处于初始化中......");
                return false;
            }

            // 测试流程报警记录功能是否正常，确保异常能被正确记录
            if (!TestProcessErrorFunctionality())
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "用于运行流程报警记录的功能测试失败，请重试！！");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 显示确认对话框<br/>
        /// 封装通用对话框逻辑，统一处理用户确认交互<br/>
        /// 使用模态对话框（ShowDialog）确保操作的同步性
        /// </summary>
        /// <param name="message">对话框提示信息</param>
        /// <returns>用户是否确认（true：是；false：否）</returns>
        private bool ShowConfirmDialog(string message)
        {
            Frm_ConfirmBox.Instance.lbl_Message.Text = message;
            Frm_ConfirmBox.Instance.Visible = false;
            Frm_ConfirmBox.Instance.TopMost = true; // 置顶显示，确保用户能看到
            Frm_ConfirmBox.Instance.ShowDialog(); // 模态对话框，阻塞等待用户输入

            return Frm_ConfirmBox.Instance.Result == ConfirmBoxResult.Yes;
        }

        /// <summary>
        /// 检查资源是否未释放（如CPU占用过高）<br/>
        /// 判定逻辑：通过全局变量CpuAbnormalMonitor判断，值>50视为资源未释放
        /// </summary>
        /// <returns>是否存在资源未释放问题（true：是；false：否）</returns>
        private bool IsResourceNotReleased()
        {
            return Project.Instance.GlobalVariables.CpuAbnormalMonitor > 50;
        }

        /// <summary>
        /// 处理资源未释放问题<br/>
        /// 处理流程：
        /// 1. 记录资源未释放日志
        /// 2. 提示用户重启软件，确认后强制关闭程序
        /// </summary>
        private void HandleResourceReleaseIssue()
        {
            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "系统检测到上次运行资源未完全释放，请\r\n重启软件后再初始化机台！！！！！");

            if (ShowConfirmDialog("\r\n系统检测到上次运行资源未完全释放，请\r\n重启软件后再初始化机台！！！！！\r\n点击“是”--关闭软件！！！"))
            {
                if (Project.Instance.GlobalVariables.CpuAbnormalMonitor > 10)
                {
                    ForceShutdown(); // 强制关闭软件，释放资源
                }
            }
        }

        /// <summary>
        /// 选择运动轴速度（如需手动调整）<br/>
        /// 操作逻辑：
        /// 1. 检查运动轴点列表是否存在（存在则需要选择速度）
        /// 2. 弹出速度选择对话框，允许用户设置轴运行速度
        /// </summary>
        private void SelectMotionSpeedIfNeeded()
        {
            if (MotionSystem.Instance.MotionPoints != null && MotionSystem.Instance.MotionPoints.Count > 0)
            {
                // 显示速度选择对话框
                using (var speedBox = new Frm_SpeedBox())
                {
                    speedBox.lbl_info.Text = "在执行该流程前，请选择轴运行的速度！！默认：100%！！！";
                    speedBox.TopMost = true; // 置顶显示
                    speedBox.ShowDialog();
                }
            }
        }

        /// <summary>
        /// 启动复位流程<br/>
        /// 复位流程：
        /// 1. 初始化复位流程状态为"未运行"
        /// 2. 初始化IO状态数组（记录IO点状态）
        /// 3. 启动复位流程任务（AutoCore_手动为手动模式流程执行器）
        /// 4. 异步监控流程状态（避免阻塞主线程）
        /// </summary>
        private void StartResetProcess()
        {
            Project.Instance.StationInfo.ResetProcessStatus = ProcessStatus.NotRunning;

            // 初始化IO状态数组，用于记录各IO点的状态
            for (int i = 0; i < StationInfo.DIs.Length; i++)
            {
                StationInfo.DIs[i] = new IOStateKeep();
            }

            // 启动复位流程（AutoCore_手动为手动模式的流程执行器）
            AutoCore_手动 autoRun = new AutoCore_手动(ProcessPath._复位流程_ProcessPath, "复位流程");
            autoRun.Run();
            Project.Instance.StationInfo.ResetProcessStatus = ProcessStatus.Running;

            // 异步监控流程状态（使用Task.Run避免阻塞主线程）
            bool flag = true;
            Task.Run(() =>
            {
                while (flag)
                {
                    Thread.Sleep(100); // 100ms轮询一次，平衡实时性与性能
                    switch (autoRun.AutoRunStatus)
                    {
                        case GeneralStatus.Wait:
                            // 等待状态，无操作
                            break;
                        case GeneralStatus.NoError:
                            // 流程无错误完成：更新状态为回零完成，记录日志
                            flag = false;
                            UpdateMachineRunStatus(MachineRunStatus.HomeSuccess);
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black, "工位初始化完成，允许手动执行工位相关操作！！");
                            Project.Instance.StationInfo.ResetProcessStatus = ProcessStatus.Completed;
                            break;
                        case GeneralStatus.Running:
                            // 流程运行中：更新状态标识
                            Project.Instance.StationInfo.ResetProcessStatus = ProcessStatus.Running;
                            break;
                        case GeneralStatus.Error:
                            // 流程错误：更新状态为报警，记录日志
                            flag = false;
                            UpdateMachineRunStatus(MachineRunStatus.Alarming);
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "工位初始化失败！！");
                            Project.Instance.StationInfo.ResetProcessStatus = ProcessStatus.Alarm;
                            return;
                    }
                }
            });
        }

        /// <summary>
        /// 检查启动条件是否满足<br/>
        /// 禁止在以下状态启动：
        /// - 急停状态（EmergencyStopped）
        /// - 暂停状态（Paused）
        /// - 未完成初始化（复位流程未完成）
        /// 额外条件：自动模式下需处于联机操作类型
        /// </summary>
        /// <returns>是否满足条件（true：满足；false：不满足）</returns>
        private bool CheckStartConditions()
        {
            var currentStatus = MenuProxy.Instance.MachineRunStatus;
            if (currentStatus == MachineRunStatus.EmergencyStopped)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "接收到急停信号，不允许启动机台自动运行！！！");
                return false;
            }

            if (currentStatus == MachineRunStatus.Paused)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "接收到暂停信号，不允许启动机台自动运行！！！");
                return false;
            }

            // 需完成初始化才能启动，确保设备处于就绪状态
            if (Project.Instance.StationInfo.ResetProcessStatus != ProcessStatus.Completed)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, "工位未完成初始化操作，请先初始化工位后再尝试！！！");
                return false;
            }

            //// 自动模式下需处于联机操作类型，确保通信正常
            //if (MenuProxy.Instance.MachineRunModel == MachineMode.Auto &&
            //    MenuProxy.Instance.OperateType != OperateMode.Online)
            //{
            //    RLog.SaveLog(LogType.Operation,LogLevel.Info, Color.Orange, "工站未处于联机模式，不允许启动机台运行！！！");
            //    return false;
            //}

            return true;
        }

        /// <summary>
        /// 根据操作类型加载对应运行流程<br/>
        /// 联机模式加载联机流程，单机模式加载单机流程（此处单机流程省略）
        /// </summary>
        private void LoadProcessByOperationType()
        {
            switch (MenuProxy.Instance.OperateType)
            {
                case OperateMode.Online:
                    LoadOnlineProcesses(); // 加载联机流程
                    break;
                case OperateMode.Standalone:
                    // 单机模式流程（省略）
                    LoadStandaloneProcesses();
                    break;
            }
        }
        private void LoadOnlineProcesses()
        {


            StationInfo.Get_EPSON2上相机 = EPSON2上相机_ProcessMode.判断是否需要拍照;
            new AutoCore(ProcessPath.EPSON2上相机_主流程path, "Epson2上相机流程").Run();



            StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.下相机拍照点位确认;
            new AutoCore(ProcessPath.EPSON2下相机_主流程path, "Epson2下相机流程").Run();


            StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.等待请求最终结果;
            new AutoCore(ProcessPath.EPSON2请求最终结果_主流程path, "Epson2请求最终结果流程").Run();






            StationInfo.Get_EPSON1上相机 = EPSON1上相机_ProcessMode.判断是否需要拍照;
            new AutoCore(ProcessPath.EPSON1上相机_主流程path, "Epson1上相机流程").Run();



            StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.下相机拍照点位确认;
            new AutoCore(ProcessPath.EPSON1下相机_主流程path, "Epson1下相机流程").Run();


            StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.等待请求最终结果;
            new AutoCore(ProcessPath.EPSON1请求最终结果_主流程path, "Epson1请求最终结果流程").Run();


            StationInfo.Get_飞拍组装 = 飞拍组装流程_ProcessMode.飞拍;
            new AutoCore(ProcessPath.飞拍组装流程_主流程path, "飞拍组装流程").Run();

        }





        private void LoadStandaloneProcesses()
        {
            StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.治具选择;
            new AutoCore(ProcessPath.治具选择_主流程path, "换治具流程").Run();
        }



        /// <summary>
        /// 测试流程错误记录功能是否正常<br/>
        /// 验证报警记录服务（ProcessErrorService）是否可正常工作<br/>
        /// 通过添加测试报警记录并检查结果判断功能是否正常
        /// </summary>
        /// <returns>测试是否通过（true：通过；false：失败）</returns>
        private bool TestProcessErrorFunctionality()
        {
            try
            {
                var testError = new ProcessErrorDto
                {
                    报警ID = "test",
                    异常开始时间 = DateTime.Now.ToString("HH:mm:ss"),
                    结束时间 = DateTime.Now.ToString("HH:mm:ss"),
                    报警内容 = "test",
                    耗时 = 0,
                    日期 = DateTime.Now.Date
                };

                return ProcessErrorService.Instance.Add(testError).Status;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return false;
            }
        }

        /// <summary>
        /// 强制关闭软件<br/>
        /// 用于资源无法释放时的紧急处理，确保硬件资源正确释放：
        /// 1. 关闭蜂鸣器LED
        /// 2. 标记程序即将退出（通知其他模块停止操作）
        /// 3. 更新状态为急停
        /// 4. 断开相机、运动轴卡、Modbus设备、TCP连接
        /// </summary>
        private void ForceShutdown()
        {
            try
            {
                // 关闭蜂鸣器LED，停止报警提示
                SysParm.Instance.BuzzerLed.SetBuzzerLedStatus(BuzzerLED.LedColor.Undefined);
                // 标记程序即将退出，通知其他模块停止操作
                Project.Instance.GlobalVariables.IsProgramExiting = true;
                // 更新状态为急停
                UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);

                // 断开所有相机连接
                CogFrameGrabberGigEs cameras = new CogFrameGrabberGigEs();
                foreach (CogFrameGrabberGigE icf in cameras)
                {
                    icf.Disconnect(true);
                }

                // 关闭运动轴卡，释放运动控制资源
                var motionCards = MotionSystem.Instance.MotionCards;
                foreach (var card in motionCards)
                {
                    MotionSystem.Instance.MotionController.Close(card);
                }



                // 断开Modbus设备连接（PLC/机器人）
                DisconnectModbusDevices();

                // 关闭TCP连接（服务器/客户端）
                CloseTcpConnections();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 断开所有Modbus设备连接<br/>
        /// 包括PLC和机器人，释放通信资源，避免残留连接占用资源
        /// </summary>
        private void DisconnectModbusDevices()
        {
            foreach (var plc in ModbusSystem.Instance.PlcModbusList)
            {
                plc.Disconnect();
            }

            foreach (var robot in ModbusSystem.Instance.RobotModbusList)
            {
                robot.Disconnect();
            }
        }

        /// <summary>
        /// 关闭所有TCP连接<br/>
        /// 包括TCP服务器（关闭客户端）和TCP客户端（断开连接），确保网络资源释放
        /// </summary>
        private void CloseTcpConnections()
        {
            foreach (var server in TcpIpSystem.Instance.TcpServers)
            {
                server.CloseAllClients(); // 关闭所有客户端连接
                server.StopServer();         // 关闭服务器
            }

            foreach (var client in TcpIpSystem.Instance.TcpClients)
            {
                client.Close();         // 关闭客户端
            }
        }

        /// <summary>
        /// 刷新系统参数（同步各模块的最新状态）<br/>
        /// 目的：确保初始化过程中各模块参数的一致性，避免因缓存导致的状态偏差<br/>
        /// 同步范围：IO、Modbus、视觉、TCPIP、运动、串口等核心模块
        /// </summary>
        /// <returns>true：所有模块刷新成功；false：至少一个模块刷新失败</returns>
        /// <summary>
        /// 刷新系统参数（同步各模块的最新状态）<br/>
        /// 目的：确保初始化过程中各模块参数的一致性，避免因缓存导致的状态偏差<br/>
        /// 同步范围：IO、Modbus、视觉、TCPIP、运动、串口等核心模块
        /// </summary>
        /// <returns>true：所有模块刷新成功；false：至少一个模块刷新失败</returns>
        private async Task<bool> RefreshParamAsync()
        {
            // 记录各模块刷新结果
            var refreshResults = new Dictionary<string, bool>();

            try
            {
                // 异步并行刷新所有模块，提高效率
                await Task.WhenAll(
                    RefreshModuleAsync("IO模块", () => IOSystem.Instance.RefreshDataAsync(), refreshResults),
                    RefreshModuleAsync("Modbus设备", () => ModbusSystem.Instance.RefreshDataAsync(), refreshResults),
                    RefreshModuleAsync("视觉系统", () => VisionSystem.Instance.RefreshDataAsync(), refreshResults),
                    RefreshModuleAsync("TCPIP通讯", () => TcpIpSystem.Instance.RefreshDataAsync(), refreshResults),
                    RefreshModuleAsync("运动系统", () => MotionSystem.Instance.RefreshDataAsync(), refreshResults),
                    RefreshModuleAsync("串口设备", () => SerialportSystem.Instance.RefreshDataAsync(), refreshResults)
                );

                // 检查是否所有模块都刷新成功
                return refreshResults.Values.All(sucess => true);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return false;
            }
        }

        /// <summary>
        /// 刷新单个模块数据并记录结果
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="refreshAction">刷新操作</param>
        /// <param name="results">结果字典</param>
        private async Task RefreshModuleAsync(string moduleName, Func<Task> refreshAction, Dictionary<string, bool> results)
        {
            try
            {
                await refreshAction();
                results[moduleName] = true;
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green, $"{moduleName}数据刷新成功");
            }
            catch (Exception ex)
            {
                results[moduleName] = false;
                RLog.SaveError(ex);
            }
        }

        #endregion

        #region 常量定义（提取硬编码字符串和参数）
        private const string PauseSignalAlarm = "接收到来自机台的暂停信号！！！";
        private const string RobotAlarmSignal = "接收到来自机械手的报警信号！！！";
        private const string EpsonCollisionRisk = "左右Epson同时处于非安全区域，有撞机风险！！！！！！！";
        private const int IoSignalDelay = 100; // IO信号延迟毫秒数，确保硬件有足够时间响应
        #endregion

        #region 模式检查辅助方法

        /// <summary>
        /// 检查当前是否为PurePC控制模式
        /// 所有监控操作前都需要调用此方法验证
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

        #region 机台暂停监控
        private bool _isMachinePauseMonitored;
        private DateTime? _pauseSignalStartTime; // 记录暂停信号开始时间（即IsMachinePauseMonitored变为true的时间）

        /// <summary>
        /// 机台暂停监控状态
        /// 触发逻辑：状态从 true → false 时（暂停信号释放）触发暂停处理
        /// 处理内容：停止运动轴并报警
        /// 仅在PurePC模式下生效
        /// </summary>
        public bool IsMachinePauseMonitored
        {
            get => _isMachinePauseMonitored;
            set
            {
                // 非PurePC模式下不处理状态变化
                if (!IsPurePCMode())
                {
                    _isMachinePauseMonitored = value;
                    return;
                }

                // 状态从 true 变为 false 时，触发暂停处理
                if (_isMachinePauseMonitored && !value)
                {
                    // 记录暂停信号释放的时间点，用于计算持续时间
                    _pauseSignalStartTime = DateTime.Now;
                    OnMachinePauseTriggered();
                }
                // 状态从 false 变为 true 时，重置开始时间（如果需要）
                else if (!_isMachinePauseMonitored && value)
                {
                    // 在触发暂停后，检查是否需要解锁安全门
                    OnMachinePauseReleased();
                    // 当监控重新开启时，可以选择重置之前的时间记录
                    _pauseSignalStartTime = null;
                }

                _isMachinePauseMonitored = value;
            }
        }

        /// <summary>
        /// 机台暂停触发时的处理逻辑（现在在信号释放时触发）
        /// </summary>
        private void OnMachinePauseTriggered()
        {
            if (!IsPurePCMode())
                return;

            try
            {
                // 停止所有运动轴
                if (MotionSystem.Instance?.MotionAxes != null && MotionSystem.Instance.MotionAxes.Count > 0)
                {
                    StopAllMotionAxes();
                }
                //ExecuteIoPulse("ModbusRobot|输出|Robot|Epson1Pause|1|5", 0, 0);
                //ExecuteIoPulse("ModbusRobot|输出|Robot|Epson2Pause|1|5", 0, 0);

                new ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|Epson1Pause|1|3", "隐藏").Run();
                new ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|Epson2Pause|1|3", "隐藏").Run();
                Thread.Sleep(200);
                new ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|Epson1Pause|1|3", "隐藏").Run();
                new ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|Epson2Pause|0|3", "隐藏").Run();
                // 记录报警信息并触发警告
                if (Project.Instance?.GlobalVariables != null)
                {
                    Project.Instance.GlobalVariables.AlarmMessage = PauseSignalAlarm;
                    StatusUpdater.TriggerWarning(WarningColor.Orange);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 暂停信号释放后解锁安全门的逻辑
        /// 核心：仅当暂停信号（IsMachinePauseMonitored为true的状态）持续满1秒才解锁门
        /// </summary>
        private void OnMachinePauseReleased()
        {
            // 非手动模式或无暂停开始时间记录，直接返回
            if (!IsPurePCMode() || MenuProxy.Instance.MachineRunModel != MachineMode.Manual || !_pauseSignalStartTime.HasValue)
                return;

            try
            {
                // 计算从暂停信号释放到现在的时间（这里逻辑需要调整，因为我们现在是在释放时触发）
                // 实际上，我们需要计算的是 IsMachinePauseMonitored 处于 true 状态的总时长
                // 假设在进入此方法时，_pauseSignalStartTime 记录的是信号释放的时间点
                // 这似乎不对，我们需要重新考虑。

                // 正确的逻辑应该是：
                // 当 IsMachinePauseMonitored 变为 false 时，我们需要知道它之前保持 true 状态有多久。
                // 因此，_pauseSignalStartTime 应该在 IsMachinePauseMonitored 变为 true 时记录。
                // 让我们调整一下思路：
                // 1. 当 IsMachinePauseMonitored 变为 true 时，记录开始时间 (StartTimer)。
                // 2. 当 IsMachinePauseMonitored 变为 false 时，记录结束时间 (EndTimer)。
                // 3. 计算 EndTimer - StartTimer 的时长。

                // 所以，我们需要在 IsMachinePauseMonitored 变为 true 时记录开始时间。
                // 让我们回到属性的 set 访问器进行调整。

                // 假设我们已经在 IsMachinePauseMonitored 变为 true 时记录了开始时间，
                // 那么当它变为 false 时，我们可以这样计算：
                var elapsedMs = (DateTime.Now - _pauseSignalStartTime.Value).TotalMilliseconds;

                // 如果持续时间满1秒，则解锁
                if (elapsedMs >= 1000)
                {
                    UnlockSafetyDoor();
                }
                // （可选）如果需要在不足1秒时也做些什么，可以在这里添加
                // else
                // {
                //     // 例如，记录一个警告或执行其他逻辑
                // }

            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                _pauseSignalStartTime = null; // 重置开始时间
            }
        }


        /// <summary>
        /// 解锁安全门（封装为独立方法，便于复用和维护）
        /// </summary>
        private void UnlockSafetyDoor()
        {
            if (Project.Instance?.GlobalVariables?.IsProgramExiting != true)
            {
                var unlockProcess = new IOProcess("IO|输出|前安全门上锁|0|5", "隐藏");
                unlockProcess.Run();
            }
        }

        /// <summary>
        /// 安全停止所有运动轴
        /// </summary>
        private void StopAllMotionAxes()
        {
            if (!IsPurePCMode())
                return;

            foreach (var axis in MotionSystem.Instance.MotionAxes)
            {
                if (Project.Instance?.GlobalVariables?.IsProgramExiting == true)
                    break;

                MotionSystem.Instance.MotionController?.SmtStop(axis, 0);
            }
        }
        #endregion

        #region 机台启动监控
        private bool _isMachineStartMonitored;
        private DateTime? _startSignalStartTime; // 记录启动信号开始时间

        /// <summary>
        /// 机台启动监控状态
        /// 触发逻辑：状态从 false → true 时开始计时，若信号持续满1秒，则触发启动处理
        /// 处理内容：根据缓存的状态恢复设备运行（如从暂停→运行中）
        /// 仅在PurePC模式下生效
        /// </summary>
        public bool IsMachineStartMonitored
        {
            get => _isMachineStartMonitored;
            set
            {
                // 非PurePC模式下不处理状态变化
                if (!IsPurePCMode())
                {
                    _isMachineStartMonitored = value;
                    return;
                }

                // 状态从 false → true 时，记录启动信号开始时间
                if (!_isMachineStartMonitored && value)
                {
                    _startSignalStartTime = DateTime.Now;
                    OnMachineStartTriggered();
                }
                // 状态从 true → false 时，检查信号持续时间
                else if (_isMachineStartMonitored && !value)
                {
                    OnMachineStartReleased();
                }

                _isMachineStartMonitored = value;
            }
        }

        /// <summary>
        /// 启动信号释放时的处理逻辑
        /// 核心：仅当启动信号持续满3秒才触发真正的启动操作
        /// </summary>
        private void OnMachineStartReleased()
        {
            // 非PurePC模式或无启动开始时间记录，直接返回
            if (!IsPurePCMode() || !_startSignalStartTime.HasValue)
                return;

            try
            {
                // 计算启动信号持续的时间
                var elapsedMs = (DateTime.Now - _startSignalStartTime.Value).TotalMilliseconds;
                var currentStatus = MenuProxy.Instance.MachineRunStatus;

                // 如果持续时间满3秒，则触发启动逻辑
                if (elapsedMs >= 3000)
                {
                    if (MenuProxy.Instance.MachineRunModel == MachineMode.Manual)
                    {
                        return;
                    }

                    if (currentStatus == MachineRunStatus.HomeSuccess)
                    {
                        WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () =>
                        {
                            try
                            {
                                Project.Instance.Controller.StartStation();
                            }
                            catch (Exception ex)
                            {
                                RLog.SaveError(ex);
                            }
                        });

                    }
                }
                // 否则，不执行任何操作，视为无效的短按
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                // 无论是否触发启动，都重置开始时间
                _startSignalStartTime = null;
            }
        }

        /// <summary>
        /// 机台启动触发时的处理逻辑
        /// 核心操作：根据缓存的状态恢复设备运行
        /// </summary>
        private void OnMachineStartTriggered()
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            try
            {
                // 安全门未监控或仍处于STO状态，则不启动
                if (Controller.Instance.IsSafetyDoorStatusMonitored == false || Controller.Instance.IsMachineInternalStoMonitored)
                {
                    return;
                }

                // 自动模式下，需检查前后站安全门状态
                if (MenuProxy.Instance.MachineRunModel == MachineMode.Auto)
                {
                    if (Controller.Instance.前站安全门状态监控 == false || Controller.Instance.后站安全门状态监控 == false)
                    {
                        return;
                    }
                }

                // 局部变量简化代码，减少重复访问
                var currentStatus = MenuProxy.Instance.MachineRunStatus;
                var cachedStatus = Controller.Instance?.StationInfo?.CachedMachineRunStatus;

                // 状态有效性检查：缓存状态为空则不处理
                if (cachedStatus == null) return;

                // 根据当前状态和缓存状态恢复运行
                if (currentStatus == MachineRunStatus.Paused)
                {
                    switch (cachedStatus)
                    {
                        case MachineRunStatus.Running:
                        case MachineRunStatus.Homing:
                        case MachineRunStatus.HomeSuccess:
                            UpdateMachineRunStatus(cachedStatus.Value);
                            break;
                        case MachineRunStatus.Reseting:
                        case MachineRunStatus.EmergencyStopped:
                            UpdateMachineRunStatus(MachineRunStatus.Reseting);
                            break;
                        default:
                            UpdateMachineRunStatus(MachineRunStatus.None);
                            break;
                    }
                }
                // 如果当前已在运行，则直接更新状态以确保一致性
                else if (currentStatus == MachineRunStatus.Running)
                {
                    UpdateMachineRunStatus(MachineRunStatus.Running);
                }

                // 如果STO已解除，重置STO状态并检查轴使能
                if (Controller.Instance.IsMachineInternalStoMonitored == false)
                {
                    ResetStoAndCheckAxisEnable();
                }
                //if (Controller.Instance.前焊接机无焊接中信号报警)
                //{
                //    ExecuteIoPulse("IO|输出|前焊接机启动", 1, 1);
                //    Controller.Instance.前焊接机无焊接中信号报警 = false;
                //}
                //if (Controller.Instance.后焊接机无焊接中信号报警)
                //{
                //    ExecuteIoPulse("IO|输出|后焊接机启动", 1, 1);
                //    Controller.Instance.后焊接机无焊接中信号报警 = false;
                //}


            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 机械手报警监控
        private bool _isRobotAlarmMonitored;
        /// <summary>
        /// 机械手报警监控状态
        /// 触发逻辑：状态从false→true时（首次检测到报警信号）触发报警处理
        /// 处理内容：记录报警信息并触发橙色警告
        /// 仅在PurePC模式下生效
        /// </summary>
        public bool IsRobotAlarmMonitored
        {
            get => _isRobotAlarmMonitored;
            set
            {
                // 非PurePC模式下不处理状态变化
                if (!IsPurePCMode())
                {
                    _isRobotAlarmMonitored = value;
                    return;
                }

                if (!_isRobotAlarmMonitored && value)
                {
                    OnRobotAlarmTriggered();
                }
                _isRobotAlarmMonitored = value;
            }
        }

        /// <summary>
        /// 机械手报警触发时的处理逻辑
        /// 触发条件：仅在当前处于报警状态时处理（避免无效操作）
        /// 核心操作：记录报警信息到全局变量，触发橙色警告
        /// 异常处理：记录处理过程中的异常
        /// 仅在PurePC模式下执行
        /// </summary>
        private void OnRobotAlarmTriggered()
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            try
            {
                // 仅在当前处于报警状态时处理，过滤无效触发
                if (!_isRobotAlarmMonitored && MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming)
                {
                    if (Project.Instance?.GlobalVariables != null)
                    {
                        Project.Instance.GlobalVariables.AlarmMessage = RobotAlarmSignal;
                        StatusUpdater.TriggerWarning(WarningColor.Orange);
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 机台急停监控
        private bool _isMachineEmergencyStopMonitored;
        /// <summary>
        /// 机台急停监控状态
        /// 触发逻辑：状态变化时（true↔false）触发急停处理
        /// 处理内容：触发红色警告（最高严重程度）
        /// 仅在PurePC模式下生效
        /// </summary>
        public bool IsMachineEmergencyStopMonitored
        {
            get => _isMachineEmergencyStopMonitored;
            set
            {
                // 非PurePC模式下不处理状态变化
                if (!IsPurePCMode())
                {
                    _isMachineEmergencyStopMonitored = value;
                    return;
                }

                if (_isMachineEmergencyStopMonitored != value)
                {
                    OnMachineEmergencyStopChanged();
                }
                _isMachineEmergencyStopMonitored = value;
            }
        }

        /// <summary>
        /// 机台急停状态变化时的处理逻辑
        /// 核心操作：
        /// 1. 急停解除时（状态从true→false）触发红色警告
        /// 2. 使用WFUtil.UpdateMainThreadUIAsync确保UI操作在主线程执行（避免跨线程异常）
        /// 异常处理：记录处理过程中的异常
        /// 仅在PurePC模式下执行
        /// </summary>
        private void OnMachineEmergencyStopChanged()
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            try
            {
                ExecuteIoPulse("IO|输出|前覆膜机正转", 0, 0);
                ExecuteIoPulse("IO|输出|后覆膜机正转", 0, 0);
                ExecuteIoPulse("IO|输出|前覆膜机反转", 0, 0);
                ExecuteIoPulse("IO|输出|后覆膜机反转", 0, 0);

                if (!_isMachineEmergencyStopMonitored)
                {
                    // UI操作必须在主线程执行，避免WinForm跨线程操作异常
                    WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () =>
                    {
                        StatusUpdater.TriggerWarning(WarningColor.Red);
                    });
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 机台初始化监控
        private bool _isMachineInitializingMonitored;
        /// <summary>
        /// 机台初始化监控状态
        /// 触发逻辑：状态变化时（true↔false）触发初始化警告
        /// 处理内容：触发白色警告（提示初始化状态）
        /// 仅在PurePC模式下生效
        /// </summary>
        public bool IsMachineInitializingMonitored
        {
            get => _isMachineInitializingMonitored;
            set
            {
                // 非PurePC模式下不处理状态变化
                if (!IsPurePCMode())
                {
                    _isMachineInitializingMonitored = value;
                    return;
                }

                if (_isMachineInitializingMonitored != value)
                {
                    OnMachineInitializingChanged();
                }
                _isMachineInitializingMonitored = value;
            }
        }

        /// <summary>
        /// 机台初始化状态变化时的处理逻辑
        /// 核心操作：
        /// 1. 初始化完成时（状态从true→false）触发白色警告
        /// 2. 使用WFUtil.UpdateMainThreadUIAsync确保UI操作线程安全
        /// 异常处理：记录处理过程中的异常
        /// 仅在PurePC模式下执行
        /// </summary>
        private void OnMachineInitializingChanged()
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            try
            {
                if (!_isMachineInitializingMonitored)
                {
                    WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () =>
                    {
                        StatusUpdater.TriggerWarning(WarningColor.White);
                    });
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 机台复位监控
        private bool _isMachineResetMonitored;
        /// <summary>
        /// 机台复位监控状态
        /// 触发逻辑：状态从false→true时（首次检测到复位信号）触发复位操作
        /// 处理内容：复位机器人、安全继电器等设备，清除报警状态
        /// 仅在PurePC模式下生效
        /// </summary>
        public bool IsMachineResetMonitored
        {
            get => _isMachineResetMonitored;
            set
            {
                // 非PurePC模式下不处理状态变化
                if (!IsPurePCMode())
                {
                    _isMachineResetMonitored = value;
                    return;
                }

                if (!_isMachineResetMonitored && value)
                {
                    OnMachineResetTriggered();
                }
                _isMachineResetMonitored = value;
            }
        }

        /// <summary>
        /// 机台复位触发时的处理逻辑
        /// 核心操作：
        /// 1. 复位小黄机器人暂停状态
        /// 2. 复位左右Epson机器人
        /// 3. 发送脉冲信号复位安全继电器和Epson机器人
        /// 4. 检查安全门锁状态（如未锁定则报警，当前逻辑隐藏）
        /// 异常处理：记录处理过程中的异常
        /// 仅在PurePC模式下执行
        /// </summary>
        private void OnMachineResetTriggered()
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            try
            {
                // 检查安全门锁状态（如未锁定则报警）
                if (Controller.Instance?._isSafetyDoorStatusMonitored == true)
                {
                    var process = new IOProcess("IO|输出|前安全门上锁|1|5", "隐藏");
                    process.Run();
                }
                if (Controller.Instance.IsMachineInternalStoMonitored == false)
                {
                    ResetStoAndCheckAxisEnable();

                    var process = new IOProcess("IO|输出|前安全门上锁|1|5", "隐藏");
                    process.Run();
                }

                // 复位安全继电器（脉冲信号：先通后断）
                ExecuteIoPulse("IO|输出|安全继电器复位", 1, 0);
                ExecuteIoPulse("IO|输出|前焊接机启动", 0, 0);
                ExecuteIoPulse("IO|输出|后焊接机启动", 0, 0);
                ExecuteIoPulse("IO|输出|前焊接机复位", 1, 0);
                ExecuteIoPulse("IO|输出|后焊接机复位", 1, 0);

                ExecuteIoPulse("IO|输出|前覆膜机正转", 0, 0);
                ExecuteIoPulse("IO|输出|后覆膜机正转", 0, 0);
                ExecuteIoPulse("IO|输出|前覆膜机反转", 0, 0);
                ExecuteIoPulse("IO|输出|后覆膜机反转", 0, 0);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// STO复位处理方法
        /// 复位后检查轴使能状态，未使能的轴重新上使能
        /// 仅在PurePC模式下执行
        /// </summary>
        public void ResetStoAndCheckAxisEnable()
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            try
            {
                // 1. 执行STO复位操作
                // 这里根据实际硬件情况实现STO复位逻辑
                PerformStoReset();

                // 3. 检查所有轴的使能状态，未使能的重新上使能
                CheckAndEnableAllAxes();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 执行STO复位的具体操作
        /// 根据实际硬件接口实现
        /// 仅在PurePC模式下执行
        /// </summary>
        private void PerformStoReset()
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            // 实际STO复位逻辑，例如：
            // MotionSystem.Instance.ResetSto();
        }

        /// <summary>
        /// 检查所有轴的使能状态，对未使能的轴重新上使能
        /// 仅在PurePC模式下执行
        /// </summary>
        private void CheckAndEnableAllAxes()
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            // 缓存MotionSystem实例，减少重复调用
            var motionSystem = MotionSystem.Instance;
            var motionAxes = motionSystem.MotionAxes;
            if (motionAxes == null || !motionAxes.Any()) return;

            var motionController = motionSystem.MotionController;

            // 用于收集所有报警复位成功的轴名称
            List<string> resetAxisNames = new List<string>();
            // 用于收集所有重新使能成功的轴名称
            List<string> enabledAxisNames = new List<string>();

            foreach (var axis in motionAxes)
            {
                try
                {
                    // 检查轴是否已使能
                    if (MotionSystem.Instance.MotionController.ServoRead(axis) == 0)
                    {
                        bool isAlarmReset = false;

                        // 如果轴未使能，检查是否存在报警
                        if (MotionSystem.Instance.MotionController.CheckServoAlarm(axis))
                        {
                            // 执行报警复位操作
                            MotionSystem.Instance.MotionController.ERCSet(axis, 1);
                            Thread.Sleep(100); // 等待复位生效
                            MotionSystem.Instance.MotionController.ERCSet(axis, 0);

                            // 将轴名称添加到复位列表
                            resetAxisNames.Add(axis.轴名称);
                            isAlarmReset = true;
                        }

                        // 重新上使能
                        MotionSystem.Instance.MotionController.ServoSet(axis, 1);

                        // 将轴名称添加到使能列表
                        // 即使之前没有报警，只要执行了使能操作，也记录
                        enabledAxisNames.Add(axis.轴名称);
                    }
                }
                catch (Exception ex)
                {
                    // 异常日志单独记录，便于定位问题
                    RLog.SaveError(ex, $"轴 {axis.轴名称} 操作失败: {ex.Message}");
                }
            }

            // 统一打印所有报警复位的日志
            if (resetAxisNames.Count > 0)
            {
                string axisNames = string.Join("、", resetAxisNames);
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue,
                    $"报警已复位的轴：{axisNames}");
            }

            // 统一打印所有重新使能的日志
            if (enabledAxisNames.Count > 0)
            {
                string axisNames = string.Join("、", enabledAxisNames);
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue,
                    $"已重新上使能的轴：{axisNames}");
            }
        }


        /// <summary>
        /// 执行IO脉冲信号（辅助方法，减少重复代码）
        /// 脉冲逻辑：先输出导通值，延迟指定时间（确保硬件响应），再输出关闭值
        /// 仅在PurePC模式下执行
        /// </summary>
        /// <param name="ioPath">IO路径（标识具体IO点）</param>
        /// <param name="onValue">导通值（IO点激活值）</param>
        /// <param name="offValue">关闭值（IO点关闭值）</param>
        private void ExecuteIoPulse(string ioPath, int onValue, int offValue)
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            ExecuteIoProcess($"{ioPath}|{onValue}|5", "隐藏");
            Thread.Sleep(IoSignalDelay); // 延时确保信号生效
            ExecuteIoProcess($"{ioPath}|{offValue}|5", "隐藏");
        }

        /// <summary>
        /// 执行IO操作（辅助方法，封装重复逻辑）
        /// 创建IOProcess实例并执行，包含异常捕获
        /// 仅在PurePC模式下执行
        /// </summary>
        private void ExecuteIoProcess(string parameters, string name)
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            try
            {
                var ioProcess = new IOProcess(parameters, name);
                ioProcess.Run();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 机台STO监控（安全扭矩关闭）
        private bool _isMachineInternalStoMonitored;
        /// <summary>
        /// 机台内部STO监控状态
        /// STO（Safe Torque Off）：安全扭矩关闭，使电机失能
        /// 触发逻辑：
        /// - true→false：STO激活，记录触发时位置并缓存
        /// - false→true：STO复位，记录当前位置并与触发时位置比对
        /// 仅在PurePC模式下生效
        /// </summary>
        public bool IsMachineInternalStoMonitored
        {
            get => _isMachineInternalStoMonitored;
            set
            {
                if (!IsPurePCMode())
                {
                    _isMachineInternalStoMonitored = value;
                    return;
                }

                bool oldValue = _isMachineInternalStoMonitored;
                _isMachineInternalStoMonitored = value;

                // STO激活（false→true）：记录触发时位置并缓存
                if (!oldValue && value)
                {
                    RecordAxisPositionsOnStoTrigger("内部STO");
                }
                // STO复位（true→false）：记录复位后位置并比对
                else if (oldValue && !value)
                {
                    RecordAndCompareAxisPositionsOnStoReset("内部STO");
                }
            }
        }

        // 上料STO状态（支持触发记录和复位比对）
        private bool _isMachine上料StoMonitored;
        public bool IsMachine上料StoMonitored
        {
            get => _isMachine上料StoMonitored;
            set
            {
                if (!IsPurePCMode())
                {
                    _isMachine上料StoMonitored = value;
                    return;
                }

                _isMachine上料StoMonitored = value;
            }
        }

        // 下料STO状态（支持触发记录和复位比对）
        private bool _isMachine下料StoMonitored;
        public bool IsMachine下料StoMonitored
        {
            get => _isMachine下料StoMonitored;
            set
            {
                if (!IsPurePCMode())
                {
                    _isMachine下料StoMonitored = value;
                    return;
                }

                _isMachine下料StoMonitored = value;
            }
        }

        /// <summary>
        /// STO激活时记录位置并缓存（用于后续复位时比对）
        /// </summary>
        /// <param name="stoType">STO类型（内部/上料/下料）</param>
        private void RecordAxisPositionsOnStoTrigger(string stoType)
        {
            if (!IsPurePCMode())
                return;

            try
            {
                DateTime triggerTime = DateTime.Now;
                string eventId = $"{stoType}_{triggerTime:yyyyMMddHHmmssfff}"; // 毫秒级唯一标识
                string logPrefix = $"{triggerTime:yyyy-MM-dd HH:mm:ss} | {stoType}激活 | ";

                // 1. 获取所有轴位置
                int[] allAxisIndices = Enumerable.Range(0, 16).ToArray();
                Dictionary<int, double> triggerPositions = AllAxisPos.GetMultipleEncPos(allAxisIndices);

                // 2. 缓存触发时的位置（用唯一ID关联）
                _stoEventCache[eventId] = new StoEventData
                {
                    StoType = stoType,
                    TriggerTime = triggerTime,
                    TriggerPositions = triggerPositions,
                    IsReset = false // 标记未复位
                };

                // 3. 记录触发日志
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, $"{logPrefix}触发，位置已缓存（事件ID：{eventId}）");
                foreach (var kvp in triggerPositions)
                {
                    string axisName = AllAxisPos.GetAxisName(kvp.Key);
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red,
                        $"{logPrefix}{axisName}触发位置：{kvp.Value:F2}");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                RLog.SaveLog(LogType.Operation, LogLevel.Error, Color.DarkRed, $"{stoType}激活时记录位置失败：{ex.Message}");
            }
        }

        /// <summary>
        /// STO复位时记录当前位置，并与触发时的位置比对
        /// </summary>
        /// <param name="stoType">STO类型（内部/上料/下料）</param>
        private void RecordAndCompareAxisPositionsOnStoReset(string stoType)
        {
            if (!IsPurePCMode())
                return;

            try
            {
                switch (MenuProxy.Instance.MachineRunModel)
                {
                    case MachineMode.Manual:
                        break;
                    case MachineMode.Auto:
                        // 为了代码更清晰，可以先将状态取出来
                        var cachedRunStatus = Controller.Instance.StationInfo.CachedMachineRunStatus;

                        // 条件判断：
                        // 1. 缓存的机台运行状态是 运行中、回零中 或 回零成功。
                        // 2. 并且，当前报警是由STO引起的。
                        if ((cachedRunStatus == MachineRunStatus.Running ||
                             cachedRunStatus == MachineRunStatus.Homing ||
                             cachedRunStatus == MachineRunStatus.HomeSuccess) &&
                            _isAlarmCausedBySTO)
                        {
                            Controller.Instance.IsMachineResetMonitored = true;
                        }
                        break;
                }

                DateTime resetTime = DateTime.Now;
                string logPrefix = $"{resetTime:yyyy-MM-dd HH:mm:ss} | {stoType}复位 | ";

                // 1. 查找最近一次未复位的STO触发事件（按时间倒序）
                var lastTriggerEvent = _stoEventCache.Values
                    .Where(data => data.StoType == stoType && !data.IsReset)
                    .OrderByDescending(data => data.TriggerTime)
                    .FirstOrDefault();

                if (lastTriggerEvent == null)
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"{logPrefix}未找到对应的触发记录，无法比对位置");
                    return;
                }

                // 2. 获取复位时的当前轴位置
                int[] allAxisIndices = Enumerable.Range(0, 16).ToArray();
                Dictionary<int, double> resetPositions = AllAxisPos.GetMultipleEncPos(allAxisIndices);

                // 3. 计算并记录位置差值
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green, $"{logPrefix}开始比对位置（触发时间：{lastTriggerEvent.TriggerTime:HH:mm:ss}）");
                foreach (var axisIndex in allAxisIndices)
                {
                    string axisName = AllAxisPos.GetAxisName(axisIndex);
                    double triggerPos = lastTriggerEvent.TriggerPositions[axisIndex];
                    double resetPos = resetPositions[axisIndex];
                    double diff = resetPos - triggerPos; // 差值（复位位置 - 触发位置）

                    // 记录差值（绝对值>0.001时才记录，忽略微小波动）
                    if (Math.Abs(diff) > 0.001)
                    {
                        RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green,
                            $"{logPrefix}{axisName}位置变化：触发{triggerPos:F2} → 复位{resetPos:F2}，差值：{diff:F2}");
                    }
                }

                // 4. 标记事件已复位，避免重复比对
                lastTriggerEvent.IsReset = true;
                lastTriggerEvent.ResetTime = resetTime;
                lastTriggerEvent.ResetPositions = resetPositions;

                // 5. 可选：清理过期缓存（保留最近100条事件）
                CleanupExpiredStoEvents(100);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                RLog.SaveLog(LogType.Operation, LogLevel.Error, Color.DarkRed, $"{stoType}复位时比对位置失败：{ex.Message}");
            }
        }

        #region STO事件缓存相关
        /// <summary>
        /// STO事件数据结构（存储触发和复位的完整信息）
        /// </summary>
        private class StoEventData
        {
            public string StoType { get; set; } // STO类型
            public DateTime TriggerTime { get; set; } // 触发时间
            public Dictionary<int, double> TriggerPositions { get; set; } // 触发时位置
            public bool IsReset { get; set; } // 是否已复位
            public DateTime? ResetTime { get; set; } // 复位时间（可为空）
            public Dictionary<int, double> ResetPositions { get; set; } // 复位时位置
        }

        /// <summary>
        /// STO事件缓存（键：唯一事件ID，值：事件详情）
        /// 用于关联触发和复位的位置数据
        /// </summary>
        private readonly Dictionary<string, StoEventData> _stoEventCache = new Dictionary<string, StoEventData>();

        /// <summary>
        /// 清理过期的STO事件缓存（只保留最近N条）
        /// </summary>
        /// <param name="maxKeepCount">最大保留数量</param>
        private void CleanupExpiredStoEvents(int maxKeepCount)
        {
            if (_stoEventCache.Count <= maxKeepCount)
                return;

            // 按触发时间正序排序，删除最早的事件
            var eventsToRemove = _stoEventCache.Values
                .OrderBy(data => data.TriggerTime)
                .Take(_stoEventCache.Count - maxKeepCount)
                .Select(data => $"{data.StoType}_{data.TriggerTime:yyyyMMddHHmmssfff}")
                .ToList();

            foreach (var eventId in eventsToRemove)
            {
                _stoEventCache.Remove(eventId);
            }
            RLog.SaveLog(LogType.Operation, LogLevel.Debug, Color.Gray, $"STO事件缓存已清理，保留最近{maxKeepCount}条");
        }
        #endregion

        /// <summary>
        /// 对外提供STO事件记录查询（可选）
        /// </summary>
        private List<StoEventData> GetStoEventHistory()
        {
            return _stoEventCache.Values.ToList();
        }
        #endregion

        #region 安全门锁开关监控
        private bool _isSafetyDoorLockMonitored;
        /// <summary>
        /// 安全门锁开关监控状态
        /// 触发逻辑：状态变化时（true↔false）控制安全门指示灯
        /// 处理内容：锁定/解锁时切换指示灯状态（当前逻辑隐藏）
        /// 仅在PurePC模式下生效
        /// </summary>
        public bool IsSafetyDoorLockMonitored
        {
            get => _isSafetyDoorLockMonitored;
            set
            {
                // 非PurePC模式下不处理状态变化
                if (!IsPurePCMode())
                {
                    _isSafetyDoorLockMonitored = value;
                    return;
                }

                if (_isSafetyDoorLockMonitored != value)
                {
                    OnSafetyDoorLockChanged();
                }
                _isSafetyDoorLockMonitored = value;
            }
        }

        /// <summary>
        /// 安全门锁状态变化时的处理逻辑
        /// 核心操作：控制安全门锁定/解锁指示灯（当前逻辑隐藏）
        /// 异常处理：记录处理过程中的异常
        /// 仅在PurePC模式下执行
        /// </summary>
        private void OnSafetyDoorLockChanged()
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            try
            {
                var process = new IOProcess(
                     Controller.Instance.IsSafetyDoorLockMonitored ? "IO|输出|后安全门上锁|0|5" : "IO|输出|后安全门上锁|1|5", "隐藏");
                process.Run();

                process = new IOProcess(
    Controller.Instance.IsSafetyDoorLockMonitored ? "IO|输出|前安全门上锁|0|5" : "IO|输出|前安全门上锁|1|5", "隐藏");
                process.Run();

            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 安全门状态监控
        public bool 前站安全门状态监控;

        public bool 后站安全门状态监控;



        private bool _isSafetyDoorStatusMonitored;
        /// <summary>
        /// 安全门状态监控状态
        /// 触发逻辑：门未关闭时（状态从true→false）触发暂停监控
        /// 安全机制：门打开时强制暂停设备，确保人员安全
        /// 仅在PurePC模式下生效
        /// </summary>
        public bool IsSafetyDoorStatusMonitored
        {
            get => _isSafetyDoorStatusMonitored;
            set
            {
                // 非PurePC模式下不处理状态变化
                if (!IsPurePCMode())
                {
                    _isSafetyDoorStatusMonitored = value;
                    return;
                }

                if (_isSafetyDoorStatusMonitored && !value)
                {
                    Controller.Instance.IsMachinePauseMonitored = false; // 门打开时触发暂停
                }

                // 状态发生变化时才处理
                if (_isSafetyDoorStatusMonitored != value)
                {
                    // 安全门状态时给前后站PLC寄存器赋值
                    short 赋值 = (short)(value ? 1 : 0);
                    try
                    {
                        // 向前站PLC寄存器写入值
                        new ModbusPLCCommunicationProcess("ModbusPLC|输出|PLC|给前站安全门状态|" + 赋值 + "|5", "").Run();
                        // 向后站PLC寄存器写入值
                        new ModbusPLCCommunicationProcess("ModbusPLC|输出|PLC|给后站安全门状态|" + 赋值 + "|5", "").Run();

                        // 记录状态变化日志
                        RLog.SaveLog(LogType.Operation, LogLevel.Info,
                            value ? Color.Blue : Color.Green,
                            $"本站{(value ? "安全门关闭" : "安全门打开")}，已同步更新前后站PLC寄存器");
                    }
                    catch (Exception ex)
                    {
                        RLog.SaveLog(LogType.Operation, LogLevel.Error, Color.Red,
                            $"更新前后站PLC寄存器状态失败: {ex.Message}");
                    }
                }

                _isSafetyDoorStatusMonitored = value;


            }
        }
        #endregion

        #region 机台上下站状态管理及启动监控触发逻辑
        // 定义魔法数字常量，说明业务含义（假设64代表“机台Running状态”）
        private const int StationReadyStatus = 64;

        /// <summary>
        /// 一个布尔标志，用于标识当前机台的报警状态是否是由STO (Safe Torque Off, 安全转矩关闭) 事件直接触发的。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 当机台因STO条件（如急停按钮按下、安全门打开等）而触发报警时，此标志应被设置为 <c>true</c>。
        /// 这有助于系统在后续的报警处理逻辑中，区分STO相关报警和其他类型的报警（如传感器故障、程序错误等），
        /// 并执行针对性的处理或恢复操作。
        /// </para>
        /// <para>
        /// 例如，在STO导致的报警解除后，系统可能需要参考此标志来决定是否需要恢复到STO发生前的运行状态。
        /// 当报警被清除或检测到非STO原因的新报警时，此标志应被重置为 <c>false</c>。
        /// </para>
        /// </remarks>
        private bool _isAlarmCausedBySTO;

        // 私有字段（封装，仅内部访问）
        private int _previousStationStatus = 0; // 上一站机台状态
        private int _nextStationStatus = 0;     // 下一站机台状态

        /// <summary>
        /// 上一站机台状态（属性，控制读写逻辑）
        /// </summary>
        public int PreviousStationStatus
        {
            get => _previousStationStatus;
            set
            {
                // 非PurePC模式：直接更新状态，不触发额外逻辑
                if (!IsPurePCMode())
                {
                    _previousStationStatus = value;
                    return;
                }

                // 先更新状态，再检查是否满足“启动监控”条件（逻辑更直观）
                _previousStationStatus = value;
                CheckStartMonitorCondition();
            }
        }

        /// <summary>
        /// 下一站机台状态（属性，控制读写逻辑）
        /// </summary>
        public int NextStationStatus
        {
            get => _nextStationStatus;
            set
            {
                // 非PurePC模式：直接更新状态，不触发额外逻辑
                if (!IsPurePCMode())
                {
                    _nextStationStatus = value;
                    return;
                }

                // 先更新状态，再检查是否满足“启动监控”条件
                _nextStationStatus = value;
                CheckStartMonitorCondition();
            }
        }

        /// <summary>
        /// 提取重复逻辑：检查是否满足“启动机台监控”的条件
        /// 条件：上下站均为就绪状态（64） + 报警由STO引起
        /// </summary>
        private void CheckStartMonitorCondition()
        {
            // 短路逻辑优化：
            // 1. 首先检查是否由STO引起的报警，这是整个条件成立的前提。
            // 2. 如果是，再检查上一站或下一站是否处于就绪状态。
            // 这样写逻辑更清晰，并且避免了对_isAlarmCausedBySTO的重复判断。
            if (_isAlarmCausedBySTO && _previousStationStatus == StationReadyStatus && _nextStationStatus == StationReadyStatus)
            {
                Controller.Instance.IsMachineStartMonitored = true;
                _isAlarmCausedBySTO = false; // 触发后重置标志，防止重复触发
            }
        }
        #endregion

        #region 自定义信号
        private bool _烟雾报警;

        public bool 烟雾报警
        {
            get { return _烟雾报警; }
            set
            {
                if (_烟雾报警 != value && !_烟雾报警)
                {
                    Project.Instance.GlobalVariables.AlarmMessage = "烟雾报警！！！";
                    StatusUpdater.TriggerWarning(WarningColor.Orange);
                    //RLog.SaveLog(LogType.Operation,LogLevel.Info, Color.Red, "烟雾报警！！！");
                    //Frm_Main.Instance.toolStripLabel_日志显示.PerformClick();
                    //Controller.Instance.IsMachinePauseMonitored = true;
                };
                _烟雾报警 = value;
            }
        }




        private DateTime? _filmLackAlarmStartTime = null;

        private bool _覆膜机缺料报警;

        public bool 覆膜机缺料报警
        {
            get { return _覆膜机缺料报警; }
            set
            {

                if (value == true)
                {
                    if (!_filmLackAlarmStartTime.HasValue)
                    {
                        // 首次检测到有效，记录开始时间
                        _filmLackAlarmStartTime = DateTime.Now;
                    }
                    else
                    {
                        // 计算信号已持续有效时间
                        TimeSpan duration = DateTime.Now - _filmLackAlarmStartTime.Value;
                        if (duration.TotalSeconds >= 3)
                        {
                            // 持续1秒以上，触发报警
                            //Controller.Instance.覆膜机缺料报警 = true;

                            //RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "覆膜机缺料报警！！！");
                            //Frm_Main.Instance.toolStripLabel_日志显示.PerformClick();
                            Project.Instance.GlobalVariables.AlarmMessage = "覆膜机缺料报警！！！";
                            StatusUpdater.TriggerWarning(WarningColor.Orange);
                            //Controller.Instance.IsMachinePauseMonitored = true;
                        }
                    }
                }
                else
                {
                    _filmLackAlarmStartTime = null;
                }

                //if (_覆膜机缺料报警!=value && !_覆膜机缺料报警)
                //{
                //    RLog.SaveLog(LogType.Operation,LogLevel.Info, Color.Red, "覆膜机缺料报警！！！");
                //    //Frm_Main.Instance.toolStripLabel_日志显示.PerformClick();
                //    Controller.Instance.IsMachinePauseMonitored = true;
                //}
                //_覆膜机缺料报警= value;
            }
        }

        private bool _超声波焊接报警;

        public bool 超声波焊接报警
        {
            get { return _超声波焊接报警; }
            set
            {
                if (_超声波焊接报警 != value && !_超声波焊接报警)
                {
                    //RLog.SaveLog(LogType.Operation,LogLevel.Info, Color.Red, "超声波焊接报警！！！");
                    //Frm_Main.Instance.toolStripLabel_日志显示.PerformClick();
                    //Controller.Instance.IsMachinePauseMonitored = true;
                    Project.Instance.GlobalVariables.AlarmMessage = "超声波焊接报警！！！";
                    StatusUpdater.TriggerWarning(WarningColor.Orange);
                }
                _超声波焊接报警 = value;
            }
        }

        private int _飞拍功能选择;

        public int 飞拍功能选择
        {
            get { return _飞拍功能选择; }
            set { _飞拍功能选择 = value; }
        }


        private int _In飞拍功能选择;

        public int In飞拍功能选择
        {
            get { return _In飞拍功能选择; }
            set { _In飞拍功能选择 = value; }
        }








        private int _屏蔽滑台信号;

        public int 是否屏蔽滑台
        {
            get { return _屏蔽滑台信号; }
            set { _屏蔽滑台信号 = value; }
        }


        private int _前滑台呼叫放料;

        public int 前滑台呼叫放料
        {
            get { return _前滑台呼叫放料; }
            set { _前滑台呼叫放料 = value; }
        }


        private int _后滑台呼叫放料;

        public int 后滑台呼叫放料
        {
            get { return _后滑台呼叫放料; }
            set { _后滑台呼叫放料 = value; }
        }


        public int 换治具安全 { get; set; }

        public bool 前焊接机无焊接中信号报警 { get; set; }


        public bool 后焊接机无焊接中信号报警 { get; set; }

        private bool _上料缺料信号;
        public bool 上料缺料信号
        {
            get => _上料缺料信号;
            set
            {
                if (_上料缺料信号 != value && !_上料缺料信号)
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, "上料工位缺料，请及时补充！！！");
                    Frm_Main.Instance.toolStripLabel_日志显示.PerformClick();
                }
                // 状态修正逻辑（确保状态一致性，避免重复报警）
                _上料缺料信号 = value;
            }
        }
        #endregion

        #region 通信处理（TCP/串口）
        #region TCPServer 绑定事件
        #region Server1
        /// <summary>
        /// Server1 TCP服务器接收数据事件处理<br/>
        /// 处理逻辑：
        /// 1. 延时50ms确保数据完整接收
        /// 2. 解析ASCII编码的数据并按协议分割
        /// 3. 记录接收日志
        /// </summary>
        public void _Server1_TCPServerDataArrived(byte[] value, int Len, string IPAddress, string Port)
        {
            Thread.Sleep(50); // 延时确保数据完整
            string str = System.Text.ASCIIEncoding.ASCII.GetString(value);
            str = str.Trim();
            string[] var = str.Split('^'); // 按协议分割数据
            ProcessLog.Instance.SaveLog("TCPIP交握", Color.Black, "Server1接收", str, "");
        }

        /// <summary>
        /// Server1 TCP服务器异常事件处理<br/>
        /// 预留异常处理逻辑，可根据实际需求添加错误恢复或报警
        /// </summary>
        public void _Server1_TCPServerException(Exception ex)
        {
            // 异常处理逻辑（预留）
        }

        /// <summary>
        /// Server1 TCP服务器客户端断开事件处理<br/>
        /// 处理逻辑：关闭指定IP和端口的客户端连接，释放资源
        /// </summary>
        public void _Server1_TCPServerClientClose(string IP, string Port)
        {
            if (TcpIpSystem.Instance.TcpServers.Count > 0)
            {
                foreach (var item in TcpIpSystem.Instance.TcpServers)
                {
                    if (item.Name == "Server1")
                    {
                        item.CloseClient(IP, Port); // 关闭指定IP:Port的客户端
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Server1 TCP服务器客户端连接事件处理<br/>
        /// 预留连接成功处理逻辑，如记录连接日志
        /// </summary>
        public void _Server1_TCPServerClientConnected(string IP, string Port)
        {
            // 连接成功处理逻辑（预留）
        }
        #endregion

        #region Epson_1
        /// <summary>
        /// Epson_1 TCP服务器接收数据事件处理<br/>
        /// 处理逻辑：
        /// 1. 延时50ms确保数据完整接收
        /// 2. 解析Epson机器人发送的ASCII数据（如计时结束指令）
        /// 3. 记录接收日志并按协议分割指令
        /// </summary>
        public void _Epson_1_TCPServerDataArrived(byte[] value, int Len, string IPAddress, string Port)
        {
            Thread.Sleep(50);
            string str = System.Text.ASCIIEncoding.ASCII.GetString(value);
            ProcessLog.Instance.SaveLog("TCPIP交握", Color.Black, "Epson发送 ", str, "");
            str = str.Trim();
            string[] cmd = str.Replace("\r\n", "").Split('^'); // 去除换行符并分割指令
            string xuewei = cmd[1]; // 工位信息（示例）
            switch (cmd[0])
            {
                case "jieshujishi":
                    // 处理计时结束指令（预留）
                    break;
            }
        }

        /// <summary>
        /// Epson_1 TCP服务器异常事件处理<br/>
        /// 预留异常处理逻辑
        /// </summary>
        public void _Epson_1_TCPServerException(Exception ex)
        {
            // 异常处理逻辑（预留）
        }

        /// <summary>
        /// Epson_1 TCP服务器客户端断开事件处理<br/>
        /// 处理逻辑：关闭客户端连接并记录断开日志
        /// </summary>
        public void _Epson_1_TCPServerClientClose(string IP, string Port)
        {
            if (TcpIpSystem.Instance.TcpServers.Count > 0)
            {
                foreach (var item in TcpIpSystem.Instance.TcpServers)
                {
                    if (item.Name == "Epson_1")
                    {
                        item.CloseClient(IP, Port);
                        ProcessLog.Instance.SaveLog("TCPIP交握", Color.Red, " ", "下相机服务器已断开！！！！ ", "");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Epson_1 TCP服务器客户端连接事件处理<br/>
        /// 处理逻辑：记录连接成功日志
        /// </summary>
        public void _Epson_1_TCPServerClientConnected(string IP, string Port)
        {
            ProcessLog.Instance.SaveLog("TCPIP交握", Color.Black, " ", "下相机服务器已连接！！！！ ", "");
        }
        #endregion

        /// <summary>
        /// 通过TCP服务器发送数据<br/>
        /// 功能：向指定名称的TCP服务器的客户端发送数据并记录日志<br/>
        /// 注意：仅当服务器存在且有连接的客户端时发送
        /// </summary>
        /// <param name="name">TCP服务器名称（如"Server1"）</param>
        /// <param name="value">发送的数据内容</param>
        public void TCPServerSendData(string name, string value)
        {
            if (TcpIpSystem.Instance.TcpServers.Count > 0)
            {
                foreach (var item in TcpIpSystem.Instance.TcpServers)
                {
                    if (item.Name == name && item._clients.Count > 0)
                    {
                        item.SendData(value); // 发送数据
                        ProcessLog.Instance.SaveLog("TCPIP交握", Color.Black, "向" + name + "发送", value, "");
                        break;
                    }
                }
            }
        }
        #endregion

        #region TCPClient 绑定事件（Client1）
        /// <summary>
        /// Client1 TCP客户端接收数据事件处理<br/>
        /// 预留客户端接收数据逻辑
        /// </summary>
        public void _Client1_TCPClientDataArrived(byte[] value, int Len)
        {
            // 客户端接收数据逻辑（预留）
        }

        /// <summary>
        /// Client1 TCP客户端异常事件处理<br/>
        /// 预留异常处理逻辑
        /// </summary>
        public void _Client1_TCPClientException(Exception ex)
        {
            // 异常处理逻辑（预留）
        }

        /// <summary>
        /// Client1 TCP客户端断开事件处理<br/>
        /// 预留断开处理逻辑
        /// </summary>
        public void _Client1_TCPClientClientClose()
        {
            // 断开处理逻辑（预留）
        }

        /// <summary>
        /// Client1 TCP客户端连接事件处理<br/>
        /// 预留连接成功处理逻辑
        /// </summary>
        public void _Client1_TCPClientClientConnected()
        {
            // 连接成功处理逻辑（预留）
        }
        #endregion

        #region SerialPort通讯交握
        /// <summary>
        /// 串口接收锁<br/>
        /// 确保串口接收数据时的线程安全，避免多线程同时处理接收数据导致异常
        /// </summary>
        private static readonly object objReceivedSerialport = new object();
        /// <summary>
        /// 串口发送锁<br/>
        /// 确保串口发送数据时的线程安全，避免多线程同时发送导致数据混乱
        /// </summary>
        private static readonly object objSendSerialport = new object();

        /// <summary>
        /// 串口接收数据事件处理<br/>
        /// 处理逻辑：
        /// 1. 使用线程锁确保接收数据的线程安全
        /// 2. 按协议分割数据（#和/为分隔符）
        /// 3. 根据串口名称处理数据（示例：COM7和COM9分别对应不同产品）
        /// 4. 发送清除指令，记录接收日志
        /// </summary>
        /// <param name="ItemName">串口设备名称（如COM7）</param>
        /// <param name="ItemValue">接收的数据</param>
        public void ReceivedSerialPortStr_DataChanged(string ItemName, string ItemValue)
        {
            try
            {
                lock (objReceivedSerialport) // 线程安全锁，避免多线程冲突
                {
                    SerialportSystem.Instance.IsDataFormatValid = false;

                    // 按协议分割数据（#和/为分隔符）
                    string[] array_全 = ItemValue.Split(new char[] { '#', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    string result = array_全.Length > 0 ? array_全[0] : ""; // 解析结果（如OK/NG）
                    string ReceivedStr = "无效的数据！！！！";

                    // 根据串口名称处理（示例：COM7和COM9分别对应不同产品）
                    // switch (ItemName)
                    // {
                    //     case "COM7": //5号产品
                    //         ReceivedStr = " 通讯设备:" + ItemName + " 指令内容:" + ItemValue;
                    //         RLog.SaveLog(LogType.磁通量, Color.Orange, "COM7接收数据:," + ItemValue);
                    //         // 发送清除指令
                    //         SerialportSystem.Instance.L_SerialPort[0].SendData("DATAC>", SerialPortReadWrite.SendFormat.String);
                    //         break;
                    //     case "COM9":  //1234产品
                    //         ReceivedStr = " 通讯设备:" + ItemName + " 指令内容:" + ItemValue;
                    //         RLog.SaveLog(LogType.磁通量, Color.Orange, "COM9接收数据:," + ItemValue);
                    //         // 发送清除指令
                    //         SerialportSystem.Instance.L_SerialPort[1].SendData("STOP>", SerialPortReadWrite.SendFormat.String);
                    //         break;
                    // }

                    SerialportSystem.Instance.IsDataFormatValid = true;
                    ProcessLog.Instance.SaveLog("TCPIP交握", Color.Black, "接收数据", ReceivedStr, "");
                }
            }
            catch (Exception ex)
            {
                SerialportSystem.Instance.IsDataFormatValid = false;
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 串口发送数据处理<br/>
        /// 处理逻辑：
        /// 1. 使用线程锁确保发送数据的线程安全
        /// 2. 查找目标串口并发送数据（仅在串口打开时发送）
        /// </summary>
        /// <param name="ItemName">串口名称（如COM7）</param>
        /// <param name="ItemValue">发送的数据</param>
        public void SendStrSerialport_DataChanged(string ItemName, string ItemValue)
        {
            try
            {
                lock (objSendSerialport) // 线程安全锁，避免多线程冲突
                {
                    SerialportSystem.Instance.IsDataFormatValid = false;
                    if (SerialportSystem.Instance.SerialPorts != null && SerialportSystem.Instance.SerialPorts.Count > 0)
                    {
                        foreach (var item in SerialportSystem.Instance.SerialPorts)
                        {
                            // 找到目标串口并发送数据（仅在串口打开时）
                            if (ItemName == item.PortName && item.IsOpen)
                            {
                                item.SendData(ItemValue, SerialPortCommunicator.SendFormat.String);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion
        #endregion
        private void IniSign()
        {
            //public ConcurrentQueue<double> XYR ;


            StationInfo.EPSON位置请求 = 0;
            StationInfo.上相机是否需要拍照 = 0;
            StationInfo.Epson2下相机请求拍照 = 0;
            StationInfo.Epson2下相机请求位置号 = 0;
            StationInfo.Epson2下相机是否刷新 = true;
            StationInfo.Epson2皮带线请求结果 = 0;
            StationInfo.左皮带线是否刷新 = true;

            StationInfo.皮带线允许组装 = 0;

            StationInfo.供料器震动 = 0;
            #region 清除  XYR
            while (StationInfo.Epson2X.TryDequeue(out double xyr))
            {

            }


            while (StationInfo.Epson2Y.TryDequeue(out double xyr))
            {

            }


            while (StationInfo.Epson2R.TryDequeue(out double xyr))
            {

            }

            while (StationInfo.Epson1X.TryDequeue(out double xyr))
            {

            }

            while (StationInfo.Epson1Y.TryDequeue(out double xyr))
            {

            }

            while (StationInfo.Epson1R.TryDequeue(out double xyr))
            {

            }
            #endregion




            StationInfo.Epson1下相机请求拍照 = 0;
            StationInfo.Epson1下相机请求位置号 = 0;
            StationInfo.Epson1下相机是否刷新 = true;
            StationInfo.Epson1皮带线请求结果 = 0;

            StationInfo.右皮带线是否刷新 = true;
        }
    }

    /// <summary>
    /// 菜单配置类<br/>
    /// 存储不同运行状态下的菜单按钮状态（启用/禁用）<br/>
    /// 与MachineRunStatus配合使用，控制UI界面按钮的可操作性
    /// </summary>
    public class MenuConfig
    {
        /// <summary>
        /// 暂停状态标识（0：未暂停；1：已暂停）
        /// </summary>
        public int PauseStatus { get; set; }
        /// <summary>
        /// 启动按钮是否启用
        /// </summary>
        public bool EnableStart { get; set; }
        /// <summary>
        /// 暂停按钮是否启用
        /// </summary>
        public bool EnablePause { get; set; }
        /// <summary>
        /// 停止按钮是否启用
        /// </summary>
        public bool EnableStop { get; set; }
        /// <summary>
        /// 初始化按钮是否启用
        /// </summary>
        public bool EnableInit { get; set; }
        /// <summary>
        /// 复位按钮是否启用
        /// </summary>
        public bool EnableReset { get; set; }
        /// <summary>
        /// 运行模式切换是否启用
        /// </summary>
        public bool EnableMachineRunType { get; set; }
    }



    /// <summary>
    /// 工位信息类<br/>
    /// 继承自JStationInfo，存储设备运行过程中的状态数据<br/>
    /// 包含：传感器状态、流程模式、机器人状态、缓存状态等核心信息
    /// </summary>
    public class StationInfo : JStationInfo
    {
        /// <summary>
        /// 工位ID（如"S2270Trim"），用于标识当前工位
        /// </summary>
        public string StationId { get; set; } = "OP070";

        /// <summary>
        /// 最近一次 JVS 检测结果显示文本（OK/ERR + 点位 + 点数 + X,Y,R;…，供主界面显示/追溯）
        /// </summary>
        public string JvsLastResultDisplay { get; set; } = "";
        /// <summary>
        /// 缓存的设备运行状态（用于暂停后恢复到正确状态）
        /// </summary>
        public MachineRunStatus CachedMachineRunStatus { get; set; } = MachineRunStatus.None;
        /// <summary>
        /// 输入IO状态保持数组（记录各IO点的状态，长度128）
        /// </summary>
        public IOStateKeep[] DIs { get; set; } = new IOStateKeep[128];

        /// <summary>
        /// 当前控制模式
        /// 默认为纯PC控制模式（PurePC）
        /// 可根据实际需求切换为其他控制模式（如PLC主导控制）
        /// </summary>
        public ControlMode currentMode = ControlMode.PurePC;


        public 滑台_ProcessMode Get_前滑台流程 = 滑台_ProcessMode.未指定;

        public 滑台_ProcessMode Get_后滑台流程 = 滑台_ProcessMode.未指定;

        public 搬运轴_ProcessMode Get_搬运轴流程 = 搬运轴_ProcessMode.未指定;

        public 覆膜机_ProcessMode Get_前覆膜机 = 覆膜机_ProcessMode.未指定;

        public 覆膜机_ProcessMode Get_后覆膜机 = 覆膜机_ProcessMode.未指定;

        public 搬运轴换治具_ProcessMode Get_换治具 = 搬运轴换治具_ProcessMode.未指定;

        public 手动测试_ProcessMode Get_手动测试 = 手动测试_ProcessMode.未指定;




        public EPSON2上相机_ProcessMode Get_EPSON2上相机 = EPSON2上相机_ProcessMode.未指定;


        public EPSON2下相机_ProcessMode Get_EPSON2下相机 = EPSON2下相机_ProcessMode.未指定;


        public EPSON2发送请求最终结果_ProcessMode Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.未指定;



        public EPSON1上相机_ProcessMode Get_EPSON1上相机 = EPSON1上相机_ProcessMode.未指定;


        public EPSON1下相机_ProcessMode Get_EPSON1下相机 = EPSON1下相机_ProcessMode.未指定;
        public 飞拍组装流程_ProcessMode Get_飞拍组装 = 飞拍组装流程_ProcessMode.未指定;


        public EPSON1发送请求最终结果_ProcessMode Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.未指定;

        /// <summary>
        /// 周期时间（Cycle Time）的私有存储字段
        /// 用于实现属性的变更通知逻辑
        /// </summary>
        private double _ctTime;

        /// <summary>
        /// 周期时间（CT）记录
        /// 表示完成一个生产周期所需的时间，是生产效率的重要指标
        /// 当值发生变化时，会自动更新到机器控制器
        /// </summary>
        public double CtTime
        {
            get => _ctTime;
            set
            {
                // 只有当值发生变化时才执行更新操作，减少不必要的计算和通知
                if (_ctTime != value)
                {
                    _ctTime = value;
                    // 当值变化时，通知机器控制器更新当前CT值
                    MachineController.UpdateCurrentCtValue((decimal)Controller.Instance.StationInfo.CtTime);
                }
            }
        }


        /// <summary>
        /// 机械手侧光栅状态监控（是否被遮挡，用于安全防护）
        /// </summary>
        public bool 机械手侧光栅状态监控 = false;
        /// <summary>
        /// 拍照NG强制跳过标识（忽略拍照不合格结果）
        /// </summary>
        public bool _拍照NG强制跳过 = false;
        /// <summary>
        /// 机台复位后启动监控信号（复位完成后允许启动）
        /// </summary>
        public bool 机台复位后启动 = false;
        /// <summary>
        /// 先复位再启动标识（需复位后才能启动，强制安全流程）
        /// </summary>
        public bool 先复位再启动 = false;
        /// <summary>
        /// 初始化状态标识（是否完成初始化）
        /// </summary>
        public bool _初始化状态 = false;
        /// <summary>
        /// 双手启动按钮闪烁标识（控制按钮闪烁状态）
        /// </summary>
        public bool _双手启动闪烁 = false;
        /// <summary>
        /// 双手启动按钮按下标识（检测按钮是否被按下）
        /// </summary>
        public bool _双手启动按钮按下 = false;
        /// <summary>
        /// 屏蔽蜂鸣器标识（是否关闭蜂鸣器报警）
        /// </summary>
        public bool _屏蔽蜂鸣器 = false;
        /// <summary>
        /// 屏蔽安全门标识（忽略安全门状态，仅用于特殊场景）
        /// </summary>
        public bool _屏蔽安全门 = false;
        /// <summary>
        /// 飞拍图像
        /// </summary>
        public CogImage8Grey image8Greys = new CogImage8Grey();


        /// <summary>
        /// 旋转算法
        /// </summary>
        /// <param name="normUpData">上相机标准位</param>
        /// <param name="normDownData">下相机标准位</param>
        /// <param name="upData">上相机数据</param>
        /// <param name="downData">下相机数据</param>
        /// /// <param name="offsetData">补偿数据</param>
        /// <param name="rotationCent">旋转中心</param>
        /// <returns></returns>
        public CoordinateData Calculate(CoordinateData normDownData, CoordinateData downData, CoordinateData offsetData, CoordinateData rotationCent)
        {

            CoordinateData outputData;
            outputData.X = 0;
            outputData.Y = 0;
            outputData.Rotation = 0;
            //旋转中心
            double rotationCenterX = rotationCent.X;
            double rotationCenterY = rotationCent.Y;


            //下相机标准位数据
            double downNormX = normDownData.X;
            double downNormY = normDownData.Y;
            double downNormR = normDownData.Rotation;


            //下相机数据
            double downX = downData.X;
            double downY = downData.Y;
            double downR = downData.Rotation;
            //double downR = 0;

            //补偿数据
            double offsetX = offsetData.X;
            double offsetY = offsetData.Y;
            double offsetR = offsetData.Rotation;

            //if (downNormR > 90) downNormR -= 180;
            //if (downNormR < -90) downNormR += 180;

            //if (downR > 90) downR -= 180;
            //if (downR < -90) downR += 180;

            double tempr2 = (downNormR - downR);
            double R = tempr2 + offsetR;

            R = R * Math.PI / 180;

            double tempx1 = Math.Cos(R) * (downX - rotationCenterX) - Math.Sin(R) * (downY - rotationCenterY) + rotationCenterX;
            double tempy1 = Math.Cos(R) * (downY - rotationCenterY) + Math.Sin(R) * (downX - rotationCenterX) + rotationCenterY;


            double offsetX_Down = downNormX - tempx1;
            double offsetY_Down = downNormY - tempy1;

            outputData.X = offsetX_Down + offsetX;
            outputData.Y = offsetY_Down + offsetY;
            outputData.Rotation = R * 180 / Math.PI;

            return outputData;
        }

        public void SaveImage(CogImage8Grey image8Grey, string Path, string DataTime)
        {
            Task.Factory.StartNew(new Action(() =>
            {
                if (!Directory.Exists(Project.Instance.ImagePath))
                {
                    Directory.CreateDirectory(Project.Instance.ImagePath);
                }

                string path2 = Project.Instance.ImagePath + "\\" + DateTime.Now.ToString("yyyyMMdd");

                if (!Directory.Exists(path2))
                {
                    Directory.CreateDirectory(path2);
                }

                string path3 = path2 + "\\" + Path;

                if (!Directory.Exists(path3))
                {
                    Directory.CreateDirectory(path3);
                }



                string FileName = path3 + "\\" + DataTime.Split(':')[0] + DataTime.Split(':')[1] + DataTime.Split(':')[2] + ".bmp";

                CogImageFileTool imageFile = new CogImageFileTool();
                imageFile.InputImage = image8Grey;
                imageFile.Operator.Open(FileName, CogImageFileModeConstants.Write);
                imageFile.Run();
                imageFile.Dispose();
            }));
        }

        public void Macqfifo_complete_飞达下相机(object sender, Cognex.VisionPro.CogCompleteEventArgs e)
        {
            ICogAcqFifo fifo = (ICogAcqFifo)sender;
            ICogImage image = null;
            int numReadyVal, numPendingVal;
            bool busyVal;
            CogAcqInfo info = new CogAcqInfo();
            try
            {
                fifo.GetFifoState(out numPendingVal, out numReadyVal, out busyVal);
                if (numReadyVal > 0)
                {
                    image = fifo.CompleteAcquireEx(info);
                    Controller.Instance.StationInfo.image8Greys = image as CogImage8Grey;
                   // MainPage.Instance.qrCamera.Image = image;

                }
            }
            catch (Exception ex)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "飞达前下相机-->" + ex);
            }
        }

        public void SaveCSV(string str)
        {
            if (!Directory.Exists(Project.Instance.CSVPath))
            {
                Directory.CreateDirectory(Project.Instance.CSVPath);
            }
            string FilePath = Project.Instance.CSVPath + "//" + DateTime.Now.ToString("yyyyMMdd") + ".CSV";
            if (!File.Exists(FilePath))
            {
                //File.Create(FilePath);

                string ExcelTitle = "时间,左1,左2,左3,左4,左5,右1,右2,右3,右4,右5,结果";
                using (FileStream fileStream = new FileStream(FilePath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
                    {
                        writer.WriteLine(ExcelTitle);
                    }
                }
            }

            using (FileStream fileStream = new FileStream(FilePath, FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    writer.WriteLine(str);
                }
            }
        }



        #region 码值相关

        //码值  1和3
        public ConcurrentQueue<OP70> 前滑台上传信息 = new ConcurrentQueue<OP70>();


        //码值  2和4
        public ConcurrentQueue<OP70> 后滑台上传信息 = new ConcurrentQueue<OP70>();


        //码值  1  2  3  4
        public ConcurrentQueue<OP70> 前后滑台上传下发信息汇总 = new ConcurrentQueue<OP70>();

        public ConcurrentQueue<获取最后焊接结果> weldResult = new ConcurrentQueue<获取最后焊接结果>();

        public string ADD(OP70 oP70)
        {
            return @$"INSERT INTO op070
            (product_code, process_result, inbound_time, outbound_time, rfid, link_greas,zf,bh,xdsdzz,cfyl,hjsj,sd,jdsdzz,hjxdsd,hjfzgl,hjnl,zdhjyl)
             VALUES ('{oP70.product_code}', 'P', '{oP70.inbound_time:MM-dd-yyyy HH:mm:ss}', '{oP70.outbound_time:MM-dd-yyyy HH:mm:ss}', '{oP70.rfid}', 'OP70','{oP70.zf}','{oP70.bh}'
             ,'{oP70.xdsdzz}' ,'{oP70.cfyl}' ,'{oP70.hjsj}' ,'{oP70.sd}' ,'{oP70.jdsdzz}' ,'{oP70.hjxdsd}' ,'{oP70.hjfzgl}' ,'{oP70.hjnl}' ,'{oP70.zdhjyl}');";
        }



        #endregion



        #region 项目相关

        //public int 手动流程选择等待 = 0;

        //public int 手动流程选择 = 0;

        //public int 治具选择 = 0;     //界面选择用哪个治具

        public Dictionary<string, int> 扫码对应治具 = new Dictionary<string, int>()
        {
            {"A1_1",1 },
            {"A1_2",1 },
            {"A2_1",2 },
            {"A2_2",2 },
            {"A3_1",3 },
            {"A3_2",3 },
        };

        public string 治具码值1 = "";

        public string 治具码值2 = "";

        public int 允许进入治具选择流程 = 0;

       



        public int 治具选择治具位空位 = 0;


        public int 扫码1值对应治具位 = 0;

        public int 扫码2值对应治具位 = 0;

        public int 前焊接机触发压力 = 0;

        public int 后焊接机触发压力 = 0;

        public int 前焊接机振幅 = 0;

        public int 后焊接机振幅 = 0;




        public int 前滑台穴位选择 = 0;
        public int 后滑台穴位选择 = 0;
        public int 前覆膜机覆膜完成 = 0;
        public int 后覆膜机覆膜完成 = 0;
        public int 前覆膜机允许覆膜 = 0;
        public int 后覆膜机允许覆膜 = 0;
        public int 搬运轴在前滑台取料完成 = 0;
        public int 搬运轴在后滑台取料完成 = 0;
        public int 搬运轴流程选择 = 0;


        public int 前滑台穴位选择完成 = 0;
        public int 后滑台穴位选择完成 = 0;
        public int 呼叫搬运轴来前滑台取料 = 0;
        public int 呼叫搬运轴来后滑台取料 = 0;

        public int 搬运轴流程选择完成 = 0;



        public string 前滑台码值1 = "";
        public string 前滑台码值2 = "";
        public string 前滑台码值3 = "";
        public string 前滑台码值4 = "";
        public string 后滑台码值1 = "";
        public string 后滑台码值2 = "";
        public string 后滑台码值3 = "";
        public string 后滑台码值4 = "";




        #region LOMA


        //用于存放飞拍数据
        public double[] 飞拍X = new double[6];

        public double[] 飞拍Y = new double[6];









        //用于存放上相机灰度值平均数

        public double 右上相机Mean = 0;

        public double 左上相机Mean = 0;
        //用于存放上相机拍照结果
        public ConcurrentQueue<double> Epson1X = new ConcurrentQueue<double>();

        public ConcurrentQueue<double> Epson1Y = new ConcurrentQueue<double>();

        public ConcurrentQueue<double> Epson1R = new ConcurrentQueue<double>();

        public ConcurrentQueue<double> Epson2X = new ConcurrentQueue<double>();

        public ConcurrentQueue<double> Epson2Y = new ConcurrentQueue<double>();

        public ConcurrentQueue<double> Epson2R = new ConcurrentQueue<double>();


        //供料器

        public int 供料器震动 = 0;




        //皮带线
        public int 皮带线允许组装 = 0;

        //用于存放飞拍结果
        public int FlyVisionResult = 0;

        public double[] FlyVisionX = new double[6];

        public double[] FlyVisionY = new double[6];

        public double[] FlyVisionR = new double[6];


        //用于存放飞拍复检结果
        public int FlyVisionCheck = 0;


        public bool[] FlyVisionCheckResult = new bool[6];





        #region EPSON2

        public int EPSON位置请求 = 0;

        public int 上相机是否需要拍照 = 0;

        public int Epson2下相机请求拍照 = 0;

        public int Epson2下相机请求位置号 = 0;

        public int Epson2下相机拍照结果 = 0;

        public int Epson2皮带线请求结果 = 0;
        //用于存放下相机结果

        public double[] downCamX左 = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6 };

        public double[] downCamY左 = new double[] { 0.11, 0.11, 0.11, 0.11, 0.1, 0.1 };

        public double[] downCamR左 = new double[] { 0.12, 0.11, 0.13, 0.1, 0.13, 0.15 };

        //用于控制刷新进度

        public bool Epson2下相机是否刷新 = false;


        public bool 左皮带线是否刷新 = false;


        public int Epson2请求安装数据号 = 0;


        public int Epson2流水线组装完成 = 0;


        public int Epson2呼叫组装 = 0;
        #endregion





        #region Epson1

        //public int EPSON位置请求 = 0;

        //public int 上相机是否需要拍照 = 0;

        public int Epson1下相机请求拍照 = 0;

        public int Epson1下相机请求位置号 = 0;

        public int Epson1下相机拍照结果 = 0;

        public int Epson1皮带线请求结果 = 0;

        public int Epson1呼叫组装 = 0;
        //用于存放下相机结果

        public double[] downCamX右 = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6 };

        public double[] downCamY右 = new double[] { 0.11, 0.11, 0.11, 0.11, 0.1, 0.1 };

        public double[] downCamR右 = new double[] { 0.12, 0.11, 0.13, 0.1, 0.13, 0.15 };

        //用于控制刷新进度

        public bool Epson1下相机是否刷新 = false;


        public bool 右皮带线是否刷新 = false;


        public int Epson1请求安装数据号 = 0;


        public int Epson1流水线组装完成 = 0;



        #endregion







        #endregion





















        public DateTime 上次下料时间;


        public DateTime 前焊接机无动作时间记录;


        public DateTime 后焊接机无动作时间记录;
        #endregion

        #region 伺服安全移动

        public bool 前滑台气缸动点M60_1 = false;

        public bool 后滑台气缸动点M60_1 = false;



        public bool 前滑台气缸动点M60_2 = false;

        public bool 后滑台气缸动点M60_2 = false;


        public bool 搬运轴气缸动点1 = false;

        public bool 搬运轴气缸动点2 = false;

        public bool 搬运轴气缸动点3 = false;

        public bool 搬运轴气缸动点4 = false;


        public double EncoderPosition_搬运轴 = 0;

        #endregion


    }



    public enum EPSON2上相机_ProcessMode
    {
        判断是否需要拍照,
        无点位拍照,
        有点位直接发送,
        未指定
    }

    public enum EPSON2下相机_ProcessMode
    {
        下相机拍照点位确认,
        一点位拍照,
        二点位拍照,
        三点位拍照,
        四点位拍照,
        五点位拍照,
        六点位拍照,
        未指定
    }

    public enum EPSON2发送请求最终结果_ProcessMode
    {
        等待请求最终结果,
        请求位置1结果,
        请求位置2结果,
        请求位置3结果,
        请求位置4结果,
        请求位置5结果,
        请求位置6结果,
        未指定
    }




    public enum EPSON1上相机_ProcessMode
    {
        判断是否需要拍照,
        无点位拍照,
        有点位直接发送,
        未指定
    }
    public enum 飞拍组装流程_ProcessMode
    {
        
        飞拍,
        组装,
        复检,
        未指定
    }
    public enum EPSON1下相机_ProcessMode
    {
        下相机拍照点位确认,
        一点位拍照,
        二点位拍照,
        三点位拍照,
        四点位拍照,
        五点位拍照,
        六点位拍照,
        未指定
    }

    public enum EPSON1发送请求最终结果_ProcessMode
    {
        等待请求最终结果,
        请求位置1结果,
        请求位置2结果,
        请求位置3结果,
        请求位置4结果,
        请求位置5结果,
        请求位置6结果,
        未指定
    }


    public enum 滑台_ProcessMode
    {
        滑台上下料穴位选择,
        _13上料焊接13下料,
        _24上料焊接24下料,
        未指定
    }

    public enum 搬运轴_ProcessMode
    {
        流程选择,
        先取前滑台13再取后滑台24,
        先取后滑台24再取前滑台13,
        屏蔽前滑台,
        屏蔽后滑台,
        未指定
    }


    public enum 搬运轴换治具_ProcessMode
    {
        治具选择,
        取治具1放治具2,
        取治具1放治具3,
        取治具2放治具1,
        取治具2放治具3,
        取治具3放治具1,
        取治具3放治具2,
        未指定
    }

    public enum 覆膜机_ProcessMode
    {
        覆膜机工作,
        未指定
    }


    public enum 手动测试_ProcessMode
    {
        流程选择,
        前滑台手动,
        后滑台手动,
        未指定
    }



    public struct OP70
    {
        //link_greas位OP70
        public string link_greas;


        //产品码
        public string product_code;


        //产品结果
        public string process_result;



        //入站时间
        public DateTime inbound_time;



        //出站时间
        public DateTime outbound_time;


        //滑台  给1
        public string rfid;


        //振幅
        public int zf;

        //编号
        public string bh;

        //相对深度总值
        public double xdsdzz;

        //触发压力
        public double cfyl;

        //焊接时间
        public double hjsj;


        //速度
        public int sd;

        //绝对深度总值
        public double jdsdzz;

        //焊接相对深度
        public double hjxdsd;


        //焊接峰值功率
        public double hjfzgl;


        //焊接能量
        public double hjnl;

        //最大焊接压力
        public double zdhjyl;

    }

    public class Post
    {
        public Post(int _sid)
        {
            Sid = _sid;
        }
        public int Sid { get; set; }
    }

    public class Login
    {
        public Login(string userid, string password, string key)
        {
            UserId = userid;
            Password = password;
            Key = key;

        }


        public string UserId { get; set; }
        public string Password { get; set; }
        public string Key { get; set; }
    }



    //登录返回
    public class LoginBackup
    {
        public int StatusCode { get; set; }

        public int Sid { get; set; }
    }

    //登出返回
    public class LogoutBackup
    {
        public int StatusCode { get; set; }

    }

    public class SetStoreLogin
    {
        public SetStoreLogin(int sid, int recipeNo)
        {
            Sid = sid;

            RecipeNo = recipeNo;
        }
        public int Sid { get; set; }

        public int RecipeNo { get; set; }
    }
    public class SetStoreLogout
    {

        public int StatusCode { get; set; }

        public string RecipeNo { get; set; }
    }

    [Serializable]
    public class 获取最后焊接结果
    {
        // Nose码
        [JsonProperty("Code")]
        public string 二维码 { get; set; }
        // 状态码
        [JsonProperty("StatusCode")]
        public int 状态码 { get; set; }

        // 数字键对应的属性
        [JsonProperty("1")]
        public int 参数组编号 { get; set; }

        [JsonProperty("2")]
        public int 参数组版本号 { get; set; }

        [JsonProperty("3")]
        public DateTime 焊接日期和时间 { get; set; }

        [JsonProperty("4")]
        public int 超声波组件序列号 { get; set; }

        [JsonProperty("5")]
        public int 循环计数器 { get; set; }

        [JsonProperty("6")]
        public int 焊接模式 { get; set; }

        [JsonProperty("7")]
        public double 最大焊接压力 { get; set; }

        [JsonProperty("8")]
        public double 结束时保持压力 { get; set; }

        [JsonProperty("9")]
        public double 焊接绝对深度 { get; set; }

        [JsonProperty("10")]
        public double 绝对深度总值 { get; set; }

        [JsonProperty("11")]
        public double 焊接相对深度 { get; set; }

        [JsonProperty("12")]
        public double 保压相对深度 { get; set; }

        [JsonProperty("13")]
        public double 相对深度总值 { get; set; }

        [JsonProperty("14")]
        public double 触发距离 { get; set; }

        [JsonProperty("15")]
        public int 速度 { get; set; }


        [JsonProperty("16")]
        public double 焊接时间 { get; set; }

        [JsonProperty("17")]
        public double 焊接能量 { get; set; }

        [JsonProperty("18")]
        public double 焊接峰值功率 { get; set; }

        [JsonProperty("19")]
        public int 启动频率 { get; set; }

        [JsonProperty("20")]
        public int 频率变化 { get; set; }

        [JsonProperty("21")]
        public int 循环时间 { get; set; }

        [JsonProperty("22")]
        public string 用户名 { get; set; }

        [JsonProperty("23")]
        public string 工件ID { get; set; }

        [JsonProperty("24")]
        public string 批量ID { get; set; }

        [JsonProperty("25")]
        public double 触发起始点 { get; set; }

        [JsonProperty("26")]
        public double 焊接起始点 { get; set; }

        [JsonProperty("27")]
        public double 保压起始点 { get; set; }

        [JsonProperty("28")]
        public int 报警信号旗 { get; set; }

        [JsonProperty("29")]
        public int 参数组状态 { get; set; }


        public override string ToString()
        {
            return $"{二维码},{焊接绝对深度},{绝对深度总值},{焊接相对深度},{保压相对深度},{相对深度总值},{触发距离},{速度},{焊接时间},{焊接能量},{焊接峰值功率}";
        }

    }




}
