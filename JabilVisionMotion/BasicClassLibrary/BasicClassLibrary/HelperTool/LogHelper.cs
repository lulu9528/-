using log4net;
using log4net.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 日志等级
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 调试
        /// </summary>
        Debug,
        /// <summary>
        /// 信息
        /// </summary>
        Info,
        /// <summary>
        /// 错误
        /// </summary>
        Error,
        /// <summary>
        /// 报警
        /// </summary>
        Warn,
        /// <summary>
        /// 
        /// </summary>
        Fatal
    }
    /// <summary>
    /// 日志内容
    /// </summary>
    public class LogMessage
    {

        /// <summary>
        /// 异常
        /// </summary>
        public System.Exception Exception { get; set; }

        /// <summary>
        /// 日志等级
        /// </summary>
        public HelperTool.LogLevel Level { get; set; }

        /// <summary>
        /// 日志字符串
        /// </summary>
        public string Message { get; set; }
    }
    /// <summary>
    /// 日志委托
    /// </summary>
    /// <param name="message">日志信息</param>
    /// <param name="level">日志等级</param>
    public delegate void LogHandler(string message, HelperTool.LogLevel level);
    /// <summary>
    /// 日志记录类
    /// </summary>
    public class LogHelper : SingletonTemplate<LogHelper>
    {
        /// <summary>
        /// 是否注册
        /// </summary>
        private static bool IsValid = (Security.GetRemainDays() > 0);
        private bool m_bAddClassName = false;
        private bool m_bAddTime = true;
        /// <summary>
        /// 线程运行标志
        /// </summary>
        private bool m_bRunning = true;
        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILog m_log;
        /// <summary>
        /// 信号
        /// </summary>
        private readonly ManualResetEvent m_mREvent;
        /// <summary>
        /// 记录消息Queue
        /// </summary>
        private readonly ConcurrentQueue<LogMessage> m_queLog;
        private static string m_strConfig = "stlog4net.config";
        private static string m_strLogger = "loginfo";
        /// <summary>
        /// 写日志线程
        /// </summary>
        private Thread m_thread;


        /// <summary>
        /// 日志事件
        /// </summary>

        public event LogHandler LogEvent;

        /// <summary>
        /// 构造函数
        /// </summary>
        public LogHelper()
        {
            FileInfo configFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, m_strConfig));
            if (!configFile.Exists)
            {
                using (StreamWriter writer = new StreamWriter(configFile.FullName))
                {
                    ///David
                    //writer.Write(Properties.Resource.stlog4net);
                    //writer.Flush();
                    //writer.Close();
                }
            }
            XmlConfigurator.Configure(configFile);
            this.m_log = LogManager.GetLogger(m_strLogger);
            this.m_queLog = new ConcurrentQueue<LogMessage>();
            this.m_mREvent = new ManualResetEvent(false);
            this.m_thread = new Thread(new ThreadStart(this.ThreadProc));
            this.m_thread.IsBackground = false;
            this.m_thread.Start();
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="message">日志文本</param>
        /// <param name="level">等级</param>
        /// <param name="ex">Exception</param>
        public void AddMessage(string message, HelperTool.LogLevel level, Exception ex = null)
        {
            StringBuilder builder = new StringBuilder();
            if (this.m_bAddTime)
            {
                builder.Append("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] - ");
            }
            if (this.m_bAddClassName)
            {
                MethodBase method;
                StackTrace trace = new StackTrace();
                int index = 1;
                do
                {
                    method = trace.GetFrame(index).GetMethod();
                    index++;
                }
                while (method.ReflectedType.Name == base.GetType().Name);
                builder.Append("[" + method.ReflectedType.Name + "] - ");
            }
            builder.Append("[" + level.ToString() + "] - ");
            builder.Append(message);
            if (this.LogEvent != null)
            {
                this.LogEvent(builder.ToString(), level);
            }
            if (((((level == HelperTool.LogLevel.Debug) && this.m_log.IsDebugEnabled) || ((level == HelperTool.LogLevel.Error) && this.m_log.IsErrorEnabled)) || (((level == HelperTool.LogLevel.Fatal) && this.m_log.IsFatalEnabled) || ((level == HelperTool.LogLevel.Info) && this.m_log.IsInfoEnabled))) || ((level == HelperTool.LogLevel.Warn) && this.m_log.IsWarnEnabled))
            {
                LogMessage item = new LogMessage
                {
                    Message = builder.ToString(),
                    Level = level,
                    Exception = ex
                };
                this.m_queLog.Enqueue(item);
                this.m_mREvent.Set();
            }
        }

        /// <summary>
        /// 调试
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        public static void Debug(string msg, Exception ex = null)
        {
            LogHelper.GetInstance().AddMessage(msg, HelperTool.LogLevel.Debug, ex);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            this.m_bRunning = false;
            this.m_mREvent.Set();
            if (this.m_thread != null)
            {
                if (!this.m_thread.Join(50))
                {
                    this.m_thread.Abort();
                }
                this.m_thread = null;
            }
        }

        /// <summary>
        /// 错误
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        public static void Error(string msg, Exception ex = null)
        {
            LogHelper.GetInstance().AddMessage(msg, HelperTool.LogLevel.Error, ex);
        }

        /// <summary>
        /// 致命
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        public static void Fatal(string msg, Exception ex = null)
        {
            LogHelper.GetInstance().AddMessage(msg, HelperTool.LogLevel.Fatal, ex);
        }

        /// <summary>
        /// 信息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        public static void Info(string msg, Exception ex = null)
        {
            LogHelper.GetInstance().AddMessage(msg, HelperTool.LogLevel.Info, ex);
        }

        private void ThreadProc()
        {
            while (this.m_bRunning)
            {
                LogMessage message;
                this.m_mREvent.WaitOne(0x1388);
                while (((this.m_queLog.Count > 0) && this.m_queLog.TryDequeue(out message)) && this.m_bRunning)
                {
                    switch (message.Level)
                    {
                        case HelperTool.LogLevel.Debug:
                            this.m_log.Debug(message.Message, message.Exception);
                            break;

                        case HelperTool.LogLevel.Info:
                            this.m_log.Info(message.Message, message.Exception);
                            break;

                        case HelperTool.LogLevel.Error:
                            this.m_log.Error(message.Message, message.Exception);
                            break;

                        case HelperTool.LogLevel.Warn:
                            this.m_log.Warn(message.Message, message.Exception);
                            break;

                        case HelperTool.LogLevel.Fatal:
                            this.m_log.Fatal(message.Message, message.Exception);
                            break;
                    }
                }
                this.m_mREvent.Reset();
            }
        }

        /// <summary>
        /// 警告
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        public static void Warn(string msg, Exception ex = null)
        {
            LogHelper.GetInstance().AddMessage(msg, HelperTool.LogLevel.Warn, ex);
        }

        /// <summary>
        /// 配置文件
        /// </summary>
        public static string Config
        {
            get
            {
                return m_strConfig;
            }
            set
            {
                m_strConfig = value;
            }
        }

        /// <summary>
        /// 是否插入调用类名
        /// </summary>
        public bool IsAddClassName
        {
            get
            {
                return this.m_bAddClassName;
            }
            set
            {
                this.m_bAddClassName = value;
            }
        }

        /// <summary>
        /// 是否插入时间
        /// </summary>
        public bool IsAddTime
        {
            get
            {
                return this.m_bAddTime;
            }
            set
            {
                this.m_bAddTime = value;
            }
        }

        /// <summary>
        /// 配置文件中配置的Logger
        /// </summary>
        public static string Logger
        {
            get
            {
                return m_strLogger;
            }
            set
            {
                m_strLogger = value;
            }
        }
    }
}
