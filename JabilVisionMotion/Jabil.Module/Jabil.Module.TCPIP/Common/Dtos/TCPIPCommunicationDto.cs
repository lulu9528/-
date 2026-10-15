using System;

namespace Jabil.Module.TCPIP.Common.Dtos
{
    /// <summary>
    /// TCPIP通讯设备数据传输对象，用于设备网络配置的数据交互
    /// </summary>
    public class TcpIpCommunicationDto : BaseDto
    {
        private string _deviceType;
        /// <summary>
        /// 设备类型（型号或类别）
        /// </summary>
        public string 设备类型
        {
            get => _deviceType;
            set => SetProperty(ref _deviceType, value);
        }

        private int _deviceIndex;
        /// <summary>
        /// 设备索引（序号或唯一标识）
        /// </summary>
        public int 设备索引
        {
            get => _deviceIndex;
            set => SetProperty(ref _deviceIndex, value);
        }

        private string _deviceIp;
        /// <summary>
        /// 设备IP地址
        /// </summary>
        public string 设备IP
        {
            get => _deviceIp;
            set => SetProperty(ref _deviceIp, value);
        }

        private int _devicePort;
        /// <summary>
        /// 设备通讯端口号（1-65535）
        /// </summary>
        public int 设备Port
        {
            get => _devicePort;
            set => SetProperty(ref _devicePort, value);
        }

        private int _timeout;
        /// <summary>
        /// 通讯超时时间（毫秒）
        /// </summary>
        public int 超时
        {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }
    }
}
