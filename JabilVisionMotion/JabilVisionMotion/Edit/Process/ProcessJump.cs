using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using System;
using System.Collections.Generic;
using System.Threading;

namespace JabilVisionMotion.Edit.Process
{
    /// <summary>
    /// 流程跳转处理类，继承自ProcessBase基类
    /// 功能：根据指定的流程类型和跳转指令，返回对应的下一步流程信息，实现流程间的动态导航
    /// 指令格式：ProcessJump|流程类型|跳转流程
    /// 示例：ProcessJump|左Epson取放料流程|显示下一步流程  （查询左机械臂取放料流程的下一步操作）
    /// 支持的流程类型：
    /// - 镭雕治具供料流程、左Epson取放料流程、右Epson取放料流程
    /// - 左抽屉流程、右抽屉流程、中途打开左/右抽屉流程、Fanuc下料流程等
    /// 支持的跳转流程：当前仅支持"显示下一步流程"
    /// 核心特性：通过映射表管理不同流程的状态获取逻辑，减少代码冗余并提高扩展性
    /// </summary>
    public class ProcessJump : ProcessBase, ICloneable
    {
        #region 私有字段
        private readonly string _process;        // 原始流程跳转指令字符串
        private readonly string _processTip;     // 指令提示信息（用于日志）
        private string _commandName;             // 指令名称（固定为"ProcessJump"）
        private string _processType;             // 流程类型（如"左Epson取放料流程"）
        private string _jumpProcess;             // 跳转指令（如"显示下一步流程"）

        /// <summary>
        /// 流程类型与状态获取器的映射表
        /// 键：流程类型名称；值：获取该流程下一步状态的委托函数
        /// 作用：通过字典映射统一管理不同流程的状态获取逻辑，避免大量if-else判断
        /// </summary>
        private readonly Dictionary<string, Func<string>> _processStatusGetters;
        #endregion

        /// <summary>
        /// 初始化流程跳转实例
        /// </summary>
        /// <param name="process">流程跳转指令字符串</param>
        /// <param name="processTip">指令提示信息</param>
        public ProcessJump(string process, string processTip) : base(process, processTip)
        {
            try
            {
                // 验证输入参数非空
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                // 初始化流程状态获取器映射表，注册支持的流程类型及其状态获取逻辑
                _processStatusGetters = new Dictionary<string, Func<string>>
            {
                { "飞拍组装流程", () => Controller.Instance.StationInfo.Get_飞拍组装.ToString() },

                { "Epson1上相机流程", () => Controller.Instance.StationInfo.Get_EPSON1上相机.ToString() },
                { "Epson1下相机流程", () => Controller.Instance.StationInfo.Get_EPSON1下相机.ToString() },
                { "Epson1请求最终结果流程", () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果.ToString() },
                { "Epson2上相机流程", () => Controller.Instance.StationInfo.Get_EPSON2上相机.ToString() },
                { "Epson2下相机流程", () => Controller.Instance.StationInfo.Get_EPSON2下相机.ToString() },
                { "Epson2请求最终结果流程", () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果.ToString() },
                
                { "手动测试流程", () => Controller.Instance.StationInfo.Get_手动测试.ToString() },
            };

                ParseCommand(process);  // 解析指令参数

                // 初始化状态为"等待"
                CurrentStatus = GeneralStatus.Wait;
                Message = "Wait";
            }
            catch (Exception ex)
            {
                // 初始化失败时设置错误状态并立即返回
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);  // 记录错误日志
                return;
            }
        }

        /// <summary>
        /// 克隆方法，支持副流程的缓存机制
        /// 确保多实例并发时的状态独立性
        /// </summary>
        public override object Clone()
        {
            var clone = new ProcessJump(_process, _processTip)
            {
                _commandName = this._commandName,
                _processType = this._processType,
                _jumpProcess = this._jumpProcess
            };
            // 初始化克隆实例的状态为"等待"
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";
            return clone;
        }

