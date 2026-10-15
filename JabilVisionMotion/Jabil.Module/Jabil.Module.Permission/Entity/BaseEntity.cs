using System;
using SqlSugar; // 引入SqlSugar ORM框架命名空间

namespace Jabil.Module.Permission.Entity
{
    /// <summary>
    /// 实体基类
    /// 所有业务实体类的基类，封装了通用的字段和属性
    /// 使用SqlSugar特性配置数据库映射关系
    /// </summary>
    public class BaseEntity
    {
        /// <summary>
        /// 主键ID
        /// 数据库自增主键，用于唯一标识实体记录
        /// </summary>
        [SugarColumn(
            ColumnName = nameof(Id),  // 数据库列名与属性名保持一致（使用nameof避免硬编码）
            IsPrimaryKey = true,      // 标记为主键
            IsIdentity = true         // 标记为自增列
        )]
        public int Id { get; set; }

        /// <summary>
        /// 创建时间
        /// 记录实体的创建时间，默认为当前时间
        /// </summary>
        [SugarColumn(
            ColumnName = nameof(CreateTime)  // 数据库列名与属性名保持一致
        )]
        public DateTime CreateTime { get; set; } = DateTime.Now;  // 初始化默认值为当前时间

        /// <summary>
        /// 更新时间
        /// 记录实体最后一次更新的时间
        /// </summary>
        [SugarColumn(
            ColumnName = nameof(UpdateTime)  // 数据库列名与属性名保持一致
        )]
        public DateTime UpdateTime { get; set; }
    }
}