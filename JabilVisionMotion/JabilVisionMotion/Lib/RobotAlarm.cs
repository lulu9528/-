using HslCommunication;                   // 用于Modbus通信的库
using Jabil.Lib;                          // Jabil通用工具库
using Jabil.Module.Alarm;                 // 报警模块核心功能
using Jabil.Module.Alarm.Common.Dtos;     // 报警数据传输对象定义
using Jabil.Module.Alarm.Service;         // 报警记录服务
using Jabil.Module.Log.Lib;               // 日志记录工具
using Jabil.Module.Modbus;                // Modbus设备管理
using Jabil.Module.Permission;            // 权限管理
using JabilVisionMotion.Frm;              // 主窗体相关
using JabilVisionMotion.Frm.Dialog;       // 对话框窗体
using JabilVisionMotion.Frm.UserPage;     // 用户页面
using System;
using System.Collections.Generic;
using System.Diagnostics;                 // 提供Stopwatch计时器
using System.Drawing;                     // 提供颜色类
using System.Linq;                        // LINQ查询支持
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;               // Windows Forms UI组件

namespace JabilVisionMotion.Lib
{
    /// <summary>
    /// 机器人报警监控基类<br/>
    /// 设计模式：模板方法模式，封装不变的报警监控流程，将设备特定参数延迟到子类实现<br/>
    /// 核心功能：
    /// - 统一处理PLC报警的触发与复位逻辑
    /// - 管理报警时间与持续时长计算
    /// - 提供报警信息的获取与记录机制
    /// 子类仅需通过构造函数提供设备特定参数（如机器人名称、报警索引）即可快速实现监控功能。
    /// </summary>
    public abstract class BaseRobotAlarm
    {
        #region 保护字段（子类可访问的通用字段）
        /// <summary>当前触发的报警信息对象</summary>
        protected AlarmCommunicationDIODto _currentAlarm = new AlarmCommunicationDIODto();

        /// <summary>报警开始时间戳</summary>
        protected DateTime _alarmStartTime = DateTime.Now;

        /// <summary>报警结束（复位）时间戳</summary>
        protected DateTime _alarmEndTime = DateTime.Now;

        /// <summary>
        /// 用于计算报警持续时间的计时器<br/>
        /// 选择Stopwatch而非DateTime差值计算，因为其提供更高精度的计时
        /// </summary>
        protected readonly Stopwatch _alarmProcessingStopwatch = new Stopwatch();

        /// <summary>机器人设备名称（由子类在构造函数中指定）</summary>
        protected readonly string _robotName;

        /// <summary>报警起始索引（Modbus地址，由子类在构造函数中指定）</summary>
        protected readonly string _alarmIndex;

        /// <summary>需要监控的报警点位数量</summary>
        protected readonly ushort _count;

        #endregion

        #region 报警监控属性
        private bool _localPlcAlarmMonitoring;

