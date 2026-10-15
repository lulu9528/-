using SqlSugar;
using System;

namespace Jabil.Module.Serialport.Entity
{
    /// <summary>
    /// 串口相关实体的基类，包含通用的COM口主键定义
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// COM口号（主键）
        /// </summary>
        /// <remarks>
        /// 例如："COM1", "COM2"等串口标识
        /// </remarks>
        [SugarColumn(
            ColumnName = nameof(COM口),
            IsPrimaryKey = true,
            ColumnDescription = "COM口")]
        public string COM口 { get; set; } = string.Empty;
    }
}
