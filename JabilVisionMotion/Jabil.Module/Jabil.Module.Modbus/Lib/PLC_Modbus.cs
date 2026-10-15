using BasicClassLibrary.RexHelps;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.ModBus;
using Jabil.Lib;
using Jabil.Module.Log.Lib;
using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.Modbus.Lib
{
    /// <summary>
    /// Modbus协议PLC通信类
    /// 负责与支持Modbus TCP协议的PLC设备进行数据交互
    /// 实现了连接管理、数据读写及资源释放等功能
    /// </summary>
    public class PLC_Modbus : IDisposable
    {
        /// <summary>
        /// PLC类型枚举
        /// 用于标识当前连接的PLC型号，便于后续扩展支持多种PLC
        /// </summary>
        public enum PLCType
        {
            /// <summary>
            /// 汇川H5U系列PLC
            /// </summary>
            H5u,
            /// <summary>
            /// 西门子S7系列PLC（Modbus TCP兼容模式）
            /// </summary>
            SiemensS7,
            /// <summary>
            /// 三菱FX5系列PLC
            /// </summary>
            MitsubishiFX5,
            /// <summary>
            /// 通用Modbus TCP设备
            /// </summary>
            GenericModbus
        }

        // 私有字段
        /// <summary>
        /// Modbus TCP客户端实例
        /// 用于底层的Modbus协议通信实现
        /// </summary>
        private ModbusTcpNet _busTcpClient;

        /// <summary>
        /// 线程同步锁对象
        /// 确保多线程环境下数据读写操作的线程安全
        /// </summary>
        private readonly object _lockObj = new object();

        /// <summary>
        /// 资源释放标识
        /// 用于IDisposable模式中防止资源重复释放
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// 重连次数计数器
        /// </summary>
        private int _reconnectAttempts;

        /// <summary>
        /// 重连锁定标识，防止并发重连
        /// </summary>
        private bool _isReconnecting;

        /// <summary>
        /// 最后一次通信时间戳
        /// 用于监控通信活性
        /// </summary>
        private DateTime _lastCommunicationTime = DateTime.MinValue;

        // 常量定义
        /// <summary>
        /// 默认接收超时时间(毫秒)
        /// 定义与PLC通信时的响应超时阈值
        /// </summary>
        private const int DefaultReceiveTimeout = 3000;

        /// <summary>
        /// 默认发送超时时间(毫秒)
        /// </summary>
        private const int DefaultSendTimeout = 3000;

        /// <summary>
        /// 最小端口号
        /// 用于端口号参数验证的下限值
        /// </summary>
        private const int MinPort = 1;

        /// <summary>
        /// 最大端口号
        /// 用于端口号参数验证的上限值
        /// </summary>
        private const int MaxPort = 65535;

        /// <summary>
        /// 最小寄存器地址
        /// </summary>
        private const int MinRegisterAddress = 0;

        /// <summary>
        /// 最大寄存器地址
        /// </summary>
        private const int MaxRegisterAddress = 65535;

        /// <summary>
        /// 最小位索引
        /// </summary>
        private const int MinBitIndex = 0;

        /// <summary>
        /// 最大位索引
        /// </summary>
        private const int MaxBitIndex = 15;

        /// <summary>
        /// 最大重连次数
        /// </summary>
        private const int MaxReconnectAttempts = 3;

        /// <summary>
        /// 重连间隔时间(毫秒)
        /// </summary>
        private const int ReconnectIntervalMs = 1000;

        /// <summary>
        /// 通信超时阈值(毫秒)
        /// 超过此时长无通信则判定为通信超时
        /// </summary>
        private const int CommunicationTimeoutThresholdMs = 10000;

        /// <summary>
        /// 设备名称
        /// 用于标识当前通信的PLC设备，便于日志记录和故障排查
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// PLC类型
        /// 只读属性，记录当前连接的PLC型号
        /// </summary>
        public PLCType PlcType { get; private set; }

        /// <summary>
        /// 是否连接
        /// 标识与PLC的当前连接状态
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; private set; }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// 站号
        /// </summary>
        public byte Station { get; private set; }

        /// <summary>
        /// 接收超时时间(毫秒)
        /// </summary>
        public int ReceiveTimeout
        {
            get => _busTcpClient?.ReceiveTimeOut ?? DefaultReceiveTimeout;
            set
            {
                if (_busTcpClient != null && value > 0)
                    _busTcpClient.ReceiveTimeOut = value;
            }
        }

        /// <summary>
        /// 发送超时时间(毫秒)
        /// </summary>
        public int SendTimeout
        {
            get => _busTcpClient?.ConnectTimeOut ?? DefaultSendTimeout;
            set
            {
                if (_busTcpClient != null && value > 0)
                    _busTcpClient.ConnectTimeOut = value;
            }
        }

        /// <summary>
        /// 通信状态变化事件
        /// 包含连接状态和通信活性信息
        /// </summary>
        public event Action<bool, bool> CommunicationStatusChanged;

        /// <summary>
        /// 通信错误事件
        /// </summary>
        public event Action<string> CommunicationErrorOccurred;

        /// <summary>
        /// 通信活性检查
        /// 判断是否在规定时间内有成功通信
        /// </summary>
        public bool IsCommunicationAlive =>
            (DateTime.Now - _lastCommunicationTime).TotalMilliseconds < CommunicationTimeoutThresholdMs;

        /// <summary>
        /// 构造函数
        /// 初始化Modbus TCP客户端实例
        /// </summary>
        public PLC_Modbus()
        {
            _busTcpClient = new ModbusTcpNet();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        public PLC_Modbus(string deviceName) : this()
        {
            DeviceName = deviceName;
        }


        /// <summary>
        /// 更新通信时间戳
        /// </summary>
        private void UpdateCommunicationTime()
        {
            _lastCommunicationTime = DateTime.Now;
            CommunicationStatusChanged?.Invoke(IsConnected, IsCommunicationAlive);
        }

        /// <summary>
        /// 连接PLC设备
        /// </summary>
        /// <param name="plcType">PLC类型，指定要连接的PLC型号</param>
        /// <param name="ipAddress">IP地址，PLC设备的网络地址</param>
        /// <param name="port">端口号，Modbus通信端口，通常为502</param>
        /// <param name="station">站号，PLC的Modbus站地址，默认值为1</param>
        /// <returns>操作结果，包含IsSuccess指示连接是否成功，Message包含相关信息</returns>
        /// <exception cref="ArgumentNullException">当IP地址为空或空白时抛出</exception>
        /// <exception cref="ArgumentException">当IP地址格式无效时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">当端口号超出有效范围时抛出</exception>
        public OperateResult Connect(PLCType plcType, string ipAddress, int port, byte station = 1)
        {
            // 参数验证：确保IP地址有效
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress), "IP地址不能为空");

            // 验证IP地址格式是否正确
            if (!IPAddress.TryParse(ipAddress, out _))
                throw new ArgumentException("无效的IP地址格式", nameof(ipAddress));

            // 验证端口号是否在有效范围内
            if (port < MinPort || port > MaxPort)
                throw new ArgumentOutOfRangeException(nameof(port), $"端口号必须在{MinPort}-{MaxPort}范围内");

            try
            {
                // 记录连接参数
                PlcType = plcType;
                IpAddress = ipAddress;
                Port = port;
                Station = station;

                // 断开现有连接
                Disconnect();

                // 根据PLC类型创建对应的Modbus客户端实例并设置特定参数
                _busTcpClient = CreateModbusClient(plcType, ipAddress, port, station);

                // 设置超时时间
                _busTcpClient.ReceiveTimeOut = DefaultReceiveTimeout;
                _busTcpClient.ConnectTimeOut = DefaultSendTimeout;
                //设置高低位
                _busTcpClient.IsStringReverse = true;
                // 尝试连接到PLC服务器
                var result = _busTcpClient.ConnectServer();

                // 更新连接状态
                UpdateConnectionStatus(result.IsSuccess);

                // 重置重连计数器
                _reconnectAttempts = 0;

                // 记录连接日志
                var deviceIdentifier = $"{DeviceName ?? "PLC"} ({ipAddress}:{port})";
                if (result.IsSuccess)
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"{deviceIdentifier} 连接成功，站号: {station}");
                    UpdateCommunicationTime();
                }
                else
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"{deviceIdentifier} 连接失败: {result.Message}");
                    CommunicationErrorOccurred?.Invoke($"{deviceIdentifier} 连接失败: {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                // 发生异常时标记为未连接状态
                UpdateConnectionStatus(false);
                // 记录异常日志
                RLog.SaveError(ex);
                CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "PLC"} 连接异常: {ex.Message}");
                // 返回包含异常信息的失败结果
                return new OperateResult { IsSuccess = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 异步连接PLC设备（带取消令牌）
        /// </summary>
        public async Task<OperateResult> ConnectAsync(PLCType plcType, string ipAddress, int port,
                                                     byte station = 1, CancellationToken token = default)
        {
            return await Task.Run(() => Connect(plcType, ipAddress, port, station), token);
        }

        /// <summary>
        /// 根据PLC类型创建对应的Modbus客户端
        /// </summary>
        private ModbusTcpNet CreateModbusClient(PLCType plcType, string ipAddress, int port, byte station)
        {
            var client = new ModbusTcpNet(ipAddress, port, station);

            // 根据不同PLC类型设置特定参数
            switch (plcType)
            {
                case PLCType.H5u:
                    // 汇川H5U特定设置
                    client.DataFormat = DataFormat.CDAB;
                    break;
                case PLCType.SiemensS7:
                    // 西门子S7系列特定设置
                    client.DataFormat = DataFormat.ABCD;
                    break;
                case PLCType.MitsubishiFX5:
                    // 三菱FX5系列特定设置
                    client.DataFormat = DataFormat.BADC;
                    break;
                case PLCType.GenericModbus:
                default:
                    // 通用Modbus设备使用默认设置
                    break;
            }

            return client;
        }

        /// <summary>
        /// 断开与PLC的连接
        /// </summary>
        public void Disconnect()
        {
            try
            {
                // 仅在客户端busTcpClient实例存在且当前处于连接状态时执行断开操作
                if (_busTcpClient != null && IsConnected)
                {
                    // 关闭连接
                    _busTcpClient.ConnectClose();
                    // 更新连接状态
                    UpdateConnectionStatus(false);
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"{DeviceName ?? "PLC"} 已断开连接");
                }
            }
            catch (Exception ex)
            {
                // 记录断开连接过程中的异常
                RLog.SaveError(ex);
                CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "PLC"} 断开连接异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试重新连接PLC
        /// </summary>
        /// <returns>是否重连成功</returns>
        public bool TryReconnect()
        {
            // 双重检查锁定，防止并发重连
            if (_isReconnecting)
                return false;

            lock (_lockObj)
            {
                if (_isReconnecting)
                    return false;

                _isReconnecting = true;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(IpAddress) || Port <= 0)
                    return false;

                // 限制重连次数，避免无限重连
                if (_reconnectAttempts >= MaxReconnectAttempts)
                {
                    var message = $"{DeviceName ?? "PLC"} 达到最大重连次数 ({MaxReconnectAttempts})，停止尝试";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, message);
                    CommunicationErrorOccurred?.Invoke(message);
                    return false;
                }

                _reconnectAttempts++;
                var attemptMessage = $"{DeviceName ?? "PLC"} 尝试第 {_reconnectAttempts} 次重连...";
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, attemptMessage);

                // 重连前等待一段时间，避免频繁尝试
                Thread.Sleep(ReconnectIntervalMs);

                var result = Connect(PlcType, IpAddress, Port, Station);
                return result.IsSuccess;
            }
            finally
            {
                _isReconnecting = false;
            }
        }

        /// <summary>
        /// 异步尝试重新连接PLC（带取消令牌）
        /// </summary>
        public async Task<bool> TryReconnectAsync(CancellationToken token = default)
        {
            return await Task.Run(() => TryReconnect(), token);
        }

        /// <summary>
        /// 更新连接状态并触发事件
        /// </summary>
        private void UpdateConnectionStatus(bool connected)
        {
            if (IsConnected != connected)
            {
                IsConnected = connected;
                CommunicationStatusChanged?.Invoke(connected, IsCommunicationAlive);

                // 记录连接状态变化
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"{DeviceName ?? "PLC"} 连接状态: {(connected ? "已连接" : "已断开")}");
            }
        }

        /// <summary>
        /// 向PLC写入bool类型数据
        /// </summary>
        /// <param name="data">要写入的bool值</param>
        /// <param name="index">PLC中的数据地址索引，仅支持"36"或"36.1"格式</param>
        /// <returns>操作结果，包含写入操作的执行状态</returns>
        public OperateResult WriteBool(bool data, string index)
        {
            // 验证地址格式
            var validationResult = ValidateAddressFormat(index, true);
            if (!validationResult.IsSuccess)
                return validationResult;

            // 通过连接检查包装器执行写入操作
            return ExecuteWithConnectionCheck(() =>
            {
                // 加锁确保线程安全
                lock (_lockObj)
                {
                    return _busTcpClient.Write(index, data);
                }
            });
        }

        /// <summary>
        /// 异步向PLC写入bool类型数据（带取消令牌）
        /// </summary>
        public async Task<OperateResult> WriteBoolAsync(bool data, string index, CancellationToken token = default)
        {
            return await Task.Run(() => WriteBool(data, index), token);
        }

        /// <summary>
        /// 向PLC写入short类型数据
        /// </summary>
        /// <param name="data">要写入的short值</param>
        /// <param name="index">PLC中的数据地址索引，仅支持"36"格式</param>
        /// <returns>操作结果，包含写入操作的执行状态</returns>
        public OperateResult WriteInt16(short data, string index)
        {
            // 验证地址格式（整数类型不支持位索引）
            var validationResult = ValidateAddressFormat(index, false);
            if (!validationResult.IsSuccess)
                return validationResult;

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.Write(index, data);
                }
            });
        }

        /// <summary>
        /// 异步向PLC写入short类型数据（带取消令牌）
        /// </summary>
        public async Task<OperateResult> WriteInt16Async(short data, string index, CancellationToken token = default)
        {
            return await Task.Run(() => WriteInt16(data, index), token);
        }

        /// <summary>
        /// 向PLC写入int类型数据
        /// </summary>
        /// <param name="data">要写入的int值</param>
        /// <param name="index">PLC中的数据地址索引，仅支持"36"格式</param>
        /// <returns>操作结果，包含写入操作的执行状态</returns>
        public OperateResult WriteInt32(int data, string index)
        {
            // 验证地址格式（整数类型不支持位索引）
            var validationResult = ValidateAddressFormat(index, false);
            if (!validationResult.IsSuccess)
                return validationResult;

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.Write(index, data);
                }
            });
        }

        /// <summary>
        /// 异步向PLC写入int类型数据（带取消令牌）
        /// </summary>
        public async Task<OperateResult> WriteInt32Async(int data, string index, CancellationToken token = default)
        {
            return await Task.Run(() => WriteInt32(data, index), token);
        }

        /// <summary>
        /// 向PLC写入float类型数据
        /// </summary>
        /// <param name="data">要写入的float值</param>
        /// <param name="index">PLC中的数据地址索引</param>
        /// <returns>操作结果</returns>
        public OperateResult WriteFloat(float data, string index)
        {
            var validationResult = ValidateAddressFormat(index, false);
            if (!validationResult.IsSuccess)
                return validationResult;

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.Write(index, data);
                }
            });
        }

        /// <summary>
        /// 异步向PLC写入float类型数据（带取消令牌）
        /// </summary>
        public async Task<OperateResult> WriteFloatAsync(float data, string index, CancellationToken token = default)
        {
            return await Task.Run(() => WriteFloat(data, index), token);
        }

        /// <summary>
        /// 向PLC写入short数组
        /// </summary>
        public OperateResult WriteInt16Array(string startIndex, short[] data)
        {
            if (data == null || data.Length == 0)
                return new OperateResult { IsSuccess = false, Message = "写入的数据数组不能为空" };

            var validationResult = ValidateAddressFormat(startIndex, false);
            if (!validationResult.IsSuccess)
                return validationResult;

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.Write(startIndex, data);
                }
            });
        }

        /// <summary>
        /// 异步向PLC写入short数组（带取消令牌）
        /// </summary>
        public async Task<OperateResult> WriteInt16ArrayAsync(string startIndex, short[] data, CancellationToken token = default)
        {
            return await Task.Run(() => WriteInt16Array(startIndex, data), token);
        }


        /// <summary>
        /// 异步向PLC写入string数组（带取消令牌）
        /// </summary>
        public async Task<OperateResult> WriteStringAsync(string startIndex, string data, CancellationToken token = default)
        {
            return await Task.Run(() => WriteString(startIndex, data), token);
        }


        /// <summary>
        /// 向PLC写入字符串
        /// </summary>
        public OperateResult WriteString(string startIndex, string data)
        {
            if (data == null || data.Length == 0)
                return new OperateResult { IsSuccess = false, Message = "写入的数据数组不能为空" };

            var validationResult = ValidateAddressFormat(startIndex, false);
            if (!validationResult.IsSuccess)
                return validationResult;

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.Write(startIndex, data,46);
                }
            });
        }



        /// <summary>
        /// 从PLC读取bool数组
        /// </summary>
        /// <param name="index">起始地址索引，仅支持"36"格式</param>
        /// <param name="count">要读取的元素数量</param>
        /// <returns>操作结果，包含读取到的bool数组（如果成功）</returns>
        public OperateResult<bool[]> ReadBoolArray(string index, ushort count)
        {
            if (count <= 0)
                return new OperateResult<bool[]> { IsSuccess = false, Message = "读取数量必须大于0" };

            // 验证地址格式
            var validationResult = ValidateAddressFormat(index, false);
            if (!validationResult.IsSuccess)
                return new OperateResult<bool[]> { IsSuccess = false, Message = validationResult.Message };

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.ReadBool(index, count);
                }
            });
        }

        /// <summary>
        /// 异步从PLC读取bool数组（带取消令牌）
        /// </summary>
        public async Task<OperateResult<bool[]>> ReadBoolArrayAsync(string index, ushort count, CancellationToken token = default)
        {
            return await Task.Run(() => ReadBoolArray(index, count), token);
        }

        /// <summary>
        /// 从PLC读取单个bool值
        /// </summary>
        /// <param name="index">数据地址索引，仅支持"36"或"36.1"格式</param>
        /// <returns>操作结果，包含读取到的bool值（如果成功）</returns>
        public OperateResult<bool> ReadBool(string index)
        {
            // 验证地址格式
            var validationResult = ValidateAddressFormat(index, true);
            if (!validationResult.IsSuccess)
                return new OperateResult<bool> { IsSuccess = false, Message = validationResult.Message };

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.ReadBool(index);
                }
            });
        }

        /// <summary>
        /// 异步从PLC读取单个bool值（带取消令牌）
        /// </summary>
        public async Task<OperateResult<bool>> ReadBoolAsync(string index, CancellationToken token = default)
        {
            return await Task.Run(() => ReadBool(index), token);
        }

        /// <summary>
        /// 从PLC读取short值
        /// </summary>
        /// <param name="index">数据地址索引，仅支持"36"格式</param>
        /// <returns>操作结果，包含读取到的short值（如果成功）</returns>
        public OperateResult<short> ReadInt16(string index)
        {
            // 验证地址格式（整数类型不支持位索引）
            var validationResult = ValidateAddressFormat(index, false);
            if (!validationResult.IsSuccess)
                return new OperateResult<short> { IsSuccess = false, Message = validationResult.Message };

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.ReadInt16(index);
                }
            });
        }


        public OperateResult<string> ReadString(string index)
        {
            // 验证地址格式（整数类型不支持位索引）
            var validationResult = ValidateAddressFormat(index, false);
            if (!validationResult.IsSuccess)
                return new OperateResult<string> { IsSuccess = false, Message = validationResult.Message };

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.ReadString(index, 21);
                    
                }
            });
        }


        /// <summary>
        /// 异步从PLC读取short值（带取消令牌）
        /// </summary>
        public async Task<OperateResult<short>> ReadInt16Async(string index, CancellationToken token = default)
        {
            return await Task.Run(() => ReadInt16(index), token);
        }


        /// <summary>
        /// 异步从PLC读取string值（带取消令牌）
        /// </summary>
        public async Task<OperateResult<string>> ReadStringAsync(string index, CancellationToken token = default)
        {
            return await Task.Run(() => ReadString(index), token);
        }
        /// <summary>
        /// 从PLC读取int值
        /// </summary>
        /// <param name="index">数据地址索引，仅支持"36"格式</param>
        /// <returns>操作结果，包含读取到的int值（如果成功）</returns>
        public OperateResult<int> ReadInt32(string index)
        {
            // 验证地址格式（整数类型不支持位索引）
            var validationResult = ValidateAddressFormat(index, false);
            if (!validationResult.IsSuccess)
                return new OperateResult<int> { IsSuccess = false, Message = validationResult.Message };

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.ReadInt32(index);
                }
            });
        }

        /// <summary>
        /// 异步从PLC读取int值（带取消令牌）
        /// </summary>
        public async Task<OperateResult<int>> ReadInt32Async(string index, CancellationToken token = default)
        {
            return await Task.Run(() => ReadInt32(index), token);
        }

        /// <summary>
        /// 从PLC读取float值
        /// </summary>
        /// <param name="index">数据地址索引</param>
        /// <returns>操作结果，包含读取到的float值</returns>
        public OperateResult<float> ReadFloat(string index)
        {
            var validationResult = ValidateAddressFormat(index, false);
            if (!validationResult.IsSuccess)
                return new OperateResult<float> { IsSuccess = false, Message = validationResult.Message };

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.ReadFloat(index);
                }
            });
        }

        /// <summary>
        /// 异步从PLC读取float值（带取消令牌）
        /// </summary>
        public async Task<OperateResult<float>> ReadFloatAsync(string index, CancellationToken token = default)
        {
            return await Task.Run(() => ReadFloat(index), token);
        }

        /// <summary>
        /// 从PLC读取short数组
        /// </summary>
        /// <param name="startIndex">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <returns>操作结果，包含读取到的short数组</returns>
        public OperateResult<short[]> ReadInt16Array(string startIndex, ushort length)
        {
            if (length <= 0)
                return new OperateResult<short[]> { IsSuccess = false, Message = "读取长度必须大于0" };

            var validationResult = ValidateAddressFormat(startIndex, false);
            if (!validationResult.IsSuccess)
                return new OperateResult<short[]> { IsSuccess = false, Message = validationResult.Message };

            return ExecuteWithConnectionCheck(() =>
            {
                lock (_lockObj)
                {
                    return _busTcpClient.ReadInt16(startIndex, length);
                }
            });
        }

        /// <summary>
        /// 异步从PLC读取short数组（带取消令牌）
        /// </summary>
        public async Task<OperateResult<short[]>> ReadInt16ArrayAsync(string startIndex, ushort length, CancellationToken token = default)
        {
            return await Task.Run(() => ReadInt16Array(startIndex, length), token);
        }

        /// <summary>
        /// 验证地址格式是否有效
        /// </summary>
        /// <param name="index">要验证的地址字符串</param>
        /// <param name="allowBitIndex">是否允许包含位索引（如"36.1"）</param>
        /// <returns>验证结果，成功或失败信息</returns>
        private OperateResult ValidateAddressFormat(string index, bool allowBitIndex)
        {
            // 检查地址是否为空
            if (string.IsNullOrWhiteSpace(index))
            {
                return new OperateResult { IsSuccess = false, Message = "地址索引不能为空" };
            }

            // 检查是否包含多个点号
            if (index.Count(c => c == '.') > 1)
            {
                return new OperateResult { IsSuccess = false, Message = $"无效的地址格式: {index}，只支持纯数字或数字.数字格式（如36或36.1）" };
            }

            // 处理带位索引的地址格式（如"36.1"）
            if (index.Contains('.') && allowBitIndex)
            {
                string[] parts = index.Split('.');

                // 验证寄存器地址部分
                if (!int.TryParse(parts[0], out int registerAddress) ||
                    registerAddress < MinRegisterAddress ||
                    registerAddress > MaxRegisterAddress)
                {
                    return new OperateResult { IsSuccess = false, Message = $"寄存器地址必须是{MinRegisterAddress}-{MaxRegisterAddress}之间的整数: {parts[0]}" };
                }

                // 验证位索引部分
                if (!int.TryParse(parts[1], out int bitIndex) ||
                    bitIndex < MinBitIndex ||
                    bitIndex > MaxBitIndex)
                {
                    return new OperateResult { IsSuccess = false, Message = $"位索引必须是{MinBitIndex}-{MaxBitIndex}之间的整数: {parts[1]}" };
                }

                return new OperateResult { IsSuccess = true };
            }

            // 不允许带位索引的情况
            if (index.Contains('.') && !allowBitIndex)
            {
                return new OperateResult { IsSuccess = false, Message = $"地址格式无效: {index}，此操作不支持位索引（如.1）" };
            }

            // 验证纯寄存器地址（如"36"）
            if (!int.TryParse(index, out int address) ||
                address < MinRegisterAddress ||
                address > MaxRegisterAddress)
            {
                return new OperateResult { IsSuccess = false, Message = $"地址必须是{MinRegisterAddress}-{MaxRegisterAddress}之间的整数: {index}" };
            }

            return new OperateResult { IsSuccess = true };
        }

        /// <summary>
        /// 带连接检查的操作执行器
        /// 统一处理连接状态检查、异常捕获和结果处理
        /// </summary>
        /// <typeparam name="T">操作结果类型，必须继承自OperateResult</typeparam>
        /// <param name="operation">要执行的操作委托</param>
        /// <returns>操作执行结果</returns>
        private T ExecuteWithConnectionCheck<T>(Func<T> operation) where T : OperateResult, new()
        {
            try
            {
                // 检查资源是否已释放
                if (_isDisposed)
                {
                    var message = $"{DeviceName ?? "PLC"} 资源已释放，无法执行操作";
                    CommunicationErrorOccurred?.Invoke(message);
                    return new T { IsSuccess = false, Message = message };
                }

                // 检查PLC是否处于连接状态
                if (!IsConnected)
                {
                    // 尝试自动重连
                    if (!TryReconnect())
                    {
                        var message = "PLC未连接，重连失败";
                        CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "PLC"} {message}");
                        return new T { IsSuccess = false, Message = message };
                    }
                }

                // 检查客户端实例是否有效
                if (_busTcpClient == null)
                {
                    var message = "客户端实例为空";
                    CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "PLC"} {message}");
                    return new T { IsSuccess = false, Message = message };
                }

                // 执行实际操作并获取结果
                var result = operation.Invoke();

                // 记录操作日志
                LogOperationResult(result);

                // 根据操作结果更新连接状态
                if (!result.IsSuccess)
                {
                    UpdateConnectionStatus(false);
                    CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "PLC"} 操作失败: {result.Message}");

                    // 如果操作失败，尝试一次重连
                    if (_reconnectAttempts < MaxReconnectAttempts)
                    {
                        TryReconnect();
                    }
                }
                else
                {
                    // 操作成功更新通信时间
                    UpdateCommunicationTime();
                }

                return result;
            }
            catch (Exception ex)
            {
                // 发生异常时标记为未连接
                UpdateConnectionStatus(false);
                // 记录异常日志
                RLog.SaveError(ex);
                CommunicationErrorOccurred?.Invoke($"{DeviceName ?? "PLC"} 操作异常: {ex.Message}");
                // 返回包含异常信息的失败结果
                return new T { IsSuccess = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 记录操作结果日志
        /// </summary>
        private void LogOperationResult(OperateResult result)
        {
            if (!result.IsSuccess)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"{DeviceName ?? "PLC"} 操作失败: {result.Message}");
            }
        }

        /// <summary>
        /// 释放资源
        /// 实现IDisposable接口，供外部显式释放资源
        /// </summary>
        public void Dispose()
        {
            // 调用带参数的Dispose方法，释放托管和非托管资源
            Dispose(true);
            // 告知垃圾回收器不需要再调用析构函数
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实际实现
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            // 防止重复释放
            if (_isDisposed) return;

            // 如果是显式调用Dispose()，释放托管资源
            if (disposing)
            {
                // 断开连接
                Disconnect();
                // 释放客户端实例
                if (_busTcpClient != null)
                {
                    _busTcpClient = null;
                }
            }

            // 标记为已释放
            _isDisposed = true;
        }

        /// <summary>
        /// 析构函数
        /// 当对象被垃圾回收时调用，用于释放非托管资源
        /// </summary>
        ~PLC_Modbus()
        {
            // 仅释放非托管资源
            Dispose(false);
        }
    }
}
