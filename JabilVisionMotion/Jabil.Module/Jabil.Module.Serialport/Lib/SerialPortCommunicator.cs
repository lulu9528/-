using BasicClassLibrary.RexHelps;
using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Jabil.Module.Serialport.Lib
{
    /// <summary>
    /// 串口通讯管理类，封装了串口通讯的核心功能
    /// 采用面向对象设计，提供了简洁易用的接口同时保持灵活性
    /// 支持自定义编码、数据包判断、缓冲区配置等高级功能
    /// 实现了IDisposable接口，确保资源正确释放
    /// </summary>
    public class SerialPortCommunicator : IDisposable
    {
        #region 枚举定义
        /// <summary>
        /// 数据发送格式枚举，指定发送数据时使用的编码格式
        /// </summary>
        public enum SendFormat
        {
            /// <summary>
            /// 十六进制格式发送
            /// 适用于二进制协议或需要精确控制每个字节的场景
            /// </summary>
            Hex,
            /// <summary>
            /// 字符串格式发送
            /// 使用配置的DataEncoding进行编码
            /// </summary>
            String
        }
        #endregion

        #region 私有字段
        /// <summary>
        /// 底层串口对象，封装了系统串口功能
        /// </summary>
        private SerialPort _serialPort;

        /// <summary>
        /// 资源释放标志，volatile确保多线程环境下的可见性
        /// 用于控制线程退出和资源释放
        /// </summary>
        private volatile bool _isDisposed;

        /// <summary>
        /// 同步锁对象，用于确保多线程操作的线程安全
        /// 保护共享资源的访问
        /// </summary>
        private readonly object _syncLock = new object();

        /// <summary>
        /// 存储最近接收的完整数据包
        /// 提供给外部访问的接收数据
        /// </summary>
        private byte[] _receivedData = Array.Empty<byte>();

        /// <summary>
        /// 动态接收缓冲区，用于临时存储接收的数据
        /// 直到数据包完整后才会转移到_receivedData
        /// </summary>
        private byte[] _receiveBuffer;

        /// <summary>
        /// 缓冲区当前位置指针，指示下一个接收字节的存储位置
        /// </summary>
        private int _bufferPosition = 0;

        /// <summary>
        /// 数据接收事件信号，用于通知数据处理线程有新数据到达
        /// 实现生产者-消费者模式的数据同步
        /// </summary>
        private readonly AutoResetEvent _dataReceivedEvent = new AutoResetEvent(false);

        /// <summary>
        /// 数据处理线程，负责判断数据包完整性并触发事件
        /// 与接收事件分离，避免在中断处理中执行耗时操作
        /// </summary>
        private Thread _processingThread;
        #endregion

        #region 公共属性
        /// <summary>
        /// 获取当前串口是否打开的状态
        /// 使用空合并运算符处理_serialPort为null的情况
        /// </summary>
        public bool IsOpen => _serialPort?.IsOpen ?? false;

        /// <summary>
        /// 获取当前计算机上可用的串口列表
        /// 调用SerialPort静态方法获取系统可用端口
        /// </summary>
        public string[] AvailablePorts => SerialPort.GetPortNames();

        /// <summary>
        /// 获取或设置串口波特率
        /// 常见值：9600, 19200, 38400, 115200等
        /// </summary>
        public int BaudRate
        {
            get => _serialPort.BaudRate;
            set => _serialPort.BaudRate = value;
        }

        /// <summary>
        /// 获取或设置串口校验位
        /// 用于数据传输中的错误检测
        /// </summary>
        public Parity Parity
        {
            get => _serialPort.Parity;
            set => _serialPort.Parity = value;
        }

        /// <summary>
        /// 获取或设置数据位长度
        /// 通常为8位，某些设备可能使用7位
        /// </summary>
        public int DataBits
        {
            get => _serialPort.DataBits;
            set => _serialPort.DataBits = value;
        }

        /// <summary>
        /// 获取或设置停止位
        /// 用于标识一个数据帧的结束
        /// </summary>
        public StopBits StopBits
        {
            get => _serialPort.StopBits;
            set => _serialPort.StopBits = value;
        }

        /// <summary>
        /// 获取或设置端口名称
        /// 如"COM1", "COM3"等
        /// 端口打开时不允许修改
        /// </summary>
        public string PortName
        {
            get => _serialPort.PortName;
            set
            {
                if (IsOpen)
                    throw new InvalidOperationException("无法在端口打开状态下修改端口名称");

                _serialPort.PortName = value;
            }
        }

        /// <summary>
        /// 接收超时时间（毫秒）
        /// 用于判断一包数据是否接收完成的超时阈值
        /// 超过此时间无新数据则认为当前数据包已完整
        /// 默认值：500ms
        /// </summary>
        public int ReceiveTimeout { get; set; } = 500;

        /// <summary>
        /// 数据编码格式
        /// 用于字符串和字节数组之间的转换
        /// 默认值：ASCII编码
        /// </summary>
        public Encoding DataEncoding { get; set; } = Encoding.ASCII;

        /// <summary>
        /// 最大接收缓冲区大小（字节）
        /// 防止缓冲区无限增长导致内存问题
        /// 默认值：4096字节
        /// </summary>
        public int MaxBufferSize { get; set; } = 4096;

        /// <summary>
        /// 发送重试次数
        /// 发送失败时的重试次数，提高通讯可靠性
        /// 默认值：1次（共尝试2次）
        /// </summary>
        public int SendRetryCount { get; set; } = 1;

        /// <summary>
        /// 发送重试间隔（毫秒）
        /// 两次重试之间的等待时间
        /// 默认值：100ms
        /// </summary>
        public int RetryInterval { get; set; } = 100;

        /// <summary>
        /// 接收到的字符串数据
        /// 使用配置的DataEncoding对接收的字节数据进行编码
        /// 方便直接获取字符串形式的数据
        /// </summary>
        public string StringData => DataEncoding.GetString(_receivedData);

        /// <summary>
        /// 接收到的字节数据
        /// get访问器返回数据副本，防止外部修改内部数据
        /// set访问器允许手动设置接收数据
        /// </summary>
        public byte[] ReceivedData
        {
            get => (byte[])_receivedData.Clone();
            set => _receivedData = value;
        }

        /// <summary>
        /// 数据包完成判断委托
        /// 允许外部自定义判断数据包是否完整的逻辑
        /// 参数：缓冲区数据、当前数据长度
        /// 返回值：true表示数据包完整
        /// </summary>
        public Func<byte[], int, bool> PacketCompleteChecker { get; set; }

        /// <summary>
        /// 串口信息摘要
        /// 简洁展示当前串口配置信息
        /// 格式：端口名称 [波特率,数据位校验位停止位]
        /// 如："COM1 [115200,8N1]"
        /// </summary>
        public string SerialPortInfo => IsOpen
            ? $"{_serialPort.PortName} [{_serialPort.BaudRate},{_serialPort.DataBits}{_serialPort.Parity}{(int)_serialPort.StopBits}]"
            : "串口未打开";
        #endregion

        #region 事件定义
        /// <summary>
        /// 数据接收事件参数类
        /// 封装了数据接收事件中需要传递的信息
        /// </summary>
        public class DataReceivedEventArgs : EventArgs
        {
            /// <summary>
            /// 接收到的字节数据
            /// 原始二进制数据，可用于精确解析
            /// </summary>
            public byte[] Data { get; }

            /// <summary>
            /// 接收到的字符串数据
            /// 使用配置的编码方式转换后的字符串
            /// </summary>
            public string StringData { get; }

            /// <summary>
            /// 数据接收时间戳
            /// 记录数据接收完成的时间
            /// </summary>
            public DateTime Timestamp { get; }

            /// <summary>
            /// 初始化数据接收事件参数
            /// </summary>
            /// <param name="data">接收到的字节数组</param>
            /// <param name="encoding">数据编码方式</param>
            public DataReceivedEventArgs(byte[] data, Encoding encoding)
            {
                Data = data ?? throw new ArgumentNullException(nameof(data));
                StringData = encoding.GetString(data);
                Timestamp = DateTime.Now;
            }
        }

        /// <summary>
        /// 数据接收事件
        /// 当接收到完整的数据包时触发
        /// 事件参数包含接收到的数据和时间戳
        /// </summary>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>
        /// 通讯错误事件
        /// 当串口通讯发生错误时触发
        /// 事件参数包含错误信息和发生时间
        /// </summary>
        public event EventHandler<ExceptionEventArgs> CommunicationError;

        /// <summary>
        /// 调试日志事件
        /// 用于输出调试信息，便于开发和问题排查
        /// 可在生产环境中忽略
        /// </summary>
        public event Action<string> DebugLog;

        /// <summary>
        /// 串口状态变化事件
        /// 当串口打开或关闭时触发
        /// 事件参数为当前状态（true为打开，false为关闭）
        /// </summary>
        public event EventHandler<bool> PortStatusChanged; // 参数：是否打开
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 初始化串口通讯管理器
        /// 构造函数负责基础参数设置和资源初始化
        /// </summary>
        /// <param name="portName">端口名称，如"COM1"</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="parity">校验位</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        public SerialPortCommunicator(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            // 初始化串口对象并设置基本参数
            _serialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = baudRate,
                Parity = parity,
                DataBits = dataBits,
                StopBits = stopBits,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            // 初始化接收缓冲区
            _receiveBuffer = new byte[MaxBufferSize];

            // 注册数据接收事件处理方法
            _serialPort.DataReceived += SerialPort_DataReceived;

            // 启动数据处理线程
            StartProcessingThread();
        }

        /// <summary>
        /// 启动数据处理线程
        /// 确保线程只被启动一次
        /// 配置为后台线程，避免阻碍程序退出
        /// </summary>
        private void StartProcessingThread()
        {
            // 检查线程是否已存在且正在运行
            if (_processingThread?.IsAlive ?? false)
                return;

            // 创建并配置数据处理线程
            _processingThread = new Thread(ProcessReceivedData)
            {
                IsBackground = true,  // 后台线程，主程序退出时自动终止
                Name = "SerialPortDataProcessor", // 线程名称，便于调试
                Priority = ThreadPriority.BelowNormal // 低于正常优先级，避免影响主线程
            };
            _processingThread.Start();
            DebugLog?.Invoke("数据处理线程已启动");
        }
        #endregion

        #region 串口连接管理
        /// <summary>
        /// 打开串口
        /// 线程安全的实现，确保同一时间只有一个操作
        /// </summary>
        /// <returns>是否打开成功</returns>
        public bool Open()
        {
            // 已打开则直接返回成功
            if (IsOpen)
            {
                DebugLog?.Invoke("串口已处于打开状态");
                return true;
            }

            // 使用锁确保线程安全
            lock (_syncLock)
            {
                try
                {
                    // 验证端口名称是否设置
                    if (string.IsNullOrEmpty(_serialPort.PortName))
                        throw new InvalidOperationException("未设置端口名称");

                    // 打开串口
                    _serialPort.Open();
                    DebugLog?.Invoke($"串口 {_serialPort.PortName} 已打开");
                    // 触发端口状态变化事件
                    PortStatusChanged?.Invoke(this, true);
                    return true;
                }
                catch (Exception ex)
                {
                    // 处理异常并通知
                    OnCommunicationError(ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// 关闭串口
        /// 线程安全的实现
        /// </summary>
        public void Close()
        {
            // 已关闭则直接返回
            if (!IsOpen)
            {
                DebugLog?.Invoke("串口已处于关闭状态");
                return;
            }

            // 使用锁确保线程安全
            lock (_syncLock)
            {
                try
                {
                    string portName = _serialPort.PortName;
                    _serialPort.Close();
                    DebugLog?.Invoke($"串口 {portName} 已关闭");
                    // 触发端口状态变化事件
                    PortStatusChanged?.Invoke(this, false);
                }
                catch (Exception ex)
                {
                    // 处理异常并通知
                    OnCommunicationError(ex);
                }
            }
        }

        /// <summary>
        /// 切换串口状态（打开/关闭）
        /// 如果指定端口已打开则关闭，否则打开指定端口
        /// </summary>
        /// <param name="portName">端口名称</param>
        /// <returns>操作后的状态（true为打开，false为关闭）</returns>
        public bool TogglePort(string portName)
        {
            if (string.IsNullOrEmpty(portName))
                throw new ArgumentNullException(nameof(portName), "端口名称不能为空");

            // 如果当前端口已打开且是目标端口，则关闭
            if (IsOpen && _serialPort.PortName == portName)
            {
                Close();
                return false;
            }

            // 切换到目标端口并打开
            _serialPort.PortName = portName;
            return Open();
        }
        #endregion

        #region 数据发送
        /// <summary>
        /// 发送数据
        /// 根据指定的格式将数据发送到串口
        /// </summary>
        /// <param name="data">要发送的数据字符串</param>
        /// <param name="format">数据格式（十六进制或字符串）</param>
        /// <exception cref="InvalidOperationException">端口未打开时抛出</exception>
        /// <exception cref="ArgumentException">数据格式不正确时抛出</exception>
        public void SendData(string data, SendFormat format)
        {
            // 验证端口是否打开
            if (!IsOpen)
                throw new InvalidOperationException("端口未打开，请先打开端口");

            // 验证数据是否为空
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("发送数据不能为空", nameof(data));

            // 根据格式转换数据
            byte[] byteData = format == SendFormat.Hex
                ? ConvertHexToBytes(data)
                : DataEncoding.GetBytes(data);

            // 调用字节发送方法
            SendData(byteData);
        }

        /// <summary>
        /// 发送字节数据
        /// 核心发送方法，实现带重试机制的字节数据发送
        /// </summary>
        /// <param name="data">要发送的字节数组</param>
        public void SendData(byte[] data)
        {
            // 验证端口是否打开
            if (!IsOpen)
                throw new InvalidOperationException("端口未打开，请先打开端口");

            // 验证数据是否为空
            if (data == null || data.Length == 0)
                throw new ArgumentException("发送数据不能为空", nameof(data));

            // 使用锁确保线程安全
            lock (_syncLock)
            {
                // 带重试机制的发送逻辑
                for (int i = 0; i <= SendRetryCount; i++)
                {
                    try
                    {
                        // 清空接收缓冲区和已接收数据，准备接收新的响应
                        lock (_receiveBuffer)
                        {
                            _bufferPosition = 0;
                            _receivedData = Array.Empty<byte>();
                        }

                        // 发送数据
                        _serialPort.Write(data, 0, data.Length);
                        DebugLog?.Invoke($"发送成功，长度：{data.Length}字节，重试次数：{i}");
                        return;
                    }
                    catch (TimeoutException ex)
                    {
                        // 处理超时异常，进行重试
                        DebugLog?.Invoke($"发送超时，正在重试（{i + 1}/{SendRetryCount + 1}）：{ex.Message}");
                        if (i == SendRetryCount)
                        {
                            // 达到最大重试次数，触发错误事件并抛出异常
                            OnCommunicationError(ex);
                            throw new TimeoutException($"发送超时，已达到最大重试次数（{SendRetryCount}）", ex);
                        }
                        // 重试间隔
                        Thread.Sleep(RetryInterval);
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        OnCommunicationError(ex);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 将十六进制字符串转换为字节数组
        /// 支持空格分隔的十六进制格式，如"A1 B2 C3"
        /// </summary>
        /// <param name="hex">十六进制字符串</param>
        /// <returns>转换后的字节数组</returns>
        private byte[] ConvertHexToBytes(string hex)
        {
            // 验证十六进制字符串有效性
            if (IsInvalidHex(hex))
                throw new ArgumentException("无效的十六进制格式", nameof(hex));

            try
            {
                // 调用外部方法进行转换
                return RString.From16ToBtyes(hex);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("十六进制转换失败", nameof(hex), ex);
            }
        }

        /// <summary>
        /// 检查十六进制字符串是否有效
        /// 有效字符为0-9, A-F, a-f以及空格
        /// </summary>
        /// <param name="hex">十六进制字符串</param>
        /// <returns>无效返回true，有效返回false</returns>
        private bool IsInvalidHex(string hex)
        {
            return string.IsNullOrWhiteSpace(hex) ||
                   System.Text.RegularExpressions.Regex.IsMatch(hex, @"[^A-Fa-f0-9\s]");
        }
        #endregion

        #region 数据接收
        /// <summary>
        /// 串口数据接收事件处理
        /// 仅负责将数据读取到缓冲区，不处理业务逻辑
        /// 保证事件处理的高效性
        /// </summary>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // 检查端口是否打开
                if (!IsOpen) return;

                // 获取可读取的字节数
                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead <= 0) return;

                // 读取数据到缓冲区（确保线程安全）
                lock (_receiveBuffer)
                {
                    // 计算剩余空间
                    int remainingSpace = _receiveBuffer.Length - _bufferPosition;

                    // 如果空间不足，动态扩容（不超过最大限制）
                    if (bytesToRead > remainingSpace)
                    {
                        // 计算新大小，最多扩容到1.5倍，不超过最大缓冲区大小
                        int newSize = Math.Min(_receiveBuffer.Length * 3 / 2, MaxBufferSize);
                        if (newSize <= _receiveBuffer.Length) // 已达到最大限制
                        {
                            OnCommunicationError(new InvalidOperationException(
                                $"接收缓冲区已满（{_receiveBuffer.Length}字节），无法接收更多数据"));
                            return;
                        }

                        // 扩容缓冲区
                        Array.Resize(ref _receiveBuffer, newSize);
                        DebugLog?.Invoke($"缓冲区已扩容至 {newSize} 字节");
                    }

                    // 读取数据到缓冲区
                    int bytesRead = _serialPort.Read(_receiveBuffer, _bufferPosition, bytesToRead);
                    _bufferPosition += bytesRead;
                    DebugLog?.Invoke($"接收数据 {bytesRead} 字节，缓冲区累计 {_bufferPosition} 字节");
                }

                // 通知数据处理线程有新数据
                _dataReceivedEvent.Set();
            }
            catch (Exception ex)
            {
                // 处理异常
                OnCommunicationError(ex);
            }
        }

        /// <summary>
        /// 数据处理线程
        /// 负责判断数据包是否完整，并在完整时触发事件
        /// 与接收事件分离，避免影响数据接收的实时性
        /// </summary>
        private void ProcessReceivedData()
        {
            DebugLog?.Invoke("数据处理线程开始运行");

            // 循环处理，直到资源被释放
            while (!_isDisposed)
            {
                // 等待数据或超时（最多等待ReceiveTimeout毫秒）
                bool dataReceived = _dataReceivedEvent.WaitOne(ReceiveTimeout);

                // 检查是否已释放资源，如已释放则退出
                if (_isDisposed) break;

                // 检查端口是否关闭
                if (!IsOpen)
                {
                    // 如果端口已关闭，清空缓冲区
                    lock (_receiveBuffer)
                    {
                        if (_bufferPosition > 0)
                        {
                            DebugLog?.Invoke($"端口关闭，清空缓冲区 {_bufferPosition} 字节数据");
                            _bufferPosition = 0;
                        }
                    }
                    continue;
                }

                // 检查是否有数据且满足数据包完成条件
                if (_bufferPosition > 0)
                {
                    bool isComplete = false;

                    // 优先使用自定义判断逻辑，否则用超时判断
                    if (PacketCompleteChecker != null)
                    {
                        lock (_receiveBuffer)
                        {
                            isComplete = PacketCompleteChecker(_receiveBuffer, _bufferPosition);
                        }
                    }
                    else
                    {
                        // 超时判断：没有新数据且缓冲区有数据，认为数据包完整
                        isComplete = !dataReceived;
                    }

                    // 如果数据包完整，则处理
                    if (isComplete)
                    {
                        lock (_receiveBuffer)
                        {
                            // 复制完整数据包
                            byte[] completeData = new byte[_bufferPosition];
                            Array.Copy(_receiveBuffer, completeData, _bufferPosition);
                            DebugLog?.Invoke($"数据包接收完成，长度：{completeData.Length}字节");

                            // 更新接收数据（线程安全）
                            lock (_syncLock)
                            {
                                _receivedData = completeData;
                            }

                            // 触发数据接收事件
                            OnDataReceived(completeData);

                            // 重置缓冲区，准备接收下一个数据包
                            _bufferPosition = 0;
                        }
                    }
                }
            }

            DebugLog?.Invoke("数据处理线程已退出");
        }

        /// <summary>
        /// 触发数据接收事件
        /// 保护方法，确保事件触发的线程安全
        /// </summary>
        /// <param name="data">接收到的数据</param>
        protected virtual void OnDataReceived(byte[] data)
        {
            DataReceived?.Invoke(this, new DataReceivedEventArgs(data, DataEncoding));
        }
        #endregion

        #region 错误处理
        /// <summary>
        /// 触发通讯错误事件
        /// 集中处理错误通知，确保错误信息被正确传递
        /// </summary>
        /// <param name="ex">发生的异常</param>
        protected virtual void OnCommunicationError(Exception ex)
        {
            DebugLog?.Invoke($"通讯错误：{ex.Message}");
            CommunicationError?.Invoke(this, new ExceptionEventArgs(ex));
        }
        #endregion

        #region 资源释放
        /// <summary>
        /// 释放资源的公共接口
        /// 实现IDisposable接口
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实际实现
        /// 采用标准的Dispose模式
        /// </summary>
        /// <param name="disposing">是否手动释放（true为手动调用，false为垃圾回收）</param>
        protected virtual void Dispose(bool disposing)
        {
            // 避免重复释放
            if (_isDisposed) return;
            _isDisposed = true;

            DebugLog?.Invoke("开始释放串口资源");

            if (disposing)
            {
                // 释放托管资源
                // 移除事件订阅
                _serialPort.DataReceived -= SerialPort_DataReceived;
                // 关闭串口
                Close();
                // 释放串口对象
                _serialPort.Dispose();

                // 终止数据处理线程
                _dataReceivedEvent.Set(); // 唤醒等待的线程
                if (_processingThread?.IsAlive ?? false)
                {
                    // 等待线程退出（最多等待1秒）
                    if (!_processingThread.Join(1000))
                    {
                        DebugLog?.Invoke("数据处理线程未能正常退出，强制终止");
                        _processingThread.Abort();
                    }
                }
                // 释放事件对象
                _dataReceivedEvent.Dispose();
            }

            DebugLog?.Invoke("串口资源释放完成");
        }

        /// <summary>
        /// 析构函数
        /// 确保在垃圾回收时释放非托管资源
        /// </summary>
        ~SerialPortCommunicator()
        {
            Dispose(false);
        }
        #endregion
    }

    /// <summary>
    /// 异常事件参数类
    /// 封装异常信息和时间戳
    /// </summary>
    public class ExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// 发生的异常
        /// 包含错误信息和堆栈跟踪
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 异常发生的时间戳
        /// 记录异常发生的时间
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 初始化异常事件参数
        /// </summary>
        /// <param name="ex">异常对象</param>
        public ExceptionEventArgs(Exception ex)
        {
            Exception = ex ?? throw new ArgumentNullException(nameof(ex));
            Timestamp = DateTime.Now;
        }
    }
}
