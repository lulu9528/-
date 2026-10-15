using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using JabilVisionMotion.Lib;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace JabilVisionMotion.Edit.Process
{
    /// <summary>
    /// 延时流程处理类，继承自ProcessBase基类
    /// 功能：提供可控的延时功能，支持在延时过程中实时响应急停信号和取消操作
    /// 指令格式：TimeDelay|延时类型|延时时间(ms)
    /// 示例：TimeDelay|默认|1000  （执行默认类型的1000毫秒延时）
    /// 支持的延时类型：当前仅支持"默认"类型
    /// 核心特性：分段检查延时进度，兼顾延时精度和响应速度，支持多实例并发
    /// </summary>
    public class TimeDelay : ProcessBase, ICloneable
    {
        #region 常量定义
        private const string DefaultDelayType = "默认";
        private const int DelayCheckIntervalMs = 10; // 平衡精度与响应速度的检查间隔
        #endregion

        #region 私有字段
        private readonly string _process;        // 原始延时指令字符串
        private readonly string _processTip;     // 指令提示信息（用于日志）
        private string _commandName;             // 指令名称（固定为"TimeDelay"）
        private string _delayType;               // 延时类型
        private int _delayTimeMs;                // 延时时间（毫秒）
        #endregion

        /// <summary>
        /// 初始化延时流程
        /// </summary>
        public TimeDelay(string process, string processTip) : base(process, processTip)
        {
            try
            {
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);
                CurrentStatus = GeneralStatus.Wait;
                Message = "Wait";
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);
                return; // 初始化错误时立即返回
            }
        }

        /// <summary>
        /// 克隆构造函数，用于深克隆
        /// </summary>
        private TimeDelay(TimeDelay source) : base(source._process, source._processTip)
        {
            _commandName = source._commandName;
            _delayType = source._delayType;
            _delayTimeMs = source._delayTimeMs;
            CurrentStatus = GeneralStatus.Wait;
            Message = "Wait";
        }

        /// <summary>
        /// 克隆方法，确保多实例并发时的状态独立性
        /// </summary>
        public override object Clone() => new TimeDelay(this);

        /// <summary>
        /// 解析延时指令参数
        /// </summary>
        private void ParseCommand(string process)
        {
            var commandParts = process.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            if (commandParts.Length < 3)
            {
                HandleError($"指令格式错误: {process}，正确格式：TimeDelay|延时类型|延时时间(ms)");
                return; // 错误状态下返回
            }

            _commandName = commandParts[0];
            _delayType = commandParts[1];

            if (!int.TryParse(commandParts[2], out _delayTimeMs) || _delayTimeMs < 0)
            {
                HandleError($"无效的延时时间: {commandParts[2]}（必须是非负整数）");
                return; // 错误状态下返回
            }

            if (_delayType != DefaultDelayType)
            {
                HandleError($"不支持的延时类型: {_delayType}，仅支持'{DefaultDelayType}'");
                return; // 错误状态下返回
            }
        }

        /// <summary>
        /// 执行延时操作（核心方法）
        /// </summary>
        public override void Run()
        {
            try
            {
                //// 检查当前是否已处于错误状态
                //if (CurrentStatus == GeneralStatus.Error)
                //    return;

                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                CurrentStatus = GeneralStatus.Running;
                Message = $"Running-- 延时 {_delayTimeMs} ms";

                // 根据延时类型执行对应操作
                if (_delayType == DefaultDelayType)
                    ExecuteDefaultDelay();
                else
                    HandleInvalidDelayType();

                // 如果执行过程中出现错误则不再更新状态
                if (CurrentStatus == GeneralStatus.Error)
                    return;

                CurrentStatus = GeneralStatus.NoError;
                Message = $"NoError-- 延时 {_delayTimeMs} ms 完成";
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);
                return; // 异常导致错误状态时返回
            }
        }

        /// <summary>
        /// 执行默认延时（支持报警和急停响应）
        /// </summary>
        private void ExecuteDefaultDelay()
        {
            if (_delayTimeMs <= 0) return;

            var stopwatch = Stopwatch.StartNew();
            bool isAlarming = CheckAlarming();

            // 检查是否已处于错误状态
            if (CurrentStatus == GeneralStatus.Error)
                return;

            while (stopwatch.ElapsedMilliseconds < _delayTimeMs)
            {
                // 处理报警状态变化
                bool currentAlarming = CheckAlarming();

                // 检查是否已处于错误状态
                if (CurrentStatus == GeneralStatus.Error)
                    return;

                if (isAlarming != currentAlarming)
                {
                    if (currentAlarming) stopwatch.Stop();
                    else stopwatch.Start();
                    isAlarming = currentAlarming;
                }

                // 检查急停
                if (CheckEmergencyStop())
                    return; // 急停导致错误状态，直接返回

                Thread.Sleep(DelayCheckIntervalMs);
            }
        }

        #region 辅助方法
        private void HandleInvalidDelayType()
        {
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            ProcessLog.Instance.SaveLog(_processTip, Color.Red, "紧急停止", _process,
                $"不支持的延时类型: {_delayType}");
            SetErrorStatus($"不支持的延时类型: {_delayType}");
            return; // 错误状态下返回
        }

        private bool CheckAlarming()
        {
            var status = MenuProxy.Instance.MachineRunStatus;
            if (status == MachineRunStatus.Alarming || status == MachineRunStatus.Paused)
            {
                SetErrorStatus("机台报警");
                return true;
            }
            return false;
        }

        private bool CheckEmergencyStop()
        {
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                SetErrorStatus("机台急停");
                return true;
            }
            return false;
        }

        private void SetErrorStatus(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
            // 无需额外return，调用方应处理返回逻辑
        }

        private void HandleError(string errorMessage)
        {
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            ProcessLog.Instance.SaveLog(_processTip, Color.Red, "紧急停止", _process,
                $"错误：{errorMessage}！！！！");
            SetErrorStatus(errorMessage);
            // 无需额外return，调用方应处理返回逻辑
        }
        #endregion
    }
}