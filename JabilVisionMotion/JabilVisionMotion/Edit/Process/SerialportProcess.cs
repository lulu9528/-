using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using Jabil.Module.Serialport;
using Jabil.Module.Serialport.Common.Dtos;
using Jabil.Module.Serialport.Lib;
using JabilVisionMotion.Lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace JabilVisionMotion.Edit.Process
{
    /// <summary>
    /// 串口通讯流程处理类，继承自ProcessBase基类
    /// 功能：处理串口设备的通讯操作，支持输入（接收数据）和输出（发送数据）类型
    /// </summary>
    public class SerialportProcess : ProcessBase, ICloneable
    {
        #region 私有字段
        private readonly string _process;          // 原始指令字符串
        private readonly string _processTip;       // 指令提示信息（用于日志）
        private static readonly object _lock = new object(); // 线程锁，确保串口操作线程安全

        // 指令解析结果
        private string _commandName;               // 指令名称（固定为"Serialport"）
        private string _pointName;                 // 点位名称（通讯信号标识）
        private string _pointType;                 // 点位类型（"输入"：接收数据；"输出"：发送数据）
        private string _comPort;                   // 串口名称（如"COM7"）
        private string _messageDefinition;         // 报文定义（从配置中获取的通讯格式）
        private int _timeoutSeconds;               // 超时时间（秒）

        // 串口配置参数
        private SerialportDIODto _serialportDio;   // 串口点位配置
        #endregion

        /// <summary>
        /// 初始化串口通讯流程
        /// </summary>
        public SerialportProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                CurrentStatus = GeneralStatus.Wait;
                Message = "Wait";
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                if (!ParseCommand(_process)) return;
                if (!BindSerialportParameters()) return;
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 解析串口指令参数，提取关键信息
        /// </summary>
        /// <returns>true:解析成功, false:解析失败</returns>
        private bool ParseCommand(string process)
        {
            var commandParts = process.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            if (commandParts.Length < 5)
            {
                return HandleConfigurationError($"指令格式错误: {process}，正确格式：Serialport|点位类型|Com口|点位名称|超时");
            }

            // 提取指令各部分参数
            _commandName = commandParts[0];
            _pointType = commandParts[1];
            _comPort = commandParts[2];
            _pointName = commandParts[3];

            // 解析超时时间（必须为整数）
            if (!int.TryParse(commandParts[4], out _timeoutSeconds))
            {
                return HandleConfigurationError($"超时值格式错误: {commandParts[4]}，必须为整数");
            }

            return true;
        }

        /// <summary>
        /// 绑定串口配置参数
        /// </summary>
        /// <returns>true:绑定成功, false:绑定失败</returns>
        private bool BindSerialportParameters()
        {
            // 验证系统是否加载串口配置
            if (SerialportSystem.Instance.SerialportDIOList == null || SerialportSystem.Instance.SerialportDIOList.Count == 0)
            {
                return HandleConfigurationError("未加载串口配置列表");
            }

            // 根据点位名称查找匹配的配置
            _serialportDio = SerialportSystem.Instance.SerialportDIOList
                .Find(dio => dio.点位名称 == _pointName && dio.点位类型 == _pointType);

            // 配置不存在时触发急停并报错
            if (_serialportDio == null)
            {
                return HandleConfigurationError($"未找到点位名称为[{_pointName}]的串口配置");
            }

            // 绑定流程ID和报文定义
            ProcessId = _serialportDio.绑定代码;
            _messageDefinition = _serialportDio.通讯格式定义;

            return true;
        }

        /// <summary>
        /// 处理配置错误（触发急停并记录日志）
        /// </summary>
        /// <returns>始终返回false，表示处理失败</returns>
        private bool HandleConfigurationError(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;

            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            ProcessLog.Instance.SaveLog(_processTip, Color.Red, "紧急停止", _process, $"--->>错误：{errorMessage}！！！！");

            return false; // 表示配置错误，阻止继续执行
        }

        /// <summary>
        /// 执行串口通讯逻辑（核心方法）
        /// </summary>
        public override void Run()
        {
            try
            {
                // 测试模式：直接返回成功
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                // 验证串口模块是否加载
                if (!IsSerialportModuleLoaded())
                {
                    SetErrorStatus("未加载SerialPort模块！！！");
                    return;
                }

                // 初始化运行状态
                CurrentStatus = GeneralStatus.Running;
                Message = "Running";
                DateTime startTime = DateTime.Now;

                // 遍历串口列表，找到目标COM口设备
                foreach (var serialPortDevice in SerialportSystem.Instance.SerialPorts)
                {
                    if (serialPortDevice.PortName != _comPort)
                        continue;

                    // 检查串口是否打开
                    if (!serialPortDevice.IsOpen)
                    {
                        SetErrorStatus($"{_comPort}未开启！！！");
                        return;
                    }

                    // 根据点位类型执行对应操作
                    switch (_pointType)
                    {
                        case "输入":
                            ProcessInput(serialPortDevice, startTime);
                            return; // 处理完成后立即返回
                        case "输出":
                            ProcessOutput(serialPortDevice);
                            return; // 处理完成后立即返回
                        default:
                            SetErrorStatus($"未知点位类型：{_pointType}");
                            return;
                    }
                }

                // 如果未找到目标串口
                SetErrorStatus($"未找到串口设备: {_comPort}");
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 克隆方法
        /// </summary>
        public override object Clone()
        {
            var clone = new SerialportProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _pointName = this._pointName,
                _pointType = this._pointType,
                _comPort = this._comPort,
                _messageDefinition = this._messageDefinition,
                _timeoutSeconds = this._timeoutSeconds,
                _serialportDio = this._serialportDio != null ?
                    new SerialportDIODto
                    {
                        点位名称 = this._serialportDio.点位名称,
                        点位类型 = this._serialportDio.点位类型,
                        绑定代码 = this._serialportDio.绑定代码,
                        通讯格式定义 = this._serialportDio.通讯格式定义,
                    } : null
            };

            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";
            clone.ProcessId = this.ProcessId;

            return clone;
        }

        /// <summary>
        /// 处理输入类型操作（接收串口数据）
        /// </summary>
        private void ProcessInput(SerialPortCommunicator device, DateTime startTime)
        {
            while (true)
            {
                lock (_lock)
                {
                    // 检查急停状态
                    if (IsEmergencyStopped())
                        return;

                    // 检查超时
                    if (IsTimeout(startTime))
                        return;

                    // 接收数据（ASCII编码）
                    if (device.ReceivedData.Count() == 0) continue;
                    string receivedData = Encoding.ASCII.GetString(device.ReceivedData);
                    //device.ReceivedData = Array.Empty<byte>();

                    // 解析报文定义
                    string[] messageParts = _messageDefinition.Split(new[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
                    if (messageParts.Length < 2)
                    {
                        SetErrorStatus($"报文定义格式错误：{_messageDefinition}（正确格式：指令名称^内容）");
                        return;
                    }

                    string commandName = messageParts[0];
                    string commandContent = messageParts[1];

                    switch (commandName)
                    {
                        case "CONST":
                            if (receivedData == commandContent)
                            {
                                CurrentStatus = GeneralStatus.NoError;
                                Message = $"接收成功：{receivedData}";
                                return; // 成功后立即返回
                            }
                            break;
                        case "VAR":
                            switch (commandContent)
                            {
                                case "ContinuityResult":
                                    string[] strs = receivedData.Split(',');
                                    if (strs.Length == 16)
                                    {
                                        try
                                        {
                                            double[] data = new double[8];
                                            string[] result = new string[8];

                                            for (int i = 0; i < strs.Length - 1; i += 2)
                                            {
                                                data[i / 2] = Convert.ToDouble(strs[i]);
                                                result[i / 2] = strs[i + 1];
                                            }

                                            CurrentStatus = GeneralStatus.NoError;
                                            Message = $"接收成功：{receivedData}";
                                            return; // 成功后立即返回
                                        }
                                        catch (Exception ex)
                                        {
                                            SetErrorStatus($"数据转换错误：{ex.Message}");
                                            return;
                                        }
                                    }
                                    break;
                                case "Hi-PotResult":
                                    string[] str2s = receivedData.Split(',').Select(s => s.Trim()).ToArray();
                                    if (str2s.Length == 4)
                                    {
                                        try
                                        {
                                            double[] data2 = new double[4];
                                            for (int i = 0; i < str2s.Length; i++)
                                            {
                                                data2[i] = Convert.ToDouble(str2s[i]);
                                            }

                                            CurrentStatus = GeneralStatus.NoError;
                                            Message = $"接收成功：{receivedData}";
                                            return; // 成功后立即返回
                                        }
                                        catch (Exception ex)
                                        {
                                            SetErrorStatus($"数据转换错误：{ex.Message}");
                                            return;
                                        }
                                    }
                                    break;
                                default:
                                    SetErrorStatus($"未知的变量类型：{commandContent}");
                                    return;
                            }
                            break;
                        default:
                            SetErrorStatus($"未知的指令名称：{commandName}");
                            return;
                    }
                }
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 处理输出类型操作（向串口发送数据）
        /// </summary>
        private void ProcessOutput(SerialPortCommunicator device)
        {
            // 解析报文定义
            string[] messageParts = _messageDefinition.Split(new[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
            if (messageParts.Length < 2)
            {
                SetErrorStatus($"报文定义格式错误：{_messageDefinition}（正确格式：指令名称^内容）");
                return;
            }

            string commandName = messageParts[0];
            string commandContent = messageParts[1];

            switch (commandName)
            {
                case "CONST":
                    try
                    {
                        device.SendData(commandContent, SerialPortCommunicator.SendFormat.String);
                        CurrentStatus = GeneralStatus.NoError;
                        Message = $"NoError--->发送：{_messageDefinition}";
                    }
                    catch (Exception ex)
                    {
                        SetErrorStatus($"发送数据失败：{ex.Message}");
                    }
                    return; // 无论成功失败都返回
                default:
                    SetErrorStatus($"未知指令：{commandName}（{_comPort}）");
                    return;
            }
        }

        #region 辅助方法
        /// <summary>
        /// 检查串口模块是否加载
        /// </summary>
        private bool IsSerialportModuleLoaded()
        {
            return SerialportSystem.Instance.SerialPorts != null && SerialportSystem.Instance.SerialPorts.Count > 0;
        }

        /// <summary>
        /// 检查是否触发急停
        /// </summary>
        private bool IsEmergencyStopped()
        {
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                SetErrorStatus("机台急停");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否超时
        /// </summary>
        private bool IsTimeout(DateTime startTime)
        {
            if (_timeoutSeconds != -1 && DateTime.Now.Subtract(startTime).TotalSeconds > _timeoutSeconds)
            {
                SetErrorStatus($"通讯超时！报警信息：{Message}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置错误状态并立即返回
        /// </summary>
        private void SetErrorStatus(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
        }
        #endregion
    }
}
