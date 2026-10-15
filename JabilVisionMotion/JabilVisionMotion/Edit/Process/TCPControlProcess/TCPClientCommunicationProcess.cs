using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using Jabil.Module.TCPIP;
using Jabil.Module.TCPIP.Common.Dtos;
using Jabil.Module.TCPIP.Lib;
using JabilVisionMotion.Lib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JabilVisionMotion.Edit.Process.TCPControlProcess
{
    /// <summary>
    /// TCPClient通讯流程处理类
    /// 指令示例：TCPClient|点位类型|设备名称|点位名称|超时
    /// 超时说明：-1 表示等待流程完成；>0 表示超时秒数，到达后触发报警
    /// 功能：处理TCP客户端的通讯操作，包括数据收发和状态监控
    /// </summary>
    public class TCPClientCommunicationProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        private const int ReceiveCheckIntervalMs = 100;  // 接收检查间隔(毫秒)
        #endregion

        #region 私有字段
        private readonly string _process;
        private readonly string _processTip;
        private static readonly object _lock = new object();

        // 指令解析结果
        private string _commandName;
        private string _pointType;
        private string _deviceName;
        private string _pointName;
        private string _messageDefinition;
        private int _timeoutSeconds;

        // TCPIP参数
        private TcpIpCommunicationDioDto _tcpipDio;
        private DateTime _processStartTime;
        #endregion

        /// <summary>
        /// 初始化TCPClient通讯流程
        /// </summary>
        public TCPClientCommunicationProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);
                BindTcpipParameters();

                CurrentStatus = GeneralStatus.Wait;
                Message = "Wait";
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);
                return; // 设置错误状态后立即返回
            }
        }

        /// <summary>
        /// 克隆方法，支持副流程的缓存机制
        /// 确保多实例并发时的状态独立性
        /// </summary>
        public override object Clone()
        {
            var clone = new TCPClientCommunicationProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _pointType = this._pointType,
                _deviceName = this._deviceName,
                _pointName = this._pointName,
                _messageDefinition = this._messageDefinition,
                _timeoutSeconds = this._timeoutSeconds,
                _tcpipDio = this._tcpipDio
            };
            // 初始化克隆实例的状态为"等待"
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";

            return clone;
        }


        /// <summary>
        /// 解析TCP客户端指令参数
        /// </summary>
        private void ParseCommand(string process)
        {
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            if (commandParts.Length < 5)
            {
                HandleTcpipError($"指令格式错误: {process}，至少需要5个部分，格式为TCPClient|点位类型|设备名称|点位名称|超时");
                return;
            }

            _commandName = commandParts[0];
            _pointType = commandParts[1];
            _deviceName = commandParts[2];
            _pointName = commandParts[3];

            if (!int.TryParse(commandParts[4], out _timeoutSeconds))
            {
                HandleTcpipError($"超时值格式错误: {commandParts[4]}");
                return;
            }
        }

        /// <summary>
        /// 绑定TCPIP参数配置
        /// </summary>
        private void BindTcpipParameters()
        {
            // 验证TCPIP配置列表
            if (TcpIpSystem.Instance.TcpIpCommunicationDios == null ||
                TcpIpSystem.Instance.TcpIpCommunicationDios.Count == 0)
            {
                HandleTcpipError("TCPIP通讯配置列表为空");
                return;
            }

            // 查找匹配的DIO配置
            _tcpipDio = TcpIpSystem.Instance.TcpIpCommunicationDios
                .FirstOrDefault(dio => dio.点位名称 == _pointName);

            if (_tcpipDio == null)
            {
                HandleTcpipError($"未找到匹配的TCPIP DIO配置 [点位名称:{_pointName}]");
                return;
            }

            ProcessId = _tcpipDio.绑定代码;

            // 解析报文定义
            if (!string.IsNullOrEmpty(_tcpipDio.通讯格式定义))
            {
                var messageParts = _tcpipDio.通讯格式定义.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                _messageDefinition = messageParts.Length > 0 ? messageParts[0] : string.Empty;
            }
        }

        /// <summary>
        /// 处理TCPIP配置错误
        /// </summary>
        private void HandleTcpipError(string errorMessage)
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
            // 抛出异常，终止当前流程
            throw new InvalidOperationException(errorMessage);
        }

        /// <summary>
        /// 执行TCPClient通讯操作
        /// </summary>
        public override void Run()
        {
            try
            {
                // 测试模式直接返回成功
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                // 初始化运行状态
                CurrentStatus = GeneralStatus.Running;
                Message = "Running";
                _processStartTime = DateTime.Now;

                // 验证TCP客户端是否开启
                if (TcpIpSystem.Instance.TcpClients == null ||
                    TcpIpSystem.Instance.TcpClients.Count == 0)
                {
                    SetErrorStatus("服务器未开启！！！");
                    return;
                }

                // 处理目标设备通讯
                ProcessTargetClientCommunication();
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);
                return; // 设置错误状态后立即返回
            }
        }

        /// <summary>
        /// 处理目标客户端的通讯逻辑
        /// </summary>
        private void ProcessTargetClientCommunication()
        {
            foreach (var client in TcpIpSystem.Instance.TcpClients)
            {
                // 只处理目标设备
                if (client.Name != _deviceName)
                    continue;

                // 检查服务器连接状态
                if (!client.IsConnected)
                {
                    client.ReceiveString = string.Empty;
                    SetErrorStatus("与服务器断开连接 ！！");
                    return;
                }

                // 根据点位类型执行对应操作
                switch (_pointType)
                {
                    case "输入":
                        ProcessInputOperation(client);
                        break;
                    case "输出":
                        ProcessOutputOperation(client);
                        break;
                    default:
                        SetErrorStatus($"未知点位类型--{_pointType}");
                        return;
                }

                // 找到并处理目标设备后退出循环
                break;
            }
        }

        /// <summary>
        /// 处理输入类型操作（接收数据）
        /// </summary>
        private void ProcessInputOperation(TCPClient client)
        {
            while (true)
            {
                lock (_lock)
                {
                    // 检查急停状态
                    if (CheckEmergencyStop())
                        return;

                    // 检查超时
                    if (CheckTimeout())
                        return;

                    // 检查服务器连接
                    if (!client.IsConnected)
                    {
                        client.ReceiveString = string.Empty;
                        SetErrorStatus("与服务器断开连接 ！！");
                        return;
                    }

                    // 检查是否接收到数据
                    if (!string.IsNullOrEmpty(client.ReceiveString))
                    {
                        if (client.ReceiveString == _messageDefinition)
                        {
                            client.ReceiveString = string.Empty;
                            CurrentStatus = GeneralStatus.NoError;
                            Message = "NoError";
                            break;
                        }
                    }
                }

                Thread.Sleep(ReceiveCheckIntervalMs);
            }
        }

        /// <summary>
        /// 处理输出类型操作（发送数据）
        /// </summary>
        private void ProcessOutputOperation(TCPClient client)
        {
            // 解析报文定义
            var messageParts = _messageDefinition.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (messageParts.Length == 0)
            {
                SetErrorStatus("报文定义格式错误");
                return;
            }

            string commandName = messageParts[0];
            string commandContent = _messageDefinition;

            // 根据设备名称处理特定指令
            switch (client.Name)
            {
                case "Client1":
                    commandContent = HandleClient1Command(commandName);
                    if (CurrentStatus == GeneralStatus.Error)
                        return; // 若处理指令时已设置错误状态，立即返回
                    break;
                case "Slave":
                    //switch (_pointName)
                    //{
                    //    case "发送测量结果1":
                    //        Step1Data[] data1 = new Step1Data[2] { Controller.Instance.StationInfo.Cavity1_FAI1, Controller.Instance.StationInfo.Cavity3_FAI1 };
                    //        commandContent = JsonConvert.SerializeObject(data1);
                    //        break;
                    //    case "发送测量结果2":
                    //        Step1Data[] data2 = new Step1Data[2] { Controller.Instance.StationInfo.Cavity2_FAI1, Controller.Instance.StationInfo.Cavity4_FAI1 };
                    //        commandContent = JsonConvert.SerializeObject(data2);
                    //        break;
                    //    case "发送测量结果3":
                    //        Step2Data[] data3 = new Step2Data[2] { Controller.Instance.StationInfo.Cavity2_FAI2, Controller.Instance.StationInfo.Cavity4_FAI2 };
                    //        commandContent = JsonConvert.SerializeObject(data3);
                    //        break;
                    //    case "发送测量结果4":
                    //        Step2Data[] data4 = new Step2Data[2] { Controller.Instance.StationInfo.Cavity1_FAI2, Controller.Instance.StationInfo.Cavity3_FAI2 };
                    //        commandContent = JsonConvert.SerializeObject(data4);
                    //        break;
                    //    default:
                    //        break;
                    //}
                    break;
                default:
                    SetErrorStatus($"未知设备名称：{client.Name}");
                    return;
            }

            // 发送数据并检查结果
            if (client.SendData(commandContent))
            {
                client.ReceiveString = string.Empty;
                CurrentStatus = GeneralStatus.NoError;
                Message = "NoError";
            }
            else
            {
                client.ReceiveString = string.Empty;
                SetErrorStatus("与服务器断开连接 ！！");
                return;
            }
        }

        /// <summary>
        /// 处理Client1设备的特定指令
        /// </summary>
        private string HandleClient1Command(string commandName)
        {
            switch (commandName)
            {
                case "Over":
                    return "Over^Put";
                default:
                    SetErrorStatus($"未知的指令：{commandName}");
                    return string.Empty;
            }
        }

        #region 辅助方法
        /// <summary>
        /// 检查急停状态
        /// </summary>
        private bool CheckEmergencyStop()
        {
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                RLog.SaveLog(LogType.Error, LogLevel.Info, Color.Red, "机台紧急停止，如需继续运行，需复位后初始化！！");
                SetErrorStatus("机台急停");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否超时
        /// </summary>
        private bool CheckTimeout()
        {
            if (_timeoutSeconds != -1 &&
                DateTime.Now.Subtract(_processStartTime).TotalSeconds > _timeoutSeconds)
            {
                SetErrorStatus($"通讯超时！！   报警信息：{Message}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置错误状态
        /// </summary>
        private void SetErrorStatus(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
        }
        #endregion
    }
}
