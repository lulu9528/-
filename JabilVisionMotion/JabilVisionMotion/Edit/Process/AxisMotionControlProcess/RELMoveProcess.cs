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
    /// 相对运动流程类，继承自ProcessBase基类
    /// 指令格式：RELMove|轴名称|相对距离|速度|超时
    /// 超时（单位：S）：超过设定秒数后触发超时报警
    /// 特性：控制指定轴进行相对距离运动，等待运动完成并确保位置稳定
    /// 增加位置信号稳定检查，避免机械振动或信号波动导致的位置误判
    /// </summary>
    public class RELMoveProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>运动检查延迟时间(毫秒)，控制位置检查的频率</summary>
        private const int MotionDelayMs = 1;
        /// <summary>位置稳定判定时间(毫秒)，位置需在此时间内保持稳定</summary>
        private const int PositionStableTimeMs = 500;
        /// <summary>位置允许误差范围，在此范围内视为位置稳定</summary>
        private const double PositionTolerance = 0.01; // 根据实际精度需求调整
        #endregion

        #region 私有字段
        private readonly string _process;        // 原始相对运动指令字符串
        private readonly string _processTip;     // 相对运动指令提示信息
        private static readonly object _motionLock = new object(); // 线程锁，确保运动控制的线程安全

        // 指令解析结果
        private string _commandName;       // 指令名称（如"RELMove"）
        private string _axisName;          // 要运动的轴名称
        private double _relativeDistance;  // 相对移动距离
        private double _speed;             // 运动速度
        private int _timeoutSeconds;       // 超时时间（秒）

        // 运动参数与状态跟踪
        private MotionAxisDto _axis;       // 运动轴数据传输对象
        private double _currentPosition;   // 轴运动后的当前位置
        private double _targetPosition;    // 目标位置（初始位置 + 相对距离）
        private DateTime? _positionStableStartTime; // 位置开始稳定的时间点
        private double _lastPosition;      // 上一次检测的位置，用于稳定性判断
        #endregion

        /// <summary>
        /// 初始化相对运动流程
        /// </summary>
        /// <param name="process">相对运动指令字符串</param>
        /// <param name="processTip">相对运动指令提示信息</param>
        public RELMoveProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                CurrentStatus = GeneralStatus.Wait; // 初始状态设为等待
                Message = "Wait";                  // 初始消息设为等待
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);      // 解析相对运动指令
                BindAxisParameter();        // 绑定轴参数
            }
            catch (Exception ex)
            {
                Message = ex.Message;       // 记录异常消息
                CurrentStatus = GeneralStatus.Error; // 状态设为错误
                RLog.SaveError(ex);         // 保存错误日志
                return; // 设置错误状态后立即返回
            }
        }

        /// <summary>
        /// 克隆方法，支持副流程的缓存机制
        /// 确保多实例并发时的状态独立性
        /// </summary>
        public override object Clone()
        {
            var clone = new RELMoveProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _axisName = this._axisName,
                _relativeDistance = this._relativeDistance,
                _speed = this._speed,
                _timeoutSeconds = this._timeoutSeconds,
                _axis = this._axis
            };

            // 重置状态，确保克隆实例的独立性
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";
            clone._positionStableStartTime = null;
            clone._lastPosition = 0;
            clone._targetPosition = 0;

            return clone;
        }

        /// <summary>
        /// 解析相对运动指令字符串，提取各部分信息
        /// </summary>
        /// <param name="process">待解析的相对运动指令</param>
        private void ParseCommand(string process)
        {
            // 按竖线分割指令，移除空条目
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式是否正确
            if (commandParts.Length < 5)
            {
                HandleMotionError($"指令格式错误: {process}，至少需要5个部分，格式为RELMove|轴名称|相对距离|速度|超时");
                return; // 设置错误后立即返回
            }

            // 提取指令各部分信息并进行格式转换
            _commandName = commandParts[0];
            _axisName = commandParts[1];

            // 解析相对距离（数值转换）
            if (!double.TryParse(commandParts[2], out _relativeDistance))
            {
                HandleMotionError($"相对距离格式错误: {commandParts[2]}");
                return; // 设置错误后立即返回
            }

            // 解析运动速度（数值转换）
            if (!double.TryParse(commandParts[3], out _speed))
            {
                HandleMotionError($"速度格式错误: {commandParts[3]}");
                return; // 设置错误后立即返回
            }

            // 解析超时时间（数值转换）
            if (!int.TryParse(commandParts[4], out _timeoutSeconds))
            {
                HandleMotionError($"超时格式错误: {commandParts[4]}");
                return; // 设置错误后立即返回
            }
        }

        /// <summary>
        /// 绑定轴参数，从系统中获取轴的详细配置
        /// </summary>
        private void BindAxisParameter()
        {
            // 从运动系统中查找匹配的轴信息
            _axis = MotionSystem.Instance.MotionAxes
                .FirstOrDefault(a => a.轴名称 == _axisName);

            // 如果未找到匹配的轴，处理错误
            if (_axis == null)
            {
                HandleMotionError($"未找到名称为[{_axisName}]的轴信息");
                return; // 设置错误后立即返回
            }

            ProcessId = _axis.绑定代码; // 设置流程ID
        }

        /// <summary>
        /// 处理运动错误，更新机台状态并记录错误日志
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        private void HandleMotionError(string errorMessage)
        {
            // 更新机台状态为紧急停止
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            // 保存错误日志
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                _process,
                $"--->>错误：{errorMessage}！！！！"
            );
            // 设置错误状态
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
        }

        /// <summary>
        /// 执行相对运动的主方法
        /// </summary>
        public override void Run()
        {
            try
            {
                CurrentStatus = GeneralStatus.Running; // 设置状态为运行中
                Message = "Running";

                // 测试模式直接返回成功，不实际执行运动
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

                // 初始化位置跟踪变量
                _positionStableStartTime = null;
                _lastPosition = 0;
                _targetPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis) + _relativeDistance;

                var startTime = DateTime.Now; // 记录运动开始时间

                // 执行相对运动
                ExecuteRelativeMove(startTime);

                // 如果运动成功完成，更新完成消息
                if (CurrentStatus == GeneralStatus.NoError)
                {
                    UpdateCompletionMessage();
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;       // 记录异常消息
                CurrentStatus = GeneralStatus.Error; // 设置状态为错误
                RLog.SaveError(ex);         // 保存错误日志
                return; // 设置错误后立即返回
            }
        }

        /// <summary>
        /// 执行相对运动并等待完成及位置稳定
        /// 核心逻辑：发送相对运动指令 → 等待运动完成 → 等待位置稳定
        /// </summary>
        /// <param name="startTime">运动开始时间，用于超时判断</param>
        private void ExecuteRelativeMove(DateTime startTime)
        {
            // 发送相对运动指令
            string moveResult;
            lock (_motionLock) // 加锁确保线程安全
            {
                moveResult = MotionSystem.Instance.MotionController
                    .RelMove(_axis, _relativeDistance);
            }

            // 检查运动指令是否成功发送
            if (moveResult != "NoError")
            {
                CurrentStatus = GeneralStatus.Error;
                Message = $"轴[{_axisName}]相对运动指令失败: {moveResult}";
                return; // 设置错误后立即返回
            }

            // 第一阶段：等待运动机构完成物理移动
            while (true)
            {
                Thread.Sleep(MotionDelayMs);
                CurrentStatus = GeneralStatus.Running;

                // 检查异常状态
                if (CheckMachineStatus(startTime) || CheckTimeout(startTime))
                {
                    return;
                }

                // 运动完成后进入位置稳定检查阶段
                if (MotionSystem.Instance.MotionController.CheckMoveDone(_axis))
                {
                    _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                    _lastPosition = _currentPosition;
                    break;
                }
            }

            // 第二阶段：等待位置信号稳定
            while (true)
            {
                Thread.Sleep(MotionDelayMs);
                CurrentStatus = GeneralStatus.Running;

                // 检查异常状态
                if (CheckMachineStatus(startTime) || CheckTimeout(startTime))
                {
                    return;
                }

                // 检查位置是否稳定
                if (CheckPositionStability())
                {
                    _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                    CurrentStatus = GeneralStatus.NoError;
                    break;
                }
            }
        }

        /// <summary>
        /// 检查位置是否稳定
        /// 判定标准：位置在目标范围内且连续稳定达到设定时间
        /// </summary>
        private bool CheckPositionStability()
        {
            _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);

            // 1. 检查当前位置是否在目标位置的允许误差范围内
            bool isWithinTolerance = Math.Abs(_currentPosition - _targetPosition) <= PositionTolerance;

            // 2. 检查位置变化是否在允许范围内（过滤微小振动）
            bool isPositionStable = Math.Abs(_currentPosition - _lastPosition) <= PositionTolerance;

            // 更新上一次位置记录
            _lastPosition = _currentPosition;

            // 不在误差范围内直接重置计时器
            if (!isWithinTolerance || !isPositionStable)
            {
                _positionStableStartTime = null;
                return false;
            }

            // 开始计时或检查是否达到稳定时间
            if (!_positionStableStartTime.HasValue)
            {
                _positionStableStartTime = DateTime.Now;
                return false;
            }

            // 检查是否达到设定的稳定时间
            var stableDuration = DateTime.Now - _positionStableStartTime.Value;
            return stableDuration.TotalMilliseconds >= PositionStableTimeMs;
        }

        /// <summary>
        /// 检查机台状态，处理紧急停止和报警情况
        /// </summary>
        /// <param name="startTime">运动开始时间</param>
        /// <returns>true表示机台状态异常，false表示正常</returns>
        private bool CheckMachineStatus(DateTime startTime)
        {
            // 处理紧急停止情况
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：机台紧急停止报警\r\n{_axisName}轴实际点位：{_currentPosition}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0); // 停止当前轴运动
                return true;
            }

            // 处理机台报警情况
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming)
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：机台报警\r\n{_axisName}轴实际点位：{_currentPosition}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0); // 停止当前轴运动
                return true;
            }

            // 检查轴是否报警
            if (MotionSystem.Instance.MotionController.CheckServoAlarm(_axis))
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"运动中发生伺服报警\r\n当前位置: {_currentPosition}";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0); // 停止当前轴运动
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查运动是否超时
        /// </summary>
        /// <param name="startTime">运动开始时间</param>
        /// <returns>true表示超时，false表示未超时</returns>
        private bool CheckTimeout(DateTime startTime)
        {
            // 检查是否超过设定的超时时间
            if (DateTime.Now.Subtract(startTime).TotalSeconds > _timeoutSeconds)
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：运动超时报警\r\n{_axisName}轴实际点位：{_currentPosition}\r\n目标位置：{_targetPosition}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0); // 停止当前轴运动
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新运动完成消息，包含位置稳定信息
        /// </summary>
        private void UpdateCompletionMessage()
        {
            Message = $"NoError\r\n{_axisName}轴相对运动完成（位置稳定{PositionStableTimeMs}ms）\r\n" +
                      $"目标位置：{_targetPosition:F3}，实际位置：{_currentPosition:F3}，误差：{Math.Abs(_currentPosition - _targetPosition):F3}\r\n";
            CurrentStatus = GeneralStatus.NoError; // 设置状态为无错误
        }
    }
}