        /// <summary>
        /// 本地PLC报警监控状态（核心触发属性）<br/>
        /// 设计思路：通过属性的setter实现状态变化检测，类似事件触发机制<br/>
        /// 状态转换逻辑：
        /// - 当状态从false变为true时：触发报警处理逻辑
        /// - 当状态从true变为false时：触发报警复位逻辑
        /// - 状态未变化时不执行任何操作（防止重复处理）
        /// </summary>
        public bool LocalPlcAlarmMonitoring
        {
            get => _localPlcAlarmMonitoring;
            set
            {
                // 状态未变化则直接返回，避免重复处理
                if (_localPlcAlarmMonitoring == value) return;

                // 根据状态变化方向执行对应逻辑
                if (!_localPlcAlarmMonitoring)
                {
                    OnAlarmTriggered(); // 报警触发（状态从false→true）
                }
                else
                {
                    OnAlarmReset(); // 报警复位（状态从true→false）
                }

                _localPlcAlarmMonitoring = value; // 更新状态
            }
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化机器人报警监控基类<br/>
        /// 设计模式：依赖注入，通过构造函数注入设备特定参数<br/>
        /// 由子类调用，传入设备特定参数
        /// </summary>
        /// <param name="robotName">机器人设备名称（用于定位PLC设备）</param>
        /// <param name="alarmIndex">报警起始索引（Modbus地址，用于读取报警状态）</param>
        /// <param name="count">需要监控的报警点位数量</param>
        protected BaseRobotAlarm(string robotName, string alarmIndex, ushort count)
        {
            // 校验机器人名称不为空，确保后续逻辑能正确找到PLC设备
            _robotName = robotName ?? throw new ArgumentNullException(
                nameof(robotName), "机器人名称不能为空");

            _alarmIndex = alarmIndex;
            _count = count;
        }
        #endregion

        #region 报警处理虚方法（子类可重写自定义逻辑）
        /// <summary>
        /// 报警触发时的处理逻辑（虚方法，子类可重写以实现特定逻辑）<br/>
        /// 标准处理流程：<br/>
        /// 1. 启动计时器记录报警持续时间<br/>
        /// 2. 记录报警开始时间<br/>
        /// 3. 检查并获取具体报警信息<br/>
        /// 4. 更新设备运行状态为报警中<br/>
        /// 5. 在主线程中显示报警窗口<br/>
        /// 异常处理：所有异常被捕获并记录，避免影响整体监控流程
        /// </summary>
        protected virtual void OnAlarmTriggered()
        {
            try
            {
                _alarmProcessingStopwatch.Restart(); // 重启计时器，开始记录报警时长
                _alarmStartTime = DateTime.Now;      // 记录报警开始时间点

                // 获取当前报警的详细信息
                _currentAlarm = CheckRobotAlarm();

                // 无有效报警信息则返回，避免空引用异常
                if (_currentAlarm?.报警内容 == null) return;

                // 主线程更新UI：UI操作必须在主线程执行，否则可能引发跨线程异常
                WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () =>
                {
                    // 如果设备不是紧急停止状态，才处理报警（紧急停止可能有专门处理流程）
                    if (MenuProxy.Instance.MachineRunStatus != MachineRunStatus.EmergencyStopped)
                    {
                        // 保存报警信息到全局变量
                        Project.Instance.GlobalVariables.AlarmMessage = _currentAlarm.报警内容;

                        // 更新设备运行状态为报警中
                        MenuProxy.Instance.MachineRunStatus = MachineRunStatus.Alarming;

                        // 显示报警窗口
                        AlarmWindowManager.ShowAlarmWindow();
                    }
                });
            }
            catch (Exception ex)
            {
                // 记录异常日志，便于问题排查
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 报警复位时的处理逻辑（虚方法，子类可重写）<br/>
        /// 标准处理流程：<br/>
        /// 1. 记录报警结束时间并停止计时器<br/>
        /// 2. 生成报警记录并保存到服务<br/>
        /// 3. 在监控页面输出报警信息<br/>
        /// 4. 清除当前报警信息<br/>
        /// 异常处理：所有异常被捕获并记录
        /// </summary>
        protected virtual void OnAlarmReset()
        {
            try
            {
                _alarmEndTime = DateTime.Now;            // 记录报警结束时间
                _alarmProcessingStopwatch.Stop();        // 停止计时器，结束计时

                // 无有效报警信息则返回
                if (_currentAlarm?.报警内容 == null) return;

                // 构建报警记录对象，整理需要保存的报警信息
                var errorRecord = new ProcessErrorDto
                {
                    报警ID = _currentAlarm.绑定代码,
                    异常开始时间 = _alarmStartTime.ToString("HH:mm:ss"), // 格式化时间为时分秒
                    结束时间 = _alarmEndTime.ToString("HH:mm:ss"),
                    报警内容 = _currentAlarm.报警内容,
                    // 转换为秒并保留1位小数，更直观的显示耗时
                    耗时 = Math.Round(_alarmProcessingStopwatch.ElapsedMilliseconds / 1000.0, 1),
                    日期 = DateTime.Now.Date,
                    站别 = _currentAlarm.站别
                };

                // 保存报警记录到服务，实现持久化存储
                ProcessErrorService.Instance.Add(errorRecord);

                // 主线程更新监控页面显示，确保UI操作线程安全
                WFUtil.UpdateMainThreadUIAsync(ProcessMonitorPage.Instance, () =>
                {
                    // 在监控页面输出报警信息，使用红色标识错误级别
                    //ProcessMonitorPage.Instance.alarmView_PLC报警.OutputMsg(
                    //    LogType.Error,
                    //    Color.Red,
                    //    errorRecord.报警ID,
                    //    errorRecord.异常开始时间,
                    //    errorRecord.结束时间,
                    //    errorRecord.报警内容,
                    //    $"{errorRecord.耗时}s",
                    //    errorRecord.日期.ToString("yyyyMMdd"),
                    //    errorRecord.站别
                    //);
                });

                _currentAlarm = null; // 清除当前报警信息，准备处理下一次报警
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录异常日志
            }
        }
        #endregion

        #region 核心报警检查方法
        /// <summary>
        /// 检查机器人当前报警状态（核心方法）<br/>
        /// 实现逻辑：<br/>
        /// 1. 查找目标PLC设备<br/>
        /// 2. 读取报警状态（从指定索引开始读取指定数量的点位）<br/>
        /// 3. 收集所有触发的报警信息<br/>
        /// 4. 返回最高优先级的报警（优先级数值越小，优先级越高）
        /// </summary>
        /// <returns>最高优先级的报警信息对象，无报警时返回null</returns>
        protected AlarmCommunicationDIODto CheckRobotAlarm()
        {
            try
            {
                // 查找目标PLC设备（根据机器人名称）
                var plcDevice = ModbusSystem.Instance.PlcModbusList
                    .FirstOrDefault(s => s.DeviceName == _robotName);

                // 如果未找到PLC设备，记录错误并返回null
                if (plcDevice == null)
                {
                    RLog.SaveError(new KeyNotFoundException($"未找到名称为【{_robotName}】的PLC设备"));
                    return null;
                }

                // 读取报警状态：从报警索引开始读取指定数量的布尔值（每个值代表一个报警点位）
                var readResult = plcDevice.ReadBoolArray(_alarmIndex, _count);

                // 读取失败时记录错误并返回null
                if (!readResult.IsSuccess)
                {
                    RLog.SaveError(new InvalidOperationException(
                        $"读取【{_robotName}】报警状态失败: {readResult.Message}"));
                    return null;
                }

                // 收集所有触发的报警（点位值为true表示报警）
                var triggeredAlarms = new List<AlarmCommunicationDIODto>();
                for (int i = 0; i < readResult.Content.Length; i++)
                {
                    if (readResult.Content[i]) // 该点位为true表示触发报警
                    {
                        // 查找匹配的报警信息（点位索引和设备名称需匹配）
                        var alarm = AlarmSystem.Instance.ListAlarmCommunicationDIO
                            .FirstOrDefault(a =>
                                a.点位索引 == (_alarmIndex + i) &&
                                a.设备名称 == _robotName);

                        if (alarm != null)
                        {
                            triggeredAlarms.Add(alarm);
                        }
                    }
                }

                // 返回最高优先级的报警（按优先级升序排序，取第一个）
                return triggeredAlarms
                    .OrderBy(a => a.优先级)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录异常日志
                return null;
            }
        }
        #endregion
    }

    /// <summary>
    /// Epson机器人1号站报警监控类<br/>
    /// 设计思路：通过继承基类并在构造函数中传入特定参数，快速实现特定设备的报警监控<br/>
    /// 无需编写额外逻辑，完全复用基类的通用功能
    /// </summary>
    public class RobotAlarm_Epson_1 : BaseRobotAlarm
    {
        /// <summary>
        /// 初始化Epson机器人1号站报警监控<br/>
        /// 设备特定参数：
        /// - 设备名称：H5u-1
        /// - 报警起始索引：1840
        /// - 监控点位数量：10
        /// </summary>
        public RobotAlarm_Epson_1() : base("H5u-1", "1840", 10)
        {
            // 设备特定参数通过基类构造函数传入，无需额外逻辑
        }
    }

    /// <summary>
    /// Epson机器人2号站报警监控类<br/>
    /// 设计思路：与1号站相同，仅参数不同，体现了基类设计的复用性
    /// </summary>
    public class RobotAlarm_Epson_2 : BaseRobotAlarm
    {
        /// <summary>
        /// 初始化Epson机器人2号站报警监控<br/>
        /// 设备特定参数：
        /// - 设备名称：H5u-2
        /// - 报警起始索引：1740
        /// - 监控点位数量：10
        /// </summary>
        public RobotAlarm_Epson_2() : base("H5u-2", "1740", 10)
        {
            // 设备特定参数通过基类构造函数传入，无需额外逻辑
        }
    }
}
