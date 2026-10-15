using Jabil.Lib.Common;
using System;
using System.Runtime.CompilerServices;

namespace Jabil.Module.Serialport.Common.Dtos
{
    /// <summary>
    /// 串口DIO（数字输入输出）数据传输对象
    /// 用于在各层之间传递串口DIO配置信息
    /// </summary>
    public class SerialportDIODto : BaseDto
    {
        private string _pointName;
        /// <summary>
        /// 点位名称
        /// </summary>
        public string 点位名称
        {
            get => _pointName;
            set
            {
                if (_pointName != value)
                {
                    _pointName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _pointType;
        /// <summary>
        /// 点位类型（输入/输出等）
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

        private string _communicationFormat;
        /// <summary>
        /// 通讯格式定义
        /// </summary>
        public string 通讯格式定义
        {
            get => _communicationFormat;
            set
            {
                if (_communicationFormat != value)
                {
                    _communicationFormat = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _bindingCode;
        /// <summary>
        /// 绑定代码
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
    }
}
