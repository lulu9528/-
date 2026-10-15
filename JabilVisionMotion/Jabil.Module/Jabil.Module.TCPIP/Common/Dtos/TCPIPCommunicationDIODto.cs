using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jabil.Module.TCPIP.Common.Dtos
{
    /// <summary>
    /// TCPIP通讯DIO数据传输对象，用于DIO点位配置的数据交互
    /// </summary>
    public class TcpIpCommunicationDioDto : BaseDto
    {
        private string _pointName;
        /// <summary>
        /// 点位名称（联合主键）
        /// </summary>
        public string 点位名称
        {
            get => _pointName;
            set => SetProperty(ref _pointName, value);
        }

        private string _pointType;
        /// <summary>
        /// 点位类型（输入/输出等）
        /// </summary>
        public string 点位类型
        {
            get => _pointType;
            set => SetProperty(ref _pointType, value);
        }

        private string _communicationFormat;
        /// <summary>
        /// 通讯格式定义
        /// </summary>
        public string 通讯格式定义
        {
            get => _communicationFormat;
            set => SetProperty(ref _communicationFormat, value);
        }

        private string _bindingCode;
        /// <summary>
        /// 绑定代码
        /// </summary>
        public string 绑定代码
        {
            get => _bindingCode;
            set => SetProperty(ref _bindingCode, value);
        }

    }
}
