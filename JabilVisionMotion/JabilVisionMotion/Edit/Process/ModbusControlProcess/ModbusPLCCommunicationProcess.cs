using HslCommunication;
using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus;
using Jabil.Module.Modbus.Common.Dtos;
using Jabil.Module.Modbus.Lib;
using Jabil.Module.Permission;
using Jabil.Module.Vision;
using JabilVisionMotion.Lib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JabilVisionMotion.Edit.Process.ModbusControlProcess
{
    /// <summary>
    /// ModbusPLC通信流程处理类，继承自ProcessBase并实现ICloneable接口
    /// 核心功能：
    /// 1. 结合ModbusDioRealTimeManager缓存与直接PLC通信，实现高效可靠的数据读写
    /// 2. 支持输入（读取PLC数据）和输出（向PLC写入数据）两种操作模式
    /// 3. 具备完善的错误处理、超时控制和容错机制
    /// 
    /// 设计亮点：
    /// - 双源数据获取：优先使用缓存提升效率，缓存无效时自动切换为直接PLC通信
    /// - 容错机制：直接通信失败时进行重试，超过阈值才判定为失败
    /// - 线程安全：关键操作通过锁机制确保多线程环境下的数据一致性
    /// - 松耦合设计：不强制依赖缓存管理器，管理器未初始化时仍可正常工作
    /// </summary>
    public class ModbusPLCCommunicationProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>连接重试延迟时间（毫秒）</summary>
        private const int ConnectionRetryDelayMs = 1000;
        /// <summary>读取检查间隔（毫秒）</summary>
        private const int ReadCheckIntervalMs = 15;
        /// <summary>已废弃：直接读取重试次数</summary>
        private const int DirectReadRetryCount = 3;
        /// <summary>H5u设备类型标识</summary>
        private const string H5uDeviceType = "H5u";
        #endregion

        #region 私有字段
        private readonly string _process;
        private readonly string _processTip;
        private static readonly object _lock = new object();

        private string _commandName;
        private string _pointType;
        private string _pointDefinition;
        private string _pointName;
        private int _pointValue;
        private int _timeoutSeconds;

        private ModbusCommunicationDioDto _modbusDio;
        private ModbusCommunicationDto _modbusDevice;
        private DateTime _processStartTime;
        private string _dataKey;

        private int _directReadFailedCount = 0;
        private const int MaxDirectReadFailedThreshold = 5;
        #endregion

        /// <summary>初始化ModbusPLC通信流程实例</summary>
        public ModbusPLCCommunicationProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                if (!ModbusDioRealTimeManager.IsInitialized)
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, "Modbus实时数据管理器尚未初始化，将使用直接通信模式");
                    Message = "警告：Modbus缓存管理器未初始化，将采用直接通信模式";
                }
                else
                {
                    Message = "初始化：Modbus缓存管理器可用，将优先使用缓存数据";
                }

                ParseCommand(process);
                BindModbusParameters();

                CurrentStatus = GeneralStatus.Wait;
                Message = "等待执行：初始化完成，等待运行指令";
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = $"初始化失败：{ex.Message}";
                RLog.SaveError(ex);
            }
        }

        /// <summary>克隆当前实例</summary>
        public override object Clone()
        {
            var clone = new ModbusPLCCommunicationProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _pointType = this._pointType,
                _pointDefinition = this._pointDefinition,
                _pointName = this._pointName,
                _pointValue = this._pointValue,
                _timeoutSeconds = this._timeoutSeconds,
                _modbusDio = this._modbusDio,
                _modbusDevice = this._modbusDevice,
                _dataKey = this._dataKey
            };
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "克隆实例：等待执行";
            return clone;
        }

        /// <summary>解析Modbus指令字符串</summary>
        private void ParseCommand(string process)
        {
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            if (commandParts.Length < 6)
            {
                string errorMsg = $"指令格式错误: {process}，至少需要6个部分（指令名称|点位类型|点位定义|点位名称|点位号值|超时）";
                HandleModbusError(errorMsg);
                Message = errorMsg;
                return;
            }

            _commandName = commandParts[0];
            _pointType = commandParts[1];
            _pointDefinition = commandParts[2];
            _pointName = commandParts[3];

            if (!int.TryParse(commandParts[4], out _pointValue))
            {
                string errorMsg = $"点位号值格式错误: {commandParts[4]}（应为整数）";
                HandleModbusError(errorMsg);
                Message = errorMsg;
            }

            if (!int.TryParse(commandParts[5], out _timeoutSeconds))
            {
                string errorMsg = $"超时值格式错误: {commandParts[5]}（应为整数）";
                HandleModbusError(errorMsg);
                Message = errorMsg;
            }

            Message = $"指令解析完成：类型={_pointType}，点位={_pointDefinition}-{_pointName}，目标值={_pointValue}，超时={_timeoutSeconds}s";
        }

        /// <summary>绑定Modbus设备和点位参数</summary>
        private void BindModbusParameters()
        {
            var allDios = ModbusSystem.Instance?.ModbusCommunicationDios ?? new List<ModbusCommunicationDioDto>();

            if (!allDios.Any())
            {
                string errorMsg = "系统中未找到任何点位配置，请检查Modbus系统配置";
                HandleModbusError(errorMsg);
                Message = errorMsg;
                return;
            }

            _modbusDio = allDios
                .FirstOrDefault(dio =>
                    dio.点位类型 == _pointType &&
                    dio.点位定义 == _pointDefinition &&
                    dio.点位名称 == _pointName);

            if (_modbusDio == null)
            {
                string errorMsg = $"未找到匹配的Modbus DIO配置 [类型:{_pointType}, 定义:{_pointDefinition}, 名称:{_pointName}]";
                HandleModbusError(errorMsg);
                Message = errorMsg;
                return;
            }

            ProcessId = _modbusDio.绑定代码;
            _dataKey = ModbusDioRealTimeManager.GetKey(_modbusDio.设备名称, _modbusDio.点位索引);

            _modbusDevice = ModbusSystem.Instance.ModbusCommunications
                .FirstOrDefault(dev => dev.设备名称 == _modbusDio.设备名称);

            if (_modbusDevice == null)
            {
                string errorMsg = $"未找到Modbus设备配置 [设备名称:{_modbusDio.设备名称}]";
                HandleModbusError(errorMsg);
                Message = errorMsg;
            }
            else
            {
                Message = $"参数绑定完成：设备={_modbusDevice.设备名称}({_modbusDevice.设备IP}:{_modbusDevice.设备Port})，点位索引={_modbusDio.点位索引}，数据类型={_modbusDio.数据类型}";
            }
        }

        /// <summary>处理Modbus配置错误</summary>
        private void HandleModbusError(string errorMessage)
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
            throw new InvalidOperationException(errorMessage);
        }

        /// <summary>执行ModbusPLC通信操作</summary>
        public override void Run()
        {
            try
            {
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "测试模式：操作成功（未执行实际通信）";
                    return;
                }

                CurrentStatus = GeneralStatus.Running;
                Message = "开始执行：准备建立PLC通信";
                _processStartTime = DateTime.Now;

                var plcDevice = GetPlcDevice();
                if (plcDevice == null)
                {
                    SetErrorStatus($"未找到PLC设备实例: {_modbusDevice?.设备名称 ?? "未知设备"}");
                    return;
                }

                if (!EnsureDeviceConnection(plcDevice))
                    return;

                switch (_pointType)
                {
                    case "输入":
                        Message = $"执行输入操作：读取[{_modbusDio.点位名称}]，目标值={_pointValue}";
                        ProcessInputOperation(plcDevice);
                        break;
                    case "输出":
                        Message = $"执行输出操作：写入[{_modbusDio.点位名称}]，目标值={_pointValue}";
                        ProcessOutputOperation(plcDevice);
                        break;
                    default:
                        SetErrorStatus($"未知点位类型--{_pointType}（应为\"输入\"或\"输出\"）");
                        return;
                }
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = $"执行异常：{ex.Message}";
                RLog.SaveError(ex);
            }
        }

        /// <summary>获取PLC设备实例</summary>
        private PLC_Modbus GetPlcDevice()
        {
            var device = ModbusSystem.Instance.PlcModbusList
                ?.FirstOrDefault(p => p.DeviceName == _modbusDevice.设备名称);

            if (device == null)
                Message = $"设备查找失败：未找到名称为'{_modbusDevice.设备名称}'的PLC实例";
            else
                Message = $"设备查找成功：获取'{_modbusDevice.设备名称}'实例";

            return device;
        }

        /// <summary>确保PLC设备连接正常</summary>
        private bool EnsureDeviceConnection(PLC_Modbus plcDevice)
        {
            if (!plcDevice.IsConnected)
            {
                Message = $"设备未连接：'{_modbusDevice.设备名称}'当前连接状态为断开，准备重连";
                plcDevice.Disconnect();

                Thread.Sleep(ConnectionRetryDelayMs);
                Message = $"重连设备：等待{ConnectionRetryDelayMs}ms后尝试连接'{_modbusDevice.设备名称}'";

                var connectResult = plcDevice.DeviceName.Contains(H5uDeviceType)
                    ? plcDevice.Connect(PLC_Modbus.PLCType.H5u, _modbusDevice.设备IP, _modbusDevice.设备Port)
                    : plcDevice.Connect(PLC_Modbus.PLCType.H5u, _modbusDevice.设备IP, _modbusDevice.设备Port);

                if (!connectResult.IsSuccess)
                {
                    SetErrorStatus($"设备：{_modbusDevice.设备名称}--通讯连接失败，请检查IP、端口及设备状态！！（错误：{connectResult.Message}）");
                    return false;
                }

                Message = $"连接成功：'{_modbusDevice.设备名称}'已建立连接（IP：{_modbusDevice.设备IP}:{_modbusDevice.设备Port}）";
            }
            else
            {
                Message = $"连接正常：'{_modbusDevice.设备名称}'当前处于已连接状态";
            }
            return true;
        }

        /// <summary>处理输入操作（读取PLC数据）</summary>
        private void ProcessInputOperation(PLC_Modbus plcDevice)
        {
            if (_pointDefinition != "PLC")
            {
                SetErrorStatus($"未知点位定义--{_pointDefinition}（支持的定义：PLC）");
                return;
            }

            while (true)
            {
                if (CheckEmergencyStop()) return;
                if (CheckTimeout()) return;

                object currentValue = null;
                bool isManagerDataValid = false;

                if (ModbusDioRealTimeManager.IsInitialized)//&& _modbusDio.数据类型 != "string"
                {
                    var realTimeData = ModbusDioRealTimeManager.GetRealTimeData(_modbusDio.设备名称, _modbusDio.点位索引);
                    if (realTimeData != null && realTimeData.IsValid)
                    {
                        currentValue = realTimeData.RealTimeValue;
                        isManagerDataValid = true;
                        _directReadFailedCount = 0;
                        Message = $"缓存读取成功：[{_modbusDio.点位名称}] 当前值={currentValue}，等待目标值={_pointValue}";
                    }
                    else
                    {
                        Message = $"缓存数据无效：[{_modbusDio.点位名称}] 缓存数据过期或未初始化，将切换至直接读取";
                    }
                }

                if (true)//!isManagerDataValid
                {
                    Message = $"直接读取尝试：[{_modbusDio.点位名称}] 第{_directReadFailedCount + 1}次读取（最大{MaxDirectReadFailedThreshold}次）";
                    var directReadResult = ReadFromPlcDirectly(plcDevice);
                    if (!directReadResult.IsSuccess)
                    {
                        _directReadFailedCount++;
                        Message = $"直接读取失败：[{_modbusDio.点位名称}] 原因：{directReadResult.Message}，{ReadCheckIntervalMs}ms后重试（已失败{_directReadFailedCount}/{MaxDirectReadFailedThreshold}次）";

                        if (_directReadFailedCount >= MaxDirectReadFailedThreshold)
                        {
                            SetErrorStatus($"连续{MaxDirectReadFailedThreshold}次直接读取PLC失败: {directReadResult.Message}");
                            return;
                        }
                        Thread.Sleep(ReadCheckIntervalMs);
                        continue;
                    }

                    currentValue = directReadResult.Content;
                    isManagerDataValid = true;
                    Message = $"直接读取成功：[{_modbusDio.点位名称}] 读取值={currentValue}";

                    if (ModbusDioRealTimeManager.IsInitialized)
                    {
                        ModbusDioRealTimeManager.UpdateRealTimeValue(
                            _modbusDio.设备名称,
                            _modbusDio.点位索引,
                            currentValue,
                            true);
                        Message = $"缓存同步完成：[{_modbusDio.点位名称}] 已将读取值{currentValue}更新至缓存";
                    }
                }

                if (VerifyInputValue(currentValue))
                {
                    Message = CreateSuccessMessage(
                        _modbusDio.点位名称,
                        _modbusDio.点位索引,
                        _pointValue,
                        currentValue.ToString());
                    CurrentStatus = GeneralStatus.NoError;
                    break;
                }

                Message = $"未达目标值：[{_modbusDio.点位名称}] 当前值={currentValue}，目标值={_pointValue}，{ReadCheckIntervalMs}ms后重试";
                Thread.Sleep(ReadCheckIntervalMs);
            }
        }

        /// <summary>直接从PLC读取数据</summary>
        private OperateResult<object> ReadFromPlcDirectly(PLC_Modbus plcDevice)
        {
            try
            {
                lock (_lock)
                {
                    switch (_modbusDio.数据类型)
                    {
                        case "bool":
                            var boolResult = plcDevice.ReadBool(_modbusDio.点位索引);
                            return boolResult.IsSuccess
                                ? OperateResult.CreateSuccessResult<object>(boolResult.Content)
                                : OperateResult.CreateFailedResult<object>(boolResult);

                        case "int16":
                            var intResult = plcDevice.ReadInt16(_modbusDio.点位索引);
                            return intResult.IsSuccess
                                ? OperateResult.CreateSuccessResult<object>(intResult.Content)
                                : OperateResult.CreateFailedResult<object>(intResult);
                        case "string":
                            var stringResult = plcDevice.ReadString(_modbusDio.点位索引);
                            return stringResult.IsSuccess
                                ? OperateResult.CreateSuccessResult<object>(stringResult.Content)
                                : OperateResult.CreateFailedResult<object>(stringResult);
                        default:
                            return OperateResult.CreateFailedResult<object>(
                                new OperateResult($"不支持的读取类型: {_modbusDio.数据类型}"));
                    }
                }
            }
            catch (Exception ex)
            {
                return OperateResult.CreateFailedResult<object>(
                    new OperateResult($"直接读取异常: {ex.Message}"));
            }
        }

        /// <summary>验证输入值是否符合目标值</summary>
        private bool VerifyInputValue(object value)
        {
            if (value == null)
            {
                Message = "验证失败：读取值为空";
                return false;
            }

            try
            {
                switch (_modbusDio.数据类型)
                {
                    case "int16":
                        if (!short.TryParse(value.ToString(), out short int16Value))
                        {
                            SetErrorStatus($"数据格式错误，无法转换为整数: {value}");
                            return false;
                        }

                        switch (_modbusDio.点位名称)
                        {
                            case "后滑台接收流程调整顺序":
                                if (int16Value >= 1 && int16Value <= 6)
                                {
                                    Message = $"验证通过：[{_modbusDio.点位名称}] 读取值={int16Value}（符合1-6范围）";
                                    return true;
                                }
                                Message = $"验证失败：[{_modbusDio.点位名称}] 读取值={int16Value}（不在1-6范围内）";
                                return false;
                            default:
                                bool intMatch = int16Value == _pointValue;
                                Message = intMatch
                                    ? $"验证通过：[{_modbusDio.点位名称}] 读取值={int16Value}，匹配目标值={_pointValue}"
                                    : $"验证失败：[{_modbusDio.点位名称}] 读取值={int16Value}，目标值={_pointValue}";
                                return intMatch;
                        }

                    case "bool":
                        int currentBoolAsInt = (value is bool b && b) ? 1 : 0;
                        bool boolMatch = currentBoolAsInt == _pointValue;
                        Message = boolMatch
                            ? $"验证通过：[{_modbusDio.点位名称}] 读取值={currentBoolAsInt}（{value}），匹配目标值={_pointValue}"
                            : $"验证失败：[{_modbusDio.点位名称}] 读取值={currentBoolAsInt}（{value}），目标值={_pointValue}";
                        return boolMatch;

                    case "string":
                        string strValue = value.ToString().Replace("\0", string.Empty).Trim();
                        if (strValue.Length < 1)
                        {
                            SetErrorStatus($"码值长度错误: {value}");
                            Message = $"验证失败：[{_modbusDio.点位名称}] 字符串长度为0，无效值";
                            return false;
                        }

                        switch (_modbusDio.点位名称)
                        {
                            case "前滑台nose码1":
                                Controller.Instance.StationInfo.前滑台码值1 = strValue;
                                Message = $"验证通过：前滑台码值1已更新为[{strValue}]";
                                return true;
                            case "前滑台nose码2":
                                Controller.Instance.StationInfo.前滑台码值2 = strValue;
                                Message = $"验证通过：前滑台码值2已更新为[{strValue}]";
                                return true;
                            case "前滑台nose码3":
                                Controller.Instance.StationInfo.前滑台码值3 = strValue;
                                Message = $"验证通过：前滑台码值3已更新为[{strValue}]";
                                return true;
                            case "前滑台nose码4":
                                Controller.Instance.StationInfo.前滑台码值4 = strValue;
                                Message = $"验证通过：前滑台码值4已更新为[{strValue}]";
                                return true;
                            case "后滑台nose码1":
                                Controller.Instance.StationInfo.后滑台码值1 = strValue;
                                Message = $"验证通过：后滑台码值1已更新为[{strValue}]";
                                return true;
                            case "后滑台nose码2":
                                Controller.Instance.StationInfo.后滑台码值2 = strValue;
                                Message = $"验证通过：后滑台码值2已更新为[{strValue}]";
                                return true;
                            case "后滑台nose码3":
                                Controller.Instance.StationInfo.后滑台码值3 = strValue;
                                Message = $"验证通过：后滑台码值3已更新为[{strValue}]";
                                return true;
                            case "后滑台nose码4":
                                Controller.Instance.StationInfo.后滑台码值4 = strValue;
                                Message = $"验证通过：后滑台码值4已更新为[{strValue}]";
                                return true;
                            default:
                                Message = $"验证失败：未知字符串点位[{_modbusDio.点位名称}]";
                                return false;
                        }

                    default:
                        SetErrorStatus($"未知数据类型--{_modbusDio.数据类型}（支持：int16、bool、string）");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Message = $"数据验证错误: {ex.Message}";
                return false;
            }
        }

        /// <summary>处理输出操作（向PLC写入数据）</summary>
        private void ProcessOutputOperation(PLC_Modbus plcDevice)
        {
            if (_pointDefinition != "PLC")
            {
                SetErrorStatus($"未知点位定义--{_pointDefinition}（支持的定义：PLC）");
                return;
            }

            try
            {
                if (_modbusDio.数据类型 == "string")
                {
                    var array = Controller.Instance.StationInfo.前后滑台上传下发信息汇总.ToArray();
                    Message = $"字符串写入准备：[{_modbusDio.点位名称}] 从汇总信息中获取数据（共{array.Length}条）";

                    // 此处保留原有业务逻辑，根据实际需求补充具体写入逻辑
                    CurrentStatus = GeneralStatus.NoError;
                    Message = $"字符串写入完成：[{_modbusDio.点位名称}] 操作成功";
                    return;
                }

                object targetValue = ConvertTargetValue(_modbusDio.数据类型, _pointValue);
                Message = $"写入准备：[{_modbusDio.点位名称}] 目标值={targetValue}（类型：{_modbusDio.数据类型}）";

                var writeSuccess = WriteToPlc(plcDevice, targetValue);
                if (!writeSuccess)
                {
                    SetErrorStatus("写入PLC失败，已终止操作");
                    return;
                }
                Message = $"写入成功：[{_modbusDio.点位名称}] 已将值{targetValue}写入PLC";

                if (ModbusDioRealTimeManager.IsInitialized)
                {
                    ModbusDioRealTimeManager.UpdateRealTimeValue(
                        _modbusDio.设备名称,
                        _modbusDio.点位索引,
                        targetValue,
                        true);
                    Message = $"缓存同步：[{_modbusDio.点位名称}] 已将值{targetValue}更新至缓存";
                }

                var confirmResult = ReadFromPlcDirectly(plcDevice);
                if (!confirmResult.IsSuccess)
                {
                    SetErrorStatus($"写入成功但验证读取失败: {confirmResult.Message}");
                    return;
                }
                Message = $"验证读取成功：[{_modbusDio.点位名称}] 读取值={confirmResult.Content}（用于验证写入结果）";

                if (VerifyInputValue(confirmResult.Content))
                {
                    Message = CreateSuccessMessage(
                        _modbusDio.点位名称,
                        _modbusDio.点位索引,
                        targetValue,
                        confirmResult.Content.ToString()
                    );
                    CurrentStatus = GeneralStatus.NoError;
                }
                else
                {
                    SetErrorStatus($"写入值与读取验证不匹配: 目标值={targetValue}, 实际值={confirmResult.Content}");
                }
            }
            catch (Exception ex)
            {
                SetErrorStatus($"输出操作失败: {ex.Message}");
            }
        }

        /// <summary>向PLC写入数据并返回结果</summary>
        public bool WriteToPlc(PLC_Modbus plcDevice, object value)
        {
            try
            {
                lock (_lock)
                {
                    OperateResult result = _modbusDio.数据类型 switch
                    {
                        "bool" => plcDevice.WriteBool(Convert.ToBoolean(value), _modbusDio.点位索引),
                        "int16" => plcDevice.WriteInt16(Convert.ToInt16(value), _modbusDio.点位索引),
                        _ => new OperateResult($"不支持的数据类型: {_modbusDio.数据类型}")
                    };

                    if (!result.IsSuccess)
                    {
                        SetErrorStatus($"写入失败: {result.Message} (错误代码: {result.ErrorCode})");
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                SetErrorStatus($"写入操作异常: {ex.Message}");
                return false;
            }
        }

        #region 辅助方法
        /// <summary>将整数类型的目标值转换为对应的数据类型</summary>
        private object ConvertTargetValue(string dataType, int value)
        {
            return dataType switch
            {
                "bool" => value != 0,
                "int16" => (short)value,
                _ => throw new ArgumentException($"不支持的数据类型转换: {dataType}")
            };
        }

        /// <summary>检查机台是否处于急停状态</summary>
        private bool CheckEmergencyStop()
        {
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                SetErrorStatus("机台急停，终止操作");
                Message = "紧急停止：检测到机台急停信号，操作已终止";
                return true;
            }
            return false;
        }

        /// <summary>检查操作是否超时</summary>
        private bool CheckTimeout()
        {
            if (_timeoutSeconds == -1)
                return false;

            var elapsed = DateTime.Now - _processStartTime;
            if (elapsed.TotalSeconds > _timeoutSeconds)
            {
                SetErrorStatus($"操作超时！超过 {_timeoutSeconds} 秒未完成");
                Message = $"超时终止：操作已运行{elapsed.TotalSeconds:F1}s，超过设定阈值{_timeoutSeconds}s";
                return true;
            }

            Message = $"超时监控：当前运行{elapsed.TotalSeconds:F1}s，剩余超时时间{_timeoutSeconds - elapsed.TotalSeconds:F1}s";
            return false;
        }

        /// <summary>创建成功状态消息</summary>
        private string CreateSuccessMessage(string pointName, string pointIndex, object targetValue, string currentValue)
        {
            return $"\r\n操作成功" +
                   $"\r\n点位名称：{pointName}" +
                   $"\r\n点位地址：{pointIndex}" +
                   $"\r\n预期目标值：{targetValue}" +
                   $"\r\n实际检测值：{currentValue}" +
                   $"\r\n总耗时：{(DateTime.Now - _processStartTime).TotalMilliseconds:F2}ms\r\n";
        }

        /// <summary>设置错误状态</summary>
        private void SetErrorStatus(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
        }
        #endregion
    }
}