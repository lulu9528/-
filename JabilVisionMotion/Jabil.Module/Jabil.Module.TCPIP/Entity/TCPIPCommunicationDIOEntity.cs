using SqlSugar;
using System;

namespace Jabil.Module.TCPIP.Entity
{
    /// <summary>
    /// TCPIP通讯DIO实体类，存储DIO点位的配置信息
    /// </summary>
    [SugarTable("TCPIPCommunicationDIO", TableDescription = "TCPIP通讯DIO点位配置表")]
    public class TcpIpCommunicationDioEntity : BaseEntity
    {
        /// <summary>
        /// 点位名称（联合主键）
        /// </summary>
        [SugarColumn(
            ColumnName = "点位名称",
            IsPrimaryKey = true,
            ColumnDescription = "DIO点位的唯一名称")]
        public string 点位名称 { get; set; }

        /// <summary>
        /// 点位类型
        /// </summary>
        [SugarColumn(
            ColumnName = "点位类型",
            ColumnDescription = "DIO点位类型（输入/输出等）",
            Length = 50)]
        public string 点位类型 { get; set; }

        /// <summary>
        /// 通讯格式定义
        /// </summary>
        [SugarColumn(
            ColumnName = "通讯格式定义",
            ColumnDescription = "该点位的数据通讯格式规范",
            Length = 200)]
        public string 通讯格式定义 { get; set; }

        /// <summary>
        /// 绑定代码
        /// </summary>
        [SugarColumn(
            ColumnName = "绑定代码",
            ColumnDescription = "与该点位绑定的业务代码",
            Length = 100)]
        public string 绑定代码 { get; set; }

    }
}
