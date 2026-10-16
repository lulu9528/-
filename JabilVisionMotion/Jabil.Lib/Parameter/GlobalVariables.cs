using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Lib.Parameter
{
    /// <summary>
    /// 全局变量存储类，用于在应用程序各模块间共享关键数据
    /// </summary>
    public class GlobalVariables
    {
        private bool _isTestMode = false;
        private double _cpuAbnormalMonitor;
        private bool _isProgramExiting = false;
        private string _alarmMessage;
        private string _alarmId;
        private readonly Stopwatch _ctStopwatch = new Stopwatch();

        /// <summary>
        /// 获取或设置是否启用测试模式
        /// </summary>
        public bool IsTestMode
        {
            get => _isTestMode;
            set => _isTestMode = value;
        }

        /// <summary>
        /// 获取或设置CPU异常监控值
        /// </summary>
        public double CpuAbnormalMonitor
        {
            get => _cpuAbnormalMonitor;
            set => _cpuAbnormalMonitor = value;
        }

        /// <summary>
        /// 获取或设置程序是否即将退出的状态
        /// </summary>
        public bool IsProgramExiting
        {
            get => _isProgramExiting;
            set => _isProgramExiting = value;
        }

        /// <summary>
        /// 获取或设置当前报警信息
        /// </summary>
        public string AlarmMessage
        {
            get => _alarmMessage;
            set => _alarmMessage = value;
        }

        /// <summary>
        /// 获取或设置当前报警ID
        /// </summary>
        public string AlarmId
        {
            get => _alarmId;
            set => _alarmId = value;
        }

        /// <summary>
        /// 获取CT（周期时间）计时器
        /// </summary>
        public Stopwatch CtStopwatch => _ctStopwatch;

        /// <summary>
        /// 重置所有全局变量到初始状态
        /// </summary>
        public void Reset()
        {
            _isTestMode = false;
            _cpuAbnormalMonitor = 0;
            _isProgramExiting = false;
            _alarmMessage = string.Empty;
            _alarmId = string.Empty;

            if (_ctStopwatch.IsRunning)
                _ctStopwatch.Stop();

            _ctStopwatch.Reset();
        }


        public int 治具选择 { get; set; }


        public int 手动流程选择 { get; set; }


        public int 手动流程选择等待 { get; set; }
    }


    public class Login
    {
        public Login(string userid, string password, string key)
        {
            UserId = userid;
            Password = password;
            Key = key;

        }


        public string UserId { get; set; }
        public string Password { get; set; }
        public string Key { get; set; }
    }


    public class Post
    {
        public Post(int _sid)
        {
            Sid = _sid;
        }
        public int Sid { get; set; }
    }


    public class LoginBackup
    {
        public int StatusCode { get; set; }

        public int Sid { get; set; }
    }



    public class LogoutBackup
    {
        public int StatusCode { get; set; }

    }
}
