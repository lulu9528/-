using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;

namespace Jabil.Module.Modbus.Entity
{
    /// <summary>
    /// Modbus通信配置实体类
    /// 存储Modbus设备的通信连接参数配置，与数据库表ModbusCommunication映射
    /// </summary>
    [SugarTable("ModbusCommunication", TableDescription = "Modbus设备通信配置表")]
    public class ModbusCommunicationEntity : BaseEntity
    {
        /// <summary>
        /// 设备类型
        /// 描述Modbus设备的型号或类型信息（如PLC、机器人等具体型号）
        /// </summary>
        [SugarColumn(ColumnName = "设备类型", Length = 100, IsNullable = false, ColumnDescription = "Modbus设备的型号或类型")]
        [Required(ErrorMessage = "设备类型不能为空")]
        [MaxLength(100, ErrorMessage = "设备类型长度不能超过100个字符")]
        public string 设备类型 { get; set; }

        /// <summary>
        /// 站号
        /// Modbus设备的站号地址（1-247），用于在总线中唯一标识设备
        /// </summary>
        [SugarColumn(ColumnName = "站号", IsNullable = false, ColumnDescription = "Modbus设备站号(1-247)")]
        [Range(1, 247, ErrorMessage = "站号必须在1-247范围内")]
        public int 站号 { get; set; }

        /// <summary>
        /// 设备IP地址
        /// Modbus TCP设备的网络地址，采用IPv4格式（如192.168.1.1）
        /// </summary>
        [SugarColumn(ColumnName = "设备IP", Length = 50, IsNullable = false, ColumnDescription = "Modbus TCP设备的IPv4地址")]
        [Required(ErrorMessage = "设备IP不能为空")]
        [MaxLength(50, ErrorMessage = "设备IP长度不能超过50个字符")]
        [RegularExpression(@"^((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$", ErrorMessage = "IP地址格式不正确")]
        public string 设备IP { get; set; }

        /// <summary>
        /// 设备端口号
        /// Modbus TCP通信端口（默认502），范围1-65535
        /// </summary>
        [SugarColumn(ColumnName = "设备Port", IsNullable = false, ColumnDescription = "Modbus TCP通信端口(1-65535)")]
        [Range(1, 65535, ErrorMessage = "端口号必须在1-65535范围内")]
        public int 设备Port { get; set; } = 502;

        /// <summary>
        /// 超时时间
        /// 通信超时阈值（毫秒），超过该时间未响应则判定为通信失败
        /// </summary>
        [SugarColumn(ColumnName = "超时", IsNullable = false, ColumnDescription = "通信超时时间(毫秒)")]
        [Range(100, 30000, ErrorMessage = "超时时间必须在100-30000毫秒范围内")]
        public int 超时 { get; set; } = 3000;

    }
}