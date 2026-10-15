using SqlSugar;
using System;

namespace Jabil.Module.TCPIP.Entity
{
    /// <summary>
    /// TCPIP通讯设备实体类，存储设备的网络配置信息
    /// </summary>
    [SugarTable("TCPIPCommunication", TableDescription = "TCPIP通讯设备配置表")]
    public class TcpIpCommunicationEntity : BaseEntity
    {
        /// <summary>
        /// 设备类型
        /// </summary>
        [SugarColumn(
            ColumnName = "设备类型",
            ColumnDescription = "设备的型号或类别",
            Length = 100,
            IsNullable = false)]
        public string 设备类型 { get; set; }

        /// <summary>
        /// 设备索引
        /// </summary>
        [SugarColumn(
            ColumnName = "设备索引",
            ColumnDescription = "设备的序号或唯一标识",
            IsNullable = false)]
        public int 设备索引 { get; set; }

        /// <summary>
        /// 设备IP地址
        /// </summary>
        [SugarColumn(
            ColumnName = "设备IP",
            ColumnDescription = "设备的网络IP地址",
            Length = 50,
            IsNullable = false)]
        public string 设备IP { get; set; }

        /// <summary>
        /// 设备端口号
        /// </summary>
        [SugarColumn(
            ColumnName = "设备Port",
            ColumnDescription = "设备的通讯端口号",
            IsNullable = false)]
        public int 设备Port { get; set; }

        /// <summary>
        /// 通讯超时时间(毫秒)
        /// </summary>
        [SugarColumn(
            ColumnName = "超时",
            ColumnDescription = "通讯操作的超时时间设置",
            IsNullable = false,
            DefaultValue = "5000")]
        public int 超时 { get; set; } = 5000;

    }
}
