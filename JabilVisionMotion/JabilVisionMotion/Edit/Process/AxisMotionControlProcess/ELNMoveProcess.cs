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
    /// 轴运动到负极限流程类，继承自ProcessBase基类
    /// 指令格式：ELNMove|轴名称|速度|超时（单位：秒）
    /// 特性：控制指定轴以设定速度向负极限方向运动，需等待负极限传感器信号稳定保持设定时间后确认完成
    /// 增加信号稳定检查，避免传感器抖动导致的误判
    /// </summary>
    public class ELNMoveProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>运动状态检查间隔（毫秒），控制负极限运动过程中的状态查询频率</summary>
        private const int MotionCheckDelayMs = 1;
        /// <summary>负极限传感器状态位索引，用于从运动控制器状态数组中获取传感器触发状态</summary>
        private const int NegativeLimitSensorBitIndex = 4;
        /// <summary>负向运动方向参数（约定值，0表示负向，具体含义由运动控制器定义）</summary>
        private const int NegativeDirection = 0;
        /// <summary>负极限传感器信号稳定时间（毫秒）</summary>
        /// <remarks>确保传感器信号持续稳定后再确认到达负极限位置</remarks>
        private const int NegativeLimitStableTimeMs = 1000;
        #endregion

        #region 私有字段
        private readonly string _process;        // 原始负极限运动指令字符串
        private readonly string _processTip;     // 负极限运动指令提示信息
        private static readonly object _motionLock = new object(); // 线程锁，确保运动操作的线程安全

        // 指令解析结果
        private string _commandName;       // 指令名称（如"ELNMove"）
        private string _axisName;          // 要运动的轴名称
        private double _moveSpeed;         // 运动速度
        private int _timeoutSeconds;       // 运动超时时间（秒）

        // 运动参数与状态跟踪
        private MotionAxisDto _axis;       // 运动轴的配置信息
        private double _currentPosition;   // 到达负极限后的位置
        private DateTime? _negativeLimitStableStartTime; // 负极限信号开始稳定的时间
        private bool _lastNegativeLimitState;            // 上一次检测的负极限传感器状态
        #endregion

        /// <summary>
        /// 初始化轴运动到负极限流程
        /// </summary>
        /// <param name="process">负极限运动指令字符串</param>
        /// <param name="processTip">负极限运动指令提示信息</param>
        public ELNMoveProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                CurrentStatus = GeneralStatus.Wait; // 初始状态设为等待
                Message = "Wait";                  // 初始消息设为等待
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);      // 解析负极限运动指令
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
        /// 克隆方法，支持多实例状态隔离
        /// </summary>
        public override object Clone()
        {
            var clone = new ELNMoveProcess(_process, _processTip)
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

            return clone;
        }

        /// <summary>
        /// 解析负极限运动指令参数
        /// 提取轴名称、运动速度和超时时间
        /// </summary>
        /// <param name="process">待解析的负极限运动指令</param>
        private void ParseCommand(string process)
        {
            // 按竖线分割指令，移除空条目
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式是否正确
            if (commandParts.Length < 4)
            {
                HandleMotionError($"指令格式错误: {process}，至少需要4个部分，格式为ELNMove|轴名称|速度|超时");
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
        /// 执行轴运动到负极限操作（核心方法）
        /// 协调负向极限运动流程，包括启动运动和状态检查
        /// </summary>
        public override void Run()
        {
            try
            {
                CurrentStatus = GeneralStatus.Running; // 状态设为运行中
                Message = "Running";

                // 测试模式：直接返回成功（跳过实际运动）
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
                _negativeLimitStableStartTime = null;
                _lastNegativeLimitState = false;

                var startTime = DateTime.Now; // 记录运动开始时间

                // 执行向负极限运动
                ExecuteNegativeLimitMove(startTime);

                // 运动成功完成后更新完成消息
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
        /// 执行向负极限运动并等待触发极限传感器
        /// 核心逻辑：启动点动运动，持续运行直到负极限传感器信号稳定
        /// </summary>
        /// <param name="startTime">运动开始时间（用于超时判断）</param>
        private void ExecuteNegativeLimitMove(DateTime startTime)
        {
            // 启动向负极限点动运动（加锁确保线程安全）
            string moveResult;
            lock (_motionLock)
            {
                // 0表示负向运动方向（约定值，具体含义由运动控制器定义）
                moveResult = MotionSystem.Instance.MotionController.JogMove(_axis, NegativeDirection);
            }

            // 检查运动指令是否成功发送
            if (moveResult != "NoError")
            {
                CurrentStatus = GeneralStatus.Error;
                Message = $"轴[{_axisName}]启动向负极限运动失败: {moveResult}";
                return; // 设置错误后立即返回
            }

            // 等待负极限传感器信号稳定
            while (true)
            {
                Thread.Sleep(MotionCheckDelayMs); // 短暂延迟，降低CPU占用
                CurrentStatus = GeneralStatus.Running;

                // 检查机台状态（紧急停止/报警）或超时
                if (CheckMachineStatus(startTime) || CheckTimeout(startTime))
                {
                    return; // 异常情况下终止运动
                }

                // 检查负极限传感器是否稳定触发
                if (CheckNegativeLimitStability())
                {
                    StopMotion(); // 信号稳定后停止运动
                    CurrentStatus = GeneralStatus.NoError;
                    break;
                }
            }
        }

        /// <summary>
        /// 检查负极限传感器信号稳定性
        /// 信号需持续稳定达到设定时间（NegativeLimitStableTimeMs）
        /// </summary>
        private bool CheckNegativeLimitStability()
        {
            var motionStatus = MotionSystem.Instance.MotionController.GetMotionSts(_axis);
            bool currentSensorState = motionStatus.Length > NegativeLimitSensorBitIndex
                && motionStatus[NegativeLimitSensorBitIndex];

            // 传感器状态变化：重新计时
            if (currentSensorState != _lastNegativeLimitState)
            {
                _negativeLimitStableStartTime = currentSensorState ? DateTime.Now : (DateTime?)null;
                _lastNegativeLimitState = currentSensorState;
                return false;
            }

            // 传感器未触发：重置计时器
            if (!currentSensorState)
            {
                _negativeLimitStableStartTime = null;
                return false;
            }

            // 传感器已触发：开始计时或检查是否达到稳定时间
            if (!_negativeLimitStableStartTime.HasValue)
            {
                _negativeLimitStableStartTime = DateTime.Now;
                return false;
            }

            // 检查是否达到稳定时间
            var stableDuration = DateTime.Now - _negativeLimitStableStartTime.Value;
            return stableDuration.TotalMilliseconds >= NegativeLimitStableTimeMs;
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
        /// 检查运动超时
        /// 超过设定时间未稳定触发极限传感器则停止运动并报警
        /// 防止因传感器故障导致轴无限运动
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
        /// 更新运动完成消息
        /// 提示用户轴已到达负极限位置及当前位置，包含信号稳定时间信息
        /// </summary>
        private void UpdateCompletionMessage()
        {
            Message = $"NoError\r\n{_axisName}轴已到达负极限位置（信号稳定{NegativeLimitStableTimeMs}ms），当前点位：{_currentPosition}\r\n";
            CurrentStatus = GeneralStatus.NoError; // 标记为成功状态
        }
    }
}
