using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Vision.Entity
{
    /// <summary>
    /// 实体基类
    /// 所有数据库实体类的抽象基类，提供统一的主键和审计字段
    /// 确保实体类的一致性和可维护性
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 主键ID
        /// 数据库自增主键，唯一标识一条记录
        /// </summary>
        [SugarColumn(
            ColumnName = nameof(ID),
            IsPrimaryKey = true,
            IsIdentity = true,
            ColumnDescription = "记录唯一标识")]
        public int ID { get; set; }
    }
}
