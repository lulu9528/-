using Jabil.Module.Serialport.Entity;
using SqlSugar;
using System;

namespace Jabil.Module.Serialport.Entities
{
    /// <summary>
    /// 串口配置实体类
    /// 对应数据库表：Serialport
    /// </summary>
    [SugarTable("Serialport")]
    public class Serialport : BaseEntity
    {
        /// <summary>
        /// 主键ID，自增
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int ID { get; set; }

        /// <summary>
        /// 波特率（串口通信速率）
        /// </summary>
        [SugarColumn(ColumnName = "波特率", IsNullable = false)]
        public int 波特率 { get; set; }

        /// <summary>
        /// 校验位（0-无校验，1-奇校验，2-偶校验）
        /// </summary>
        [SugarColumn(ColumnName = "校验位", IsNullable = false)]
        public int 校验位 { get; set; }

        /// <summary>
        /// 数据位（通常为8）
        /// </summary>
        [SugarColumn(ColumnName = "数据位", IsNullable = false)]
        public int 数据位 { get; set; }

        /// <summary>
        /// 停止位（通常为1）
        /// </summary>
        [SugarColumn(ColumnName = "停止位", IsNullable = false)]
        public int 停止位 { get; set; }

        /// <summary>
        /// 超时时间（毫秒）
        /// </summary>
        [SugarColumn(ColumnName = "超时", IsNullable = false)]
        public int 超时 { get; set; }
    }
}
