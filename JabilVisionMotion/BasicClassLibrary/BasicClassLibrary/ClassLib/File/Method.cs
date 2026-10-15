using BasicClassLibrary.RexHelps;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace BasicClassLibrary
{
    public class IniReadWrite
    {
        #region 外部接口
        public string FilePath;
        public void IniFile(string INIPath)
        {
            FilePath = INIPath;
        }
        #endregion
        #region 调用Windows API
        /// 
        /// 写入INI文件
        /// 
        ///节点名称[如[TypeName]] 
        ///键 
        ///值 
        ///文件路径 
        /// 
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filepath);
        /// 
        /// 读取INI文件
        /// 
        ///节点名称 
        ///键 
        ///值 
        ///stringbulider对象 
        ///字节大小 
        ///文件路径 
        /// 
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string defVal, byte[] retVal, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);

        #endregion
        public void IniWriteValue(string section, string key, string value)
        {
            IniReadWrite.WritePrivateProfileString(section, key, value, this.FilePath);
        }
        public void IniWriteConfig(string key, string value)
        {
            IniReadWrite.WritePrivateProfileString("Config", key, value, this.FilePath);
        }
        public string IniReadValue(string section, string key)
        {
            StringBuilder stringBuilder = new StringBuilder(2048);
            int privateProfileString = IniReadWrite.GetPrivateProfileString(section, key, "", stringBuilder, 2014, this.FilePath);
            return stringBuilder.ToString();
        }
        public string IniReadConfig(string key)
        {
            StringBuilder stringBuilder = new StringBuilder(2048);
            int privateProfileString = IniReadWrite.GetPrivateProfileString("Config", key, "", stringBuilder, 2014, FilePath);
            return stringBuilder.ToString();
        }
        public void IniDeleteKey(string section, string key)
        {
            if (section.Trim().Length > 0 && key.Trim().Length > 0)
            {
                IniReadWrite.WritePrivateProfileString(section, key, null, this.FilePath);
            }
        }
        public void IniDeleteSection(string section)
        {
            IniReadWrite.WritePrivateProfileString(section, null, null, this.FilePath);
        }
        public string[] IniReadAllKeys(string section)
        {
            byte[] bytes = this.ReadAllKeys(section, null);
            string @string = Encoding.Default.GetString(bytes);
            string Text = @string;
            char[] separator = new char[1];
            return Text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }
        public byte[] ReadAllKeys(string section, string key)
        {
            byte[] array = new byte[2048];
            int privateProfileString = IniReadWrite.GetPrivateProfileString(section, key, "", array, 2048, this.FilePath);
            return array;
        }
    }
    public class TxtReadWrite
    {
        #region 外部接口
        public string sFileName { get; set; }
        public string sContent { get; set; }
        #endregion
        /// <summary>
        /// 保存txt文件
        /// </summary>
        /// <param name="sFileMode">保存模式</param>
        public void SaveTxt(FileMode sFileMode)
        {
            try
            {
                if (!System.IO.File.Exists(sFileName))
                {
                    FileStream fs;
                    fs = File.Create(sFileName);
                    fs.Close();
                }
                FileStream fsTxtWrite = new FileStream(sFileName, sFileMode, FileAccess.Write);
                StreamWriter srWrite = new StreamWriter(fsTxtWrite, System.Text.Encoding.UTF8);
                //StreamReader rd = new StreamReader(fsTxtWrite);

                //string a = rd.ReadToEnd();
                srWrite.Write(sContent);

                //srWrite.WriteLine(sContent);

                srWrite.Close();
                srWrite.Dispose();
                fsTxtWrite.Dispose();
                //=====================================================
                //string alllog = "";
                //alllog = rd.ReadToEnd();
                //=====================================================

            }
            catch (Exception ex)
            {
                #region ### error info
                //try
                //{
                //    string sFolder = Directory.GetParent(sFileName).ToString();
                //    string sErrFullName = sFolder + "_err_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
                //    if (!System.IO.File.Exists(sErrFullName))
                //    {
                //        FileStream fs;
                //        fs = File.Create(sErrFullName);

                //        StreamWriter srWrite = new StreamWriter(fs, System.Text.Encoding.UTF8);

                //        srWrite.Write(sFileName);

                //        srWrite.Close();
                //        srWrite.Dispose();

                //        fs.Close();
                //    }
                //}
                //catch (Exception)
                //{ }
                #endregion ###error
            }
        }
        /// <summary>
        /// 读取txt文件
        /// </summary>
        /// <returns>文件内容</returns>
        public string ReadTxt()
        {
            if (!System.IO.File.Exists(sFileName))
            {
                return "File Not Exist!";
            }
            else
            {
                return File.ReadAllText(sFileName);
            }
        }
    }
    public class TcpIpReadWrite
    {
        private class TCPServer
        {
            #region 外部接口
            public string ServerIP { get; set; }
            public Int32 port { get; set; }
            public List<User> ClientList
            {
                get
                {
                    return _ClientList;
                }
            }
            public string receiveString
            {
                get { return _Rcvmsg; }
                private set
                {
                    _Rcvmsg = value;
                    OnMsgReceived(new EventArgs());
                }
            }
            public string StatusMessage
            {
                get { return _StatusMessage; }
                private set
                {
                    _StatusMessage = value;
                    OnStatusChanged(new EventArgs());
                }
            }

            #endregion
            #region 内部变量
            private TcpListener myListener;
            private bool isNormalExit = false;
            private string _Rcvmsg = "";
            private string _StatusMessage;
            private List<User> _ClientList = new List<User>();
            #endregion
            #region 事件
            public event EventHandler MsgReceived;
            public event EventHandler StatusChanged;
            #endregion
            public void StartListen()
            {
                myListener = new TcpListener(IPAddress.Parse(ServerIP), port);
                myListener.Start();
                StatusMessage = string.Format("开始在{0}:{1}监听客户连接", ServerIP, port);
                //创建一个线程监客户端连接请求
                Thread myThread = new Thread(_Listen);
                myThread.Start();
            }
            private void _Listen()
            {
                TcpClient newClient = null;
                while (true)
                {
                    try
                    {
                        newClient = myListener.AcceptTcpClient();
                    }
                    catch
                    {
                        //当单击‘停止监听’或者退出此窗体时 AcceptTcpClient() 会产生异常
                        //因此可以利用此异常退出循环
                        break;
                    }
                    //每接收一个客户端连接，就创建一个对应的线程循环接收该客户端发来的信息；
                    User user = new User(newClient);
                    Thread threadReceive = new Thread(ReceiveData);
                    threadReceive.Start(user);
                    _ClientList.Add(user);
                    StatusMessage = string.Format(string.Format("[{0}]进入", newClient.Client.RemoteEndPoint)) + Environment.NewLine;
                    StatusMessage = string.Format("当前连接用户数：{0}", _ClientList.Count) + Environment.NewLine;
                }
            }
            /// <summary>
            /// 处理接收的客户端信息
            /// </summary>
            /// <param name="userState">客户端信息</param>
            private void ReceiveData(object userState)
            {
                User user = (User)userState;
                TcpClient client = user.client;
                while (isNormalExit == false)
                {
                    receiveString = null;
                    try
                    {
                        //从网络流中读出字符串，此方法会自动判断字符串长度前缀
                        receiveString = user.br.ReadString();

                    }
                    catch (Exception)
                    {
                        if (isNormalExit == false)
                        {
                            StatusMessage = string.Format("与[{0}]失去联系，已终止接收该用户信息", client.Client.RemoteEndPoint) + Environment.NewLine;
                            RemoveUser(user);
                        }
                        break;
                    }
                    StatusMessage = string.Format("来自[{0}]：{1}", user.client.Client.RemoteEndPoint, receiveString) + Environment.NewLine;
                    #region 命令处理
                    //string[] splitString = receiveString.Split(',');
                    //switch (splitString[0])
                    //{
                    //    case "Login":
                    //        user.userName = splitString[1];
                    //        SendToAllClient(user, receiveString);
                    //        break;
                    //    case "Logout":
                    //        SendToAllClient(user, receiveString);
                    //        RemoveUser(user);
                    //        return;
                    //    case "Talk":
                    //        string talkString = receiveString.Substring(splitString[0].Length + splitString[1].Length + 2);
                    //        StatusMessage = string.Format("{0}对{1}说：{2}", user.userName, splitString[1], talkString) + Environment.NewLine;
                    //        SendToClient(user, "talk," + user.userName + "," + talkString);
                    //        foreach (User target in _ClientList)
                    //        {
                    //            if (target.userName == splitString[1] && user.userName != splitString[1])
                    //            {
                    //                SendToClient(target, "talk," + user.userName + "," + talkString);
                    //                break;
                    //            }
                    //        }
                    //        break;
                    //    default:
                    //        StatusMessage = string.Format("未知指令：" + receiveString) + Environment.NewLine;
                    //        break;
                    //}
                    #endregion
                }
            }
            /// <summary>
            /// 移除用户
            /// </summary>
            /// <param name="user">指定要移除的用户</param>
            private void RemoveUser(User user)
            {
                _ClientList.Remove(user);
                user.Close();
                //AddItemToListBox(string.Format("当前连接用户数：{0}", userList.Count));
            }
            /// <summary>
            /// 发送消息给所有客户
            /// </summary>
            /// <param name="user">指定发给哪个用户</param>
            /// <param name="message">信息内容</param>
            public void SendToAllClient(User user, string message)
            {
                string command = message.Split(',')[0].ToLower();
                if (command == "login")
                {
                    //获取所有客户端在线信息到当前登录用户
                    for (int i = 0; i < _ClientList.Count; i++)
                    {
                        SendToClient(user, "login," + _ClientList[i].userName);
                    }
                    //把自己上线，发送给所有客户端
                    for (int i = 0; i < _ClientList.Count; i++)
                    {
                        if (user.userName != _ClientList[i].userName)
                        {
                            SendToClient(_ClientList[i], "login," + user.userName);
                        }
                    }
                }
                else if (command == "logout")
                {
                    for (int i = 0; i < _ClientList.Count; i++)
                    {
                        if (_ClientList[i].userName != user.userName)
                        {
                            SendToClient(_ClientList[i], message);
                        }
                    }
                }
            }
            /// <summary>
            /// 发送 message 给 user
            /// </summary>
            /// <param name="user">指定发给哪个用户</param>
            /// <param name="message">信息内容</param>
            public void SendToClient(User user, string message)
            {
                try
                {
                    //将字符串写入网络流，此方法会自动附加字符串长度前缀
                    user.bw.Write(message);
                    user.bw.Flush();
                    //AddItemToListBox(string.Format("向[{0}]发送：{1}", user.userName, message));
                }
                catch
                {
                    //AddItemToListBox(string.Format("向[{0}]发送信息失败", user.userName));
                }
            }

            private void OnMsgReceived(EventArgs eventArgs)
            {
                if (MsgReceived != null)
                {
                    MsgReceived(this, eventArgs);
                }
            }
            private void OnStatusChanged(EventArgs eventArgs)
            {
                if (StatusChanged != null)
                {
                    StatusChanged(this, eventArgs);
                }
            }

            public void CloseServer()
            {
                StatusMessage = "开始停止服务，并依次使用户退出！";
                isNormalExit = true;
                for (int i = ClientList.Count - 1; i >= 0; i--)
                {
                    RemoveUser(ClientList[i]);
                }
                //通过停止监听让 myListener.AcceptTcpClient() 产生异常退出监听线程
                myListener.Stop();
            }
        }
        private class TCPClient
        {
            #region 外部接口
            public string userName { get; set; }
            public string ServerIP { get; set; }
            public Int32 port { get; set; }
            public string StatusMessage
            {
                get { return _statusmsg; }
                private set
                {
                    _statusmsg = value;
                    OnStatusChanged(new EventArgs());
                }
            }
            public List<string> OtherOnlineUsers { get; private set; }
            public string receiveString
            {
                get { return _RcvStr; }
                private set
                {
                    _RcvStr = value;
                    OnMsgReceived(new EventArgs());
                }
            }
            public bool ConnectStatus
            {
                get { return _connectStatus; }
                private set
                {
                    _connectStatus = value;
                    OnConnectStatusChanged(new EventArgs());
                }
            }
            #endregion
            #region 事件
            public event EventHandler MsgReceived;
            public event EventHandler StatusChanged;
            public event EventHandler ConnectStatusChanged;
            #endregion
            #region 内部变量
            private bool isExit = false;
            private TcpClient client;
            private BinaryReader br;
            private BinaryWriter bw;
            private string _statusmsg;
            private string _RcvStr;
            private bool _connectStatus;
            #endregion

            /// <summary>
            /// 关闭本客户端
            /// </summary>
            public void Close()
            {
                br.Close();
                bw.Close();
                client.Close();
            }
            /// <summary>
            /// 连接到服务端
            /// </summary>
            public void ConnectToServer()
            {
                try
                {
                    //此处为方便演示，实际使用时要将Dns.GetHostName()改为服务器域名
                    //IPAddress ipAd = IPAddress.Parse("182.150.193.7");
                    client = new TcpClient();
                    client.Connect(IPAddress.Parse(ServerIP), port);
                    StatusMessage = "连接成功" + Environment.NewLine;
                    ConnectStatus = true;
                }
                catch (Exception ex)
                {
                    StatusMessage = "连接失败，原因：" + ex.Message + Environment.NewLine;
                    ConnectStatus = false;
                    return;
                }
                //获取网络流
                NetworkStream networkStream = client.GetStream();
                //将网络流作为二进制读写对象
                br = new BinaryReader(networkStream);
                bw = new BinaryWriter(networkStream);
                OtherOnlineUsers = new List<string>();
                SendMessage("Login," + userName);
                Thread threadReceive = new Thread(new ThreadStart(ReceiveData));
                threadReceive.IsBackground = true;
                threadReceive.Start();
            }
            /// <summary>
            /// 向服务端发送消息
            /// </summary>
            /// <param name="message"></param>
            public void SendMessage(string message)
            {
                try
                {
                    //将字符串写入网络流，此方法会自动附加字符串长度前缀
                    bw.Write(message);
                    bw.Flush();
                }
                catch
                {
                    StatusMessage = "发送 " + message + " 至服务器失败" + Environment.NewLine;
                    ConnectStatus = false;
                }
            }
            /// <summary>
            /// 接收服务器信息
            /// </summary>
            public void ReceiveData()
            {
                receiveString = null;
                while (isExit == false)
                {
                    try
                    {
                        //从网络流中读出字符串
                        //此方法会自动判断字符串长度前缀，并根据长度前缀读出字符串
                        receiveString = br.ReadString();
                    }
                    catch
                    {
                        if (isExit == false)
                        {
                            StatusMessage = "与服务器失去连接" + Environment.NewLine;
                            ConnectStatus = false;
                        }
                        break;
                    }
                    #region 命令处理-暂不用
                    //string[] splitString = receiveString.Split(',');
                    //string command = splitString[0].ToLower();
                    //switch (command)
                    //{
                    //    case "login":   //格式： login,用户名
                    //        OtherOnlineUsers.Add(splitString[1]);
                    //        break;
                    //    case "logout":  //格式： logout,用户名
                    //        OtherOnlineUsers.Remove(splitString[1]);
                    //        break;
                    //    case "talk":    //格式： talk,用户名,对话信息
                    //        StatusMessage = splitString[1] + "：\r\n" + Environment.NewLine;
                    //        StatusMessage = receiveString.Substring(splitString[0].Length + splitString[1].Length + 2);
                    //        break;
                    //    default:
                    //        StatusMessage = "未知信息：" + receiveString;
                    //        break;
                    //}
                    #endregion
                }
                return;
            }

            private void OnStatusChanged(EventArgs eventArgs)
            {
                if (StatusChanged != null)
                {
                    StatusChanged(this, eventArgs);
                }
            }
            private void OnMsgReceived(EventArgs eventArgs)
            {
                if (MsgReceived != null)
                {
                    MsgReceived(this, eventArgs);
                }
            }
            private void OnConnectStatusChanged(EventArgs eventArgs)
            {
                if (ConnectStatusChanged != null)
                {
                    ConnectStatusChanged(this, eventArgs);
                }
            }
        }
        private class User
        {
            public TcpClient client { get; private set; }
            public BinaryReader br { get; private set; }
            public BinaryWriter bw { get; private set; }
            public string userName { get; set; }

            public User(TcpClient client)
            {
                client = client;
                NetworkStream networkStream = client.GetStream();
                br = new BinaryReader(networkStream);
                bw = new BinaryWriter(networkStream);
            }

            public void Close()
            {
                br.Close();
                bw.Close();
                client.Close();
            }

        }
    }
    public class SerialPortReadWrite
    {
        #region 相关属性

        /// <summary>
        /// 创建一个串口操作对象
        /// </summary>
        private SerialPort serialPort = null;
        public SerialPort SerialPortObject
        {
            get { return serialPort; }
        }

        /// <summary>
        /// 获取当前计算机上可用的端口列表【只读】
        /// </summary>
        public string[] PortNames
        {
            get { return System.IO.Ports.SerialPort.GetPortNames(); }
        }

        /// <summary>
        /// 构造方法中初始化相关数据
        /// </summary>
        public SerialPortReadWrite()
        {
            this.serialPort = new SerialPort();

            //串口基本参数初始化
            this.serialPort.BaudRate = 9600;
            this.serialPort.Parity = System.IO.Ports.Parity.None;//校验位默认NONE
            this.serialPort.DataBits = 8;//数据位默认8位
            this.serialPort.StopBits = System.IO.Ports.StopBits.One;//停止位默认1位

        }

        #endregion
        #region 打开或关闭端口

        /// <summary>
        /// 根据端口名称打开端口或关闭端口
        /// </summary>
        /// <param name="portName">端口名称</param>
        /// <param name="status">操作状态：1表示打开，0表示关闭</param>
        /// <returns>返回当前端口的打开状态true或false</returns>
        public bool OpenSerialPort(string portName, int status)
        {
            if (status == 1)
            {
                this.serialPort.PortName = portName;//只有在端口没有打开的时候，才能设置名称。
                this.serialPort.Open();
            }
            else
            {
                this.serialPort.Close();
            }
            return this.serialPort.IsOpen;//返回串口打开的状态
        }


        #endregion
        #region 发送数据

        /// <summary>
        /// 判断十六进制字符串hex是否正确
        /// </summary>
        /// <param name="hex">十六进制字符串</param>
        /// <returns>true：正确，false：不正确<returns>
        private bool IsIIlegalHex(string hex)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(hex, @"([^A-Fa-f0-9]|\s+?)+");
        }
        /// <summary>
        /// 发送数据（可以是16进制，也可以是字符串）
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <param name="format">发送的数据格式</param>
        public void SendData(string data, SendFormat format)
        {
            if (!this.serialPort.IsOpen)
            {
                throw new Exception("端口未打开！请打开相关端口！");//让调用者处理
            }
            else
            {
                byte[] byteData;

                if (format == SendFormat.Hex)//如果是16进制
                {
                    if (this.IsIIlegalHex(data))
                    {
                        byteData = RString.From16ToBtyes(data);//将16进制转换成byte[]数组
                    }
                    else
                    {
                        throw new Exception("数据不是16进制格式！");
                    }
                }
                else  //发送字符串
                {
                    byteData = RString.StringToBytes(data, false);
                }
                this.serialPort.Write(byteData, 0, byteData.Length);//发送数据（数据，从0开始，结束位置）
            }
        }

        #endregion
        #region 接收数据

        /// <summary>
        /// 串口接收数据
        /// </summary>
        /// <returns></returns>
        public byte[] ReceiveData()
        {
            //定义一个接收数组，获取接收缓冲区数据的字节数
            byte[] byteData = new byte[this.serialPort.BytesToRead];
            //读取数据
            this.serialPort.Read(byteData, 0, serialPort.BytesToRead);
            return byteData;
        }

        #endregion
        #region 发送格式选择枚举

        /// <summary>
        /// 发送格式选择
        /// </summary>
        public enum SendFormat
        {
            Hex,//十六进制
            String
        }

        #endregion
    }
    public class XMLReadWrite
    {
        #region XML
        /// <summary>
        /// 保存XML
        /// </summary>
        /// <typeparam name="T">保存类型(Class)</typeparam>
        /// <param name="obj">需保存的数据</param>
        /// <param name="Path">路径</param>
        /// <param name="MySerializType">类别</param>
        public static void ToSave<T>(T obj, string Path, System.Data.SerializationFormat MySerializType = System.Data.SerializationFormat.Xml)
        {
            FileInfo SaveFile = new FileInfo(Path);
            if (!SaveFile.Directory.Exists)
            {
                try
                {
                    SaveFile.Directory.Create();
                }
                catch { return; }
            }
            else
            {
                try
                {
                    if (SaveFile.Exists)
                        SaveFile.Delete();
                }
                catch { return; }
            }
            try
            {
                FileStream myFs = new FileStream(SaveFile.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                switch (MySerializType)
                {
                    case System.Data.SerializationFormat.Xml:
                        {
                            XmlSerializer myXS = new XmlSerializer(obj.GetType());
                            myXS.Serialize(myFs, obj);
                            break;
                        }
                    case System.Data.SerializationFormat.Binary:
                        {
                            BinaryFormatter myBny = new BinaryFormatter();
                            myBny.Serialize(myFs, obj);
                            break;
                        }
                }
                myFs.Close();
            }
            catch (Exception Ex)
            {
            }
        }
        /// <summary>
        /// 载入数据
        /// </summary>
        /// <typeparam name="T">载入数据类型(Class)</typeparam>
        /// <param name="obj">载入数据</param>
        /// <param name="Path">路径</param>
        /// <param name="MySerializType">类型</param>
        /// <returns></returns>
        public static bool ToTryLoad<T>(ref T obj, string Path, System.Data.SerializationFormat MySerializType = System.Data.SerializationFormat.Xml)
        {
            FileInfo LoadFile = new FileInfo(Path);
            if (!LoadFile.Exists)
                return false;
            FileStream myReader = new FileStream(LoadFile.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            try
            {
                switch (MySerializType)
                {
                    case System.Data.SerializationFormat.Xml:
                        {
                            XmlSerializer x = new XmlSerializer(obj.GetType());
                            obj = (T)x.Deserialize(myReader);
                            break;
                        }
                    case System.Data.SerializationFormat.Binary:
                        {
                            BinaryFormatter myBny = new BinaryFormatter();
                            obj = (T)myBny.Deserialize(myReader);
                            break;
                        }
                }
                myReader.Close();
            }
            catch (Exception Ex)
            {
                myReader.Close();
                return false;
            }
            return true;
        }
        #endregion
    }
    public class BindingReadWrite
    {
        #region Binding
        private static Dictionary<string, object> oldSaveParam = new Dictionary<string, object>();
        private static Dictionary<string, object> oldSaveParam_changeproduct = new Dictionary<string, object>();
        public static bool UpdataVal(object obj_class, BindingFlags flag, string Key, object Val)
        {
            try
            {
                obj_class.GetType().GetField(Key, flag).SetValue(obj_class, Val);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool UpdataVal(Type type, BindingFlags flag, string Key, object Val)
        {
            try
            {
                type.GetField(Key, flag).SetValue(type, Val);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static object GetVal(object obj_class, BindingFlags flag, string Key)
        {
            try
            {
                return obj_class.GetType().GetField(Key, flag).GetValue(obj_class);
            }
            catch (Exception)
            {
                return null;
            }
        }
        public abstract class EntityClassXmlSerializer<T>
        {
            public static string ToXMLString(T entity)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, null);
                    new XmlSerializer(entity.GetType()).Serialize(xmlTextWriter, entity);
                    xmlTextWriter.Formatting = Formatting.Indented;
                    using (StreamReader streamReader = new StreamReader(memoryStream, Encoding.UTF8))
                    {
                        memoryStream.Position = 0L;
                        string result = streamReader.ReadToEnd();
                        streamReader.Close();
                        memoryStream.Close();
                        return result;
                    }
                }
            }

            public static T ReadFromXML(string xml)
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                {
                    return (T)new XmlSerializer(typeof(T)).Deserialize(stream);
                }
            }
        }
        public static bool SerializeSave(string filepath, object obj)
        {
            try
            {
                FileStream fileStream = new FileStream(filepath, FileMode.Create);
                new BinaryFormatter().Serialize(fileStream, obj);
                fileStream.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static object SerializeLoad(string filepath)
        {
            try
            {
                FileStream fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
                object result = new BinaryFormatter().Deserialize(fileStream);
                fileStream.Close();
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static bool CompareObject(object objA, object objB)
        {
            bool result = false;
            if (objA == null || objB == null)
            {
                result = false;
            }
            else if (objA == DBNull.Value && objB != DBNull.Value)
            {
                result = false;
            }
            else if (objA != DBNull.Value && objB == DBNull.Value)
            {
                result = false;
            }
            else if (objA == DBNull.Value && objB == DBNull.Value)
            {
                result = true;
            }
            else if (objA.GetType() != objB.GetType())
            {
                result = false;
            }
            else if (objA is int || objA is short || objA is long || objA is float || objA is double || objA is decimal)
            {
                if (objA is int)
                {
                    if ((int)objA == (int)objB)
                    {
                        result = true;
                    }
                }
                else if (objA is short)
                {
                    if ((short)objA == (short)objB)
                    {
                        result = true;
                    }
                }
                else if (objA is long)
                {
                    if ((long)objA == (long)objB)
                    {
                        result = true;
                    }
                }
                else if (objA is float)
                {
                    if ((float)objA == (float)objB)
                    {
                        result = true;
                    }
                }
                else if (objA is double)
                {
                    if ((double)objA == (double)objB)
                    {
                        result = true;
                    }
                }
                else if (objA is decimal && (decimal)objA == (decimal)objB)
                {
                    result = true;
                }
            }
            else if (!objA.GetType().ToString().Contains("Dictionary"))
            {
                string a = EntityClassXmlSerializer<object>.ToXMLString(objA);
                string b = EntityClassXmlSerializer<object>.ToXMLString(objB);
                if (a == b)
                {
                    result = true;
                }
            }
            else
            {
                result = true;
            }

            return result;
        }
        public static Dictionary<string, object> GetListVal(Type type, BindingFlags flag)
        {
            try
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                FieldInfo[] fields = type.GetFields(flag);
                foreach (FieldInfo fieldInfo in fields)
                {
                    dictionary.Add(fieldInfo.Name, fieldInfo.GetValue(type));
                }

                return dictionary;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static bool SaveParam(Type type, string savepath)
        {
            try
            {
                Dictionary<string, object> listVal = GetListVal(type, BindingFlags.Static | BindingFlags.Public);
                if (!Directory.Exists(savepath))
                {
                    Directory.CreateDirectory(savepath);
                }

                foreach (string key in listVal.Keys)
                {
                    SerializeSave(savepath + "\\" + key, listVal[key]);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static int LoadParam(Type type, string loadpath)
        {
            Dictionary<string, object> dictionary = null;
            string name = type.Name;
            if (!(name == "Gold_RAM_Val"))
            {
                if (name == "Change_Gold_RAM_Val")
                {
                    dictionary = oldSaveParam_changeproduct;
                }
            }
            else
            {
                dictionary = oldSaveParam;
            }

            try
            {
                int num = 0;
                Dictionary<string, object> listVal = GetListVal(type, BindingFlags.Static | BindingFlags.Public);
                if (!Directory.Exists(loadpath))
                {
                    Directory.CreateDirectory(loadpath);
                }

                foreach (string key in listVal.Keys)
                {
                    if (File.Exists(loadpath + "\\" + key))
                    {
                        object obj = SerializeLoad(loadpath + "\\" + key);
                        UpdataVal(type, BindingFlags.Static | BindingFlags.Public, key, obj);
                        dictionary.Add(key, obj);
                    }
                    else
                    {
                        num = 1;
                    }
                }

                if (num == 1)
                {
                    return 1;
                }

                return 0;
            }
            catch (Exception)
            {
                return -1;
            }
        }
        public static bool SaveParamChange(Type type)
        {
            Dictionary<string, object> dictionary = null;
            string name = type.Name;
            if (!(name == "Gold_RAM_Val"))
            {
                if (name == "Change_Gold_RAM_Val")
                {
                    dictionary = oldSaveParam_changeproduct;
                }
            }
            else
            {
                dictionary = oldSaveParam;
            }

            Dictionary<string, object> listVal = GetListVal(type, BindingFlags.Static | BindingFlags.Public);
            foreach (string key in listVal.Keys)
            {
                object objB = listVal[key];
                if (dictionary.ContainsKey(key))
                {
                    if (!CompareObject(dictionary[key], objB))
                    {
                        return true;
                    }

                    continue;
                }

                return true;
            }

            return false;
        }
        #endregion
    }
    public class RegeditReadWrite
    {
        /// <summary>
        /// 读取的注册表中HKEY\HKEYSubKey目录下的SubKey目录中名称为name的注册表值
        /// </summary>
        /// <param name="HKEY">HKEY</param>
        /// <param name="HKEYSubKey">目录</param>
        /// <param name="SubKey">子键</param>
        /// <param name="name">名称</param>
        /// <returns>注册表值</returns>
        private string GetRegistData(RegistryKey HKEY, string HKEYSubKey, string SubKey, string name)
        {
            string registData;
            RegistryKey hkml = HKEY;
            RegistryKey software = hkml.OpenSubKey(HKEYSubKey, true);
            RegistryKey aimdir = software.OpenSubKey(SubKey, true);
            registData = aimdir.GetValue(name).ToString();
            return registData;
        }
        /// <summary>
        /// 在注册表中HKEY\HKEYSubKey目录下新建SubKey目录并在此目录下创建名称为name,值为tovalue的注册表项
        /// </summary>
        /// <param name="HKEY">HKEY</param>
        /// <param name="HKEYSubKey">目录</param>
        /// <param name="SubKey">子键</param>
        /// <param name="name">名称</param>
        /// <param name="tovalue">写入值</param>
        private void WriteRegedit(RegistryKey HKEY, string HKEYSubKey, string SubKey, string name, string tovalue)
        {
            RegistryKey hklm = HKEY;
            RegistryKey software = hklm.OpenSubKey(HKEYSubKey, true);
            RegistryKey aimdir = software.CreateSubKey(SubKey);
            aimdir.SetValue(name, tovalue);
        }
        /// <summary>
        /// 在注册表中HKEY\HKEYSubKey目录下SubKey目录中删除名称为name注册表项
        /// </summary>
        /// <param name="HKEY">HKEY</param>
        /// <param name="HKEYSubKey">目录</param>
        /// <param name="SubKey">子键</param>
        /// <param name="name">名称</param>
        private void DeleteRegist(RegistryKey HKEY, string HKEYSubKey, string SubKey, string name)
        {
            string[] aimnames;
            RegistryKey hkml = HKEY;
            RegistryKey software = hkml.OpenSubKey(HKEYSubKey, true);
            RegistryKey aimdir = software.OpenSubKey(SubKey, true);
            aimnames = aimdir.GetSubKeyNames();
            foreach (string aimKey in aimnames)
            {
                if (aimKey == name)
                    aimdir.DeleteSubKeyTree(name);
            }
        }
        /// <summary>
        /// 判断在注册表中HKEY\HKEYSubKey目录下SubKey目录中是否存在名称为name注册表项
        /// </summary>
        /// <param name="HKEY">HKEY</param>
        /// <param name="HKEYSubKey">目录</param>
        /// <param name="SubKey">子键</param>
        /// <param name="name">名称</param>
        private bool IsRegeditExit(RegistryKey HKEY, string HKEYSubKey, string SubKey, string name)
        {
            bool _exsit = false;
            string[] subkeyNames;
            RegistryKey hkml = HKEY;
            RegistryKey software = hkml.OpenSubKey(HKEYSubKey, true);
            RegistryKey aimdir = software.OpenSubKey(SubKey, true);
            subkeyNames = aimdir.GetSubKeyNames();
            foreach (string keyName in subkeyNames)
            {
                if (keyName == name)
                {
                    _exsit = true;
                    return _exsit;
                }
            }
            return _exsit;
        }
    }
    public class DataCal
    {
        /// <summary>
        /// 获取List 中的最小值
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public  double GetMinValue(List<double> list)
        {
            double num = list[0];
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] < num)
                {
                    num = list[i];
                }
            }
            return num;
        }
        /// <summary>
        /// 获取List 中的最大值
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public  double GetMaxValue(List<double> list)
        {
            double num = list[0];
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] > num)
                {
                    num = list[i];
                }
            }
            return num;
        }
        /// <summary>
        /// 获取List 中的平均值
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public  double GetAverage(List<double> list)
        {
            return list.Average();
        }
    }
    public class SerHelper
    {

        /// <summary>
        /// 将实体序列化成byte[]数组
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] Serialize(object obj)
        {
            // 1.0 实例化流的序列化帮助类
            BinaryFormatter bf = new BinaryFormatter();
            //2.0 定义内存流，用来接收通过bf的Serialize（）方法将obj对象序列化成byte[]字节数组
            byte[] res;
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);

                res = ms.ToArray();
            }

            return res;
        }

        /// <summary>
        /// 根据byte[]字节数组反序列化成 对象实体
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static T Deserialize<T>(byte[] buffer)
        {
            // 1.0 实例化流的序列化帮助类
            BinaryFormatter bf = new BinaryFormatter();
            T obj;
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                obj = (T)bf.Deserialize(ms);
            }
            //bf.Deserialize(
            return obj;
        }
        /// <summary>
        /// 根据byte[]字节数组反序列化成 对象实体
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static object Deserialize(byte[] datas, int index)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream stream = new MemoryStream(datas, index, datas.Length - index);
            object obj = bf.Deserialize(stream);
            stream.Dispose();
            return obj;
        }
    }
    public  class ImageHelper
    {
        /// <summary>
        /// Convert Image to Byte[]
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static byte[] ImageToBytes(Image image)
        {
            ImageFormat format = image.RawFormat;
            using (MemoryStream ms = new MemoryStream())
            {
                if (format.Equals(ImageFormat.Jpeg))
                {
                    image.Save(ms, ImageFormat.Jpeg);
                }
                else if (format.Equals(ImageFormat.Png))
                {
                    image.Save(ms, ImageFormat.Png);
                }
                else if (format.Equals(ImageFormat.Bmp))
                {
                    image.Save(ms, ImageFormat.Bmp);
                }
                else if (format.Equals(ImageFormat.Gif))
                {
                    image.Save(ms, ImageFormat.Gif);
                }
                else if (format.Equals(ImageFormat.Icon))
                {
                    image.Save(ms, ImageFormat.Icon);
                }
                byte[] buffer = new byte[ms.Length];
                //Image.Save()会改变MemoryStream的Position，需要重新Seek到Begin
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        /// <summary>
        /// Convert Byte[] to Image
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Image BytesToImage(byte[] buffer)
        {
            MemoryStream ms = new MemoryStream(buffer);
            Image image = System.Drawing.Image.FromStream(ms);
            return image;
        }

        /// <summary>
        /// Convert Byte[] to a picture and Store it in file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string CreateImageFromBytes(string fileName, byte[] buffer)
        {
            string file = fileName;
            Image image = BytesToImage(buffer);
            ImageFormat format = image.RawFormat;
            if (format.Equals(ImageFormat.Jpeg))
            {
                file += ".jpeg";
            }
            else if (format.Equals(ImageFormat.Png))
            {
                file += ".png";
            }
            else if (format.Equals(ImageFormat.Bmp))
            {
                file += ".bmp";
            }
            else if (format.Equals(ImageFormat.Gif))
            {
                file += ".gif";
            }
            else if (format.Equals(ImageFormat.Icon))
            {
                file += ".icon";
            }
            System.IO.FileInfo info = new System.IO.FileInfo(file);
            System.IO.Directory.CreateDirectory(info.Directory.FullName);
            File.WriteAllBytes(file, buffer);
            return file;
        }
    }
    public  class MD5Helper
    {
        public static string CretaeMD5(string fileName)
        {
            string hashStr = string.Empty;
            try
            {
                FileStream fs = new FileStream(
                    fileName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                byte[] hash = md5.ComputeHash(fs);
                hashStr = ByteArrayToHexString(hash);
                fs.Close();
                fs.Dispose();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return hashStr;
        }

        public static string CretaeMD5(Stream stream)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(stream);
            return ByteArrayToHexString(hash);
        }

        public static string CretaeMD5(byte[] buffer, int offset, int count)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(buffer, offset, count);
            return ByteArrayToHexString(hash);
        }

        private static string ByteArrayToHexString(byte[] values)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte value in values)
            {
                sb.AppendFormat("{0:X2}", value);
            }
            return sb.ToString();
        }
    }

}
