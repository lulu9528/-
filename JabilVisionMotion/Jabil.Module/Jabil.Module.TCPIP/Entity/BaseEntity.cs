using SqlSugar;
using System;

namespace Jabil.Module.TCPIP.Entity
{
    /// <summary>
    /// 实体基类，提供统一的主键和基础字段
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 设备名称（主键）
        /// </summary>
        [SugarColumn(
            ColumnName = "设备名称",
            IsPrimaryKey = true,
            ColumnDescription = "设备唯一标识名称")]
        public string 设备名称 { get; set; }
    }
}