        /// <summary>
        /// 解析流程跳转指令参数，提取流程类型和跳转指令
        /// </summary>
        /// <param name="process">待解析的指令字符串</param>
        private void ParseCommand(string process)
        {
            // 按竖线分割指令，移除空条目
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式（至少3个部分）
            if (commandParts.Length < 3)
            {
                SetErrorStatus($"指令格式错误: {process}，至少需要3个部分，格式为ProcessJump|流程类型|跳转流程");
                return;
            }

            // 提取各部分参数
            _commandName = commandParts[0];
            _processType = commandParts[1];
            _jumpProcess = commandParts[2];

            // 验证流程类型是否支持（检查是否在映射表中注册）
            if (!_processStatusGetters.ContainsKey(_processType))
            {
                SetErrorStatus($"不支持的流程类型: {_processType}，请检查流程配置表！！");
                return;
            }

            // 验证跳转流程是否支持（当前仅支持"显示下一步流程"）
            if (_jumpProcess != "显示下一步流程")
            {
                SetErrorStatus($"不支持的跳转流程: {_jumpProcess}，当前仅支持'显示下一步流程'");
                return;
            }
        }

        /// <summary>
        /// 执行流程跳转逻辑（核心方法）
        /// 根据解析的流程类型和跳转指令，获取并返回下一步流程信息
        /// </summary>
        public override void Run()
        {
            try
            {
                // 测试模式：直接返回成功（不实际执行跳转逻辑）
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                // 更新运行状态为"执行中"
                CurrentStatus = GeneralStatus.Running;
                Message = $"Running-- 处理[{_processType}]的[{_jumpProcess}]";

                // 执行跳转流程（如显示下一步流程）
                ExecuteJumpProcess();

                // 检查是否在执行跳转过程中已设置错误状态
                if (CurrentStatus == GeneralStatus.Error)
                    return;

                // 跳转完成，更新状态为"无错误"
                CurrentStatus = GeneralStatus.NoError;
            }
            catch (Exception ex)
            {
                // 处理执行过程中的异常
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);  // 记录错误日志
                return;
            }
        }

        /// <summary>
        /// 执行具体的跳转流程（根据跳转指令调用对应逻辑）
        /// </summary>
        private void ExecuteJumpProcess()
        {
            // 根据跳转指令类型执行对应操作（当前仅支持"显示下一步流程"）
            switch (_jumpProcess)
            {
                case "显示下一步流程":
                    ShowNextProcess();  // 获取并显示下一步流程信息
                    // 检查ShowNextProcess是否设置了错误状态
                    if (CurrentStatus == GeneralStatus.Error)
                        return;
                    break;
                default:
                    SetErrorStatus($"{_processType}--->>Error--->>不支持的跳转流程: {_jumpProcess}");
                    return;
            }
        }

        /// <summary>
        /// 显示下一步流程信息（核心逻辑）
        /// 通过映射表中的委托函数获取指定流程的下一步状态
        /// </summary>
        private void ShowNextProcess()
        {
            // 从映射表中获取当前流程类型对应的状态获取器
            if (_processStatusGetters.TryGetValue(_processType, out var statusGetter))
            {
                try
                {
                    // 调用委托函数获取下一步流程信息，并更新消息
                    string nextProcess = statusGetter.Invoke();
                    Message = $"NoError--->>>{nextProcess}";
                }
                catch (Exception ex)
                {
                    SetErrorStatus($"{_processType}--->>Error--->>获取下一步流程失败: {ex.Message}");
                    return;
                }
            }
            else
            {
                // 未找到对应状态获取器
                SetErrorStatus($"{_processType}--->>Error--->>未找到对应的流程状态获取器");
                return;
            }
        }

        #region 辅助方法
        /// <summary>
        /// 设置错误状态（统一更新运行状态和消息）
        /// </summary>
        /// <param name="errorMessage">错误描述信息</param>
        private void SetErrorStatus(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
            return;
        }
        #endregion
    }
}
