using Jabil.Module.Alarm.Common;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jabil.Module.Alarm.Common.Dtos
{
    /// <summary>
    /// 过程错误数据传输对象(DTO)
    /// 用于在服务层与表现层之间传输过程错误相关数据
    /// 继承自BaseDto，支持属性变更通知
    /// </summary>
    public class ProcessErrorDto
    {
        private string _alarmId;
        /// <summary>
        /// 报警ID
        /// 错误报警的唯一标识符
        /// </summary>
        public string 报警ID
        {
            get => _alarmId;
            set
            {
                if (_alarmId != value)
                {
                    _alarmId = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _errorStartTime;
        /// <summary>
        /// 异常开始时间
        /// 错误发生的具体时间
        /// </summary>
        public string 异常开始时间
        {
            get => _errorStartTime;
            set
            {
                if (_errorStartTime != value)
                {
                    _errorStartTime = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _endTime;
        /// <summary>
        /// 结束时间
        /// 错误被解决的时间
        /// </summary>
        public string 结束时间
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _alarmContent;
        /// <summary>
        /// 报警内容
        /// 错误的详细描述信息
        /// </summary>
        public string 报警内容
        {
            get => _alarmContent;
            set
            {
                if (_alarmContent != value)
                {
                    _alarmContent = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _duration;
        /// <summary>
        /// 耗时
        /// 错误从发生到解决的持续时间（单位：分钟）
        /// </summary>
        public double 耗时
        {
            get => _duration;
            set
            {
                if (_duration != value)
                {
                    _duration = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _date;
        /// <summary>
        /// 日期
        /// 错误发生的日期
        /// </summary>
        public DateTime 日期
        {
            get => _date;
            set
            {
                if (_date != value)
                {
                    _date = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _station;
        /// <summary>
        /// 站别
        /// 发生错误的站点或工位标识
        /// </summary>
        public string 站别
        {
            get => _station;
            set
            {
                if (_station != value)
                {
                    _station = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 属性变更事件
        /// 当属性值发生变化时触发
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// </summary>
        /// <param name="propertyName">属性名称，默认使用调用者属性名</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            // 安全触发事件，避免空引用异常
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
