using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Permission;
using JabilVisionMotion.Lib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JabilVisionMotion.Edit.Process.AxisMotionControlProcess
{
    /// <summary>
    /// 判断轴运动到正极限流程类，继承自ProcessBase基类
    /// 指令格式：CheckELPDone|轴名称|速度|超时（单位：秒）
    /// 特性：先检查轴是否已在正极限位置，如未在则控制轴以设定速度向正极限运动，需等待正极限传感器信号稳定保持设定时间后确认完成
    /// 增加信号稳定检查，避免传感器抖动导致的误判
    /// </summary>
    public class CheckELPDoneProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>运动状态检查间隔（毫秒），控制极限运动过程中的状态查询频率</summary>
        private const int MotionCheckDelayMs = 1;
        /// <summary>正极限传感器状态位索引，用于从运动控制器状态数组中获取传感器触发状态</summary>
        private const int PositiveLimitSensorBitIndex = 1;
        /// <summary>正向运动方向参数（约定值，1表示正向，具体含义由运动控制器定义）</summary>
        private const int PositiveDirection = 1;
        /// <summary>正极限传感器信号稳定时间（毫秒）</summary>
        /// <remarks>确保传感器信号持续稳定后再确认到达正极限位置</remarks>
        private const int PositiveLimitStableTimeMs = 1000;
        #endregion

        #region 私有字段
        private readonly string _process;        // 原始判断正极限运动指令字符串
        private readonly string _processTip;     // 判断正极限运动指令提示信息
        private static readonly object _motionLock = new object(); // 线程锁，确保运动操作的线程安全

        // 指令解析结果
        private string _commandName;       // 指令名称（如"CheckELPDone"）
        private string _axisName;          // 要检查/运动的轴名称
        private double _moveSpeed;         // 运动速度（如需要运动时使用）
        private int _timeoutSeconds;       // 整体超时时间（秒）

        // 运动参数与状态跟踪
        private MotionAxisDto _axis;       // 轴的配置信息
        private double _currentPosition;   // 轴当前位置（最终状态位置）
        private DateTime? _positiveLimitStableStartTime; // 正极限信号开始稳定的时间
        private bool _lastPositiveLimitState;            // 上一次检测的正极限传感器状态
        #endregion

        /// <summary>
        /// 初始化判断轴运动到正极限流程
        /// </summary>
        /// <param name="process">判断正极限运动指令字符串</param>
        /// <param name="processTip">判断正极限运动指令提示信息</param>
        public CheckELPDoneProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                CurrentStatus = GeneralStatus.Wait; // 初始状态设为等待
                Message = "Wait";                  // 初始消息设为等待
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);      // 解析判断正极限运动指令
                BindAxisParameter();        // 绑定轴配置信息
            }
            catch (Exception ex)
            {
                Message = ex.Message;       // 记录异常信息
                CurrentStatus = GeneralStatus.Error; // 状态设为错误
                RLog.SaveError(ex);         // 保存错误日志
                return; // 设置错误状态后立即返回
            }
        }

        /// <summary>
        /// 克隆方法，支持副流程缓存机制，确保多实例状态隔离
        /// </summary>
        public override object Clone()
        {
            var clone = new CheckELPDoneProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _axisName = this._axisName,
                _moveSpeed = this._moveSpeed,
                _timeoutSeconds = this._timeoutSeconds,
                _axis = this._axis
            };
            // 重置状态，确保克隆实例的独立性
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";
            clone._positiveLimitStableStartTime = null;
            clone._lastPositiveLimitState = false;

            return clone;
        }

        /// <summary>
        /// 解析判断正极限运动指令参数
        /// 提取轴名称、运动速度和超时时间
        /// </summary>
        /// <param name="process">待解析的判断正极限运动指令</param>
        private void ParseCommand(string process)
        {
            // 按竖线分割指令，移除空条目
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式是否正确
            if (commandParts.Length < 4)
            {
                HandleMotionError($"指令格式错误: {process}，至少需要4个部分，格式为CheckELPDone|轴名称|速度|超时");
                return; // 设置错误后立即返回
            }

            // 提取指令各部分信息
            _commandName = commandParts[0];
            _axisName = commandParts[1];

            // 解析运动速度（转换为双精度浮点数）
            if (!double.TryParse(commandParts[2], out _moveSpeed))
            {
                HandleMotionError($"速度格式错误: {commandParts[2]}");
                return; // 设置错误后立即返回
            }

            // 解析超时时间（转换为整数）
            if (!int.TryParse(commandParts[3], out _timeoutSeconds))
            {
                HandleMotionError($"超时格式错误: {commandParts[3]}");
                return; // 设置错误后立即返回
            }
        }

        /// <summary>
        /// 绑定轴参数，从系统中查询指定轴的配置信息并设置运动速度
        /// </summary>
        private void BindAxisParameter()
        {
            // 从运动系统中查找匹配的轴
            _axis = MotionSystem.Instance.MotionAxes
                .FirstOrDefault(a => a.轴名称 == _axisName);

            // 若未找到轴，处理错误
            if (_axis == null)
            {
                HandleMotionError($"未找到名称为[{_axisName}]的轴信息");
                return; // 设置错误后立即返回
            }

            _axis.单轴运行速度 = _moveSpeed; // 设置轴的运行速度
            ProcessId = _axis.绑定代码;   // 绑定流程ID（用于日志跟踪）
        }

        /// <summary>
        /// 处理运动初始化错误
        /// 触发紧急停止并记录错误日志
        /// </summary>
        /// <param name="errorMessage">错误描述信息</param>
        private void HandleMotionError(string errorMessage)
        {
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped); // 触发紧急停止
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                _process,
                $"--->>错误：{errorMessage}！！！！"
            );

            // 设置错误状态，不再抛出异常
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
        }

        /// <summary>
        /// 执行判断轴运动到正极限操作（核心方法）
        /// 先检查是否已在正极限位置，如未在则启动运动并等待传感器信号稳定
        /// </summary>
        public override void Run()
        {
            try
            {
                CurrentStatus = GeneralStatus.Running; // 状态设为运行中
                Message = "Running";

                // 测试模式：直接返回成功（跳过实际检查和运动）
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                // 验证轴是否有效
                if (_axis == null)
                {
                    Message = $"错误的指令：{_process}\r\n报错原因：轴[{_axisName}]未绑定";
                    CurrentStatus = GeneralStatus.Error;
                    return; // 设置错误后立即返回
                }

                // 初始化信号跟踪变量
                _positiveLimitStableStartTime = null;
                _lastPositiveLimitState = false;

                var startTime = DateTime.Now; // 记录流程开始时间

                // 检查是否已在正极限位置（信号稳定），如未在则启动运动
                if (!CheckPositiveLimitStability())
                {
                    StartPositiveLimitMovement();
                    // 检查启动运动是否失败
                    if (CurrentStatus == GeneralStatus.Error)
                        return; // 启动失败则返回
                }
                else
                {
                    // 已在正极限位置，直接设置为完成状态
                    _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                    CurrentStatus = GeneralStatus.NoError;
                }

                // 如未完成，等待正极限传感器信号稳定
                if (CurrentStatus != GeneralStatus.NoError)
                {
                    WaitForPositiveLimitStability(startTime);
                }

                // 操作成功完成后更新消息
                if (CurrentStatus == GeneralStatus.NoError)
                {
                    UpdateCompletionMessage();
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;       // 记录异常信息
                CurrentStatus = GeneralStatus.Error; // 状态设为错误
                RLog.SaveError(ex);         // 保存错误日志
                return; // 设置错误后立即返回
            }
        }

        /// <summary>
        /// 启动向正极限的运动
        /// 仅在未触发极限且轴未在运动状态时启动
        /// </summary>
        private void StartPositiveLimitMovement()
        {
            lock (_motionLock) // 加锁确保线程安全
            {
                // 检查轴是否处于非运动状态且未触发极限
                if (!CheckPositiveLimitStability() && !MotionSystem.Instance.MotionController.CheckMoveDone(_axis))
                {
                    // 1表示正向运动方向（约定值，具体含义由运动控制器定义）
                    string moveResult = MotionSystem.Instance.MotionController.JogMove(_axis, PositiveDirection);
                    if (moveResult != "NoError")
                    {
                        CurrentStatus = GeneralStatus.Error;
                        Message = $"轴[{_axisName}]启动向正极限运动失败: {moveResult}";
                        return; // 设置错误后立即返回
                    }
                }
            }
        }

        /// <summary>
        /// 等待正极限传感器信号稳定
        /// 循环检查直到信号稳定或出现异常
        /// </summary>
        private void WaitForPositiveLimitStability(DateTime startTime)
        {
            while (true)
            {
                Thread.Sleep(MotionCheckDelayMs); // 短暂延迟，降低CPU占用
                CurrentStatus = GeneralStatus.Running;

                // 检查机台状态（紧急停止/报警）或超时
                if (CheckMachineStatus(startTime) || CheckTimeout(startTime))
                {
                    return; // 异常情况下终止等待
                }

                // 检查正极限传感器是否稳定触发
                if (CheckPositiveLimitStability())
                {
                    StopMotion(); // 信号稳定后停止运动
                    // 检查停止运动是否失败
                    if (CurrentStatus == GeneralStatus.Error)
                        return; // 停止失败则返回
                    CurrentStatus = GeneralStatus.NoError;
                    break;
                }
            }
        }

        /// <summary>
        /// 检查正极限传感器信号稳定性
        /// 信号需持续稳定达到设定时间（PositiveLimitStableTimeMs）
        /// </summary>
        private bool CheckPositiveLimitStability()
        {
            var motionStatus = MotionSystem.Instance.MotionController.GetMotionSts(_axis);
            bool currentSensorState = motionStatus.Length > PositiveLimitSensorBitIndex
                && motionStatus[PositiveLimitSensorBitIndex];

            // 传感器状态变化：重新计时
            if (currentSensorState != _lastPositiveLimitState)
            {
                _positiveLimitStableStartTime = currentSensorState ? DateTime.Now : (DateTime?)null;
                _lastPositiveLimitState = currentSensorState;
                return false;
            }

            // 传感器未触发：重置计时器
            if (!currentSensorState)
            {
                _positiveLimitStableStartTime = null;
                return false;
            }

            // 传感器已触发：开始计时或检查是否达到稳定时间
            if (!_positiveLimitStableStartTime.HasValue)
            {
                _positiveLimitStableStartTime = DateTime.Now;
                return false;
            }

            // 检查是否达到稳定时间
            var stableDuration = DateTime.Now - _positiveLimitStableStartTime.Value;
            return stableDuration.TotalMilliseconds >= PositiveLimitStableTimeMs;
        }

        /// <summary>
        /// 停止轴运动
        /// 传感器信号稳定后停止运动，防止机械过冲
        /// </summary>
        private void StopMotion()
        {
            var stopResult = MotionSystem.Instance.MotionController.SmtStop(_axis, 0);
            if (stopResult != "NoError")
            {
                HandleMotionError($"轴[{_axisName}]停止运动失败: {stopResult}");
                return; // 设置错误后立即返回
            }
            _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis); // 记录停止时的位置
        }

        /// <summary>
        /// 检查机台状态（紧急停止/报警）
        /// 异常状态下停止运动并记录信息
        /// </summary>
        private bool CheckMachineStatus(DateTime startTime)
        {
            // 机台紧急停止
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：机台紧急停止报警\r\n{_axisName}轴实际点位：{_currentPosition}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0); // 停止轴运动
                return true;
            }

            // 机台报警状态
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming)
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：机台报警\r\n{_axisName}轴实际点位：{_currentPosition}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0); // 停止轴运动
                return true;
            }

            // 检查轴是否报警
            if (MotionSystem.Instance.MotionController.CheckServoAlarm(_axis))
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"运动中发生伺服报警\r\n当前位置: {_currentPosition}";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0); // 停止轴运动
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查整体流程超时
        /// 超过设定时间未完成操作则停止运动并报警
        /// </summary>
        private bool CheckTimeout(DateTime startTime)
        {
            if (DateTime.Now.Subtract(startTime).TotalSeconds > _timeoutSeconds)
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：运动超时报警\r\n{_axisName}轴实际点位：{_currentPosition}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0); // 停止轴运动
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新操作完成消息
        /// 提示用户轴已到达正极限位置及当前位置，包含信号稳定时间信息
        /// </summary>
        private void UpdateCompletionMessage()
        {
            Message = $"NoError\r\n{_axisName}轴已到达正极限位置（信号稳定{PositiveLimitStableTimeMs}ms），当前点位：{_currentPosition}\r\n";
            CurrentStatus = GeneralStatus.NoError; // 标记为成功状态
        }
    }
}
