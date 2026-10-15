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
    /// 到位状态判断流程类，继承自ProcessBase基类
    /// 指令格式：CheckMoveDone|点位类型|点位定义|点位名称|超时
    /// 超时（单位：S）：超过设定秒数后触发超时报警
    /// 特性：检查各轴是否到达目标位置，未到位则补送运动指令并等待到位及位置稳定
    /// 增加位置信号稳定检查，确保各轴在目标位置稳定后才判定为到位
    /// </summary>
    public class CheckMoveDoneProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>位置误差允许值，用于判断是否到达目标位置</summary>
        private const double PositionTolerance = 0.1;

        /// <summary>到位检查延迟时间(毫秒)，控制位置检查的频率</summary>
        private const int CheckDelayMs = 1;

        /// <summary>位置稳定判定时间(毫秒)，位置需在此时间内保持稳定</summary>
        private const int PositionStableTimeMs = 100;
        #endregion

        #region 私有字段
        private readonly string _process;        // 原始判断指令字符串
        private readonly string _processTip;     // 判断指令提示信息
        private static readonly object _motionLock = new object(); // 线程锁，确保运动控制的线程安全

        // 指令解析结果
        private string _commandName;       // 指令名称（如"CheckMoveDone"）
        private string _pointType;         // 点位类型
        private string _pointDefinition;   // 点位定义
        private string _pointName;         // 点位名称
        private int _timeoutSeconds;       // 超时时间（秒）

        // 运动参数
        private MotionPointDto _motionPoint; // 运动点位数据传输对象
        private MotionAxisDto _axisX;       // X轴数据传输对象
        private MotionAxisDto _axisY;       // Y轴数据传输对象
        private MotionAxisDto _axisZ;       // Z轴数据传输对象
        private MotionAxisDto _axisU;       // U轴数据传输对象
        private MotionAxisDto _axisR;       // R轴数据传输对象

        // 位置反馈与稳定跟踪
        private double _posX;  // X轴实际位置
        private double _posY;  // Y轴实际位置
        private double _posZ;  // Z轴实际位置
        private double _posU;  // U轴实际位置
        private double _posR;  // R轴实际位置

        // 各轴位置稳定跟踪变量
        private DateTime? _xStableStartTime;
        private DateTime? _yStableStartTime;
        private DateTime? _zStableStartTime;
        private DateTime? _uStableStartTime;
        private DateTime? _rStableStartTime;

        private double _lastPosX;
        private double _lastPosY;
        private double _lastPosZ;
        private double _lastPosU;
        private double _lastPosR;
        #endregion

        /// <summary>
        /// 初始化到位状态判断流程
        /// </summary>
        /// <param name="process">判断指令字符串</param>
        /// <param name="processTip">判断指令提示信息</param>
        public CheckMoveDoneProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                CurrentStatus = GeneralStatus.Wait; // 初始状态设为等待
                Message = "Wait";                  // 初始消息设为等待
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);      // 解析判断指令
                BindMotionParameters();     // 绑定运动参数
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
            var clone = new CheckMoveDoneProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _pointType = this._pointType,
                _pointDefinition = this._pointDefinition,
                _pointName = this._pointName,
                _timeoutSeconds = this._timeoutSeconds,
                _motionPoint = this._motionPoint,
                _axisX = this._axisX,
                _axisY = this._axisY,
                _axisZ = this._axisZ,
                _axisU = this._axisU,
                _axisR = this._axisR
            };
            // 重置状态，确保克隆实例的独立性
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";
            clone.ResetStabilityTracking();

            return clone;
        }

        /// <summary>
        /// 重置所有轴的位置稳定跟踪变量
        /// </summary>
        private void ResetStabilityTracking()
        {
            _xStableStartTime = null;
            _yStableStartTime = null;
            _zStableStartTime = null;
            _uStableStartTime = null;
            _rStableStartTime = null;

            _lastPosX = 0;
            _lastPosY = 0;
            _lastPosZ = 0;
            _lastPosU = 0;
            _lastPosR = 0;
        }

        /// <summary>
        /// 解析判断指令字符串，提取各部分信息
        /// </summary>
        /// <param name="process">待解析的判断指令</param>
        private void ParseCommand(string process)
        {
            // 按竖线分割指令，移除空条目
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式是否正确
            if (commandParts.Length < 5)
            {
                HandleMotionError($"指令格式错误: {process}，至少需要5个部分");
                return; // 设置错误后立即返回
            }

            // 提取指令各部分信息
            _commandName = commandParts[0];
            _pointType = commandParts[1];
            _pointDefinition = commandParts[2];
            _pointName = commandParts[3];

            // 解析超时时间
            if (!int.TryParse(commandParts[4], out _timeoutSeconds))
            {
                HandleMotionError($"超时格式错误: {commandParts[4]}");
                return; // 设置错误后立即返回
            }
        }

        /// <summary>
        /// 绑定运动参数和轴信息，从系统中获取点位和轴的详细配置
        /// </summary>
        private void BindMotionParameters()
        {
            // 从运动系统中查找匹配的点位信息
            _motionPoint = MotionSystem.Instance.MotionPoints
                .FirstOrDefault(p => p.点位名称 == _pointName
                                 && p.点位类型 == _pointType
                                 && p.点位定义 == _pointDefinition);

            // 如果未找到匹配点位，处理错误
            if (_motionPoint == null)
            {
                HandleMotionError("未找到匹配的运动点位信息");
                return; // 设置错误后立即返回
            }

            ProcessId = _motionPoint.绑定代码; // 设置流程ID

            // 绑定各轴信息
            _axisX = BindAxis(_motionPoint.X轴绑定轴);
            _axisY = BindAxis(_motionPoint.Y轴绑定轴);
            _axisZ = BindAxis(_motionPoint.Z轴绑定轴);
            _axisU = BindAxis(_motionPoint.U轴绑定轴);
            _axisR = BindAxis(_motionPoint.R轴绑定轴);
        }

        /// <summary>
        /// 绑定轴信息（处理"未绑定"情况）
        /// </summary>
        /// <param name="axisName">轴名称</param>
        /// <returns>轴数据传输对象，未绑定则返回null</returns>
        private MotionAxisDto BindAxis(string axisName)
        {
            // 处理未绑定情况
            if (string.IsNullOrWhiteSpace(axisName) || axisName.Contains("未绑定"))
                return null;

            // 从运动系统中查找匹配的轴信息
            var axis = MotionSystem.Instance.MotionAxes
                .FirstOrDefault(a => a.轴名称 == axisName);

            // 如果未找到轴，处理错误
            if (axis == null)
            {
                HandleMotionError($"未找到名称为[{axisName}]的轴信息");
                return null; // 设置错误后立即返回
            }

            return axis;
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

            // 设置错误状态，不再抛出异常
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
        }

        /// <summary>
        /// 执行到位状态判断的主方法
        /// 核心逻辑：检查各轴是否到达目标位置并保持稳定，未到位则补送指令
        /// </summary>
        public override void Run()
        {
            try
            {
                CurrentStatus = GeneralStatus.Running; // 设置状态为运行中
                Message = "Running";

                // 测试模式直接返回成功
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                // 检查是否有需要判断的轴
                if (NoAxesToCheck())
                {
                    Message = $"错误的指令：{_process}\r\n报错原因：未绑定任何轴";
                    CurrentStatus = GeneralStatus.Error;
                    return; // 设置错误后立即返回
                }

                // 重置所有轴的稳定跟踪变量
                ResetStabilityTracking();

                var startTime = DateTime.Now; // 记录检查开始时间

                // 按特定顺序检查各轴到位状态并等待稳定（Z→U→R→X→Y）
                CheckAxisPositionAndStability(_axisZ, () => _motionPoint.Z轴目标位置,
                                             () => _motionPoint.Z轴运行速度, ref _posZ, ref _lastPosZ, ref _zStableStartTime, startTime);
                if (CurrentStatus == GeneralStatus.Error) return;

                CheckAxisPositionAndStability(_axisU, () => _motionPoint.U轴目标位置,
                                             () => _motionPoint.U轴运行速度, ref _posU, ref _lastPosU, ref _uStableStartTime, startTime);
                if (CurrentStatus == GeneralStatus.Error) return;

                CheckAxisPositionAndStability(_axisR, () => _motionPoint.R轴目标位置,
                                             () => _motionPoint.R轴运行速度, ref _posR, ref _lastPosR, ref _rStableStartTime, startTime);
                if (CurrentStatus == GeneralStatus.Error) return;

                CheckAxisPositionAndStability(_axisX, () => _motionPoint.X轴目标位置,
                                             () => _motionPoint.X轴运行速度, ref _posX, ref _lastPosX, ref _xStableStartTime, startTime);
                if (CurrentStatus == GeneralStatus.Error) return;

                CheckAxisPositionAndStability(_axisY, () => _motionPoint.Y轴目标位置,
                                             () => _motionPoint.Y轴运行速度, ref _posY, ref _lastPosY, ref _yStableStartTime, startTime);
                if (CurrentStatus == GeneralStatus.Error) return;

                // 所有轴都已到位并稳定，更新完成消息
                UpdateCompletionMessage();
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
        /// 检查是否没有需要判断的轴（所有轴都未绑定）
        /// </summary>
        /// <returns>true表示没有轴需要判断，false表示有轴需要判断</returns>
        private bool NoAxesToCheck()
        {
            return _axisX == null && _axisY == null && _axisZ == null &&
                   _axisU == null && _axisR == null;
        }

        /// <summary>
        /// 检查轴位置并确保到位且稳定
        /// 核心功能：检查当前位置，未到位则补送运动指令，到位后等待位置稳定
        /// </summary>
        /// <param name="axis">要检查的轴</param>
        /// <param name="getTargetPos">获取目标位置的委托</param>
        /// <param name="getSpeed">获取运动速度的委托</param>
        /// <param name="currentPos">当前位置引用</param>
        /// <param name="lastPos">上一次位置引用</param>
        /// <param name="stableStartTime">稳定开始时间引用</param>
        /// <param name="startTime">检查开始时间</param>
        private void CheckAxisPositionAndStability(MotionAxisDto axis, Func<double> getTargetPos,
                                                  Func<double> getSpeed, ref double currentPos,
                                                  ref double lastPos, ref DateTime? stableStartTime, DateTime startTime)
        {
            if (axis == null) return; // 如果轴未绑定，直接返回

            try
            {
                // 检查是否已到位，未到位则补送运动指令
                lock (_motionLock) // 加锁确保线程安全
                {
                    currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                    lastPos = currentPos;

                    // 如果位置偏差超过允许值，发送运动指令
                    if (Math.Abs(getTargetPos() - currentPos) >= PositionTolerance)
                    {
                        var moveResult = MotionSystem.Instance.MotionController
                            .AbsMove(axis, getTargetPos(), getSpeed());

                        if (moveResult != "NoError")
                        {
                            CurrentStatus = GeneralStatus.Error;
                            Message = $"轴[{axis.轴名称}]补送运动指令失败: {moveResult}";
                            return; // 设置错误后立即返回
                        }
                    }
                }

                // 等待轴到位并稳定
                while (true)
                {
                    Thread.Sleep(CheckDelayMs); // 短暂延迟后检查状态
                    CurrentStatus = GeneralStatus.Running;

                    // 检查机台状态和超时，如有异常则返回
                    if (CheckMachineStatus(axis, getTargetPos(), ref currentPos) ||
                        CheckTimeout(axis, getTargetPos(), ref currentPos, startTime))
                    {
                        return; // 异常情况下终止检查
                    }

                    // 获取当前位置
                    currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);

                    // 检查是否已运动完成且在目标位置误差范围内
                    if (MotionSystem.Instance.MotionController.CheckMoveDone(axis) &&
                        Math.Abs(getTargetPos() - currentPos) < PositionTolerance)
                    {
                        // 检查位置是否稳定
                        if (IsPositionStable(currentPos, ref lastPos, getTargetPos, ref stableStartTime))
                        {
                            CurrentStatus = GeneralStatus.NoError;
                            break; // 位置稳定，退出循环
                        }
                    }
                    else
                    {
                        // 不在目标位置范围内，重置稳定计时器
                        stableStartTime = null;
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录异常时的位置信息
                currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"轴[{axis.轴名称}]到位检查异常: {ex.Message}\r\n目标位置: {getTargetPos()}\r\n实际位置: {currentPos}";
                return; // 设置错误后立即返回
            }
        }

        /// <summary>
        /// 检查位置是否稳定
        /// </summary>
        /// <param name="currentPos">当前位置</param>
        /// <param name="lastPos">上一次位置引用</param>
        /// <param name="getTargetPos">目标位置</param>
        /// <param name="stableStartTime">稳定开始时间引用</param>
        /// <returns>true表示位置已稳定，false表示未稳定</returns>
        private bool IsPositionStable(double currentPos, ref double lastPos, Func<double> getTargetPos, ref DateTime? stableStartTime)
        {
            // 1. 检查当前位置与目标位置的偏差
            bool isWithinTargetTolerance = Math.Abs(currentPos - getTargetPos()) < PositionTolerance;

            // 2. 检查当前位置与上一次位置的变化量
            bool isWithinChangeTolerance = Math.Abs(currentPos - lastPos) < PositionTolerance;

            // 更新上一次位置记录
            lastPos = currentPos;

            // 位置不稳定，重置计时器
            if (!isWithinTargetTolerance || !isWithinChangeTolerance)
            {
                stableStartTime = null;
                return false;
            }

            // 开始计时
            if (!stableStartTime.HasValue)
            {
                stableStartTime = DateTime.Now;
                return false;
            }

            // 检查是否达到稳定时间要求
            var stableDuration = DateTime.Now - stableStartTime.Value;
            return stableDuration.TotalMilliseconds >= PositionStableTimeMs;
        }

        /// <summary>
        /// 检查机台状态，处理紧急停止和报警情况
        /// </summary>
        /// <param name="axis">当前检查的轴</param>
        /// <param name="targetPos">目标位置</param>
        /// <param name="currentPos">当前位置引用</param>
        /// <returns>true表示机台状态异常，false表示正常</returns>
        private bool CheckMachineStatus(MotionAxisDto axis, double targetPos, ref double currentPos)
        {
            // 处理紧急停止情况
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：机台紧急停止报警\r\n{axis.轴名称}轴目标点位：{targetPos}\r\n{axis.轴名称}轴实际点位：{currentPos}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(axis, 0); // 停止当前轴运动
                return true;
            }

            // 处理机台报警情况
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming)
            {
                currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：机台报警\r\n{axis.轴名称}轴目标点位：{targetPos}\r\n{axis.轴名称}轴实际点位：{currentPos}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(axis, 0); // 停止当前轴运动
                return true;
            }

            // 检查轴是否报警
            if (MotionSystem.Instance.MotionController.CheckServoAlarm(axis))
            {
                currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"运动中发生伺服报警\r\n当前位置: {currentPos}";
                MotionSystem.Instance.MotionController.SmtStop(axis, 0); // 停止当前轴运动
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查到位判断是否超时
        /// </summary>
        /// <param name="axis">当前检查的轴</param>
        /// <param name="targetPos">目标位置</param>
        /// <param name="currentPos">当前位置引用</param>
        /// <param name="startTime">检查开始时间</param>
        /// <returns>true表示超时，false表示未超时</returns>
        private bool CheckTimeout(MotionAxisDto axis, double targetPos, ref double currentPos, DateTime startTime)
        {
            // 检查是否超过设定的超时时间
            if (DateTime.Now.Subtract(startTime).TotalSeconds > _timeoutSeconds)
            {
                currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：到位检查超时\r\n{axis.轴名称}轴目标点位：{targetPos}\r\n{axis.轴名称}轴实际点位：{currentPos}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(axis, 0); // 停止当前轴运动
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新所有轴到位完成消息，汇总各轴实际位置和稳定信息
        /// </summary>
        private void UpdateCompletionMessage()
        {
            var message = new StringBuilder();
            message.AppendLine("NoError");
            message.AppendLine($"各轴已到达目标位置并稳定{PositionStableTimeMs}ms：");
            message.AppendLine($"X轴实际点位：  {_posX:F3}");
            message.AppendLine($"Y轴实际点位：  {_posY:F3}");
            message.AppendLine($"Z轴实际点位：  {_posZ:F3}");
            message.AppendLine($"U轴实际点位：  {_posU:F3}");
            message.AppendLine($"R轴实际点位：  {_posR:F3}");

            Message = message.ToString();
            CurrentStatus = GeneralStatus.NoError; // 设置状态为无错误
        }
    }
}
