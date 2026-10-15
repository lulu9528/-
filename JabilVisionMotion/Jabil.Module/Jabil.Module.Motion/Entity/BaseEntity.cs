using SqlSugar;
using System;

namespace Jabil.Module.Motion.Entity
{
    /// <summary>
    /// 实体基类
    /// 所有业务实体类应继承此类，提供统一的主键和审计字段
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(
            ColumnName = nameof(ID),
            IsPrimaryKey = true,
            IsIdentity = true,
            ColumnDescription = "主键ID，自增"
        )]
        public int ID { get; set; }

    }
}
