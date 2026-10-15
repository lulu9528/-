using Jabil.Module.Alarm.Common;
using System;

namespace Jabil.Module.Alarm.Common.Dtos
{
    /// <summary>
    /// 报警通信DIO数据传输对象(DTO)
    /// 用于在服务层与表现层之间传输报警通信数字输入输出设备的配置和状态信息
    /// 继承自BaseDto，支持属性变更通知
    /// </summary>
    public class AlarmCommunicationDIODto : BaseDto
    {
        private string _pointType;
        /// <summary>
        /// 点位类型
        /// 描述DIO点位的类型（如输入、输出等）
        /// </summary>
        public string 点位类型
        {
            get => _pointType;
            set
            {
                if (_pointType != value)
                {
                    _pointType = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _pointIndex;
        /// <summary>
        /// 点位索引
        /// DIO点位的唯一索引，用于标识不同点位
        /// </summary>
        public string 点位索引
        {
            get => _pointIndex;
            set
            {
                if (_pointIndex != value)
                {
                    _pointIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _alarmContent;
        /// <summary>
        /// 报警内容
        /// 当该点位触发报警时的具体描述信息
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


        private int _priority;
        /// <summary>
        /// 优先级
        /// 报警的优先级级别（数字越大优先级越高）
        /// </summary>
        public int 优先级
        {
            get => _priority;
            set
            {
                if (_priority != value)
                {
                    _priority = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _alarmType;
        /// <summary>
        /// 报警类型
        /// 报警的分类（如设备故障、通讯中断等）
        /// </summary>
        public string 报警类型
        {
            get => _alarmType;
            set
            {
                if (_alarmType != value)
                {
                    _alarmType = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _bindingCode;
        /// <summary>
        /// 绑定代码
        /// 与该报警点位关联的业务代码或设备代码
        /// </summary>
        public string 绑定代码
        {
            get => _bindingCode;
            set
            {
                if (_bindingCode != value)
                {
                    _bindingCode = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _station;
        /// <summary>
        /// 站别
        /// 该报警点位所属的站点或工位信息
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
    }
}
