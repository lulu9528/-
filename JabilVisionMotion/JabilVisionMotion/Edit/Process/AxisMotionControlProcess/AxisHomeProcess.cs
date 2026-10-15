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
    /// 轴回零流程类，继承自ProcessBase基类
    /// 指令格式：HomeMove|轴名称|超时（单位：秒）
    /// 特性：控制指定轴执行回零操作，等待回零传感器信号稳定后确认完成
    /// 增加信号稳定检查，避免传感器抖动导致的误判
    /// </summary>
    public class AxisHomeProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>运动状态检查间隔（毫秒）</summary>
        private const int MotionDelayMs = 1;
        /// <summary>回零完成后延迟时间（毫秒）</summary>
        private const int PostHomeDelayMs = 1000;
        /// <summary>回零传感器状态位索引</summary>
        private const int HomeSensorBitIndex = 3;
        /// <summary>回零传感器信号稳定时间（毫秒）</summary>
        /// <remarks>确保传感器信号持续稳定后再确认回零完成</remarks>
        private const int HomeSensorStableTimeMs = 1000;
        #endregion

        #region 私有字段
        private readonly string _process;
        private readonly string _processTip;
        private static readonly object _motionLock = new object();

        // 指令解析结果
        private string _commandName;
        private string _axisName;
        private int _timeoutSeconds;

        // 运动参数与状态跟踪
        private MotionAxisDto _axis;
        private double _currentPosition;
        private DateTime? _homeSensorStableStartTime; // 传感器信号开始稳定的时间
        private bool _lastHomeSensorState;            // 上一次检测的传感器状态
        #endregion

        /// <summary>
        /// 初始化轴回零流程
        /// </summary>
        public AxisHomeProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                CurrentStatus = GeneralStatus.Wait;
                Message = "Wait";
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);
                BindAxisParameter();
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                return;
            }
        }

        /// <summary>
        /// 克隆方法，确保多实例状态隔离
        /// </summary>
        public override object Clone()
        {
            var clone = new AxisHomeProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _axisName = this._axisName,
                _timeoutSeconds = this._timeoutSeconds,
                _axis = this._axis
            };
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";
            return clone;
        }

        /// <summary>
        /// 解析回零指令参数
        /// </summary>
        private void ParseCommand(string process)
        {
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            if (commandParts.Length < 3)
            {
                HandleMotionError($"指令格式错误: {process}，至少需要3个部分，格式为HomeMove|轴名称|超时");
                return;
            }

            _commandName = commandParts[0];
            _axisName = commandParts[1];

            if (!int.TryParse(commandParts[2], out _timeoutSeconds))
            {
                HandleMotionError($"超时格式错误: {commandParts[2]}");
                return;
            }
        }

        /// <summary>
        /// 绑定轴参数
        /// </summary>
        private void BindAxisParameter()
        {
            _axis = MotionSystem.Instance.MotionAxes
                .FirstOrDefault(a => a.轴名称 == _axisName);

            if (_axis == null)
            {
                HandleMotionError($"未找到名称为[{_axisName}]的轴信息");
                return;
            }

            ProcessId = _axis.绑定代码;
        }

        /// <summary>
        /// 处理回零初始化错误
        /// </summary>
        private void HandleMotionError(string errorMessage)
        {
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                _process,
                $"--->>错误：{errorMessage}！！！！"
            );

            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
        }

        /// <summary>
        /// 执行轴回零操作（核心方法）
        /// </summary>
        public override void Run()
        {
            try
            {
                CurrentStatus = GeneralStatus.Running;
                Message = "Running";

                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                if (_axis == null)
                {
                    Message = $"错误的指令：{_process}\r\n报错原因：轴[{_axisName}]未绑定";
                    CurrentStatus = GeneralStatus.Error;
                    return;
                }

                // 初始化信号跟踪变量
                _homeSensorStableStartTime = null;
                _lastHomeSensorState = false;

                var startTime = DateTime.Now;
                ExecuteHoming(startTime);

                if (CurrentStatus == GeneralStatus.NoError)
                {
                    UpdateCompletionMessage();
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                return;
            }
        }

        /// <summary>
        /// 执行回零操作并等待完成
        /// </summary>
        private void ExecuteHoming(DateTime startTime)
        {
            string operationResult;
            lock (_motionLock)
            {
                // 伺服使能
                operationResult = MotionSystem.Instance.MotionController.ServoSet(_axis, 1);
                if (operationResult != "NoError")
                {
                    CurrentStatus = GeneralStatus.Error;
                    Message = $"轴[{_axisName}]伺服使能失败: {operationResult}";
                    return;
                }

                // 启动回零
                operationResult = MotionSystem.Instance.MotionController.GoHome(_axis);
                if (operationResult != "NoError")
                {
                    CurrentStatus = GeneralStatus.Error;
                    Message = $"轴[{_axisName}]回零指令启动失败: {operationResult}";
                    return;
                }
            }

            // 等待回零完成（含信号稳定检查）
            while (true)
            {
                Thread.Sleep(MotionDelayMs);
                CurrentStatus = GeneralStatus.Running;

                if (CheckMachineStatus(startTime) || CheckTimeout(startTime))
                {
                    return;
                }

                // 检查回零是否完成（运动停止 + 信号稳定）
                if (IsMotionStopped() && CheckHomeSensorStability())
                {
                    CompleteHoming();
                    if (CurrentStatus == GeneralStatus.Error)
                        return;
                    CurrentStatus = GeneralStatus.NoError;
                    break;
                }
            }
        }

        /// <summary>
        /// 检查回零传感器信号稳定性
        /// 信号需持续稳定达到设定时间（HomeSensorStableTimeMs）
        /// </summary>
        private bool CheckHomeSensorStability()
        {
            var motionStatus = MotionSystem.Instance.MotionController.GetMotionSts(_axis);
            bool currentSensorState = motionStatus.Length > HomeSensorBitIndex
                && motionStatus[HomeSensorBitIndex];

            // 传感器状态变化：重新计时
            if (currentSensorState != _lastHomeSensorState)
            {
                _homeSensorStableStartTime = currentSensorState ? DateTime.Now : (DateTime?)null;
                _lastHomeSensorState = currentSensorState;
                return false;
            }

            // 传感器未触发：重置计时器
            if (!currentSensorState)
            {
                _homeSensorStableStartTime = null;
                return false;
            }

            // 传感器已触发：开始计时或检查是否达到稳定时间
            if (!_homeSensorStableStartTime.HasValue)
            {
                _homeSensorStableStartTime = DateTime.Now;
                return false;
            }

            // 检查是否达到稳定时间
            var stableDuration = DateTime.Now - _homeSensorStableStartTime.Value;
            return stableDuration.TotalMilliseconds >= HomeSensorStableTimeMs;
        }

        /// <summary>
        /// 检查轴是否已停止运动
        /// </summary>
        private bool IsMotionStopped()
        {
            return MotionSystem.Instance.MotionController.CheckMoveDone(_axis)
                && MotionSystem.Instance.MotionController.CheckMoveHomeDone(_axis);
        }

        /// <summary>
        /// 完成回零操作（设置零点位置）
        /// </summary>
        private void CompleteHoming()
        {
            Thread.Sleep(PostHomeDelayMs);

            var setPosResult = MotionSystem.Instance.MotionController.SetPos(0, _axis);
            if (setPosResult != "NoError")
            {
                CurrentStatus = GeneralStatus.Error;
                Message = $"轴[{_axisName}]零点设置失败: {setPosResult}";
                return;
            }

            _currentPosition = 0;
        }

        /// <summary>
        /// 检查机台状态（紧急停止/报警）
        /// </summary>
        private bool CheckMachineStatus(DateTime startTime)
        {
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：机台紧急停止报警\r\n{_axisName}轴实际点位：{_currentPosition}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0);
                return true;
            }

            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming)
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：机台报警\r\n{_axisName}轴实际点位：{_currentPosition}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0);
                return true;
            }

            if (MotionSystem.Instance.MotionController.CheckServoAlarm(_axis))
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：运动轴报警\r\n{_axisName}轴实际点位：{_currentPosition}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查回零超时
        /// </summary>
        private bool CheckTimeout(DateTime startTime)
        {
            if (DateTime.Now.Subtract(startTime).TotalSeconds > _timeoutSeconds)
            {
                _currentPosition = MotionSystem.Instance.MotionController.GetEncPos(_axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：回零超时报警\r\n{_axisName}轴实际点位：{_currentPosition}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(_axis, 0);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新回零完成消息
        /// </summary>
        private void UpdateCompletionMessage()
        {
            Message = $"NoError\r\n{_axisName}轴已回零（信号稳定{HomeSensorStableTimeMs}ms），当前点位：{_currentPosition}\r\n";
            CurrentStatus = GeneralStatus.NoError;
        }
    }
}
