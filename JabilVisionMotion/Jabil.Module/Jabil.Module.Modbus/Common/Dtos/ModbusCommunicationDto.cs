using Jabil.Lib.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace Jabil.Module.Modbus.Common.Dtos
{
    /// <summary>
    /// Modbus通信配置数据传输对象
    /// 用于Modbus设备通信参数的传输、验证与展示
    /// 与ModbusCommunicationEntity实体类一一映射
    /// </summary>
    public class ModbusCommunicationDto : BaseDto
    {
        private string _deviceType;
        private byte _stationNumber;
        private string _deviceIp;
        private int _devicePort = 502;
        private int _timeout = 3000;

        /// <summary>
        /// 设备类型
        /// 描述Modbus设备的型号或类型信息，如"汇川H5U"、"西门子S7-1200"等
        /// </summary>
        public string 设备类型
        {
            get => _deviceType;
            set => SetProperty(ref _deviceType, value);
        }

        /// <summary>
        /// 站号
        /// Modbus设备的站号地址（1-247），符合Modbus协议规范
        /// </summary>
        public byte 站号
        {
            get => _stationNumber;
            set => SetProperty(ref _stationNumber, value);
        }

        /// <summary>
        /// 设备IP地址
        /// Modbus TCP设备的网络地址，需符合IPv4格式（如"192.168.1.100"）
        /// </summary>
        public string 设备IP
        {
            get => _deviceIp;
            set => SetProperty(ref _deviceIp, value);
        }

        /// <summary>
        /// 设备端口号
        /// Modbus TCP通信端口（1-65535），默认502端口
        /// </summary>
        public int 设备Port
        {
            get => _devicePort;
            set => SetProperty(ref _devicePort, value);
        }

        /// <summary>
        /// 通信超时时间
        /// 单位：毫秒，默认3000ms，用于设置Modbus通信的超时阈值
        /// </summary>
        public int 超时
        {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }


        /// <summary>
        /// 验证当前DTO数据的有效性
        /// 确保所有属性值符合业务规则和协议规范
        /// </summary>
        /// <returns>验证结果及错误信息列表</returns>
        public new (bool IsValid, List<string> Errors)  Validate()
        {
            var errors = new List<string>();

            // 设备类型验证
            if (string.IsNullOrWhiteSpace(设备类型))
                errors.Add("设备类型不能为空");
            else if (设备类型.Length > 100)
                errors.Add("设备类型长度不能超过100个字符");

            // 站号验证（1-247）
            if (站号 < 1 || 站号 > 247)
                errors.Add("站号必须在1-247范围内");

            // IP地址验证
            if (string.IsNullOrWhiteSpace(设备IP))
                errors.Add("设备IP地址不能为空");
            else if (!IPAddress.TryParse(设备IP, out _))
                errors.Add("设备IP地址格式无效");

            // 端口号验证（1-65535）
            if (设备Port < 1 || 设备Port > 65535)
                errors.Add("端口号必须在1-65535范围内");

            // 超时时间验证（大于0）
            if (超时 <= 0)
                errors.Add("超时时间必须大于0毫秒");
            else if (超时 > 30000)
                errors.Add("超时时间不能超过30000毫秒");

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 克隆当前DTO实例
        /// 用于创建属性值完全相同的新对象
        /// </summary>
        /// <returns>新的DTO副本</returns>
        public ModbusCommunicationDto Clone()
        {
            return new ModbusCommunicationDto
            {
                ID = this.ID,
                设备名称 = this.设备名称,
                设备类型 = this.设备类型,
                站号 = this.站号,
                设备IP = this.设备IP,
                设备Port = this.设备Port,
                超时 = this.超时,
                是否启用 = this.是否启用
            };
        }

        /// <summary>
        /// 重写ToString方法
        /// 提供设备的关键信息标识
        /// </summary>
        /// <returns>设备标识字符串</returns>
        public override string ToString()
        {
            return $"{设备名称}({设备类型}) - {设备IP}:{设备Port} 站号:{站号}";
        }
    }
}