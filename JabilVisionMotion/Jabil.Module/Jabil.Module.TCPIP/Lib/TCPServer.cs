using Jabil.Module.Log.Lib;
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
    /// TCP服务器类，支持多客户端连接、数据收发及相关事件通知
    /// </summary>
    public class TCPServer : IDisposable
    {
        #region 事件定义
        /// <summary>
        /// 客户端连接成功事件
        /// </summary>
        public event ClientConnectedEventHandler ClientConnected;
        /// <summary>
        /// 客户端连接关闭事件
        /// </summary>
        public event ClientCloseEventHandler ClientClose;
        /// <summary>
        /// 数据到达事件
        /// </summary>
        public event DataArrivedEventHandler DataArrived;
        /// <summary>
        /// 异常发生事件
        /// </summary>
        public event ExceptionEventHandler ExceptionOccurred;

        /// <summary>
        /// 客户端连接事件委托
        /// </summary>
        /// <param name="ip">客户端IP地址</param>
        /// <param name="port">客户端端口</param>
        public delegate void ClientConnectedEventHandler(string ip, string port);
        /// <summary>
        /// 客户端断开事件委托
        /// </summary>
        /// <param name="ip">客户端IP地址</param>
        /// <param name="port">客户端端口</param>
        public delegate void ClientCloseEventHandler(string ip, string port);
        /// <summary>
        /// 数据到达事件委托
        /// </summary>
        /// <param name="data">接收的数据</param>
        /// <param name="length">数据长度</param>
        /// <param name="ipAddress">客户端IP</param>
        /// <param name="port">客户端端口</param>
        public delegate void DataArrivedEventHandler(byte[] data, int length, string ipAddress, string port);
        /// <summary>
        /// 异常事件委托
        /// </summary>
        /// <param name="ex">发生的异常</param>
        public delegate void ExceptionEventHandler(Exception ex);
        #endregion

        #region 属性
        /// <summary>
        /// 服务器名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 最后接收的字符串
        /// </summary>
        public string ReceiveString { get;  set; }

        /// <summary>
        /// 获取服务器是否已启动
        /// </summary>
        public bool IsServerStarted => _isServerRunning;

        /// <summary>
        /// 当前连接的客户端数量
        /// </summary>
        public int ClientCount => _clients.Count;

        /// <summary>
        /// 监听的IP地址
        /// </summary>
        public string ListeningIP => "0.0.0.0"; // 绑定到所有网络接口

        /// <summary>
        /// 监听的端口
        /// </summary>
        public string ListeningPort => _listenPort;
        /// <summary>
        /// _clients
        /// </summary>
        public readonly SortedDictionary<string, Socket> _clients = new SortedDictionary<string, Socket>();

        #endregion


        #region 私有字段
        private readonly string _listenPort;
        private readonly int _maxClient;
        private readonly int _recvMax;
        private Socket _listenSocket;
        private Thread _listenThread;
        private bool _isServerRunning;
        private bool _isDisposed;
        private readonly object _clientLock = new object();
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化TCP服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port">监听端口</param>
        /// <param name="maxClient">最大客户端数量</param>
        /// <param name="recvMax">接收缓冲区大小</param>
        /// <param name="recvSleep">接收休眠时间（未使用，保留参数兼容）</param>
        public TCPServer(string ip,string port, int maxClient, int recvMax, int recvSleep)
        {
            // 参数验证
            if (string.IsNullOrEmpty(port))
                throw new ArgumentNullException(nameof(port), "端口号不能为空");

            if (!int.TryParse(port, out int portNumber) || portNumber < 1 || portNumber > 65535)
                throw new ArgumentException("无效的端口号，必须在1-65535范围内", nameof(port));

            if (maxClient <= 0)
                throw new ArgumentException("最大客户端数量必须大于0", nameof(maxClient));

            if (recvMax <= 0)
                throw new ArgumentException("接收缓冲区大小必须大于0", nameof(recvMax));
            
            _listenPort = port;
            _maxClient = maxClient;
            _recvMax = recvMax;

            try
            {
                // 初始化监听Socket
                var localEndPoint = new IPEndPoint(IPAddress.Parse(ip)??IPAddress.Any, portNumber);
                _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listenSocket.Bind(localEndPoint);
                _listenSocket.Listen(_maxClient);
                _listenSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.AcceptConnection, true);
            }
            catch (Exception ex)
            {
                OnExceptionOccurred(ex);
                Dispose();
                throw;
            }
        }
        #endregion

        #region 服务器控制
        /// <summary>
        /// 启动服务器监听
        /// </summary>
        public void StartServer()
        {
            if (_isServerRunning) return;

            _isServerRunning = true;
            _listenThread = new Thread(ListenForClients)
            {
                Name = "TCP Server Listener",
                IsBackground = true
            };
            _listenThread.Start();
        }

        /// <summary>
        /// 停止服务器并关闭所有连接
        /// </summary>
        public void StopServer()
        {
            if (!_isServerRunning) return;

            _isServerRunning = false;

            // 关闭监听线程
            if (_listenThread != null && _listenThread.IsAlive)
            {
                if (!_listenThread.Join(1000))
                {
                    try { _listenThread.Abort(); }
                    catch { /* 忽略终止异常 */ }
                }
                _listenThread = null;
            }

            // 关闭所有客户端连接
            CloseAllClients();

            // 关闭监听Socket
            if (_listenSocket != null)
            {
                try { _listenSocket.Close(); }
                catch { /* 忽略关闭异常 */ }
                _listenSocket = null;
            }
        }

        /// <summary>
        /// 监听客户端连接的循环
        /// </summary>
        private void ListenForClients()
        {
            while (_isServerRunning)
            {
                try
                {
                    if (_listenSocket == null) break;

                    // 异步接受连接（避免阻塞）
                    var clientSocket = _listenSocket.Accept();
                    if (clientSocket != null)
                    {
                        HandleNewClient(clientSocket);
                    }
                }
                catch (SocketException ex)
                {
                    // 10004表示被中断的调用，通常是服务器正在关闭
                    if (ex.ErrorCode != 10004 || _isServerRunning)
                    {
                        OnExceptionOccurred(ex);
                    }
                }
                catch (Exception ex)
                {
                    if (_isServerRunning)
                    {
                        OnExceptionOccurred(ex);
                    }
                }

                // 控制循环频率，避免CPU占用过高
                if (_isServerRunning)
                    Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 处理新连接的客户端
        /// </summary>
        private void HandleNewClient(Socket clientSocket)
        {
            try
            {
                var remoteEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                string clientKey = $"{remoteEndPoint.Address}:{remoteEndPoint.Port}";
                string clientIp = remoteEndPoint.Address.ToString();
                string clientPort = remoteEndPoint.Port.ToString();

                // 检查是否已达到最大连接数
                lock (_clientLock)
                {
                    if (_clients.Count >= _maxClient)
                    {
                        clientSocket.Close();
                        throw new InvalidOperationException("已达到最大客户端连接数");
                    }

                    _clients.Add(clientKey, clientSocket);
                }

                // 触发客户端连接事件
                ClientConnected?.Invoke(clientIp, clientPort);

                // 启动客户端通信线程
                var clientComm = new ClientCommunication(clientSocket, this, clientIp, clientPort, _recvMax);
                clientComm.ExceptionOccurred += OnClientException;
                clientComm.ClientDisconnected += OnClientDisconnected;
                clientComm.DataReceived += OnClientDataReceived;

                var clientThread = new Thread(clientComm.StartCommunication)
                {
                    Name = $"Client Handler - {clientKey}",
                    IsBackground = true
                };
                clientThread.Start();
            }
            catch (Exception ex)
            {
                clientSocket?.Close();
                OnExceptionOccurred(ex);
            }
        }
        #endregion

        #region 客户端管理
        /// <summary>
        /// 关闭指定客户端连接
        /// </summary>
        /// <param name="ip">客户端IP</param>
        /// <param name="port">客户端端口</param>
        public void CloseClient(string ip, string port)
        {
            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
                throw new ArgumentException("IP地址和端口不能为空");

            string clientKey = $"{ip}:{port}";
            CloseClientByKey(clientKey);
        }

        /// <summary>
        /// 通过客户端标识关闭连接
        /// </summary>
        private void CloseClientByKey(string clientKey)
        {
            lock (_clientLock)
            {
                if (_clients.TryGetValue(clientKey, out Socket clientSocket))
                {
                    try
                    {
                        clientSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch { }
                    finally
                    {
                        clientSocket.Close();
                        _clients.Remove(clientKey);
                    }

                    // 解析IP和端口并触发事件
                    var parts = clientKey.Split(':');
                    if (parts.Length == 2)
                    {
                        ClientClose?.Invoke(parts[0], parts[1]);
                    }
                }
            }
        }

        /// <summary>
        /// 关闭所有客户端连接
        /// </summary>
        public void CloseAllClients()
        {
            lock (_clientLock)
            {
                var clientKeys = _clients.Keys.ToList();
                foreach (var key in clientKeys)
                {
                    CloseClientByKey(key);
                }
            }
        }

        /// <summary>
        /// 获取客户端Socket
        /// </summary>
        private Socket GetClientSocket(string ip, string port)
        {
            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
                return null;

            string clientKey = $"{ip}:{port}";
            lock (_clientLock)
            {
                _clients.TryGetValue(clientKey, out Socket clientSocket);
                return clientSocket;
            }
        }
        #endregion

        #region 数据发送
        /// <summary>
        /// 向指定客户端发送字节数据
        /// </summary>
        /// <param name="data">要发送的字节数组</param>
        /// <param name="ipAddress">客户端IP</param>
        /// <param name="port">客户端端口</param>
        /// <returns>发送成功返回true</returns>
        public bool SendData(byte[] data, string ipAddress, string port)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("发送的数据不能为空", nameof(data));

            var clientSocket = GetClientSocket(ipAddress, port);
            if (clientSocket == null || !clientSocket.Connected)
            {
                OnExceptionOccurred(new InvalidOperationException("客户端连接不存在或已关闭"));
                return false;
            }

            try
            {
                clientSocket.Send(data, data.Length, SocketFlags.None);
                return true;
            }
            catch (Exception ex)
            {
                OnExceptionOccurred(ex);
                CloseClient(ipAddress, port); // 发送失败时关闭连接
                return false;
            }
        }

        /// <summary>
        /// 向最后一个连接的客户端发送字节数据
        /// </summary>
        /// <param name="data">要发送的字节数组</param>
        /// <returns>发送成功返回true</returns>
        public bool SendData(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("发送的数据不能为空", nameof(data));

            lock (_clientLock)
            {
                if (_clients.Count == 0)
                    return false;

                // 获取最后一个连接的客户端
                var lastClient = _clients.Last();
                return SendData(data, lastClient.Key.Split(':')[0], lastClient.Key.Split(':')[1]);
            }
        }

        /// <summary>
        /// 向最后一个连接的客户端发送字符串数据(ASCII编码)
        /// </summary>
        /// <param name="data">要发送的字符串</param>
        /// <returns>发送成功返回true</returns>
        public bool SendData(string data)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("发送的字符串不能为空", nameof(data));

            return SendData(Encoding.ASCII.GetBytes(data));
        }

        /// <summary>
        /// 向指定客户端发送文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="ipAddress">客户端IP</param>
        /// <param name="port">客户端端口</param>
        /// <returns>发送成功返回true</returns>
        public bool SendFile(string filePath, string ipAddress, string port)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);

            var clientSocket = GetClientSocket(ipAddress, port);
            if (clientSocket == null || !clientSocket.Connected)
            {
                OnExceptionOccurred(new InvalidOperationException("客户端连接不存在或已关闭"));
                return false;
            }

            try
            {
                clientSocket.SendFile(filePath);
                return true;
            }
            catch (Exception ex)
            {
                OnExceptionOccurred(ex);
                CloseClient(ipAddress, port); // 发送失败时关闭连接
                return false;
            }
        }

        /// <summary>
        /// 向所有客户端广播字节数据
        /// </summary>
        /// <param name="data">要广播的字节数组</param>
        /// <returns>全部发送成功返回true</returns>
        public bool BroadcastData(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("广播的数据不能为空", nameof(data));

            bool allSuccess = true;
            List<string> disconnectedClients = new List<string>();

            lock (_clientLock)
            {
                foreach (var client in _clients)
                {
                    try
                    {
                        if (client.Value.Connected)
                        {
                            client.Value.Send(data, data.Length, SocketFlags.None);
                        }
                        else
                        {
                            disconnectedClients.Add(client.Key);
                            allSuccess = false;
                        }
                    }
                    catch
                    {
                        disconnectedClients.Add(client.Key);
                        allSuccess = false;
                    }
                }

                // 移除已断开的客户端
                foreach (var key in disconnectedClients)
                {
                    CloseClientByKey(key);
                }
            }

            return allSuccess;
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 处理客户端接收的数据
        /// </summary>
        private void OnClientDataReceived(byte[] data, int length, string ip, string port)
        {
            try
            {
                ReceiveString = Encoding.ASCII.GetString(data, 0, length);
                DataArrived?.Invoke(data, length, ip, port);
            }
            catch (Exception ex)
            {
                OnExceptionOccurred(ex);
            }
        }

        /// <summary>
        /// 处理客户端断开连接
        /// </summary>
        private void OnClientDisconnected(string ip, string port)
        {
            CloseClient(ip, port);
        }

        /// <summary>
        /// 处理客户端通信异常
        /// </summary>
        private void OnClientException(Exception ex)
        {
            OnExceptionOccurred(ex);
        }

        /// <summary>
        /// 触发异常事件
        /// </summary>
        private void OnExceptionOccurred(Exception ex)
        {
            // 使用临时变量避免多线程竞态条件
            var handler = ExceptionOccurred;
            handler?.Invoke(ex);
        }
        #endregion

        #region 资源释放
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 资源释放实现
        /// </summary>
        /// <param name="disposing">是否手动释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                // 释放托管资源
                StopServer();
            }

            _isDisposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~TCPServer()
        {
            Dispose(false);
        }
        #endregion

        #region 客户端通信内部类
        /// <summary>
        /// 客户端通信管理类，处理单个客户端的数据流
        /// </summary>
        private class ClientCommunication
        {
            #region 事件
            /// <summary>
            /// 数据接收事件
            /// </summary>
            internal event Action<byte[], int, string, string> DataReceived;
            /// <summary>
            /// 客户端断开事件
            /// </summary>
            internal event Action<string, string> ClientDisconnected;
            /// <summary>
            /// 异常发生事件
            /// </summary>
            internal event Action<Exception> ExceptionOccurred;
            #endregion

            private readonly Socket _clientSocket;
            private readonly TCPServer _parentServer;
            private readonly string _clientIp;
            private readonly string _clientPort;
            private readonly int _bufferSize;
            private bool _isRunning;

            /// <summary>
            /// 初始化客户端通信管理器
            /// </summary>
            internal ClientCommunication(Socket clientSocket, TCPServer parentServer,
                                        string clientIp, string clientPort, int bufferSize)
            {
                _clientSocket = clientSocket ?? throw new ArgumentNullException(nameof(clientSocket));
                _parentServer = parentServer ?? throw new ArgumentNullException(nameof(parentServer));
                _clientIp = clientIp ?? throw new ArgumentNullException(nameof(clientIp));
                _clientPort = clientPort ?? throw new ArgumentNullException(nameof(clientPort));
                _bufferSize = bufferSize > 0 ? bufferSize : 8192;
                _isRunning = true;
            }

            /// <summary>
            /// 开始客户端通信循环
            /// </summary>
            internal void StartCommunication()
            {
                var buffer = new byte[_bufferSize];

                while (_isRunning && _clientSocket.Connected)
                {
                    try
                    {
                        // 检查是否有数据可读
                        if (_clientSocket.Poll(100, SelectMode.SelectRead))
                        {
                            int bytesRead = _clientSocket.Receive(buffer);
                            if (bytesRead > 0)
                            {
                                // 复制有效数据
                                var receivedData = new byte[bytesRead];
                                Array.Copy(buffer, receivedData, bytesRead);

                                // 触发数据接收事件
                                DataReceived?.Invoke(receivedData, bytesRead, _clientIp, _clientPort);
                            }
                            else
                            {
                                // 对方关闭连接
                                break;
                            }
                        }
                        else
                        {
                            // 短暂休眠降低CPU占用
                            Thread.Sleep(10);
                        }
                    }
                    catch (SocketException ex)
                    {
                        // 10054表示远程主机强制关闭连接
                        if (ex.ErrorCode == 10054)
                        {
                            break;
                        }
                        ExceptionOccurred?.Invoke(ex);
                        Thread.Sleep(100);
                    }
                    catch (Exception ex)
                    {
                        ExceptionOccurred?.Invoke(ex);
                        Thread.Sleep(100);
                    }
                }

                // 通信结束，触发断开事件
                _isRunning = false;
                ClientDisconnected?.Invoke(_clientIp, _clientPort);
            }
        }
        #endregion
    }
}
