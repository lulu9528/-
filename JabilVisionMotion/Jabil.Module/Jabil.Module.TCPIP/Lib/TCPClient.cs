using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.TCPIP.Lib
{
    /// <summary>
    /// TCP客户端通信类，提供完整的TCP通信功能
    /// 设计目标：
    /// 1. 兼容.NET Framework（包括旧版本如4.0及以上）
    /// 2. 支持同步/异步连接、发送、接收操作
    /// 3. 提供完善的事件通知（连接状态、数据到达、异常等）
    /// 4. 支持自定义编码格式和超时设置
    /// 5. 实现优雅的资源管理和异常处理
    /// </summary>
    public class TCPClient : IDisposable
    {
        #region 事件定义
        /// <summary>
        /// 客户端成功建立连接后触发的事件
        /// 触发时机：TCP三次握手完成，与远程主机建立稳定连接后
        /// </summary>
        public event ClientConnectedEventHandler ClientConnected;

        /// <summary>
        /// 客户端连接关闭后触发的事件
        /// 触发时机：主动调用Close()或远程主机断开连接后
        /// </summary>
        public event ClientClosedEventHandler ClientClosed;

        /// <summary>
        /// 接收到远程主机发送的数据时触发的事件
        /// 触发时机：成功读取到有效数据（字节数>0）后
        /// </summary>
        public event DataArrivedEventHandler DataArrived;

        /// <summary>
        /// 通信过程中发生异常时触发的事件
        /// 触发时机：连接、发送、接收等操作抛出异常时
        /// </summary>
        public event ExceptionEventHandler ExceptionOccurred;

        /// <summary>
        /// 连接成功事件的委托类型
        /// </summary>
        public delegate void ClientConnectedEventHandler();

        /// <summary>
        /// 连接关闭事件的委托类型
        /// </summary>
        public delegate void ClientClosedEventHandler();

        /// <summary>
        /// 数据到达事件的委托类型
        /// </summary>
        /// <param name="data">接收到的原始字节数据</param>
        /// <param name="length">有效数据长度（字节数）</param>
        public delegate void DataArrivedEventHandler(byte[] data, int length);

        /// <summary>
        /// 异常事件的委托类型
        /// </summary>
        /// <param name="ex">发生的异常对象，包含错误信息</param>
        public delegate void ExceptionEventHandler(Exception ex);
        #endregion

        #region 属性
        /// <summary>
        /// 客户端的标识名称，可用于日志或UI显示
        /// 可自由设置，不影响通信功能
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 最后一次接收到的字符串数据
        /// 内部通过Encoding属性指定的编码格式从字节数组转换而来
        /// </summary>
        public string ReceiveString { get;  set; }

        /// <summary>
        /// 获取当前TCP连接的状态
        /// 实现逻辑：检查底层Socket是否存在且处于连接状态
        /// </summary>
        public bool IsConnected => _clientSocket?.Connected ?? false;

        /// <summary>
        /// 本地主机名称（当前运行客户端的设备名称）
        /// 初始化时通过Dns.GetHostName()获取
        /// </summary>
        public string LocalHostName { get; private set; }

        /// <summary>
        /// 本地IP地址（客户端用于通信的IP）
        /// 连接成功后通过Socket.LocalEndPoint解析获取
        /// </summary>
        public string LocalIP { get; private set; }

        /// <summary>
        /// 本地端口号（客户端用于通信的端口）
        /// 类型为int，避免字符串类型的转换错误
        /// 连接成功后通过Socket.LocalEndPoint解析获取
        /// </summary>
        public int LocalPort { get; private set; }

        /// <summary>
        /// 远程主机的IP地址
        /// 连接成功后通过Socket.RemoteEndPoint解析获取
        /// </summary>
        public string RemoteIP { get; private set; }

        /// <summary>
        /// 远程主机的端口号
        /// 类型为int，与构造函数传入的远程端口对应
        /// 连接成功后通过Socket.RemoteEndPoint解析确认
        /// </summary>
        public int RemotePort { get; private set; }

        /// <summary>
        /// 远程主机的名称（如域名）
        /// 若构造函数传入的是主机名而非IP，则此处为该主机名
        /// 否则通过Dns.GetHostEntry解析远程IP获取
        /// </summary>
        public string RemoteHostName { get; private set; }

        /// <summary>
        /// 数据编码格式，用于字符串与字节数组的转换
        /// 默认值为UTF8，可根据通信协议需求修改（如GB2312、ASCII等）
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// 连接超时时间（单位：毫秒）
        /// 超过此时间未建立连接则视为失败
        /// 默认值为10000ms（10秒），可根据网络环境调整
        /// </summary>
        public int ConnectTimeout { get; set; } = 10000;

        /// <summary>
        /// 发送数据超时时间（单位：毫秒）
        /// 超过此时间未完成发送则抛出异常
        /// 默认值为5000ms（5秒），可根据数据大小和网络环境调整
        /// </summary>
        public int SendTimeout { get; set; } = 5000;
        #endregion

        #region 私有字段
        /// <summary>
        /// 底层TCP通信的Socket实例
        /// 封装了TCP协议的核心通信功能
        /// </summary>
        private Socket _clientSocket;

        /// <summary>
        /// 远程主机的IP地址或主机名
        /// 由构造函数传入，用于建立连接时定位远程主机
        /// </summary>
        private readonly string _remoteIPOrHostName;

        /// <summary>
        /// 远程主机的端口号（整数形式）
        /// 由构造函数传入，用于建立连接时定位远程服务
        /// </summary>
        private readonly int _remotePort;

        /// <summary>
        /// 接收缓冲区的最大大小（字节）
        /// 决定了单次接收操作能处理的最大数据量
        /// 由构造函数传入，默认值为8192字节
        /// </summary>
        private readonly int _recvMax;

        /// <summary>
        /// 异步接收循环的任务对象
        /// 用于管理后台接收数据的异步操作
        /// </summary>
        private Task _receiveTask;

        /// <summary>
        /// 取消令牌源，用于终止异步接收循环
        /// 当需要关闭连接时，通过此对象发送取消信号
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// 标记对象是否已被释放
        /// 用于Dispose模式中防止重复释放资源
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// 标记是否正在建立连接
        /// 用于防止并发调用Connect()导致的异常
        /// </summary>
        private bool _isConnecting;
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化TCP客户端（核心构造函数）
        /// 仅完成参数初始化，不自动建立连接（需显式调用Connect/ConnectAsync）
        /// </summary>
        /// <param name="remoteIPOrHostName">远程主机的IP地址（如"192.168.1.1"）或主机名（如"example.com"）</param>
        /// <param name="remotePort">远程主机的端口号（1-65535之间的整数）</param>
        /// <param name="recvMax">接收缓冲区大小（字节），默认8192字节</param>
        /// <exception cref="ArgumentNullException">当remoteIPOrHostName为null时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">当remotePort不在1-65535范围时抛出</exception>
        public TCPClient(string remoteIPOrHostName, int remotePort, int recvMax = 8192)
        {
            // 验证远程地址参数
            _remoteIPOrHostName = remoteIPOrHostName ?? throw new ArgumentNullException(nameof(remoteIPOrHostName));

            // 验证端口号有效性（TCP端口范围为1-65535）
            if (remotePort < 1 || remotePort > 65535)
                throw new ArgumentOutOfRangeException(nameof(remotePort), "端口号必须在1-65535之间");

            // 初始化私有字段
            _remotePort = remotePort;
            _recvMax = recvMax > 0 ? recvMax : 8192; // 确保缓冲区大小为正数
            LocalHostName = Dns.GetHostName(); // 获取本地主机名
        }

        /// <summary>
        /// 兼容字符串端口的构造函数
        /// 用于处理以字符串形式传入的端口号（如配置文件读取场景）
        /// </summary>
        /// <param name="remoteIPOrHostName">远程主机的IP地址或主机名</param>
        /// <param name="port">字符串形式的远程端口号（将被转换为int）</param>
        /// <param name="recvMax">接收缓冲区大小</param>
        public TCPClient(string remoteIPOrHostName, string port, int recvMax = 8192)
            : this(remoteIPOrHostName, int.Parse(port), recvMax) { }
        #endregion

        #region 连接与关闭（同步/异步）
        /// <summary>
        /// 同步建立与远程主机的TCP连接
        /// 阻塞当前线程直到连接成功、失败或超时
        /// </summary>
        /// <returns>连接成功返回true，失败返回false</returns>
        public bool Connect()
        {
            // 防止重复连接：若正在连接或已连接，则直接返回当前状态
            if (_isConnecting || IsConnected) return IsConnected;

            try
            {
                _isConnecting = true; // 标记为正在连接
                CloseInternal(); // 确保之前的连接已关闭（若有）

                // 初始化Socket：IPv4地址族、流式套接字（TCP）、TCP协议
                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendBufferSize = _recvMax,   // 设置发送缓冲区大小
                    ReceiveBufferSize = _recvMax, // 设置接收缓冲区大小
                    SendTimeout = SendTimeout     // 设置发送超时
                };

                // 解析远程主机地址（支持IP地址或主机名）
                IPAddress ipAddress;
                if (!IPAddress.TryParse(_remoteIPOrHostName, out ipAddress))
                {
                    // 若不是IP地址，则视为主机名，解析其对应的IP地址列表
                    var addresses = Dns.GetHostAddresses(_remoteIPOrHostName);
                    // 优先选择IPv4地址（避免IPv6可能的兼容性问题）
                    ipAddress = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                        ?? throw new Exception("未找到有效的IPv4地址");
                    RemoteHostName = _remoteIPOrHostName; // 记录主机名
                }

                // 同步连接（带超时控制）
                // 使用BeginConnect+WaitOne实现超时，避免Socket.Connect的无限阻塞
                var connectResult = _clientSocket.BeginConnect(ipAddress, _remotePort, null, null);
                if (!connectResult.AsyncWaitHandle.WaitOne(ConnectTimeout, false))
                {
                    // 超时处理：关闭Socket并抛出超时异常
                    _clientSocket.Close();
                    throw new TimeoutException($"连接超时（{ConnectTimeout}ms）");
                }

                // 完成连接（处理可能的连接异常）
                _clientSocket.EndConnect(connectResult);

                // 连接成功后的初始化
                if (_clientSocket.Connected)
                {
                    UpdateEndpointInfo(); // 更新本地/远程端点信息
                    StartReceiveLoop();   // 启动后台接收循环
                    ClientConnected?.Invoke(); // 触发连接成功事件
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                // 触发异常事件，由外部处理
                OnExceptionOccurred(ex);
                return false;
            }
            finally
            {
                _isConnecting = false; // 重置连接状态标记
            }
        }

        /// <summary>
        /// 异步建立与远程主机的TCP连接
        /// 不阻塞当前线程，通过Task返回连接结果
        /// 兼容.NET Framework的异步模式（使用Begin/End+Task包装）
        /// </summary>
        /// <returns>连接成功返回true，失败返回false</returns>
        public async Task<bool> ConnectAsync()
        {
            // 防止重复连接
            if (_isConnecting || IsConnected) return IsConnected;

            try
            {
                _isConnecting = true;
                CloseInternal(); // 清理旧连接

                // 初始化Socket（同同步方法）
                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendBufferSize = _recvMax,
                    ReceiveBufferSize = _recvMax,
                    SendTimeout = SendTimeout
                };

                // 解析远程地址（异步版本）
                IPAddress ipAddress;
                if (!IPAddress.TryParse(_remoteIPOrHostName, out ipAddress))
                {
                    // 异步获取主机地址（避免阻塞）
                    var addresses = await Dns.GetHostAddressesAsync(_remoteIPOrHostName).ConfigureAwait(false);
                    ipAddress = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                        ?? throw new Exception("未找到有效的IPv4地址");
                    RemoteHostName = _remoteIPOrHostName;
                }

                // 使用Task包装BeginConnect/EndConnect，将APM模式转换为TAP模式
                var tcs = new TaskCompletionSource<bool>();
                var asyncResult = _clientSocket.BeginConnect(ipAddress, _remotePort, ar =>
                {
                    try
                    {
                        // 完成连接并设置任务结果
                        _clientSocket.EndConnect(ar);
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        // 连接异常时设置任务异常
                        tcs.SetException(ex);
                    }
                }, null);

                // 处理连接超时（使用CancellationTokenSource实现）
                using (var cts = new CancellationTokenSource(ConnectTimeout))
                {
                    // 超时后触发任务异常
                    using (cts.Token.Register(() => tcs.TrySetException(new TimeoutException($"连接超时（{ConnectTimeout}ms）"))))
                    {
                        await tcs.Task.ConfigureAwait(false); // 等待连接完成
                    }
                }

                // 连接成功处理
                if (_clientSocket.Connected)
                {
                    UpdateEndpointInfo();
                    StartReceiveLoop();
                    ClientConnected?.Invoke();
                    return true;
                }
                return false;
            }
            catch (OperationCanceledException)
            {
                // 超时导致的取消操作
                OnExceptionOccurred(new TimeoutException($"连接超时（{ConnectTimeout}ms）"));
                return false;
            }
            catch (Exception ex)
            {
                OnExceptionOccurred(ex);
                return false;
            }
            finally
            {
                _isConnecting = false;
            }
        }

        /// <summary>
        /// 重新连接到远程主机
        /// 内部调用同步连接方法，适用于连接断开后的重试场景
        /// </summary>
        /// <returns>重连成功返回true</returns>
        public bool Reconnect() => Connect();

        /// <summary>
        /// 异步重新连接到远程主机
        /// 内部调用异步连接方法，适用于连接断开后的异步重试场景
        /// </summary>
        /// <returns>重连成功返回true</returns>
        public Task<bool> ReconnectAsync() => ConnectAsync();

        /// <summary>
        /// 更新本地和远程端点的信息（IP、端口等）
        /// 连接成功后调用，从Socket的EndPoint中解析详细信息
        /// </summary>
        private void UpdateEndpointInfo()
        {
            // 安全检查：确保Socket存在且已连接
            if (_clientSocket == null || !_clientSocket.Connected) return;

            // 解析远程端点信息
            var remoteEndpoint = (IPEndPoint)_clientSocket.RemoteEndPoint;
            RemoteIP = remoteEndpoint.Address.ToString();
            RemotePort = remoteEndpoint.Port;
            // 若未设置远程主机名，则通过IP解析
            RemoteHostName ??= Dns.GetHostEntry(remoteEndpoint.Address).HostName;

            // 解析本地端点信息
            var localEndpoint = (IPEndPoint)_clientSocket.LocalEndPoint;
            LocalIP = localEndpoint.Address.ToString();
            LocalPort = localEndpoint.Port;
        }

        /// <summary>
        /// 关闭当前TCP连接（公开方法）
        /// 调用内部关闭实现，对外提供统一的关闭入口
        /// </summary>
        public void Close()
        {
            // 避免重复关闭
            if (!IsConnected && _cts == null) return;
            CloseInternal();
        }

        /// <summary>
        /// 内部关闭连接的实现（核心关闭逻辑）
        /// 负责安全终止接收任务、关闭Socket并触发关闭事件
        /// </summary>
        private void CloseInternal()
        {
            // 终止接收任务：发送取消信号并释放资源
            if (_cts != null)
            {
                _cts.Cancel();   // 发送取消信号给接收循环
                _cts.Dispose();  // 释放取消令牌源
                _cts = null;
            }

            // 等待接收任务结束（最多等待1秒）
            if (_receiveTask != null && !_receiveTask.IsCompleted)
            {
                try { _receiveTask.Wait(1000); }
                catch { /* 忽略等待过程中的异常（如任务已取消） */ }
                _receiveTask = null; // 重置任务引用
            }

            // 关闭并释放Socket
            if (_clientSocket != null)
            {
                try
                {
                    if (_clientSocket.Connected)
                    {
                        // 优雅关闭：先关闭发送和接收通道
                        _clientSocket.Shutdown(SocketShutdown.Both);
                    }
                }
                catch { /* 忽略关闭过程中的异常（如已断开） */ }
                finally
                {
                    _clientSocket.Close(); // 关闭Socket（释放底层资源）
                    _clientSocket = null;  // 重置Socket引用
                }
            }

            // 触发连接关闭事件
            ClientClosed?.Invoke();
        }
        #endregion

        #region 数据发送（确保完整发送）
        /// <summary>
        /// 同步发送字节数组数据
        /// 循环发送直到所有数据被发送（解决单次Send可能发送不完整的问题）
        /// </summary>
        /// <param name="data">要发送的字节数组（不能为空或空数组）</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        /// <exception cref="ArgumentException">当data为null或空数组时抛出</exception>
        public bool SendData(byte[] data)
        {
            // 验证输入数据
            if (data == null || data.Length == 0)
                throw new ArgumentException("发送的数据不能为空", nameof(data));

            // 检查连接状态
            if (!IsConnected)
            {
                OnExceptionOccurred(new InvalidOperationException("未建立连接"));
                return false;
            }

            try
            {
                int totalSent = 0;
                // 循环发送：TCP可能因缓冲区限制导致单次发送不完整
                while (totalSent < data.Length)
                {
                    // 发送剩余数据（从totalSent开始，长度为剩余字节数）
                    int sent = _clientSocket.Send(
                        data,
                        totalSent,
                        data.Length - totalSent,
                        SocketFlags.None
                    );

                    // 发送字节数为0表示连接已断开
                    if (sent == 0)
                        throw new IOException("发送失败，远程主机已关闭连接");

                    totalSent += sent; // 更新已发送字节数
                }
                return true; // 全部数据发送完成
            }
            catch (Exception ex)
            {
                OnExceptionOccurred(ex);
                CloseInternal(); // 发送失败时主动关闭连接
                return false;
            }
        }

        /// <summary>
        /// 同步发送字符串数据
        /// 内部将字符串通过Encoding编码为字节数组，再调用SendData(byte[])
        /// </summary>
        /// <param name="data">要发送的字符串（不能为空或空字符串）</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        /// <exception cref="ArgumentException">当data为null或空字符串时抛出</exception>
        public bool SendData(string data)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("发送的字符串不能为空", nameof(data));

            // 编码为字节数组并发送
            return SendData(Encoding.GetBytes(data));
        }

        /// <summary>
        /// 异步发送字节数组数据
        /// 兼容.NET Framework的异步模式，确保所有数据发送完成
        /// </summary>
        /// <param name="data">要发送的字节数组</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        /// <exception cref="ArgumentException">当data为null或空数组时抛出</exception>
        public async Task<bool> SendDataAsync(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("发送的数据不能为空", nameof(data));

            if (!IsConnected)
            {
                OnExceptionOccurred(new InvalidOperationException("未建立连接"));
                return false;
            }

            try
            {
                int totalSent = 0;
                // 循环发送（异步版本）
                while (totalSent < data.Length)
                {
                    // 使用Task包装BeginSend/EndSend，转换为异步操作
                    var tcs = new TaskCompletionSource<int>();
                    _clientSocket.BeginSend(
                        data,
                        totalSent,
                        data.Length - totalSent,
                        SocketFlags.None,
                        ar =>
                        {
                            try
                            {
                                // 完成发送并获取发送的字节数
                                int sent = _clientSocket.EndSend(ar);
                                tcs.SetResult(sent);
                            }
                            catch (Exception ex)
                            {
                                tcs.SetException(ex);
                            }
                        },
                        null
                    );

                    // 等待单次发送完成
                    int sentBytes = await tcs.Task.ConfigureAwait(false);
                    if (sentBytes == 0)
                        throw new IOException("发送失败，远程主机已关闭连接");

                    totalSent += sentBytes;
                }
                return true;
            }
            catch (Exception ex)
            {
                OnExceptionOccurred(ex);
                CloseInternal();
                return false;
            }
        }

        /// <summary>
        /// 异步发送字符串数据
        /// 内部将字符串编码为字节数组，再调用SendDataAsync(byte[])
        /// </summary>
        /// <param name="data">要发送的字符串</param>
        /// <returns>发送成功返回true，失败返回false</returns>
        /// <exception cref="ArgumentException">当data为null或空字符串时抛出</exception>
        public Task<bool> SendDataAsync(string data)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("发送的字符串不能为空", nameof(data));

            return SendDataAsync(Encoding.GetBytes(data));
        }
        #endregion

        #region 数据接收（异步非阻塞）
        /// <summary>
        /// 启动异步接收循环
        /// 在后台线程中运行接收逻辑，通过取消令牌控制生命周期
        /// </summary>
        private void StartReceiveLoop()
        {
            _cts = new CancellationTokenSource(); // 初始化取消令牌源
            var token = _cts.Token;
            // 在后台线程中启动接收循环（使用Task.Run确保线程池线程）
            _receiveTask = Task.Run(async () => await ReceiveLoopAsync(token), token);
        }

        /// <summary>
        /// 异步接收循环（核心接收逻辑）
        /// 持续接收远程主机发送的数据，直到连接关闭或被取消
        /// 兼容.NET Framework：使用SocketAsyncEventArgs实现异步操作
        /// </summary>
        /// <param name="cancellationToken">取消令牌，用于终止接收循环</param>
        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            // 初始化接收缓冲区（大小为_recvMax）
            var buffer = new byte[_recvMax];
            // 创建SocketAsyncEventArgs（用于高效异步Socket操作）
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, 0, buffer.Length); // 绑定缓冲区

            // 定义事件处理委托（处理异步接收完成事件）
            EventHandler<SocketAsyncEventArgs> completedHandler = null;
            completedHandler = (s, e) =>
            {
                // 从UserToken中获取任务完成源（用于将事件转换为Task）
                var tcs = e.UserToken as TaskCompletionSource<SocketAsyncEventArgs>;
                if (tcs != null)
                {
                    if (e.SocketError != SocketError.Success)
                    {
                        // 接收出错：设置任务异常
                        tcs.TrySetException(new SocketException((int)e.SocketError));
                    }
                    else
                    {
                        // 接收成功：设置任务结果
                        tcs.TrySetResult(e);
                    }
                }
                e.UserToken = null; // 清理用户令牌（避免内存泄漏）
            };

            // 绑定事件处理程序
            args.Completed += completedHandler;

            // 接收循环：持续接收直到取消或连接断开
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                try
                {
                    // 重置事件参数（仅重置错误状态，BytesTransferred为只读不可重置）
                    args.SocketError = SocketError.Success;

                    // 创建任务完成源（用于等待异步操作）
                    var tcs = new TaskCompletionSource<SocketAsyncEventArgs>();
                    args.UserToken = tcs; // 将任务完成源关联到事件参数

                    // 注册取消回调：当令牌取消时，标记任务为取消状态
                    using (var registration = cancellationToken.Register(() =>
                    {
                        tcs.TrySetCanceled();
                    }))
                    {
                        // 发起异步接收操作
                        bool isPending = _clientSocket.ReceiveAsync(args);
                        if (!isPending)
                        {
                            // 操作同步完成（数据已在缓冲区），直接处理结果
                            ProcessReceiveResult(args);
                        }
                        else
                        {
                            // 操作异步完成，等待任务完成后处理结果
                            var resultArgs = await tcs.Task.ConfigureAwait(false);
                            ProcessReceiveResult(resultArgs);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // 正常取消（如调用Close()），退出循环
                    break;
                }
                catch (SocketException ex) when (ex.ErrorCode == 10054)
                {
                    // 错误码10054：远程主机强制关闭连接
                    OnExceptionOccurred(new IOException("远程主机强制关闭连接", ex));
                    CloseInternal(); // 关闭连接
                    break;
                }
                catch (Exception ex)
                {
                    // 其他异常：触发事件并等待100ms后重试（避免频繁报错）
                    OnExceptionOccurred(ex);
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }

            // 清理资源：移除事件绑定并释放SocketAsyncEventArgs
            args.Completed -= completedHandler;
            args.Dispose();
        }

        /// <summary>
        /// 处理接收操作的结果
        /// 解析接收到的数据，更新接收字符串，触发数据到达事件
        /// </summary>
        /// <param name="args">包含接收结果的SocketAsyncEventArgs</param>
        /// <exception cref="SocketException">当接收操作出错时抛出</exception>
        /// <exception cref="IOException">当远程主机关闭连接时抛出</exception>
        private void ProcessReceiveResult(SocketAsyncEventArgs args)
        {
            // 检查接收操作是否成功
            if (args.SocketError != SocketError.Success)
            {
                throw new SocketException((int)args.SocketError);
            }

            int bytesRead = args.BytesTransferred; // 获取接收的字节数
            if (bytesRead > 0)
            {
                // 复制有效数据（避免缓冲区被下一次接收覆盖）
                var receivedData = new byte[bytesRead];
                Array.Copy(args.Buffer, args.Offset, receivedData, 0, bytesRead);

                // 更新最后接收的字符串（使用配置的编码）
                ReceiveString = Encoding.GetString(receivedData);

                // 触发数据到达事件（将数据传递给外部处理）
                DataArrived?.Invoke(receivedData, bytesRead);
            }
            else if (bytesRead == 0)
            {
                // 接收字节数为0表示远程主机正常关闭连接
                throw new IOException("远程主机已关闭连接");
            }
        }
        #endregion

        #region 事件触发与异常处理
        /// <summary>
        /// 触发异常事件（线程安全实现）
        /// 避免多线程环境下事件订阅者变更导致的异常
        /// </summary>
        /// <param name="ex">要传递的异常对象</param>
        private void OnExceptionOccurred(Exception ex)
        {
            // 使用临时变量存储事件引用：防止在触发前事件被取消订阅
            var handler = ExceptionOccurred;
            handler?.Invoke(ex); // 安全触发事件（若有订阅者）
        }
        #endregion

        #region 资源释放（标准Dispose模式）
        /// <summary>
        /// 释放当前对象占用的资源（公开方法）
        /// 遵循IDisposable接口规范，确保资源被正确释放
        /// </summary>
        public void Dispose()
        {
            Dispose(true); // 手动释放：释放托管和非托管资源
            GC.SuppressFinalize(this); // 通知GC不需要调用析构函数
        }

        /// <summary>
        /// 资源释放的核心实现
        /// </summary>
        /// <param name="disposing">
        /// true：手动调用Dispose()，需释放托管资源
        /// false：GC调用析构函数，仅释放非托管资源
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return; // 避免重复释放

            if (disposing)
            {
                // 释放托管资源：关闭连接（包含Socket、Task等托管对象）
                CloseInternal();
            }

            // 标记为已释放
            _isDisposed = true;
        }

        /// <summary>
        /// 析构函数（Finalizer）
        /// 由GC自动调用，用于释放非托管资源
        /// </summary>
        ~TCPClient()
        {
            Dispose(false); // 仅释放非托管资源
        }
        #endregion
    }
}
 