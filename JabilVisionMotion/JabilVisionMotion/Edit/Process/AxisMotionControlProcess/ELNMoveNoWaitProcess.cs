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
    /// 轴运动到负极限不等待流程类，继承自ProcessBase基类
    /// 指令格式：ELNMoveNoWait|轴名称|速度|超时（单位：秒）
    /// 特性：控制轴以设定速度向负极限方向运动，发送运动指令后立即返回，不等待极限传感器触发
    /// 与ELNMoveProcess的核心区别：不等待运动结果，适用于需要并行控制多个轴运动的场景
    /// </summary>
    public class ELNMoveNoWaitProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>负向运动方向参数（约定值，0表示负向，具体含义由运动控制器定义）</summary>
        private const int NegativeDirection = 0;
        #endregion

        #region 私有字段
        private readonly string _process;        // 原始负极限不等待运动指令字符串
        private readonly string _processTip;     // 负极限不等待运动指令提示信息
        private static readonly object _motionLock = new object(); // 线程锁，确保指令发送的线程安全

        // 指令解析结果
        private string _commandName;       // 指令名称（如"ELNMoveNoWait"）
        private string _axisName;          // 要运动的轴名称
        private double _moveSpeed;         // 运动速度
        private int _timeoutSeconds;       // 超时参数（保留字段，以备后续扩展使用）

        // 运动参数
        private MotionAxisDto _axis;       // 运动轴的配置信息
        private double _currentPosition;   // 到达负极限前的位置

        #endregion

        /// <summary>
        /// 初始化轴运动到负极限不等待流程
        /// </summary>
        /// <param name="process">负极限不等待运动指令字符串</param>
        /// <param name="processTip">负极限不等待运动指令提示信息</param>
        public ELNMoveNoWaitProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                CurrentStatus = GeneralStatus.Wait; // 初始状态设为等待
                Message = "Wait";                  // 初始消息设为等待
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);      // 解析负极限不等待运动指令
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
            var clone = new ELNMoveNoWaitProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _axisName = this._axisName,
                _moveSpeed = this._moveSpeed,
                _timeoutSeconds = this._timeoutSeconds,
                _axis = this._axis // 轴配置可共享，状态独立
            };

            // 重置状态，确保克隆实例的独立性
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";

            return clone;
        }

        /// <summary>
        /// 解析负极限不等待运动指令参数
        /// 提取轴名称、运动速度和超时时间（超时为保留参数）
        /// </summary>
        /// <param name="process">待解析的负极限不等待运动指令</param>
        private void ParseCommand(string process)
        {
            // 按竖线分割指令，移除空条目
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式是否正确
            if (commandParts.Length < 4)
            {
                HandleMotionError($"指令格式错误: {process}，至少需要4个部分，格式为ELNMoveNoWait|轴名称|速度|超时");
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

            // 解析超时时间（转换为整数，作为保留参数）
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
        /// 检查机台状态（紧急停止/报警）
        /// 异常状态下停止运动并记录信息
        /// </summary>
        private bool CheckMachineStatus()
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
        /// 执行轴运动到负极限不等待操作（核心方法）
        /// 仅发送运动指令，不等待传感器触发或运动完成
        /// </summary>
        public override void Run()
        {
            try
            {
                CurrentStatus = GeneralStatus.Running; // 状态设为运行中
                Message = "Running";

                // 测试模式：直接返回成功（跳过实际指令发送）
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
                // 检查机台状态（紧急停止/报警）
                if (CheckMachineStatus())
                {
                    return; // 异常情况下终止运动
                }

                // 发送向负极限运动指令（不等待完成）
                SendNegativeLimitMoveCommand();

                // 若指令发送成功，更新状态为完成
                if (CurrentStatus == GeneralStatus.Running)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = $"NoError\r\n{_axisName}轴向负极限运动指令已发送";
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
        /// 发送向负极限运动指令（核心操作）
        /// 仅负责发送指令并检查指令是否成功发送，不监控后续运动状态
        /// </summary>
        private void SendNegativeLimitMoveCommand()
        {
            lock (_motionLock) // 加锁确保线程安全，避免并发指令冲突
            {
                // 发送负向点动指令（0表示负向运动方向）
                string moveResult = MotionSystem.Instance.MotionController
                    .JogMove(_axis, NegativeDirection);

                // 仅检查指令是否成功发送，不关心运动是否完成
                if (moveResult != "NoError")
                {
                    CurrentStatus = GeneralStatus.Error;
                    Message = $"轴[{_axisName}]启动向负极限运动失败: {moveResult}";
                    return; // 设置错误后立即返回
                }
            }
        }
    }
}
