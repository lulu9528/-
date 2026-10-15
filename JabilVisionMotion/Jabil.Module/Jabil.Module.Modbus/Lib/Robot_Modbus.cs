using BasicClassLibrary.RexHelps;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.ModBus;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus.Common.Dtos;
using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.Modbus.Lib
{
    /// <summary>
    /// 机器人Modbus通信类
    /// 设计目标：提供统一接口实现多种品牌机器人的Modbus TCP协议通信
    /// 支持的机器人品牌：Fanuc（发那科）、Efort（埃夫特）、Epson（爱普生）
    /// 统一使用HSL库实现通信，简化维护
    /// 实现了IDisposable接口，确保资源正确释放
    /// </summary>
    public class Robot_Modbus : IDisposable
    {
        /// <summary>
        /// 机器人类型枚举
        /// 定义当前类支持的机器人品牌，便于后续扩展新品牌
        /// </summary>
        public enum RobotType
        {
            Fanuc,   // 发那科机器人
            Efort,   // 埃夫特机器人
            Epson,   // 爱普生机器人
            Unknown
        }

        #region 私有字段
        /// <summary>
        /// HslCommunication库的Modbus TCP客户端
        /// 统一使用HSL库处理所有品牌机器人通信
        /// </summary>
        private ModbusTcpNet _hslClient;

        /// <summary>
        /// 线程同步锁对象
        /// 确保多线程环境下数据读写操作的原子性，避免数据不一致
        /// </summary>
        private readonly object _operationLock = new object();

        /// <summary>
        /// 资源释放标识
        /// 用于IDisposable模式中防止资源被重复释放
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// 重连次数计数器
        /// 用于限制自动重连的次数
        /// </summary>
        private int _reconnectAttempts;

        /// <summary>
        /// 机器人IP地址
        /// 保存连接参数用于自动重连
        /// </summary>
        private string _ipAddress;

        /// <summary>
        /// 通信端口号
        /// 保存连接参数用于自动重连
        /// </summary>
        private int _port;

        /// <summary>
        /// Modbus站号
        /// 保存连接参数用于自动重连
        /// </summary>
        private int _station;

        /// <summary>
        /// 重连状态标识
        /// 防止重连操作嵌套执行
        /// </summary>
        private bool _isReconnecting;
        #endregion

        #region 常量定义
        /// <summary>
        /// 默认接收超时时间(毫秒)
        /// 设定通信超时阈值，避免无限等待导致程序假死
        /// </summary>
        private const int DefaultTimeout = 1000;

        /// <summary>
        /// 最小端口号常量
        /// 用于端口号参数验证
        /// </summary>
        private const int MinPort = 1;

        /// <summary>
        /// 最大端口号常量
        /// 用于端口号参数验证，TCP端口号范围为1-65535
        /// </summary>
        private const int MaxPort = 65535;

        /// <summary>
        /// 最小寄存器地址
        /// 用于地址参数验证
        /// </summary>
        private const int MinRegisterAddress = 1;

        /// <summary>
        /// 最大寄存器地址
        /// 用于地址参数验证
        /// </summary>
        private const int MaxRegisterAddress = 65535;

        /// <summary>
        /// 最大重连次数
        /// 限制自动重连尝试次数
        /// </summary>
        private const int MaxReconnectAttempts = 3;

        /// <summary>
        /// 重连间隔时间(毫秒)
        /// 控制重连尝试的时间间隔
        /// </summary>
        private const int ReconnectIntervalMs = 1000;
        #endregion

        #region 公共属性
        /// <summary>
        /// 设备名称
        /// 用于日志记录和错误信息中标识具体设备，便于问题定位
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 当前连接的机器人类型
        /// 只读属性，确保只能通过Connect方法修改连接的机器人类型
        /// </summary>
        public RobotType CurrentRobotType { get; private set; }

        /// <summary>
        /// 连接状态标识
        /// 只读属性，反映当前是否与机器人建立有效连接
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// 接收超时时间(毫秒)
        /// </summary>
        public int ReceiveTimeout
        {
            get => _hslClient?.ReceiveTimeOut ?? DefaultTimeout;
            set
            {
                if (_hslClient != null && value > 0)
                    _hslClient.ReceiveTimeOut = value;
            }
        }

        /// <summary>
        /// 发送超时时间(毫秒)
        /// </summary>
        public int SendTimeout
        {
            get => _hslClient?.ConnectTimeOut ?? DefaultTimeout;
            set
            {
                if (_hslClient != null && value > 0)
                    _hslClient.ConnectTimeOut = value;
            }
        }

        /// <summary>
        /// 自动重连开关
        /// 控制操作失败时是否自动触发重连
        /// </summary>
        public bool AutoReconnectEnabled { get; set; } = true;
        #endregion

        #region 事件定义
        /// <summary>
        /// 连接状态变化事件
        /// 当连接状态发生改变时触发
        /// </summary>
        public event Action<bool> ConnectionStatusChanged;

        /// <summary>
        /// 通信错误事件
        /// 当通信发生错误时触发，包含错误详情
        /// </summary>
        public event Action<string, Exception> CommunicationErrorOccurred;

        /// <summary>
        /// 重连事件
        /// 当开始重连和重连完成时触发
        /// </summary>
        public event Action<bool, int> ReconnectionAttempted;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// 初始化机器人Modbus通信类的新实例
        /// </summary>
        public Robot_Modbus() { }

        /// <summary>
        /// 构造函数
        /// 初始化机器人Modbus通信类的新实例并指定设备名称
        /// </summary>
        /// <param name="deviceName">设备名称，用于日志记录</param>
        public Robot_Modbus(string deviceName)
        {
            DeviceName = deviceName ?? throw new ArgumentNullException(nameof(deviceName), "设备名称不能为空");
        }
        #endregion

        #region 连接管理
        /// <summary>
        /// 连接机器人的核心方法
        /// 根据机器人类型初始化对应的Modbus客户端并建立连接
        /// 所有品牌统一使用HSL库实现
        /// </summary>
        /// <param name="robotType">要连接的机器人类型</param>
        /// <param name="ipAddress">机器人的IP地址</param>
        /// <param name="port">Modbus通信端口号，范围1-65535</param>
        /// <param name="station">Modbus站号，默认值为1</param>
        /// <returns>操作结果对象，包含连接是否成功及相关消息</returns>
        /// <exception cref="ArgumentNullException">当IP地址为空时抛出</exception>
        /// <exception cref="ArgumentException">当IP地址格式无效时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">当端口号或站号超出有效范围时抛出</exception>
        public OperateResult Connect(RobotType robotType, string ipAddress, int port, int station = 1)
        {
            // 参数验证：确保IP地址不为空
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress), "IP地址不能为空");

            // 参数验证：确保IP地址格式有效
            if (!IPAddress.TryParse(ipAddress, out _))
                throw new ArgumentException("无效的IP地址格式", nameof(ipAddress));

            // 参数验证：确保端口号在有效范围内
            if (port < MinPort || port > MaxPort)
                throw new ArgumentOutOfRangeException(nameof(port), $"端口号必须在{MinPort}-{MaxPort}范围内");

            // 参数验证：确保站号在有效范围内
            if (station < 1 || station > 247)
                throw new ArgumentOutOfRangeException(nameof(station), "站号必须在1-247范围内");

            // 保存连接参数用于可能的重连操作
            _ipAddress = ipAddress;
            _port = port;
            _station = station;

            // 连接新设备前先断开现有连接，避免资源冲突
            Disconnect();

            // 初始化操作结果对象
            var result = new OperateResult();
            try
            {
                // 记录当前连接的机器人类型
                CurrentRobotType = robotType;

                // 所有机器人类型均使用HSL客户端，根据品牌设置不同参数
                _hslClient = new ModbusTcpNet(ipAddress, port, (byte)station)
                {
                    ReceiveTimeOut = DefaultTimeout,
                    ConnectTimeOut = DefaultTimeout,
                };

                // 根据机器人类型设置特定的数据格式和参数
                ConfigureClientForRobotType(robotType);

                // 尝试连接到机器人
                var hslResult = _hslClient.ConnectServer();
                IsConnected = hslResult.IsSuccess;
                result.IsSuccess = hslResult.IsSuccess;
                result.Message = hslResult.Message;

                // 触发连接状态变化事件
                ConnectionStatusChanged?.Invoke(IsConnected);

                // 记录连接日志
                var logMessage = $"{DeviceName ?? "机器人"} {(IsConnected ? "连接成功" : "连接失败")}: {ipAddress}:{port}, 站号: {station}";
                if (IsConnected)
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, logMessage);
                    _reconnectAttempts = 0; // 重置重连计数器
                }
                else
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"{logMessage}, 原因: {result.Message}");
                    CommunicationErrorOccurred?.Invoke(logMessage + $", 原因: {result.Message}", null);
                }
            }
            catch (Exception ex)
            {
                // 异常处理：记录错误并更新连接状态
                IsConnected = false;
                RLog.SaveError(ex);
                result.IsSuccess = false;
                result.Message = ex.Message;
                ConnectionStatusChanged?.Invoke(false);
                CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "机器人"} 连接异常", ex);
            }

            return result;
        }

        /// <summary>
        /// 异步连接机器人
        /// </summary>
        public async Task<OperateResult> ConnectAsync(RobotType robotType, string ipAddress, int port, int station = 1, CancellationToken token = default)
        {
            return await Task.Run(() => Connect(robotType, ipAddress, port, station), token);
        }

        /// <summary>
        /// 断开与机器人的连接
        /// 释放相关资源，确保连接关闭
        /// </summary>
        public void Disconnect()
        {
            try
            {
                // 如果未连接，则直接返回
                if (!IsConnected) return;

                // 断开HSL客户端连接
                _hslClient?.ConnectClose();

                // 更新连接状态
                bool wasConnected = IsConnected;
                IsConnected = false;

                // 只有状态发生变化时才触发事件
                if (wasConnected)
                {
                    ConnectionStatusChanged?.Invoke(false);
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"{DeviceName ?? "机器人"} 已断开连接");
                }
            }
            catch (Exception ex)
            {
                // 记录断开连接过程中的异常
                RLog.SaveError(ex);
                CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "机器人"} 断开连接异常", ex);
            }
        }

        /// <summary>
        /// 尝试重新连接机器人
        /// </summary>
        /// <returns>是否重连成功</returns>
        public bool TryReconnect()
        {
            // 锁定重连操作，防止并发重连
            lock (_operationLock)
            {
                // 如果正在重连中，直接返回失败
                if (_isReconnecting)
                    return false;

                _isReconnecting = true;
                try
                {
                    // 验证重连参数是否有效
                    if (string.IsNullOrWhiteSpace(_ipAddress) || _port <= 0 || _port > MaxPort || _station < 1 || _station > 247)
                        return false;

                    // 限制重连次数，避免无限重连
                    if (_reconnectAttempts >= MaxReconnectAttempts)
                    {
                        var message = $"{DeviceName ?? "机器人"} 达到最大重连次数 ({MaxReconnectAttempts})，停止尝试";
                        RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, message);
                        CommunicationErrorOccurred?.Invoke(message, null);
                        ReconnectionAttempted?.Invoke(false, _reconnectAttempts);
                        return false;
                    }

                    _reconnectAttempts++;
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"{DeviceName ?? "机器人"} 尝试第 {_reconnectAttempts} 次重连...");
                    ReconnectionAttempted?.Invoke(true, _reconnectAttempts);

                    // 重连前等待一段时间，避免频繁尝试
                    System.Threading.Thread.Sleep(ReconnectIntervalMs);

                    var result = Connect(CurrentRobotType, _ipAddress, _port, _station);
                    ReconnectionAttempted?.Invoke(result.IsSuccess, _reconnectAttempts);
                    return result.IsSuccess;
                }
                finally
                {
                    _isReconnecting = false;
                }
            }
        }

        /// <summary>
        /// 异步尝试重新连接机器人
        /// </summary>
        /// <returns>是否重连成功</returns>
        public async Task<bool> TryReconnectAsync(CancellationToken token = default)
        {
            return await Task.Run(() => TryReconnect(), token);
        }

        /// <summary>
        /// 根据机器人类型配置客户端参数
        /// </summary>
        private void ConfigureClientForRobotType(RobotType robotType)
        {
            switch (robotType)
            {
                case RobotType.Epson:
                    // 爱普生机器人特定设置
                    _hslClient.DataFormat = DataFormat.ABCD;
                    break;
                case RobotType.Fanuc:
                    // 发那科机器人特定设置
                    _hslClient.DataFormat = DataFormat.CDAB;
                    break;
                case RobotType.Efort:
                    // 埃夫特机器人特定设置
                    _hslClient.DataFormat = DataFormat.BADC;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(robotType), robotType, "不支持的机器人类型");
            }
        }
        #endregion

        #region 数据写入方法
        /// <summary>
        /// 向机器人写入bool类型数据
        /// 支持"寄存器.位"格式的地址（如"36.1"表示寄存器36的第1位）
        /// </summary>
        /// <param name="data">要写入的bool数据</param>
        /// <param name="index">写入地址索引（字符串格式，支持"寄存器.位"表示法）</param>
        /// <returns>操作结果，指示写入是否成功</returns>
        public OperateResult WriteBool(bool data, string index)
        {
            // 使用通用执行器，自动处理连接检查和异常捕获
            return ExecuteWithConnectionCheck(() =>
            {
                // 加锁确保线程安全
                lock (_operationLock)
                {
                    // 解析地址格式（支持"寄存器.位"或纯寄存器格式）
                    if (!TryParseAddress(index, out int registerAddress, out int bitIndex))
                    {
                        string errorMsg = $"无效的地址格式: {index}，正确格式应为'寄存器.位'（如36.1）或纯寄存器地址（如36）";
                        return new OperateResult { IsSuccess = false, Message = errorMsg };
                    }

                    // 验证寄存器地址有效性
                    if (registerAddress < MinRegisterAddress || registerAddress > MaxRegisterAddress)
                    {
                        string errorMsg = $"寄存器地址必须在{MinRegisterAddress}-{MaxRegisterAddress}范围内，当前值: {registerAddress}";
                        return new OperateResult { IsSuccess = false, Message = errorMsg };
                    }

                    // 验证位索引是否在有效范围内（0-15）
                    if (bitIndex < 0 || bitIndex > 15)
                    {
                        string errorMsg = $"位索引必须在0-15范围内，当前值: {bitIndex}";
                        return new OperateResult { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    //int address = registerAddress - 1;
                    //string fullAddress = $"{address}.{bitIndex}";

                    int address = registerAddress - 1;
                    string fullAddress = $"{address}";


                    // 使用Hsl库写入bool数据
                    var hslResult = _hslClient.Write(fullAddress, data);
                    return hslResult;
                }
            });
        }

        /// <summary>
        /// 异步向机器人写入bool类型数据
        /// </summary>
        public async Task<OperateResult> WriteBoolAsync(bool data, string index, CancellationToken token = default)
        {
            return await Task.Run(() => WriteBool(data, index), token);
        }

        /// <summary>
        /// 向机器人写入short（Int16）类型数据
        /// 支持纯寄存器地址格式（如"39"表示寄存器39）
        /// </summary>
        /// <param name="data">要写入的short数据</param>
        /// <param name="index">写入地址索引（字符串格式，支持纯寄存器地址表示法）</param>
        /// <returns>操作结果，指示写入是否成功</returns>
        public OperateResult WriteInt16(short data, string index)
        {
            // 参数验证：检查地址是否为空
            if (string.IsNullOrWhiteSpace(index))
            {
                string errorMsg = "地址索引不能为空";
                return new OperateResult { IsSuccess = false, Message = errorMsg };
            }

            // 使用通用执行器，自动处理连接检查和异常捕获
            return ExecuteWithConnectionCheck(() =>
            {
                // 加锁确保线程安全
                lock (_operationLock)
                {
                    // 解析地址（仅支持纯寄存器地址格式）
                    if (!TryParseRegisterAddress(index, out int registerAddress))
                    {
                        string errorMsg = $"无效的地址格式: {index}，正确格式应为纯寄存器地址（如39）";
                        return new OperateResult { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    int address = registerAddress - 1;

                    // 使用Hsl库写入Int16数据
                    var writeResult = _hslClient.Write(address.ToString(), data);
                    return writeResult;
                }
            });
        }

        /// <summary>
        /// 异步向机器人写入short类型数据
        /// </summary>
        public async Task<OperateResult> WriteInt16Async(short data, string index, CancellationToken token = default)
        {
            return await Task.Run(() => WriteInt16(data, index), token);
        }

        /// <summary>
        /// 向机器人写入int（Int32）类型数据
        /// int类型占2个寄存器，需进行数据转换
        /// 支持纯寄存器地址格式（如"39"表示寄存器39）
        /// </summary>
        /// <param name="data">要写入的int数据</param>
        /// <param name="index">写入地址索引（字符串格式，支持纯寄存器地址表示法）</param>
        /// <returns>操作结果，指示写入是否成功</returns>
        public OperateResult WriteInt32(int data, string index)
        {
            // 参数验证：检查地址是否为空
            if (string.IsNullOrWhiteSpace(index))
            {
                string errorMsg = "地址索引不能为空";
                return new OperateResult { IsSuccess = false, Message = errorMsg };
            }

            // 使用通用执行器，自动处理连接检查和异常捕获
            return ExecuteWithConnectionCheck(() =>
            {
                // 加锁确保线程安全
                lock (_operationLock)
                {
                    // 解析地址（仅支持纯寄存器地址格式）
                    if (!TryParseRegisterAddress(index, out int registerAddress))
                    {
                        string errorMsg = $"无效的地址格式: {index}，正确格式应为纯寄存器地址（如39）";
                        return new OperateResult { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    int address = registerAddress - 1;

                    // 使用Hsl库写入Int32数据
                    var writeResult = _hslClient.Write(address.ToString(), data);
                    return writeResult;
                }
            });
        }

        /// <summary>
        /// 异步向机器人写入int类型数据
        /// </summary>
        public async Task<OperateResult> WriteInt32Async(int data, string index, CancellationToken token = default)
        {
            return await Task.Run(() => WriteInt32(data, index), token);
        }

        /// <summary>
        /// 向机器人写入float类型数据
        /// float类型占2个寄存器，需进行数据转换
        /// 支持纯寄存器地址格式（如"39"表示寄存器39）
        /// </summary>
        /// <param name="data">要写入的float数据</param>
        /// <param name="index">写入地址索引（字符串格式，支持纯寄存器地址表示法）</param>
        /// <returns>操作结果，指示写入是否成功</returns>
        public OperateResult WriteFloat(float data, string index)
        {
            // 参数验证：检查地址是否为空
            if (string.IsNullOrWhiteSpace(index))
            {
                string errorMsg = "地址索引不能为空";
                return new OperateResult { IsSuccess = false, Message = errorMsg };
            }

            // 使用通用执行器，自动处理连接检查和异常捕获
            return ExecuteWithConnectionCheck(() =>
            {
                // 加锁确保线程安全
                lock (_operationLock)
                {
                    // 解析地址（仅支持纯寄存器地址格式）
                    if (!TryParseRegisterAddress(index, out int registerAddress))
                    {
                        string errorMsg = $"无效的地址格式: {index}，正确格式应为纯寄存器地址（如39）";
                        return new OperateResult { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    int address = registerAddress - 1;

                    // 使用Hsl库写入float数据
                    var writeResult = _hslClient.Write(address.ToString(), data);
                    return writeResult;
                }
            });
        }

        /// <summary>
        /// 异步向机器人写入float类型数据
        /// </summary>
        public async Task<OperateResult> WriteFloatAsync(float data, string index, CancellationToken token = default)
        {
            return await Task.Run(() => WriteFloat(data, index), token);
        }

        /// <summary>
        /// 向机器人写入short数组
        /// </summary>
        public OperateResult WriteInt16Array(string startIndex, short[] data)
        {
            if (data == null || data.Length == 0)
                return new OperateResult { IsSuccess = false, Message = "写入的数据数组不能为空" };

            // 参数验证：检查地址是否为空
            if (string.IsNullOrWhiteSpace(startIndex))
            {
                string errorMsg = "起始地址索引不能为空";
                return new OperateResult { IsSuccess = false, Message = errorMsg };
            }

            // 使用通用执行器，自动处理连接检查和异常捕获
            return ExecuteWithConnectionCheck(() =>
            {
                // 加锁确保线程安全
                lock (_operationLock)
                {
                    // 解析地址（仅支持纯寄存器地址格式）
                    if (!TryParseRegisterAddress(startIndex, out int registerAddress))
                    {
                        string errorMsg = $"无效的地址格式: {startIndex}，正确格式应为纯寄存器地址（如39）";
                        return new OperateResult { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    int address = registerAddress - 1;

                    // 使用Hsl库写入short数组
                    var writeResult = _hslClient.Write(address.ToString(), data);
                    return writeResult;
                }
            });
        }

        /// <summary>
        /// 异步向机器人写入short数组
        /// </summary>
        public async Task<OperateResult> WriteInt16ArrayAsync(string startIndex, short[] data, CancellationToken token = default)
        {
            return await Task.Run(() => WriteInt16Array(startIndex, data), token);
        }
        #endregion

        #region 数据读取方法
        /// <summary>
        /// 从机器人读取单个bool值
        /// 支持"寄存器.位"格式的地址（如"36.1"表示寄存器36的第1位）
        /// </summary>
        /// <param name="index">地址索引（字符串格式，支持"寄存器.位"表示法）</param>
        /// <returns>包含bool值的操作结果</returns>
        public OperateResult<bool> ReadBool(string index)
        {
            // 参数验证：检查地址是否为空
            if (string.IsNullOrWhiteSpace(index))
            {
                string errorMsg = "地址索引不能为空";
                return new OperateResult<bool> { IsSuccess = false, Message = errorMsg };
            }

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_operationLock)
                {
                    // 解析地址格式（支持"寄存器.位"或纯寄存器格式）
                    if (!TryParseAddress(index, out int registerAddress, out int bitIndex))
                    {
                        string errorMsg = $"无效的地址格式: {index}，正确格式应为'寄存器.位'（如36.1）或纯寄存器地址（如36）";
                        return new OperateResult<bool> { IsSuccess = false, Message = errorMsg };
                    }

                    // 验证寄存器地址有效性
                    if (registerAddress < MinRegisterAddress || registerAddress > MaxRegisterAddress)
                    {
                        string errorMsg = $"寄存器地址必须在{MinRegisterAddress}-{MaxRegisterAddress}范围内，当前值: {registerAddress}";
                        return new OperateResult<bool> { IsSuccess = false, Message = errorMsg };
                    }

                    // 验证位索引是否在有效范围内（0-15）
                    if (bitIndex < 0 || bitIndex > 15)
                    {
                        string errorMsg = $"位索引必须在0-15范围内，当前值: {bitIndex}";
                        return new OperateResult<bool> { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    int address = registerAddress - 1;
                   // string fullAddress = $"{address}.{bitIndex}";
                    string fullAddress = $"{address}";
                    // 使用Hsl库读取bool值，支持"地址.位"格式
                    var hslResult = _hslClient.ReadBool(fullAddress);
                    return hslResult;
                }
            });
        }



        /// <summary>
        /// 专门读取 Epson 机器人 DI(输入) / DO(输出)【已修正：统一HSL返回类型】
        /// Epson 专属规则：
        /// DI 输入 = Modbus 线圈(ReadBool)
        /// DO 输出 = Modbus 离散输入(ReadDiscreteInput)
        /// </summary>
        public OperateResult<bool> ReadEpsonIoPoint(Robot_Modbus robot, ModbusCommunicationDioDto point)
        {
            // ============== Epson 核心规则 ==============
            switch (point.点位类型)
            {
                // 机器人输入 DI → 读线圈 ReadBool
                case "输出":
                    return robot.ReadBool(point.点位索引);

                // 机器人输出 DO → 读离散输入 ReadDiscreteInput（修复方法名！）
                case "输入":
                    return robot.ReadDiscrete(point.点位索引);

                // 缺失的默认分支，必须加，否则编译报错
                default:
                    return new OperateResult<bool>("未知的IO点位类型");
            }
        }





        /// <summary>
        /// 从机器人读取单个bool值
        /// 支持"寄存器.位"格式的地址（如"36.1"表示寄存器36的第1位）
        /// </summary>
        /// <param name="index">地址索引（字符串格式，支持"寄存器.位"表示法）</param>
        /// <returns>包含bool值的操作结果</returns>
        public OperateResult<bool> ReadDiscrete(string index)
        {
            // 参数验证：检查地址是否为空
            if (string.IsNullOrWhiteSpace(index))
            {
                string errorMsg = "地址索引不能为空";
                return new OperateResult<bool> { IsSuccess = false, Message = errorMsg };
            }

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_operationLock)
                {
                    // 解析地址格式（支持"寄存器.位"或纯寄存器格式）
                    if (!TryParseAddress(index, out int registerAddress, out int bitIndex))
                    {
                        string errorMsg = $"无效的地址格式: {index}，正确格式应为'寄存器.位'（如36.1）或纯寄存器地址（如36）";
                        return new OperateResult<bool> { IsSuccess = false, Message = errorMsg };
                    }

                    // 验证寄存器地址有效性
                    if (registerAddress < MinRegisterAddress || registerAddress > MaxRegisterAddress)
                    {
                        string errorMsg = $"寄存器地址必须在{MinRegisterAddress}-{MaxRegisterAddress}范围内，当前值: {registerAddress}";
                        return new OperateResult<bool> { IsSuccess = false, Message = errorMsg };
                    }

                    // 验证位索引是否在有效范围内（0-15）
                    if (bitIndex < 0 || bitIndex > 15)
                    {
                        string errorMsg = $"位索引必须在0-15范围内，当前值: {bitIndex}";
                        return new OperateResult<bool> { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    int address = registerAddress - 1;
                    // string fullAddress = $"{address}.{bitIndex}";

                    string fullAddress = $"{address}";

                    // 使用Hsl库读取bool值，支持"地址.位"格式
                    var hslResult = _hslClient.ReadDiscrete(fullAddress);
                    return hslResult;
                }
            });
        }






        /// <summary>
        /// 异步从机器人读取单个bool值
        /// </summary>
        public async Task<OperateResult<bool>> ReadBoolAsync(string index, CancellationToken token = default)
        {
            return await Task.Run(() => ReadBool(index), token);
        }

        /// <summary>
        /// 从机器人读取short（Int16）类型数据
        /// 支持纯寄存器地址格式（如"39"表示寄存器39）
        /// </summary>
        /// <param name="index">地址索引（字符串格式，支持纯寄存器地址表示法）</param>
        /// <returns>包含short值的操作结果</returns>
        public OperateResult<short> ReadInt16(string index)
        {
            // 参数验证：检查地址是否为空
            if (string.IsNullOrWhiteSpace(index))
            {
                string errorMsg = "地址索引不能为空";
                return new OperateResult<short> { IsSuccess = false, Message = errorMsg };
            }

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_operationLock)
                {
                    // 解析地址（仅支持纯寄存器地址格式）
                    if (!TryParseRegisterAddress(index, out int registerAddress))
                    {
                        string errorMsg = $"无效的地址格式: {index}，正确格式应为纯寄存器地址（如39）";
                        return new OperateResult<short> { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    int address = registerAddress - 1;

                    // 使用Hsl库读取Int16值
                    var hslResult = _hslClient.ReadInt16(address.ToString());
                    return hslResult;
                }
            });
        }

        /// <summary>
        /// 异步从机器人读取short值
        /// </summary>
        public async Task<OperateResult<short>> ReadInt16Async(string index, CancellationToken token = default)
        {
            return await Task.Run(() => ReadInt16(index), token);
        }

        /// <summary>
        /// 从机器人读取int（Int32）类型数据
        /// int类型占2个寄存器，需进行数据转换
        /// 支持纯寄存器地址格式（如"39"表示寄存器39）
        /// </summary>
        /// <param name="index">地址索引（字符串格式，支持纯寄存器地址表示法）</param>
        /// <returns>包含int值的操作结果</returns>
        public OperateResult<int> ReadInt32(string index)
        {
            // 参数验证：检查地址是否为空
            if (string.IsNullOrWhiteSpace(index))
            {
                string errorMsg = "地址索引不能为空";
                return new OperateResult<int> { IsSuccess = false, Message = errorMsg };
            }

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_operationLock)
                {
                    // 解析地址（仅支持纯寄存器地址格式）
                    if (!TryParseRegisterAddress(index, out int registerAddress))
                    {
                        string errorMsg = $"无效的地址格式: {index}，正确格式应为纯寄存器地址（如39）";
                        return new OperateResult<int> { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    int address = registerAddress - 1;

                    // 使用Hsl库读取Int32值
                    var hslResult = _hslClient.ReadInt32(address.ToString());
                    return hslResult;
                }
            });
        }

        /// <summary>
        /// 异步从机器人读取int值
        /// </summary>
        public async Task<OperateResult<int>> ReadInt32Async(string index, CancellationToken token = default)
        {
            return await Task.Run(() => ReadInt32(index), token);
        }

        /// <summary>
        /// 从机器人读取float类型数据
        /// float类型占2个寄存器，需进行数据转换
        /// 支持纯寄存器地址格式（如"39"表示寄存器39）
        /// </summary>
        /// <param name="index">地址索引（字符串格式，支持纯寄存器地址表示法）</param>
        /// <returns>包含float值的操作结果</returns>
        public OperateResult<float> ReadFloat(string index)
        {
            // 参数验证：检查地址是否为空
            if (string.IsNullOrWhiteSpace(index))
            {
                string errorMsg = "地址索引不能为空";
                return new OperateResult<float> { IsSuccess = false, Message = errorMsg };
            }

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_operationLock)
                {
                    // 解析地址（仅支持纯寄存器地址格式）
                    if (!TryParseRegisterAddress(index, out int registerAddress))
                    {
                        string errorMsg = $"无效的地址格式: {index}，正确格式应为纯寄存器地址（如39）";
                        return new OperateResult<float> { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    int address = registerAddress - 1;

                    // 使用Hsl库读取float值
                    var hslResult = _hslClient.ReadFloat(address.ToString());
                    return hslResult;
                }
            });
        }

        /// <summary>
        /// 异步从机器人读取float值
        /// </summary>
        public async Task<OperateResult<float>> ReadFloatAsync(string index, CancellationToken token = default)
        {
            return await Task.Run(() => ReadFloat(index), token);
        }

        /// <summary>
        /// 从机器人读取short数组
        /// </summary>
        /// <param name="startIndex">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <returns>包含short数组的操作结果</returns>
        public OperateResult<short[]> ReadInt16Array(string startIndex, ushort length)
        {
            if (length <= 0)
                return new OperateResult<short[]> { IsSuccess = false, Message = "读取长度必须大于0" };

            // 参数验证：检查地址是否为空
            if (string.IsNullOrWhiteSpace(startIndex))
            {
                string errorMsg = "起始地址索引不能为空";
                return new OperateResult<short[]> { IsSuccess = false, Message = errorMsg };
            }

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_operationLock)
                {
                    // 解析地址（仅支持纯寄存器地址格式）
                    if (!TryParseRegisterAddress(startIndex, out int registerAddress))
                    {
                        string errorMsg = $"无效的地址格式: {startIndex}，正确格式应为纯寄存器地址（如39）";
                        return new OperateResult<short[]> { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    int address = registerAddress - 1;

                    // 检查读取范围是否超出最大地址
                    if (address + length > MaxRegisterAddress)
                    {
                        string errorMsg = $"读取范围超出最大地址 {MaxRegisterAddress}";
                        return new OperateResult<short[]> { IsSuccess = false, Message = errorMsg };
                    }

                    // 使用Hsl库读取short数组
                    var hslResult = _hslClient.ReadInt16(address.ToString(), length);
                    return hslResult;
                }
            });
        }

        /// <summary>
        /// 异步从机器人读取short数组
        /// </summary>
        public async Task<OperateResult<short[]>> ReadInt16ArrayAsync(string startIndex, ushort length, CancellationToken token = default)
        {
            return await Task.Run(() => ReadInt16Array(startIndex, length), token);
        }

        /// <summary>
        /// 从机器人读取bool数组
        /// </summary>
        /// <param name="index">起始地址索引</param>
        /// <param name="count">要读取的元素数量</param>
        /// <returns>包含bool数组的操作结果</returns>
        public OperateResult<bool[]> ReadBoolArray(string index, ushort count)
        {
            if (count <= 0)
                return new OperateResult<bool[]> { IsSuccess = false, Message = "读取数量必须大于0" };

            // 参数验证：检查地址是否为空
            if (string.IsNullOrWhiteSpace(index))
            {
                string errorMsg = "地址索引不能为空";
                return new OperateResult<bool[]> { IsSuccess = false, Message = errorMsg };
            }

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_operationLock)
                {
                    // 解析地址（仅支持纯寄存器地址格式）
                    if (!TryParseRegisterAddress(index, out int registerAddress))
                    {
                        string errorMsg = $"无效的地址格式: {index}，正确格式应为纯寄存器地址（如39）";
                        return new OperateResult<bool[]> { IsSuccess = false, Message = errorMsg };
                    }

                    // 地址转换：将1基地址转换为0基地址
                    int address = registerAddress - 1;

                    // 使用Hsl库读取bool数组
                    var hslResult = _hslClient.ReadBool(address.ToString(), count);
                    return hslResult;
                }
            });
        }

        /// <summary>
        /// 异步从机器人读取bool数组
        /// </summary>
        public async Task<OperateResult<bool[]>> ReadBoolArrayAsync(string index, ushort count, CancellationToken token = default)
        {
            return await Task.Run(() => ReadBoolArray(index, count), token);
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 解析地址字符串为寄存器地址和位索引
        /// 支持两种格式："寄存器.位"（如"36.1"）和纯寄存器地址（如"36"）
        /// </summary>
        /// <param name="addressString">地址字符串</param>
        /// <param name="registerAddress">输出：寄存器地址</param>
        /// <param name="bitIndex">输出：位索引，默认值为0</param>
        /// <returns>解析是否成功</returns>
        private bool TryParseAddress(string addressString, out int registerAddress, out int bitIndex)
        {
            // 初始化输出参数
            registerAddress = 0;
            bitIndex = 0;

            // 检查地址是否为空
            if (string.IsNullOrWhiteSpace(addressString))
                return false;

            // 处理包含位索引的地址格式（如"36.1"）
            if (addressString.Contains('.'))
            {
                // 分割地址部分，移除空项
                string[] parts = addressString.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                // 必须正好包含两部分：寄存器地址和位索引
                if (parts.Length != 2)
                    return false;

                // 尝试解析寄存器地址和位索引为整数
                return int.TryParse(parts[0], out registerAddress) &&
                       int.TryParse(parts[1], out bitIndex);
            }

            // 处理纯寄存器地址格式（如"36"）
            return int.TryParse(addressString, out registerAddress);
        }

        /// <summary>
        /// 解析地址字符串为寄存器地址（仅支持纯寄存器地址格式）
        /// </summary>
        /// <param name="addressString">地址字符串（如"39"）</param>
        /// <param name="registerAddress">输出：寄存器地址</param>
        /// <returns>解析是否成功</returns>
        private bool TryParseRegisterAddress(string addressString, out int registerAddress)
        {
            registerAddress = 0;

            // 检查地址是否为空
            if (string.IsNullOrWhiteSpace(addressString))
                return false;

            // 检查是否包含位索引（不支持带位索引的格式）
            if (addressString.Contains('.'))
                return false;

            // 尝试解析为整数并验证范围
            if (int.TryParse(addressString, out registerAddress))
            {
                return registerAddress >= MinRegisterAddress && registerAddress <= MaxRegisterAddress;
            }

            return false;
        }

        /// <summary>
        /// 带连接检查的操作执行器
        /// 通用方法，封装了连接状态检查、异常捕获和结果处理逻辑
        /// 避免在每个读写方法中重复编写相同代码
        /// </summary>
        /// <typeparam name="T">操作结果类型，必须继承自OperateResult</typeparam>
        /// <param name="operation">要执行的具体操作</param>
        /// <returns>操作结果对象</returns>
        private T ExecuteWithConnectionCheck<T>(Func<T> operation) where T : OperateResult, new()
        {
            try
            {
                // 检查资源是否已释放
                if (_isDisposed)
                {
                    string errorMsg = $"{DeviceName ?? "机器人"} 资源已释放，无法执行操作";
                    CommunicationErrorOccurred?.Invoke(errorMsg, null);
                    return new T { IsSuccess = false, Message = errorMsg };
                }

                // 检查连接状态，未连接则尝试重连
                if (!IsConnected && AutoReconnectEnabled)
                {
                    // 尝试自动重连
                    if (!TryReconnect())
                    {
                        string errorMsg = "机器人未连接，重连失败";
                        CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "机器人"} {errorMsg}", null);
                        return new T { IsSuccess = false, Message = errorMsg };
                    }
                }

                // 检查客户端实例是否有效
                if (_hslClient == null)
                {
                    string errorMsg = "Hsl客户端未初始化，请先建立连接";
                    CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "机器人"} {errorMsg}", null);
                    return new T { IsSuccess = false, Message = errorMsg };
                }

                // 执行具体的操作（读取或写入）
                T result = operation.Invoke();

                // 记录操作日志
                LogOperationResult(result);

                // 操作失败时，将连接状态设为断开并尝试重连
                if (!result.IsSuccess)
                {
                    UpdateConnectionStatus(false);
                    CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "机器人"} 操作失败: {result.Message}", null);

                    // 如果操作失败且启用了自动重连，尝试一次重连
                    if (AutoReconnectEnabled && _reconnectAttempts < MaxReconnectAttempts)
                    {
                        TryReconnect();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                // 捕获异常，更新连接状态并返回错误信息
                UpdateConnectionStatus(false);
                RLog.SaveError(ex);
                CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "机器人"} 操作异常", ex);
                return new T { IsSuccess = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 更新连接状态并触发事件
        /// </summary>
        private void UpdateConnectionStatus(bool connected)
        {
            if (IsConnected != connected)
            {
                IsConnected = connected;
                ConnectionStatusChanged?.Invoke(connected);
            }
        }

        /// <summary>
        /// 记录操作结果日志
        /// </summary>
        private void LogOperationResult(OperateResult result)
        {
            if (!result.IsSuccess)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"{DeviceName ?? "机器人"} 操作失败: {result.Message}");
            }
        }
        #endregion

        #region 资源释放
        /// <summary>
        /// 释放资源的公共方法
        /// 实现IDisposable接口，供外部显式释放资源
        /// </summary>
        public void Dispose()
        {
            // 调用实际的释放方法，参数true表示释放托管资源
            Dispose(true);
            // 告知垃圾回收器不需要再调用析构函数
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 资源释放的实际实现
        /// 遵循IDisposable模式的标准实现，分离托管和非托管资源释放
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            // 防止重复释放
            if (_isDisposed) return;

            if (disposing)
            {
                // 释放托管资源
                Disconnect();
                _hslClient = null;
            }

            // 标记为已释放
            _isDisposed = true;
        }

        /// <summary>
        /// 析构函数
        /// 当对象被垃圾回收时调用，确保资源最终被释放
        /// </summary>
        ~Robot_Modbus()
        {
            // 只释放非托管资源
            Dispose(false);
        }
        #endregion
    }
}